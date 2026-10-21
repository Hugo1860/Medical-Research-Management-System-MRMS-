#!/usr/bin/env bash
set -euo pipefail
umask 077
out='/private/tmp/scientificresearch-uat-tokens.env'
tmp="${out}.new.$$"
command -v pbpaste >/dev/null 2>&1 || { echo '此脚本需要 macOS pbpaste 命令' >&2; exit 1; }
read_clipboard_token() {
  local label="$1"
  local name="$2"
  local value
  printf '%s' "$label"
  IFS= read -r _
  value="$(pbpaste | tr -d '\r\n')"
  case "$value" in
    eyJ*.*.*) ;;
    *) echo '剪贴板内容不像 JWT，请先在浏览器 Console 执行 copy(...) 后重试' >&2; exit 2 ;;
  esac
  printf '已接收 %s（%d 字符，内容不显示）\n' "$name" "${#value}"
  printf -v "$name" '%s' "$value"
}

echo '剪贴板采集模式：每次先在浏览器复制 token，再回到终端按回车。'
read_clipboard_token '科研人员：复制 token 后按回车: ' RESEARCHER_TOKEN
read_clipboard_token '科室负责人：复制 token 后按回车: ' DEPARTMENT_LEAD_TOKEN
read_clipboard_token '科研秘书：复制 token 后按回车: ' SECRETARY_TOKEN
read_clipboard_token '专家：复制 token 后按回车: ' EXPERT_TOKEN
read_clipboard_token '财务管理员：复制 token 后按回车: ' FINANCE_TOKEN
read_clipboard_token '系统管理员：复制 token 后按回车: ' SYSTEM_ADMIN_TOKEN
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
