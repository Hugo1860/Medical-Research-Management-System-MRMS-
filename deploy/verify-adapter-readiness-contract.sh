#!/usr/bin/env bash
set -euo pipefail

# Static-only gate for the real adapter readiness boundary.  It never reads a
# secret, connects to PostgreSQL, starts Docker, or calls a supplier endpoint.
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
register="$root/deploy/register-core-adapters.sh"
core_gate="$root/deploy/verify-core-adapter-config-contract.sh"
worker="$root/ScientificResearch/ScientificResearch/Infrastucture/ExternalSyncWorker.cs"
operations="$root/ScientificResearch/ScientificResearch/Controllers/OperationsController.cs"
compose="$root/docker-compose.yml"
env_example="$root/.env.docker.example"
template="$root/docs/external-adapter-config-template.md"
failures=0
fail() { echo "适配器就绪边界契约失败: $*" >&2; failures=$((failures + 1)); }

[[ -x "$register" ]] || fail '核心适配器登记脚本必须可执行'
[[ -x "$core_gate" ]] || fail '核心适配器配置契约门禁必须可执行'
for marker in \
  'external_sync_enabled="${EXTERNAL_SYNC_ENABLED:-false}"' \
  '登记脚本不会在 ExternalSync worker 已启用时写入适配器' \
  'operator_exists=' \
  'hr_conflict=' \
  'finance_conflict=' \
  'Keep psql variable interpolation outside dollar-quoted DO blocks' \
  '未读取、未输出'; do
  grep -Fq -- "$marker" "$register" || fail "登记脚本缺少 fail-closed 边界: $marker"
done

for marker in \
  'if (model.是否启用 && !地址有效(address))' \
  '已存在其他启用适配器' \
  'ApplyExternalAuthentication(request, authMode, authRef)' \
  'X-Request-ID' \
  'response.IsSuccessStatusCode' \
  '测试模式 = "dry-run"'; do
  grep -Fq -- "$marker" "$operations" || fail "运行管理接口缺少边界: $marker"
done

for marker in \
  'HasCompleteOutboundConfigurationAsync' \
  'RequiredSystemTypes' \
  'Idempotency-Key' \
  'X-Request-ID' \
  'MarkSuccessAsync' \
  'MarkFailureAsync' \
  'RetryAfter' \
  'ExternalSystems:Secrets:' \
  'External sync worker disabled by configuration'; do
  grep -Fq -- "$marker" "$worker" || fail "worker 缺少认证/回执/失败处理边界: $marker"
done

for marker in \
  'ExternalSync__Enabled: ${EXTERNAL_SYNC_ENABLED:-false}' \
  'ExternalSystems__Secrets__HIS_HR_TOKEN: ${HIS_HR_TOKEN:-}' \
  'ExternalSystems__Secrets__FINANCE_API_TOKEN: ${FINANCE_API_TOKEN:-}' \
  'ExternalSystems__Secrets__PROCUREMENT_API_TOKEN: ${PROCUREMENT_API_TOKEN:-}'; do
  grep -Fq -- "$marker" "$compose" || fail "Compose 缺少受控配置映射: $marker"
done

grep -Eq '^EXTERNAL_SYNC_ENABLED=false$' "$env_example" || fail '.env.docker.example 必须默认关闭 worker'
for marker in '真实地址' '真实回执' '幂等重放' 'ExternalSync__Enabled=false'; do
  grep -Fq -- "$marker" "$template" || fail "配置模板缺少真实联调边界: $marker"
done

if (( failures > 0 )); then
  echo "适配器就绪边界契约未通过: ${failures} 项" >&2
  exit 1
fi
echo '适配器就绪边界契约通过：地址/HTTPS、认证引用、唯一启用、worker 开关、幂等请求、2xx 回执与失败重试边界均已静态固化。'
