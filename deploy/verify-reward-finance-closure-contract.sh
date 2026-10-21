#!/usr/bin/env bash
set -euo pipefail

# Static-only gate for reward payment batches and finance/payroll closure.
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
controller="$root/ScientificResearch/ScientificResearch/Controllers/EvaluationController.cs"
migration="$root/database/postgresql/migrations/087_evaluation_reward_finance_closure.sql"
api="$root/frontend/src/api/evaluation.ts"
for path in "$controller" "$migration"; do [[ -f "$path" ]] || { echo "奖励财务闭环契约失败: 缺少 $path" >&2; exit 1; }; done
[[ -f "$api" ]] || { echo "奖励财务闭环契约失败: 缺少 $api" >&2; exit 1; }
for marker in \
  '创建科研评价奖励支付批次' '获取科研评价奖励支付批次' \
  '提交科研评价奖励支付批次' '回写科研评价奖励支付' '对账科研评价奖励支付批次' \
  '科研评价奖励支付流水' '支付批次编号' '财务请求幂等键' '支付失败原因' \
  '提交财务' '回写成功' '回写失败' '对账一致' '对账不一致' \
  '拒绝演示凭证' '幂等命中'; do
  grep -Fq "$marker" "$controller" || { echo "奖励财务闭环契约失败: 控制器缺少 $marker" >&2; exit 1; }
done
for marker in 'submitEvaluationRewardPaymentBatch' 'callbackEvaluationRewardPayment' 'reconcileEvaluationRewardPaymentBatch' '不会在浏览器内伪造外部支付调用'; do
  grep -Fq "$marker" "$api" || { echo "奖励财务闭环契约失败: 前端 API 缺少 $marker" >&2; exit 1; }
done
for marker in '科研评价奖励支付批次' '科研评价奖励支付流水' '不可变审计记录' 'trg_科研评价奖励支付_状态约束' '奖励标记已支付必须绑定支付批次'; do
  grep -Fq "$marker" "$migration" || { echo "奖励财务闭环契约失败: 087 缺少 $marker" >&2; exit 1; }
done
echo '奖励财务闭环静态契约通过: 支付批次、幂等键、凭证约束、失败状态和不可变流水已登记（未连接运行库）'
