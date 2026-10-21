#!/usr/bin/env bash
set -euo pipefail

# Read-only release gate for the external adapter contract. It never prints
# credentials or sends requests to a supplier system.
database_container="${DATABASE_CONTAINER:-2018-7-24-db-1}"
database_name="${DATABASE_NAME:-ScientificResearch_Model}"
database_user="${DATABASE_USER:-postgres}"
app_container="${APP_CONTAINER:-2018-7-24-app-1}"
failures=0
warnings=0

case "${1:-}" in
  ''|'--dry-run') ;;
  '--help'|'-h')
    echo '用法: ./deploy/verify-external-adapter-contract.sh [--dry-run]'
    echo '仅检查数据库登记、应用容器密钥引用和队列契约，不发起外部网络请求。'
    exit 0
    ;;
  *)
    echo "用法错误: 仅支持 --dry-run（默认）" >&2
    exit 2
    ;;
esac

fail() {
  echo "外部适配器契约校验失败: $*" >&2
  failures=$((failures + 1))
}

warn() {
  echo "外部适配器契约预警: $*" >&2
  warnings=$((warnings + 1))
}

run_psql() {
  docker exec -i "$database_container" psql -U "$database_user" -d "$database_name" -AtF '|' "$@"
}

worker_enabled="$(docker inspect -f '{{range .Config.Env}}{{println .}}{{end}}' "$app_container" 2>/dev/null | awk -F= '$1 == "ExternalSync__Enabled" { print tolower($2); exit }')"
worker_enabled="${worker_enabled:-false}"
container_env="$(docker inspect -f '{{range .Config.Env}}{{println .}}{{end}}' "$app_container" 2>/dev/null || true)"
if [[ -z "$container_env" ]]; then
  if [[ "$worker_enabled" == 'true' ]]; then
    fail "worker 已启用但应用容器不可读取，无法确认认证密钥引用"
  else
    warn "应用容器不可读取，无法检查认证密钥引用（worker 当前关闭）"
  fi
fi
if [[ "$worker_enabled" != 'true' ]]; then
  echo 'ExternalSync worker 当前关闭：不会向外部系统发起请求'
fi
echo '干运行报告：只读检查数据库和容器配置，不发起外部网络请求；“通过”不代表供应商联调成功'
echo '认证密钥只检查 ExternalSystems__Secrets__<认证引用> 是否存在且非空，不输出密钥内容'

secret_available() {
  local reference="$1"
  [[ -z "$reference" ]] && return 0
  local key="ExternalSystems__Secrets__${reference}"
  printf '%s\n' "$container_env" | awk -F= -v wanted="$key" '$1 == wanted { value=substr($0, index($0, "=") + 1); if (value != "") found=1 } END { exit found ? 0 : 1 }'
}

valid_auth_reference() {
  local reference="$1"
  [[ "$reference" =~ ^[A-Za-z0-9_.-]{1,100}$ ]]
}

valid_address() {
  local address="$1"
  [[ "$address" =~ ^https?://[^[:space:]]+$ ]] || return 1
  if [[ "$address" =~ ^http:// ]] && ! local_address "$address"; then return 1; fi
  [[ "$address" != *'example.'* && "$address" != *'your-'* && "$address" != *'<host>'* && "$address" != *'CHANGE_ME'* ]]
}

local_address() {
  local address="$1"
  [[ "$address" =~ ^http://(localhost|127\.0\.0\.1|\[::1\])([/:]|$) ]]
}

idempotency_key_valid() {
  local key="$1"
  [[ -n "$key" && ${#key} -le 200 && ! "$key" =~ [[:space:][:cntrl:]] ]]
}

systems="$(run_psql <<'SQL'
SELECT "系统代码", "系统名称", "系统类型", "是否启用",
       COALESCE("基础地址",''), COALESCE("认证方式",'无'),
       COALESCE("认证引用",''), COALESCE("超时秒数",0), COALESCE("最大重试次数",-1)
  FROM "外部系统" ORDER BY "系统类型", "系统代码";
SQL
)"
required_types=(HR 财务 采购 伦理 合同 人才库 生物样本库 论文采集)
echo '--- 系统登记逐项报告 ---'
while IFS='|' read -r code name type enabled address auth auth_ref timeout retries; do
  [[ -z "$code" ]] && continue
  issues=()
  if ! valid_address "$address"; then issues+=("基础地址缺失/格式无效")
  elif [[ "$address" =~ ^http:// ]] && ! local_address "$address"; then issues+=("生产联调应使用 HTTPS")
  fi
  if [[ "$auth" != '无' ]]; then
    valid_auth_reference "$auth_ref" || issues+=("认证引用格式无效")
    if [[ "$worker_enabled" == 'true' ]] && ! secret_available "$auth_ref"; then issues+=("部署环境未注入认证引用")
    fi
  elif [[ -n "$auth_ref" ]]; then
    issues+=("认证方式为无但仍填写认证引用")
  fi
  [[ "$timeout" =~ ^[0-9]+$ && "$timeout" -ge 1 && "$timeout" -le 120 ]] || issues+=("超时秒数必须为1-120")
  [[ "$retries" =~ ^[0-9]+$ && "$retries" -ge 0 && "$retries" -le 10 ]] || issues+=("最大重试次数必须为0-10")
  if ((${#issues[@]} == 0)); then
    echo "${type}/${code}: ${enabled}，配置字段通过（密钥值未显示）"
  else
    echo "${type}/${code}: ${enabled}，${issues[*]}"
    if [[ "$enabled" == 't' || "$enabled" == 'true' ]]; then
      fail "启用的 ${type}/${code} 配置不完整: ${issues[*]}"
    else
      warn "停用的 ${type}/${code} 仍有配置问题: ${issues[*]}"
    fi
  fi
done <<< "$systems"

echo '--- 八类必需系统汇总 ---'
for type in "${required_types[@]}"; do
  type_lines="$(printf '%s\n' "$systems" | awk -F'|' -v wanted="$type" '$3 == wanted')"
  if [[ -z "$type_lines" ]]; then
    echo "${type}: 未登记"
    if [[ "$worker_enabled" == 'true' ]]; then fail "worker 已启用但 ${type} 未登记"; fi
  else
    total="$(printf '%s\n' "$type_lines" | awk 'NF { count++ } END { print count + 0 }')"
    enabled_count="$(printf '%s\n' "$type_lines" | awk -F'|' '$4 == "t" || $4 == "true" { count++ } END { print count + 0 }')"
    complete_count=0
    enabled_complete_count=0
    while IFS='|' read -r code name row_type enabled address auth auth_ref timeout retries; do
      [[ -z "$code" ]] && continue
      valid_address "$address" || continue
      if [[ "$auth" != '无' ]]; then
        valid_auth_reference "$auth_ref" || continue
        [[ "$worker_enabled" != 'true' ]] || secret_available "$auth_ref" || continue
      fi
      [[ "$timeout" =~ ^[0-9]+$ && "$timeout" -ge 1 && "$timeout" -le 120 ]] || continue
      [[ "$retries" =~ ^[0-9]+$ && "$retries" -ge 0 && "$retries" -le 10 ]] || continue
      complete_count=$((complete_count + 1))
      if [[ "$enabled" == 't' || "$enabled" == 'true' ]]; then
        enabled_complete_count=$((enabled_complete_count + 1))
      fi
    done <<< "$type_lines"
    echo "${type}: 已登记 ${total} 个，启用 ${enabled_count} 个，完整配置 ${complete_count} 个，启用且完整 ${enabled_complete_count} 个"
    if [[ "$enabled_count" -eq 0 ]]; then
      if [[ "$worker_enabled" == 'true' ]]; then fail "worker 已启用但 ${type} 没有启用的系统"; else warn "${type} 已登记但没有启用系统"; fi
    elif [[ "$enabled_count" -ne 1 || "$enabled_complete_count" -ne 1 ]]; then
      if [[ "$worker_enabled" == 'true' ]]; then
        fail "worker 已启用但 ${type} 必须有且仅有一个完整启用适配器（当前启用 ${enabled_count}，启用且完整 ${enabled_complete_count}）"
      else
        warn "${type} 必须保留唯一完整启用适配器（当前启用 ${enabled_count}，启用且完整 ${enabled_complete_count}）"
      fi
    fi
  fi
done

invalid_enabled="$(run_psql <<'SQL'
SELECT count(*)::integer
  FROM "外部系统"
 WHERE "是否启用"=TRUE
   AND ("基础地址" IS NULL OR btrim("基础地址") = ''
        OR "超时秒数" NOT BETWEEN 1 AND 120
        OR "最大重试次数" NOT BETWEEN 0 AND 10
        OR ("认证方式" <> '无' AND ("认证引用" IS NULL OR btrim("认证引用") = '')));
SQL
)"
if [[ "$invalid_enabled" != '0' ]]; then
  fail "有 ${invalid_enabled} 个已启用外部系统缺少必填字段或超时/重试参数无效"
fi

if [[ "$worker_enabled" == 'true' ]]; then
  invalid_tasks="$(run_psql <<'SQL'
SELECT count(*)::integer FROM "外部同步任务"
 WHERE "状态" IN ('待处理','处理中')
   AND (NULLIF(btrim("幂等键"),'') IS NULL OR length("幂等键") > 200
        OR "幂等键" ~ '[[:space:][:cntrl:]]'
        OR "最大重试次数" NOT BETWEEN 0 AND 10);
SQL
)"
  if [[ "$invalid_tasks" != '0' ]]; then fail "worker 已启用但队列中有 ${invalid_tasks} 个任务的幂等键或重试参数无效"; fi
else
  pending_tasks="$(run_psql <<'SQL'
SELECT count(*)::integer FROM "外部同步任务" WHERE "状态" IN ('待处理','处理中');
SQL
)"
  if [[ "$pending_tasks" != '0' ]]; then warn "worker 当前关闭，但队列仍有 ${pending_tasks} 个待处理/处理中任务；不会发起外部请求"; fi
fi

if (( failures > 0 )); then
  echo "外部适配器契约校验未通过: ${failures} 项" >&2
  exit 1
fi
echo "外部适配器契约预检通过（未发起外部请求；预警 ${warnings} 项）"
