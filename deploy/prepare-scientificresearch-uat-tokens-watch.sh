#!/usr/bin/env bash
set -euo pipefail
umask 077
out='/private/tmp/scientificresearch-uat-tokens.env'
tmp="${out}.new.$$"
progress_dir='/private/tmp/scientificresearch-uat-token-progress'
mkdir -p "$progress_dir"
chmod 700 "$progress_dir"
command -v pbpaste >/dev/null 2>&1 || { echo '此脚本需要 macOS pbpaste 命令' >&2; exit 1; }
clipboard() { (pbpaste 2>/dev/null || true) | tr -d '\r\n'; }
wait_clipboard_token() {
  local label="$1"
  local name="$2"
  local previous current
  previous="$(clipboard)"
  printf '%s\n' "$label"
  while :; do
    current="$(clipboard)"
    if [[ "$current" != "$previous" && "$current" == eyJ*.*.* ]]; then
      printf '已接收 %s（%d 字符，内容不显示）\n' "$name" "${#current}"
      printf -v "$name" '%s' "$current"
      printf '%s\n' "$current" > "$progress_dir/$name"
      chmod 600 "$progress_dir/$name"
      return 0
    fi
    sleep 0.5
  done
}

echo '剪贴板监听模式：复制 token 后无需按回车，脚本会自动继续。'
wait_clipboard_token '请在浏览器复制“科研人员” token：' RESEARCHER_TOKEN
wait_clipboard_token '请切换账号并复制“科室负责人” token：' DEPARTMENT_LEAD_TOKEN
wait_clipboard_token '请切换账号并复制“科研秘书” token：' SECRETARY_TOKEN
wait_clipboard_token '请切换账号并复制“专家” token：' EXPERT_TOKEN
wait_clipboard_token '请切换账号并复制“财务管理员” token：' FINANCE_TOKEN
wait_clipboard_token '请切换账号并复制“系统管理员” token：' SYSTEM_ADMIN_TOKEN
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
