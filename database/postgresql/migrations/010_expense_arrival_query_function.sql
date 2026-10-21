-- Expense arrival list with the workflow progress columns used by the finance pages.
BEGIN;

CREATE OR REPLACE FUNCTION "tfn_项目经费到账"(p_employee_id integer, p_template_id integer)
RETURNS TABLE (
    "编号" integer,
    "流程模板编号" integer,
    "项目编号" integer,
    "拨款单位" varchar,
    "到账日期" date,
    "到账金额" numeric,
    "技术交易额" numeric,
    "管理费用" numeric,
    "技术合同认定服务费" numeric,
    "增值税" numeric,
    "到账单位" varchar,
    "经办人编号" integer,
    "到账凭证路径" text,
    "建立时间" timestamp,
    "备注" varchar,
    "经办人工号" varchar,
    "经办人姓名" varchar,
    "经办人部门编号" integer,
    "经办人部门名称" varchar,
    "审核进度" text,
    "流程编号" integer,
    "流程名称" varchar,
    "流程相关项目路径" varchar,
    "发起人编号" integer,
    "发起人工号" varchar,
    "发起人姓名" varchar,
    "发起人部门名称" varchar,
    "状态" integer,
    "状态名称" text,
    "流程创建时间" timestamp,
    "步骤编号" integer,
    "步骤名称" varchar,
    "步骤状态" integer,
    "步骤链接名称" varchar,
    "步骤链接路径" varchar,
    "步骤链接样式" varchar,
    "步骤操作人编号" integer,
    "步骤操作人工号" varchar,
    "步骤操作人姓名" varchar,
    "步骤操作人部门名称" varchar,
    "步骤执行时间" timestamp,
    "步骤状态说明" varchar,
    "是否可处理" boolean
)
LANGUAGE sql
STABLE
AS $$
    SELECT a."编号", a."流程模板编号", a."项目编号", a."拨款单位", a."到账日期",
           a."到账金额", a."技术交易额", a."管理费用", a."技术合同认定服务费", a."增值税",
           a."到账单位", a."经办人编号", a."到账凭证路径", a."建立时间", a."备注",
           a."经办人工号", a."经办人姓名", a."经办人部门编号", a."经办人部门名称",
           concat(c."状态名称", '-', c."步骤状态说明") AS "审核进度",
           c."流程编号", c."流程名称", c."流程相关项目路径", c."发起人编号",
           c."发起人工号", c."发起人姓名", c."发起人部门名称", c."状态", c."状态名称",
           c."流程创建时间", c."步骤编号", c."步骤名称", c."步骤状态",
           c."步骤链接名称", c."步骤链接路径", c."步骤链接样式", c."步骤操作人编号",
           c."步骤操作人工号", c."步骤操作人姓名", c."步骤操作人部门名称", c."步骤执行时间",
           c."步骤状态说明", c."是否可处理"
    FROM "v2_项目经费到账" AS a
    LEFT JOIN LATERAL "tfn_流程"(p_template_id, a."编号", p_employee_id) AS c ON TRUE;
$$;

COMMIT;
