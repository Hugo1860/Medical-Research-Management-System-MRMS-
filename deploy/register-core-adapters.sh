#!/usr/bin/env bash
set -euo pipefail

# Stage exactly one HR and one finance adapter registration. This helper never
# receives or prints a secret and never enables ExternalSync. It defaults to a
# local dry-run; --apply requires an explicit confirmation and only writes the
# two registration rows after validating their addresses and references.
database_container="${DATABASE_CONTAINER:-2018-7-24-db-1}"
database_name="${DATABASE_NAME:-ScientificResearch_Model}"
database_user="${DATABASE_USER:-postgres}"
mode="dry-run"

case "${1:-}" in
  ''|'--dry-run') ;;
  '--apply') mode='apply' ;;
  '--help'|'-h')
    cat <<'USAGE'
用法: ./deploy/register-core-adapters.sh [--dry-run|--apply]

环境变量（地址和认证引用必填；不读取密钥值）：
  HR_ADAPTER_ENDPOINT       HR 适配器 HTTPS 地址
  FINANCE_ADAPTER_ENDPOINT  财务适配器 HTTPS 地址
  HR_ADAPTER_AUTH_REF       默认 HIS_HR_TOKEN
  FINANCE_ADAPTER_AUTH_REF  默认 FINANCE_API_TOKEN
  HR_ADAPTER_AUTH_MODE / FINANCE_ADAPTER_AUTH_MODE（默认 Bearer）
  CORE_ADAPTERS_ENABLED     默认 false；仅控制两行“是否启用”，不启用 worker
  ADAPTER_OPERATOR_ID       --apply 时必填，必须是启用的本地人员编号
  ALLOW_HTTP_EXTERNAL_ADAPTER=true 仅允许 localhost/127.0.0.1 本机联调
  CONFIRM_EXTERNAL_ADAPTER_WRITE=YES  --apply 时必填

默认只校验，不连接数据库、不发起供应商请求。--apply 只登记 HR/财务
系统，保留 ExternalSync__Enabled=false；八类系统完成真实回执验收后才可
由单独的受控变更开启 worker。
USAGE
    exit 0
    ;;
  *) echo '用法错误：仅支持 --dry-run（默认）、--apply 或 --help' >&2; exit 2 ;;
esac

hr_endpoint="${HR_ADAPTER_ENDPOINT:-}"
finance_endpoint="${FINANCE_ADAPTER_ENDPOINT:-}"
hr_code="${HR_ADAPTER_SYSTEM_CODE:-HIS-HR}"
finance_code="${FINANCE_ADAPTER_SYSTEM_CODE:-HIS-FINANCE}"
hr_name="${HR_ADAPTER_SYSTEM_NAME:-医院人事适配器}"
finance_name="${FINANCE_ADAPTER_SYSTEM_NAME:-医院财务适配器}"
hr_mode="${HR_ADAPTER_AUTH_MODE:-Bearer}"
finance_mode="${FINANCE_ADAPTER_AUTH_MODE:-Bearer}"
hr_ref="${HR_ADAPTER_AUTH_REF:-HIS_HR_TOKEN}"
finance_ref="${FINANCE_ADAPTER_AUTH_REF:-FINANCE_API_TOKEN}"
enabled="${CORE_ADAPTERS_ENABLED:-false}"
allow_http="${ALLOW_HTTP_EXTERNAL_ADAPTER:-false}"
operator_id="${ADAPTER_OPERATOR_ID:-}"
external_sync_enabled="${EXTERNAL_SYNC_ENABLED:-false}"

fail() { echo "核心适配器登记失败: $*" >&2; exit 1; }

[[ -n "$hr_endpoint" ]] || fail 'HR_ADAPTER_ENDPOINT 未设置'
[[ -n "$finance_endpoint" ]] || fail 'FINANCE_ADAPTER_ENDPOINT 未设置'
[[ "$enabled" == 'true' || "$enabled" == 'false' ]] || fail 'CORE_ADAPTERS_ENABLED 必须为 true 或 false'
[[ "$allow_http" == 'true' || "$allow_http" == 'false' ]] || fail 'ALLOW_HTTP_EXTERNAL_ADAPTER 必须为 true 或 false'
[[ "$external_sync_enabled" == 'true' || "$external_sync_enabled" == 'false' ]] || fail 'EXTERNAL_SYNC_ENABLED 必须为 true 或 false'
[[ "$external_sync_enabled" == 'false' ]] || fail '登记脚本不会在 ExternalSync worker 已启用时写入适配器；请先关闭 worker 并完成八类系统验收'
[[ "$hr_mode" =~ ^(Bearer|Basic|ApiKey|OIDC|无)$ ]] || fail 'HR_ADAPTER_AUTH_MODE 不受支持'
[[ "$finance_mode" =~ ^(Bearer|Basic|ApiKey|OIDC|无)$ ]] || fail 'FINANCE_ADAPTER_AUTH_MODE 不受支持'
if [[ "$hr_mode" == '无' ]]; then [[ -z "$hr_ref" ]] || fail 'HR 认证方式为无时认证引用必须为空'; else [[ "$hr_ref" =~ ^[A-Za-z0-9_.-]{1,100}$ ]] || fail 'HR_ADAPTER_AUTH_REF 格式无效'; fi
if [[ "$finance_mode" == '无' ]]; then [[ -z "$finance_ref" ]] || fail '财务认证方式为无时认证引用必须为空'; else [[ "$finance_ref" =~ ^[A-Za-z0-9_.-]{1,100}$ ]] || fail 'FINANCE_ADAPTER_AUTH_REF 格式无效'; fi

validate_endpoint() {
  local label="$1" endpoint="$2"
  python3 - "$label" "$endpoint" "$allow_http" <<'PY'
import sys
from urllib.parse import urlparse
label, raw, allow_http = sys.argv[1:]
parsed = urlparse(raw)
if parsed.scheme not in ('http', 'https') or not parsed.netloc or any(ch.isspace() for ch in raw):
    raise SystemExit(f'{label} 必须是完整的 http/https URL')
if any(marker in raw.lower() for marker in ('example.', 'change_me', '<host>', 'your-')):
    raise SystemExit(f'{label} 仍包含占位地址')
if parsed.scheme == 'http':
    local = parsed.hostname in ('localhost', '127.0.0.1', '::1')
    if allow_http != 'true' or not local:
        raise SystemExit(f'{label} 生产环境必须使用 HTTPS；HTTP 仅可在显式本机联调时使用')
PY
}
validate_endpoint 'HR_ADAPTER_ENDPOINT' "$hr_endpoint"
validate_endpoint 'FINANCE_ADAPTER_ENDPOINT' "$finance_endpoint"

if [[ "$mode" == 'dry-run' ]]; then
  cat <<REPORT
核心适配器登记 dry-run 通过（未访问数据库、未发起外部请求）：
  HR: ${hr_code} / ${hr_mode} / 引用 ${hr_ref:-无} / 是否启用 ${enabled}
  财务: ${finance_code} / ${finance_mode} / 引用 ${finance_ref:-无} / 是否启用 ${enabled}
  worker: 保持关闭；密钥值未读取、未输出
REPORT
  exit 0
fi

[[ "$CONFIRM_EXTERNAL_ADAPTER_WRITE" == 'YES' ]] || fail '--apply 必须设置 CONFIRM_EXTERNAL_ADAPTER_WRITE=YES'
[[ "$operator_id" =~ ^[1-9][0-9]*$ ]] || fail '--apply 必须设置有效的 ADAPTER_OPERATOR_ID'

if ! command -v timeout >/dev/null 2>&1; then fail '需要 timeout 命令'; fi
docker_timeout="${DOCKER_COMMAND_TIMEOUT_SECONDS:-60}"
run_psql() {
  timeout --foreground --kill-after=5 "$docker_timeout" docker exec -i "$database_container" \
    psql -q -v ON_ERROR_STOP=1 -U "$database_user" -d "$database_name" "$@"
}

# Keep psql variable interpolation outside dollar-quoted DO blocks.  psql does
# not expand :name references inside a dollar-quoted body, so validating here
# makes --apply fail closed instead of silently using a literal placeholder.
operator_exists="$(run_psql -At -v operator_id="$operator_id" -c 'SELECT EXISTS (SELECT 1 FROM "人员" WHERE "编号"=:operator_id::integer AND "是否启用"=TRUE);')"
[[ "$operator_exists" == 't' ]] || fail 'ADAPTER_OPERATOR_ID 必须对应启用的本地人员'
if [[ "$enabled" == 'true' ]]; then
  hr_conflict="$(run_psql -At -v hr_code="$hr_code" -c "SELECT EXISTS (SELECT 1 FROM \"外部系统\" WHERE \"系统类型\"='HR' AND \"是否启用\"=TRUE AND \"系统代码\"<>:hr_code);")"
  [[ "$hr_conflict" == 'f' ]] || fail '已有其他启用 HR 适配器，拒绝重复启用'
  finance_conflict="$(run_psql -At -v finance_code="$finance_code" -c "SELECT EXISTS (SELECT 1 FROM \"外部系统\" WHERE \"系统类型\"='财务' AND \"是否启用\"=TRUE AND \"系统代码\"<>:finance_code);")"
  [[ "$finance_conflict" == 'f' ]] || fail '已有其他启用财务适配器，拒绝重复启用'
fi

run_psql -v operator_id="$operator_id" -v hr_code="$hr_code" -v hr_name="$hr_name" \
  -v hr_endpoint="$hr_endpoint" -v hr_mode="$hr_mode" -v hr_ref="$hr_ref" -v hr_enabled="$enabled" \
  -v finance_code="$finance_code" -v finance_name="$finance_name" -v finance_endpoint="$finance_endpoint" \
  -v finance_mode="$finance_mode" -v finance_ref="$finance_ref" -v finance_enabled="$enabled" <<'SQL'
BEGIN;
SELECT CASE WHEN EXISTS (SELECT 1 FROM "外部系统" WHERE "系统代码"=:'hr_code' AND "系统类型"<>'HR')
            THEN 1/0 ELSE 1 END;
SELECT CASE WHEN EXISTS (SELECT 1 FROM "外部系统" WHERE "系统代码"=:'finance_code' AND "系统类型"<>'财务')
            THEN 1/0 ELSE 1 END;

INSERT INTO "外部系统" ("系统代码","系统名称","系统类型","基础地址","认证方式","认证引用","是否启用","超时秒数","最大重试次数","建立人编号")
VALUES (:'hr_code',:'hr_name','HR',:'hr_endpoint',:'hr_mode',NULLIF(:'hr_ref',''),:'hr_enabled'::boolean,30,3,:'operator_id'::integer)
ON CONFLICT ("系统代码") DO UPDATE SET
  "系统名称"=EXCLUDED."系统名称", "系统类型"=EXCLUDED."系统类型", "基础地址"=EXCLUDED."基础地址",
  "认证方式"=EXCLUDED."认证方式", "认证引用"=EXCLUDED."认证引用", "是否启用"=EXCLUDED."是否启用",
  "超时秒数"=EXCLUDED."超时秒数", "最大重试次数"=EXCLUDED."最大重试次数", "更新时间"=CURRENT_TIMESTAMP;

INSERT INTO "外部系统" ("系统代码","系统名称","系统类型","基础地址","认证方式","认证引用","是否启用","超时秒数","最大重试次数","建立人编号")
VALUES (:'finance_code',:'finance_name','财务',:'finance_endpoint',:'finance_mode',NULLIF(:'finance_ref',''),:'finance_enabled'::boolean,30,3,:'operator_id'::integer)
ON CONFLICT ("系统代码") DO UPDATE SET
  "系统名称"=EXCLUDED."系统名称", "系统类型"=EXCLUDED."系统类型", "基础地址"=EXCLUDED."基础地址",
  "认证方式"=EXCLUDED."认证方式", "认证引用"=EXCLUDED."认证引用", "是否启用"=EXCLUDED."是否启用",
  "超时秒数"=EXCLUDED."超时秒数", "最大重试次数"=EXCLUDED."最大重试次数", "更新时间"=CURRENT_TIMESTAMP;
COMMIT;
SQL

echo '核心 HR/财务适配器登记完成：只写入地址、认证引用和参数，未读取或写入密钥，ExternalSync worker 仍需保持关闭。'
