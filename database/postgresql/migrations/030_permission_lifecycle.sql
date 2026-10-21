-- P0: permission revocation lifecycle for offboarding and delegated access.
BEGIN;

ALTER TABLE "权限代理"
    ADD COLUMN IF NOT EXISTS "撤销人编号" integer,
    ADD COLUMN IF NOT EXISTS "撤销原因" varchar(500);

CREATE INDEX IF NOT EXISTS "ix_权限代理_有效状态"
    ON "权限代理" ("撤销时间", "结束时间");

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

    IF v_revoked_at IS NOT NULL THEN
        RETURN 0;
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

    UPDATE "权限代理"
       SET "撤销时间" = CURRENT_TIMESTAMP,
           "撤销人编号" = p_actor_id,
           "撤销原因" = LEFT(NULLIF(BTRIM(p_reason), ''), 500)
     WHERE "编号" = p_proxy_id;
    RETURN 1;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_权限代理_回收人员"(
    p_person_id integer,
    p_reason varchar DEFAULT '人员已禁用'
)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE v_count integer;
BEGIN
    IF p_person_id IS NULL OR p_person_id <= 0 THEN
        RAISE EXCEPTION '人员编号无效';
    END IF;

    UPDATE "权限代理"
       SET "撤销时间" = CURRENT_TIMESTAMP,
           "撤销原因" = LEFT(COALESCE(NULLIF(BTRIM(p_reason), ''), '人员权限已回收'), 500)
     WHERE "撤销时间" IS NULL
       AND ("授权人编号" = p_person_id OR "被代理人编号" = p_person_id);
    GET DIAGNOSTICS v_count = ROW_COUNT;
    RETURN v_count;
END;
$$;

CREATE OR REPLACE FUNCTION "trg_人员_禁用回收代理"()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NEW."是否启用" IS FALSE
       AND OLD."是否启用" IS DISTINCT FROM NEW."是否启用" THEN
        PERFORM "sp_权限代理_回收人员"(NEW."编号", '人员已禁用，自动回收代理权限');
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS "trg_人员_禁用回收代理" ON "人员";
CREATE TRIGGER "trg_人员_禁用回收代理"
AFTER UPDATE OF "是否启用" ON "人员"
FOR EACH ROW EXECUTE FUNCTION "trg_人员_禁用回收代理"();

COMMIT;
