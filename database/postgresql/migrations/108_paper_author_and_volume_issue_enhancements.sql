-- Migration 108: Paper author metadata and volume/issue/page enhancements
BEGIN;

-- 1. 为成果论文表增加 第一作者、所有作者、通讯作者、页码范围、DOI、PMID 字段
ALTER TABLE "成果论文" ADD COLUMN IF NOT EXISTS "第一作者" character varying(200);
ALTER TABLE "成果论文" ADD COLUMN IF NOT EXISTS "所有作者" character varying(2000);
ALTER TABLE "成果论文" ADD COLUMN IF NOT EXISTS "通讯作者" character varying(200);
ALTER TABLE "成果论文" ADD COLUMN IF NOT EXISTS "页码范围" character varying(100);
ALTER TABLE "成果论文" ADD COLUMN IF NOT EXISTS "DOI" character varying(200);
ALTER TABLE "成果论文" ADD COLUMN IF NOT EXISTS "PMID" character varying(100);

-- 2. 重建 v1_成果论文_列表 视图
DROP VIEW IF EXISTS "v1_成果论文_列表" CASCADE;
CREATE OR REPLACE VIEW "v1_成果论文_列表" AS 
SELECT 
    a."编号",
    a."论文标题",
    a."论文Online日期",
    a."论文正式出版日期",
    COALESCE(b."名称", b."英文刊名") AS "刊物名称",
    a."认领人编号",
    COALESCE("fn_成果作者"(1, a."编号", '第一作者'::character varying), a."第一作者") AS "第一作者",
    a."通讯作者",
    a."所有作者",
    a."年度",
    a."卷号",
    a."期号",
    COALESCE(a."页码范围", CASE WHEN a."页码范围起" IS NOT NULL AND a."页码范围止" IS NOT NULL THEN a."页码范围起"::text || '-' || a."页码范围止"::text WHEN a."页码范围起" IS NOT NULL THEN a."页码范围起"::text ELSE NULL END) AS "页码范围",
    a."作者人数",
    a."DOI",
    a."PMID"
FROM "成果论文" a
LEFT JOIN "期刊" b ON a."刊物编号" = b."编号";

-- 3. 重建 v1_成果论文_详情 视图
DROP VIEW IF EXISTS "v1_成果论文_详情" CASCADE;
CREATE OR REPLACE VIEW "v1_成果论文_详情" AS 
SELECT 
    a."编号",
    a."认领人编号",
    a."论文标题",
    a."论文标题类型",
    a."论文Online日期",
    a."论文正式出版日期",
    a."他引次数",
    a."年度",
    a."收稿日期",
    a."论文所属栏目",
    a."版面费",
    a."超额版面费",
    a."超额费用来源",
    a."奖励情况",
    a."论文字数",
    a."计划来源",
    a."论文版面",
    a."学科门类",
    a."一级学科",
    a."二级学科",
    a."三级学科",
    a."作者人数",
    a."关键字",
    a."论文摘要",
    a."备注",
    a."刊物编号",
    a."卷号",
    a."期号",
    a."页码范围起",
    a."页码范围止",
    COALESCE(a."页码范围", CASE WHEN a."页码范围起" IS NOT NULL AND a."页码范围止" IS NOT NULL THEN a."页码范围起"::text || '-' || a."页码范围止"::text WHEN a."页码范围起" IS NOT NULL THEN a."页码范围起"::text ELSE NULL END) AS "页码范围",
    COALESCE("fn_成果作者"(1, a."编号", '第一作者'::character varying), a."第一作者") AS "第一作者",
    a."通讯作者",
    a."所有作者",
    a."DOI",
    a."PMID",
    a."论文类型",
    a."是否与行业联合发表",
    a."是否与地方联合发表",
    a."是否与国际联合发表",
    a."是否跨学科",
    a."是否与本专业相关",
    a."检索信息页相关文件路径",
    a."论文正文路径",
    a."其他相关文件路径",
    b."代码",
    COALESCE(b."名称", b."英文刊名") AS "刊物名称",
    b."主办单位",
    b."期刊系列",
    b."JCR分区",
    b."编目名称",
    b."编目类别",
    b."创刊日期",
    b."被引用次数",
    b."影响因子",
    b."特征因子分值",
    b."ISSN号",
    b."CN号"
FROM "成果论文" a
LEFT JOIN "期刊" b ON a."刊物编号" = b."编号";

-- 4. 重建 tfn_成果论文 函数
DROP FUNCTION IF EXISTS "tfn_成果论文"(integer, integer);
CREATE OR REPLACE FUNCTION "tfn_成果论文"(p_employee_id integer, p_template_id integer)
RETURNS TABLE (
    "编号" integer, "论文标题" varchar, "论文Online日期" date, "论文正式出版日期" date,
    "刊物名称" varchar, "认领人编号" integer, "第一作者" varchar, "通讯作者" varchar,
    "所有作者" varchar, "年度" integer, "卷号" integer, "期号" integer, "页码范围" varchar,
    "作者人数" integer, "DOI" varchar, "PMID" varchar, "审核进度" text,
    "流程编号" integer, "流程模板编号" integer, "流程名称" varchar, "流程相关项目路径" varchar,
    "项目编号" integer, "发起人编号" integer, "发起人工号" varchar, "发起人姓名" varchar,
    "发起人部门名称" varchar, "状态" integer, "状态名称" text, "流程创建时间" timestamp,
    "步骤编号" integer, "步骤名称" varchar, "步骤状态" integer, "步骤链接名称" varchar,
    "步骤链接路径" varchar, "步骤链接样式" varchar, "步骤操作人编号" integer,
    "步骤操作人工号" varchar, "步骤操作人姓名" varchar, "步骤操作人部门名称" varchar,
    "步骤执行时间" timestamp, "步骤状态说明" varchar, "是否可处理" boolean
)
LANGUAGE sql STABLE AS $$
    SELECT a."编号", a."论文标题"::varchar, a."论文Online日期", a."论文正式出版日期", a."刊物名称"::varchar,
           a."认领人编号", a."第一作者"::varchar, a."通讯作者"::varchar, a."所有作者"::varchar, a."年度", a."卷号", a."期号",
           a."页码范围"::varchar, a."作者人数", a."DOI"::varchar, a."PMID"::varchar,
           concat(c."状态名称", '-', c."步骤状态说明"),
           c."流程编号", c."流程模板编号", c."流程名称", c."流程相关项目路径", c."项目编号",
           c."发起人编号", c."发起人工号", c."发起人姓名", c."发起人部门名称", c."状态",
           c."状态名称", c."流程创建时间", c."步骤编号", c."步骤名称", c."步骤状态",
           c."步骤链接名称", c."步骤链接路径", c."步骤链接样式", c."步骤操作人编号",
           c."步骤操作人工号", c."步骤操作人姓名", c."步骤操作人部门名称", c."步骤执行时间",
           c."步骤状态说明", c."是否可处理"
    FROM "v1_成果论文_列表" AS a
    LEFT JOIN LATERAL "tfn_流程"(p_template_id, a."编号", p_employee_id) AS c ON TRUE;
$$;

-- 5. 升级 sp_导入论文 存储过程：提取并保存第一作者、所有作者、通讯作者、年卷期页码，并对已存在的论文执行信息回填补全
CREATE OR REPLACE FUNCTION "sp_导入论文"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE 
    v_inserted_count integer := 0;
    v_updated_count integer := 0;
BEGIN
    CREATE TEMP TABLE _imported_papers ("编号" integer, "论文标题" varchar(200)) ON COMMIT DROP;

    -- 1) 回填更新库中已存在的论文（补齐第一作者、所有作者、通讯作者、年度、卷号、期号、页码范围、刊物）
    WITH updated AS (
        UPDATE "成果论文" ep
        SET "第一作者" = COALESCE(NULLIF(r."第一作者", ''), NULLIF(r."作者", ''), ep."第一作者"),
            "所有作者" = COALESCE(NULLIF(r."所有作者", ''), NULLIF(r."作者", ''), ep."所有作者"),
            "通讯作者" = COALESCE(NULLIF(r."通讯作者", ''), ep."通讯作者"),
            "年度" = COALESCE(r."年度", ep."年度"),
            "卷号" = COALESCE(r."卷号", ep."卷号"),
            "期号" = COALESCE(r."期号", ep."期号"),
            "页码范围起" = COALESCE(r."页码范围起", ep."页码范围起"),
            "页码范围止" = COALESCE(r."页码范围止", ep."页码范围止"),
            "页码范围" = COALESCE(NULLIF(r."页码范围", ''), ep."页码范围"),
            "作者人数" = COALESCE(r."作者人数", ep."作者人数"),
            "关键字" = COALESCE(ep."关键字", SUBSTRING(r."关键字", 1, 500)),
            "论文摘要" = COALESCE(ep."论文摘要", SUBSTRING(r."论文摘要", 1, 1000)),
            "刊物编号" = COALESCE(ep."刊物编号", journal."编号")
        FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r(
            "论文标题" text, "作者" text, "第一作者" text, "所有作者" text, "通讯作者" text,
            "作者人数" integer, "年度" integer, "关键字" text, "论文摘要" text, "卷号" integer, "期号" integer,
            "页码范围" text, "页码范围起" integer, "页码范围止" integer, "刊物名称" text)
        LEFT JOIN LATERAL (
            SELECT j."编号" FROM "期刊" j
            WHERE j."名称" = r."刊物名称" OR j."英文刊名" = r."刊物名称"
            ORDER BY j."编号" LIMIT 1
        ) journal ON TRUE
        WHERE ep."论文标题" = r."论文标题"
        RETURNING ep."编号"
    )
    SELECT count(*) INTO v_updated_count FROM updated;

    -- 2) 插入全新论文
    WITH inserted AS (
        INSERT INTO "成果论文" (
            "论文标题", "年度", "作者人数", "关键字", "论文摘要", "刊物编号", 
            "卷号", "期号", "页码范围起", "页码范围止", "页码范围", 
            "第一作者", "所有作者", "通讯作者"
        )
        SELECT 
            SUBSTRING(r."论文标题", 1, 200),
            r."年度",
            COALESCE(r."作者人数", 1),
            SUBSTRING(r."关键字", 1, 500),
            SUBSTRING(r."论文摘要", 1, 1000),
            journal."编号",
            r."卷号",
            r."期号",
            r."页码范围起",
            r."页码范围止",
            SUBSTRING(COALESCE(r."页码范围", CASE WHEN r."页码范围起" IS NOT NULL AND r."页码范围止" IS NOT NULL THEN r."页码范围起"::text || '-' || r."页码范围止"::text WHEN r."页码范围起" IS NOT NULL THEN r."页码范围起"::text ELSE NULL END), 1, 100),
            SUBSTRING(COALESCE(NULLIF(r."第一作者", ''), r."作者"), 1, 200),
            SUBSTRING(COALESCE(NULLIF(r."所有作者", ''), r."作者"), 1, 2000),
            SUBSTRING(r."通讯作者", 1, 200)
        FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r(
            "论文标题" text, "作者" text, "第一作者" text, "所有作者" text, "通讯作者" text,
            "作者人数" integer, "年度" integer, "关键字" text, "论文摘要" text, "卷号" integer, "期号" integer,
            "页码范围" text, "页码范围起" integer, "页码范围止" integer, "刊物名称" text)
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

    SELECT count(*) INTO v_inserted_count FROM _imported_papers;

    -- 3) 若作者在人员表中存在，补充关联成果作者
    INSERT INTO "成果作者" ("成果类型", "成果编号", "作者级别", "作者编号")
    SELECT 1, imported."编号", '第一作者', author_person."编号"
    FROM _imported_papers imported
    JOIN jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("论文标题" text, "作者" text, "第一作者" text)
      ON r."论文标题" = imported."论文标题"
    JOIN "人员" author_person
      ON author_person."姓名" = COALESCE(NULLIF(r."第一作者", ''), r."作者")
    ON CONFLICT DO NOTHING;

    RETURN v_inserted_count + v_updated_count;
END;
$$;

-- 6. 升级 sp_导入自定义论文 存储过程
CREATE OR REPLACE FUNCTION "sp_导入自定义论文"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE 
    v_inserted_count integer := 0;
    v_updated_count integer := 0;
BEGIN
    CREATE TEMP TABLE _imported_custom_papers ("编号" integer, "论文标题" varchar(200)) ON COMMIT DROP;

    WITH updated AS (
        UPDATE "成果论文" ep
        SET "第一作者" = COALESCE(NULLIF(r."第一作者", ''), NULLIF(r."作者", ''), ep."第一作者"),
            "所有作者" = COALESCE(NULLIF(r."所有作者", ''), NULLIF(r."作者", ''), ep."所有作者"),
            "通讯作者" = COALESCE(NULLIF(r."通讯作者", ''), ep."通讯作者"),
            "年度" = COALESCE(r."年度", ep."年度"),
            "卷号" = COALESCE(r."卷号", ep."卷号"),
            "期号" = COALESCE(r."期号", ep."期号"),
            "页码范围起" = COALESCE(r."页码范围起", ep."页码范围起"),
            "页码范围止" = COALESCE(r."页码范围止", ep."页码范围止"),
            "页码范围" = COALESCE(NULLIF(r."页码范围", ''), ep."页码范围"),
            "作者人数" = COALESCE(r."作者人数", ep."作者人数")
        FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r(
            "论文标题" text, "作者" text, "第一作者" text, "所有作者" text, "通讯作者" text,
            "作者人数" integer, "年度" integer, "卷号" integer, "期号" integer,
            "页码范围" text, "页码范围起" integer, "页码范围止" integer)
        WHERE ep."论文标题" = r."论文标题"
        RETURNING ep."编号"
    )
    SELECT count(*) INTO v_updated_count FROM updated;

    WITH inserted AS (
        INSERT INTO "成果论文" (
            "论文标题", "论文标题类型", "论文Online日期", "论文正式出版日期", "他引次数", "年度",
            "收稿日期", "论文所属栏目", "论文字数", "计划来源", "论文版面", "学科门类", "一级学科",
            "二级学科", "三级学科", "作者人数", "关键字", "论文摘要", "备注", "刊物编号", "卷号", "期号",
            "页码范围起", "页码范围止", "页码范围", "第一作者", "所有作者", "通讯作者"
        )
        SELECT 
            SUBSTRING(r."论文标题", 1, 200),
            SUBSTRING(r."论文标题类型", 1, 50),
            r."论文Online日期",
            r."论文正式出版日期",
            r."他引次数",
            r."年度",
            r."收稿日期",
            SUBSTRING(r."论文所属栏目", 1, 100),
            r."论文字数",
            SUBSTRING(r."计划来源", 1, 100),
            SUBSTRING(r."论文版面", 1, 100),
            SUBSTRING(r."学科门类", 1, 100),
            SUBSTRING(r."一级学科", 1, 100),
            SUBSTRING(r."二级学科", 1, 100),
            SUBSTRING(r."三级学科", 1, 100),
            COALESCE(r."作者人数", 1),
            SUBSTRING(r."关键字", 1, 500),
            SUBSTRING(r."论文摘要", 1, 1000),
            SUBSTRING(r."备注", 1, 500),
            journal."编号",
            r."卷号",
            r."期号",
            r."页码范围起",
            r."页码范围止",
            SUBSTRING(COALESCE(r."页码范围", CASE WHEN r."页码范围起" IS NOT NULL AND r."页码范围止" IS NOT NULL THEN r."页码范围起"::text || '-' || r."页码范围止"::text WHEN r."页码范围起" IS NOT NULL THEN r."页码范围起"::text ELSE NULL END), 1, 100),
            SUBSTRING(COALESCE(NULLIF(r."第一作者", ''), r."作者"), 1, 200),
            SUBSTRING(COALESCE(NULLIF(r."所有作者", ''), r."作者"), 1, 2000),
            SUBSTRING(r."通讯作者", 1, 200)
        FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r(
            "论文标题" text, "论文标题类型" text, "论文Online日期" date, "论文正式出版日期" date,
            "他引次数" integer, "年度" integer, "收稿日期" date, "论文所属栏目" text,
            "论文字数" numeric, "计划来源" text, "论文版面" text, "学科门类" text,
            "一级学科" text, "二级学科" text, "三级学科" text, "作者人数" integer,
            "关键字" text, "论文摘要" text, "备注" text, "刊物名称" text,
            "卷号" integer, "期号" integer, "页码范围起" integer, "页码范围止" integer,
            "页码范围" text, "作者" text, "第一作者" text, "所有作者" text, "通讯作者" text)
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

    SELECT count(*) INTO v_inserted_count FROM _imported_custom_papers;

    INSERT INTO "成果作者" ("成果类型", "成果编号", "作者级别", "作者编号")
    SELECT 1, imported."编号", '第一作者', author_person."编号"
    FROM _imported_custom_papers imported
    JOIN jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("论文标题" text, "作者" text, "第一作者" text)
      ON r."论文标题" = imported."论文标题"
    JOIN "人员" author_person
      ON author_person."姓名" = COALESCE(NULLIF(r."第一作者", ''), r."作者")
    ON CONFLICT DO NOTHING;

    RETURN v_inserted_count + v_updated_count;
END;
$$;

-- 7. 登记版本为 108
UPDATE "系统迁移版本"
   SET "版本" = 108,
       "脚本" = '108_paper_author_and_volume_issue_enhancements.sql',
       "检查摘要" = '增加论文第一作者/所有作者/通讯作者/年卷期页码结构化支持与列表视图投影',
       "应用时间" = CURRENT_TIMESTAMP
 WHERE "编号" = 1;

COMMIT;
