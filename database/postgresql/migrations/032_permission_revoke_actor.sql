-- P0: retain the administrator that initiated a bulk delegation revocation.
BEGIN;

CREATE OR REPLACE FUNCTION "sp_权限代理_回收人员"(
    p_person_id integer,
    p_reason varchar,
    p_revoker_id integer
)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE v_count integer;
BEGIN
    IF p_person_id IS NULL OR p_person_id <= 0 THEN
        RAISE EXCEPTION '人员编号无效';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM "人员" WHERE "编号" = p_person_id) THEN
        RAISE EXCEPTION '人员不存在';
    END IF;

    UPDATE "权限代理"
       SET "撤销时间" = CURRENT_TIMESTAMP,
           "撤销人编号" = p_revoker_id,
           "撤销原因" = LEFT(COALESCE(NULLIF(BTRIM(p_reason), ''), '人员权限已回收'), 500)
     WHERE "撤销时间" IS NULL
       AND ("授权人编号" = p_person_id OR "被代理人编号" = p_person_id);
    GET DIAGNOSTICS v_count = ROW_COUNT;
    RETURN v_count;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_权限代理_回收人员"(
    p_person_id integer,
    p_reason varchar DEFAULT '人员已禁用'
)
RETURNS integer LANGUAGE plpgsql AS $$
BEGIN
    RETURN "sp_权限代理_回收人员"(p_person_id, p_reason, NULL);
END;
$$;

COMMIT;
