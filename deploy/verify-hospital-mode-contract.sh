#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
fail(){ echo "医院运营模式契约失败: $1" >&2; exit 1; }

migration="$root/database/postgresql/migrations/111_hospital_tier_mode_and_self_funded_projects.sql"
migration119="$root/database/postgresql/migrations/119_product_tier_config.sql"
controller="$root/ScientificResearch/ScientificResearch/Controllers/HospitalModeController.cs"
api="$root/frontend/src/api/hospital-mode.ts"
store="$root/frontend/src/stores/hospitalMode.ts"
sidebar="$root/frontend/src/layout/components/Sidebar.vue"
configs="$root/frontend/src/views/base-info/system-configs.vue"

for file in "$migration" "$migration119" "$controller" "$api" "$store" "$sidebar" "$configs"; do
  [[ -f "$file" ]] || fail "缺少 $file"
done

for marker in '医院运营配置' '运营模式' '隐藏菜单' '资助类型' '"版本" = 111'; do
  grep -Fq "$marker" "$migration" || fail "111 缺少 $marker"
done

for marker in '获取医院运营配置' '保存医院运营配置'; do
  grep -Fq "$marker" "$controller" || fail "控制器缺少 $marker"
done


for marker in '产品层级' '字段策略' '流程策略' '统计指标策略' '基层核心版' '医院增强版' '综合科研平台版' '119_product_tier_config.sql'; do
  grep -Fq "$marker" "$migration119" || fail "119 缺少 $marker"
done

for marker in '产品层级' '字段策略' '流程策略' '统计指标策略' 'ProductTiers' 'ResolveProductTier'; do
  grep -Fq "$marker" "$controller" || fail "控制器缺少策略契约 $marker"
done

for marker in '产品层级' 'parseProductTier' 'parseFieldStrategy' 'parseWorkflowStrategy' 'parseMetricStrategy'; do
  grep -Fq "$marker" "$api" || fail "API 缺少策略解析器 $marker"
done

for marker in 'productTier' 'fieldStrategy' 'workflowStrategy' 'metricStrategy'; do
  grep -Fq "$marker" "$store" || fail "Store 缺少策略状态 $marker"
done

grep -Fq 'metricPolicy' "$root/frontend/src/views/analytics/statistics.vue" || fail '统计页面未使用统一统计指标策略'

grep -Fq 'useHospitalModeStore' "$sidebar" || fail 'Sidebar 未接入 hospitalModeStore'
grep -Fq '医院运营模式' "$configs" || fail '系统配置未包含运营模式切换'

echo '医院运营模式与三层产品策略契约校验通过: 119 迁移、后端策略读写、前端解析与统一统计指标已就绪。'
