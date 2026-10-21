#!/usr/bin/env bash
set -euo pipefail

# Read-only finance/HRP contract preflight. It never creates invoices, ledger
# rows, external tasks, or sample payments, and never calls a supplier URL.
database_container="${DATABASE_CONTAINER:-2018-7-24-db-1}"
database_name="${DATABASE_NAME:-ScientificResearch_Model}"
database_user="${DATABASE_USER:-postgres}"
app_container="${APP_CONTAINER:-2018-7-24-app-1}"
strict="${FINANCE_CONTRACT_STRICT:-false}"
failures=0
warnings=0

case "${1:-}" in
  ''|'--dry-run') ;;
  '--strict') strict=true ;;
  '--help'|'-h')
    echo '用法: ./deploy/verify-finance-adapter-contract.sh [--dry-run|--strict]'
    echo '只读检查 HRP/财务/采购适配器、发票 OCR、采购订单关联、预算调整和付款回写契约。'
    exit 0
    ;;
  *) echo '用法错误: 仅支持 --dry-run（默认）或 --strict' >&2; exit 2 ;;
esac

fail() { echo "经费适配器契约失败: $*" >&2; failures=$((failures + 1)); }
warn() { echo "经费适配器契约预警: $*" >&2; warnings=$((warnings + 1)); }

if ! command -v timeout >/dev/null 2>&1; then
  echo '经费适配器契约校验需要 timeout 命令' >&2
  exit 1
fi
run_psql() {
  timeout --foreground --kill-after=5 "${DOCKER_COMMAND_TIMEOUT_SECONDS:-60}" docker exec -i "$database_container" \
    psql -q -v ON_ERROR_STOP=1 -U "$database_user" -d "$database_name" "$@"
}

worker_enabled="$(docker inspect -f '{{range .Config.Env}}{{println .}}{{end}}' "$app_container" 2>/dev/null \
  | awk -F= '$1 == "ExternalSync__Enabled" { print tolower($2); exit }' || true)"
worker_enabled="${worker_enabled:-false}"
container_env="$(docker inspect -f '{{range .Config.Env}}{{println .}}{{end}}' "$app_container" 2>/dev/null || true)"
secret_available() {
  local reference="$1"
  [[ -z "$reference" ]] && return 0
  local key="ExternalSystems__Secrets__${reference}"
  printf '%s\n' "$container_env" | awk -F= -v wanted="$key" '$1 == wanted { value=substr($0, index($0, "=") + 1); if (value != "") found=1 } END { exit found ? 0 : 1 }'
}
valid_auth_reference() { [[ "$1" =~ ^[A-Za-z0-9_.-]{1,100}$ ]]; }
local_address() { [[ "$1" =~ ^http://(localhost|127\.0\.0\.1|\[::1\])([/:]|$) ]]; }
valid_address() {
  local address="$1"
  [[ "$address" =~ ^https?://[^[:space:]]+$ ]] || return 1
  if [[ "$address" =~ ^http:// ]] && ! local_address "$address"; then return 1; fi
  [[ "$address" != *'example.'* && "$address" != *'your-'* && "$address" != *'<host>'* && "$address" != *'CHANGE_ME'* ]]
}
valid_auth() {
  local mode="$1" ref="$2"
  if [[ "$mode" == '无' ]]; then [[ -z "$ref" ]]; else valid_auth_reference "$ref"; fi
}
valid_params() { [[ "$1" =~ ^[0-9]+$ && "$1" -ge 1 && "$1" -le 120 && "$2" =~ ^[0-9]+$ && "$2" -ge 0 && "$2" -le 10 ]]; }

if [[ -z "$container_env" && "$worker_enabled" == 'true' ]]; then
  fail 'worker 已启用但应用容器不可读取，无法验证认证引用'
elif [[ "$worker_enabled" != 'true' ]]; then
  echo 'ExternalSync worker 当前关闭：本检查不会发起外部请求'
fi

echo '--- HR/财务/采购适配器逐项检查 ---'
systems="$(run_psql -AtF '|' <<'SQL'
SELECT "系统代码", "系统类型", CASE WHEN "是否启用" THEN 'true' ELSE 'false' END,
       COALESCE("基础地址",''), COALESCE("认证方式",'无'), COALESCE("认证引用",''),
       COALESCE("超时秒数",0), COALESCE("最大重试次数",-1)
  FROM "外部系统" WHERE "系统类型" IN ('HR','财务','采购')
 ORDER BY "系统类型", "系统代码";
SQL
)"
required_types=(HR 财务 采购)
for type in "${required_types[@]}"; do
  total=0; enabled=0; complete=0; enabled_complete=0
  while IFS='|' read -r code row_type is_enabled address auth auth_ref timeout retries; do
    [[ -z "$code" ]] && continue
    [[ "$row_type" == "$type" ]] || continue
    total=$((total + 1))
    [[ "$is_enabled" == 'true' ]] && enabled=$((enabled + 1))
    issues=()
    valid_address "$address" || issues+=(地址)
    valid_auth "$auth" "$auth_ref" || issues+=(认证引用)
    if [[ "$auth" != '无' && "$worker_enabled" == 'true' ]] && ! secret_available "$auth_ref"; then issues+=(密钥未注入); fi
    valid_params "$timeout" "$retries" || issues+=(超时重试)
    if ((${#issues[@]} == 0)); then
      complete=$((complete + 1))
      [[ "$is_enabled" == 'true' ]] && enabled_complete=$((enabled_complete + 1))
    fi
    if ((${#issues[@]} > 0)); then
      if [[ "$is_enabled" == 'true' ]]; then fail "启用的 ${type}/${code} 配置不完整: ${issues[*]}"; else warn "停用的 ${type}/${code} 配置不完整: ${issues[*]}"; fi
    fi
  done <<< "$systems"
  echo "${type}: 已登记 ${total}，启用 ${enabled}，完整 ${complete}，启用且完整 ${enabled_complete}"
  if [[ "$enabled" -ne 1 || "$enabled_complete" -ne 1 ]]; then
    if [[ "$strict" == 'true' || "$worker_enabled" == 'true' ]]; then fail "${type} 必须有且仅有一个启用且完整的适配器"; else warn "${type} 尚未达到唯一启用且完整适配器要求"; fi
  fi
done

flags="$(run_psql -AtF '|' <<'SQL'
SELECT
  (to_regclass('public."hrp_sync_project"') IS NOT NULL),
  (to_regclass('public."hrp_sync_budget"') IS NOT NULL),
  (to_regclass('public."hrp_sync_incoming"') IS NOT NULL),
  (to_regclass('public."hrp_sync_expense"') IS NOT NULL),
  (to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)') IS NOT NULL),
  (to_regclass('public."科研经费发票"') IS NOT NULL),
  (to_regclass('public."项目经费预算调整申请"') IS NOT NULL),
  (to_regclass('public."项目经费预算版本"') IS NOT NULL),
  (to_regclass('public."项目经费年度结算"') IS NOT NULL),
  (to_regclass('public."项目经费台账"') IS NOT NULL),
  EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='科研经费发票' AND column_name='OCR状态'),
  EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name IN ('科研经费发票','报销','项目经费台账') AND column_name IN ('采购订单编号','采购订单号','采购订单ID')),
  EXISTS (SELECT 1 FROM pg_proc p WHERE p.proname='sp_hrp中间库双向协同'
          AND (pg_get_functiondef(p.oid) ~ '(模拟|HRP-VOUCHER|管理员.*1580|push_and_replay|replay)'
               OR pg_get_functiondef(p.oid) NOT ILIKE '%push_only%'));
SQL
)"
IFS='|' read -r hrp_project hrp_budget hrp_incoming hrp_expense hrp_proc invoice budget_adjust budget_version annual_settlement ledger invoice_ocr purchase_link fake_logic <<< "$flags"
[[ "$hrp_project" == 't' && "$hrp_budget" == 't' && "$hrp_incoming" == 't' && "$hrp_expense" == 't' ]] || fail 'HRP 四类中间表不完整'
[[ "$hrp_proc" == 't' ]] || fail 'HRP 协同过程不存在'
[[ "$invoice" == 't' && "$invoice_ocr" == 't' ]] || fail '科研经费发票表或 OCR 状态字段不存在'
[[ "$budget_adjust" == 't' && "$budget_version" == 't' ]] || fail '预算调整申请/版本表不完整'
[[ "$annual_settlement" == 't' && "$ledger" == 't' ]] || fail '年度结算或付款台账不存在'
if [[ "$purchase_link" != 't' ]]; then
  if [[ "$strict" == 'true' ]]; then fail '发票/报销/付款台账尚无采购订单稳定编号字段'; else warn '发票/报销/付款台账尚无采购订单稳定编号字段'; fi
fi
if [[ "$fake_logic" == 't' ]]; then
  fail '运行库 HRP 协同过程仍包含历史示例付款逻辑；禁止真实回放'
fi

synthetic_rows=0
if [[ "$hrp_expense" == 't' ]]; then
  synthetic_rows="$(run_psql -At <<'SQL'
SELECT count(*) FROM "hrp_sync_expense"
 WHERE "hrp_voucher_no" LIKE 'HRP-VOUCHER-%'
    OR ("claim_id"=101 AND "applicant_name"='管理员' AND "amount"=1580.00);
SQL
)"
fi
if [[ "$synthetic_rows" != '0' ]]; then fail "发现 ${synthetic_rows} 条疑似示例 HRP 支出流水，禁止作为真实联调证据"; fi

if [[ "$invoice" == 't' ]]; then
  echo '--- 发票 OCR 状态分布（只读） ---'
  run_psql -AtF '|' <<'SQL' | sed 's/^/OCR状态: /'
SELECT "OCR状态", count(*)::bigint FROM "科研经费发票" GROUP BY "OCR状态" ORDER BY "OCR状态";
SQL
fi

echo "采购订单关联字段: ${purchase_link}；HRP 过程示例逻辑: ${fake_logic}；疑似示例流水: ${synthetic_rows}"
if [[ "$failures" -gt 0 ]]; then
  if [[ "$strict" == 'true' ]]; then
    echo "经费适配器契约严格校验未通过: ${failures} 项" >&2
  else
    echo "经费适配器契约预检发现阻断项: ${failures} 项（示例逻辑/结构问题即使默认模式也阻断）" >&2
  fi
  exit 1
fi
echo "经费/HRP 适配器契约预检完成（预警 ${warnings} 项；未发起外部请求）"
