-- Academic lecture and conference queries with workflow progress.
BEGIN;

CREATE OR REPLACE FUNCTION "tfn_主办讲座"(p_employee_id integer, p_template_id integer)
RETURNS TABLE (
    "编号" integer, "活动名称" varchar, "所属部门编号" integer, "所属部门名称" varchar,
    "活动经费" numeric, "会议类型" varchar, "活动对象" varchar, "开始时间" timestamp,
    "结束时间" timestamp, "主办人编号" integer, "主讲人姓名" varchar, "主讲人国别" varchar,
    "主讲人单位" varchar, "主讲人学位" varchar, "主讲人职称" varchar, "主讲人简历路径" text,
    "讲座地点" varchar, "内容概述" varchar, "主办人工号" varchar, "主办人姓名" varchar,
    "主办人部门名称" varchar, "审核进度" text, "流程编号" integer, "流程模板编号" integer,
    "流程名称" varchar, "流程相关项目路径" varchar, "项目编号" integer, "发起人编号" integer,
    "发起人工号" varchar, "发起人姓名" varchar, "发起人部门名称" varchar, "状态" integer,
    "状态名称" text, "流程创建时间" timestamp, "步骤编号" integer, "步骤名称" varchar,
    "步骤状态" integer, "步骤链接名称" varchar, "步骤链接路径" varchar, "步骤链接样式" varchar,
    "步骤操作人编号" integer, "步骤操作人工号" varchar, "步骤操作人姓名" varchar,
    "步骤操作人部门名称" varchar, "步骤执行时间" timestamp, "步骤状态说明" varchar,
    "是否可处理" boolean, "是否可以反馈" boolean, "是否反馈过" boolean
)
LANGUAGE sql STABLE AS $$
    SELECT a.*, concat(c."状态名称", '-', c."步骤状态说明"), c."流程编号", c."流程模板编号",
           c."流程名称", c."流程相关项目路径", c."项目编号", c."发起人编号", c."发起人工号",
           c."发起人姓名", c."发起人部门名称", c."状态", c."状态名称", c."流程创建时间",
           c."步骤编号", c."步骤名称", c."步骤状态", c."步骤链接名称", c."步骤链接路径",
           c."步骤链接样式", c."步骤操作人编号", c."步骤操作人工号", c."步骤操作人姓名",
           c."步骤操作人部门名称", c."步骤执行时间", c."步骤状态说明", c."是否可处理",
           CASE WHEN c."状态" = 1 THEN TRUE ELSE FALSE END,
           EXISTS (SELECT 1 FROM "讲座反馈" AS f WHERE f."讲座编号" = a."编号")
    FROM "v2_主办讲座" AS a
    LEFT JOIN LATERAL "tfn_流程"(p_template_id, a."编号", p_employee_id) AS c ON TRUE;
$$;

CREATE OR REPLACE FUNCTION "tfn_讲座反馈"(p_employee_id integer, p_template_id integer)
RETURNS TABLE (
    "编号" integer, "讲座编号" integer, "活动名称" varchar, "所属部门编号" integer,
    "所属部门名称" varchar, "活动经费" numeric, "会议类型" varchar, "活动对象" varchar,
    "开始时间" timestamp, "结束时间" timestamp, "主办人编号" integer, "主讲人姓名" varchar,
    "主讲人国别" varchar, "主讲人单位" varchar, "主讲人学位" varchar, "主讲人职称" varchar,
    "主讲人简历路径" text, "讲座地点" varchar, "内容概述" varchar, "主办人工号" varchar,
    "主办人姓名" varchar, "主办人部门名称" varchar, "专家姓名" varchar, "专家身份证号码" varchar,
    "专家护照号" varchar, "专家联系方式" varchar, "专家讲课费金额" numeric, "结算方式" varchar,
    "活动持续分钟" integer, "参会人数" integer, "讲座附件路径" text, "讲座效果" varchar,
    "审核进度" text, "流程编号" integer, "流程模板编号" integer, "流程名称" varchar,
    "流程相关项目路径" varchar, "项目编号" integer, "发起人编号" integer, "发起人工号" varchar,
    "发起人姓名" varchar, "发起人部门名称" varchar, "状态" integer, "状态名称" text,
    "流程创建时间" timestamp, "步骤编号" integer, "步骤名称" varchar, "步骤状态" integer,
    "步骤链接名称" varchar, "步骤链接路径" varchar, "步骤链接样式" varchar,
    "步骤操作人编号" integer, "步骤操作人工号" varchar, "步骤操作人姓名" varchar,
    "步骤操作人部门名称" varchar, "步骤执行时间" timestamp, "步骤状态说明" varchar,
    "是否可处理" boolean
)
LANGUAGE sql STABLE AS $$
    SELECT a.*, concat(c."状态名称", '-', c."步骤状态说明"), c."流程编号", c."流程模板编号",
           c."流程名称", c."流程相关项目路径", c."项目编号", c."发起人编号", c."发起人工号",
           c."发起人姓名", c."发起人部门名称", c."状态", c."状态名称", c."流程创建时间",
           c."步骤编号", c."步骤名称", c."步骤状态", c."步骤链接名称", c."步骤链接路径",
           c."步骤链接样式", c."步骤操作人编号", c."步骤操作人工号", c."步骤操作人姓名",
           c."步骤操作人部门名称", c."步骤执行时间", c."步骤状态说明", c."是否可处理"
    FROM "v3_讲座反馈" AS a
    LEFT JOIN LATERAL "tfn_流程"(p_template_id, a."编号", p_employee_id) AS c ON TRUE;
$$;

CREATE OR REPLACE FUNCTION "tfn_参加会议"(p_employee_id integer, p_template_id integer)
RETURNS TABLE (
    "编号" integer, "会议名称" varchar, "会议主办单位" varchar, "会议承办单位" varchar,
    "研讨领域" varchar, "学科类型" varchar, "会议地址" varchar, "会议开始时间" timestamp,
    "会议结束时间" timestamp, "往返时间" integer, "会议网址" varchar, "会议简介及申请理由" varchar,
    "申请人编号" integer, "参会相关文件路径" text, "申请人工号" varchar, "申请人姓名" varchar,
    "申请人部门名称" varchar, "审核进度" text, "流程编号" integer, "流程模板编号" integer,
    "流程名称" varchar, "流程相关项目路径" varchar, "项目编号" integer, "发起人编号" integer,
    "发起人工号" varchar, "发起人姓名" varchar, "发起人部门名称" varchar, "状态" integer,
    "状态名称" text, "流程创建时间" timestamp, "步骤编号" integer, "步骤名称" varchar,
    "步骤状态" integer, "步骤链接名称" varchar, "步骤链接路径" varchar, "步骤链接样式" varchar,
    "步骤操作人编号" integer, "步骤操作人工号" varchar, "步骤操作人姓名" varchar,
    "步骤操作人部门名称" varchar, "步骤执行时间" timestamp, "步骤状态说明" varchar,
    "是否可处理" boolean, "是否可以反馈" boolean, "是否反馈过" boolean
)
LANGUAGE sql STABLE AS $$
    SELECT a.*, concat(c."状态名称", '-', c."步骤状态说明"), c."流程编号", c."流程模板编号",
           c."流程名称", c."流程相关项目路径", c."项目编号", c."发起人编号", c."发起人工号",
           c."发起人姓名", c."发起人部门名称", c."状态", c."状态名称", c."流程创建时间",
           c."步骤编号", c."步骤名称", c."步骤状态", c."步骤链接名称", c."步骤链接路径",
           c."步骤链接样式", c."步骤操作人编号", c."步骤操作人工号", c."步骤操作人姓名",
           c."步骤操作人部门名称", c."步骤执行时间", c."步骤状态说明", c."是否可处理",
           CASE WHEN c."状态" = 1 THEN TRUE ELSE FALSE END,
           EXISTS (SELECT 1 FROM "参加会议反馈" AS f WHERE f."参加会议编号" = a."编号")
    FROM "v2_参加会议" AS a
    LEFT JOIN LATERAL "tfn_流程"(p_template_id, a."编号", p_employee_id) AS c ON TRUE;
$$;

CREATE OR REPLACE FUNCTION "tfn_参加会议反馈"(p_employee_id integer, p_template_id integer)
RETURNS TABLE (
    "编号" integer, "参加会议编号" integer, "会议名称" varchar, "会议主办单位" varchar,
    "会议承办单位" varchar, "研讨领域" varchar, "学科类型" varchar, "会议地址" varchar,
    "会议开始时间" timestamp, "会议结束时间" timestamp, "往返时间" integer, "会议网址" varchar,
    "会议简介及申请理由" varchar, "申请人编号" integer, "参会相关文件路径" text,
    "申请人工号" varchar, "申请人姓名" varchar, "申请人部门名称" varchar, "活动持续分钟" integer,
    "参会人数" integer, "参会总结" varchar, "会议反馈文件路径" text, "审核进度" text,
    "流程编号" integer, "流程模板编号" integer, "流程名称" varchar, "流程相关项目路径" varchar,
    "项目编号" integer, "发起人编号" integer, "发起人工号" varchar, "发起人姓名" varchar,
    "发起人部门名称" varchar, "状态" integer, "状态名称" text, "流程创建时间" timestamp,
    "步骤编号" integer, "步骤名称" varchar, "步骤状态" integer, "步骤链接名称" varchar,
    "步骤链接路径" varchar, "步骤链接样式" varchar, "步骤操作人编号" integer,
    "步骤操作人工号" varchar, "步骤操作人姓名" varchar, "步骤操作人部门名称" varchar,
    "步骤执行时间" timestamp, "步骤状态说明" varchar, "是否可处理" boolean
)
LANGUAGE sql STABLE AS $$
    SELECT a.*, concat(c."状态名称", '-', c."步骤状态说明"), c."流程编号", c."流程模板编号",
           c."流程名称", c."流程相关项目路径", c."项目编号", c."发起人编号", c."发起人工号",
           c."发起人姓名", c."发起人部门名称", c."状态", c."状态名称", c."流程创建时间",
           c."步骤编号", c."步骤名称", c."步骤状态", c."步骤链接名称", c."步骤链接路径",
           c."步骤链接样式", c."步骤操作人编号", c."步骤操作人工号", c."步骤操作人姓名",
           c."步骤操作人部门名称", c."步骤执行时间", c."步骤状态说明", c."是否可处理"
    FROM "v3_参加会议反馈" AS a
    LEFT JOIN LATERAL "tfn_流程"(p_template_id, a."编号", p_employee_id) AS c ON TRUE;
$$;

CREATE OR REPLACE FUNCTION "tfn_获取某步骤的可用操作_废弃"(p_step_id integer)
RETURNS TABLE ("状态值" integer, "操作说明" varchar)
LANGUAGE sql STABLE AS $$
    SELECT d."状态值", d."操作说明"
    FROM "步骤" AS a
    JOIN "步骤模板" AS b ON b."编号" = a."步骤模板编号"
    JOIN "步骤类型" AS c ON c."编号" = b."步骤类型编号"
    JOIN "步骤类型状态" AS d ON d."步骤类型编号" = c."编号"
    WHERE a."编号" = p_step_id
      AND d."状态值" <> 0;
$$;

COMMIT;
