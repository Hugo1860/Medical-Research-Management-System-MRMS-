-- Vertical project change and closeout queries with workflow state.
BEGIN;

CREATE OR REPLACE FUNCTION "tfn_纵向项目变更"(
    p_employee_id integer,
    p_approval_template_id integer,
    p_change_template_id integer
)
RETURNS TABLE (
    "编号" integer, "变更内容简介" varchar, "变更事由" varchar, "变更备注" varchar,
    "变更时间" timestamp, "纵向项目立项编号" integer, "分类" integer, "分类名称" text,
    "项目中文名称" varchar, "项目英文名称" varchar, "项目类型" varchar, "项目级别" varchar,
    "项目来源单位" varchar, "项目类别" varchar, "批准经费" numeric, "配套经费" numeric,
    "计划开始日期" date, "计划完成日期" date, "成果归属部门名称" varchar, "负责人编号" integer,
    "负责人工号" varchar, "负责人姓名" varchar, "负责人部门编号" integer, "负责人部门名称" varchar,
    "立项登记人编号" integer, "立项登记人姓名" varchar, "审核进度" text, "流程编号" integer,
    "流程模板编号" integer, "流程名称" varchar, "流程相关项目路径" varchar, "项目编号" integer,
    "发起人编号" integer, "发起人工号" varchar, "发起人姓名" varchar, "发起人部门名称" varchar,
    "状态" integer, "状态名称" text, "流程创建时间" timestamp, "步骤编号" integer,
    "步骤名称" varchar, "步骤状态" integer, "步骤链接名称" varchar, "步骤链接路径" varchar,
    "步骤链接样式" varchar, "步骤操作人编号" integer, "步骤操作人工号" varchar,
    "步骤操作人姓名" varchar, "步骤操作人部门名称" varchar, "步骤执行时间" timestamp,
    "步骤状态说明" varchar, "是否可处理" boolean, "是否可发起变更" integer
)
LANGUAGE sql STABLE AS $$
    SELECT b."编号", b."变更内容简介", b."变更事由", b."变更备注", b."变更时间", a."编号",
           a."分类", a."分类名称", a."项目中文名称", a."项目英文名称", a."项目类型", a."项目级别",
           a."项目来源单位", a."项目类别", a."批准经费", a."配套经费", a."计划开始日期",
           a."计划完成日期", a."成果归属部门名称", a."负责人编号", a."负责人工号", a."负责人姓名",
           a."负责人部门编号", a."负责人部门名称", a."登记人编号", a."登记人姓名",
           CASE WHEN c."状态" IS NULL THEN '未发起变更'::text
                ELSE concat(c."状态名称", '-', c."步骤状态说明") END,
           c."流程编号", c."流程模板编号", c."流程名称", c."流程相关项目路径", c."项目编号",
           c."发起人编号", c."发起人工号", c."发起人姓名", c."发起人部门名称", c."状态",
           c."状态名称", c."流程创建时间", c."步骤编号", c."步骤名称", c."步骤状态",
           c."步骤链接名称", c."步骤链接路径", c."步骤链接样式", c."步骤操作人编号",
           c."步骤操作人工号", c."步骤操作人姓名", c."步骤操作人部门名称", c."步骤执行时间",
           c."步骤状态说明", c."是否可处理",
           CASE WHEN count(*) FILTER (WHERE c."状态" IS NOT NULL AND c."状态" <> 1)
                          OVER (PARTITION BY a."编号") = 0
                     AND p_employee_id = a."登记人编号" THEN 1 ELSE 0 END
    FROM "v2_纵向项目立项_列表" AS a
    LEFT JOIN "纵向项目变更" AS b ON b."纵向项目编号" = a."编号"
    LEFT JOIN LATERAL "tfn_流程"(p_change_template_id, b."编号", p_employee_id) AS c ON TRUE
    WHERE EXISTS (
        SELECT 1 FROM "流程" AS f
        WHERE f."流程模板编号" = p_approval_template_id
          AND f."项目编号" = a."编号"
          AND f."状态" = 1
    )
      AND NOT EXISTS (
        SELECT 1 FROM "纵向项目结题" AS closeout
        WHERE closeout."纵向项目编号" = a."编号"
    );
$$;

CREATE OR REPLACE FUNCTION "tfn_纵向项目结题"(
    p_employee_id integer,
    p_approval_template_id integer,
    p_midterm_template_id integer,
    p_change_template_id integer,
    p_closeout_template_id integer
)
RETURNS TABLE (
    "编号" integer, "纵向项目编号" integer, "结题验收编号" varchar, "结题验收时间" timestamp,
    "结题验收是否合格" boolean, "结题验收参与人数" integer, "结题资料路径" varchar,
    "备注" varchar, "建立时间" timestamp, "纵向项目立项编号" integer, "分类" integer,
    "分类名称" text, "项目中文名称" varchar, "项目英文名称" varchar, "项目类型" varchar,
    "项目级别" varchar, "项目来源单位" varchar, "项目类别" varchar, "批准经费" numeric,
    "配套经费" numeric, "计划开始日期" date, "计划完成日期" date, "成果归属部门名称" varchar,
    "负责人编号" integer, "负责人工号" varchar, "负责人姓名" varchar, "负责人部门编号" integer,
    "负责人部门名称" varchar, "立项登记人编号" integer, "立项登记人姓名" varchar,
    "审核进度" text, "流程编号" integer, "流程模板编号" integer, "流程名称" varchar,
    "流程相关项目路径" varchar, "项目编号" integer, "发起人编号" integer, "发起人工号" varchar,
    "发起人姓名" varchar, "发起人部门名称" varchar, "状态" integer, "状态名称" text,
    "流程创建时间" timestamp, "步骤编号" integer, "步骤名称" varchar, "步骤状态" integer,
    "步骤链接名称" varchar, "步骤链接路径" varchar, "步骤链接样式" varchar,
    "步骤操作人编号" integer, "步骤操作人工号" varchar, "步骤操作人姓名" varchar,
    "步骤操作人部门名称" varchar, "步骤执行时间" timestamp, "步骤状态说明" varchar,
    "是否可处理" boolean, "是否可发起结题" integer
)
LANGUAGE sql STABLE AS $$
    SELECT b."编号", b."纵向项目编号", b."结题验收编号", b."结题验收时间", b."结题验收是否合格",
           b."结题验收参与人数", b."结题资料路径", b."备注", b."建立时间", a."编号", a."分类",
           a."分类名称", a."项目中文名称", a."项目英文名称", a."项目类型", a."项目级别",
           a."项目来源单位", a."项目类别", a."批准经费", a."配套经费", a."计划开始日期",
           a."计划完成日期", a."成果归属部门名称", a."负责人编号", a."负责人工号", a."负责人姓名",
           a."负责人部门编号", a."负责人部门名称", a."登记人编号", a."登记人姓名",
           CASE WHEN c."状态" IS NULL THEN '未发起结题'::text
                ELSE concat(c."状态名称", '-', c."步骤状态说明") END,
           c."流程编号", c."流程模板编号", c."流程名称", c."流程相关项目路径", c."项目编号",
           c."发起人编号", c."发起人工号", c."发起人姓名", c."发起人部门名称", c."状态",
           c."状态名称", c."流程创建时间", c."步骤编号", c."步骤名称", c."步骤状态",
           c."步骤链接名称", c."步骤链接路径", c."步骤链接样式", c."步骤操作人编号",
           c."步骤操作人工号", c."步骤操作人姓名", c."步骤操作人部门名称", c."步骤执行时间",
           c."步骤状态说明", c."是否可处理", CASE WHEN c."状态" IS NULL THEN 1 ELSE 0 END
    FROM "v2_纵向项目立项_列表" AS a
    LEFT JOIN "纵向项目结题" AS b ON b."纵向项目编号" = a."编号"
    LEFT JOIN LATERAL "tfn_流程"(p_closeout_template_id, b."编号", p_employee_id) AS c ON TRUE
    WHERE EXISTS (
        SELECT 1 FROM "流程" AS f
        WHERE f."流程模板编号" = p_approval_template_id
          AND f."项目编号" = a."编号"
          AND f."状态" = 1
    )
      AND NOT EXISTS (
        SELECT 1
        FROM "流程" AS f
        JOIN "纵向项目中检" AS m ON m."编号" = f."项目编号"
        WHERE f."流程模板编号" = p_midterm_template_id
          AND m."纵向项目编号" = a."编号"
          AND f."状态" <> 1
    )
      AND NOT EXISTS (
        SELECT 1
        FROM "流程" AS f
        JOIN "纵向项目变更" AS ch ON ch."编号" = f."项目编号"
        WHERE f."流程模板编号" = p_change_template_id
          AND ch."纵向项目编号" = a."编号"
          AND f."状态" <> 1
    );
$$;

COMMIT;
