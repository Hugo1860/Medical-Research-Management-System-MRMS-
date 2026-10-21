#!/usr/bin/env bash
set -euo pipefail

# Idempotently create or update the production OIDC client in a target Realm.
# The administrator token is never printed. HTTPS is mandatory unless the
# operator explicitly opts into an HTTP-only local test with ALLOW_HTTP=true.
: "${KEYCLOAK_AUTHORITY:?请设置 KEYCLOAK_AUTHORITY，例如 https://sso.example.org/realms/scientific-research}"
: "${KEYCLOAK_ADMIN_TOKEN:?请设置 KEYCLOAK_ADMIN_TOKEN（短期管理员令牌）}"

config_file="${KEYCLOAK_CLIENT_CONFIG:-deploy/keycloak-client-scientificresearch.production.example.json}"
allow_http="${ALLOW_HTTP:-false}"
dry_run="${KEYCLOAK_DRY_RUN:-false}"
authority="${KEYCLOAK_AUTHORITY%/}"

# Bound every admin API call; a stopped Realm must fail instead of hanging.
curl() { command curl --max-time "${CURL_MAX_TIME:-10}" "$@"; }

if [[ "$allow_http" != "true" && "$authority" != https://* ]]; then
  echo 'Keycloak 客户端导入失败: 生产 Realm 必须使用 HTTPS；本地测试请显式设置 ALLOW_HTTP=true' >&2
  exit 1
fi
if [[ ! -f "$config_file" ]]; then
  echo "Keycloak 客户端导入失败: 找不到配置文件 $config_file" >&2
  exit 1
fi

realm_base="${authority%/realms/*}"
realm_name="${authority##*/realms/}"
if [[ "$realm_base" == "$authority" || -z "$realm_name" ]]; then
  echo 'Keycloak 客户端导入失败: KEYCLOAK_AUTHORITY 必须包含 /realms/<realm>' >&2
  exit 1
fi

query_file="$(mktemp)"
payload_file="$(mktemp)"
trap 'rm -f "$query_file" "$payload_file"' EXIT

python3 - "$config_file" "$allow_http" <<'PY'
import json, sys, urllib.parse
path, allow_http = sys.argv[1], sys.argv[2] == "true"
with open(path, encoding="utf-8-sig") as handle:
    client = json.load(handle)
required = {
    "clientId", "publicClient", "standardFlowEnabled", "implicitFlowEnabled",
    "directAccessGrantsEnabled", "serviceAccountsEnabled", "redirectUris", "webOrigins"
}
missing = sorted(required - client.keys())
if missing:
    raise SystemExit("客户端配置缺少: " + ", ".join(missing))
if client["publicClient"] is not True or client["standardFlowEnabled"] is not True:
    raise SystemExit("客户端必须启用 public client 和 Authorization Code 标准流程")
if client["implicitFlowEnabled"] is not False or client["directAccessGrantsEnabled"] is not False:
    raise SystemExit("生产客户端必须关闭 implicit flow 和 direct access grants")
if client["serviceAccountsEnabled"] is not False:
    raise SystemExit("生产客户端必须关闭 service account")
redirects = client["redirectUris"]
origins = client["webOrigins"]
if not isinstance(redirects, list) or len(redirects) != 1:
    raise SystemExit("生产客户端必须只有一个回调地址")
if not isinstance(origins, list) or len(origins) != 1:
    raise SystemExit("生产客户端必须只有一个 Web Origin")
redirect = redirects[0]
origin = urllib.parse.urlsplit(redirect)
if "*" in redirect or (not allow_http and origin.scheme != "https"):
    raise SystemExit("生产客户端回调地址必须是无通配符 HTTPS 地址")
expected_origin = f"{origin.scheme}://{origin.netloc}"
if origins[0] != expected_origin:
    raise SystemExit("Web Origin 必须与回调地址的 scheme 和 host 完全一致")
if (client.get("attributes") or {}).get("pkce.code.challenge.method") != "S256":
    raise SystemExit("客户端必须登记 PKCE S256")
PY

encoded_realm="$(python3 -c 'import sys,urllib.parse; print(urllib.parse.quote(sys.argv[1], safe=""))' "$realm_name")"
client_name="$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1], encoding="utf-8-sig"))["clientId"])' "$config_file")"
encoded_client="$(KEYCLOAK_CLIENT_ID="$client_name" python3 -c 'import urllib.parse,os; print(urllib.parse.quote(os.environ["KEYCLOAK_CLIENT_ID"], safe=""))')"
client_endpoint="${realm_base}/admin/realms/${encoded_realm}/clients?clientId=${encoded_client}"
curl --fail --silent --show-error --location \
  -H "Authorization: Bearer ${KEYCLOAK_ADMIN_TOKEN}" \
  "$client_endpoint" -o "$query_file"

client_count="$(python3 -c 'import json,sys; print(len(json.load(open(sys.argv[1]))))' "$query_file")"
if [[ "$client_count" != 0 && "$client_count" != 1 ]]; then
  echo "Keycloak 客户端导入失败: 目标 Realm 中同名客户端数量为 $client_count" >&2
  exit 1
fi

if [[ "$client_count" == 0 ]]; then
  if [[ "$dry_run" == "true" ]]; then
    echo "Keycloak 客户端 dry-run: 将创建 $(python3 -c 'import json,sys; print(json.load(open(sys.argv[1], encoding="utf-8-sig"))["clientId"])' "$config_file")"
    exit 0
  fi
  curl --fail --silent --show-error --location -X POST \
    -H "Authorization: Bearer ${KEYCLOAK_ADMIN_TOKEN}" \
    -H 'Content-Type: application/json' \
    --data-binary "@${config_file}" \
    "${realm_base}/admin/realms/${encoded_realm}/clients" >/dev/null
  echo 'Keycloak 客户端已创建'
  exit 0
fi

python3 - "$query_file" "$config_file" "$payload_file" <<'PY'
import json, sys
query_path, config_path, payload_path = sys.argv[1:]
existing = json.load(open(query_path))[0]
desired = json.load(open(config_path, encoding="utf-8-sig"))
existing.update(desired)
with open(payload_path, "w", encoding="utf-8") as handle:
    json.dump(existing, handle, ensure_ascii=False)
PY

if [[ "$dry_run" == "true" ]]; then
  echo "Keycloak 客户端 dry-run: 将更新 $(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["clientId"])' "$payload_file")"
  exit 0
fi

client_id="$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["id"])' "$query_file")"
curl --fail --silent --show-error --location -X PUT \
  -H "Authorization: Bearer ${KEYCLOAK_ADMIN_TOKEN}" \
  -H 'Content-Type: application/json' \
  --data-binary "@${payload_file}" \
  "${realm_base}/admin/realms/${encoded_realm}/clients/${client_id}" >/dev/null
echo 'Keycloak 客户端已更新'
