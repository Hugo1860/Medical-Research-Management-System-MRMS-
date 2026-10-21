-- 修复纵向项目申报存储过程：正确返回自增项目编号并同步关联项目参与人
BEGIN;

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

UPDATE "系统迁移版本"
   SET "版本"=68,"脚本"='001-068',"检查摘要"='修复纵向项目申报存储过程返回值与参与人同步'
 WHERE "编号"=1;

COMMIT;
