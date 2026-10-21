#!/usr/bin/env bash
set -euo pipefail

# Static-only contract for finance controls. Runtime database and supplier
# acceptance remain separate because OCR/HRP/ERP require real endpoints.
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
controller="$root/ScientificResearch/ScientificResearch/Controllers/ExpenseController.cs"
migration="$root/database/postgresql/migrations/078_finance_controls.sql"
api="$root/frontend/src/api/finance.ts"
view="$root/frontend/src/views/finance/expense-claims.vue"
release="$root/deploy/verify-local-release.sh"
adapter_gate="$root/deploy/verify-finance-adapter-contract.sh"
failures=0
fail() { echo "经费控制契约失败: $*" >&2; failures=$((failures + 1)); }

for path in "$controller" "$migration" "$api" "$view" "$release" "$adapter_gate"; do
  [[ -f "$path" ]] || fail "缺少 $path"
done

for marker in \
  '获取科研经费发票查重' \
  '登记科研经费发票' \
  '复核科研经费发票' \
  '获取项目经费预算调整' \
  '提交项目经费预算调整' \
  '审批项目经费预算调整' \
  '获取项目经费年度结算' \
  '回写项目付款状态' \
  'tfn_科研经费发票查重' \
  'OCR状态 = "人工录入"' \
  '发现疑似重复发票' \
  '实际付款必须使用已付款状态并填写付款日期' \
  'CanAccessProjectAsync'; do
  grep -Fq -- "$marker" "$controller" || fail "ExpenseController 缺少服务端约束: $marker"
done

for marker in \
  '科研经费发票' \
  '项目经费预算调整申请' \
  '项目经费预算版本' \
  '项目经费年度结算' \
  'tfn_科研经费发票查重' \
  'fn_科研经费发票封账检查' \
  'sp_项目经费年度结算' \
  '付款回写'; do
  grep -Fq -- "$marker" "$migration" || fail "078 迁移缺少经费控制结构: $marker"
done

for marker in \
  'checkFinanceInvoice' \
  'registerFinanceInvoice' \
  'reviewFinanceInvoice' \
  'getBudgetAdjustments' \
  'submitBudgetAdjustment' \
  'approveBudgetAdjustment' \
  'getYearSettlements' \
  'writebackPayment'; do
  grep -Fq -- "$marker" "$api" || fail "前端 API 缺少经费控制接口: $marker"
done

for forbidden in '模拟付款' 'HRP-VOUCHER-' '管理员.*1580' '合同金额: 100000' '剩余经费: 50000'; do
  if grep -Eq -- "$forbidden" "$controller" "$view"; then
    fail "发现经费示例/伪造回退: $forbidden"
  fi
done

grep -Fq './deploy/verify-finance-controls-contract.sh' "$release" || fail '统一发布门禁未纳入经费控制契约'
grep -Fq '采购订单关联字段' "$adapter_gate" || fail '经费适配器契约未登记采购订单关联字段检查'

if (( failures > 0 )); then
  echo "经费控制契约未通过: ${failures} 项" >&2
  exit 1
fi
echo '经费控制契约静态校验通过: 发票查重/OCR状态、预算调整、采购关联、年终结算和付款回写边界已登记（未连接运行库）'
