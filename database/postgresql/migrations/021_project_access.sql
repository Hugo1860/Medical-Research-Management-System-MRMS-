-- P0: project-level read authorization shared by detail endpoints.
BEGIN;

CREATE OR REPLACE FUNCTION "fn_人员可访问项目"(
    p_person_id integer,
    p_flow_template_id integer,
    p_project_id integer
)
RETURNS boolean LANGUAGE sql STABLE AS $$
SELECT
    EXISTS (
        SELECT 1
        FROM "人员角色" AS pr
        JOIN "角色" AS r ON r."编号" = pr."角色编号"
        WHERE pr."人员编号" = p_person_id
          AND r."名称" IN ('系统管理员', '科研管理员', '科技办', '科研科负责人', '科研处处长')
    )
    OR EXISTS (
        SELECT 1 FROM "流程" AS f
        WHERE f."流程模板编号" = p_flow_template_id
          AND f."项目编号" = p_project_id
          AND f."发起人编号" = p_person_id
    )
    OR EXISTS (
        SELECT 1 FROM "项目参与人" AS pp
        WHERE pp."流程模板编号" = p_flow_template_id
          AND pp."项目编号" = p_project_id
          AND pp."参与人编号" = p_person_id
    )
    OR (p_flow_template_id = 410 AND EXISTS (
        SELECT 1 FROM "纵向项目立项" AS yp
        WHERE yp."编号" = p_project_id AND yp."负责人编号" = p_person_id
    ))
    OR (p_flow_template_id = 450 AND EXISTS (
        SELECT 1 FROM "横向项目" AS xp
        WHERE xp."编号" = p_project_id AND xp."项目负责人编号" = p_person_id
    ));
$$;

COMMIT;
