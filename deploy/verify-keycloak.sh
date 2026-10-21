#!/usr/bin/env bash
set -euo pipefail

discovery_only="${KEYCLOAK_DISCOVERY_ONLY:-false}"
: "${KEYCLOAK_AUTHORITY:?请设置 KEYCLOAK_AUTHORITY，例如 https://sso.example.org/realms/scientific-research}"
if [[ "$discovery_only" != "true" || -n "${KEYCLOAK_REVOKED_ACCESS_TOKEN:-}" || -n "${KEYCLOAK_REVOKED_PROXY_ACCESS_TOKEN:-}" ]]; then
  : "${APP_BASE_URL:?请设置 APP_BASE_URL，例如 https://research.example.org}"
fi

authority="${KEYCLOAK_AUTHORITY%/}"

# Bound every discovery/admin/API probe so a stopped Realm fails the gate clearly.
curl() { command curl --max-time "${CURL_MAX_TIME:-10}" "$@"; }
discovery_file="$(mktemp)"
token_file="$(mktemp)"
me_file="$(mktemp)"
client_file="$(mktemp)"
realm_roles_file="$(mktemp)"
client_roles_file="$(mktemp)"
client_default_scopes_file="$(mktemp)"
client_optional_scopes_file="$(mktemp)"
trap 'rm -f "$discovery_file" "$token_file" "$me_file" "$client_file" "$realm_roles_file" "$client_roles_file" "$client_default_scopes_file" "$client_optional_scopes_file"' EXIT
curl --fail --silent --show-error --location \
  "${authority}/.well-known/openid-configuration" -o "$discovery_file"
token_endpoint="$(python3 - "$discovery_file" "$authority" <<'PY'
import json, sys
doc=json.load(open(sys.argv[1]))
issuer=doc.get("issuer", "")
if not issuer:
    raise SystemExit("Keycloak discovery 缺少 issuer")
authority=sys.argv[2].rstrip('/')
if issuer.rstrip('/') != authority:
    raise SystemExit(f"Keycloak issuer 与 Authority 不匹配: {issuer}")
print(doc.get("token_endpoint", ""))
PY
 )"
if [[ -z "$token_endpoint" ]]; then
  echo "Keycloak discovery 缺少 token_endpoint" >&2
  exit 1
fi
python3 - "$discovery_file" "$authority" "${KEYCLOAK_REQUIRED_DISCOVERY_SCOPES:-}" <<'PY'
import json, sys
doc=json.load(open(sys.argv[1]))
authority=sys.argv[2].rstrip('/')
for name in ("authorization_endpoint", "token_endpoint", "jwks_uri"):
    value=doc.get(name)
    if not isinstance(value, str) or not value.startswith(("http://", "https://")):
        raise SystemExit(f"Keycloak discovery 缺少有效 {name}")
if doc["issuer"].rstrip('/') != authority:
    raise SystemExit(f"Keycloak discovery issuer 与 Authority 不匹配: {doc['issuer']}")
required={item.strip() for item in sys.argv[3].split(",") if item.strip()}
if required:
    supported=set(doc.get("scopes_supported") or [])
    missing=sorted(required - supported)
    if missing:
        raise SystemExit("Keycloak discovery 缺少必需 scopes_supported: " + ", ".join(missing))
    print("Keycloak discovery scopes_supported OK:", ", ".join(sorted(required)))
print("Keycloak discovery endpoints OK: authorization_endpoint, token_endpoint, jwks_uri")
PY
echo "Keycloak Realm discovery OK: $authority"

# Optional administrator-side client contract. A user token proves that the
# client can authenticate, while this check proves the actual Realm client has
# the intended flow flags and redirect URI. The admin token is supplied by the
# operator and is never printed.
if [[ -n "${KEYCLOAK_ADMIN_TOKEN:-}" ]]; then
  : "${KEYCLOAK_CLIENT_ID:?提供 KEYCLOAK_ADMIN_TOKEN 时还必须设置 KEYCLOAK_CLIENT_ID}"
  realm_base="${authority%/realms/*}"
  realm_name="${authority##*/realms/}"
  client_endpoint="${realm_base}/admin/realms/${realm_name}/clients?clientId=$(python3 -c 'import os,urllib.parse; print(urllib.parse.quote(os.environ["KEYCLOAK_CLIENT_ID"], safe=""))')"
  curl --fail --silent --show-error --location \
    -H "Authorization: Bearer ${KEYCLOAK_ADMIN_TOKEN}" \
    "$client_endpoint" -o "$client_file"
  python3 - "$client_file" "${KEYCLOAK_EXPECT_DIRECT_ACCESS_GRANTS:-false}" "${KEYCLOAK_EXPECT_REDIRECT_URI:-}" "${KEYCLOAK_EXPECT_WEB_ORIGIN:-}" "${KEYCLOAK_EXPECT_PKCE_METHOD:-}" "${KEYCLOAK_REQUIRE_EXACT_REDIRECTS:-false}" "${KEYCLOAK_REQUIRE_EXACT_WEB_ORIGINS:-false}" <<'PY'
import json, sys
clients=json.load(open(sys.argv[1]))
if len(clients) != 1:
    raise SystemExit(f"Keycloak 客户端数量不符合预期: {len(clients)}")
client=clients[0]
if client.get("standardFlowEnabled") is not True:
    raise SystemExit("Keycloak 客户端未启用 Authorization Code 标准流程")
if client.get("publicClient") is not True:
    raise SystemExit("科研管理客户端必须是 public client（PKCE）")
expected_direct=sys.argv[2].lower() == "true"
if bool(client.get("directAccessGrantsEnabled")) != expected_direct:
    raise SystemExit(
        "Keycloak 客户端 direct access grants 与验收要求不一致: "
        f"actual={client.get('directAccessGrantsEnabled')} expected={expected_direct}"
    )
if client.get("serviceAccountsEnabled") is True:
    raise SystemExit("科研管理客户端不应启用 service account")
if client.get("implicitFlowEnabled") is True:
    raise SystemExit("科研管理客户端不应启用 implicit flow")
expected_redirect=sys.argv[3]
redirect_uris=client.get("redirectUris") or []
if expected_redirect and expected_redirect not in redirect_uris:
    raise SystemExit("Keycloak 客户端缺少验收回调地址")
if sys.argv[6].lower() == "true" and redirect_uris != [expected_redirect]:
    raise SystemExit(f"Keycloak 客户端回调地址不是生产精确集合: {redirect_uris}")
expected_origin=sys.argv[4]
web_origins=client.get("webOrigins") or []
if expected_origin and expected_origin not in web_origins:
    raise SystemExit("Keycloak 客户端缺少验收 Web Origin")
if sys.argv[7].lower() == "true" and web_origins != [expected_origin]:
    raise SystemExit(f"Keycloak 客户端 Web Origin 不是生产精确集合: {web_origins}")
expected_pkce=sys.argv[5]
if expected_pkce and (client.get("attributes") or {}).get("pkce.code.challenge.method") != expected_pkce:
    raise SystemExit("Keycloak 客户端 PKCE 方法与验收要求不一致")
if expected_pkce == "S256" and expected_redirect.startswith("https://") and any("*" in uri or not uri.startswith("https://") for uri in redirect_uris):
    raise SystemExit("生产 Keycloak 客户端回调地址必须全部为 HTTPS 且不得使用通配符")
print("Keycloak client contract OK:", client.get("clientId"))
PY
  required_roles="${KEYCLOAK_REQUIRED_ROLES:-}"
  if [[ -n "$required_roles" ]]; then
    client_internal_id="$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))[0]["id"])' "$client_file")"
    curl --fail --silent --show-error --location \
      -H "Authorization: Bearer ${KEYCLOAK_ADMIN_TOKEN}" \
      "${realm_base}/admin/realms/${realm_name}/roles" -o "$realm_roles_file"
    curl --fail --silent --show-error --location \
      -H "Authorization: Bearer ${KEYCLOAK_ADMIN_TOKEN}" \
      "${realm_base}/admin/realms/${realm_name}/clients/${client_internal_id}/roles" -o "$client_roles_file"
    python3 - "$realm_roles_file" "$client_roles_file" "$required_roles" <<'PY'
import json, sys
realm=json.load(open(sys.argv[1]))
client=json.load(open(sys.argv[2]))
available={item.get("name") for item in realm + client if item.get("name")}
required={item.strip() for item in sys.argv[3].split(",") if item.strip()}
missing=sorted(required - available)
if missing:
    raise SystemExit("Keycloak Realm/client 缺少声明角色: " + ", ".join(missing))
print("Keycloak required Realm/client roles OK:", ", ".join(sorted(required)))
PY
  fi
  required_scopes="${KEYCLOAK_REQUIRED_CLIENT_SCOPES:-}"
  if [[ -n "$required_scopes" ]]; then
    if [[ -z "${client_internal_id:-}" ]]; then
      client_internal_id="$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))[0]["id"])' "$client_file")"
    fi
    curl --fail --silent --show-error --location \
      -H "Authorization: Bearer ${KEYCLOAK_ADMIN_TOKEN}" \
      "${realm_base}/admin/realms/${realm_name}/clients/${client_internal_id}/default-client-scopes" -o "$client_default_scopes_file"
    curl --fail --silent --show-error --location \
      -H "Authorization: Bearer ${KEYCLOAK_ADMIN_TOKEN}" \
      "${realm_base}/admin/realms/${realm_name}/clients/${client_internal_id}/optional-client-scopes" -o "$client_optional_scopes_file"
    python3 - "$client_default_scopes_file" "$client_optional_scopes_file" "$required_scopes" <<'PY'
import json, sys
default=json.load(open(sys.argv[1]))
optional=json.load(open(sys.argv[2]))
available={item.get("name") for item in default + optional if item.get("name")}
required={item.strip() for item in sys.argv[3].split(",") if item.strip()}
missing=sorted(required - available)
if missing:
    raise SystemExit("Keycloak 客户端缺少必需 client scope: " + ", ".join(missing))
print("Keycloak client scopes OK:", ", ".join(sorted(required)))
PY
  fi
fi

access_token="${KEYCLOAK_ACCESS_TOKEN:-}"
if [[ -z "$access_token" && -n "${KEYCLOAK_CLIENT_ID:-}" && -n "${KEYCLOAK_USERNAME:-}" && -n "${KEYCLOAK_PASSWORD:-}" ]]; then
  token_args=(
    --data-urlencode "grant_type=password"
    --data-urlencode "client_id=${KEYCLOAK_CLIENT_ID}"
    --data-urlencode "username=${KEYCLOAK_USERNAME}"
    --data-urlencode "password=${KEYCLOAK_PASSWORD}"
  )
  if [[ -n "${KEYCLOAK_CLIENT_SECRET:-}" ]]; then
    token_args+=(--data-urlencode "client_secret=${KEYCLOAK_CLIENT_SECRET}")
  fi
  curl --fail --silent --show-error --location \
    -H 'Content-Type: application/x-www-form-urlencoded' \
    "${token_args[@]}" "$token_endpoint" -o "$token_file"
  access_token="$(python3 - "$token_file" <<'PY'
import json, sys
token=json.load(open(sys.argv[1])).get("access_token", "")
if not token:
    raise SystemExit("Keycloak token 响应缺少 access_token")
print(token)
PY
)"
  echo "Keycloak client token OK: ${KEYCLOAK_CLIENT_ID}"
fi

if [[ -n "$access_token" ]]; then
  python3 - "$access_token" "${KEYCLOAK_AUDIENCE:-scientificresearch-api}" "$authority" "${KEYCLOAK_REQUIRE_MFA:-false}" "${KEYCLOAK_MFA_AMR:-otp,mfa}" "${KEYCLOAK_CLIENT_ID:-}" "${KEYCLOAK_REQUIRED_CLAIMS:-iss,sub,azp,aud,preferred_username}" "${KEYCLOAK_REQUIRED_ROLES:-}" "${KEYCLOAK_REQUIRED_CLIENT_SCOPES:-}" <<'PY'
import base64, json, sys, time
parts=sys.argv[1].split('.')
if len(parts) != 3:
    raise SystemExit("access token 不是 JWT")
payload=parts[1] + '=' * (-len(parts[1]) % 4)
claims=json.loads(base64.urlsafe_b64decode(payload))
required_claims={item.strip() for item in sys.argv[7].split(",") if item.strip()}
missing_claims=sorted(item for item in required_claims if not claims.get(item))
if missing_claims:
    raise SystemExit("access token 缺少必需 claims: " + ", ".join(missing_claims))
aud=claims.get("aud", [])
if isinstance(aud, str): aud=[aud]
if sys.argv[2] not in aud:
    raise SystemExit(f"access token 缺少目标 audience: {sys.argv[2]}")
issuer=claims.get("iss", "")
if issuer.rstrip("/") != sys.argv[3].rstrip("/"):
    raise SystemExit(f"access token issuer 不匹配: {issuer}")
if claims.get("exp") is not None and int(claims["exp"]) <= int(time.time()):
    raise SystemExit("access token 已过期")
if not claims.get("preferred_username") and not claims.get("sub"):
    raise SystemExit("access token 缺少 preferred_username/sub")
if sys.argv[4].lower() == "true":
    amr = claims.get("amr", [])
    if isinstance(amr, str):
        amr = [amr]
    accepted = {item.strip() for item in sys.argv[5].split(",") if item.strip()}
    if not accepted.intersection(amr):
        raise SystemExit(f"access token 未通过 MFA 声明验收，当前 amr={amr}")
    print("Keycloak MFA claim OK:", ",".join(amr))
expected_client = sys.argv[6]
if expected_client and claims.get("azp") != expected_client:
    raise SystemExit(f"access token azp 与客户端不匹配: {claims.get('azp')} != {expected_client}")
required_roles={item.strip() for item in sys.argv[8].split(",") if item.strip()}
if required_roles:
    roles=set((claims.get("realm_access") or {}).get("roles") or [])
    roles.update(((claims.get("resource_access") or {}).get(expected_client) or {}).get("roles") or [])
    missing_roles=sorted(required_roles - roles)
    if missing_roles:
        raise SystemExit("access token 缺少必需角色: " + ", ".join(missing_roles))
    print("Keycloak required token roles OK:", ", ".join(sorted(required_roles)))
required_scopes={item.strip() for item in sys.argv[9].split(",") if item.strip()}
if required_scopes:
    token_scopes=set((claims.get("scope") or "").split())
    missing_scopes=sorted(required_scopes - token_scopes)
    if missing_scopes:
        raise SystemExit("access token 缺少必需 scopes: " + ", ".join(missing_scopes))
    print("Keycloak required token scopes OK:", ", ".join(sorted(required_scopes)))
print("Keycloak access token claims OK:", claims.get("preferred_username") or claims.get("sub"))
PY
  curl --fail --silent --show-error \
    -H "Authorization: Bearer ${access_token}" \
    "${APP_BASE_URL%/}/health/ready" >/dev/null
  curl --fail --silent --show-error \
    -H "Authorization: Bearer ${access_token}" \
    "${APP_BASE_URL%/}/Access/Me" -o "$me_file"
  python3 - "$me_file" <<'PY'
import json, sys
data=json.load(open(sys.argv[1]))
if not data.get("data", {}).get("人员"):
    raise SystemExit("/Access/Me 未返回本地人员映射")
print("Bearer token /Access/Me OK")
PY
  for endpoint in '/Office/获取科研工作台摘要' '/Expense/获取预算异常对账'; do
    curl --fail --silent --show-error \
      -H "Authorization: Bearer ${access_token}" \
      "${APP_BASE_URL%/}${endpoint}" -o /dev/null
    echo "Bearer token ${endpoint} OK"
  done
fi

# 令牌撤销/代理撤销探针独立于当前有效令牌验收运行。这样在只有失效前
# 保存的旧令牌时，也能得到明确的 401/403 结果，而不是被误判为 discovery-only。
revocation_probe=false
if [[ -n "${KEYCLOAK_REVOKED_ACCESS_TOKEN:-}" ]]; then
  revocation_probe=true
  revoked_endpoints="${KEYCLOAK_REVOKED_ENDPOINTS:-${KEYCLOAK_REVOKED_ENDPOINT:-/Access/Me}}"
  IFS=',' read -r -a revoked_endpoint_list <<< "$revoked_endpoints"
  for revoked_endpoint in "${revoked_endpoint_list[@]}"; do
    revoked_endpoint="${revoked_endpoint//[[:space:]]/}"
    [[ -n "$revoked_endpoint" ]] || continue
    revoked_status="$(curl --silent --show-error \
      -o /dev/null -w '%{http_code}' \
      -H "Authorization: Bearer ${KEYCLOAK_REVOKED_ACCESS_TOKEN}" \
      "${APP_BASE_URL%/}${revoked_endpoint}" || true)"
    case ",${KEYCLOAK_EXPECT_REVOKED_TOKEN_STATUS:-401,403}," in
      *",${revoked_status},"*) echo "Revoked/disabled token rejected OK: ${revoked_endpoint} -> ${revoked_status}" ;;
      *) echo "撤销/离岗令牌仍可访问或响应不符合预期: ${revoked_endpoint} -> ${revoked_status}" >&2; exit 1 ;;
    esac
  done
fi
if [[ -n "${KEYCLOAK_REVOKED_PROXY_ACCESS_TOKEN:-}" ]]; then
  revocation_probe=true
  : "${KEYCLOAK_REVOKED_PROXY_GRANTOR_ID:?提供 KEYCLOAK_REVOKED_PROXY_ACCESS_TOKEN 时还必须设置 KEYCLOAK_REVOKED_PROXY_GRANTOR_ID}"
  : "${KEYCLOAK_REVOKED_PROXY_PAYLOAD:?提供 KEYCLOAK_REVOKED_PROXY_ACCESS_TOKEN 时还必须设置专用验收请求体 KEYCLOAK_REVOKED_PROXY_PAYLOAD}"
  proxy_endpoint="${KEYCLOAK_REVOKED_PROXY_ENDPOINT:-/YProject/审核并应用纵向项目变更}"
  proxy_method="${KEYCLOAK_REVOKED_PROXY_METHOD:-POST}"
  proxy_status="$(curl --silent --show-error \
    -o /dev/null -w '%{http_code}' \
    -X "$proxy_method" \
    -H "Authorization: Bearer ${KEYCLOAK_REVOKED_PROXY_ACCESS_TOKEN}" \
    -H "X-Proxy-Grantor-Id: ${KEYCLOAK_REVOKED_PROXY_GRANTOR_ID}" \
    -H 'Content-Type: application/json' \
    --data "$KEYCLOAK_REVOKED_PROXY_PAYLOAD" \
    "${APP_BASE_URL%/}${proxy_endpoint}" || true)"
  case ",${KEYCLOAK_EXPECT_REVOKED_PROXY_STATUS:-401,403}," in
    *",${proxy_status},"*) echo "Revoked proxy grant rejected OK: ${proxy_endpoint} -> ${proxy_status}" ;;
    *) echo "代理撤销后仍可使用旧代理令牌或响应不符合预期: ${proxy_endpoint} -> ${proxy_status}" >&2; exit 1 ;;
  esac
fi
if [[ -z "$access_token" ]]; then
  if [[ "$discovery_only" == "true" && "$revocation_probe" == "false" ]]; then
    echo "Discovery-only mode OK；未提供 admin/user token，未宣称客户端、角色、MFA、离岗回收或代理撤销验收" >&2
    exit 0
  elif [[ "$revocation_probe" == "true" ]]; then
    echo "Keycloak discovery 与令牌撤销探针完成；未宣称有效客户端/用户 claims 验收" >&2
    exit 0
  else
    echo "仅完成 Realm discovery；请提供 KEYCLOAK_ACCESS_TOKEN，或 KEYCLOAK_CLIENT_ID/KEYCLOAK_USERNAME/KEYCLOAK_PASSWORD 进行真实客户端验收。当前退出码 2 表示验收未完成" >&2
    exit 2
  fi
fi
