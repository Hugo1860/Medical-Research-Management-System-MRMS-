#!/usr/bin/env bash
set -euo pipefail

# Static P0 contract for the continuing-education module. Runtime acceptance
# must still use a refreshed authenticated role token and real records.
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
migration="$root/database/postgresql/migrations/092_continuing_education_p0.sql"
controller="$root/ScientificResearch/ScientificResearch/Controllers/ContinuingEducationController.cs"
model="$root/ScientificResearch/ScientificResearch/Models/Custom/ContinuingEducation.cs"
api="$root/frontend/src/api/further-education.ts"
page="$root/frontend/src/views/office/further-education.vue"
router="$root/frontend/src/router/index.ts"

for path in "$migration" "$controller" "$model" "$api" "$page" "$router"; do
  [[ -f "$path" ]] || { echo "进修 P0 契约失败: 缺少 $path" >&2; exit 1; }
done

for marker in \
  '进修年度计划' \
  '进修申请' \
  '进修审批流程配置' \
  '进修审批待办' \
  '进修通知' \
  '进修操作审计' \
  '预算预占' \
  '预算释放'; do
  grep -Fq -- "$marker" "$migration" "$controller" || {
    echo "进修 P0 契约失败: 缺少 $marker" >&2
    exit 1
  }
done

for marker in \
  '查询进修年度计划' \
  '保存进修年度计划' \
  '提交进修申请' \
  '审批进修申请' \
  '撤回进修申请' \
  '查询进修预算台账' \
  '查询进修审计' \
  '查询进修待办' \
  '获取进修待办' \
  '查询进修通知' \
  '标记进修通知已读' \
  '保存进修流程配置' \
  '发布进修流程配置'; do
  grep -Fq -- "$marker" "$controller" || {
    echo "进修 P0 契约失败: 控制器缺少 $marker" >&2
    exit 1
  }
done

for marker in \
  '/FurtherEducation' \
  '人员进修管理' \
  '预算预占与审计归档'; do
  grep -Fq -- "$marker" "$api" "$page" "$router" || {
    echo "进修 P0 契约失败: 前端缺少 $marker" >&2
    exit 1
  }
done

echo '进修 P0 静态契约通过：年度计划、申请、可配置审批、预算预占/释放、待办通知和审计已登记（未连接运行库）'
