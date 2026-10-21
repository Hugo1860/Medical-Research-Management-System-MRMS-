#!/usr/bin/env bash
set -euo pipefail

# P2 ethics acceptance matrix. Static checks prove the workflow surfaces exist;
# an optional ETHICS_P2_TOKEN performs read-only runtime probes without creating
# or modifying ethics business records.
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
controller="$root/ScientificResearch/ScientificResearch/Controllers/EthicsController.cs"
page="$root/frontend/src/views/ethics/review-workflow.vue"
api="$root/frontend/src/api/ethics.ts"
migration="$root/database/postgresql/migrations/091_ethics_p2_anonymous_vote.sql"
report="$root/reports/ethics-p2-acceptance-$(date +%Y%m%d-%H%M%S).tsv"
mkdir -p "$root/reports"

printf '场景\t静态契约\t运行时只读探针\t说明\n' > "$report"
check_marker() {
  local scenario="$1" marker="$2" file="$3"
  if grep -Fq -- "$marker" "$file"; then
    printf '%s\t通过\t未执行\t%s\n' "$scenario" "$marker" >> "$report"
  else
    printf '%s\t失败\t未执行\t缺少 %s\n' "$scenario" "$marker" >> "$report"
    return 1
  fi
}

check_marker '初审' '提交伦理申请' "$controller"
check_marker '修正案' '修正案' "$controller"
check_marker '持续审查' '提交伦理持续记录' "$controller"
check_marker '委员回避' '委员回避' "$controller"
check_marker '匿名投票' '获取伦理匿名表决' "$controller"
check_marker '决定书归档' '归档伦理电子材料' "$controller"
check_marker '严重不良事件上报' '不良事件必须填写严重程度' "$controller"
check_marker '逾期提醒' '查询伦理到期提醒' "$controller"
check_marker '权限回收' '回收人员代理' "$root/ScientificResearch/ScientificResearch/Controllers/AccessController.cs"
check_marker '匿名投票迁移' '是否匿名表决' "$migration"
check_marker '匿名投票前端' '匿名表决汇总' "$page"
check_marker '匿名投票 API' '获取伦理匿名表决' "$api"

if [[ -n "${ETHICS_P2_TOKEN:-}" ]]; then
  base="${APP_BASE_URL:-http://127.0.0.1:16379}"
  auth=( -H "Authorization: Bearer ${ETHICS_P2_TOKEN}" )
  runtime_failures=0
  probe() {
    local scenario="$1" path="$2" expected="$3" code
    code="$(curl -sS "${auth[@]}" -o /dev/null -w '%{http_code}' "$base$path" || true)"
    if [[ "$code" == "$expected" ]]; then
      printf '%s\t通过\t%s\tHTTP %s\n' "$scenario" "$code" "$path" >> "$report"
    else
      printf '%s\t通过\t失败\t%s 返回 %s（期望 %s）\n' "$scenario" "$path" "$code" "$expected" >> "$report"
      runtime_failures=$((runtime_failures + 1))
    fi
  }
  # Encode the Chinese query key explicitly; otherwise curl may send an invalid
  # request target and the server can return 400 before authentication runs.
  probe '逾期提醒' '/Ethics/查询伦理到期提醒?%E6%8F%90%E5%89%8D%E5%A4%A9%E6%95%B0=60' 200
  probe '严重不良事件上报' '/Ethics/获取伦理持续记录汇总' 200
  printf '匿名投票\t通过\t需会议编号\t运行时探针需提供 ETHICS_P2_MEETING_ID\n' >> "$report"
  if [[ -n "${ETHICS_P2_MEETING_ID:-}" ]]; then
    probe '匿名投票' "/Ethics/获取伦理匿名表决?%E4%BC%9A%E8%AE%AE%E7%BC%96%E5%8F%B7=${ETHICS_P2_MEETING_ID}" 200
  fi
  if (( runtime_failures > 0 )); then
    echo "伦理 P2 运行时只读探针失败: ${runtime_failures} 项（请刷新 Keycloak 令牌后重试）" >&2
    cat "$report"
    exit 1
  fi
else
  printf '真实写入场景\t通过\t未执行\t未提供 ETHICS_P2_TOKEN；静态契约不冒充真实业务写入验收\n' >> "$report"
fi

echo "伦理 P2 验收矩阵已生成: $report"
cat "$report"
