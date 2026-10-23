#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
fail(){ echo "继续医学教育CME学分契约失败: $1" >&2; exit 1; }

migration="$root/database/postgresql/migrations/113_cme_credits.sql"
controller="$root/ScientificResearch/ScientificResearch/Controllers/CmeCreditController.cs"
api="$root/frontend/src/api/cme-credits.ts"
view="$root/frontend/src/views/office/cme-credits.vue"
html="$root/ScientificResearch/ScientificResearch/wwwroot/IMIS/views/科研办公/科研办公/继续医学教育学分.html"

for file in "$migration" "$controller" "$api" "$view" "$html"; do
  [[ -f "$file" ]] || fail "缺少 $file"
done

for marker in '继教学分标准' '讲座学分设置' '院内讲座签到' '继教学分记录' '继教催办记录' '"版本" = 113' '220'; do
  grep -Fq "$marker" "$migration" || fail "113 缺少 $marker"
done

for marker in '获取我的学分' '申报学分' '审核学分' '设置讲座学分' '讲座签到' '讲座授分' '年度达标统计' '批量催办' '导出达标统计CSV'; do
  grep -Fq "$marker" "$controller" || fail "控制器缺少 $marker"
done

grep -Fq '/CmeCredit/获取我的学分' "$api" || fail '前端未接入学分接口'
grep -Fq 'office/cme-credits' "$root/frontend/src/router/index.ts" || fail '路由未注册'

echo '继续医学教育CME学分契约校验通过: I/II类学分申报审核、讲座扫码授分、达标统计与催办已就绪。'
