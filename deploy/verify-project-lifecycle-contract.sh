#!/usr/bin/env bash
set -euo pipefail

# Static-only contract for the vertical-project lifecycle foundation.  This
# gate verifies that the source keeps the preflight, taskbook, local-confirmation
# and archive-list boundaries intact.  It does not connect to PostgreSQL, create
# projects, call a signature/archive supplier, or treat a page opening as UAT.
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
controller="$root/ScientificResearch/ScientificResearch/Controllers/YProjectController.cs"
models="$root/ScientificResearch/ScientificResearch/Models/Custom/YProject.cs"
migration="$root/database/postgresql/migrations/077_project_precheck_taskbook_archive.sql"
settings="$root/ScientificResearch/ScientificResearch/appsettings.json"
release="$root/deploy/verify-local-release.sh"

for path in "$controller" "$models" "$migration" "$settings" "$release"; do
  [[ -f "$path" ]] || { echo "项目全生命周期契约失败: 缺少 $path" >&2; exit 1; }
done

python3 - "$controller" "$models" "$migration" "$settings" "$release" <<'PY'
import json
import pathlib
import re
import sys

controller_path, models_path, migration_path, settings_path, release_path = map(pathlib.Path, sys.argv[1:])
controller = controller_path.read_text(encoding='utf-8-sig')
models = models_path.read_text(encoding='utf-8-sig')
migration = migration_path.read_text(encoding='utf-8-sig')
settings = json.loads(settings_path.read_text(encoding='utf-8-sig'))
release = release_path.read_text(encoding='utf-8-sig')

def require(text, marker, label):
    if marker not in text:
        raise SystemExit(f'项目全生命周期契约失败: {label} 缺少 {marker!r}')

def require_all(text, markers, label):
    for marker in markers:
        require(text, marker, label)

# 1) Application preflight: both eligibility and completeness must be server
# side checks, persisted as a snapshot, and enforced before workflow creation.
require_all(controller, [
    '预检查纵向项目申报',
    '获取纵向项目申报预检查',
    '记录项目申报预检查',
], 'YProjectController')
for marker in ('APPLICANT_ACTIVE', 'APPLICANT_SCOPE', 'PLAN_WINDOW',
               'APPLICATION_QUOTA', 'APPLICATION_DOCUMENT', 'SUMMARY',
               'ETHICS_DOCUMENT', 'PARTICIPANT_STATUS', '材料完整',
               '项目申报预检查未通过'):
    require(controller, marker, '申报资格/材料预检')
if 'var preflight = await 获取纵向项目申报预检查' not in controller:
    raise SystemExit('项目全生命周期契约失败: 正式申报未调用服务端预检查')
if 'if (!preflight.通过)' not in controller:
    raise SystemExit('项目全生命周期契约失败: 预检查失败未阻断正式申报')
if 'INSERT INTO ""纵向项目申报预检查记录""' not in controller:
    raise SystemExit('项目全生命周期契约失败: 预检查结果未写入留痕表')

# The source uses a Chinese applicant check, not the English word above.  Keep
# this explicit check to prevent a future accidental relaxation of that gate.
require(controller, '申报人状态', '申报资格/材料预检')
require(controller, '申报人操作范围', '申报资格/材料预检')

# 2) Taskbook: generated content must be valid JSON, include source snapshots,
# persist as jsonb, and allocate a monotonic per-project version under a DB
# uniqueness constraint.  No fixed/demo version is accepted.
require_all(models, [
    '纵向项目任务书生成请求', '任务书内容', '上传修订',
], 'YProject models')
require_all(controller, [
    '生成纵向项目任务书',
    'JsonConvert.SerializeObject',
    'JToken.Parse(content)',
    '任务书内容必须是合法 JSON',
    'CAST(@任务书内容 AS jsonb)',
    'COALESCE(MAX(""版本号""),0)+1',
    "CASE WHEN @上传修订 THEN '上传修订' ELSE '系统生成' END",
    "'草稿'",
    'tfn_项目计划(',
    'tfn_项目经费预算(',
    'tfn_项目预期成果(',
], '任务书生成/版本')
require_all(migration, [
    '"任务书内容" jsonb NOT NULL',
    '"uq_纵向项目任务书_版本" UNIQUE ("纵向项目立项编号", "版本号")',
    '"ck_纵向项目任务书_版本" CHECK ("版本号" > 0)',
    '"ck_纵向项目任务书_生成方式" CHECK ("生成方式" IN (\'系统生成\',\'上传修订\'))',
], '077 任务书迁移')

# 3) Internal confirmation is an auditable workflow distinct from legal
# signature.  The signer is active and tied to the project (or a management
# role), confirmation is self-scoped, and effectiveness waits for all records.
require_all(models, [
    '纵向项目任务书签署请求', '纵向项目任务书确认请求',
    '签署角色', '签署说明',
], '内部确认模型')
require_all(controller, [
    '发起纵向项目任务书内部签署',
    '确认纵向项目任务书内部签署',
    '生效纵向项目任务书',
    '签署角色必须是项目负责人、科研秘书、科室负责人、科研管理员或科技办',
    '签署人不存在或已停用',
    'signerInProject',
    'CurrentUser.编号',
    "'待签署'",
    "'已确认'",
    '仍有未完成的内部确认，不能生效任务书',
    '内部确认不构成法律电子签章',
], '内部确认流程')
require_all(migration, [
    '"签署方式" varchar(20) NOT NULL DEFAULT \'内部确认\'',
    '"ck_纵向项目任务书签署_方式" CHECK ("签署方式" = \'内部确认\')',
    '"ck_纵向项目任务书签署_状态" CHECK ("状态" IN (\'待签署\',\'已确认\',\'已拒绝\',\'已撤回\'))',
], '077 内部确认迁移')

# 4) Formal archive: required material checklist has deterministic codes,
# unique project/code rows, explicit missing/upload/verified states, and a
# server-side missing-material block before verification.
require_all(controller, [
    '默认档案清单',
    '确保项目档案清单',
    '获取纵向项目电子档案清单',
    '保存纵向项目电子档案清单项',
    '核验纵向项目电子档案清单',
    '""状态""=\'缺失\'',
    '仍缺少',
    '""状态""=\'已核验\'',
    'APPROVAL', 'TASKBOOK', 'MIDTERM', 'CLOSEOUT', 'FINANCE',
], '项目档案清单')
require_all(migration, [
    '"纵向项目电子档案清单"',
    '"uq_纵向项目电子档案清单_项目代码" UNIQUE',
    '"ck_纵向项目电子档案清单_状态" CHECK',
    "'缺失','已上传','已核验','不适用','作废'",
], '077 档案清单迁移')

# 5) Provider boundary: default configuration is fail-closed.  Local signing
# and archive retention must never be reported as a legal signature or formal
# archive; provider submission remains a 503/501 gate until a real adapter is
# explicitly configured and accepted.
for section in ('ElectronicSignature', 'ElectronicArchive'):
    cfg = settings.get(section)
    if not isinstance(cfg, dict):
        raise SystemExit(f'项目全生命周期契约失败: 缺少 {section} 配置')
    for key in ('Enabled', 'Provider', 'Endpoint', 'ProtocolVersion', 'AdapterReady', 'RequireHttps'):
        if key not in cfg:
            raise SystemExit(f'项目全生命周期契约失败: {section}.{key} 未定义')
    if cfg['Enabled'] is not False or cfg['AdapterReady'] is not False:
        raise SystemExit(f'项目全生命周期契约失败: 默认 {section} 必须关闭且未验收')
    if cfg['RequireHttps'] is not True:
        raise SystemExit(f'项目全生命周期契约失败: 默认 {section}.RequireHttps 必须为 true')

provider_region = controller[controller.index('#region 正式电子签章与电子档案服务契约'):]
require_all(provider_region, [
    '读取外部文档服务契约',
    'DOCUMENT_PROVIDER_NOT_READY',
    'DOCUMENT_PROVIDER_ADAPTER_NOT_IMPLEMENTED',
    '提交纵向项目任务书电子签章',
    '提交纵向项目正式电子档案',
    '未调用外部服务，也未改变任务书状态',
    '未调用外部服务，也未改变本地档案状态',
    '只有外部合格电子签章服务返回并验收后，才可声明电子签章效力',
    '只有外部正式电子档案服务完成接入和验收后，才可声明正式电子档案托管',
], '正式供应商边界')
if 'GetRequiredService<IHttpClientFactory>' in provider_region or 'new HttpClient' in provider_region:
    raise SystemExit('项目全生命周期契约失败: 当前正式文档门禁不应在适配器未验收时调用外部 HTTP')
if provider_region.count('StatusCode(501') < 2:
    raise SystemExit('项目全生命周期契约失败: 电子签章/正式档案提交均需保留 501 未实现边界')

if './deploy/verify-project-lifecycle-contract.sh' not in release:
    raise SystemExit('项目全生命周期契约失败: verify-local-release.sh 未纳入静态门禁')

print('项目全生命周期契约静态校验通过: 资格预检、材料完整性、任务书 JSON/版本、内部确认、档案清单与正式供应商边界均已固化（未连接运行库）')
PY
