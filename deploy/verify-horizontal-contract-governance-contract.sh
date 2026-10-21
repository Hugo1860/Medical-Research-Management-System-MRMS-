#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
fail(){ echo "横向合同治理契约失败: $1" >&2; exit 1; }
migration="$root/database/postgresql/migrations/100_horizontal_contract_governance.sql"
controller="$root/ScientificResearch/ScientificResearch/Controllers/HorizontalContractGovernanceController.cs"
api="$root/frontend/src/api/horizontal-contract-governance.ts"
html="$root/ScientificResearch/ScientificResearch/wwwroot/IMIS/views/科研办公/科研办公/横向合同治理.html"
for file in "$migration" "$controller" "$api" "$html"; do [[ -f "$file" ]] || fail "缺少 $file"; done
for marker in '横向合同版本' '横向合作单位资质' '横向合同收入认定' '横向合同模板' '"版本"=100' '211'; do grep -Fq "$marker" "$migration" || fail "100 缺少 $marker"; done
for marker in '查询合同治理' '保存合同版本' '更新合同签审状态' '保存合作方资质' '核验合作方资质' '保存收入认定' '查询合同模板'; do grep -Fq "$marker" "$controller" || fail "控制器缺少 $marker"; done
grep -Fq '/HorizontalContractGovernance/查询合同治理' "$api" || fail '前端未接入横向合同治理接口'
echo '横向合同治理契约静态校验通过: 版本签审、合作方资质、收入认定和模板库已登记。'
