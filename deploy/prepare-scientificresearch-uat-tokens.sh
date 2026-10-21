#!/usr/bin/env bash
set -euo pipefail
umask 077
out='/private/tmp/scientificresearch-uat-tokens.env'
tmp="${out}.new.$$"
read_token() {
  local label="$1"
  local __name="$2"
  local value
  printf '%s' "$label"
  IFS= read -r -s value
  printf '\n已接收 %s（输入内容不会回显）\n' "$__name"
  printf -v "$__name" '%s' "$value"
}

echo '令牌输入为隐藏模式；每项粘贴后必须按回车，看到“已接收”再输入下一项。'
read_token '科研人员 token: ' RESEARCHER_TOKEN
read_token '科室负责人 token: ' DEPARTMENT_LEAD_TOKEN
read_token '科研秘书 token: ' SECRETARY_TOKEN
read_token '专家 token: ' EXPERT_TOKEN
read_token '财务管理员 token: ' FINANCE_TOKEN
read_token '系统管理员 token: ' SYSTEM_ADMIN_TOKEN
for value in "$RESEARCHER_TOKEN" "$DEPARTMENT_LEAD_TOKEN" "$SECRETARY_TOKEN" "$EXPERT_TOKEN" "$FINANCE_TOKEN" "$SYSTEM_ADMIN_TOKEN"; do
  case "$value" in
    eyJ*.*.*) ;;
    *) echo '输入中有一项不像 JWT，未写入文件' >&2; exit 2 ;;
  esac
done
{
  printf 'RESEARCHER_TOKEN=%s\n' "$RESEARCHER_TOKEN"
  printf 'DEPARTMENT_LEAD_TOKEN=%s\n' "$DEPARTMENT_LEAD_TOKEN"
  printf 'SECRETARY_TOKEN=%s\n' "$SECRETARY_TOKEN"
  printf 'EXPERT_TOKEN=%s\n' "$EXPERT_TOKEN"
  printf 'FINANCE_TOKEN=%s\n' "$FINANCE_TOKEN"
  printf 'SYSTEM_ADMIN_TOKEN=%s\n' "$SYSTEM_ADMIN_TOKEN"
} > "$tmp"
chmod 600 "$tmp"
mv "$tmp" "$out"
printf '已安全写入 %s（权限 600）\n' "$out"
