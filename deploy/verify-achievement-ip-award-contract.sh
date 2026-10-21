#!/usr/bin/env bash
set -euo pipefail
root="$(CDPATH= cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
migration="$root/database/postgresql/migrations/097_achievement_ip_award_extensions.sql"
controller="$root/ScientificResearch/ScientificResearch/Controllers/AchievementIpController.cs"
api="$root/frontend/src/api/achievement-ip-awards.ts"
view="$root/frontend/src/views/fruit/achievement-ip-awards.vue"
legacy="$root/ScientificResearch/ScientificResearch/wwwroot/IMIS/views/科研办公/科研办公/成果与知识产权.html"
for file in "$migration" "$controller" "$api" "$view" "$legacy"; do
  [[ -f "$file" ]] || { echo "成果/IP/获奖契约失败：缺少 $file" >&2; exit 1; }
done
for marker in '科研成果扩展记录' '研究报告' '鉴定成果' '艺术作品' '知识产权记录' '知识产权事件' '知识产权缴费提醒' '获奖申报批次' '获奖申报评审' '获奖认定'; do
  grep -q "$marker" "$migration" || { echo "成果/IP/获奖契约失败：迁移缺少 $marker" >&2; exit 1; }
done
for marker in '保存成果扩展记录' '保存知识产权' '记录知识产权事件' '保存获奖申报批次' '指派获奖评审' '认定获奖申报'; do
  grep -q "$marker" "$controller" || { echo "成果/IP/获奖契约失败：控制器缺少 $marker" >&2; exit 1; }
done
grep -q "achievement-ip-awards" "$api" "$view" || { echo '成果/IP/获奖契约失败：前端路由/接口未引用现代页面' >&2; exit 1; }
echo '成果/IP/获奖静态契约通过：成果扩展、知识产权生命周期/缴费提醒、获奖批次/评审/认定已具备（未连接运行库）'
