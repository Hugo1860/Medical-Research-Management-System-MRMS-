#!/usr/bin/env bash
set -euo pipefail

# Static contract for finance incoming funds. This does not claim that a real
# finance adapter or external voucher has been connected.
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
migration="$root/database/postgresql/migrations/095_finance_incoming_claim_and_card.sql"
controller="$root/ScientificResearch/ScientificResearch/Controllers/IncomingController.cs"
model="$root/ScientificResearch/ScientificResearch/Models/Custom/Expense.cs"
for path in "$migration" "$controller" "$model"; do
  [[ -f "$path" ]] || { echo "经费到账契约失败: 缺少 $path" >&2; exit 1; }
done

for marker in \
  '到账费用提取方案' \
  '到账费用提取规则' \
  '财务到款登记' \
  '财务到款认领' \
  '项目经费卡' \
  'sp_财务到款登记' \
  'sp_财务到款确认' \
  'sp_财务到款认领' \
  'sp_财务到款批量认领' \
  'sp_项目经费卡拆分' \
  '真实财务回执'; do
  grep -Fq -- "$marker" "$migration" "$controller" || {
    echo "经费到账契约失败: 缺少 $marker" >&2
    exit 1
  }
done

for marker in \
  '获取到账费用提取方案' \
  '保存到账费用提取方案' \
  '预览到账费用提取' \
  '登记财务到款' \
  '确认财务到款' \
  '获取待认领财务到款' \
  '认领财务到款' \
  '批量认领财务到款' \
  '获取项目经费卡' \
  '拆分项目经费卡'; do
  grep -Fq -- "$marker" "$controller" || {
    echo "经费到账契约失败: 控制器缺少 $marker" >&2
    exit 1
  }
done

python3 - "$migration" <<'PY'
import pathlib, sys
p = pathlib.Path(sys.argv[1]).read_text(encoding='utf-8')
if '迁移 095' not in p or '"版本"=95' not in p or "'095_finance_incoming_claim_and_card.sql'" not in p:
    raise SystemExit('经费到账契约失败: 迁移版本或脚本登记不一致')
if '待财务核验' not in p or "'真实财务回执',false" not in p:
    raise SystemExit('经费到账契约失败: 未建立真实财务回执边界')
if 'ON CONFLICT ("外部系统编号","外部单据号")' not in p:
    raise SystemExit('经费到账契约失败: 缺少外部单据幂等约束')
print('经费到账静态契约通过：规则、到款池、认领/批量分配、内部经费卡拆分和真实回执边界已登记')
PY
