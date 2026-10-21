#!/usr/bin/env bash
set -euo pipefail
root="$(CDPATH= cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
migration="$root/database/postgresql/migrations/096_ministry_statistics_package.sql"
controller="$root/ScientificResearch/ScientificResearch/Controllers/EducationStatisticsController.cs"
api="$root/frontend/src/api/education-statistics.ts"
view="$root/frontend/src/views/office/education-statistics.vue"
router="$root/frontend/src/router/index.ts"
legacy="$root/ScientificResearch/ScientificResearch/wwwroot/IMIS/views/科研办公/科研办公/教育部科研统计.html"
for file in "$migration" "$controller" "$api" "$view" "$router" "$legacy"; do
  [[ -f "$file" ]] || { echo "教育部统计契约失败：缺少 $file" >&2; exit 1; }
done
for marker in '教育部统计任务' '教育部统计版本' '教育部统计数据包' '教育部统计字段校验' '教育部统计调整留痕' '教育部统计导出任务' '教育部统计操作审计'; do
  grep -Fq "$marker" "$migration" || { echo "教育部统计契约失败：迁移缺少 $marker" >&2; exit 1; }
done
for marker in '保存统计任务' '保存统计版本' '登记统计数据包' '校验统计数据包' '调整统计字段' '创建统计导出任务'; do
  grep -Fq "$marker" "$controller" || { echo "教育部统计契约失败：控制器缺少 $marker" >&2; exit 1; }
done
grep -Fq "EducationStatistics" "$router" || { echo '教育部统计契约失败：现代路由未注册' >&2; exit 1; }
if grep -n '外部回执\s*=\s*true|模拟成功' "$controller" "$migration" >/dev/null; then
  echo '教育部统计契约失败：不得伪造外部上报或成功回执' >&2; exit 1
fi
echo '教育部统计契约校验通过：任务、版本、数据包校验、调整留痕和导出状态已具备'
