#!/usr/bin/env bash
set -euo pipefail

# Static contract for the horizontal-project lifecycle.  It verifies that
# delay/suspend/terminate, reports, contract-payment nodes and archive records
# are backed by real server data and that the SPA has no synthetic status or
# progress fallback.  It never connects to PostgreSQL or calls a supplier.
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
controller="$root/ScientificResearch/ScientificResearch/Controllers/XProjectController.cs"
models="$root/ScientificResearch/ScientificResearch/Models/Custom/XProject.cs"
expense="$root/ScientificResearch/ScientificResearch/Controllers/ExpenseController.cs"
migration="$root/database/postgresql/migrations/088_horizontal_project_lifecycle_archive.sql"
frontend="$root/frontend/src/views/project/horizontal-contract.vue"
api="$root/frontend/src/api/horizontal-project.ts"
release="$root/deploy/verify-local-release.sh"

for path in "$controller" "$models" "$expense" "$migration" "$frontend" "$api" "$release"; do
  [[ -f "$path" ]] || { echo "横向项目全生命周期契约失败: 缺少 $path" >&2; exit 1; }
done

python3 - "$controller" "$models" "$expense" "$migration" "$frontend" "$api" "$release" <<'PY'
import pathlib
import re
import sys

controller_path, models_path, expense_path, migration_path, frontend_path, api_path, release_path = map(pathlib.Path, sys.argv[1:])
controller = controller_path.read_text(encoding='utf-8-sig')
models = models_path.read_text(encoding='utf-8-sig')
expense = expense_path.read_text(encoding='utf-8-sig')
migration = migration_path.read_text(encoding='utf-8-sig')
frontend = frontend_path.read_text(encoding='utf-8-sig')
api = api_path.read_text(encoding='utf-8-sig')
release = release_path.read_text(encoding='utf-8-sig')

def require(text, marker, label):
    if marker not in text:
        raise SystemExit(f'横向项目全生命周期契约失败: {label} 缺少 {marker!r}')

def require_all(text, markers, label):
    for marker in markers:
        require(text, marker, label)

# 1) Database objects: no sample project, amount, user or payment data may be
# inserted by the lifecycle migration.  Existing generic payment-node tables
# are linked by template 450 at the application boundary.
require_all(migration, [
    '088_horizontal_project_lifecycle_archive.sql',
    'CREATE TABLE IF NOT EXISTS "横向项目生命周期申请"',
    'CREATE TABLE IF NOT EXISTS "横向项目研究报告"',
    'CREATE TABLE IF NOT EXISTS "横向项目档案材料"',
    '"uq_横向项目生命周期申请_待审核"',
    '"uq_横向项目研究报告_项目类型年度"',
    '"uq_横向项目档案材料_版本"',
    '"ck_横向项目生命周期申请_延期日期"',
    '"ck_横向项目研究报告_内容"',
    '"ck_横向项目档案材料_校验和"',
    '"ck_横向项目档案材料_路径"',
    '"版本" = 88',
], '088 横向项目迁移')
if re.search(r'INSERT\s+INTO\s+"(?:横向项目|横向项目生命周期申请|横向项目研究报告|横向项目档案材料|项目合同付款节点)"', migration, re.I):
    raise SystemExit('横向项目全生命周期契约失败: 088 不得写入示例项目、合同、金额、身份或付款节点')

# 2) Server workflow: every lifecycle write/list endpoint must check project
# scope; approval is role guarded and changes the contract end date only after
# an approved delay.  Reports and archives retain author/status/checksum.
require_all(models, [
    '横向项目生命周期申请新增', '横向项目生命周期审核',
    '横向项目研究报告新增', '横向项目研究报告审核',
    '横向项目档案材料新增',
], '横向项目模型')
require_all(controller, [
    '获取横向项目生命周期档案', '申请横向项目延期暂停终止', '审核横向项目生命周期申请',
    '提交横向项目研究报告', '审核横向项目研究报告', '归档横向项目材料', '上传横向项目档案材料',
    '新增横向项目变更', '完善横向项目变更资料',
    'CanAccessProjectAsync(横向项目申请流程模板编号',
    '事件类型必须是延期、暂停或终止', '""状态""=\'待审核\'',
    '项目已结题或终止，不能再申请生命周期变更', '延期后的合同结束日期必须晚于当前合同结束日期',
    'HasAnyRoleAsync("科研管理员", "系统管理员")', 'UPDATE ""横向项目"" SET ""合同结束时间""',
    'var status = model.提交审核 ? "待审核" : "草稿"', '同年度同类型报告已由其他人员提交审核，不能覆盖',
    '文件校验和必须是64位SHA-256', 'CurrentUser.编号',
    '项目已结题、终止或暂停，不能新增合同变更',
], '横向项目控制器')
if 'SELECT 1 FROM ""横向项目生命周期申请""' not in controller or 'FROM ""项目合同付款节点""' not in controller:
    raise SystemExit('横向项目全生命周期契约失败: 生命周期和合同付款节点未在详情接口关联')

# 3) Generic finance endpoints must retain a horizontal project boundary and
# never accept an arbitrary project merely because a caller supplies template
# 450.  The existing access query must include the horizontal table.
require_all(expense, [
    '获取项目合同付款节点', '保存项目合同付款节点',
    '""横向项目"" WHERE @流程模板编号 = 450',
    '项目合同付款节点', '状态 is not ("计划" or "待申请"',
], '横向合同付款节点')

# 4) SPA: status, archive and execution progress must be server supplied.  A
# missing value is rendered as an explicit empty state rather than a claim.
for forbidden in ('合格结题', '决算已留痕', ':percentage="65"', '合同金额: 100000', 'new Date(Date.now() + 365', '委托研发', '生物医药与健康', '双方共有 / 委托方所有'):
    if forbidden in frontend:
        raise SystemExit(f'横向项目全生命周期契约失败: 前端仍含伪造回退 {forbidden!r}')
require_all(frontend, [
    'row.验收结果', '未提供验收结论', 'row.归档状态', '未提供档案状态',
    'expenseProgress', '尚未取得真实经费执行数据',
], '横向项目页面')
require_all(api, [
    '归档状态?', '批准预算?', '已审核支出?', '已付款金额?',
    'getHorizontalProjectLifecycle', 'applyHorizontalProjectLifecycle',
    'submitHorizontalResearchReport', 'archiveHorizontalProjectMaterial',
], '横向项目 API 类型')

if './deploy/verify-horizontal-lifecycle-contract.sh' not in release:
    raise SystemExit('横向项目全生命周期契约失败: verify-local-release.sh 未纳入静态门禁')

print('横向项目全生命周期契约静态校验通过: 延期/暂停/终止、研究报告、合同付款节点、档案留痕与前端真实状态边界已固化（未连接运行库）')
PY
