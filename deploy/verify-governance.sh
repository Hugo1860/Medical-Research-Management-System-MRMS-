#!/usr/bin/env bash
set -euo pipefail

# Run governance checks inside the PostgreSQL container. All writes happen in
# one transaction and are rolled back, so this is safe against a live database.
database_container="${DATABASE_CONTAINER:-2018-7-24-db-1}"
database_name="${DATABASE_NAME:-ScientificResearch_Model}"
database_user="${DATABASE_USER:-postgres}"
command_timeout="${GOVERNANCE_TIMEOUT_SECONDS:-60}"

timeout "$command_timeout" docker exec -i "$database_container" psql \
  -U "$database_user" \
  -d "$database_name" \
  -v ON_ERROR_STOP=1 \
  -At <<'SQL'
BEGIN;

DO $$
DECLARE
    grantor_id integer;
    delegate_id integer;
    proxy_id bigint;
    flow_id integer;
    project_id integer;
    anomaly_type varchar;
    disposition_id bigint;
    smoke_scope varchar := '治理验收-' || txid_current()::varchar;
BEGIN
    SELECT min("编号")
      INTO grantor_id
      FROM "人员"
     WHERE "是否启用" = TRUE;
    SELECT min("编号")
      INTO delegate_id
      FROM "人员"
     WHERE "是否启用" = TRUE
       AND "编号" <> grantor_id;
    IF grantor_id IS NULL OR delegate_id IS NULL THEN
        RAISE EXCEPTION '治理验收需要至少两名启用人员';
    END IF;

    proxy_id := "sp_权限代理_增"(
        grantor_id,
        delegate_id,
        smoke_scope::varchar,
        (CURRENT_TIMESTAMP - INTERVAL '1 minute')::timestamp,
        (CURRENT_TIMESTAMP + INTERVAL '1 hour')::timestamp,
        '事务回滚 smoke test'::varchar
    );
    IF NOT "fn_代理有效"(grantor_id, delegate_id, smoke_scope) THEN
        RAISE EXCEPTION '代理授权有效性检查失败';
    END IF;
    IF "sp_权限代理_撤销"(proxy_id, grantor_id, '事务回滚 smoke test') <> 1 THEN
        RAISE EXCEPTION '代理撤销检查失败';
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM "权限代理"
         WHERE "编号" = proxy_id
           AND "撤销人编号" = grantor_id
           AND "撤销原因" = '事务回滚 smoke test'
    ) THEN
        RAISE EXCEPTION '代理撤销审计字段检查失败';
    END IF;
    IF "fn_代理有效"(grantor_id, delegate_id, smoke_scope) THEN
        RAISE EXCEPTION '代理撤销后仍被判定有效';
    END IF;

    -- Verify the offboarding path separately from single-record revoke.  The
    -- call records the administrator and must invalidate every active grant
    -- involving the target person in the same transaction.
    smoke_scope := '治理验收-离岗-' || txid_current()::varchar;
    proxy_id := "sp_权限代理_增"(
        grantor_id,
        delegate_id,
        smoke_scope::varchar,
        (CURRENT_TIMESTAMP - INTERVAL '1 minute')::timestamp,
        (CURRENT_TIMESTAMP + INTERVAL '1 hour')::timestamp,
        '事务回滚 offboarding smoke test'::varchar
    );
    IF NOT "fn_代理有效"(grantor_id, delegate_id, smoke_scope) THEN
        RAISE EXCEPTION '批量回收前代理有效性检查失败';
    END IF;
    IF "sp_权限代理_回收人员"(delegate_id, '事务回滚离岗回收 smoke test', grantor_id) < 1 THEN
        RAISE EXCEPTION '按人员批量回收代理检查失败';
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM "权限代理"
         WHERE "编号" = proxy_id
           AND "撤销人编号" = grantor_id
           AND "撤销原因" = '事务回滚离岗回收 smoke test'
    ) THEN
        RAISE EXCEPTION '批量回收审计字段检查失败';
    END IF;
    IF "fn_代理有效"(grantor_id, delegate_id, smoke_scope) THEN
        RAISE EXCEPTION '按人员批量回收后仍被判定有效';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_trigger
         WHERE tgname = 'trg_权限代理_禁止重叠'
    ) THEN
        RAISE EXCEPTION '代理并发保护触发器缺失';
    END IF;

    SELECT x."流程模板编号", x."项目编号", x."异常类型"
      INTO flow_id, project_id, anomaly_type
      FROM "tfn_预算异常对账"(NULL, NULL) x
     LIMIT 1;
    IF FOUND THEN
        disposition_id := "sp_预算异常对账_处理"(
            jsonb_build_object(
                '流程模板编号', flow_id,
                '项目编号', project_id,
                '异常类型', anomaly_type,
                '状态', '处理中',
                '责任人编号', delegate_id,
                '处理说明', '事务回滚 smoke test',
                '处理人编号', grantor_id
            )
        );
        IF disposition_id IS NULL OR disposition_id <= 0 THEN
            RAISE EXCEPTION '预算异常处理登记检查失败';
        END IF;
    END IF;
END;
$$;

ROLLBACK;
SELECT 'governance_smoke_ok';
SQL
