#!/usr/bin/env bash
set -euo pipefail
: "${UAT_DATABASE_CONTAINER:?Provide the explicitly created isolated UAT PostgreSQL container}"
: "${UAT_APPROVAL_NUMBER:?Provide fixture approval number}"
: "${UAT_EXPECTED_OWNER_ID:?Provide the numeric isolated UAT owner ID}"
[[ "$UAT_APPROVAL_NUMBER" == 'UAT-PDF-26a8e6cfd677' ]] || { echo 'Refused: approval number does not match this fixture' >&2; exit 2; }
[[ "$UAT_EXPECTED_OWNER_ID" =~ ^[1-9][0-9]*$ ]] || { echo 'Refused: invalid owner ID' >&2; exit 2; }
label="$(docker inspect --format '{{index .Config.Labels "scientificresearch.isolated-uat"}}' "$UAT_DATABASE_CONTAINER")"
[[ "$label" == true ]] || { echo 'Refused: database container lacks scientificresearch.isolated-uat=true label' >&2; exit 2; }
root="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
sha="$(shasum -a 256 "$root/proposal.pdf" | awk '{print $1}')"
result="$(docker exec -i "$UAT_DATABASE_CONTAINER" psql -X -q -A -t -v ON_ERROR_STOP=1 \
  -U "${UAT_DATABASE_USER:-postgres}" -d "${UAT_DATABASE_NAME:-ScientificResearch_Model}" \
  -v approval="$UAT_APPROVAL_NUMBER" -v owner="$UAT_EXPECTED_OWNER_ID" -v sha="$sha" <<'SQL'
BEGIN TRANSACTION READ ONLY;
SET LOCAL statement_timeout='10s';
WITH b AS (
  SELECT * FROM "纵向项目导入批次" WHERE "批次名称"='UAT-BATCH-26a8e6cfd677'
), d AS (
  SELECT d.* FROM "纵向项目导入明细" d JOIN b ON d."批次编号"=b."编号"
), p AS (
  SELECT * FROM "纵向项目立项" WHERE "项目编号"=:'approval'
), checks AS (
 SELECT
   (SELECT count(*)=1 FROM b) AS batch_unique,
   (SELECT count(*)=1 FROM d) AS detail_unique,
   (SELECT count(*)=1 FROM p) AS project_unique,
   EXISTS(SELECT 1 FROM b WHERE "状态"='已导入' AND "总文件数"=1 AND "导入成功数"=1) AS batch_complete,
   EXISTS(SELECT 1 FROM d WHERE "校验状态"='通过' AND "处理状态"='已导入'
      AND "校验结果"->>'人工复核'='已确认' AND "文件哈希"=:'sha'
      AND "解析字段"->>'批准号'=:'approval') AS reviewed_file,
   EXISTS(SELECT 1 FROM p JOIN d ON d."导入纵向项目编号"=p."编号"
      WHERE p."项目中文名称"='UAT Synthetic Research Study 26a8e6cfd677'
        AND p."负责人编号"=:'owner'::integer AND p."分类"=1 AND p."立项年度"=2026
        AND p."批准经费"=123450 AND p."批准日期"=DATE '2026-01-02'
        AND p."计划开始日期"=DATE '2026-02-01' AND p."计划完成日期"=DATE '2028-01-31'
        AND p."学科门类"='Medicine' AND p."学科类型"='Clinical' AND p."研究类型"='Applied'
        AND p."项目类型"='Research' AND p."项目级别"='Institutional' AND p."项目来源单位"='Synthetic UAT') AS linked_project_exact
)
SELECT json_build_object('batch_unique',batch_unique,'detail_unique',detail_unique,
 'project_unique',project_unique,'batch_complete',batch_complete,'reviewed_file',reviewed_file,
 'linked_project_exact',linked_project_exact,
 'all_passed',batch_unique AND detail_unique AND project_unique AND batch_complete AND reviewed_file AND linked_project_exact)
FROM checks;
COMMIT;
SQL
)"
printf '%s\n' "$result" | python3 -c 'import json,sys; value=json.load(sys.stdin); print(json.dumps(value,ensure_ascii=False)); sys.exit(0 if value.get("all_passed") is True else 1)'
