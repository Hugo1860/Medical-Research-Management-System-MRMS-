-- Vertical project application and approval lists with workflow progress.
BEGIN;

CREATE OR REPLACE FUNCTION "tfn_纵向项目申报"(p_employee_id integer, p_template_id integer)
RETURNS TABLE (
    "编号" integer, "分类" integer, "分类名称" text, "项目名称" varchar,
    "申报人编号" integer, "工号" varchar, "姓名" varchar, "部门编号" integer, "部门名称" varchar,
    "学科门类" varchar, "项目类型" varchar, "项目级别" varchar, "项目来源单位" varchar,
    "项目类别" varchar, "申报时间" timestamp, "申请经费" numeric, "计划开始日期" date,
    "计划完成日期" date, "是否立项" boolean, "审核进度" text,
    "流程编号" integer, "流程模板编号" integer, "流程名称" varchar, "流程相关项目路径" varchar,
    "项目编号" integer, "发起人编号" integer, "发起人工号" varchar, "发起人姓名" varchar,
    "发起人部门名称" varchar, "状态" integer, "状态名称" text, "流程创建时间" timestamp,
    "步骤编号" integer, "步骤名称" varchar, "步骤状态" integer, "步骤链接名称" varchar,
    "步骤链接路径" varchar, "步骤链接样式" varchar, "步骤操作人编号" integer,
    "步骤操作人工号" varchar, "步骤操作人姓名" varchar, "步骤操作人部门名称" varchar,
    "步骤执行时间" timestamp, "步骤状态说明" varchar, "是否可处理" boolean
)
LANGUAGE sql
STABLE
AS $$
    SELECT a."编号", a."分类", a."分类名称", a."项目名称", a."申报人编号", a."工号", a."姓名",
           a."部门编号", a."部门名称", a."学科门类", a."项目类型", a."项目级别", a."项目来源单位",
           a."项目类别", a."申报时间", a."申请经费", a."计划开始日期", a."计划完成日期", a."是否立项",
           concat(c."状态名称", '-', c."步骤状态说明"), c."流程编号", c."流程模板编号", c."流程名称",
           c."流程相关项目路径", c."项目编号", c."发起人编号", c."发起人工号", c."发起人姓名",
           c."发起人部门名称", c."状态", c."状态名称", c."流程创建时间", c."步骤编号",
           c."步骤名称", c."步骤状态", c."步骤链接名称", c."步骤链接路径", c."步骤链接样式",
           c."步骤操作人编号", c."步骤操作人工号", c."步骤操作人姓名", c."步骤操作人部门名称",
           c."步骤执行时间", c."步骤状态说明", c."是否可处理"
    FROM "v2_纵向项目申报_列表" AS a
    LEFT JOIN LATERAL "tfn_流程"(p_template_id, a."编号", p_employee_id) AS c ON TRUE;
$$;

CREATE OR REPLACE FUNCTION "tfn_纵向项目立项"(p_employee_id integer, p_template_id integer)
RETURNS TABLE (
    "编号" integer, "分类" integer, "分类名称" text, "项目中文名称" varchar, "项目英文名称" varchar,
    "项目类型" varchar, "项目级别" varchar, "项目来源单位" varchar, "项目类别" varchar,
    "批准经费" numeric, "配套经费" numeric, "计划开始日期" date, "计划完成日期" date,
    "成果归属部门名称" varchar, "负责人编号" integer, "负责人工号" varchar, "负责人姓名" varchar,
    "负责人部门编号" integer, "负责人部门名称" varchar, "登记人编号" integer, "登记人姓名" varchar,
    "审核进度" text, "流程编号" integer, "流程模板编号" integer, "流程名称" varchar,
    "流程相关项目路径" varchar, "项目编号" integer, "发起人编号" integer, "发起人工号" varchar,
    "发起人姓名" varchar, "发起人部门名称" varchar, "状态" integer, "状态名称" text,
    "流程创建时间" timestamp, "步骤编号" integer, "步骤名称" varchar, "步骤状态" integer,
    "步骤链接名称" varchar, "步骤链接路径" varchar, "步骤链接样式" varchar,
    "步骤操作人编号" integer, "步骤操作人工号" varchar, "步骤操作人姓名" varchar,
    "步骤操作人部门名称" varchar, "步骤执行时间" timestamp, "步骤状态说明" varchar,
    "是否可处理" boolean
)
LANGUAGE sql
STABLE
AS $$
    SELECT a."编号", a."分类", a."分类名称", a."项目中文名称", a."项目英文名称", a."项目类型",
           a."项目级别", a."项目来源单位", a."项目类别", a."批准经费", a."配套经费",
           a."计划开始日期", a."计划完成日期", a."成果归属部门名称", a."负责人编号", a."负责人工号",
           a."负责人姓名", a."负责人部门编号", a."负责人部门名称", a."登记人编号", a."登记人姓名",
           concat(c."状态名称", '-', c."步骤状态说明"), c."流程编号", c."流程模板编号", c."流程名称",
           c."流程相关项目路径", c."项目编号", c."发起人编号", c."发起人工号", c."发起人姓名",
           c."发起人部门名称", c."状态", c."状态名称", c."流程创建时间", c."步骤编号",
           c."步骤名称", c."步骤状态", c."步骤链接名称", c."步骤链接路径", c."步骤链接样式",
           c."步骤操作人编号", c."步骤操作人工号", c."步骤操作人姓名", c."步骤操作人部门名称",
           c."步骤执行时间", c."步骤状态说明", c."是否可处理"
    FROM "v2_纵向项目立项_列表" AS a
    LEFT JOIN LATERAL "tfn_流程"(p_template_id, a."编号", p_employee_id) AS c ON TRUE;
$$;

COMMIT;
