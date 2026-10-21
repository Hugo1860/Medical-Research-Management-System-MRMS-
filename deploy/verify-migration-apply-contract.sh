#!/usr/bin/env bash
set -euo pipefail

# Static-only contract for applying PostgreSQL migrations. It does not connect
# to Docker or PostgreSQL and cannot claim that migrations are applied.
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
apply="$root/deploy/apply-postgres-migrations.sh"
verify="$root/database/postgresql/verify_migration.sh"
runbook="$root/docs/production-uat-runbook.md"
release="$root/deploy/verify-local-release.sh"
failures=0
fail() { echo "迁移应用契约失败: $*" >&2; failures=$((failures + 1)); }

for path in "$apply" "$verify" "$runbook" "$release"; do
  [[ -f "$path" ]] || fail "缺少 $path"
done

for marker in \
  'docker exec -i "$database_container" psql' \
  'pg_advisory_lock' \
  'pg_advisory_unlock' \
  'ON_ERROR_STOP=1' \
  'DRY_RUN' \
  '目标版本' \
  '拒绝降级' \
  '迁移序列不连续' \
  '执行后版本不匹配' \
  'dry-run 完成，数据库未修改'; do
  grep -Fq -- "$marker" "$apply" || fail "应用脚本缺少安全边界: $marker"
done

for marker in \
  'EXPECTED_SCHEMA_VERSION=${EXPECTED_SCHEMA_VERSION:-106}' \
  'latest_migration_version != EXPECTED_SCHEMA_VERSION' \
  'schema_version_ok' \
  '横向项目生命周期申请' \
  '横向项目研究报告' \
  '横向项目档案材料' \
  '伦理审查会议委员' \
  '伦理电子档案' \
  '实际校验状态' \
  '是否匿名表决' \
  '进修年度计划' \
  '进修申请' \
  '进修审批流程配置' \
  '科研平台' \
  '科研服务申请' \
  '院级项目申报批次' \
  '到账费用提取方案' \
  '伦理 P0 治理迁移 089 校验失败'; do
  grep -Fq -- "$marker" "$verify" || fail "运行库验收脚本缺少 089-091 版本校验: $marker"
done

grep -Fq '先完成备份，再执行' "$runbook" || fail '运行手册未要求迁移前完成备份'
grep -Fq './deploy/verify-migration-apply-contract.sh' "$release" || fail '统一发布门禁未纳入迁移应用契约'

if (( failures > 0 )); then
  echo "迁移应用契约未通过: ${failures} 项" >&2
  exit 1
fi
echo '迁移应用契约静态校验通过: 备份前置、连续版本、同会话锁、逐步核对、失败停止和 dry-run 边界已固化（未连接运行库）'
