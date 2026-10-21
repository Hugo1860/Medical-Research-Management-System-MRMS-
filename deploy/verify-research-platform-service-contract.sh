#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
migration="$root/database/postgresql/migrations/093_research_platform_service_catalog.sql"
controller="$root/ScientificResearch/ScientificResearch/Controllers/ResearchPlatformController.cs"
api="$root/frontend/src/api/research-platform.ts"
view="$root/frontend/src/views/office/research-platform.vue"
router="$root/frontend/src/router/index.ts"
for path in "$migration" "$controller" "$api" "$view" "$router"; do
  [[ -f "$path" ]] || { echo "科研平台服务契约失败: 缺少 $path" >&2; exit 1; }
done
for marker in '科研平台' '科研平台成员' '科研服务事项' '科研服务申请' '科研服务反馈' '科研导入导出日志' 'UPDATE "系统迁移版本"' '"版本"=93'; do
  grep -Fq -- "$marker" "$migration" || { echo "科研平台服务契约失败: 093 缺少 $marker" >&2; exit 1; }
done
for marker in '查询科研平台' '保存科研平台' '保存科研平台成员' '查询科研服务事项' '发起科研服务申请' '查询我的科研服务申请' '处理科研服务申请' '评价科研服务' '查询科研导入导出日志'; do
  grep -Fq -- "$marker" "$controller" || { echo "科研平台服务契约失败: 控制器缺少 $marker" >&2; exit 1; }
done
for marker in 'listResearchServices' 'createResearchServiceApplication' 'listMyResearchServiceApplications' 'listPendingResearchServices' 'processResearchServiceApplication' 'rateResearchService'; do
  grep -Fq -- "$marker" "$api" || { echo "科研平台服务契约失败: API 缺少 $marker" >&2; exit 1; }
done
grep -Fq "office/research-platform" "$router" || { echo '科研平台服务契约失败: 前端路由未注册' >&2; exit 1; }
grep -Fq 'ResearchPlatform' "$view" || { echo '科研平台服务契约失败: 前端页面未接入' >&2; exit 1; }
if grep -Fq 'INSERT INTO "科研平台"' "$migration" || grep -Fq 'INSERT INTO "科研服务事项"' "$migration"; then
  echo '科研平台服务契约失败: 迁移不允许写入演示平台或服务数据' >&2
  exit 1
fi
echo '科研平台与服务目录契约静态校验通过: 平台、服务目录、申请、评价、导入导出日志和前端路由均已接入'
