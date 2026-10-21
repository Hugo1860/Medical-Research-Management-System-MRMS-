-- Common list queries that enrich business rows with workflow progress.
BEGIN;

CREATE OR REPLACE FUNCTION "tfn_任职"(p_employee_id integer, p_template_id integer)
RETURNS TABLE (
    "编号" integer, "任职开始时间" timestamp, "任职截止时间" timestamp, "任职人员编号" integer,
    "工号" varchar, "姓名" varchar, "部门名称" varchar, "任职团体名称" varchar, "所任职务" varchar,
    "相关文件路径" text, "建立时间" timestamp, "备注" varchar, "审核进度" text,
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
    SELECT a."编号", a."任职开始时间", a."任职截止时间", a."任职人员编号", a."工号", a."姓名",
           a."部门名称", a."任职团体名称", a."所任职务", a."相关文件路径", a."建立时间", a."备注",
           concat(c."状态名称", '-', c."步骤状态说明"), c."流程编号", c."流程模板编号", c."流程名称",
           c."流程相关项目路径", c."项目编号", c."发起人编号", c."发起人工号", c."发起人姓名",
           c."发起人部门名称", c."状态", c."状态名称", c."流程创建时间", c."步骤编号",
           c."步骤名称", c."步骤状态", c."步骤链接名称", c."步骤链接路径", c."步骤链接样式",
           c."步骤操作人编号", c."步骤操作人工号", c."步骤操作人姓名", c."步骤操作人部门名称",
           c."步骤执行时间", c."步骤状态说明", c."是否可处理"
    FROM "v2_任职" AS a
    LEFT JOIN LATERAL "tfn_流程"(p_template_id, a."编号", p_employee_id) AS c ON TRUE;
$$;

CREATE OR REPLACE FUNCTION "tfn_横向项目"(p_employee_id integer, p_template_id integer)
RETURNS TABLE (
    "编号" integer, "合同编号" varchar, "合同名称" varchar, "合同金额" numeric, "签订时间" timestamp,
    "项目负责人编号" integer, "项目负责人工号" varchar, "项目负责人姓名" varchar,
    "项目负责人部门编号" integer, "项目负责人部门名称" varchar, "项目归属部门编号" integer,
    "项目归属部门名称" varchar, "审核进度" text, "流程编号" integer, "流程模板编号" integer,
    "流程名称" varchar, "流程相关项目路径" varchar, "项目编号" integer, "发起人编号" integer,
    "发起人工号" varchar, "发起人姓名" varchar, "发起人部门名称" varchar, "状态" integer,
    "状态名称" text, "流程创建时间" timestamp, "步骤编号" integer, "步骤名称" varchar,
    "步骤状态" integer, "步骤链接名称" varchar, "步骤链接路径" varchar, "步骤链接样式" varchar,
    "步骤操作人编号" integer, "步骤操作人工号" varchar, "步骤操作人姓名" varchar,
    "步骤操作人部门名称" varchar, "步骤执行时间" timestamp, "步骤状态说明" varchar,
    "是否可处理" boolean
)
LANGUAGE sql
STABLE
AS $$
    SELECT a."编号", a."合同编号", a."合同名称", a."合同金额", a."签订时间", a."项目负责人编号",
           a."项目负责人工号", a."项目负责人姓名", a."项目负责人部门编号", a."项目负责人部门名称",
           a."项目归属部门编号", a."项目归属部门名称", concat(c."状态名称", '-', c."步骤状态说明"),
           c."流程编号", c."流程模板编号", c."流程名称", c."流程相关项目路径", c."项目编号",
           c."发起人编号", c."发起人工号", c."发起人姓名", c."发起人部门名称", c."状态",
           c."状态名称", c."流程创建时间", c."步骤编号", c."步骤名称", c."步骤状态",
           c."步骤链接名称", c."步骤链接路径", c."步骤链接样式", c."步骤操作人编号",
           c."步骤操作人工号", c."步骤操作人姓名", c."步骤操作人部门名称", c."步骤执行时间",
           c."步骤状态说明", c."是否可处理"
    FROM "v2_横向项目_列表" AS a
    LEFT JOIN LATERAL "tfn_流程"(p_template_id, a."编号", p_employee_id) AS c ON TRUE;
$$;

COMMIT;
