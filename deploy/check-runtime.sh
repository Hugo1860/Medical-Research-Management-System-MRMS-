#!/usr/bin/env bash
set -euo pipefail

# Read-only preflight for local Docker/Compose acceptance.
# It deliberately does not restart containers, prune images, or modify data.
script_dir="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
min_free_gb="${MIN_FREE_GB:-10}"
app_base_url="${APP_BASE_URL:-http://127.0.0.1:${APP_HOST_PORT:-16379}}"
# A local acceptance run must prove that the application advertises the same
# authentication mode that the operator intended.  Password-only mode remains
# available only as an explicit emergency choice (REQUIRE_KEYCLOAK=false).
require_keycloak="${REQUIRE_KEYCLOAK:-true}"
keycloak_local_overlay="${KEYCLOAK_LOCAL_OVERLAY:-false}"
check_keycloak_discovery="${CHECK_KEYCLOAK_DISCOVERY:-true}"
expected_keycloak_authority="${EXPECTED_KEYCLOAK_AUTHORITY:-}"
# Keycloak 26 exposes health management on port 9000 by default; the host
# commonly publishes only the business port 8180. Realm discovery is the
# application readiness contract and is reachable on that business port.
keycloak_url="${KEYCLOAK_HEALTH_URL:-http://127.0.0.1:8180/realms/scientific-research/.well-known/openid-configuration}"
db_host="${POSTGRES_HOST:-127.0.0.1}"
db_port="${POSTGRES_HOST_PORT:-15433}"
curl_timeout="${CURL_MAX_TIME:-5}"
disk_path="${DISK_PATH:-/System/Volumes/Data}"
if [[ ! -d "$disk_path" ]]; then disk_path='/'; fi
docker_raw_path="${DOCKER_RAW_PATH:-$HOME/Library/Containers/com.docker.docker/Data/vms/0/data/Docker.raw}"

failures=0
docker_server_ok=true
fail() { echo "运行时预检失败: $*" >&2; failures=$((failures + 1)); }

env_file="${ENV_FILE:-.env}"
if [[ -f "$env_file" || -n "${ENV_FILE:-}" ]]; then
  if ! ENV_FILE="$env_file" CHECK_REDIS="${CHECK_REDIS:-false}" \
      "$script_dir/verify-docker-env.sh"; then
    fail 'Docker 环境文件未通过密码安全校验'
  fi
fi

# Optional read-only gate for the latest backup artifacts.  This intentionally
# verifies the dump/hash/metadata tuple only; the isolated restore drill stays
# in verify-postgres-backup.sh and never runs against the production volume.
if [[ "${CHECK_BACKUP_ARTIFACTS:-false}" == 'true' ]]; then
  backup_file="${BACKUP_FILE:-}"
  if [[ -z "$backup_file" || ! -f "$backup_file" || -L "$backup_file" ]]; then
    fail "备份文件不存在、不是普通文件或为符号链接: ${backup_file:-未设置}"
  else
    backup_checksum="$(shasum -a 256 "$backup_file" | awk '{print $1}')"
    checksum_file="${backup_file}.sha256"
    if [[ ! -f "$checksum_file" || -L "$checksum_file" ]]; then
      fail "备份校验文件缺失或为符号链接: $checksum_file"
    else
      declared_checksum="$(awk 'NF >= 1 { print $1; exit }' "$checksum_file")"
      if [[ "$declared_checksum" != "$backup_checksum" ]]; then
        fail "备份校验和不匹配: $backup_file"
      else
        echo "备份工件校验: OK ($backup_checksum)"
      fi
    fi
    metadata_file="${backup_file}.json"
    if [[ ! -f "$metadata_file" || -L "$metadata_file" ]]; then
      fail "备份元数据缺失或为符号链接: $metadata_file"
    else
      if ! python3 - "$metadata_file" "$backup_checksum" "${POSTGRES_EXPECTED_SCHEMA_VERSION:-106}" <<'PY'
import json, sys
path, checksum, expected_version = sys.argv[1:]
try:
    with open(path, encoding='utf-8') as fh:
        data = json.load(fh)
except Exception as exc:
    raise SystemExit(f'备份元数据不是有效 JSON: {exc}')
if data.get('sha256') != checksum:
    raise SystemExit('备份元数据 sha256 与实际文件不匹配')
if str(data.get('schemaVersion', '')) != str(expected_version):
    raise SystemExit(f'备份元数据 schemaVersion={data.get("schemaVersion")} 与期望 {expected_version} 不一致')
PY
      then
        fail "备份元数据校验失败: $metadata_file"
      fi
      echo "备份元数据校验: OK (${metadata_file})"
    fi
  fi
fi

free_kb="$(df -Pk "$disk_path" 2>/dev/null | awk 'NR==2 {print $4}')"
if [[ -z "$free_kb" ]]; then
  fail "无法读取 ${disk_path} 可用空间"
else
  free_gb=$((free_kb / 1024 / 1024))
  echo "磁盘可用空间: ${free_gb} GiB（门槛 ${min_free_gb} GiB）"
  if (( free_gb < min_free_gb )); then fail "可用空间低于 ${min_free_gb} GiB"; fi
fi

if [[ -f "$docker_raw_path" ]]; then
  docker_raw_kb="$(du -k "$docker_raw_path" 2>/dev/null | awk 'NR==1 {print $1}')"
  if [[ -n "$docker_raw_kb" ]]; then
    echo "Docker 虚拟磁盘占用: $((docker_raw_kb / 1024 / 1024)) GiB (${docker_raw_path})"
  fi
fi

if ! timeout 8 docker version --format 'Docker Server {{.Server.Version}}' 2>/tmp/scientificresearch-docker-version.err; then
  cat /tmp/scientificresearch-docker-version.err >&2 || true
  docker_server_ok=false
  fail 'Docker Server 不可用，请先恢复 Docker Desktop'
fi

compose_args=(-f docker-compose.yml -f docker-compose.published.yml)
if [[ "$keycloak_local_overlay" == 'true' ]]; then
  compose_args+=(-f deploy/docker-compose.keycloak.local.yml)
fi
if timeout 10 docker compose "${compose_args[@]}" config >/dev/null; then
  echo 'Compose 配置: OK'
else
  fail 'Compose 配置解析失败'
fi

check_tcp() {
  local label="$1" host="$2" port="$3"
  if timeout 3 bash -c "</dev/tcp/${host}/${port}" >/dev/null 2>&1; then
    echo "${label} TCP: OPEN (${host}:${port})"
  else
    fail "${label} TCP 不可达 (${host}:${port})"
  fi
}
check_http() {
  local label="$1" url="$2" expected="$3" actual
  actual="$(command curl --max-time "$curl_timeout" --silent --show-error --output /dev/null --write-out '%{http_code}' "$url" 2>/dev/null || true)"
  if [[ "$actual" == "$expected" ]]; then
    echo "${label} HTTP: ${actual} (${url})"
  else
    fail "${label} HTTP 返回 ${actual}，期望 ${expected} (${url})"
  fi
}

check_oidc_config() {
  local oidc_file
  oidc_file="$(mktemp /tmp/scientificresearch-oidc.XXXXXX.json)"
  trap 'rm -f "$oidc_file"' RETURN
  if ! command curl --max-time "$curl_timeout" --silent --show-error --fail \
      "${app_base_url%/}/Access/OidcConfig" -o "$oidc_file" 2>/dev/null; then
    fail "应用 OIDC 公共配置不可读取 (${app_base_url%/}/Access/OidcConfig)"
    return
  fi
  if ! python3 - "$oidc_file" "$expected_keycloak_authority" <<'PY'
import json, sys
path, expected = sys.argv[1:]
try:
    raw = json.load(open(path, encoding='utf-8'))
except Exception as exc:
    raise SystemExit(f'OIDC 公共配置不是有效 JSON: {exc}')
config = raw.get('data', raw)
if config.get('enabled') is not True:
    raise SystemExit('OIDC 公共配置 enabled=false；当前运行实例会隐藏统一登录入口')
for key in ('authority', 'clientId', 'redirectUri'):
    if not str(config.get(key) or '').strip():
        raise SystemExit(f'OIDC 公共配置缺少 {key}')
if expected and str(config['authority']).rstrip('/') != expected.rstrip('/'):
    raise SystemExit(f"OIDC authority 不匹配: {config['authority']} != {expected}")
print('OIDC 公共配置: enabled=true, authority/clientId/redirectUri 已配置')
PY
  then
    fail '应用 OIDC 公共配置未通过统一登录门禁'
  fi
}

if [[ "$docker_server_ok" == 'false' ]]; then
  echo '运行时预检跳过: Docker Server 不可用，避免把残留端口或旧代理响应当作当前容器健康证据' >&2
else
  check_tcp 'PostgreSQL' "$db_host" "$db_port"
  check_http '应用存活' "${app_base_url%/}/health/live" 200
  check_http '应用就绪' "${app_base_url%/}/health/ready" 200
  if [[ "$require_keycloak" == 'true' ]]; then
    check_http '应用 Keycloak 健康' "${app_base_url%/}/health/keycloak" 200
    check_oidc_config
  fi
  if [[ "$check_keycloak_discovery" == 'true' ]]; then
    check_http 'Keycloak 就绪' "$keycloak_url" 200
  fi
fi

if (( failures > 0 )); then
  if [[ "$docker_server_ok" == 'false' ]]; then
    echo '提示: Docker Server 不可用时，开放端口或 /health/live 可能来自旧代理/残留进程，不能作为容器健康证据' >&2
  fi
  echo "运行时预检未通过: ${failures} 项" >&2
  exit 1
fi
echo '运行时预检通过'
