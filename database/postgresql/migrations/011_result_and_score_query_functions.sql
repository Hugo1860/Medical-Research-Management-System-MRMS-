-- Result author/project and expert score queries used by the成果 pages.
BEGIN;

CREATE OR REPLACE FUNCTION "tfn_项目"(p_project_type integer, p_project_id integer)
RETURNS TABLE ("项目类型" varchar, "项目名称" varchar)
LANGUAGE sql
STABLE
AS $$
    SELECT '纵向项目'::varchar, concat("项目中文名称", "项目英文名称")::varchar
    FROM "纵向项目立项"
    WHERE p_project_type = 1 AND "编号" = p_project_id
    UNION ALL
    SELECT '横向项目'::varchar, "合同名称"::varchar
    FROM "横向项目"
    WHERE p_project_type = 2 AND "编号" = p_project_id;
$$;

CREATE OR REPLACE FUNCTION "tfn_专家评分"(p_template_id integer, p_project_id integer)
RETURNS SETOF "专家评分"
LANGUAGE sql
STABLE
AS $$
    SELECT a.*
    FROM "专家评分" AS a
    WHERE a."流程模板编号" = p_template_id
      AND a."项目编号" = p_project_id;
$$;

CREATE OR REPLACE FUNCTION "tfn_成果作者"(p_result_type integer, p_result_id integer)
RETURNS TABLE (
    "编号" integer,
    "作者级别" varchar,
    "作者编号" integer,
    "工作量占比" integer,
    "完成字数" integer,
    "工号" varchar,
    "姓名" varchar,
    "部门编号" integer,
    "部门名称" varchar
)
LANGUAGE sql
STABLE
AS $$
    SELECT a."编号", a."作者级别", a."作者编号", a."工作量占比", a."完成字数",
           b."工号", b."姓名", b."部门编号", b."部门名称"
    FROM "成果作者" AS a
    JOIN "v1_人员_带部门名" AS b ON b."编号" = a."作者编号"
    WHERE a."成果类型" = p_result_type
      AND a."成果编号" = p_result_id;
$$;

CREATE OR REPLACE FUNCTION "tfn_成果项目"(p_result_type integer, p_result_id integer)
RETURNS TABLE (
    "编号" integer,
    "项目编号" integer,
    "项目类型" varchar,
    "项目名称" varchar,
    "是否项目带来成果" boolean
)
LANGUAGE sql
STABLE
AS $$
    SELECT a."编号", a."项目编号", b."项目类型", b."项目名称", a."是否项目带来成果"
    FROM "成果项目" AS a
    CROSS JOIN LATERAL "tfn_项目"(a."项目类型", a."项目编号") AS b
    WHERE a."成果类型" = p_result_type
      AND a."成果编号" = p_result_id;
$$;

COMMIT;
