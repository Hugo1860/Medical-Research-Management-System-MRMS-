#!/usr/bin/env bash
set -euo pipefail

# Read-only source gate: a failed API must produce an empty/explicit error state,
# never fabricated project, login, role, claimant, or sample records.
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
dashboard="$root/frontend/src/views/dashboard/index.vue"
login_log="$root/frontend/src/views/system/login-log.vue"
role_management="$root/frontend/src/views/system/role-management.vue"
governance="$root/frontend/src/views/governance/proxy-reconciliation.vue"
fruit="$root/ScientificResearch/ScientificResearch/Controllers/FruitController.cs"
biobank="$root/ScientificResearch/ScientificResearch/Controllers/BiobankController.cs"
business_contract="$root/deploy/verify-business-contract.sh"

for marker in "getAnalyticsOverview('overview')" 'metrics.map((item)' '暂无真实统计数据'; do
  grep -Fq "$marker" "$dashboard" || { echo "示例回退契约失败: 首页缺少真实统计/空状态标记 $marker" >&2; exit 1; }
done

for marker in \
  'BUSINESS_Y_PROJECT_ID:?' \
  'BUSINESS_X_PROJECT_ID:?' \
  'BUSINESS_Y_FLOW_TEMPLATE_ID:?'; do
  if ! grep -Fq "$marker" "$business_contract"; then
    echo "示例回退契约失败: 业务回归脚本缺少真实项目编号强制输入 $marker" >&2
    exit 1
  fi
done

for forbidden in 'BUSINESS_Y_PROJECT_ID:=3' 'BUSINESS_X_PROJECT_ID:=1' 'BUSINESS_Y_FLOW_TEMPLATE_ID:=410'; do
  if grep -Fq "$forbidden" "$business_contract"; then
    echo "示例回退契约失败: 业务回归脚本仍回退到示例编号 $forbidden" >&2
    exit 1
  fi
done

for forbidden in \
  'data: [120, 150, 180' \
  'value: 42, name: '\''SCI/EI高水平论文'\''' \
  '使用模拟审计数据' \
  '使用安全兜底模式' \
  '编号: 1001' \
  '默认管理员' \
  '脑机接口临床转化研究重点攻关项目' \
  '示范数据' \
  '|| 3' \
  'CurrentUser.编号 > 0 ? CurrentUser.编号 : 2' \
  'CurrentUser?.编号 > 0 ? CurrentUser.编号 : 2'; do
  if grep -Fq "$forbidden" "$dashboard" "$login_log" "$role_management" "$governance" "$fruit" "$biobank"; then
    echo "示例回退契约失败: 发现禁止的示例/管理员回退 $forbidden" >&2
    exit 1
  fi
done

echo '示例数据回退契约静态校验通过: 统计、审计日志、角色和写入人均不使用伪造记录'
