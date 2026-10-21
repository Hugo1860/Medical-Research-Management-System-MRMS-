#!/usr/bin/env bash
set -euo pipefail

# Static-only gate for the first real HR/finance adapter acceptance window.
# It does not inspect a database, read secrets, call a supplier, or enable the
# worker. Runtime configuration is checked later by the read-only adapter and
# finance contract scripts after Docker is restored.
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
compose="$root/docker-compose.yml"
env_example="$root/.env.docker.example"
register="$root/deploy/register-core-adapters.sh"
template="$root/docs/external-adapter-config-template.md"
failures=0
fail() { echo "核心适配器配置契约失败: $*" >&2; failures=$((failures + 1)); }

[[ -x "$register" ]] || fail 'register-core-adapters.sh 必须可执行'
for marker in \
  "--apply" \
  "CONFIRM_EXTERNAL_ADAPTER_WRITE=YES" \
  "未读取、未输出" \
  "ExternalSync worker 仍需保持关闭" \
  "生产环境必须使用 HTTPS" \
  "拒绝重复启用"; do
  grep -Fq -- "$marker" "$register" || fail "登记脚本缺少边界: $marker"
done

for marker in \
  'ExternalSync__Enabled: ${EXTERNAL_SYNC_ENABLED:-false}' \
  'ExternalSystems__Secrets__HIS_HR_TOKEN: ${HIS_HR_TOKEN:-}' \
  'ExternalSystems__Secrets__FINANCE_API_TOKEN: ${FINANCE_API_TOKEN:-}' \
  'Finance__InvoiceOcr__Enabled: ${FINANCE_INVOICE_OCR_ENABLED:-false}'; do
  grep -Fq -- "$marker" "$compose" || fail "docker-compose.yml 缺少受控配置映射: $marker"
done

grep -Eq '^EXTERNAL_SYNC_ENABLED=false$' "$env_example" || fail '.env.docker.example 必须默认关闭 worker'
grep -Eq '^FINANCE_INVOICE_OCR_ENABLED=false$' "$env_example" || fail '.env.docker.example 必须默认关闭真实 OCR'
for marker in \
  'HIS_HR_TOKEN=' \
  'FINANCE_API_TOKEN=' \
  'ExternalSync 默认关闭' \
  '值只在受控部署环境注入'; do
  grep -Fq -- "$marker" "$env_example" || fail ".env.docker.example 缺少安全说明: $marker"
done

for marker in \
  '真实地址' \
  '真实回执' \
  'ExternalSync__Enabled=false' \
  'HIS_HR_TOKEN' \
  'FINANCE_API_TOKEN'; do
  grep -Fq -- "$marker" "$template" || fail "外部适配器模板缺少验收边界: $marker"
done

if (( failures > 0 )); then
  echo "核心适配器配置契约未通过: ${failures} 项" >&2
  exit 1
fi
echo '核心适配器配置契约通过：HR/财务登记脚本默认 dry-run，地址需 HTTPS，密钥仅由部署环境注入，worker 默认关闭。'
