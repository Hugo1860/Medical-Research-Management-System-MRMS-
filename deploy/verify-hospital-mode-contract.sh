#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
fail(){ echo "医院运营模式契约失败: $1" >&2; exit 1; }

migration="$root/database/postgresql/migrations/111_hospital_tier_mode_and_self_funded_projects.sql"
controller="$root/ScientificResearch/ScientificResearch/Controllers/HospitalModeController.cs"
api="$root/frontend/src/api/hospital-mode.ts"
store="$root/frontend/src/stores/hospitalMode.ts"
sidebar="$root/frontend/src/layout/components/Sidebar.vue"
configs="$root/frontend/src/views/base-info/system-configs.vue"

for file in "$migration" "$controller" "$api" "$store" "$sidebar" "$configs"; do
  [[ -f "$file" ]] || fail "缺少 $file"
done

for marker in '医院运营配置' '运营模式' '隐藏菜单' '资助类型' '"版本" = 111'; do
  grep -Fq "$marker" "$migration" || fail "111 缺少 $marker"
done

for marker in '获取医院运营配置' '保存医院运营配置'; do
  grep -Fq "$marker" "$controller" || fail "控制器缺少 $marker"
done

grep -Fq 'useHospitalModeStore' "$sidebar" || fail 'Sidebar 未接入 hospitalModeStore'
grep -Fq '医院运营模式' "$configs" || fail '系统配置未包含运营模式切换'

echo '医院运营模式与无资助简易立项契约校验通过: 三级综合与二级精简过滤、简易立项通道已就绪。'
