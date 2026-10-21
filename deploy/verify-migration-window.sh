#!/usr/bin/env bash
set -euo pipefail

# Read-only dynamic gate for the first next-phase migration window. It checks
# the registered schema version and representative objects from 068-073; it
# never applies SQL, inserts data, or changes the database.
database_container="${DATABASE_CONTAINER:-2018-7-24-db-1}"
database_name="${DATABASE_NAME:-ScientificResearch_Model}"
database_user="${DATABASE_USER:-postgres}"
min_version="${MIN_SCHEMA_VERSION:-68}"
max_version="${MAX_SCHEMA_VERSION:-73}"

if ! [[ "$min_version" =~ ^[0-9]+$ && "$max_version" =~ ^[0-9]+$ && "$min_version" -le "$max_version" ]]; then
  echo '迁移窗口参数无效：MIN_SCHEMA_VERSION/MAX_SCHEMA_VERSION 必须为递增整数' >&2
  exit 2
fi

run_psql() {
  timeout --foreground --kill-after=5 "${DOCKER_COMMAND_TIMEOUT_SECONDS:-60}" docker exec -i "$database_container" \
    psql -q -v ON_ERROR_STOP=1 -U "$database_user" -d "$database_name" "$@"
}

version="$(run_psql -Atc 'SELECT COALESCE((SELECT "版本" FROM "系统迁移版本" WHERE "编号"=1),0);' </dev/null | tr -d '[:space:]')"
if ! [[ "$version" =~ ^[0-9]+$ ]]; then
  echo "无法读取运行库迁移版本: $version" >&2
  exit 1
fi
if (( version < max_version )); then
  echo "迁移窗口未完成: 运行库 Schema ${version}，至少需要 ${max_version}（窗口 ${min_version}-${max_version}）" >&2
  exit 1
fi

read -r -d '' flags_sql <<'SQL' || true
SELECT
  EXISTS (SELECT 1 FROM pg_proc WHERE proname='sp_纵向项目申报_增改'),
  to_regclass('public."hrp_sync_project"') IS NOT NULL
    AND to_regclass('public."hrp_sync_budget"') IS NOT NULL
    AND to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)') IS NOT NULL,
  to_regclass('public."科研项目过程预警记录"') IS NOT NULL
    AND to_regclass('public."伦理批件续展与修正案记录"') IS NOT NULL
    AND EXISTS (SELECT 1 FROM pg_proc WHERE proname='sp_科研过程主动巡检与预警生成'),
  to_regclass('public."论文采集结果"') IS NOT NULL
    AND EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='论文采集结果' AND column_name='消歧置信度'),
  to_regclass('public."生物样本资产"') IS NOT NULL
    AND to_regclass('public."生物样本领用借还台账"') IS NOT NULL
    AND EXISTS (SELECT 1 FROM pg_proc WHERE proname='sp_生物样本出库领用申请'),
  to_regclass('public."移动协同消息配置"') IS NOT NULL
    AND to_regclass('public."移动待办推送日志"') IS NOT NULL
    AND to_regclass('public."科研绩效动态公式表"') IS NOT NULL
    AND EXISTS (SELECT 1 FROM pg_proc WHERE proname='sp_发送企业微信待办通知');
SQL

flags="$(printf '%s\n' "$flags_sql" | run_psql -AtF '|')"
IFS='|' read -r application hrp warning paper biobank mobile <<< "$flags"
for flag in "$application" "$hrp" "$warning" "$paper" "$biobank" "$mobile"; do
  [[ "$flag" == 't' ]] || { echo "迁移窗口关键对象缺失: ${flags}" >&2; exit 1; }
done
echo "迁移窗口动态验收通过: Schema ${version} >= ${max_version}，068-073 关键对象齐全（只读）"
