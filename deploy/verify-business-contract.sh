#!/usr/bin/env bash
set -euo pipefail

# Read-only business contract smoke test for project, finance, ethics, people,
# achievement and evaluation query endpoints. It never creates or mutates a
# project, workflow, reimbursement, archive, or uploaded file.
: "${APP_BASE_URL:?请设置 APP_BASE_URL，例如 http://127.0.0.1:16379}"
: "${KEYCLOAK_AUTHORITY:?请设置 KEYCLOAK_AUTHORITY，例如 http://keycloak.localhost:8180/realms/scientific-research}"
: "${KEYCLOAK_CLIENT_ID:?请设置 KEYCLOAK_CLIENT_ID，例如 scientificresearch-api}"
: "${KEYCLOAK_USERNAME:?请设置 KEYCLOAK_USERNAME，例如 string22}"
# Real read-only UAT identifiers are mandatory. Do not silently fall back to
# sample project IDs, which could make a clean database look accepted.
: "${BUSINESS_Y_PROJECT_ID:?请设置真实的纵向项目立项编号（只读验收）}"
: "${BUSINESS_X_PROJECT_ID:?请设置真实的横向项目编号（只读验收）}"
: "${BUSINESS_Y_FLOW_TEMPLATE_ID:?请设置真实的纵向项目流程模板编号（只读验收）}"

base_url="${APP_BASE_URL%/}"
authority="${KEYCLOAK_AUTHORITY%/}"
curl() { command curl --max-time "${CURL_MAX_TIME:-10}" "$@"; }

access_token="${KEYCLOAK_ACCESS_TOKEN:-}"
if [[ -z "$access_token" ]]; then
  : "${KEYCLOAK_PASSWORD:?未提供 KEYCLOAK_ACCESS_TOKEN 时必须设置 KEYCLOAK_PASSWORD}"
  token_endpoint="$(curl --fail --silent --show-error "${authority}/.well-known/openid-configuration" | python3 -c 'import json,sys; print(json.load(sys.stdin)["token_endpoint"])')"
  access_token="$(curl --fail --silent --show-error \
    -H 'Content-Type: application/x-www-form-urlencoded' \
    --data-urlencode grant_type=password \
    --data-urlencode "client_id=${KEYCLOAK_CLIENT_ID}" \
    --data-urlencode "username=${KEYCLOAK_USERNAME}" \
    --data-urlencode "password=${KEYCLOAK_PASSWORD}" \
    "$token_endpoint" | python3 -c 'import json,sys; print(json.load(sys.stdin)["access_token"])')"
fi

paths=(
  # Project lifecycle and the read-only records introduced by 077/082.
  '/YProject/分页获取纵向项目申报计划'
  '/YProject/分页获取科研项目申报'
  '/YProject/分页获取科研项目立项'
  '/YProject/分页获取纵向项目中检'
  '/YProject/分页获取纵向项目变更'
  '/YProject/分页获取纵向项目结题'
  "/YProject/获取纵向项目任务书?纵向项目立项编号=${BUSINESS_Y_PROJECT_ID}"
  '/XProject/分页获取横向项目变更'
  '/XProject/分页获取横向项目结题'
  # Finance controls and annual settlement (read-only).
  '/Expense/分页获取纵向项目经费合计报销列表'
  '/Expense/分页获取横向项目经费合计报销列表'
  '/Expense/分页获取参加会议合计报销列表'
  '/Expense/获取预算异常对账'
  '/Expense/获取项目经费分层'
  "/Expense/获取按到账项目预算列表?项目编号=${BUSINESS_Y_PROJECT_ID}"
  '/Expense/获取Hrp中间库同步状态'
  '/Expense/获取项目经费预算调整'
  "/Expense/获取项目经费年度结算?流程模板编号=${BUSINESS_Y_FLOW_TEMPLATE_ID}&项目编号=${BUSINESS_Y_PROJECT_ID}"
  # Independent ethics workflow and archive.
  '/Ethics/查询伦理申请'
  '/Ethics/查询伦理到期提醒'
  '/Ethics/查询伦理会议'
  '/Ethics/查询伦理电子档案'
  # People/expert capabilities and evaluation/reward ledgers.
  '/People/分页获取启用的人员列表'
  '/People/查找科研合作人'
  '/People/匹配科研团队成员'
  '/People/查询科研团队'
  '/People/查询人员项目分工'
  '/People/查询专家可用时间'
  '/People/查询外部专家邀请'
  '/Evaluation/获取科研评价规则'
  '/Evaluation/获取科研评价年度批次'
  '/Evaluation/获取年度奖励发放台账'
  '/Evaluation/查询科研评价结果'
  '/Evaluation/获取科研绩效动态公式列表'
  '/Fruit/获取成果转化台账'
  '/Fruit/获取成果治理审计?成果类型=1&成果编号=1'
  '/Fruit/获取论文主动网推台账'
  '/Fruit/获取机构与学者消歧配置'
  '/Operations/获取经费适配器契约检查'
  '/YProject/获取纵向项目电子签章服务状态'
  '/YProject/获取纵向项目正式档案服务状态'
  "/YProject/预检查纵向项目中检?纵向项目立项编号=${BUSINESS_Y_PROJECT_ID}"
  "/YProject/预检查纵向项目变更?纵向项目立项编号=${BUSINESS_Y_PROJECT_ID}"
  "/YProject/预检查纵向项目结题?纵向项目立项编号=${BUSINESS_Y_PROJECT_ID}"
  "/XProject/获取横向项目详情?横向项目编号=${BUSINESS_X_PROJECT_ID}"
  '/Analytics/获取科研统计概览?维度=overview'
  '/Evaluation/获取科研评价排名'
)

for path in "${paths[@]}"; do
  encoded_path="$(python3 - "$path" <<'PY'
import sys
from urllib.parse import quote, parse_qsl, urlencode, urlsplit
parts = urlsplit(sys.argv[1])
value = quote(parts.path, safe='/')
if parts.query:
    value += '?' + urlencode(parse_qsl(parts.query, keep_blank_values=True), encoding='utf-8')
print(value)
PY
)"
  response_file="$(mktemp)"
  trap 'rm -f "$response_file"' EXIT
  request_url="${base_url}${encoded_path}"
  if [[ "$path" != *'?'* ]]; then request_url+='?Index=1&Size=10'; fi
  status="$(curl --silent --show-error --output "$response_file" --write-out '%{http_code}' \
    -H "Authorization: Bearer ${access_token}" \
    "$request_url")"
  if [[ "$status" != '200' ]]; then
    echo "业务回归失败: ${path} 返回 ${status}" >&2
    head -c 500 "$response_file" >&2 || true
    exit 1
  fi
  python3 - "$response_file" <<'PY'
import json, sys
document = json.load(open(sys.argv[1], encoding='utf-8'))
if not isinstance(document, dict) or 'data' not in document:
    raise SystemExit('业务回归失败: 响应缺少 data 包装')
PY
  echo "${path} OK (200)"
done
echo "只读 GET 业务接口回归通过: ${#paths[@]} 个"

# Preflight is intentionally called with a draft payload. It must be read-only,
# return the standard data wrapper, and expose itemized checks without creating
# a project or workflow.
preflight_file="$(mktemp)"
trap 'rm -f "$preflight_file"' EXIT
python3 - "$preflight_file" <<'PY'
import json, sys
from datetime import date
year = date.today().year
payload = {
    "基本资料": {
        "项目名称": "科研申报预检查验收",
        "申报年度": year,
        "申请经费": 0,
        "计划开始日期": f"{year}-01-01",
        "计划完成日期": f"{year}-12-31",
        "是否需要伦理评审": False,
        "是否需要伦理批文": False,
        "项目摘要": "预检查只读验收"
    },
    "参与人列表": [],
    "IsHold": True
}
json.dump(payload, open(sys.argv[1], 'w', encoding='utf-8'), ensure_ascii=False)
PY
preflight_encoded_path="$(python3 - <<'PY'
from urllib.parse import quote
print(quote('/YProject/预检查纵向项目申报', safe='/'))
PY
)"
preflight_status="$(curl --silent --show-error --output "$preflight_file" --write-out '%{http_code}' \
  -H "Authorization: Bearer ${access_token}" -H 'Content-Type: application/json' \
  --data-binary @"$preflight_file" "${base_url}${preflight_encoded_path}")"
if [[ "$preflight_status" != '200' ]]; then
  echo "项目申报预检查回归失败: 返回 ${preflight_status}" >&2
  head -c 500 "$preflight_file" >&2 || true
  exit 1
fi
python3 - "$preflight_file" <<'PY'
import json, sys
document = json.load(open(sys.argv[1], encoding='utf-8'))
data = document.get('data')
if not isinstance(data, dict) or not isinstance(data.get('检查项'), list):
    raise SystemExit('项目申报预检查回归失败: 缺少 data.检查项')
if not isinstance(data.get('通过'), bool) or not isinstance(data.get('材料完整'), bool):
    raise SystemExit('项目申报预检查回归失败: 缺少通过/材料完整标识')
PY
echo '/YProject/预检查纵向项目申报 OK (200)'

echo '科研项目、经费、伦理、人员、成果与绩效只读业务回归通过'
