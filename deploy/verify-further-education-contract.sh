#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
migration="$root/database/postgresql/migrations/092_continuing_education_p0.sql"
controller="$root/ScientificResearch/ScientificResearch/Controllers/ContinuingEducationController.cs"
model="$root/ScientificResearch/ScientificResearch/Models/Custom/ContinuingEducation.cs"
fail(){ echo "进修 P0 契约校验失败: $*" >&2; exit 1; }
[[ -f "$migration" ]] || fail "缺少 092 迁移"
[[ -f "$controller" ]] || fail "缺少 FurtherEducationController"
[[ -f "$model" ]] || fail "缺少进修模型"
for marker in 'CREATE TABLE IF NOT EXISTS "进修年度计划"' 'CREATE TABLE IF NOT EXISTS "进修申请"' 'CREATE TABLE IF NOT EXISTS "进修审批流程配置"' 'CREATE TABLE IF NOT EXISTS "进修审批流程节点"' 'CREATE TABLE IF NOT EXISTS "进修审批待办"' 'CREATE TABLE IF NOT EXISTS "进修通知"' 'CREATE TABLE IF NOT EXISTS "进修操作审计"' '预算预占' '预算释放'; do
  grep -Fq "$marker" "$migration" || fail "092 缺少 $marker"
done
for marker in '查询年度计划' '保存年度计划' '提交进修申请请求' '撤回进修申请' '审批进修申请' '查询进修待办' '查询预算台账' '查询审计记录' '查询流程模板' '保存流程配置' '发布流程配置' 'ContinuingEducation/[action]' 'FurtherEducation/[action]'; do
  grep -Fq "$marker" "$controller" || fail "控制器缺少 $marker"
done
for marker in '进修年度计划保存' '进修申请保存' '进修审批请求' '进修流程配置保存'; do
  grep -Fq "$marker" "$model" || fail "模型缺少 $marker"
done
printf '%s\n' '进修 P0 静态契约通过: 年度计划、申请、配置审批、预算预占/释放、待办通知、审计及双路由齐全'
