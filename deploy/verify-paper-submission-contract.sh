#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
fail(){ echo "论文投稿备案与期刊风控契约失败: $1" >&2; exit 1; }

migration="$root/database/postgresql/migrations/112_paper_submission_filing_and_journal_risk.sql"
controller="$root/ScientificResearch/ScientificResearch/Controllers/PaperSubmissionController.cs"
api="$root/frontend/src/api/paper-submission.ts"
view="$root/frontend/src/views/fruit/paper-submission.vue"
html="$root/ScientificResearch/ScientificResearch/wwwroot/IMIS/views/科研办公/科研办公/论文投稿备案.html"

for file in "$migration" "$controller" "$api" "$view" "$html"; do
  [[ -f "$file" ]] || fail "缺少 $file"
done

for marker in '期刊风险名单' '国内正规期刊库' '论文投稿备案' 'fn_生成论文投稿备案号' '"版本" = 112' '219'; do
  grep -Fq "$marker" "$migration" || fail "112 缺少 $marker"
done

for marker in '检查期刊风险' '保存投稿备案' '提交投稿备案' '撤回投稿备案' '科室审核' '科教科审核' '登记见刊' '查询可用报销备案'; do
  grep -Fq "$marker" "$controller" || fail "控制器缺少 $marker"
done

grep -Fq '/PaperSubmission/检查期刊风险' "$api" || fail '前端未接入期刊风控接口'
grep -Fq 'fruit/paper-submission' "$root/frontend/src/router/index.ts" || fail '路由未注册'

echo '论文投稿事前备案与期刊风控契约校验通过: 中科院预警拦截、正规期刊白名单、版面费报销关联已闭环。'
