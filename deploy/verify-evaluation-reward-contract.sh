#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
controller="$root/ScientificResearch/ScientificResearch/Controllers/EvaluationController.cs"
view="$root/frontend/src/views/evaluation/performance-evaluation.vue"
for marker in \
  '确认科研评价奖励发放' \
  'paymentStatus == "已支付" && financeStatus != "已回写"' \
  '奖励标记为已支付前必须完成财务回写并取得财务凭证' \
  '已支付必须提供支付凭证号' \
  '财务凭证号'; do
  grep -Fq "$marker" "$controller" || { echo "奖励发放契约失败: Controller 缺少 $marker" >&2; exit 1; }
done
for marker in '财务回写状态' '支付状态' '支付凭证号'; do
  grep -Fq "$marker" "$view" || { echo "奖励发放契约失败: 前端缺少 $marker" >&2; exit 1; }
done
echo '奖励发放契约静态校验通过: 已支付必须有真实财务回写与支付凭证'
