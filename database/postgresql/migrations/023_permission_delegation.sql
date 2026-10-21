-- P0: explicit, time-bounded permission delegation with revocation.
BEGIN;

CREATE TABLE IF NOT EXISTS "权限代理" (
    "编号" bigserial PRIMARY KEY,
    "授权人编号" integer NOT NULL,
    "被代理人编号" integer NOT NULL,
    "代理范围" varchar(100) NOT NULL,
    "开始时间" timestamp without time zone NOT NULL,
    "结束时间" timestamp without time zone NOT NULL,
    "撤销时间" timestamp without time zone,
    "备注" varchar(500),
    "建立时间" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "ck_权限代理_人员不同" CHECK ("授权人编号" <> "被代理人编号"),
    CONSTRAINT "ck_权限代理_时间有效" CHECK ("结束时间" > "开始时间")
);

CREATE INDEX IF NOT EXISTS "ix_权限代理_被代理人有效期"
    ON "权限代理" ("被代理人编号", "开始时间", "结束时间");

CREATE OR REPLACE FUNCTION "sp_权限代理_增"(
    p_grantor_id integer, p_delegate_id integer, p_scope varchar,
    p_start_at timestamp, p_end_at timestamp, p_note varchar
)
RETURNS bigint LANGUAGE plpgsql AS $$
DECLARE v_id bigint;
BEGIN
    IF p_grantor_id = p_delegate_id THEN RAISE EXCEPTION '授权人和被代理人不能相同'; END IF;
    IF p_end_at <= p_start_at THEN RAISE EXCEPTION '代理结束时间必须晚于开始时间'; END IF;
    IF btrim(COALESCE(p_scope, '')) = '' THEN RAISE EXCEPTION '代理范围不能为空'; END IF;
    INSERT INTO "权限代理" ("授权人编号", "被代理人编号", "代理范围", "开始时间", "结束时间", "备注")
    VALUES (p_grantor_id, p_delegate_id, LEFT(p_scope, 100), p_start_at, p_end_at, LEFT(p_note, 500))
    RETURNING "编号" INTO v_id;
    RETURN v_id;
END;
$$;

CREATE OR REPLACE FUNCTION "fn_代理有效"(p_grantor_id integer, p_delegate_id integer, p_scope varchar)
RETURNS boolean LANGUAGE sql STABLE AS $$
SELECT EXISTS (
    SELECT 1 FROM "权限代理" d
    WHERE d."授权人编号" = p_grantor_id AND d."被代理人编号" = p_delegate_id
      AND d."代理范围" = p_scope AND d."撤销时间" IS NULL
      AND CURRENT_TIMESTAMP >= d."开始时间" AND CURRENT_TIMESTAMP < d."结束时间"
);
$$;

COMMIT;
