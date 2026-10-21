-- P0: tighten delegation and budget anomaly disposition validation.
BEGIN;

CREATE OR REPLACE FUNCTION "sp_权限代理_增"(
    p_grantor_id integer, p_delegate_id integer, p_scope varchar,
    p_start_at timestamp, p_end_at timestamp, p_note varchar
)
RETURNS bigint LANGUAGE plpgsql AS $$
DECLARE v_id bigint;
BEGIN
    IF p_grantor_id IS NULL OR p_delegate_id IS NULL
       OR p_grantor_id <= 0 OR p_delegate_id <= 0
       OR p_grantor_id = p_delegate_id THEN
        RAISE EXCEPTION '授权人和被代理人必须是不同的有效人员';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM "人员" WHERE "编号" = p_grantor_id AND "是否启用" = TRUE)
       OR NOT EXISTS (SELECT 1 FROM "人员" WHERE "编号" = p_delegate_id AND "是否启用" = TRUE) THEN
        RAISE EXCEPTION '授权人和被代理人必须是已启用人员';
    END IF;
    IF p_start_at IS NULL OR p_end_at IS NULL OR p_end_at <= p_start_at THEN
        RAISE EXCEPTION '代理结束时间必须晚于开始时间';
    END IF;
    IF btrim(COALESCE(p_scope, '')) = '' THEN
        RAISE EXCEPTION '代理范围不能为空';
    END IF;
    IF EXISTS (
        SELECT 1
          FROM "权限代理" d
         WHERE d."授权人编号" = p_grantor_id
           AND d."被代理人编号" = p_delegate_id
           AND d."代理范围" = LEFT(p_scope, 100)
           AND d."撤销时间" IS NULL
           AND d."开始时间" < p_end_at
           AND d."结束时间" > p_start_at
    ) THEN
        RAISE EXCEPTION '同一代理范围存在重叠的有效授权';
    END IF;

    INSERT INTO "权限代理" ("授权人编号", "被代理人编号", "代理范围", "开始时间", "结束时间", "备注")
    VALUES (p_grantor_id, p_delegate_id, LEFT(p_scope, 100), p_start_at, p_end_at, LEFT(p_note, 500))
    RETURNING "编号" INTO v_id;
    RETURN v_id;
END;
$$;

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
    IF p_revoker_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM "人员" WHERE "编号" = p_revoker_id AND "是否启用" = TRUE) THEN
        RAISE EXCEPTION '回收人必须是已启用人员';
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
    IF v_actor_id IS NULL OR v_actor_id <= 0
       OR NOT EXISTS (SELECT 1 FROM "人员" WHERE "编号" = v_actor_id AND "是否启用" = TRUE) THEN
        RAISE EXCEPTION '处理人必须是已启用人员';
    END IF;
    IF v_owner_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM "人员" WHERE "编号" = v_owner_id AND "是否启用" = TRUE) THEN
        RAISE EXCEPTION '责任人必须是已启用人员';
    END IF;
    IF NOT EXISTS (
        SELECT 1
          FROM "tfn_预算异常对账"(v_flow_template_id, v_project_id) x
         WHERE x."异常类型" = v_anomaly_type
    ) THEN
        RAISE EXCEPTION '预算异常不存在或已被修正，请刷新对账结果后再处理';
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

UPDATE "系统迁移版本"
   SET "版本" = 39,
       "脚本" = '001-039',
       "检查摘要" = '代理授权校验启用人员与重叠范围，预算异常处理校验异常存在和人员状态'
 WHERE "编号" = 1;

COMMIT;
