#!/usr/bin/env bash
set -euo pipefail
umask 077

# Creates the two local UAT identities used by the ethics workflow.  Passwords
# are generated locally, written only to a mode-600 credential file, and both
# users must change the password and configure TOTP at first login.
realm="${KEYCLOAK_REALM:-scientific-research}"
keycloak_base="${KEYCLOAK_BASE_URL:-http://keycloak.localhost:8180}"
database_container="${DATABASE_CONTAINER:-2018-7-24-db-1}"
database_name="${DATABASE_NAME:-ScientificResearch_Model}"
database_user="${DATABASE_USER:-postgres}"
credentials_file="${CREDENTIALS_FILE:-/private/tmp/scientificresearch-ethics-accounts.env}"
department_id="${ETHICS_DEPARTMENT_ID:-10020}"

: "${KEYCLOAK_ADMIN_USERNAME:?请设置 KEYCLOAK_ADMIN_USERNAME}"
: "${KEYCLOAK_ADMIN_PASSWORD:?请设置 KEYCLOAK_ADMIN_PASSWORD}"

command -v curl >/dev/null || { echo '缺少 curl' >&2; exit 1; }
command -v jq >/dev/null || { echo '缺少 jq' >&2; exit 1; }
command -v openssl >/dev/null || { echo '缺少 openssl' >&2; exit 1; }

api="${keycloak_base%/}/admin/realms/${realm}"
token_endpoint="${keycloak_base%/}/realms/master/protocol/openid-connect/token"
admin_token="$(curl --max-time "${CURL_MAX_TIME:-10}" -sS -f -X POST "$token_endpoint" \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  --data-urlencode client_id=admin-cli \
  --data-urlencode "username=${KEYCLOAK_ADMIN_USERNAME}" \
  --data-urlencode "password=${KEYCLOAK_ADMIN_PASSWORD}" \
  --data-urlencode grant_type=password | jq -r '.access_token // empty')"
[[ "$admin_token" == eyJ*.*.* ]] || { echo 'Keycloak 管理员令牌获取失败' >&2; exit 1; }

kc_get() {
  curl --max-time "${CURL_MAX_TIME:-10}" -sS -f -H "Authorization: Bearer ${admin_token}" "$@"
}
kc_mutate() {
  curl --max-time "${CURL_MAX_TIME:-10}" -sS -f -H "Authorization: Bearer ${admin_token}" "$@"
}

ensure_realm_role() {
  local role="$1" description="$2" encoded
  encoded="$(ROLE_NAME="$role" python3 -c 'import os,urllib.parse; print(urllib.parse.quote(os.environ["ROLE_NAME"], safe=""))')"
  if ! kc_get "${api}/roles/${encoded}" >/dev/null 2>&1; then
    kc_mutate -X POST "${api}/roles" -H 'Content-Type: application/json' \
      --data "$(jq -cn --arg name "$role" --arg description "$description" '{name:$name,description:$description,composite:false,clientRole:false}')" >/dev/null
  fi
}

ensure_realm_role '伦理秘书' '科研伦理申请受理、会议排期、委员指派、持续审查和档案流程管理'
ensure_realm_role '伦理委员' '科研伦理委员会委员意见、回避、出席和匿名表决参与'

secretary_password="$(openssl rand -hex 18)"
committee_password="$(openssl rand -hex 18)"

cat > /private/tmp/provision-ethics-accounts.sql <<'SQL'
\set ON_ERROR_STOP on
BEGIN;
DO $block$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM "角色" WHERE "名称"='伦理秘书') THEN
    INSERT INTO "角色" ("名称","备注") VALUES ('伦理秘书','独立科研伦理秘书角色');
  END IF;
  IF NOT EXISTS (SELECT 1 FROM "角色" WHERE "名称"='伦理委员') THEN
    INSERT INTO "角色" ("名称","备注") VALUES ('伦理委员','独立科研伦理委员会委员角色');
  END IF;
END
$block$;

INSERT INTO "人员" ("工号","密码","姓名","部门编号","备注","是否启用")
SELECT :'secretary_username', :'secretary_local_password', 'UAT伦理秘书', :'department_id', 'KEYCLOAK_ETHICS_ACCOUNT', TRUE
WHERE NOT EXISTS (SELECT 1 FROM "人员" WHERE "工号"=:'secretary_username');

INSERT INTO "人员" ("工号","密码","姓名","部门编号","备注","是否启用")
SELECT :'committee_username', :'committee_local_password', 'UAT伦理委员', :'department_id', 'KEYCLOAK_ETHICS_ACCOUNT', TRUE
WHERE NOT EXISTS (SELECT 1 FROM "人员" WHERE "工号"=:'committee_username');

INSERT INTO "人员角色" ("角色编号","人员编号")
SELECT r."编号",p."编号" FROM "角色" r CROSS JOIN "人员" p
WHERE r."名称"='伦理秘书' AND p."工号"=:'secretary_username'
  AND NOT EXISTS (SELECT 1 FROM "人员角色" pr WHERE pr."角色编号"=r."编号" AND pr."人员编号"=p."编号");

INSERT INTO "人员角色" ("角色编号","人员编号")
SELECT r."编号",p."编号" FROM "角色" r CROSS JOIN "人员" p
WHERE r."名称"='伦理委员' AND p."工号"=:'committee_username'
  AND NOT EXISTS (SELECT 1 FROM "人员角色" pr WHERE pr."角色编号"=r."编号" AND pr."人员编号"=p."编号");

-- The modern sidebar is populated from the server permission tree.  Grant the
-- parent node and the ethics leaf to both dedicated roles so a fresh local
-- database exposes the ethics workbench after the next OIDC login.
INSERT INTO "角色权限" ("角色编号","权限编号")
SELECT r."编号",p."编号" FROM "角色" r CROSS JOIN "权限" p
WHERE r."名称" IN ('伦理秘书','伦理委员') AND p."编号" IN (1,806)
  AND NOT EXISTS (SELECT 1 FROM "角色权限" rp WHERE rp."角色编号"=r."编号" AND rp."权限编号"=p."编号");
COMMIT;

SELECT p."编号",p."工号",p."姓名",string_agg(r."名称",',' ORDER BY r."名称") AS "角色"
FROM "人员" p JOIN "人员角色" pr ON pr."人员编号"=p."编号" JOIN "角色" r ON r."编号"=pr."角色编号"
WHERE p."工号" IN (:'secretary_username',:'committee_username')
GROUP BY p."编号",p."工号",p."姓名" ORDER BY p."工号";
SQL

printf '%s\n' 'KEYCLOAK_ETHICS_ACCOUNT=仅供本机验收' > "$credentials_file.tmp"
printf 'ETHICS_SECRETARY_USERNAME=%s\n' 'uat-ethics-secretary' >> "$credentials_file.tmp"
printf 'ETHICS_SECRETARY_TEMP_PASSWORD=%s\n' "$secretary_password" >> "$credentials_file.tmp"
printf 'ETHICS_COMMITTEE_USERNAME=%s\n' 'uat-ethics-committee' >> "$credentials_file.tmp"
printf 'ETHICS_COMMITTEE_TEMP_PASSWORD=%s\n' "$committee_password" >> "$credentials_file.tmp"
chmod 600 "$credentials_file.tmp"

docker exec -i "$database_container" psql -U "$database_user" -d "$database_name" \
  -v secretary_username='uat-ethics-secretary' \
  -v committee_username='uat-ethics-committee' \
  -v secretary_local_password="${secretary_password}" \
  -v committee_local_password="${committee_password}" \
  -v department_id="$department_id" -f /dev/stdin < /private/tmp/provision-ethics-accounts.sql >/tmp/provision-ethics-db.out

ensure_user() {
  local username="$1" display_name="$2" password="$3" role="$4" users user_id payload role_json
  users="$(kc_get --get "${api}/users" --data-urlencode "username=${username}" --data-urlencode exact=true)"
  user_id="$(printf '%s' "$users" | jq -r '.[0].id // empty')"
  payload="$(jq -cn --arg username "$username" --arg firstName 'UAT' --arg lastName "$display_name" \
    '{username:$username,enabled:true,firstName:$firstName,lastName:$lastName,emailVerified:false,requiredActions:["UPDATE_PASSWORD","CONFIGURE_TOTP"]}')"
  if [[ -z "$user_id" ]]; then
    headers="$(mktemp)"
    kc_mutate -D "$headers" -X POST "${api}/users" -H 'Content-Type: application/json' --data "$payload" >/dev/null
    user_id="$(awk 'BEGIN{IGNORECASE=1} /^location:/ {gsub(/\r/,"",$0); sub(/^location:[[:space:]]*/,"",$0); n=split($0,a,"/"); print a[n]}' "$headers")"
    rm -f "$headers"
  else
    kc_mutate -X PUT "${api}/users/${user_id}" -H 'Content-Type: application/json' --data "$payload" >/dev/null
  fi
  kc_mutate -X PUT "${api}/users/${user_id}/reset-password" -H 'Content-Type: application/json' \
    --data "$(jq -cn --arg value "$password" '{type:"password",value:$value,temporary:true}')" >/dev/null
  role_json="$(kc_get "${api}/roles/$(ROLE_NAME="$role" python3 -c 'import os,urllib.parse; print(urllib.parse.quote(os.environ["ROLE_NAME"], safe=""))')")"
  kc_mutate -X POST "${api}/users/${user_id}/role-mappings/realm" -H 'Content-Type: application/json' --data "[${role_json}]" >/dev/null
  printf '%s\n' "$username"
}

ensure_user 'uat-ethics-secretary' '伦理秘书' "$secretary_password" '伦理秘书' >/tmp/provision-ethics-secretary.out
ensure_user 'uat-ethics-committee' '伦理委员' "$committee_password" '伦理委员' >/tmp/provision-ethics-committee.out

mv -f "$credentials_file.tmp" "$credentials_file"
rm -f /private/tmp/provision-ethics-accounts.sql /tmp/provision-ethics-db.out /tmp/provision-ethics-secretary.out /tmp/provision-ethics-committee.out
echo "伦理 Keycloak 账号已创建/更新；临时凭据已安全写入 ${credentials_file}（权限 600）"
echo '首次登录必须修改临时密码并配置 TOTP；账号：uat-ethics-secretary、uat-ethics-committee'
