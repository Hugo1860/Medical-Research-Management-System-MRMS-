#!/usr/bin/env bash
set -euo pipefail

# 静态伦理流程契约门禁。只检查仓库中的流程边界，不连接数据库、不创建伦理申请，
# 也不把现代页面能打开误报为运行库已完成验收。
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
controller="$repo_root/ScientificResearch/ScientificResearch/Controllers/EthicsController.cs"
page="$repo_root/frontend/src/views/ethics/review-workflow.vue"
api="$repo_root/frontend/src/api/ethics.ts"
yproject="$repo_root/ScientificResearch/ScientificResearch/Controllers/YProjectController.cs"
migration="$repo_root/database/postgresql/migrations/074_ethics_review_workflow.sql"
archive_migration="$repo_root/database/postgresql/migrations/081_ethics_committee_archive.sql"
p0_migration="$repo_root/database/postgresql/migrations/089_ethics_p0_governance.sql"
p1_migration="$repo_root/database/postgresql/migrations/090_ethics_p1_scope_file_integrity.sql"
p2_migration="$repo_root/database/postgresql/migrations/091_ethics_p2_anonymous_vote.sql"

for path in "$controller" "$page" "$api" "$migration" "$archive_migration" "$p0_migration" "$p1_migration" "$p2_migration" "$yproject"; do
  [[ -f "$path" ]] || { echo "伦理流程契约校验失败: 缺少 $path" >&2; exit 1; }
done
for marker in '是否启用""=TRUE' '拒绝执行项目伦理回写' '批准/有效状态必须带批文编号'; do
  grep -Fq "$marker" "$yproject" || { echo "伦理流程契约校验失败: YProjectController 缺少 $marker" >&2; exit 1; }
done

controller_markers=(
  '提交伦理申请'
  '受理伦理申请'
  '登记伦理委员意见'
  '伦理委员'
  '登记伦理决定'
  '提交伦理持续记录'
  '审核伦理持续记录'
  '保存伦理会议'
  '维护伦理会议议程'
  '维护伦理会议委员'
  '确认伦理会议主席'
  '保存伦理会议纪要'
  '确认伦理会议纪要'
  '归档伦理电子材料'
  '持续记录审核必须填写处理结论'
  '不良事件必须填写严重程度'
  '不良事件必须填写发生日期'
  '正式伦理档案必须填写64位SHA-256文件校验值'
  '电子档案不能使用示例或演示文件路径'
  '只有正式指派、已确认出席且未回避的委员可以提交有效意见'
  '伦理正式决定必须具备已确认会议纪要和主席确认'
  '伦理正式决定未达到法定人数或有效委员意见数'
  '校验受控伦理文件'
  '被指派委员（最小披露）'
  '实际校验状态'
  '记录伦理审计(connection, transaction'
  '匿名表决'
  '严重不良事件'
  '获取伦理持续记录汇总'
)
for marker in "${controller_markers[@]}"; do
  grep -Fq "$marker" "$controller" || { echo "伦理流程契约校验失败: 控制器缺少 $marker" >&2; exit 1; }
done

page_markers=(
  '委员会审核'
  'reviewEthicsFollowup'
  '不良事件必须选择严重程度'
  '不良事件必须填写发生日期'
  'SHA-256校验值'
  '正式伦理档案必须填写64位十六进制SHA-256校验值'
  '登记委员意见'
  '归档决定书'
  'loadError'
  '流程模板编号: 410'
)
for marker in "${page_markers[@]}"; do
  if [[ "$marker" == '流程模板编号: 410' ]]; then
    if grep -Fq "$marker" "$page"; then echo '伦理流程契约校验失败: 前端仍保留固定流程模板示例值 410' >&2; exit 1; fi
  else
    grep -Fq "$marker" "$page" || { echo "伦理流程契约校验失败: 前端缺少 $marker" >&2; exit 1; }
  fi
done

grep -Fq "'/Ethics/审核伦理持续记录'" "$api" || {
  echo '伦理流程契约校验失败: API 未绑定持续记录审核接口' >&2
  exit 1
}
for marker in '伦理审查申请' '伦理持续审查记录' '伦理审查会议' '伦理审查委员意见' '伦理审查决定'; do
  grep -Fq "$marker" "$migration" || { echo "伦理流程契约校验失败: 074 缺少 $marker" >&2; exit 1; }
done
for marker in '伦理审查会议议程' '伦理审查会议纪要' '伦理电子档案'; do
  grep -Fq "$marker" "$archive_migration" || { echo "伦理流程契约校验失败: 081 缺少 $marker" >&2; exit 1; }
done
for marker in '伦理审查会议委员' '伦理审查会议表决' '伦理审查治理审计' 'tfn_伦理委员意见_正式指派校验' 'tfn_伦理决定_P0门禁' '法定人数' '主席确认人编号'; do
  grep -Fq "$marker" "$p0_migration" || { echo "伦理流程契约校验失败: 089 缺少 $marker" >&2; exit 1; }
done
for marker in '实际校验状态' 'ck_伦理电子档案_已归档必须核验' 'fn_伦理委员可见申请'; do
  grep -Fq "$marker" "$p1_migration" || { echo "伦理流程契约校验失败: 090 缺少 $marker" >&2; exit 1; }
done
for marker in '是否匿名表决' '匿名表决'; do
  grep -Fq "$marker" "$p2_migration" || { echo "伦理流程契约校验失败: 091 缺少 $marker" >&2; exit 1; }
done

if grep -Fq "r.\\\"名称\\\" IN ('系统管理员','科研管理员','科技办','科研科负责人','科研处处长')" "$migration"; then
  echo '伦理流程契约校验失败: 074 角色授权未包含独立伦理秘书/委员角色' >&2
  exit 1
fi

echo '伦理流程契约静态校验通过: 申请/受理/排期/会议意见/决定/持续记录审核/会议纪要/电子档案闭环；不良事件必填校验已覆盖（未连接运行库）'
