-- P0: check revoke authorization even when a record was already revoked.
BEGIN;

CREATE OR REPLACE FUNCTION "sp_权限代理_撤销"(
    p_proxy_id bigint,
    p_actor_id integer,
    p_reason varchar DEFAULT NULL
)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE
    v_grantor_id integer;
    v_revoked_at timestamp;
    v_is_admin boolean;
BEGIN
    SELECT d."授权人编号", d."撤销时间"
      INTO v_grantor_id, v_revoked_at
      FROM "权限代理" d
     WHERE d."编号" = p_proxy_id
     FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION '权限代理不存在';
    END IF;

    SELECT EXISTS (
        SELECT 1
          FROM "人员角色" pr
          JOIN "角色" r ON r."编号" = pr."角色编号"
         WHERE pr."人员编号" = p_actor_id
           AND r."名称" IN ('系统管理员', '科研管理员', '科技办', '科研科负责人', '科研处处长')
    ) INTO v_is_admin;

    IF p_actor_id IS DISTINCT FROM v_grantor_id AND NOT v_is_admin THEN
        RAISE EXCEPTION '只有授权人或系统管理角色可以撤销该代理';
    END IF;

    IF v_revoked_at IS NOT NULL THEN
        RETURN 0;
    END IF;

    UPDATE "权限代理"
       SET "撤销时间" = CURRENT_TIMESTAMP,
           "撤销人编号" = p_actor_id,
           "撤销原因" = LEFT(NULLIF(BTRIM(p_reason), ''), 500)
     WHERE "编号" = p_proxy_id;
    RETURN 1;
END;
$$;

COMMIT;
