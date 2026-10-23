#!/usr/bin/env bash
set -euo pipefail

# Static-only migration feature gate. It does not connect to PostgreSQL and
# therefore cannot prove a runtime database has applied these migrations.
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
migration_dir="$root/database/postgresql/migrations"
settings="$root/ScientificResearch/ScientificResearch/appsettings.json"
expected=110

python3 - "$root" "$expected" <<'PY'
import json, re, sys
from pathlib import Path

root = Path(sys.argv[1])
expected = int(sys.argv[2])
migration_dir = root / 'database' / 'postgresql' / 'migrations'
files = sorted(migration_dir.glob('[0-9][0-9][0-9]_*.sql'))
numbers = [int(p.name[:3]) for p in files]
if numbers != list(range(1, expected + 1)):
    raise SystemExit(f'迁移特征契约失败: 序列不是 001-{expected:03d}: {numbers[-20:]}')

markers = {
    68: ('纵向项目申报', 'CREATE OR REPLACE FUNCTION'),
    69: ('hrp_sync_project', 'sp_hrp中间库双向协同'),
    70: ('科研项目过程预警记录', '伦理批文有效期至'),
    71: ('论文采集结果', '中科院分区'),
    72: ('生物样本资产', '伦理核验'),
    73: ('移动协同消息配置', 'sp_发送企业微信待办通知'),
    74: ('伦理审查申请', '伦理持续审查记录'),
    75: ('成果转化', '收益分配'),
    76: ('科研伦理审查', '角色权限'),
    77: ('纵向项目申报预检查记录', '纵向项目任务书'),
    78: ('科研经费发票', '项目经费年度结算'),
    79: ('人员科研能力', 'v1_人员团队项目分工'),
    80: ('科研评价年度批次', '支付凭证号'),
    81: ('伦理审查会议议程', '伦理电子档案'),
    82: ('移动协同消息配置', '真实网关'),
    83: ('科研成果治理历史', '科研成果合并审计'),
    84: ('专家可用时间', '外部专家邀请'),
    85: ('sp_hrp中间库双向协同', 'push_only'),
    86: ('科研成果状态凭证', '科研成果状态凭证核验'),
    87: ('科研评价奖励支付批次', '科研评价奖励支付流水'),
    88: ('横向项目生命周期申请', '横向项目研究报告', '横向项目档案材料'),
    89: ('伦理审查会议委员', '伦理审查会议表决', '伦理审查治理审计', 'tfn_伦理决定_P0门禁'),
    90: ('实际校验状态', 'ck_伦理电子档案_已归档必须核验', 'fn_伦理委员可见申请'),
    91: ('是否匿名表决', '匿名表决', '伦理表决'),
    92: ('进修年度计划', '进修申请', '进修审批流程配置', '已占用预算', '进修审批待办'),
    93: ('科研平台', '科研服务事项', '科研服务申请', '科研导入导出日志'),
    94: ('院级项目申报批次', '资格规则', '评审方案'),
    95: ('到账费用提取方案', '财务到款登记', '项目经费卡', 'sp_财务到款登记'),
    96: ('教育部统计任务', '教育部统计数据包', '教育部统计字段校验', '教育部统计导出任务'),
    97: ('科研成果扩展记录', '知识产权记录', '获奖申报批次', '获奖申报评审'),
    98: ('科研文档', '科研文档版本', '消息发送队列', '科研服务申请'),
    99: ('首页组件配置', '业务催办', '首页配置审计'),
    100: ('横向合同版本', '横向合作单位资质', '横向合同收入认定', '横向合同模板'),
    101: ('项目分类', '预算科目模板', '项目编号规则', 'fn_生成项目编号'),
    102: ('外拨经费', '经费结转', '经费分割方案', '结项结余处理', '经费执行审计'),
    103: ('平台仪器设备', '仪器预约', '仪器使用记录', '平台年报'),
    104: ('主办会议申请', '学术交流人员', '期刊分区库', 'fn_匹配期刊分区'),
    105: ('纵向项目导入批次', '纵向项目导入明细'),
    106: ('系统附件', '系统附件访问日志', 'fn_防篡改_系统附件访问日志'),
    107: ('sp_导入论文', '成果作者'),
    108: ('成果论文', '第一作者'),
    109: ('sp_主办讲座_增改', 'RETURNING "编号" INTO v_new_id'),
    110: ('办理人员编号', '科研服务事项支持从人员库选择办理人员'),
}
for number, required in markers.items():
    path = next((p for p in files if int(p.name[:3]) == number), None)
    if path is None:
        raise SystemExit(f'迁移特征契约失败: 缺少 {number:03d}')
    text = path.read_text(encoding='utf-8-sig')
    missing = [marker for marker in required if marker not in text]
    if missing:
        raise SystemExit(f'迁移特征契约失败: {path.name} 缺少 {missing}')

cfg = json.loads((root / 'ScientificResearch' / 'ScientificResearch' / 'appsettings.json').read_text(encoding='utf-8-sig'))
actual = cfg.get('Postgres', {}).get('ExpectedSchemaVersion')
if actual != expected:
    raise SystemExit(f'迁移特征契约失败: appsettings ExpectedSchemaVersion={actual}，期望 {expected}')
print(f'迁移特征契约静态校验通过: 001-{expected:03d} 连续，068-{expected:03d} 关键对象标记齐全（未连接运行库）')
PY
