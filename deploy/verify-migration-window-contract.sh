#!/usr/bin/env bash
set -euo pipefail

# Static-only safety gate for migrations 068-073. It does not connect to
# PostgreSQL and cannot prove that a runtime database has applied the window.
# It prevents the historical window from creating synthetic money, identities,
# paper metadata, biobank assets, HRP payments, or mobile delivery receipts.
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
migrations="$root/database/postgresql/migrations"

python3 - "$migrations" <<'PY'
from pathlib import Path
import sys

root = Path(sys.argv[1])
expected = {
    68: ("068_fix_vertical_application_sp.sql", [
        'CREATE OR REPLACE FUNCTION "sp_纵向项目申报_增改"',
        'RETURNING "编号" INTO v_new_id',
        "'tt_项目参与人'",
    ]),
    69: ("069_one_click_matching_balance_and_hrp_intermediate.sql", [
        'CREATE OR REPLACE FUNCTION "sp_一键建立配套项目"',
        'CREATE OR REPLACE FUNCTION "sp_一键建立结余项目"',
        'CREATE OR REPLACE FUNCTION "sp_按到账维护项目预算"',
        'p_action varchar(50) DEFAULT \'push_only\'',
        "p_action IS DISTINCT FROM 'push_only'",
    ]),
    70: ("070_milestone_warning_and_ethics_validity_shield.sql", [
        'CREATE OR REPLACE FUNCTION "sp_科研过程主动巡检与预警生成"',
        'CREATE OR REPLACE FUNCTION "sp_项目过程前置合规检查"',
        'CREATE OR REPLACE FUNCTION "sp_登记伦理批文续展与修正案"',
        "THEN '待核验'",
    ]),
    71: ("071_paper_active_push_disambiguation_and_cas_partition.sql", [
        'CREATE OR REPLACE FUNCTION sp_自动匹配论文期刊分区与影响因子',
        'CREATE OR REPLACE FUNCTION sp_论文网推消歧与智能推荐',
        "'未匹配期刊元数据，状态待核验'",
        "'未命中人员署名消歧记录，待人工核验'",
    ]),
    72: ("072_biobank_specimen_ledger_and_ethics_gated_dispense.sql", [
        'CREATE TABLE IF NOT EXISTS "生物样本资产"',
        'CREATE OR REPLACE FUNCTION sp_生物样本出库领用申请',
        'CREATE OR REPLACE FUNCTION sp_生物样本核准出库',
        'CREATE OR REPLACE FUNCTION sp_生物样本归还与消耗核销',
        '出库经办人必须由调用方显式提供',
        '归还验收人必须由调用方显式提供',
    ]),
    73: ("073_mobile_push_gateway_and_performance_formula_engine.sql", [
        'CREATE TABLE IF NOT EXISTS "移动协同消息配置"',
        'CREATE OR REPLACE FUNCTION sp_发送企业微信待办通知',
        "v_status := '待发送'",
        "v_status := '发送失败'",
        "current_setting('app.current_user_id', true)",
    ]),
}

for number, (filename, markers) in expected.items():
    path = root / filename
    if not path.exists():
        raise SystemExit(f'迁移窗口安全契约失败: 缺少 {filename}')
    text = path.read_text(encoding='utf-8-sig')
    missing = [marker for marker in markers if marker not in text]
    if missing:
        raise SystemExit(f'迁移窗口安全契约失败: {filename} 缺少 {missing}')
    if not text.lstrip().startswith('--') or 'BEGIN;' not in text or 'COMMIT;' not in text:
        raise SystemExit(f'迁移窗口安全契约失败: {filename} 必须包含事务边界')

forbidden = {
    69: [
        '100000.00', '20000.00', '1580.00', 'HRP-VOUCHER-',
        'INSERT INTO "hrp_sync_expense"', 'push_and_replay',
        "COALESCE(source_rec.\"批准经费\"",
    ],
    70: [
        "'有效' 并赋默认有效期", '+ INTERVAL \'1 year\'',
        '负责人编号" integer DEFAULT 0', "负责人姓名\" character varying(50) DEFAULT",
        "COALESCE((p_payload->>'登记人编号')::integer, 2)",
        "COALESCE(v_pi_name, '项目负责人')", "COALESCE(proj_rec.\"负责人编号\", 0)",
    ],
    71: [
        'INSERT INTO "科研单位规范"', 'INSERT INTO "科研单位别名"',
        'INSERT INTO "学者署名消歧库"', 'INSERT INTO "论文采集结果"',
        "VALUES (1, 'PUBMED_CROSSREF'", 'Nature Medicine', 'Cell Stem Cell',
        '张三', '李四', '王五', '王二麻子',
        '管理员或测试学者', 'FROM "人员" ORDER BY "编号" LIMIT 1',
        "'中科院3区'", '3.200', "'中科院4区'", '1.500',
    ],
    72: [
        'INSERT INTO "生物样本资产"', 'BB-2026-', '张三',
        '预置一笔', 'DEFAULT 150.00', '入库经办人编号" integer NOT NULL DEFAULT 2',
        "v_申请人姓名 := '科研人员'", "'样本库库管员'", "'样本库管理员'",
    ],
    73: [
        '模拟成功', '测试企业微信待办通知推送日志',
        'INSERT INTO "移动协同消息配置"', 'INSERT INTO "科研绩效动态公式表"',
        'INSERT INTO "科研自定义报表模板"', 'PRJ-2026-001',
        '建立人编号" integer NOT NULL DEFAULT 2',
        'VALUES (v_rule_id, \'PAPER_SCI\'',
        'VALUES (v_rule_id, \'PROJ_VERT\'',
        'VALUES (v_rule_id, \'PROJ_HORIZ\'',
        'VALUES (v_rule_id, \'PATENT_IP\'',
    ],
}
for number, markers in forbidden.items():
    filename = expected[number][0]
    text = (root / filename).read_text(encoding='utf-8-sig')
    found = [marker for marker in markers if marker in text]
    if found:
        raise SystemExit(f'迁移窗口安全契约失败: {filename} 仍含禁止的示例/回退逻辑 {found}')

# Active 073 push function must not claim external delivery. Historical 082 is
# the later runtime overlay; this check ensures both layers retain truthful
# states even when a fresh database replays migrations sequentially.
push = (root / expected[73][0]).read_text(encoding='utf-8-sig')
start = push.index('CREATE OR REPLACE FUNCTION sp_发送企业微信待办通知')
end = push.index('-- 测试移动通道只做本地配置检查', start)
body = push[start:end]
if "v_status := '已发送'" in body or 'errcode' in body or 'msgid' in body:
    raise SystemExit('迁移窗口安全契约失败: 073 推送函数伪造了外部成功回执')

print('迁移窗口安全契约静态校验通过: 068-073 无示例金额、身份、样本、论文元数据或移动成功回执；仅保留真实流程结构（未连接运行库）')
PY
