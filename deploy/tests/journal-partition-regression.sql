BEGIN;
SET LOCAL statement_timeout = '30s';
SET LOCAL search_path = pg_temp, public;

-- LIKE uses the migrated column definitions; none of the application's real
-- data is copied. Identity sequences are also private to these temp tables.
CREATE TEMP TABLE "成果论文" (LIKE public."成果论文" INCLUDING DEFAULTS INCLUDING IDENTITY INCLUDING CONSTRAINTS);
CREATE TEMP TABLE "期刊" (LIKE public."期刊" INCLUDING DEFAULTS INCLUDING IDENTITY INCLUDING CONSTRAINTS);
CREATE TEMP TABLE "期刊分区库" (LIKE public."期刊分区库" INCLUDING DEFAULTS INCLUDING IDENTITY INCLUDING CONSTRAINTS);
CREATE TEMP TABLE "论文期刊分区匹配" (LIKE public."论文期刊分区匹配" INCLUDING DEFAULTS INCLUDING IDENTITY INCLUDING CONSTRAINTS);

CREATE FUNCTION pg_temp.run_journal_batch(integer, integer, integer)
RETURNS TABLE ("处理论文数" integer, "产生匹配数" integer, "下一起始论文编号" integer, "截止论文编号" integer, "是否还有论文" boolean)
LANGUAGE SQL VOLATILE AS $production$
-- PRODUCTION_BATCH_QUERY
$production$;

INSERT INTO "期刊" ("编号", "名称", "英文刊名", "ISSN号") VALUES
    (1, 'Research Heart', NULL, '1234-5678'),
    (2, 'Research, Heart!', NULL, NULL),
    (3, NULL, 'English Medical Journal', NULL),
    (4, 'Different journal display name', NULL, '12345678');
INSERT INTO "期刊分区库" ("来源", "年度", "期刊名称", "规范化名称", "ISSN", "学科类别", "分区", "影响因子") VALUES
    ('JCR', 2026, 'Research Heart', 'researchheart', '1234-5678', '医学', 'Q2', 3.5),
    ('JCR', 2025, 'Research Heart', 'researchheart', '1234-5678', '医学', 'Q3', 2.5),
    ('中科院分区', 2026, 'Research Heart', 'researchheart', '1234-5678', '医学', '2区', 3.5),
    ('JCR', 2026, 'English Medical Journal', 'englishmedicaljournal', NULL, '医学', 'Q4', 1.0);
INSERT INTO "成果论文" ("编号", "论文标题", "年度", "刊物编号")
SELECT n, 'Journal regression fixture ' || n, 2026, 1 FROM generate_series(1, 101) n;
INSERT INTO "成果论文" ("编号", "论文标题", "年度", "论文正式出版日期", "刊物编号") VALUES
    (102, 'Name matching fixture', 2026, NULL, 2),
    (103, 'English fallback fixture', 2026, NULL, 3),
    (104, 'ISSN matching fixture', 2026, NULL, 4),
    (105, 'Unmatched fixture', 2026, NULL, NULL),
    (106, 'Publication date fallback fixture', NULL, '2026-04-01', 1),
    (107, 'Explicit year precedence fixture', 2025, '2026-04-01', 1),
    (108, 'Unknown year fixture', NULL, NULL, 1);
INSERT INTO "论文期刊分区匹配" ("论文编号", "来源", "年度", "分区", "匹配方式", "确认状态") VALUES
    (1, 'JCR', 2026, 'Q1', '人工确认', '已确认'),
    (2, '中科院分区', 2026, '1区', '人工确认', '已驳回');

DO $test$
DECLARE r record;
BEGIN
    SELECT * INTO r FROM pg_temp.run_journal_batch(2026, 0, NULL);
    IF r."处理论文数" <> 100 OR r."产生匹配数" <> 198 OR
       r."下一起始论文编号" <> 100 OR r."截止论文编号" <> 106 OR NOT r."是否还有论文" THEN
        RAISE EXCEPTION 'First batch did not use migrated metadata or deterministic pagination: %', row_to_json(r);
    END IF;
    SELECT * INTO r FROM pg_temp.run_journal_batch(2026, 100, 106);
    IF r."处理论文数" <> 6 OR r."产生匹配数" <> 9 OR r."下一起始论文编号" <> 106 OR r."是否还有论文" THEN
        RAISE EXCEPTION 'Continuation dropped papers or produced wrong matches: %', row_to_json(r);
    END IF;
    IF NOT EXISTS (SELECT FROM "论文期刊分区匹配" WHERE "论文编号"=102 AND "匹配方式"='名称规范匹配') OR
       NOT EXISTS (SELECT FROM "论文期刊分区匹配" WHERE "论文编号"=103 AND "分区"='Q4') OR
       NOT EXISTS (SELECT FROM "论文期刊分区匹配" WHERE "论文编号"=104 AND "匹配方式"='ISSN精确') OR
       NOT EXISTS (SELECT FROM "论文期刊分区匹配" WHERE "论文编号"=106 AND "年度"=2026) OR
       EXISTS (SELECT FROM "论文期刊分区匹配" WHERE "论文编号" IN (105,107,108)) THEN
        RAISE EXCEPTION 'ISSN/name/English/date fallback or year filtering is incorrect';
    END IF;
    SELECT * INTO r FROM pg_temp.run_journal_batch(2026, 0, 106);
    IF r."产生匹配数" <> 0 THEN RAISE EXCEPTION 'Repeated batch created duplicates'; END IF;
    SELECT * INTO r FROM pg_temp.run_journal_batch(2026, 100, 106);
    IF r."产生匹配数" <> 0 THEN RAISE EXCEPTION 'Repeated continuation created duplicates'; END IF;
    IF NOT EXISTS (SELECT FROM "论文期刊分区匹配" WHERE "论文编号"=1 AND "来源"='JCR' AND "确认状态"='已确认' AND "分区"='Q1') OR
       NOT EXISTS (SELECT FROM "论文期刊分区匹配" WHERE "论文编号"=2 AND "来源"='中科院分区' AND "确认状态"='已驳回' AND "分区"='1区') THEN
        RAISE EXCEPTION 'Batch overwrote a reviewed recommendation';
    END IF;
    SELECT * INTO r FROM pg_temp.run_journal_batch(2024, 0, NULL);
    IF r."处理论文数" <> 0 OR r."产生匹配数" <> 0 OR r."是否还有论文" THEN
        RAISE EXCEPTION 'Empty year did not return an empty result';
    END IF;
    SELECT * INTO r FROM pg_temp.run_journal_batch(NULL, 107, NULL);
    IF r."处理论文数" <> 1 OR r."产生匹配数" <> 2 OR r."截止论文编号" <> 108 OR r."是否还有论文" THEN
        RAISE EXCEPTION 'All-years matching lost a paper with unknown publication year';
    END IF;
    RAISE NOTICE 'PASS: migrated schema, ISSN/name matching, year/date fallback, pagination, idempotence and reviewed-state preservation';
END;
$test$;

-- Make one source fail and assert that the whole batch rolls back, not just
-- the failed row. This tests the same INSERT statement used by the endpoint.
CREATE FUNCTION pg_temp.reject_regression_match() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NEW."论文编号"=107 AND NEW."来源"='中科院分区' THEN
        RAISE EXCEPTION 'Fixture rejection' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END;
$$;
CREATE TRIGGER journal_regression_rejection BEFORE INSERT ON "论文期刊分区匹配"
FOR EACH ROW EXECUTE FUNCTION pg_temp.reject_regression_match();
DO $test$
BEGIN
    BEGIN
        PERFORM pg_temp.run_journal_batch(2025, 0, NULL);
        RAISE EXCEPTION 'Expected an insert failure';
    EXCEPTION WHEN check_violation THEN
        NULL;
    END;
    IF EXISTS (SELECT FROM "论文期刊分区匹配" WHERE "论文编号"=107) THEN
        RAISE EXCEPTION 'Batch left partial recommendations after failure';
    END IF;
    RAISE NOTICE 'PASS: failed batch leaves no partial recommendations';
END;
$test$;
DROP TRIGGER journal_regression_rejection ON "论文期刊分区匹配";
DO $test$
DECLARE r record;
BEGIN
    SELECT * INTO r FROM pg_temp.run_journal_batch(2025, 0, NULL);
    IF r."处理论文数" <> 1 OR r."产生匹配数" <> 2 OR r."是否还有论文" THEN
        RAISE EXCEPTION 'Year-specific replay failed: %', row_to_json(r);
    END IF;
    IF NOT EXISTS (SELECT FROM "论文期刊分区匹配" WHERE "论文编号"=107 AND "来源"='JCR' AND "年度"=2025) THEN
        RAISE EXCEPTION 'Explicit paper year did not select matching journal year';
    END IF;
    RAISE NOTICE 'PASS: explicit paper year takes precedence and failed batch can be retried';
END;
$test$;
ROLLBACK;
