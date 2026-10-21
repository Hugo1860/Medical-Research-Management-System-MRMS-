-- Paper import routines. Journal and first-author names are resolved against
-- PostgreSQL master data while keeping the import atomic.
BEGIN;

CREATE OR REPLACE FUNCTION "sp_导入论文"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    CREATE TEMP TABLE _imported_papers ("编号" integer, "论文标题" varchar(200)) ON COMMIT DROP;
    WITH inserted AS (
        INSERT INTO "成果论文" ("论文标题", "年度", "作者人数", "关键字", "论文摘要", "刊物编号", "卷号", "期号", "页码范围起", "页码范围止")
        SELECT r."论文标题", r."年度", r."作者人数", r."关键字", r."论文摘要", journal."编号", r."卷号", r."期号", r."页码范围起", r."页码范围止"
        FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r(
            "论文标题" varchar(200), "作者" varchar(50), "作者人数" integer, "年度" integer,
            "关键字" varchar(500), "论文摘要" varchar(1000), "卷号" integer, "期号" integer,
            "页码范围起" integer, "页码范围止" integer, "刊物名称" varchar(500))
        LEFT JOIN LATERAL (
            SELECT j."编号" FROM "期刊" j
            WHERE j."名称" = r."刊物名称" OR j."英文刊名" = r."刊物名称"
            ORDER BY j."编号" LIMIT 1
        ) journal ON TRUE
        RETURNING "编号", "论文标题"
    )
    INSERT INTO _imported_papers SELECT * FROM inserted;

    INSERT INTO "成果作者" ("成果类型", "成果编号", "作者级别", "作者编号")
    SELECT 1, imported."编号", '第一作者', person."编号"
    FROM _imported_papers imported
    JOIN jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("论文标题" varchar(200), "作者" varchar(50))
      ON r."论文标题" = imported."论文标题"
    JOIN "人员" person ON person."姓名" = r."作者";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN (SELECT count(*) FROM _imported_papers);
END;
$$;

CREATE OR REPLACE FUNCTION "sp_导入自定义论文"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    CREATE TEMP TABLE _imported_custom_papers ("编号" integer, "论文标题" varchar(200)) ON COMMIT DROP;
    WITH inserted AS (
        INSERT INTO "成果论文" (
            "论文标题", "论文标题类型", "论文Online日期", "论文正式出版日期", "他引次数", "年度", "收稿日期",
            "论文所属栏目", "论文字数", "计划来源", "论文版面", "学科门类", "一级学科", "二级学科", "三级学科",
            "作者人数", "关键字", "论文摘要", "刊物编号", "卷号", "期号", "页码范围起", "页码范围止", "论文类型")
        SELECT r."论文标题", r."论文标题类型", r."论文Online日期", r."论文正式出版日期", r."他引次数", r."年度", r."收稿日期",
               r."论文所属栏目", r."论文字数", r."计划来源", r."论文版面", r."学科门类", r."一级学科", r."二级学科", r."三级学科",
               r."作者人数", r."关键字", r."论文摘要", journal."编号", r."卷号", r."期号", r."页码范围起", r."页码范围止", r."论文类型"
        FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r(
            "论文标题" varchar(200), "论文标题类型" varchar(50), "论文Online日期" date, "论文正式出版日期" date,
            "作者" varchar(50), "作者人数" integer, "他引次数" integer, "年度" integer, "收稿日期" date,
            "论文所属栏目" varchar(50), "论文字数" numeric(18,2), "计划来源" varchar(50), "论文版面" varchar(50),
            "学科门类" varchar(50), "一级学科" varchar(50), "二级学科" varchar(50), "三级学科" varchar(50),
            "关键字" varchar(500), "论文摘要" varchar(1000), "论文类型" varchar(50), "卷号" integer, "期号" integer,
            "页码范围起" integer, "页码范围止" integer, "刊物名称" varchar(500))
        LEFT JOIN LATERAL (
            SELECT j."编号" FROM "期刊" j
            WHERE j."名称" = r."刊物名称" OR j."英文刊名" = r."刊物名称"
            ORDER BY j."编号" LIMIT 1
        ) journal ON TRUE
        RETURNING "编号", "论文标题"
    )
    INSERT INTO _imported_custom_papers SELECT * FROM inserted;

    INSERT INTO "成果作者" ("成果类型", "成果编号", "作者级别", "作者编号")
    SELECT 1, imported."编号", '第一作者', person."编号"
    FROM _imported_custom_papers imported
    JOIN jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("论文标题" varchar(200), "作者" varchar(50))
      ON r."论文标题" = imported."论文标题"
    JOIN "人员" person ON person."姓名" = r."作者";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN (SELECT count(*) FROM _imported_custom_papers);
END;
$$;

COMMIT;
