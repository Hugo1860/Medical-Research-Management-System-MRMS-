-- P1: compatibility functions for the two legacy multi-result procedures.
BEGIN;

CREATE OR REPLACE FUNCTION "sp_角色中的人员_获取"(p_role_id integer)
RETURNS jsonb LANGUAGE sql STABLE AS $$
SELECT jsonb_build_object(
    '角色人员', COALESCE((
        SELECT jsonb_agg(to_jsonb(x) ORDER BY x."编号")
          FROM "tfn_角色中的人员"(p_role_id) AS x
    ), '[]'::jsonb),
    '非角色人员', COALESCE((
        SELECT jsonb_agg(to_jsonb(x) ORDER BY x."编号")
          FROM "tfn_角色中没有的人员"(p_role_id) AS x
    ), '[]'::jsonb)
);
$$;

CREATE OR REPLACE FUNCTION "sp_登录"(p_employee_no varchar, p_password varchar)
RETURNS jsonb LANGUAGE plpgsql AS $$
DECLARE
    v_person jsonb;
    v_permissions jsonb;
BEGIN
    SELECT to_jsonb(x) - '密码'
      INTO v_person
      FROM "v1_人员_带部门名" AS x
     WHERE x."工号" = p_employee_no
       AND x."密码" = p_password
       AND x."是否启用" = TRUE;

    IF v_person IS NULL THEN
        RAISE EXCEPTION '登录信息错误';
    END IF;

    SELECT COALESCE(jsonb_agg(to_jsonb(x) ORDER BY x."编号"), '[]'::jsonb)
      INTO v_permissions
      FROM "tfn_人员的权限"(p_employee_no) AS x;

    RETURN jsonb_build_object('人员', v_person, '权限', v_permissions);
END;
$$;

COMMIT;
