-- Simple inline table-valued functions used by permission and project detail pages.
-- These functions preserve the SQL Server query shape while using PostgreSQL row types.
BEGIN;

CREATE OR REPLACE FUNCTION "tfn_角色中的人员"(p_role_id integer)
RETURNS SETOF "v1_人员_带部门名"
LANGUAGE sql
STABLE
AS $$
    SELECT a.*
    FROM "v1_人员_带部门名" AS a
    WHERE EXISTS (
        SELECT 1
        FROM "人员角色" AS b
        WHERE b."角色编号" = p_role_id
          AND b."人员编号" = a."编号"
    );
$$;

CREATE OR REPLACE FUNCTION "tfn_角色中没有的人员"(p_role_id integer)
RETURNS SETOF "v1_人员_带部门名"
LANGUAGE sql
STABLE
AS $$
    SELECT a.*
    FROM "v1_人员_带部门名" AS a
    WHERE NOT EXISTS (
        SELECT 1
        FROM "人员角色" AS b
        WHERE b."角色编号" = p_role_id
          AND b."人员编号" = a."编号"
    );
$$;

CREATE OR REPLACE FUNCTION "tfn_角色的权限"(p_role_id integer)
RETURNS SETOF "v1_角色的权限"
LANGUAGE sql
STABLE
AS $$
    SELECT a."编号", a."上级编号", a."名称", a."路径", a."类型", a."图标", a."备注",
           (CASE WHEN b."编号" IS NULL THEN B'0' ELSE B'1' END)::bit(1) AS "拥有"
    FROM "权限" AS a
    LEFT JOIN "角色权限" AS b
      ON b."权限编号" = a."编号"
     AND b."角色编号" = p_role_id;
$$;

CREATE OR REPLACE FUNCTION "tfn_角色没有的权限"(p_role_id integer)
RETURNS SETOF "权限"
LANGUAGE sql
STABLE
AS $$
    SELECT a.*
    FROM "权限" AS a
    WHERE NOT EXISTS (
        SELECT 1
        FROM "角色权限" AS b
        WHERE b."角色编号" = p_role_id
          AND b."权限编号" = a."编号"
    );
$$;

CREATE OR REPLACE FUNCTION "tfn_项目参与人"(p_template_id integer, p_project_id integer)
RETURNS SETOF "v2_项目参与人"
LANGUAGE sql
STABLE
AS $$
    SELECT a.*
    FROM "v2_项目参与人" AS a
    WHERE a."流程模板编号" = p_template_id
      AND a."项目编号" = p_project_id;
$$;

CREATE OR REPLACE FUNCTION "tfn_项目经费预算"(p_template_id integer, p_project_id integer)
RETURNS SETOF "项目经费预算"
LANGUAGE sql
STABLE
AS $$
    SELECT a.*
    FROM "项目经费预算" AS a
    WHERE a."流程模板编号" = p_template_id
      AND a."项目编号" = p_project_id;
$$;

CREATE OR REPLACE FUNCTION "tfn_项目计划"(p_template_id integer, p_project_id integer)
RETURNS SETOF "项目计划"
LANGUAGE sql
STABLE
AS $$
    SELECT a.*
    FROM "项目计划" AS a
    WHERE a."流程模板编号" = p_template_id
      AND a."项目编号" = p_project_id;
$$;

CREATE OR REPLACE FUNCTION "tfn_项目预期成果"(p_template_id integer, p_project_id integer)
RETURNS SETOF "项目预期成果"
LANGUAGE sql
STABLE
AS $$
    SELECT a.*
    FROM "项目预期成果" AS a
    WHERE a."流程模板编号" = p_template_id
      AND a."项目编号" = p_project_id;
$$;

CREATE OR REPLACE FUNCTION "tfn_获取下级字典_通过上级分类和名称"(
    p_category varchar(50),
    p_value varchar(50)
)
RETURNS SETOF "字典"
LANGUAGE sql
STABLE
AS $$
    SELECT b.*
    FROM "字典" AS a
    JOIN "字典" AS b ON b."上级编号" = a."编号"
    WHERE a."分类" = p_category
      AND a."值" = p_value;
$$;

COMMIT;
