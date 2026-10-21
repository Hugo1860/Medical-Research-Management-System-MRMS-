-- P0: append-only operation audit records for mutating API requests.
BEGIN;

CREATE TABLE IF NOT EXISTS "操作审计" (
    "编号" bigserial PRIMARY KEY,
    "人员编号" integer,
    "请求编号" varchar(64) NOT NULL,
    "请求方法" varchar(10) NOT NULL,
    "请求路径" varchar(500) NOT NULL,
    "代理授权人编号" integer,
    "响应状态" integer NOT NULL,
    "客户端IP" varchar(64),
    "耗时毫秒" integer NOT NULL DEFAULT 0,
    "是否成功" boolean NOT NULL,
    "创建时间" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE "操作审计"
    ADD COLUMN IF NOT EXISTS "代理授权人编号" integer;

CREATE INDEX IF NOT EXISTS "ix_操作审计_人员时间"
    ON "操作审计" ("人员编号", "创建时间" DESC);
CREATE INDEX IF NOT EXISTS "ix_操作审计_请求时间"
    ON "操作审计" ("请求路径", "创建时间" DESC);

CREATE OR REPLACE FUNCTION "sp_操作审计_增"(
    p_person_id integer,
    p_request_id varchar,
    p_method varchar,
    p_path varchar,
    p_status integer,
    p_client_ip varchar,
    p_elapsed_ms integer,
    p_success boolean
)
RETURNS bigint LANGUAGE sql AS $$
    INSERT INTO "操作审计" (
        "人员编号", "请求编号", "请求方法", "请求路径", "响应状态",
        "客户端IP", "耗时毫秒", "是否成功"
    ) VALUES (
        p_person_id, LEFT(p_request_id, 64), LEFT(p_method, 10), LEFT(p_path, 500),
        p_status, LEFT(p_client_ip, 64), GREATEST(p_elapsed_ms, 0), p_success
    )
    RETURNING "编号";
$$;

CREATE OR REPLACE FUNCTION "sp_操作审计_增"(
    p_person_id integer,
    p_request_id varchar,
    p_method varchar,
    p_path varchar,
    p_status integer,
    p_client_ip varchar,
    p_elapsed_ms integer,
    p_success boolean,
    p_proxy_grantor_id integer
)
RETURNS bigint LANGUAGE sql AS $$
    INSERT INTO "操作审计" (
        "人员编号", "代理授权人编号", "请求编号", "请求方法", "请求路径", "响应状态",
        "客户端IP", "耗时毫秒", "是否成功"
    ) VALUES (
        p_person_id, p_proxy_grantor_id, LEFT(p_request_id, 64), LEFT(p_method, 10), LEFT(p_path, 500),
        p_status, LEFT(p_client_ip, 64), GREATEST(p_elapsed_ms, 0), p_success
    )
    RETURNING "编号";
$$;

COMMIT;
