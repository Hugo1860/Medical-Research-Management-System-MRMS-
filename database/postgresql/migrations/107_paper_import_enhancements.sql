-- Migration 107: Paper import enhancements (protect against long text truncation and skip duplicate titles)
BEGIN;

CREATE OR REPLACE FUNCTION "sp_导入论文"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    CREATE TEMP TABLE _imported_papers ("编号" integer, "论文标题" varchar(200)) ON COMMIT DROP;
    WITH inserted AS (
        INSERT INTO "成果论文" ("论文标题", "年度", "作者人数", "关键字", "论文摘要", "刊物编号", "卷号", "期号", "页码范围起", "页码范围止")
        SELECT 
            SUBSTRING(r."论文标题", 1, 200),
            r."年度",
            r."作者人数",
            SUBSTRING(r."关键字", 1, 500),
            SUBSTRING(r."论文摘要", 1, 1000),
            journal."编号",
            r."卷号",
            r."期号",
            r."页码范围起",
            r."页码范围止"
        FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r(
            "论文标题" text, "作者" text, "作者人数" integer, "年度" integer,
            "关键字" text, "论文摘要" text, "卷号" integer, "期号" integer,
            "页码范围起" integer, "页码范围止" integer, "刊物名称" text)
        LEFT JOIN LATERAL (
            SELECT j."编号" FROM "期刊" j
            WHERE j."名称" = r."刊物名称" OR j."英文刊名" = r."刊物名称"
            ORDER BY j."编号" LIMIT 1
        ) journal ON TRUE
        WHERE NOT EXISTS (
            SELECT 1 FROM "成果论文" ep WHERE ep."论文标题" = r."论文标题"
        )
        RETURNING "编号", "论文标题"
    )
    INSERT INTO _imported_papers SELECT * FROM inserted;

    INSERT INTO "成果作者" ("成果类型", "成果编号", "作者级别", "作者编号")
    SELECT 1, imported."编号", '第一作者', author_person."编号"
    FROM _imported_papers imported
    JOIN jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("论文标题" text, "作者" text)
      ON r."论文标题" = imported."论文标题"
    CROSS JOIN LATERAL (
        SELECT person."编号" FROM "人员" person
        WHERE person."姓名" = r."作者"
        ORDER BY person."编号" LIMIT 1
    ) author_person;

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
        SELECT 
            SUBSTRING(r."论文标题", 1, 200),
            SUBSTRING(r."论文标题类型", 1, 50),
            r."论文Online日期",
            r."论文正式出版日期",
            r."他引次数",
            r."年度",
            r."收稿日期",
            SUBSTRING(r."论文所属栏目", 1, 50),
            r."论文字数",
            SUBSTRING(r."计划来源", 1, 50),
            SUBSTRING(r."论文版面", 1, 50),
            SUBSTRING(r."学科门类", 1, 50),
            SUBSTRING(r."一级学科", 1, 50),
            SUBSTRING(r."二级学科", 1, 50),
            SUBSTRING(r."三级学科", 1, 50),
            r."作者人数",
            SUBSTRING(r."关键字", 1, 500),
            SUBSTRING(r."论文摘要", 1, 1000),
            journal."编号",
            r."卷号",
            r."期号",
            r."页码范围起",
            r."页码范围止",
            SUBSTRING(r."论文类型", 1, 50)
        FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r(
            "论文标题" text, "论文标题类型" text, "论文Online日期" date, "论文正式出版日期" date,
            "作者" text, "作者人数" integer, "他引次数" integer, "年度" integer, "收稿日期" date,
            "论文所属栏目" text, "论文字数" numeric(18,2), "计划来源" text, "论文版面" text,
            "学科门类" text, "一级学科" text, "二级学科" text, "三级学科" text,
            "关键字" text, "论文摘要" text, "论文类型" text, "卷号" integer, "期号" integer,
            "页码范围起" integer, "页码范围止" integer, "刊物名称" text)
        LEFT JOIN LATERAL (
            SELECT j."编号" FROM "期刊" j
            WHERE j."名称" = r."刊物名称" OR j."英文刊名" = r."刊物名称"
            ORDER BY j."编号" LIMIT 1
        ) journal ON TRUE
        WHERE NOT EXISTS (
            SELECT 1 FROM "成果论文" ep WHERE ep."论文标题" = r."论文标题"
        )
        RETURNING "编号", "论文标题"
    )
    INSERT INTO _imported_custom_papers SELECT * FROM inserted;

    INSERT INTO "成果作者" ("成果类型", "成果编号", "作者级别", "作者编号")
    SELECT 1, imported."编号", '第一作者', author_person."编号"
    FROM _imported_custom_papers imported
    JOIN jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("论文标题" text, "作者" text)
      ON r."论文标题" = imported."论文标题"
    CROSS JOIN LATERAL (
        SELECT person."编号" FROM "人员" person
        WHERE person."姓名" = r."作者"
        ORDER BY person."编号" LIMIT 1
    ) author_person;

    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN (SELECT count(*) FROM _imported_custom_papers);
END;
$$;

UPDATE "系统迁移版本"
SET "版本"=107,
    "脚本"='107_paper_import_enhancements.sql',
    "检查摘要"='优化论文导入例程支持长字段截断保护并跳过重复标题',
    "应用时间"=CURRENT_TIMESTAMP
WHERE "编号"=1;

COMMIT;
