-- P0: auditable disposition workflow for budget reconciliation anomalies.
BEGIN;

CREATE TABLE IF NOT EXISTS "预算异常对账处理" (
    "编号" bigserial PRIMARY KEY,
    "流程模板编号" integer NOT NULL,
    "项目编号" integer NOT NULL,
    "异常类型" varchar(500) NOT NULL,
    "状态" varchar(20) NOT NULL DEFAULT '待处理',
    "责任人编号" integer,
    "处理说明" varchar(1000),
    "创建人编号" integer NOT NULL,
    "创建时间" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "更新时间" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "ck_预算异常对账处理_状态"
        CHECK ("状态" IN ('待处理', '处理中', '已确认', '已修正', '已忽略')),
    CONSTRAINT "uq_预算异常对账处理_异常"
        UNIQUE ("流程模板编号", "项目编号", "异常类型")
);

CREATE INDEX IF NOT EXISTS "ix_预算异常对账处理_状态"
    ON "预算异常对账处理" ("状态", "更新时间" DESC);

CREATE OR REPLACE FUNCTION "sp_预算异常对账_处理"(p_payload jsonb)
RETURNS bigint LANGUAGE plpgsql AS $$
DECLARE
    v_flow_template_id integer := NULLIF(p_payload->>'流程模板编号', '')::integer;
    v_project_id integer := NULLIF(p_payload->>'项目编号', '')::integer;
    v_anomaly_type varchar(500) := LEFT(BTRIM(COALESCE(p_payload->>'异常类型', '')), 500);
    v_status varchar(20) := COALESCE(NULLIF(BTRIM(p_payload->>'状态'), ''), '待处理');
    v_owner_id integer := NULLIF(p_payload->>'责任人编号', '')::integer;
    v_note varchar(1000) := LEFT(NULLIF(BTRIM(p_payload->>'处理说明'), ''), 1000);
    v_actor_id integer := NULLIF(p_payload->>'处理人编号', '')::integer;
    v_id bigint;
BEGIN
    IF v_flow_template_id IS NULL OR v_project_id IS NULL OR v_anomaly_type = '' THEN
        RAISE EXCEPTION '流程模板编号、项目编号和异常类型不能为空';
    END IF;
    IF v_status NOT IN ('待处理', '处理中', '已确认', '已修正', '已忽略') THEN
        RAISE EXCEPTION '预算异常处理状态无效';
    END IF;
    IF v_actor_id IS NULL OR v_actor_id <= 0 THEN
        RAISE EXCEPTION '处理人编号不能为空';
    END IF;

    INSERT INTO "预算异常对账处理" (
        "流程模板编号", "项目编号", "异常类型", "状态", "责任人编号",
        "处理说明", "创建人编号", "创建时间", "更新时间"
    ) VALUES (
        v_flow_template_id, v_project_id, v_anomaly_type, v_status, v_owner_id,
        v_note, v_actor_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
    )
    ON CONFLICT ("流程模板编号", "项目编号", "异常类型") DO UPDATE SET
        "状态" = EXCLUDED."状态",
        "责任人编号" = EXCLUDED."责任人编号",
        "处理说明" = EXCLUDED."处理说明",
        "更新时间" = CURRENT_TIMESTAMP
    RETURNING "编号" INTO v_id;
    RETURN v_id;
END;
$$;

CREATE OR REPLACE FUNCTION "tfn_预算异常对账处理"(
    p_flow_template_id integer DEFAULT NULL,
    p_project_id integer DEFAULT NULL
)
RETURNS TABLE (
    "编号" bigint,
    "流程模板编号" integer,
    "项目编号" integer,
    "异常类型" varchar,
    "状态" varchar,
    "责任人编号" integer,
    "责任人姓名" varchar,
    "处理说明" varchar,
    "创建人编号" integer,
    "创建人姓名" varchar,
    "创建时间" timestamp,
    "更新时间" timestamp
)
LANGUAGE sql STABLE AS $$
SELECT x."编号", x."流程模板编号", x."项目编号", x."异常类型", x."状态",
       x."责任人编号", owner."姓名"::varchar, x."处理说明", x."创建人编号",
       creator."姓名"::varchar, x."创建时间", x."更新时间"
  FROM "预算异常对账处理" x
  LEFT JOIN "人员" owner ON owner."编号" = x."责任人编号"
  LEFT JOIN "人员" creator ON creator."编号" = x."创建人编号"
 WHERE x."流程模板编号" = COALESCE(p_flow_template_id, x."流程模板编号")
   AND x."项目编号" = COALESCE(p_project_id, x."项目编号")
 ORDER BY x."更新时间" DESC, x."编号" DESC;
$$;

COMMIT;
