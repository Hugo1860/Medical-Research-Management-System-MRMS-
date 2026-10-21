#!/usr/bin/env bash
set -euo pipefail

# Read-only validation for the Docker .env file. Values are never printed.
env_file="${ENV_FILE:-.env}"
check_redis="${CHECK_REDIS:-false}"
failures=0

fail() {
  echo "Docker 环境校验失败: $*" >&2
  failures=$((failures + 1))
}

if [[ ! -f "$env_file" ]]; then
  fail "环境文件不存在: ${env_file}"
else
  read_value() {
    local key="$1" value
    value="$(awk -F= -v key="$key" '
      $0 !~ /^[[:space:]]*#/ && $1 == key {
        sub(/^[^=]*=/, ""); print; exit
      }' "$env_file")"
    value="${value#\"}"; value="${value%\"}"
    value="${value#\'}"; value="${value%\'}"
    printf '%s' "$value"
  }

  validate_secret() {
    local key="$1" value
    value="$(read_value "$key")"
    if [[ -z "$value" ]]; then
      fail "${key} 未设置"
      return
    fi
    if [[ "$value" == *'请替换'* || "$value" == 'change-me' || "$value" == 'password' || "$value" == 'postgres' || "$value" == 'postgres_password' ]]; then
      fail "${key} 仍使用占位或默认密码"
      return
    fi
    if (( ${#value} < 16 )); then
      fail "${key} 长度不足 16 个字符"
    fi
  }

  validate_secret POSTGRES_PASSWORD
  if [[ "$check_redis" == 'true' ]]; then
    validate_secret REDIS_PASSWORD
  fi
fi

if (( failures > 0 )); then
  echo "Docker 环境校验未通过: ${failures} 项" >&2
  exit 1
fi
echo "Docker 环境校验通过: ${env_file}（密码未输出）"
