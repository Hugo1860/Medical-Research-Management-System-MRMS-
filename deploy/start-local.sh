#!/usr/bin/env bash
set -euo pipefail

# Canonical local startup for the modern frontend/API acceptance environment.
# The Keycloak overlay is mandatory here: starting only docker-compose.yml is
# intentionally outside this contract because it can hide the SSO entry point.
script_dir="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
repo_dir="$(CDPATH= cd -- "$script_dir/.." && pwd)"
cd "$repo_dir"

compose_args=(-f docker-compose.yml -f docker-compose.published.yml -f deploy/docker-compose.keycloak.local.yml)
expected_authority="${EXPECTED_KEYCLOAK_AUTHORITY:-http://keycloak.localhost:8180/realms/scientific-research}"

if [[ "${SKIP_DOCKER_ENV_CHECK:-false}" != 'true' ]]; then
  ENV_FILE="${ENV_FILE:-.env}" CHECK_REDIS="${CHECK_REDIS:-false}" ./deploy/verify-docker-env.sh
fi

if ! command docker version >/dev/null 2>&1; then
  echo '本地启动失败: Docker Server 不可用，请先启动 Docker Desktop' >&2
  exit 1
fi

merged_config="$(docker compose "${compose_args[@]}" config)"
if ! grep -Eq '^[[:space:]]+Keycloak__Enabled: "true"$' <<<"$merged_config"; then
  echo '本地启动失败: 合并后的 Compose 配置未强制 Keycloak__Enabled=true' >&2
  exit 1
fi
if ! grep -Fq "Keycloak__Authority: ${expected_authority}" <<<"$merged_config"; then
  echo "本地启动失败: 合并后的 Compose 配置缺少预期 Keycloak Authority: ${expected_authority}" >&2
  exit 1
fi
if ! grep -Eq '^[[:space:]]+Keycloak__RedirectUri: .+/login$' <<<"$merged_config"; then
  echo '本地启动失败: 合并后的 Compose 配置缺少 /login 回调地址' >&2
  exit 1
fi
echo '本地认证模式契约: Keycloak=true，Authority 和 /login 回调已配置'

up_args=(up -d --force-recreate app db)
if [[ "${BUILD_APP:-false}" == 'true' ]]; then
  up_args+=(--build)
else
  up_args+=(--no-build)
fi
docker compose "${compose_args[@]}" "${up_args[@]}"

# Compose reports the process as started before Kestrel has bound its port.
# Wait for the live probe here so a normal warm-up is not reported as a false
# authentication failure by the stricter runtime gate below.
ready_timeout="${STARTUP_READY_TIMEOUT_SECONDS:-90}"
ready_url="${APP_BASE_URL:-http://127.0.0.1:${APP_HOST_PORT:-16379}}/health/live"
ready_started_at="$(date +%s)"
while true; do
  ready_code="$(command curl --max-time 2 --silent --output /dev/null --write-out '%{http_code}' "$ready_url" 2>/dev/null || true)"
  [[ "$ready_code" == '200' ]] && break
  if (( $(date +%s) - ready_started_at >= ready_timeout )); then
    echo "本地启动失败: 应用在 ${ready_timeout}s 内未通过 live 探针 (${ready_url})" >&2
    docker compose "${compose_args[@]}" ps >&2 || true
    exit 1
  fi
  sleep 2
done
echo "应用 live 探针已就绪 (${ready_url})"

KEYCLOAK_LOCAL_OVERLAY=true \
REQUIRE_KEYCLOAK=true \
CHECK_KEYCLOAK_DISCOVERY=true \
EXPECTED_KEYCLOAK_AUTHORITY="$expected_authority" \
APP_BASE_URL="${APP_BASE_URL:-http://127.0.0.1:${APP_HOST_PORT:-16379}}" \
./deploy/check-runtime.sh

echo '本地服务启动并通过统一登录边界检查'
