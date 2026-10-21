-- Result list queries with workflow progress for paper, book, patent and award pages.
BEGIN;

CREATE OR REPLACE FUNCTION "tfn_成果论文"(p_employee_id integer, p_template_id integer)
RETURNS TABLE (
    "编号" integer, "论文标题" varchar, "论文Online日期" date, "论文正式出版日期" date,
    "刊物名称" varchar, "认领人编号" integer, "第一作者" varchar, "审核进度" text,
    "流程编号" integer, "流程模板编号" integer, "流程名称" varchar, "流程相关项目路径" varchar,
    "项目编号" integer, "发起人编号" integer, "发起人工号" varchar, "发起人姓名" varchar,
    "发起人部门名称" varchar, "状态" integer, "状态名称" text, "流程创建时间" timestamp,
    "步骤编号" integer, "步骤名称" varchar, "步骤状态" integer, "步骤链接名称" varchar,
    "步骤链接路径" varchar, "步骤链接样式" varchar, "步骤操作人编号" integer,
    "步骤操作人工号" varchar, "步骤操作人姓名" varchar, "步骤操作人部门名称" varchar,
    "步骤执行时间" timestamp, "步骤状态说明" varchar, "是否可处理" boolean
)
LANGUAGE sql STABLE AS $$
    SELECT a."编号", a."论文标题", a."论文Online日期", a."论文正式出版日期", a."刊物名称",
           a."认领人编号", a."第一作者", concat(c."状态名称", '-', c."步骤状态说明"),
           c."流程编号", c."流程模板编号", c."流程名称", c."流程相关项目路径", c."项目编号",
           c."发起人编号", c."发起人工号", c."发起人姓名", c."发起人部门名称", c."状态",
           c."状态名称", c."流程创建时间", c."步骤编号", c."步骤名称", c."步骤状态",
           c."步骤链接名称", c."步骤链接路径", c."步骤链接样式", c."步骤操作人编号",
           c."步骤操作人工号", c."步骤操作人姓名", c."步骤操作人部门名称", c."步骤执行时间",
           c."步骤状态说明", c."是否可处理"
    FROM "v1_成果论文_列表" AS a
    LEFT JOIN LATERAL "tfn_流程"(p_template_id, a."编号", p_employee_id) AS c ON TRUE;
$$;

CREATE OR REPLACE FUNCTION "tfn_成果著作"(p_employee_id integer, p_template_id integer)
RETURNS TABLE (
    "编号" integer, "著作名称" varchar, "著作出版日期" date, "年度" integer,
    "出版社名称" varchar, "著作类型" varchar, "主编" varchar, "审核进度" text,
    "流程编号" integer, "流程模板编号" integer, "流程名称" varchar, "流程相关项目路径" varchar,
    "项目编号" integer, "发起人编号" integer, "发起人工号" varchar, "发起人姓名" varchar,
    "发起人部门名称" varchar, "状态" integer, "状态名称" text, "流程创建时间" timestamp,
    "步骤编号" integer, "步骤名称" varchar, "步骤状态" integer, "步骤链接名称" varchar,
    "步骤链接路径" varchar, "步骤链接样式" varchar, "步骤操作人编号" integer,
    "步骤操作人工号" varchar, "步骤操作人姓名" varchar, "步骤操作人部门名称" varchar,
    "步骤执行时间" timestamp, "步骤状态说明" varchar, "是否可处理" boolean
)
LANGUAGE sql STABLE AS $$
    SELECT a."编号", a."著作名称", a."著作出版日期", a."年度", a."出版社名称", a."著作类型",
           a."主编", concat(c."状态名称", '-', c."步骤状态说明"), c."流程编号", c."流程模板编号",
           c."流程名称", c."流程相关项目路径", c."项目编号", c."发起人编号", c."发起人工号",
           c."发起人姓名", c."发起人部门名称", c."状态", c."状态名称", c."流程创建时间",
           c."步骤编号", c."步骤名称", c."步骤状态", c."步骤链接名称", c."步骤链接路径",
           c."步骤链接样式", c."步骤操作人编号", c."步骤操作人工号", c."步骤操作人姓名",
           c."步骤操作人部门名称", c."步骤执行时间", c."步骤状态说明", c."是否可处理"
    FROM "v1_成果著作_列表" AS a
    LEFT JOIN LATERAL "tfn_流程"(p_template_id, a."编号", p_employee_id) AS c ON TRUE;
$$;

CREATE OR REPLACE FUNCTION "tfn_成果专利"(p_employee_id integer, p_template_id integer)
RETURNS TABLE (
    "编号" integer, "专利名称" varchar, "专利权人名称" varchar, "专利类型" varchar,
    "是否授权" boolean, "年度" integer, "第一作者" varchar, "审核进度" text,
    "流程编号" integer, "流程模板编号" integer, "流程名称" varchar, "流程相关项目路径" varchar,
    "项目编号" integer, "发起人编号" integer, "发起人工号" varchar, "发起人姓名" varchar,
    "发起人部门名称" varchar, "状态" integer, "状态名称" text, "流程创建时间" timestamp,
    "步骤编号" integer, "步骤名称" varchar, "步骤状态" integer, "步骤链接名称" varchar,
    "步骤链接路径" varchar, "步骤链接样式" varchar, "步骤操作人编号" integer,
    "步骤操作人工号" varchar, "步骤操作人姓名" varchar, "步骤操作人部门名称" varchar,
    "步骤执行时间" timestamp, "步骤状态说明" varchar, "是否可处理" boolean
)
LANGUAGE sql STABLE AS $$
    SELECT a."编号", a."专利名称", a."专利权人名称", a."专利类型", a."是否授权", a."年度",
           a."第一作者", concat(c."状态名称", '-', c."步骤状态说明"), c."流程编号", c."流程模板编号",
           c."流程名称", c."流程相关项目路径", c."项目编号", c."发起人编号", c."发起人工号",
           c."发起人姓名", c."发起人部门名称", c."状态", c."状态名称", c."流程创建时间",
           c."步骤编号", c."步骤名称", c."步骤状态", c."步骤链接名称", c."步骤链接路径",
           c."步骤链接样式", c."步骤操作人编号", c."步骤操作人工号", c."步骤操作人姓名",
           c."步骤操作人部门名称", c."步骤执行时间", c."步骤状态说明", c."是否可处理"
    FROM "v1_成果专利_列表" AS a
    LEFT JOIN LATERAL "tfn_流程"(p_template_id, a."编号", p_employee_id) AS c ON TRUE;
$$;

CREATE OR REPLACE FUNCTION "tfn_成果获奖"(p_employee_id integer, p_template_id integer)
RETURNS TABLE (
    "编号" integer, "获奖名称" varchar, "获奖日期" date, "获奖级别" varchar,
    "获奖类别" varchar, "颁奖单位" varchar, "年度" integer, "第一完成人" varchar,
    "审核进度" text, "流程编号" integer, "流程模板编号" integer, "流程名称" varchar,
    "流程相关项目路径" varchar, "项目编号" integer, "发起人编号" integer,
    "发起人工号" varchar, "发起人姓名" varchar, "发起人部门名称" varchar, "状态" integer,
    "状态名称" text, "流程创建时间" timestamp, "步骤编号" integer, "步骤名称" varchar,
    "步骤状态" integer, "步骤链接名称" varchar, "步骤链接路径" varchar, "步骤链接样式" varchar,
    "步骤操作人编号" integer, "步骤操作人工号" varchar, "步骤操作人姓名" varchar,
    "步骤操作人部门名称" varchar, "步骤执行时间" timestamp, "步骤状态说明" varchar,
    "是否可处理" boolean
)
LANGUAGE sql STABLE AS $$
    SELECT a."编号", a."获奖名称", a."获奖日期", a."获奖级别", a."获奖类别", a."颁奖单位",
           a."年度", a."第一完成人", concat(c."状态名称", '-', c."步骤状态说明"), c."流程编号",
           c."流程模板编号", c."流程名称", c."流程相关项目路径", c."项目编号", c."发起人编号",
           c."发起人工号", c."发起人姓名", c."发起人部门名称", c."状态", c."状态名称",
           c."流程创建时间", c."步骤编号", c."步骤名称", c."步骤状态", c."步骤链接名称",
           c."步骤链接路径", c."步骤链接样式", c."步骤操作人编号", c."步骤操作人工号",
           c."步骤操作人姓名", c."步骤操作人部门名称", c."步骤执行时间", c."步骤状态说明",
           c."是否可处理"
    FROM "v1_成果获奖_列表" AS a
    LEFT JOIN LATERAL "tfn_流程"(p_template_id, a."编号", p_employee_id) AS c ON TRUE;
$$;

COMMIT;
