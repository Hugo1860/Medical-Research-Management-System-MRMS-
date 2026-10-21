#!/usr/bin/env bash
set -euo pipefail

# Read-only release gate. It checks the public HTTP contract and, when enabled,
# delegates the real Realm/client/token checks to verify-keycloak.sh.
: "${APP_BASE_URL:?请设置 APP_BASE_URL，例如 https://research.example.org}"
base_url="${APP_BASE_URL%/}"
keycloak_enabled="${KEYCLOAK_ENABLED:-true}"
expect_swagger_disabled="${EXPECT_SWAGGER_DISABLED:-true}"
require_https="${REQUIRE_HTTPS:-false}"
keycloak_require_https_metadata="${KEYCLOAK_REQUIRE_HTTPS_METADATA:-true}"

# Never let a dead proxy or dependency hang the release gate indefinitely.
curl() { command curl --max-time "${CURL_MAX_TIME:-10}" "$@"; }

if [[ "$require_https" == 'true' && "$base_url" != https://* ]]; then
  echo '发布验收失败: REQUIRE_HTTPS=true 时 APP_BASE_URL 必须使用 HTTPS' >&2
  exit 1
fi
if [[ "$require_https" == 'true' && "$keycloak_enabled" == 'true' && "$keycloak_require_https_metadata" != 'true' ]]; then
  echo '发布验收失败: HTTPS 发布要求 KEYCLOAK_REQUIRE_HTTPS_METADATA=true' >&2
  exit 1
fi

http_code() {
  local path="$1"
  curl --silent --show-error --output /dev/null --write-out '%{http_code}' "${base_url}${path}"
}

assert_code() {
  local path="$1" expected="$2" actual
  actual="$(http_code "$path")"
  if [[ "$actual" != "$expected" ]]; then
    echo "发布验收失败: ${path} 返回 ${actual}，期望 ${expected}" >&2
    exit 1
  fi
  echo "${path} OK (${actual})"
}

assert_code /health/live 200
assert_code /health/ready 200

headers_file="$(mktemp)"
trap 'rm -f "$headers_file"' EXIT
curl --fail --silent --show-error --dump-header "$headers_file" --output /dev/null "${base_url}/health/live"
for header in X-Request-ID X-Content-Type-Options X-Frame-Options Referrer-Policy Permissions-Policy; do
  if ! grep -iq "^${header}:" "$headers_file"; then
    echo "发布验收失败: 缺少响应头 ${header}" >&2
    exit 1
  fi
done
echo '安全响应头 OK'

if [[ "$expect_swagger_disabled" == 'true' ]]; then
  assert_code /swagger/index.html 404
fi
assert_code '/IMIS/views/科研办公/科研办公/权限代理与预算对账.html' 200
assert_code '/IMIS/views/科研办公/科研办公/科研评价管理.html' 200
assert_code '/IMIS/views/科研统计/科研统计台账.html?module=overview' 200
assert_code '/IMIS/views/成果管理/论文/论文采集任务.html' 200
assert_code '/IMIS/views/科研办公/科研办公/项目生命周期.html' 200
assert_code '/IMIS/views/科研办公/科研办公/横向项目流程.html' 200
assert_code '/IMIS/views/科研办公/科研办公/移动科研工作台.html' 200
assert_code '/IMIS/views/科研办公/科研办公/纵向项目申报计划.html' 200
assert_code '/IMIS/views/组织架构/专家遴选.html' 200
assert_code '/IMIS/views/科研统计/科研统计台账.html?module=project' 200

if [[ "$keycloak_enabled" == 'true' ]]; then
  assert_code /health/keycloak 200
  oidc_file="$(mktemp)"
  trap 'rm -f "$headers_file" "$oidc_file"' EXIT
  curl --fail --silent --show-error "${base_url}/Access/OidcConfig" -o "$oidc_file"
  python3 - "$oidc_file" "$require_https" <<'PY'
import json, sys
doc=json.load(open(sys.argv[1]))
config=doc.get('data', doc)
if config.get('enabled') is not True:
    raise SystemExit('发布验收失败: OIDC 配置未启用')
for key in ('authority', 'clientId', 'redirectUri'):
    if not config.get(key):
        raise SystemExit(f'发布验收失败: OIDC 配置缺少 {key}')
if sys.argv[2] == 'true':
    if not str(config['authority']).startswith('https://') or not str(config['redirectUri']).startswith('https://'):
        raise SystemExit('发布验收失败: HTTPS 发布要求 OIDC authority 和 redirectUri 使用 HTTPS')
print('OIDC public config OK')
PY
  if [[ "$require_https" == 'true' ]]; then
    if [[ -z "${KEYCLOAK_ADMIN_TOKEN:-}" ]]; then
      echo '发布验收失败: REQUIRE_HTTPS=true 时必须提供 KEYCLOAK_ADMIN_TOKEN 进行真实客户端配置验收' >&2
      exit 1
    fi
    if [[ -z "${KEYCLOAK_EXPECT_REDIRECT_URI:-}" ]]; then
      export KEYCLOAK_EXPECT_REDIRECT_URI="$(python3 -c 'import json,sys; d=json.load(open(sys.argv[1])); print(d.get("data",d).get("redirectUri", ""))' "$oidc_file")"
    fi
    if [[ -z "${KEYCLOAK_CLIENT_ID:-}" ]]; then
      export KEYCLOAK_CLIENT_ID="$(python3 -c 'import json,sys; d=json.load(open(sys.argv[1])); print(d.get("data",d).get("clientId", ""))' "$oidc_file")"
    fi
    if [[ -z "${KEYCLOAK_EXPECT_WEB_ORIGIN:-}" ]]; then
      export KEYCLOAK_EXPECT_WEB_ORIGIN="$(python3 -c 'import json,sys,urllib.parse; d=json.load(open(sys.argv[1])); u=d.get("data",d).get("redirectUri", ""); p=urllib.parse.urlsplit(u); print(f"{p.scheme}://{p.netloc}")' "$oidc_file")"
    fi
    export KEYCLOAK_EXPECT_DIRECT_ACCESS_GRANTS="${KEYCLOAK_EXPECT_DIRECT_ACCESS_GRANTS:-false}"
    export KEYCLOAK_EXPECT_PKCE_METHOD="${KEYCLOAK_EXPECT_PKCE_METHOD:-S256}"
    export KEYCLOAK_REQUIRE_EXACT_REDIRECTS="${KEYCLOAK_REQUIRE_EXACT_REDIRECTS:-true}"
    export KEYCLOAK_REQUIRE_EXACT_WEB_ORIGINS="${KEYCLOAK_REQUIRE_EXACT_WEB_ORIGINS:-true}"
  fi
  ./deploy/verify-keycloak.sh
  if [[ "${VERIFY_BUSINESS_CONTRACT:-false}" == 'true' ]]; then
    ./deploy/verify-business-contract.sh
  fi
fi

echo '发布验收通过'
