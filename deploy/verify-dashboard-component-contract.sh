#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
fail(){ echo "首页组件契约失败: $1" >&2; exit 1; }
migration="$root/database/postgresql/migrations/099_dashboard_component_and_chase.sql"
controller="$root/ScientificResearch/ScientificResearch/Controllers/DashboardController.cs"
api="$root/frontend/src/api/dashboard.ts"
view="$root/frontend/src/views/dashboard/index.vue"
for file in "$migration" "$controller" "$api" "$view"; do [[ -f "$file" ]] || fail "缺少 $file"; done
for marker in '首页组件配置' '业务催办' '首页配置审计' '"版本"=99' '209'; do grep -Fq "$marker" "$migration" || fail "099 缺少 $marker"; done
for marker in '获取首页组件配置' '保存首页组件配置' '查询业务催办' '发起业务催办' '处理业务催办'; do grep -Fq "$marker" "$controller" || fail "控制器缺少 $marker"; done
grep -Fq '/Dashboard/获取首页组件配置' "$api" || fail '前端未接入首页组件接口'
grep -Fq 'configuredComponents' "$view" || fail '首页未渲染角色化组件'
echo '首页组件契约静态校验通过: 角色化组件、业务催办和配置审计已登记，统计仍由真实统计接口提供。'
