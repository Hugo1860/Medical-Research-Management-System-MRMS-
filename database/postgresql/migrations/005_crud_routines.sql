-- JSONB compatibility wrappers for simple CRUD procedures.
-- Complex workflow and multi-table procedures remain in ROUTINE_MIGRATION_REPORT.md.
BEGIN;

CREATE OR REPLACE FUNCTION "pg_first_json_array"(p_payload jsonb)
RETURNS jsonb LANGUAGE sql IMMUTABLE AS $$
    SELECT COALESCE((SELECT value FROM jsonb_each(p_payload) WHERE jsonb_typeof(value) = 'array' LIMIT 1), '[]'::jsonb);
$$;

CREATE OR REPLACE FUNCTION "sp_经费模版项目支出类型_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "经费模版项目支出类型" ("编号", "所属经费模版编号", "项目支出类型", "经费占比", "备注")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"经费模版项目支出类型"', '编号')) ELSE r."编号" END, r."所属经费模版编号", r."项目支出类型", r."经费占比", r."备注"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "所属经费模版编号" integer, "项目支出类型" varchar(100), "经费占比" integer, "备注" varchar(500))
    ON CONFLICT ("编号") DO UPDATE SET "所属经费模版编号" = EXCLUDED."所属经费模版编号", "项目支出类型" = EXCLUDED."项目支出类型", "经费占比" = EXCLUDED."经费占比", "备注" = EXCLUDED."备注";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_登录日志_增"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "登录日志" ("编号", "工号", "姓名", "登录时间", "IP")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"登录日志"', '编号')) ELSE r."编号" END, r."工号", r."姓名", r."登录时间", r."IP"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "工号" varchar(50), "姓名" varchar(50), "登录时间" timestamp without time zone, "IP" varchar(50));
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_步骤指定人_增"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "步骤指定人" ("编号", "步骤编号", "指定人编号")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"步骤指定人"', '编号')) ELSE r."编号" END, r."步骤编号", r."指定人编号"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "步骤编号" integer, "指定人编号" integer);
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_角色_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "角色" ("编号", "名称", "备注")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"角色"', '编号')) ELSE r."编号" END, r."名称", r."备注"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "名称" varchar(50), "备注" varchar(500))
    ON CONFLICT ("编号") DO UPDATE SET "名称" = EXCLUDED."名称", "备注" = EXCLUDED."备注";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_通知公告_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "通知公告" ("编号", "通知类型", "通知标识", "发送人编号", "通知名称", "关闭时间", "相关文件路径", "通知内容", "接收人数", "已接收人数", "是否启用", "建立时间", "备注")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"通知公告"', '编号')) ELSE r."编号" END, r."通知类型", r."通知标识", r."发送人编号", r."通知名称", r."关闭时间", r."相关文件路径", r."通知内容", r."接收人数", r."已接收人数", r."是否启用", r."建立时间", r."备注"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "通知类型" varchar(50), "通知标识" varchar(50), "发送人编号" integer, "通知名称" varchar(100), "关闭时间" timestamp without time zone, "相关文件路径" varchar(500), "通知内容" text, "接收人数" integer, "已接收人数" integer, "是否启用" boolean, "建立时间" timestamp without time zone, "备注" varchar(1000))
    ON CONFLICT ("编号") DO UPDATE SET "通知类型" = EXCLUDED."通知类型", "通知标识" = EXCLUDED."通知标识", "发送人编号" = EXCLUDED."发送人编号", "通知名称" = EXCLUDED."通知名称", "关闭时间" = EXCLUDED."关闭时间", "相关文件路径" = EXCLUDED."相关文件路径", "通知内容" = EXCLUDED."通知内容", "接收人数" = EXCLUDED."接收人数", "已接收人数" = EXCLUDED."已接收人数", "是否启用" = EXCLUDED."是否启用", "建立时间" = EXCLUDED."建立时间", "备注" = EXCLUDED."备注";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_主办讲座_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "主办讲座" ("编号", "活动名称", "所属部门编号", "活动经费", "会议类型", "活动对象", "开始时间", "结束时间", "主办人编号", "主讲人姓名", "主讲人国别", "主讲人单位", "主讲人学位", "主讲人职称", "主讲人简历路径", "讲座地点", "内容概述")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"主办讲座"', '编号')) ELSE r."编号" END, r."活动名称", r."所属部门编号", r."活动经费", r."会议类型", r."活动对象", r."开始时间", r."结束时间", r."主办人编号", r."主讲人姓名", r."主讲人国别", r."主讲人单位", r."主讲人学位", r."主讲人职称", r."主讲人简历路径", r."讲座地点", r."内容概述"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "活动名称" varchar(500), "所属部门编号" integer, "活动经费" numeric(18,2), "会议类型" varchar(50), "活动对象" varchar(500), "开始时间" timestamp without time zone, "结束时间" timestamp without time zone, "主办人编号" integer, "主讲人姓名" varchar(50), "主讲人国别" varchar(50), "主讲人单位" varchar(50), "主讲人学位" varchar(50), "主讲人职称" varchar(50), "主讲人简历路径" text, "讲座地点" varchar(500), "内容概述" varchar(500))
    ON CONFLICT ("编号") DO UPDATE SET "活动名称" = EXCLUDED."活动名称", "所属部门编号" = EXCLUDED."所属部门编号", "活动经费" = EXCLUDED."活动经费", "会议类型" = EXCLUDED."会议类型", "活动对象" = EXCLUDED."活动对象", "开始时间" = EXCLUDED."开始时间", "结束时间" = EXCLUDED."结束时间", "主办人编号" = EXCLUDED."主办人编号", "主讲人姓名" = EXCLUDED."主讲人姓名", "主讲人国别" = EXCLUDED."主讲人国别", "主讲人单位" = EXCLUDED."主讲人单位", "主讲人学位" = EXCLUDED."主讲人学位", "主讲人职称" = EXCLUDED."主讲人职称", "主讲人简历路径" = EXCLUDED."主讲人简历路径", "讲座地点" = EXCLUDED."讲座地点", "内容概述" = EXCLUDED."内容概述";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_期刊_删"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    DELETE FROM "期刊" AS t
    WHERE t."编号" IN (SELECT (r."编号") FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "代码" varchar(50), "名称" varchar(500), "英文刊名" varchar(500), "主办单位" varchar(500), "期刊系列" varchar(500), "收录情况" varchar(1000), "语种" varchar(50), "JCR分区" varchar(50), "编目名称" varchar(100), "编目类别" varchar(100), "年度" integer, "创刊日期" date, "被引用次数" integer, "影响因子" numeric(18,3), "特征因子分值" numeric(18,3), "ISSN号" varchar(50), "CN号" varchar(50), "出版周期" varchar(50), "出版地" varchar(50), "开本" varchar(50), "备注" varchar(500)));
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_经费模版_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "经费模版" ("编号", "名称", "备注")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"经费模版"', '编号')) ELSE r."编号" END, r."名称", r."备注"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "名称" varchar(50), "备注" varchar(500))
    ON CONFLICT ("编号") DO UPDATE SET "名称" = EXCLUDED."名称", "备注" = EXCLUDED."备注";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_下载中心_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "下载中心" ("编号", "文件名称", "文件类型", "文件路径", "是否启用", "上传人编号", "建立时间", "备注")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"下载中心"', '编号')) ELSE r."编号" END, r."文件名称", r."文件类型", r."文件路径", r."是否启用", r."上传人编号", r."建立时间", r."备注"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "文件名称" varchar(200), "文件类型" varchar(50), "文件路径" varchar(500), "是否启用" boolean, "上传人编号" integer, "建立时间" timestamp without time zone, "备注" varchar(500))
    ON CONFLICT ("编号") DO UPDATE SET "文件名称" = EXCLUDED."文件名称", "文件类型" = EXCLUDED."文件类型", "文件路径" = EXCLUDED."文件路径", "是否启用" = EXCLUDED."是否启用", "上传人编号" = EXCLUDED."上传人编号", "建立时间" = EXCLUDED."建立时间", "备注" = EXCLUDED."备注";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_经费模版_删"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    DELETE FROM "经费模版" AS t
    WHERE t."编号" IN (SELECT (r."编号") FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "名称" varchar(50), "备注" varchar(500)));
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_下载中心_启禁"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    UPDATE "下载中心" SET "是否启用" = COALESCE((p_payload->>'是否启用')::boolean, FALSE)
    WHERE "编号" = (p_payload->>'编号')::integer;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_纵向项目申报_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE
    r record;
    v_new_id integer;
    v_template_id integer := COALESCE((p_payload->>'流程模板编号')::integer, 400);
    v_tt jsonb;
BEGIN
    IF p_payload ? 'tt' AND jsonb_typeof(p_payload->'tt') = 'array' THEN
        v_tt := p_payload->'tt';
    ELSE
        v_tt := "pg_first_json_array"(p_payload);
    END IF;

    FOR r IN
        SELECT * FROM jsonb_to_recordset(v_tt) AS x(
            "编号" integer, "分类" integer, "项目名称" varchar(100), "申报人编号" integer,
            "学科门类" varchar(50), "学科类型" varchar(50), "申报年度" integer, "项目类型" varchar(50),
            "项目级别" varchar(50), "项目来源单位" varchar(50), "项目类别" varchar(50),
            "申报时间" timestamp without time zone, "申请经费" numeric(18,2), "计划开始日期" date,
            "计划完成日期" date, "项目等级" varchar(50), "是否委托外单位进行实验或实验分析" boolean,
            "实验委托检测内容" varchar(1000), "项目摘要" varchar(1000), "是否需要伦理评审" boolean,
            "是否需要伦理批文" boolean, "伦理批文编号" varchar(50), "伦理批文路径" text,
            "项目申报书路径" text, "备注" varchar(500), "建立时间" timestamp without time zone,
            "是否立项" boolean
        )
    LOOP
        IF COALESCE(r."编号", 0) = 0 THEN
            INSERT INTO "纵向项目申报" (
                "编号", "分类", "项目名称", "申报人编号", "学科门类", "学科类型", "申报年度",
                "项目类型", "项目级别", "项目来源单位", "项目类别", "申报时间", "申请经费",
                "计划开始日期", "计划完成日期", "项目等级", "是否委托外单位进行实验或实验分析",
                "实验委托检测内容", "项目摘要", "是否需要伦理评审", "是否需要伦理批文",
                "伦理批文编号", "伦理批文路径", "项目申报书路径", "备注", "建立时间", "是否立项"
            )
            VALUES (
                nextval(pg_get_serial_sequence('"纵向项目申报"', '编号')),
                COALESCE(r."分类", 1), r."项目名称", r."申报人编号", r."学科门类", r."学科类型",
                COALESCE(r."申报年度", EXTRACT(YEAR FROM CURRENT_DATE)::integer),
                r."项目类型", r."项目级别", r."项目来源单位", r."项目类别",
                COALESCE(r."申报时间", CURRENT_TIMESTAMP), COALESCE(r."申请经费", 0),
                r."计划开始日期", r."计划完成日期", r."项目等级",
                r."是否委托外单位进行实验或实验分析", r."实验委托检测内容", r."项目摘要",
                r."是否需要伦理评审", r."是否需要伦理批文", r."伦理批文编号", r."伦理批文路径",
                r."项目申报书路径", r."备注", CURRENT_TIMESTAMP, r."是否立项"
            )
            RETURNING "编号" INTO v_new_id;
        ELSE
            UPDATE "纵向项目申报" SET
                "分类" = COALESCE(r."分类", "分类"),
                "项目名称" = COALESCE(r."项目名称", "项目名称"),
                "申报人编号" = COALESCE(r."申报人编号", "申报人编号"),
                "学科门类" = r."学科门类",
                "学科类型" = r."学科类型",
                "申报年度" = COALESCE(r."申报年度", "申报年度"),
                "项目类型" = r."项目类型",
                "项目级别" = r."项目级别",
                "项目来源单位" = r."项目来源单位",
                "项目类别" = r."项目类别",
                "申报时间" = COALESCE(r."申报时间", "申报时间"),
                "申请经费" = COALESCE(r."申请经费", "申请经费"),
                "计划开始日期" = COALESCE(r."计划开始日期", "计划开始日期"),
                "计划完成日期" = COALESCE(r."计划完成日期", "计划完成日期"),
                "项目等级" = r."项目等级",
                "是否委托外单位进行实验或实验分析" = r."是否委托外单位进行实验或实验分析",
                "实验委托检测内容" = r."实验委托检测内容",
                "项目摘要" = r."项目摘要",
                "是否需要伦理评审" = r."是否需要伦理评审",
                "是否需要伦理批文" = r."是否需要伦理批文",
                "伦理批文编号" = r."伦理批文编号",
                "伦理批文路径" = r."伦理批文路径",
                "项目申报书路径" = r."项目申报书路径",
                "备注" = r."备注",
                "是否立项" = r."是否立项"
            WHERE "编号" = r."编号";
            v_new_id := r."编号";
        END IF;
    END LOOP;

    IF v_new_id IS NOT NULL AND p_payload ? 'tt_项目参与人' AND jsonb_typeof(p_payload->'tt_项目参与人') = 'array' THEN
        DELETE FROM "项目参与人" WHERE "流程模板编号" = v_template_id AND "项目编号" = v_new_id;
        INSERT INTO "项目参与人" ("流程模板编号", "项目编号", "参与人编号", "工作量占比", "备注", "建立时间")
        SELECT v_template_id, v_new_id, p."参与人编号", p."工作量占比", p."备注", CURRENT_TIMESTAMP
        FROM jsonb_to_recordset(p_payload->'tt_项目参与人') AS p(
            "参与人编号" integer, "工作量占比" integer, "备注" varchar(500)
        )
        WHERE p."参与人编号" IS NOT NULL AND p."参与人编号" > 0;
    END IF;

    RETURN COALESCE(v_new_id, 0);
END;
$$;

CREATE OR REPLACE FUNCTION "sp_讲座反馈_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "讲座反馈" ("编号", "讲座编号", "专家姓名", "专家身份证号码", "专家护照号", "专家联系方式", "专家讲课费金额", "结算方式", "活动持续分钟", "参会人数", "讲座附件路径", "讲座效果")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"讲座反馈"', '编号')) ELSE r."编号" END, r."讲座编号", r."专家姓名", r."专家身份证号码", r."专家护照号", r."专家联系方式", r."专家讲课费金额", r."结算方式", r."活动持续分钟", r."参会人数", r."讲座附件路径", r."讲座效果"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "讲座编号" integer, "专家姓名" varchar(50), "专家身份证号码" varchar(50), "专家护照号" varchar(50), "专家联系方式" varchar(50), "专家讲课费金额" numeric(18,2), "结算方式" varchar(50), "活动持续分钟" integer, "参会人数" integer, "讲座附件路径" text, "讲座效果" varchar(500))
    ON CONFLICT ("编号") DO UPDATE SET "讲座编号" = EXCLUDED."讲座编号", "专家姓名" = EXCLUDED."专家姓名", "专家身份证号码" = EXCLUDED."专家身份证号码", "专家护照号" = EXCLUDED."专家护照号", "专家联系方式" = EXCLUDED."专家联系方式", "专家讲课费金额" = EXCLUDED."专家讲课费金额", "结算方式" = EXCLUDED."结算方式", "活动持续分钟" = EXCLUDED."活动持续分钟", "参会人数" = EXCLUDED."参会人数", "讲座附件路径" = EXCLUDED."讲座附件路径", "讲座效果" = EXCLUDED."讲座效果";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_经费模版项目支出类型_删"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    DELETE FROM "经费模版项目支出类型" AS t
    WHERE t."编号" IN (SELECT (r."编号") FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "所属经费模版编号" integer, "项目支出类型" varchar(100), "经费占比" integer, "备注" varchar(500)));
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_参加会议_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "参加会议" ("编号", "会议名称", "会议主办单位", "会议承办单位", "研讨领域", "学科类型", "会议地址", "会议开始时间", "会议结束时间", "往返时间", "会议网址", "会议简介及申请理由", "申请人编号", "参会相关文件路径")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"参加会议"', '编号')) ELSE r."编号" END, r."会议名称", r."会议主办单位", r."会议承办单位", r."研讨领域", r."学科类型", r."会议地址", r."会议开始时间", r."会议结束时间", r."往返时间", r."会议网址", r."会议简介及申请理由", r."申请人编号", r."参会相关文件路径"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "会议名称" varchar(100), "会议主办单位" varchar(100), "会议承办单位" varchar(100), "研讨领域" varchar(100), "学科类型" varchar(50), "会议地址" varchar(500), "会议开始时间" timestamp without time zone, "会议结束时间" timestamp without time zone, "往返时间" integer, "会议网址" varchar(500), "会议简介及申请理由" varchar(500), "申请人编号" integer, "参会相关文件路径" text)
    ON CONFLICT ("编号") DO UPDATE SET "会议名称" = EXCLUDED."会议名称", "会议主办单位" = EXCLUDED."会议主办单位", "会议承办单位" = EXCLUDED."会议承办单位", "研讨领域" = EXCLUDED."研讨领域", "学科类型" = EXCLUDED."学科类型", "会议地址" = EXCLUDED."会议地址", "会议开始时间" = EXCLUDED."会议开始时间", "会议结束时间" = EXCLUDED."会议结束时间", "往返时间" = EXCLUDED."往返时间", "会议网址" = EXCLUDED."会议网址", "会议简介及申请理由" = EXCLUDED."会议简介及申请理由", "申请人编号" = EXCLUDED."申请人编号", "参会相关文件路径" = EXCLUDED."参会相关文件路径";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_字典_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "字典" ("编号", "分类", "值", "上级编号", "级别", "排序", "备注")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"字典"', '编号')) ELSE r."编号" END, r."分类", r."值", r."上级编号", r."级别", r."排序", r."备注"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "分类" varchar(50), "值" varchar(50), "上级编号" integer, "级别" integer, "排序" integer, "备注" varchar(500))
    ON CONFLICT ("编号") DO UPDATE SET "分类" = EXCLUDED."分类", "值" = EXCLUDED."值", "上级编号" = EXCLUDED."上级编号", "级别" = EXCLUDED."级别", "排序" = EXCLUDED."排序", "备注" = EXCLUDED."备注";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_经费模版项目支出内容_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "经费模版项目支出内容" ("编号", "所属经费模版项目支出类型编号", "项目支出内容", "财务科目", "备注")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"经费模版项目支出内容"', '编号')) ELSE r."编号" END, r."所属经费模版项目支出类型编号", r."项目支出内容", r."财务科目", r."备注"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "所属经费模版项目支出类型编号" integer, "项目支出内容" varchar(100), "财务科目" varchar(100), "备注" varchar(500))
    ON CONFLICT ("编号") DO UPDATE SET "所属经费模版项目支出类型编号" = EXCLUDED."所属经费模版项目支出类型编号", "项目支出内容" = EXCLUDED."项目支出内容", "财务科目" = EXCLUDED."财务科目", "备注" = EXCLUDED."备注";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_字典_删"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    DELETE FROM "字典" AS t
    WHERE t."编号" IN (SELECT (r."编号") FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "分类" varchar(50), "值" varchar(50), "上级编号" integer, "级别" integer, "排序" integer, "备注" varchar(500)));
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_经费模版项目支出内容_删"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    DELETE FROM "经费模版项目支出内容" AS t
    WHERE t."编号" IN (SELECT (r."编号") FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "所属经费模版项目支出类型编号" integer, "项目支出内容" varchar(100), "财务科目" varchar(100), "备注" varchar(500)));
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_人员_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "人员" ("编号", "工号", "密码", "姓名", "部门编号", "英文名", "性别", "身份证", "出生日期", "入职日期", "手机", "QQ", "邮箱", "籍贯", "毕业日期", "合同开始日期", "合同结束日期", "参加工作日期", "专业技术获得日期", "职务任职日期", "学位", "学历", "政治面貌", "技术职称类别", "技术职称", "第二技术职称类别", "第二技术职称", "婚姻情况", "学制", "学历性质", "学习形式", "人员类别", "个人身份", "民族", "专业技术级别", "所学专业名称", "岗位性质", "职务名称", "职务级别", "岗位名称", "岗位类型", "学科门类", "一级学科", "二级学科", "三级学科", "人员类型", "获得最高学历的院校", "获得最高学位的院校", "住宅地址", "学术特长", "研究方向", "个人获得的荣誉和奖项", "备注", "是否启用")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"人员"', '编号')) ELSE r."编号" END, r."工号", r."密码", r."姓名", r."部门编号", r."英文名", r."性别", r."身份证", r."出生日期", r."入职日期", r."手机", r."QQ", r."邮箱", r."籍贯", r."毕业日期", r."合同开始日期", r."合同结束日期", r."参加工作日期", r."专业技术获得日期", r."职务任职日期", r."学位", r."学历", r."政治面貌", r."技术职称类别", r."技术职称", r."第二技术职称类别", r."第二技术职称", r."婚姻情况", r."学制", r."学历性质", r."学习形式", r."人员类别", r."个人身份", r."民族", r."专业技术级别", r."所学专业名称", r."岗位性质", r."职务名称", r."职务级别", r."岗位名称", r."岗位类型", r."学科门类", r."一级学科", r."二级学科", r."三级学科", r."人员类型", r."获得最高学历的院校", r."获得最高学位的院校", r."住宅地址", r."学术特长", r."研究方向", r."个人获得的荣誉和奖项", r."备注", r."是否启用"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "工号" varchar(50), "密码" varchar(50), "姓名" varchar(50), "部门编号" integer, "英文名" varchar(50), "性别" varchar(50), "身份证" varchar(50), "出生日期" date, "入职日期" date, "手机" varchar(50), "QQ" varchar(50), "邮箱" varchar(50), "籍贯" varchar(50), "毕业日期" date, "合同开始日期" date, "合同结束日期" date, "参加工作日期" date, "专业技术获得日期" date, "职务任职日期" date, "学位" varchar(50), "学历" varchar(50), "政治面貌" varchar(50), "技术职称类别" varchar(100), "技术职称" varchar(100), "第二技术职称类别" varchar(100), "第二技术职称" varchar(100), "婚姻情况" varchar(50), "学制" varchar(50), "学历性质" varchar(50), "学习形式" varchar(50), "人员类别" varchar(50), "个人身份" varchar(50), "民族" varchar(50), "专业技术级别" varchar(50), "所学专业名称" varchar(50), "岗位性质" varchar(50), "职务名称" varchar(50), "职务级别" varchar(50), "岗位名称" varchar(50), "岗位类型" varchar(50), "学科门类" varchar(50), "一级学科" varchar(50), "二级学科" varchar(50), "三级学科" varchar(50), "人员类型" varchar(50), "获得最高学历的院校" varchar(100), "获得最高学位的院校" varchar(100), "住宅地址" varchar(100), "学术特长" varchar(1000), "研究方向" varchar(1000), "个人获得的荣誉和奖项" varchar(1000), "备注" varchar(500), "是否启用" boolean)
    ON CONFLICT ("编号") DO UPDATE SET "工号" = EXCLUDED."工号", "密码" = EXCLUDED."密码", "姓名" = EXCLUDED."姓名", "部门编号" = EXCLUDED."部门编号", "英文名" = EXCLUDED."英文名", "性别" = EXCLUDED."性别", "身份证" = EXCLUDED."身份证", "出生日期" = EXCLUDED."出生日期", "入职日期" = EXCLUDED."入职日期", "手机" = EXCLUDED."手机", "QQ" = EXCLUDED."QQ", "邮箱" = EXCLUDED."邮箱", "籍贯" = EXCLUDED."籍贯", "毕业日期" = EXCLUDED."毕业日期", "合同开始日期" = EXCLUDED."合同开始日期", "合同结束日期" = EXCLUDED."合同结束日期", "参加工作日期" = EXCLUDED."参加工作日期", "专业技术获得日期" = EXCLUDED."专业技术获得日期", "职务任职日期" = EXCLUDED."职务任职日期", "学位" = EXCLUDED."学位", "学历" = EXCLUDED."学历", "政治面貌" = EXCLUDED."政治面貌", "技术职称类别" = EXCLUDED."技术职称类别", "技术职称" = EXCLUDED."技术职称", "第二技术职称类别" = EXCLUDED."第二技术职称类别", "第二技术职称" = EXCLUDED."第二技术职称", "婚姻情况" = EXCLUDED."婚姻情况", "学制" = EXCLUDED."学制", "学历性质" = EXCLUDED."学历性质", "学习形式" = EXCLUDED."学习形式", "人员类别" = EXCLUDED."人员类别", "个人身份" = EXCLUDED."个人身份", "民族" = EXCLUDED."民族", "专业技术级别" = EXCLUDED."专业技术级别", "所学专业名称" = EXCLUDED."所学专业名称", "岗位性质" = EXCLUDED."岗位性质", "职务名称" = EXCLUDED."职务名称", "职务级别" = EXCLUDED."职务级别", "岗位名称" = EXCLUDED."岗位名称", "岗位类型" = EXCLUDED."岗位类型", "学科门类" = EXCLUDED."学科门类", "一级学科" = EXCLUDED."一级学科", "二级学科" = EXCLUDED."二级学科", "三级学科" = EXCLUDED."三级学科", "人员类型" = EXCLUDED."人员类型", "获得最高学历的院校" = EXCLUDED."获得最高学历的院校", "获得最高学位的院校" = EXCLUDED."获得最高学位的院校", "住宅地址" = EXCLUDED."住宅地址", "学术特长" = EXCLUDED."学术特长", "研究方向" = EXCLUDED."研究方向", "个人获得的荣誉和奖项" = EXCLUDED."个人获得的荣誉和奖项", "备注" = EXCLUDED."备注", "是否启用" = EXCLUDED."是否启用";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_专家评分_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "专家评分" ("编号", "流程模板编号", "项目编号", "评分项", "评分", "备注", "建立时间")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"专家评分"', '编号')) ELSE r."编号" END, r."流程模板编号", r."项目编号", r."评分项", r."评分", r."备注", r."建立时间"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "流程模板编号" integer, "项目编号" integer, "评分项" varchar(100), "评分" varchar(50), "备注" varchar(500), "建立时间" timestamp without time zone)
    ON CONFLICT ("编号") DO UPDATE SET "流程模板编号" = EXCLUDED."流程模板编号", "项目编号" = EXCLUDED."项目编号", "评分项" = EXCLUDED."评分项", "评分" = EXCLUDED."评分", "备注" = EXCLUDED."备注", "建立时间" = EXCLUDED."建立时间";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_人员_启禁"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    UPDATE "人员" SET "是否启用" = COALESCE((p_payload->>'是否启用')::boolean, FALSE)
    WHERE "编号" = (p_payload->>'编号')::integer;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_参加会议反馈_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "参加会议反馈" ("编号", "参加会议编号", "活动持续分钟", "参会人数", "参会总结", "会议反馈文件路径")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"参加会议反馈"', '编号')) ELSE r."编号" END, r."参加会议编号", r."活动持续分钟", r."参会人数", r."参会总结", r."会议反馈文件路径"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "参加会议编号" integer, "活动持续分钟" integer, "参会人数" integer, "参会总结" varchar(500), "会议反馈文件路径" text)
    ON CONFLICT ("编号") DO UPDATE SET "参加会议编号" = EXCLUDED."参加会议编号", "活动持续分钟" = EXCLUDED."活动持续分钟", "参会人数" = EXCLUDED."参会人数", "参会总结" = EXCLUDED."参会总结", "会议反馈文件路径" = EXCLUDED."会议反馈文件路径";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_纵向项目立项_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "纵向项目立项" ("编号", "分类", "项目登记情况", "项目编号", "项目中文名称", "项目英文名称", "项目分类", "成果归属部门编号", "负责人编号", "立项年度", "登记日期", "登记人编号", "批准日期", "批准经费", "配套经费", "经费卡号", "项目等级", "计划开始日期", "计划完成日期", "学科门类", "学科类型", "研究类型", "项目类型", "项目级别", "项目来源单位", "项目类别", "档案编号", "财务简称", "亚类说明", "附注说明", "纵向项目合同类型", "社会经济目标", "是否需要伦理评审", "是否需要伦理批文", "伦理批文编号", "伦理批文时间", "伦理批文路径", "立项文件路径", "备注", "建立时间")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"纵向项目立项"', '编号')) ELSE r."编号" END, r."分类", r."项目登记情况", r."项目编号", r."项目中文名称", r."项目英文名称", r."项目分类", r."成果归属部门编号", r."负责人编号", r."立项年度", r."登记日期", r."登记人编号", r."批准日期", r."批准经费", r."配套经费", r."经费卡号", r."项目等级", r."计划开始日期", r."计划完成日期", r."学科门类", r."学科类型", r."研究类型", r."项目类型", r."项目级别", r."项目来源单位", r."项目类别", r."档案编号", r."财务简称", r."亚类说明", r."附注说明", r."纵向项目合同类型", r."社会经济目标", r."是否需要伦理评审", r."是否需要伦理批文", r."伦理批文编号", r."伦理批文时间", r."伦理批文路径", r."立项文件路径", r."备注", r."建立时间"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "分类" integer, "项目登记情况" varchar(50), "项目编号" varchar(50), "项目中文名称" varchar(100), "项目英文名称" varchar(100), "项目分类" varchar(50), "成果归属部门编号" integer, "负责人编号" integer, "立项年度" integer, "登记日期" date, "登记人编号" integer, "批准日期" date, "批准经费" numeric(18,2), "配套经费" numeric(18,2), "经费卡号" varchar(50), "项目等级" varchar(50), "计划开始日期" date, "计划完成日期" date, "学科门类" varchar(50), "学科类型" varchar(50), "研究类型" varchar(50), "项目类型" varchar(50), "项目级别" varchar(50), "项目来源单位" varchar(50), "项目类别" varchar(50), "档案编号" varchar(50), "财务简称" varchar(100), "亚类说明" varchar(500), "附注说明" varchar(500), "纵向项目合同类型" varchar(50), "社会经济目标" varchar(1000), "是否需要伦理评审" boolean, "是否需要伦理批文" boolean, "伦理批文编号" varchar(50), "伦理批文时间" timestamp without time zone, "伦理批文路径" text, "立项文件路径" text, "备注" varchar(500), "建立时间" timestamp without time zone)
    ON CONFLICT ("编号") DO UPDATE SET "分类" = EXCLUDED."分类", "项目登记情况" = EXCLUDED."项目登记情况", "项目编号" = EXCLUDED."项目编号", "项目中文名称" = EXCLUDED."项目中文名称", "项目英文名称" = EXCLUDED."项目英文名称", "项目分类" = EXCLUDED."项目分类", "成果归属部门编号" = EXCLUDED."成果归属部门编号", "负责人编号" = EXCLUDED."负责人编号", "立项年度" = EXCLUDED."立项年度", "登记日期" = EXCLUDED."登记日期", "登记人编号" = EXCLUDED."登记人编号", "批准日期" = EXCLUDED."批准日期", "批准经费" = EXCLUDED."批准经费", "配套经费" = EXCLUDED."配套经费", "经费卡号" = EXCLUDED."经费卡号", "项目等级" = EXCLUDED."项目等级", "计划开始日期" = EXCLUDED."计划开始日期", "计划完成日期" = EXCLUDED."计划完成日期", "学科门类" = EXCLUDED."学科门类", "学科类型" = EXCLUDED."学科类型", "研究类型" = EXCLUDED."研究类型", "项目类型" = EXCLUDED."项目类型", "项目级别" = EXCLUDED."项目级别", "项目来源单位" = EXCLUDED."项目来源单位", "项目类别" = EXCLUDED."项目类别", "档案编号" = EXCLUDED."档案编号", "财务简称" = EXCLUDED."财务简称", "亚类说明" = EXCLUDED."亚类说明", "附注说明" = EXCLUDED."附注说明", "纵向项目合同类型" = EXCLUDED."纵向项目合同类型", "社会经济目标" = EXCLUDED."社会经济目标", "是否需要伦理评审" = EXCLUDED."是否需要伦理评审", "是否需要伦理批文" = EXCLUDED."是否需要伦理批文", "伦理批文编号" = EXCLUDED."伦理批文编号", "伦理批文时间" = EXCLUDED."伦理批文时间", "伦理批文路径" = EXCLUDED."伦理批文路径", "立项文件路径" = EXCLUDED."立项文件路径", "备注" = EXCLUDED."备注", "建立时间" = EXCLUDED."建立时间";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_部门_启禁"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    UPDATE "部门" SET "是否启用" = COALESCE((p_payload->>'是否启用')::boolean, FALSE)
    WHERE "编号" = (p_payload->>'编号')::integer;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_部门_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "部门" ("编号", "上级部门编号", "名称", "负责人编号", "排序值", "备注", "是否启用")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"部门"', '编号')) ELSE r."编号" END, r."上级部门编号", r."名称", r."负责人编号", r."排序值", r."备注", r."是否启用"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "上级部门编号" integer, "名称" varchar(50), "负责人编号" integer, "排序值" integer, "备注" varchar(500), "是否启用" boolean)
    ON CONFLICT ("编号") DO UPDATE SET "上级部门编号" = EXCLUDED."上级部门编号", "名称" = EXCLUDED."名称", "负责人编号" = EXCLUDED."负责人编号", "排序值" = EXCLUDED."排序值", "备注" = EXCLUDED."备注", "是否启用" = EXCLUDED."是否启用";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_项目经费到账_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "项目经费到账" ("编号", "流程模板编号", "项目编号", "拨款单位", "到账日期", "到账金额", "技术交易额", "管理费用", "技术合同认定服务费", "增值税", "到账单位", "经办人编号", "到账凭证路径", "建立时间", "备注")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"项目经费到账"', '编号')) ELSE r."编号" END, r."流程模板编号", r."项目编号", r."拨款单位", r."到账日期", r."到账金额", r."技术交易额", r."管理费用", r."技术合同认定服务费", r."增值税", r."到账单位", r."经办人编号", r."到账凭证路径", r."建立时间", r."备注"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "流程模板编号" integer, "项目编号" integer, "拨款单位" varchar(100), "到账日期" date, "到账金额" numeric(18,2), "技术交易额" numeric(18,2), "管理费用" numeric(18,2), "技术合同认定服务费" numeric(18,2), "增值税" numeric(18,2), "到账单位" varchar(100), "经办人编号" integer, "到账凭证路径" text, "建立时间" timestamp without time zone, "备注" varchar(1000))
    ON CONFLICT ("编号") DO UPDATE SET "流程模板编号" = EXCLUDED."流程模板编号", "项目编号" = EXCLUDED."项目编号", "拨款单位" = EXCLUDED."拨款单位", "到账日期" = EXCLUDED."到账日期", "到账金额" = EXCLUDED."到账金额", "技术交易额" = EXCLUDED."技术交易额", "管理费用" = EXCLUDED."管理费用", "技术合同认定服务费" = EXCLUDED."技术合同认定服务费", "增值税" = EXCLUDED."增值税", "到账单位" = EXCLUDED."到账单位", "经办人编号" = EXCLUDED."经办人编号", "到账凭证路径" = EXCLUDED."到账凭证路径", "建立时间" = EXCLUDED."建立时间", "备注" = EXCLUDED."备注";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_报销_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "报销" ("编号", "流程模板编号", "项目编号", "报销编号", "报销金额", "报销方式", "公务卡号", "公务卡所属人姓名", "报销人编号", "报销时间", "相关文件路径", "申请时间", "建立时间", "报销经费用途")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"报销"', '编号')) ELSE r."编号" END, r."流程模板编号", r."项目编号", r."报销编号", r."报销金额", r."报销方式", r."公务卡号", r."公务卡所属人姓名", r."报销人编号", r."报销时间", r."相关文件路径", r."申请时间", r."建立时间", r."报销经费用途"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "流程模板编号" integer, "项目编号" integer, "报销编号" varchar(50), "报销金额" numeric(18,2), "报销方式" varchar(50), "公务卡号" varchar(50), "公务卡所属人姓名" varchar(50), "报销人编号" integer, "报销时间" timestamp without time zone, "相关文件路径" text, "申请时间" timestamp without time zone, "建立时间" timestamp without time zone, "报销经费用途" varchar(1000))
    ON CONFLICT ("编号") DO UPDATE SET "流程模板编号" = EXCLUDED."流程模板编号", "项目编号" = EXCLUDED."项目编号", "报销编号" = EXCLUDED."报销编号", "报销金额" = EXCLUDED."报销金额", "报销方式" = EXCLUDED."报销方式", "公务卡号" = EXCLUDED."公务卡号", "公务卡所属人姓名" = EXCLUDED."公务卡所属人姓名", "报销人编号" = EXCLUDED."报销人编号", "报销时间" = EXCLUDED."报销时间", "相关文件路径" = EXCLUDED."相关文件路径", "申请时间" = EXCLUDED."申请时间", "建立时间" = EXCLUDED."建立时间", "报销经费用途" = EXCLUDED."报销经费用途";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_报销详情_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "报销详情" ("编号", "报销编号", "项目支出类型", "项目支出内容", "财务科目", "报销金额", "备注")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"报销详情"', '编号')) ELSE r."编号" END, r."报销编号", r."项目支出类型", r."项目支出内容", r."财务科目", r."报销金额", r."备注"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "报销编号" integer, "项目支出类型" varchar(100), "项目支出内容" varchar(100), "财务科目" varchar(50), "报销金额" numeric(18,2), "备注" varchar(500))
    ON CONFLICT ("编号") DO UPDATE SET "报销编号" = EXCLUDED."报销编号", "项目支出类型" = EXCLUDED."项目支出类型", "项目支出内容" = EXCLUDED."项目支出内容", "财务科目" = EXCLUDED."财务科目", "报销金额" = EXCLUDED."报销金额", "备注" = EXCLUDED."备注";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_纵向项目中检_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "纵向项目中检" ("编号", "纵向项目编号", "中检资料路径", "备注", "建立时间")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"纵向项目中检"', '编号')) ELSE r."编号" END, r."纵向项目编号", r."中检资料路径", r."备注", r."建立时间"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "纵向项目编号" integer, "中检资料路径" varchar(500), "备注" varchar(1000), "建立时间" timestamp without time zone)
    ON CONFLICT ("编号") DO UPDATE SET "纵向项目编号" = EXCLUDED."纵向项目编号", "中检资料路径" = EXCLUDED."中检资料路径", "备注" = EXCLUDED."备注", "建立时间" = EXCLUDED."建立时间";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_成果论文_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "成果论文" ("编号", "认领人编号", "论文标题", "论文标题类型", "论文Online日期", "论文正式出版日期", "他引次数", "年度", "收稿日期", "论文所属栏目", "版面费", "超额版面费", "超额费用来源", "奖励情况", "论文字数", "计划来源", "论文版面", "学科门类", "一级学科", "二级学科", "三级学科", "作者人数", "关键字", "论文摘要", "备注", "刊物编号", "卷号", "期号", "页码范围起", "页码范围止", "论文类型", "是否与行业联合发表", "是否与地方联合发表", "是否与国际联合发表", "是否跨学科", "是否与本专业相关", "检索信息页相关文件路径", "论文正文路径", "其他相关文件路径")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"成果论文"', '编号')) ELSE r."编号" END, r."认领人编号", r."论文标题", r."论文标题类型", r."论文Online日期", r."论文正式出版日期", r."他引次数", r."年度", r."收稿日期", r."论文所属栏目", r."版面费", r."超额版面费", r."超额费用来源", r."奖励情况", r."论文字数", r."计划来源", r."论文版面", r."学科门类", r."一级学科", r."二级学科", r."三级学科", r."作者人数", r."关键字", r."论文摘要", r."备注", r."刊物编号", r."卷号", r."期号", r."页码范围起", r."页码范围止", r."论文类型", r."是否与行业联合发表", r."是否与地方联合发表", r."是否与国际联合发表", r."是否跨学科", r."是否与本专业相关", r."检索信息页相关文件路径", r."论文正文路径", r."其他相关文件路径"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "认领人编号" integer, "论文标题" varchar(200), "论文标题类型" varchar(50), "论文Online日期" date, "论文正式出版日期" date, "他引次数" integer, "年度" integer, "收稿日期" date, "论文所属栏目" varchar(50), "版面费" numeric(18,2), "超额版面费" numeric(18,2), "超额费用来源" varchar(50), "奖励情况" varchar(50), "论文字数" numeric(18,2), "计划来源" varchar(50), "论文版面" varchar(50), "学科门类" varchar(50), "一级学科" varchar(50), "二级学科" varchar(50), "三级学科" varchar(50), "作者人数" integer, "关键字" varchar(500), "论文摘要" varchar(1000), "备注" varchar(1000), "刊物编号" integer, "卷号" integer, "期号" integer, "页码范围起" integer, "页码范围止" integer, "论文类型" varchar(50), "是否与行业联合发表" boolean, "是否与地方联合发表" boolean, "是否与国际联合发表" boolean, "是否跨学科" boolean, "是否与本专业相关" boolean, "检索信息页相关文件路径" text, "论文正文路径" text, "其他相关文件路径" text)
    ON CONFLICT ("编号") DO UPDATE SET "认领人编号" = EXCLUDED."认领人编号", "论文标题" = EXCLUDED."论文标题", "论文标题类型" = EXCLUDED."论文标题类型", "论文Online日期" = EXCLUDED."论文Online日期", "论文正式出版日期" = EXCLUDED."论文正式出版日期", "他引次数" = EXCLUDED."他引次数", "年度" = EXCLUDED."年度", "收稿日期" = EXCLUDED."收稿日期", "论文所属栏目" = EXCLUDED."论文所属栏目", "版面费" = EXCLUDED."版面费", "超额版面费" = EXCLUDED."超额版面费", "超额费用来源" = EXCLUDED."超额费用来源", "奖励情况" = EXCLUDED."奖励情况", "论文字数" = EXCLUDED."论文字数", "计划来源" = EXCLUDED."计划来源", "论文版面" = EXCLUDED."论文版面", "学科门类" = EXCLUDED."学科门类", "一级学科" = EXCLUDED."一级学科", "二级学科" = EXCLUDED."二级学科", "三级学科" = EXCLUDED."三级学科", "作者人数" = EXCLUDED."作者人数", "关键字" = EXCLUDED."关键字", "论文摘要" = EXCLUDED."论文摘要", "备注" = EXCLUDED."备注", "刊物编号" = EXCLUDED."刊物编号", "卷号" = EXCLUDED."卷号", "期号" = EXCLUDED."期号", "页码范围起" = EXCLUDED."页码范围起", "页码范围止" = EXCLUDED."页码范围止", "论文类型" = EXCLUDED."论文类型", "是否与行业联合发表" = EXCLUDED."是否与行业联合发表", "是否与地方联合发表" = EXCLUDED."是否与地方联合发表", "是否与国际联合发表" = EXCLUDED."是否与国际联合发表", "是否跨学科" = EXCLUDED."是否跨学科", "是否与本专业相关" = EXCLUDED."是否与本专业相关", "检索信息页相关文件路径" = EXCLUDED."检索信息页相关文件路径", "论文正文路径" = EXCLUDED."论文正文路径", "其他相关文件路径" = EXCLUDED."其他相关文件路径";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_任职_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "任职" ("编号", "任职开始时间", "任职截止时间", "任职人员编号", "任职团体名称", "所任职务", "相关文件路径", "建立时间", "备注")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"任职"', '编号')) ELSE r."编号" END, r."任职开始时间", r."任职截止时间", r."任职人员编号", r."任职团体名称", r."所任职务", r."相关文件路径", r."建立时间", r."备注"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "任职开始时间" timestamp without time zone, "任职截止时间" timestamp without time zone, "任职人员编号" integer, "任职团体名称" varchar(200), "所任职务" varchar(200), "相关文件路径" text, "建立时间" timestamp without time zone, "备注" varchar(500))
    ON CONFLICT ("编号") DO UPDATE SET "任职开始时间" = EXCLUDED."任职开始时间", "任职截止时间" = EXCLUDED."任职截止时间", "任职人员编号" = EXCLUDED."任职人员编号", "任职团体名称" = EXCLUDED."任职团体名称", "所任职务" = EXCLUDED."所任职务", "相关文件路径" = EXCLUDED."相关文件路径", "建立时间" = EXCLUDED."建立时间", "备注" = EXCLUDED."备注";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_期刊_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "期刊" ("编号", "代码", "名称", "英文刊名", "主办单位", "期刊系列", "收录情况", "语种", "JCR分区", "编目名称", "编目类别", "年度", "创刊日期", "被引用次数", "影响因子", "特征因子分值", "ISSN号", "CN号", "出版周期", "出版地", "开本", "备注")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"期刊"', '编号')) ELSE r."编号" END, r."代码", r."名称", r."英文刊名", r."主办单位", r."期刊系列", r."收录情况", r."语种", r."JCR分区", r."编目名称", r."编目类别", r."年度", r."创刊日期", r."被引用次数", r."影响因子", r."特征因子分值", r."ISSN号", r."CN号", r."出版周期", r."出版地", r."开本", r."备注"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "代码" varchar(50), "名称" varchar(500), "英文刊名" varchar(500), "主办单位" varchar(500), "期刊系列" varchar(500), "收录情况" varchar(1000), "语种" varchar(50), "JCR分区" varchar(50), "编目名称" varchar(100), "编目类别" varchar(100), "年度" integer, "创刊日期" date, "被引用次数" integer, "影响因子" numeric(18,3), "特征因子分值" numeric(18,3), "ISSN号" varchar(50), "CN号" varchar(50), "出版周期" varchar(50), "出版地" varchar(50), "开本" varchar(50), "备注" varchar(500))
    ON CONFLICT ("编号") DO UPDATE SET "代码" = EXCLUDED."代码", "名称" = EXCLUDED."名称", "英文刊名" = EXCLUDED."英文刊名", "主办单位" = EXCLUDED."主办单位", "期刊系列" = EXCLUDED."期刊系列", "收录情况" = EXCLUDED."收录情况", "语种" = EXCLUDED."语种", "JCR分区" = EXCLUDED."JCR分区", "编目名称" = EXCLUDED."编目名称", "编目类别" = EXCLUDED."编目类别", "年度" = EXCLUDED."年度", "创刊日期" = EXCLUDED."创刊日期", "被引用次数" = EXCLUDED."被引用次数", "影响因子" = EXCLUDED."影响因子", "特征因子分值" = EXCLUDED."特征因子分值", "ISSN号" = EXCLUDED."ISSN号", "CN号" = EXCLUDED."CN号", "出版周期" = EXCLUDED."出版周期", "出版地" = EXCLUDED."出版地", "开本" = EXCLUDED."开本", "备注" = EXCLUDED."备注";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_纵向项目结题_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "纵向项目结题" ("编号", "纵向项目编号", "结题验收编号", "结题验收时间", "结题验收是否合格", "结题验收参与人数", "结题资料路径", "备注", "建立时间")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"纵向项目结题"', '编号')) ELSE r."编号" END, r."纵向项目编号", r."结题验收编号", r."结题验收时间", r."结题验收是否合格", r."结题验收参与人数", r."结题资料路径", r."备注", r."建立时间"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "纵向项目编号" integer, "结题验收编号" varchar(100), "结题验收时间" timestamp without time zone, "结题验收是否合格" boolean, "结题验收参与人数" integer, "结题资料路径" varchar(1000), "备注" varchar(1000), "建立时间" timestamp without time zone)
    ON CONFLICT ("编号") DO UPDATE SET "纵向项目编号" = EXCLUDED."纵向项目编号", "结题验收编号" = EXCLUDED."结题验收编号", "结题验收时间" = EXCLUDED."结题验收时间", "结题验收是否合格" = EXCLUDED."结题验收是否合格", "结题验收参与人数" = EXCLUDED."结题验收参与人数", "结题资料路径" = EXCLUDED."结题资料路径", "备注" = EXCLUDED."备注", "建立时间" = EXCLUDED."建立时间";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_成果著作_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "成果著作" ("编号", "著作名称", "出版社名称", "著作形式", "年度", "著作出版日期", "ISBN号", "著作字数", "奖励情况", "出版地", "出版社级别", "著作类型", "是否译为外文", "语种", "学科门类", "一级学科", "二级学科", "三级学科", "作者人数", "发行价", "备注", "封面相关文件路径", "版权页相关文件路径", "前言页相关文件路径", "完整目录页相关文件路径", "其他相关文件路径")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"成果著作"', '编号')) ELSE r."编号" END, r."著作名称", r."出版社名称", r."著作形式", r."年度", r."著作出版日期", r."ISBN号", r."著作字数", r."奖励情况", r."出版地", r."出版社级别", r."著作类型", r."是否译为外文", r."语种", r."学科门类", r."一级学科", r."二级学科", r."三级学科", r."作者人数", r."发行价", r."备注", r."封面相关文件路径", r."版权页相关文件路径", r."前言页相关文件路径", r."完整目录页相关文件路径", r."其他相关文件路径"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "著作名称" varchar(100), "出版社名称" varchar(100), "著作形式" varchar(50), "年度" integer, "著作出版日期" date, "ISBN号" varchar(50), "著作字数" integer, "奖励情况" varchar(50), "出版地" varchar(50), "出版社级别" varchar(50), "著作类型" varchar(50), "是否译为外文" boolean, "语种" varchar(50), "学科门类" varchar(50), "一级学科" varchar(50), "二级学科" varchar(50), "三级学科" varchar(50), "作者人数" integer, "发行价" numeric(18,2), "备注" varchar(500), "封面相关文件路径" text, "版权页相关文件路径" text, "前言页相关文件路径" text, "完整目录页相关文件路径" text, "其他相关文件路径" text)
    ON CONFLICT ("编号") DO UPDATE SET "著作名称" = EXCLUDED."著作名称", "出版社名称" = EXCLUDED."出版社名称", "著作形式" = EXCLUDED."著作形式", "年度" = EXCLUDED."年度", "著作出版日期" = EXCLUDED."著作出版日期", "ISBN号" = EXCLUDED."ISBN号", "著作字数" = EXCLUDED."著作字数", "奖励情况" = EXCLUDED."奖励情况", "出版地" = EXCLUDED."出版地", "出版社级别" = EXCLUDED."出版社级别", "著作类型" = EXCLUDED."著作类型", "是否译为外文" = EXCLUDED."是否译为外文", "语种" = EXCLUDED."语种", "学科门类" = EXCLUDED."学科门类", "一级学科" = EXCLUDED."一级学科", "二级学科" = EXCLUDED."二级学科", "三级学科" = EXCLUDED."三级学科", "作者人数" = EXCLUDED."作者人数", "发行价" = EXCLUDED."发行价", "备注" = EXCLUDED."备注", "封面相关文件路径" = EXCLUDED."封面相关文件路径", "版权页相关文件路径" = EXCLUDED."版权页相关文件路径", "前言页相关文件路径" = EXCLUDED."前言页相关文件路径", "完整目录页相关文件路径" = EXCLUDED."完整目录页相关文件路径", "其他相关文件路径" = EXCLUDED."其他相关文件路径";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_纵向项目变更_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "纵向项目变更" ("编号", "变更内容简介", "变更事由", "变更备注", "变更时间", "纵向项目编号", "分类", "项目登记情况", "项目编号", "项目中文名称", "项目英文名称", "项目分类", "成果归属部门编号", "负责人编号", "立项年度", "登记日期", "登记人编号", "批准日期", "批准经费", "配套经费", "经费卡号", "项目等级", "计划开始日期", "计划完成日期", "学科门类", "学科类型", "研究类型", "项目类型", "项目级别", "项目来源单位", "项目类别", "档案编号", "财务简称", "亚类说明", "附注说明", "纵向项目合同类型", "社会经济目标", "是否需要伦理评审", "是否需要伦理批文", "伦理批文编号", "伦理批文时间", "伦理批文路径", "立项文件路径", "备注", "建立时间")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"纵向项目变更"', '编号')) ELSE r."编号" END, r."变更内容简介", r."变更事由", r."变更备注", r."变更时间", r."纵向项目编号", r."分类", r."项目登记情况", r."项目编号", r."项目中文名称", r."项目英文名称", r."项目分类", r."成果归属部门编号", r."负责人编号", r."立项年度", r."登记日期", r."登记人编号", r."批准日期", r."批准经费", r."配套经费", r."经费卡号", r."项目等级", r."计划开始日期", r."计划完成日期", r."学科门类", r."学科类型", r."研究类型", r."项目类型", r."项目级别", r."项目来源单位", r."项目类别", r."档案编号", r."财务简称", r."亚类说明", r."附注说明", r."纵向项目合同类型", r."社会经济目标", r."是否需要伦理评审", r."是否需要伦理批文", r."伦理批文编号", r."伦理批文时间", r."伦理批文路径", r."立项文件路径", r."备注", r."建立时间"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "变更内容简介" varchar(1000), "变更事由" varchar(1000), "变更备注" varchar(1000), "变更时间" timestamp without time zone, "纵向项目编号" integer, "分类" integer, "项目登记情况" varchar(50), "项目编号" varchar(50), "项目中文名称" varchar(100), "项目英文名称" varchar(100), "项目分类" varchar(50), "成果归属部门编号" integer, "负责人编号" integer, "立项年度" integer, "登记日期" date, "登记人编号" integer, "批准日期" date, "批准经费" numeric(18,2), "配套经费" numeric(18,2), "经费卡号" varchar(50), "项目等级" varchar(50), "计划开始日期" date, "计划完成日期" date, "学科门类" varchar(50), "学科类型" varchar(50), "研究类型" varchar(50), "项目类型" varchar(50), "项目级别" varchar(50), "项目来源单位" varchar(50), "项目类别" varchar(50), "档案编号" varchar(50), "财务简称" varchar(100), "亚类说明" varchar(500), "附注说明" varchar(500), "纵向项目合同类型" varchar(50), "社会经济目标" varchar(1000), "是否需要伦理评审" boolean, "是否需要伦理批文" boolean, "伦理批文编号" varchar(50), "伦理批文时间" timestamp without time zone, "伦理批文路径" text, "立项文件路径" text, "备注" varchar(500), "建立时间" timestamp without time zone)
    ON CONFLICT ("编号") DO UPDATE SET "变更内容简介" = EXCLUDED."变更内容简介", "变更事由" = EXCLUDED."变更事由", "变更备注" = EXCLUDED."变更备注", "变更时间" = EXCLUDED."变更时间", "纵向项目编号" = EXCLUDED."纵向项目编号", "分类" = EXCLUDED."分类", "项目登记情况" = EXCLUDED."项目登记情况", "项目编号" = EXCLUDED."项目编号", "项目中文名称" = EXCLUDED."项目中文名称", "项目英文名称" = EXCLUDED."项目英文名称", "项目分类" = EXCLUDED."项目分类", "成果归属部门编号" = EXCLUDED."成果归属部门编号", "负责人编号" = EXCLUDED."负责人编号", "立项年度" = EXCLUDED."立项年度", "登记日期" = EXCLUDED."登记日期", "登记人编号" = EXCLUDED."登记人编号", "批准日期" = EXCLUDED."批准日期", "批准经费" = EXCLUDED."批准经费", "配套经费" = EXCLUDED."配套经费", "经费卡号" = EXCLUDED."经费卡号", "项目等级" = EXCLUDED."项目等级", "计划开始日期" = EXCLUDED."计划开始日期", "计划完成日期" = EXCLUDED."计划完成日期", "学科门类" = EXCLUDED."学科门类", "学科类型" = EXCLUDED."学科类型", "研究类型" = EXCLUDED."研究类型", "项目类型" = EXCLUDED."项目类型", "项目级别" = EXCLUDED."项目级别", "项目来源单位" = EXCLUDED."项目来源单位", "项目类别" = EXCLUDED."项目类别", "档案编号" = EXCLUDED."档案编号", "财务简称" = EXCLUDED."财务简称", "亚类说明" = EXCLUDED."亚类说明", "附注说明" = EXCLUDED."附注说明", "纵向项目合同类型" = EXCLUDED."纵向项目合同类型", "社会经济目标" = EXCLUDED."社会经济目标", "是否需要伦理评审" = EXCLUDED."是否需要伦理评审", "是否需要伦理批文" = EXCLUDED."是否需要伦理批文", "伦理批文编号" = EXCLUDED."伦理批文编号", "伦理批文时间" = EXCLUDED."伦理批文时间", "伦理批文路径" = EXCLUDED."伦理批文路径", "立项文件路径" = EXCLUDED."立项文件路径", "备注" = EXCLUDED."备注", "建立时间" = EXCLUDED."建立时间";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_成果专利_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "成果专利" ("编号", "专利名称", "专利国别", "专利类型", "专利范围", "是否授权", "代理机构", "年度", "专利权人名称", "申请号", "申请日期", "公开号", "公开日期", "授权机构", "专利号", "国际专利主分类号", "授权公告日", "证书号", "是否职务发明", "是否生效", "发明人数", "奖励金额", "超额费用", "超额费用来源", "专利摘要", "备注", "相关文件路径", "代理费金额", "代理费发票号", "代理费付款日期", "代理资助金额", "代理办理资助日期", "代理费到账日期", "申请费是否减缓", "申请费金额", "申请费收据号", "申请费支付日期", "申请费资助金额", "申请费办理资助日期", "申请费到账日期", "申请审查费是否延缓", "申请审查费用", "申请审查费收据编号", "申请审查费付款日期", "申请审查费资助金额", "申请审查费资助日期", "申请审查费到账日期", "授权费是否减缓", "授权费用金额", "授权费收据号", "授权费付款日期", "授权费资助金额", "授权费办理资助日期", "授权费到账日期")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"成果专利"', '编号')) ELSE r."编号" END, r."专利名称", r."专利国别", r."专利类型", r."专利范围", r."是否授权", r."代理机构", r."年度", r."专利权人名称", r."申请号", r."申请日期", r."公开号", r."公开日期", r."授权机构", r."专利号", r."国际专利主分类号", r."授权公告日", r."证书号", r."是否职务发明", r."是否生效", r."发明人数", r."奖励金额", r."超额费用", r."超额费用来源", r."专利摘要", r."备注", r."相关文件路径", r."代理费金额", r."代理费发票号", r."代理费付款日期", r."代理资助金额", r."代理办理资助日期", r."代理费到账日期", r."申请费是否减缓", r."申请费金额", r."申请费收据号", r."申请费支付日期", r."申请费资助金额", r."申请费办理资助日期", r."申请费到账日期", r."申请审查费是否延缓", r."申请审查费用", r."申请审查费收据编号", r."申请审查费付款日期", r."申请审查费资助金额", r."申请审查费资助日期", r."申请审查费到账日期", r."授权费是否减缓", r."授权费用金额", r."授权费收据号", r."授权费付款日期", r."授权费资助金额", r."授权费办理资助日期", r."授权费到账日期"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "专利名称" varchar(100), "专利国别" varchar(50), "专利类型" varchar(50), "专利范围" varchar(50), "是否授权" boolean, "代理机构" varchar(500), "年度" integer, "专利权人名称" varchar(50), "申请号" varchar(50), "申请日期" date, "公开号" varchar(50), "公开日期" date, "授权机构" varchar(100), "专利号" varchar(100), "国际专利主分类号" varchar(100), "授权公告日" date, "证书号" varchar(100), "是否职务发明" boolean, "是否生效" boolean, "发明人数" integer, "奖励金额" numeric(18,2), "超额费用" numeric(18,2), "超额费用来源" varchar(100), "专利摘要" varchar(500), "备注" varchar(500), "相关文件路径" text, "代理费金额" numeric(18,2), "代理费发票号" varchar(100), "代理费付款日期" date, "代理资助金额" numeric(18,2), "代理办理资助日期" date, "代理费到账日期" date, "申请费是否减缓" boolean, "申请费金额" numeric(18,2), "申请费收据号" varchar(100), "申请费支付日期" date, "申请费资助金额" numeric(18,2), "申请费办理资助日期" date, "申请费到账日期" date, "申请审查费是否延缓" boolean, "申请审查费用" numeric(18,2), "申请审查费收据编号" varchar(100), "申请审查费付款日期" date, "申请审查费资助金额" numeric(18,2), "申请审查费资助日期" date, "申请审查费到账日期" date, "授权费是否减缓" boolean, "授权费用金额" numeric(18,2), "授权费收据号" varchar(100), "授权费付款日期" date, "授权费资助金额" numeric(18,2), "授权费办理资助日期" date, "授权费到账日期" date)
    ON CONFLICT ("编号") DO UPDATE SET "专利名称" = EXCLUDED."专利名称", "专利国别" = EXCLUDED."专利国别", "专利类型" = EXCLUDED."专利类型", "专利范围" = EXCLUDED."专利范围", "是否授权" = EXCLUDED."是否授权", "代理机构" = EXCLUDED."代理机构", "年度" = EXCLUDED."年度", "专利权人名称" = EXCLUDED."专利权人名称", "申请号" = EXCLUDED."申请号", "申请日期" = EXCLUDED."申请日期", "公开号" = EXCLUDED."公开号", "公开日期" = EXCLUDED."公开日期", "授权机构" = EXCLUDED."授权机构", "专利号" = EXCLUDED."专利号", "国际专利主分类号" = EXCLUDED."国际专利主分类号", "授权公告日" = EXCLUDED."授权公告日", "证书号" = EXCLUDED."证书号", "是否职务发明" = EXCLUDED."是否职务发明", "是否生效" = EXCLUDED."是否生效", "发明人数" = EXCLUDED."发明人数", "奖励金额" = EXCLUDED."奖励金额", "超额费用" = EXCLUDED."超额费用", "超额费用来源" = EXCLUDED."超额费用来源", "专利摘要" = EXCLUDED."专利摘要", "备注" = EXCLUDED."备注", "相关文件路径" = EXCLUDED."相关文件路径", "代理费金额" = EXCLUDED."代理费金额", "代理费发票号" = EXCLUDED."代理费发票号", "代理费付款日期" = EXCLUDED."代理费付款日期", "代理资助金额" = EXCLUDED."代理资助金额", "代理办理资助日期" = EXCLUDED."代理办理资助日期", "代理费到账日期" = EXCLUDED."代理费到账日期", "申请费是否减缓" = EXCLUDED."申请费是否减缓", "申请费金额" = EXCLUDED."申请费金额", "申请费收据号" = EXCLUDED."申请费收据号", "申请费支付日期" = EXCLUDED."申请费支付日期", "申请费资助金额" = EXCLUDED."申请费资助金额", "申请费办理资助日期" = EXCLUDED."申请费办理资助日期", "申请费到账日期" = EXCLUDED."申请费到账日期", "申请审查费是否延缓" = EXCLUDED."申请审查费是否延缓", "申请审查费用" = EXCLUDED."申请审查费用", "申请审查费收据编号" = EXCLUDED."申请审查费收据编号", "申请审查费付款日期" = EXCLUDED."申请审查费付款日期", "申请审查费资助金额" = EXCLUDED."申请审查费资助金额", "申请审查费资助日期" = EXCLUDED."申请审查费资助日期", "申请审查费到账日期" = EXCLUDED."申请审查费到账日期", "授权费是否减缓" = EXCLUDED."授权费是否减缓", "授权费用金额" = EXCLUDED."授权费用金额", "授权费收据号" = EXCLUDED."授权费收据号", "授权费付款日期" = EXCLUDED."授权费付款日期", "授权费资助金额" = EXCLUDED."授权费资助金额", "授权费办理资助日期" = EXCLUDED."授权费办理资助日期", "授权费到账日期" = EXCLUDED."授权费到账日期";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_国民行业代码_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "国民行业代码" ("编号", "上级编号", "名称", "代码", "级别", "排序", "备注")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"国民行业代码"', '编号')) ELSE r."编号" END, r."上级编号", r."名称", r."代码", r."级别", r."排序", r."备注"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "上级编号" integer, "名称" varchar(50), "代码" varchar(500), "级别" integer, "排序" integer, "备注" integer)
    ON CONFLICT ("编号") DO UPDATE SET "上级编号" = EXCLUDED."上级编号", "名称" = EXCLUDED."名称", "代码" = EXCLUDED."代码", "级别" = EXCLUDED."级别", "排序" = EXCLUDED."排序", "备注" = EXCLUDED."备注";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_成果获奖_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "成果获奖" ("编号", "获奖名称", "获奖级别", "获奖日期", "获奖类别", "奖励情况", "联系人", "联系人手机", "项目开始时间", "项目结束时间", "应用开始时间", "应用结束时间", "年度", "批文时间", "颁奖单位", "完成人数", "获奖证书编号", "完成单位数量", "获奖摘要", "备注", "相关文件路径", "获奖单位文件路径", "获奖批文")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"成果获奖"', '编号')) ELSE r."编号" END, r."获奖名称", r."获奖级别", r."获奖日期", r."获奖类别", r."奖励情况", r."联系人", r."联系人手机", r."项目开始时间", r."项目结束时间", r."应用开始时间", r."应用结束时间", r."年度", r."批文时间", r."颁奖单位", r."完成人数", r."获奖证书编号", r."完成单位数量", r."获奖摘要", r."备注", r."相关文件路径", r."获奖单位文件路径", r."获奖批文"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "获奖名称" varchar(200), "获奖级别" varchar(50), "获奖日期" date, "获奖类别" varchar(50), "奖励情况" varchar(50), "联系人" varchar(50), "联系人手机" varchar(50), "项目开始时间" timestamp without time zone, "项目结束时间" timestamp without time zone, "应用开始时间" timestamp without time zone, "应用结束时间" timestamp without time zone, "年度" integer, "批文时间" timestamp without time zone, "颁奖单位" varchar(50), "完成人数" integer, "获奖证书编号" varchar(50), "完成单位数量" integer, "获奖摘要" varchar(500), "备注" varchar(500), "相关文件路径" text, "获奖单位文件路径" text, "获奖批文" text)
    ON CONFLICT ("编号") DO UPDATE SET "获奖名称" = EXCLUDED."获奖名称", "获奖级别" = EXCLUDED."获奖级别", "获奖日期" = EXCLUDED."获奖日期", "获奖类别" = EXCLUDED."获奖类别", "奖励情况" = EXCLUDED."奖励情况", "联系人" = EXCLUDED."联系人", "联系人手机" = EXCLUDED."联系人手机", "项目开始时间" = EXCLUDED."项目开始时间", "项目结束时间" = EXCLUDED."项目结束时间", "应用开始时间" = EXCLUDED."应用开始时间", "应用结束时间" = EXCLUDED."应用结束时间", "年度" = EXCLUDED."年度", "批文时间" = EXCLUDED."批文时间", "颁奖单位" = EXCLUDED."颁奖单位", "完成人数" = EXCLUDED."完成人数", "获奖证书编号" = EXCLUDED."获奖证书编号", "完成单位数量" = EXCLUDED."完成单位数量", "获奖摘要" = EXCLUDED."获奖摘要", "备注" = EXCLUDED."备注", "相关文件路径" = EXCLUDED."相关文件路径", "获奖单位文件路径" = EXCLUDED."获奖单位文件路径", "获奖批文" = EXCLUDED."获奖批文";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_横向项目_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "横向项目" ("编号", "项目分类", "项目类型", "合同编号", "合同名称", "是否为第一承建单位", "项目负责人编号", "项目归属部门编号", "单位名称", "邮政编码", "法定代表人", "经费本编号", "联系人", "联系人电话", "通讯地址", "是否需要伦理评审", "档案编号", "财务简称", "课题来源", "合作方关系", "合作方契约关系", "合作方单位名称", "合作方法人或组织机构代码", "合作方法定代表人", "合作方单位联系人", "合作方联系电话", "合作方邮箱", "合作方邮政编码", "合作方所在省", "合作方所在市", "合作方所在地区", "合同金额", "签订时间", "付款方式", "合同开始时间", "合同结束时间", "合作方式", "研究类型", "技术领域", "合同类型", "技术合同类型", "是否申请技术合同认定", "合同技术交易额", "知识产权", "计划来源", "合作方所属国民经济行业", "技术服务的国民经济行业", "合同的社会经济目标", "合作方性质", "是否高新性质", "合同认定登记承诺书路径", "合同文件路径", "备注", "建立时间")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"横向项目"', '编号')) ELSE r."编号" END, r."项目分类", r."项目类型", r."合同编号", r."合同名称", r."是否为第一承建单位", r."项目负责人编号", r."项目归属部门编号", r."单位名称", r."邮政编码", r."法定代表人", r."经费本编号", r."联系人", r."联系人电话", r."通讯地址", r."是否需要伦理评审", r."档案编号", r."财务简称", r."课题来源", r."合作方关系", r."合作方契约关系", r."合作方单位名称", r."合作方法人或组织机构代码", r."合作方法定代表人", r."合作方单位联系人", r."合作方联系电话", r."合作方邮箱", r."合作方邮政编码", r."合作方所在省", r."合作方所在市", r."合作方所在地区", r."合同金额", r."签订时间", r."付款方式", r."合同开始时间", r."合同结束时间", r."合作方式", r."研究类型", r."技术领域", r."合同类型", r."技术合同类型", r."是否申请技术合同认定", r."合同技术交易额", r."知识产权", r."计划来源", r."合作方所属国民经济行业", r."技术服务的国民经济行业", r."合同的社会经济目标", r."合作方性质", r."是否高新性质", r."合同认定登记承诺书路径", r."合同文件路径", r."备注", r."建立时间"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "项目分类" varchar(50), "项目类型" varchar(50), "合同编号" varchar(50), "合同名称" varchar(100), "是否为第一承建单位" boolean, "项目负责人编号" integer, "项目归属部门编号" integer, "单位名称" varchar(100), "邮政编码" varchar(50), "法定代表人" varchar(50), "经费本编号" varchar(50), "联系人" varchar(50), "联系人电话" varchar(50), "通讯地址" varchar(200), "是否需要伦理评审" boolean, "档案编号" varchar(50), "财务简称" varchar(50), "课题来源" varchar(50), "合作方关系" varchar(50), "合作方契约关系" varchar(50), "合作方单位名称" varchar(200), "合作方法人或组织机构代码" varchar(50), "合作方法定代表人" varchar(50), "合作方单位联系人" varchar(50), "合作方联系电话" varchar(50), "合作方邮箱" varchar(50), "合作方邮政编码" varchar(50), "合作方所在省" varchar(50), "合作方所在市" varchar(50), "合作方所在地区" varchar(50), "合同金额" numeric(18,2), "签订时间" timestamp without time zone, "付款方式" varchar(50), "合同开始时间" timestamp without time zone, "合同结束时间" timestamp without time zone, "合作方式" varchar(50), "研究类型" varchar(50), "技术领域" varchar(50), "合同类型" varchar(50), "技术合同类型" varchar(50), "是否申请技术合同认定" boolean, "合同技术交易额" numeric(18,2), "知识产权" varchar(50), "计划来源" varchar(50), "合作方所属国民经济行业" varchar(50), "技术服务的国民经济行业" varchar(50), "合同的社会经济目标" varchar(50), "合作方性质" varchar(50), "是否高新性质" boolean, "合同认定登记承诺书路径" text, "合同文件路径" text, "备注" varchar(500), "建立时间" timestamp without time zone)
    ON CONFLICT ("编号") DO UPDATE SET "项目分类" = EXCLUDED."项目分类", "项目类型" = EXCLUDED."项目类型", "合同编号" = EXCLUDED."合同编号", "合同名称" = EXCLUDED."合同名称", "是否为第一承建单位" = EXCLUDED."是否为第一承建单位", "项目负责人编号" = EXCLUDED."项目负责人编号", "项目归属部门编号" = EXCLUDED."项目归属部门编号", "单位名称" = EXCLUDED."单位名称", "邮政编码" = EXCLUDED."邮政编码", "法定代表人" = EXCLUDED."法定代表人", "经费本编号" = EXCLUDED."经费本编号", "联系人" = EXCLUDED."联系人", "联系人电话" = EXCLUDED."联系人电话", "通讯地址" = EXCLUDED."通讯地址", "是否需要伦理评审" = EXCLUDED."是否需要伦理评审", "档案编号" = EXCLUDED."档案编号", "财务简称" = EXCLUDED."财务简称", "课题来源" = EXCLUDED."课题来源", "合作方关系" = EXCLUDED."合作方关系", "合作方契约关系" = EXCLUDED."合作方契约关系", "合作方单位名称" = EXCLUDED."合作方单位名称", "合作方法人或组织机构代码" = EXCLUDED."合作方法人或组织机构代码", "合作方法定代表人" = EXCLUDED."合作方法定代表人", "合作方单位联系人" = EXCLUDED."合作方单位联系人", "合作方联系电话" = EXCLUDED."合作方联系电话", "合作方邮箱" = EXCLUDED."合作方邮箱", "合作方邮政编码" = EXCLUDED."合作方邮政编码", "合作方所在省" = EXCLUDED."合作方所在省", "合作方所在市" = EXCLUDED."合作方所在市", "合作方所在地区" = EXCLUDED."合作方所在地区", "合同金额" = EXCLUDED."合同金额", "签订时间" = EXCLUDED."签订时间", "付款方式" = EXCLUDED."付款方式", "合同开始时间" = EXCLUDED."合同开始时间", "合同结束时间" = EXCLUDED."合同结束时间", "合作方式" = EXCLUDED."合作方式", "研究类型" = EXCLUDED."研究类型", "技术领域" = EXCLUDED."技术领域", "合同类型" = EXCLUDED."合同类型", "技术合同类型" = EXCLUDED."技术合同类型", "是否申请技术合同认定" = EXCLUDED."是否申请技术合同认定", "合同技术交易额" = EXCLUDED."合同技术交易额", "知识产权" = EXCLUDED."知识产权", "计划来源" = EXCLUDED."计划来源", "合作方所属国民经济行业" = EXCLUDED."合作方所属国民经济行业", "技术服务的国民经济行业" = EXCLUDED."技术服务的国民经济行业", "合同的社会经济目标" = EXCLUDED."合同的社会经济目标", "合作方性质" = EXCLUDED."合作方性质", "是否高新性质" = EXCLUDED."是否高新性质", "合同认定登记承诺书路径" = EXCLUDED."合同认定登记承诺书路径", "合同文件路径" = EXCLUDED."合同文件路径", "备注" = EXCLUDED."备注", "建立时间" = EXCLUDED."建立时间";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_国民行业代码_删"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    DELETE FROM "国民行业代码" AS t
    WHERE t."编号" IN (SELECT (r."编号") FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "上级编号" integer, "名称" varchar(50), "代码" varchar(500), "级别" integer, "排序" integer, "备注" integer));
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_合同买方性质_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "合同买方性质" ("编号", "上级编号", "名称", "级别", "排序", "备注")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"合同买方性质"', '编号')) ELSE r."编号" END, r."上级编号", r."名称", r."级别", r."排序", r."备注"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "上级编号" integer, "名称" varchar(50), "级别" integer, "排序" integer, "备注" varchar(500))
    ON CONFLICT ("编号") DO UPDATE SET "上级编号" = EXCLUDED."上级编号", "名称" = EXCLUDED."名称", "级别" = EXCLUDED."级别", "排序" = EXCLUDED."排序", "备注" = EXCLUDED."备注";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_合同买方性质_删"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    DELETE FROM "合同买方性质" AS t
    WHERE t."编号" IN (SELECT (r."编号") FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "上级编号" integer, "名称" varchar(50), "级别" integer, "排序" integer, "备注" varchar(500)));
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_横向项目变更_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "横向项目变更" ("编号", "变更内容简介", "变更事由", "变更备注", "变更时间", "横向项目编号", "项目分类", "项目类型", "合同编号", "合同名称", "是否为第一承建单位", "项目负责人编号", "项目归属部门编号", "单位名称", "邮政编码", "法定代表人", "经费本编号", "联系人", "联系人电话", "通讯地址", "是否需要伦理评审", "档案编号", "财务简称", "课题来源", "合作方关系", "合作方契约关系", "合作方单位名称", "合作方法人或组织机构代码", "合作方法定代表人", "合作方单位联系人", "合作方联系电话", "合作方邮箱", "合作方邮政编码", "合作方所在省", "合作方所在市", "合作方所在地区", "合同金额", "签订时间", "付款方式", "合同开始时间", "合同结束时间", "合作方式", "研究类型", "技术领域", "合同类型", "技术合同类型", "是否申请技术合同认定", "合同技术交易额", "知识产权", "计划来源", "合作方所属国民经济行业", "技术服务的国民经济行业", "合同的社会经济目标", "合作方性质", "是否高新性质", "合同认定登记承诺书路径", "合同文件路径", "备注", "建立时间")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"横向项目变更"', '编号')) ELSE r."编号" END, r."变更内容简介", r."变更事由", r."变更备注", r."变更时间", r."横向项目编号", r."项目分类", r."项目类型", r."合同编号", r."合同名称", r."是否为第一承建单位", r."项目负责人编号", r."项目归属部门编号", r."单位名称", r."邮政编码", r."法定代表人", r."经费本编号", r."联系人", r."联系人电话", r."通讯地址", r."是否需要伦理评审", r."档案编号", r."财务简称", r."课题来源", r."合作方关系", r."合作方契约关系", r."合作方单位名称", r."合作方法人或组织机构代码", r."合作方法定代表人", r."合作方单位联系人", r."合作方联系电话", r."合作方邮箱", r."合作方邮政编码", r."合作方所在省", r."合作方所在市", r."合作方所在地区", r."合同金额", r."签订时间", r."付款方式", r."合同开始时间", r."合同结束时间", r."合作方式", r."研究类型", r."技术领域", r."合同类型", r."技术合同类型", r."是否申请技术合同认定", r."合同技术交易额", r."知识产权", r."计划来源", r."合作方所属国民经济行业", r."技术服务的国民经济行业", r."合同的社会经济目标", r."合作方性质", r."是否高新性质", r."合同认定登记承诺书路径", r."合同文件路径", r."备注", r."建立时间"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "变更内容简介" varchar(1000), "变更事由" varchar(1000), "变更备注" varchar(1000), "变更时间" timestamp without time zone, "横向项目编号" integer, "项目分类" varchar(50), "项目类型" varchar(50), "合同编号" varchar(50), "合同名称" varchar(100), "是否为第一承建单位" boolean, "项目负责人编号" integer, "项目归属部门编号" integer, "单位名称" varchar(100), "邮政编码" varchar(50), "法定代表人" varchar(50), "经费本编号" varchar(50), "联系人" varchar(50), "联系人电话" varchar(50), "通讯地址" varchar(200), "是否需要伦理评审" boolean, "档案编号" varchar(50), "财务简称" varchar(50), "课题来源" varchar(50), "合作方关系" varchar(50), "合作方契约关系" varchar(50), "合作方单位名称" varchar(200), "合作方法人或组织机构代码" varchar(50), "合作方法定代表人" varchar(50), "合作方单位联系人" varchar(50), "合作方联系电话" varchar(50), "合作方邮箱" varchar(50), "合作方邮政编码" varchar(50), "合作方所在省" varchar(50), "合作方所在市" varchar(50), "合作方所在地区" varchar(50), "合同金额" numeric(18,2), "签订时间" timestamp without time zone, "付款方式" varchar(50), "合同开始时间" timestamp without time zone, "合同结束时间" timestamp without time zone, "合作方式" varchar(50), "研究类型" varchar(50), "技术领域" varchar(50), "合同类型" varchar(50), "技术合同类型" varchar(50), "是否申请技术合同认定" boolean, "合同技术交易额" numeric(18,2), "知识产权" varchar(50), "计划来源" varchar(50), "合作方所属国民经济行业" varchar(50), "技术服务的国民经济行业" varchar(50), "合同的社会经济目标" varchar(50), "合作方性质" varchar(50), "是否高新性质" boolean, "合同认定登记承诺书路径" text, "合同文件路径" text, "备注" varchar(500), "建立时间" timestamp without time zone)
    ON CONFLICT ("编号") DO UPDATE SET "变更内容简介" = EXCLUDED."变更内容简介", "变更事由" = EXCLUDED."变更事由", "变更备注" = EXCLUDED."变更备注", "变更时间" = EXCLUDED."变更时间", "横向项目编号" = EXCLUDED."横向项目编号", "项目分类" = EXCLUDED."项目分类", "项目类型" = EXCLUDED."项目类型", "合同编号" = EXCLUDED."合同编号", "合同名称" = EXCLUDED."合同名称", "是否为第一承建单位" = EXCLUDED."是否为第一承建单位", "项目负责人编号" = EXCLUDED."项目负责人编号", "项目归属部门编号" = EXCLUDED."项目归属部门编号", "单位名称" = EXCLUDED."单位名称", "邮政编码" = EXCLUDED."邮政编码", "法定代表人" = EXCLUDED."法定代表人", "经费本编号" = EXCLUDED."经费本编号", "联系人" = EXCLUDED."联系人", "联系人电话" = EXCLUDED."联系人电话", "通讯地址" = EXCLUDED."通讯地址", "是否需要伦理评审" = EXCLUDED."是否需要伦理评审", "档案编号" = EXCLUDED."档案编号", "财务简称" = EXCLUDED."财务简称", "课题来源" = EXCLUDED."课题来源", "合作方关系" = EXCLUDED."合作方关系", "合作方契约关系" = EXCLUDED."合作方契约关系", "合作方单位名称" = EXCLUDED."合作方单位名称", "合作方法人或组织机构代码" = EXCLUDED."合作方法人或组织机构代码", "合作方法定代表人" = EXCLUDED."合作方法定代表人", "合作方单位联系人" = EXCLUDED."合作方单位联系人", "合作方联系电话" = EXCLUDED."合作方联系电话", "合作方邮箱" = EXCLUDED."合作方邮箱", "合作方邮政编码" = EXCLUDED."合作方邮政编码", "合作方所在省" = EXCLUDED."合作方所在省", "合作方所在市" = EXCLUDED."合作方所在市", "合作方所在地区" = EXCLUDED."合作方所在地区", "合同金额" = EXCLUDED."合同金额", "签订时间" = EXCLUDED."签订时间", "付款方式" = EXCLUDED."付款方式", "合同开始时间" = EXCLUDED."合同开始时间", "合同结束时间" = EXCLUDED."合同结束时间", "合作方式" = EXCLUDED."合作方式", "研究类型" = EXCLUDED."研究类型", "技术领域" = EXCLUDED."技术领域", "合同类型" = EXCLUDED."合同类型", "技术合同类型" = EXCLUDED."技术合同类型", "是否申请技术合同认定" = EXCLUDED."是否申请技术合同认定", "合同技术交易额" = EXCLUDED."合同技术交易额", "知识产权" = EXCLUDED."知识产权", "计划来源" = EXCLUDED."计划来源", "合作方所属国民经济行业" = EXCLUDED."合作方所属国民经济行业", "技术服务的国民经济行业" = EXCLUDED."技术服务的国民经济行业", "合同的社会经济目标" = EXCLUDED."合同的社会经济目标", "合作方性质" = EXCLUDED."合作方性质", "是否高新性质" = EXCLUDED."是否高新性质", "合同认定登记承诺书路径" = EXCLUDED."合同认定登记承诺书路径", "合同文件路径" = EXCLUDED."合同文件路径", "备注" = EXCLUDED."备注", "建立时间" = EXCLUDED."建立时间";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_技术领域_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "技术领域" ("编号", "上级编号", "名称", "级别", "排序", "备注")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"技术领域"', '编号')) ELSE r."编号" END, r."上级编号", r."名称", r."级别", r."排序", r."备注"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "上级编号" integer, "名称" varchar(50), "级别" integer, "排序" integer, "备注" integer)
    ON CONFLICT ("编号") DO UPDATE SET "上级编号" = EXCLUDED."上级编号", "名称" = EXCLUDED."名称", "级别" = EXCLUDED."级别", "排序" = EXCLUDED."排序", "备注" = EXCLUDED."备注";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_技术领域_删"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    DELETE FROM "技术领域" AS t
    WHERE t."编号" IN (SELECT (r."编号") FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "上级编号" integer, "名称" varchar(50), "级别" integer, "排序" integer, "备注" integer));
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_学科_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "学科" ("编号", "上级编号", "名称", "级别", "排序", "备注")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"学科"', '编号')) ELSE r."编号" END, r."上级编号", r."名称", r."级别", r."排序", r."备注"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "上级编号" integer, "名称" varchar(50), "级别" integer, "排序" integer, "备注" varchar(500));
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_横向项目结题_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "横向项目结题" ("编号", "横向项目编号", "结题验收编号", "结题验收时间", "结题验收是否合格", "结题验收参与人数", "结题资料路径", "备注", "建立时间")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"横向项目结题"', '编号')) ELSE r."编号" END, r."横向项目编号", r."结题验收编号", r."结题验收时间", r."结题验收是否合格", r."结题验收参与人数", r."结题资料路径", r."备注", r."建立时间"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "横向项目编号" integer, "结题验收编号" varchar(100), "结题验收时间" timestamp without time zone, "结题验收是否合格" boolean, "结题验收参与人数" integer, "结题资料路径" varchar(1000), "备注" varchar(1000), "建立时间" timestamp without time zone)
    ON CONFLICT ("编号") DO UPDATE SET "横向项目编号" = EXCLUDED."横向项目编号", "结题验收编号" = EXCLUDED."结题验收编号", "结题验收时间" = EXCLUDED."结题验收时间", "结题验收是否合格" = EXCLUDED."结题验收是否合格", "结题验收参与人数" = EXCLUDED."结题验收参与人数", "结题资料路径" = EXCLUDED."结题资料路径", "备注" = EXCLUDED."备注", "建立时间" = EXCLUDED."建立时间";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_项目分类_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "项目分类" ("编号", "上级编号", "名称", "级别", "排序", "备注")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"项目分类"', '编号')) ELSE r."编号" END, r."上级编号", r."名称", r."级别", r."排序", r."备注"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "上级编号" integer, "名称" varchar(50), "级别" integer, "排序" integer, "备注" varchar(500))
    ON CONFLICT ("编号") DO UPDATE SET "上级编号" = EXCLUDED."上级编号", "名称" = EXCLUDED."名称", "级别" = EXCLUDED."级别", "排序" = EXCLUDED."排序", "备注" = EXCLUDED."备注";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_项目分类_删"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    DELETE FROM "项目分类" AS t
    WHERE t."编号" IN (SELECT (r."编号") FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "上级编号" integer, "名称" varchar(50), "级别" integer, "排序" integer, "备注" varchar(500)));
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_区域_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "区域" ("编号", "名称", "级别", "上级编号", "排序", "备注")
    SELECT CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"区域"', '编号')) ELSE r."编号" END, r."名称", r."级别", r."上级编号", r."排序", r."备注"
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "名称" varchar(500), "级别" integer, "上级编号" integer, "排序" integer, "备注" varchar(500))
    ON CONFLICT ("编号") DO UPDATE SET "名称" = EXCLUDED."名称", "级别" = EXCLUDED."级别", "上级编号" = EXCLUDED."上级编号", "排序" = EXCLUDED."排序", "备注" = EXCLUDED."备注";
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_区域_删"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    DELETE FROM "区域" AS t
    WHERE t."编号" IN (SELECT (r."编号") FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "名称" varchar(500), "级别" integer, "上级编号" integer, "排序" integer, "备注" varchar(500)));
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_通知公告_启禁"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    UPDATE "通知公告" SET "是否启用" = COALESCE((p_payload->>'是否启用')::boolean, FALSE)
    WHERE "编号" = (p_payload->>'编号')::integer;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_主办讲座_删"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    DELETE FROM "主办讲座" AS t
    WHERE t."编号" IN (SELECT (r."编号") FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r("编号" integer, "活动名称" varchar(500), "所属部门编号" integer, "活动经费" numeric(18,2), "会议类型" varchar(50), "活动对象" varchar(500), "开始时间" timestamp without time zone, "结束时间" timestamp without time zone, "主办人编号" integer, "主讲人姓名" varchar(50), "主讲人国别" varchar(50), "主讲人单位" varchar(50), "主讲人学位" varchar(50), "主讲人职称" varchar(50), "主讲人简历路径" text, "讲座地点" varchar(500), "内容概述" varchar(500)));
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

COMMIT;
