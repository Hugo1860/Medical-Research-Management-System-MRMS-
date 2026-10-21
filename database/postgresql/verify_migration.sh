#!/usr/bin/env bash
set -euo pipefail

# Read-only production check by default. To replay migrations, point
# FRESH_DATABASE_URL at an empty database explicitly; never replay them on the
# live application database.
EXPECTED_SCHEMA_VERSION=${EXPECTED_SCHEMA_VERSION:-106}
SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
DATABASE_CONTAINER=${DATABASE_CONTAINER:-}
DATABASE_NAME=${DATABASE_NAME:-ScientificResearch_Model}
DATABASE_USER=${DATABASE_USER:-postgres}
DOCKER_COMMAND_TIMEOUT_SECONDS=${DOCKER_COMMAND_TIMEOUT_SECONDS:-60}

# Check the repository itself before touching the running database. A current
# version row alone cannot prove that an intermediate script was not deleted or
# skipped, so require a contiguous 001..latest sequence and bind the expected
# database version to the latest filename.
migration_dir="$SCRIPT_DIR/migrations"
latest_migration_version=0
latest_migration_file=''
expected_migration_version=1
migration_count=0
while IFS= read -r migration; do
  filename="${migration##*/}"
  number="${filename%%_*}"
  if [[ ! "$number" =~ ^[0-9]{3}$ ]]; then
    echo "迁移文件编号无效: $filename" >&2
    exit 1
  fi
  numeric_number=$((10#$number))
  if (( numeric_number != expected_migration_version )); then
    echo "仓库迁移序列不连续: 期望 $(printf '%03d' "$expected_migration_version")，实际 $filename" >&2
    exit 1
  fi
  latest_migration_version=$numeric_number
  latest_migration_file=$filename
  expected_migration_version=$((expected_migration_version + 1))
  migration_count=$((migration_count + 1))
done < <(find "$migration_dir" -maxdepth 1 -type f -name '[0-9][0-9][0-9]_*.sql' -print | sed 's#.*/##' | sort -V)
if (( migration_count == 0 )); then
  echo "迁移目录为空: $migration_dir" >&2
  exit 1
fi
if (( latest_migration_version != EXPECTED_SCHEMA_VERSION )); then
  echo "期望 Schema ${EXPECTED_SCHEMA_VERSION} 与仓库最新迁移 ${latest_migration_version} 不一致（${latest_migration_file}）" >&2
  exit 1
fi

if command -v psql >/dev/null 2>&1; then
  : "${DATABASE_URL:?请设置 DATABASE_URL，例如 postgresql://postgres:password@127.0.0.1:5432/ScientificResearch_Model}"
  run_psql() { command psql "$DATABASE_URL" "$@"; }
else
  if [[ -z "$DATABASE_CONTAINER" ]]; then
    echo '请安装 psql，或设置 DATABASE_CONTAINER 使用数据库容器内的 psql' >&2
    exit 1
  fi
  if ! command -v timeout >/dev/null 2>&1; then
    echo '容器内迁移校验需要 timeout 命令，以避免 Docker/psql 无限等待' >&2
    exit 1
  fi
  run_psql() {
    timeout --foreground --kill-after=5 "$DOCKER_COMMAND_TIMEOUT_SECONDS" docker exec -i "$DATABASE_CONTAINER" \
      psql -U "$DATABASE_USER" -d "$DATABASE_NAME" "$@"
  }
  if [[ -n "${FRESH_DATABASE_URL:-}" ]]; then
    echo 'FRESH_DATABASE_URL 需要宿主机 psql；当前仅支持容器内只读校验' >&2
    exit 1
  fi
fi

if [[ -n "${FRESH_DATABASE_URL:-}" ]]; then
  while IFS= read -r migration; do
    psql "$FRESH_DATABASE_URL" -v ON_ERROR_STOP=1 -f "$SCRIPT_DIR/migrations/$migration"
  done < <(find "$SCRIPT_DIR/migrations" -maxdepth 1 -type f -name '[0-9][0-9][0-9]_*.sql' -print \
      | sed 's#.*/##' | sort -V)
fi

run_psql -v ON_ERROR_STOP=1 \
  -v expected_version="$EXPECTED_SCHEMA_VERSION" \
  -v expected_script="$latest_migration_file" <<'SQL'
\set ON_ERROR_STOP on
WITH version_check AS (
    SELECT "版本", "脚本", "检查摘要"
      FROM "系统迁移版本"
     WHERE "编号" = 1
)
SELECT CASE
         WHEN "版本" = :'expected_version'::integer
          AND "脚本" = :'expected_script' THEN 'schema_version_ok'
         ELSE 'schema_version_mismatch'
       END AS check_name,
       "版本", "脚本", "检查摘要"
  FROM version_check;

SELECT COALESCE((
    SELECT "版本" = :'expected_version'::integer
       AND "脚本" = :'expected_script'
      FROM "系统迁移版本"
     WHERE "编号" = 1
), FALSE) AS schema_version_ok;
\gset
\if :schema_version_ok
\else
  \echo '迁移版本不匹配，停止验证'
  SELECT 1 / 0 AS schema_version_mismatch;
\endif

SELECT 'tables' AS object_type, count(*) AS object_count
  FROM pg_class WHERE relkind = 'r' AND relnamespace = 'public'::regnamespace
UNION ALL
SELECT 'views', count(*) FROM pg_views WHERE schemaname = 'public'
UNION ALL
SELECT 'integrity_groups', count(*) FROM "v1_数据完整性异常";

SELECT '人员' AS table_name, count(*) AS row_count FROM "人员"
UNION ALL
SELECT '流程', count(*) FROM "流程"
UNION ALL
SELECT '项目经费预算', count(*) FROM "项目经费预算";

SELECT count(*) = 8 AS legacy_dynamic_routines_retired
  FROM "fn_迁移遗留例程状态"()
 WHERE "状态" = '已退休';
\gset
\if :legacy_dynamic_routines_retired
\else
  \echo '遗留动态过程退休登记不完整，停止验证'
  SELECT 1 / 0 AS legacy_dynamic_routines_not_retired;
\endif

SELECT to_regprocedure('public."sp_权限代理_增"(integer,integer,character varying,timestamp without time zone,timestamp without time zone,character varying)') IS NOT NULL
   AND to_regprocedure('public."sp_预算异常对账_处理"(jsonb)') IS NOT NULL
   AND EXISTS (
       SELECT 1
         FROM pg_trigger
        WHERE tgname = 'trg_权限代理_禁止重叠'
   ) AS governance_validation_functions_ok;
\gset
\if :governance_validation_functions_ok
\else
  \echo '治理校验函数缺失，停止验证'
  SELECT 1 / 0 AS governance_validation_functions_missing;
\endif

SELECT to_regprocedure('public.sp_发送企业微信待办通知(character varying,character varying,character varying,integer,character varying,text,character varying)') IS NOT NULL
   AND to_regprocedure('public.sp_测试移动推送通道(integer,integer)') IS NOT NULL
   AND EXISTS (
       SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = '移动协同消息配置'
          AND column_name IN ('配置状态','最后测试时间','最后测试状态','最后测试提示')
        GROUP BY table_schema, table_name HAVING count(*) = 4
   )
   AND EXISTS (
       SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = '移动待办推送日志'
          AND column_name = '回执来源'
   )
   AND NOT EXISTS (SELECT 1 FROM "移动待办推送日志" WHERE "推送状态" = '模拟成功')
   AND NOT EXISTS (
       SELECT 1 FROM "移动协同消息配置"
        WHERE "通道名称" IN ('北京清华长庚移动协同工作台-待办推送通道', '医院科研管理系统-钉钉移动待办网关')
          AND ("Webhook地址" IS NOT NULL OR "应用ID" IS NOT NULL OR "应用密钥" IS NOT NULL)
   ) AS mobile_push_truthful_gateway_ok;
\gset
\if :mobile_push_truthful_gateway_ok
\else
  \echo '移动推送真实状态校验失败，停止验证'
  SELECT 1 / 0 AS mobile_push_truthful_gateway_missing;
\endif

SELECT to_regclass('public."横向项目生命周期申请"') IS NOT NULL
   AND to_regclass('public."横向项目研究报告"') IS NOT NULL
   AND to_regclass('public."横向项目档案材料"') IS NOT NULL
   AND EXISTS (
       SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = '横向项目档案材料'
          AND column_name IN ('文件路径','文件校验和','版本号','是否必需')
        GROUP BY table_schema, table_name HAVING count(*) = 4
   ) AS horizontal_lifecycle_ok;
\gset
\if :horizontal_lifecycle_ok
\else
  \echo '横向项目生命周期迁移 088 校验失败，停止验证'
  SELECT 1 / 0 AS horizontal_lifecycle_missing;
\endif

SELECT to_regclass('public."伦理审查会议委员"') IS NOT NULL
   AND to_regclass('public."伦理审查会议表决"') IS NOT NULL
   AND to_regclass('public."伦理审查治理审计"') IS NOT NULL
   AND to_regprocedure('public."tfn_伦理委员意见_正式指派校验"()') IS NOT NULL
   AND to_regprocedure('public."tfn_伦理决定_P0门禁"()') IS NOT NULL
   AND to_regprocedure('public."sp_项目过程前置合规检查"(integer,character varying)') IS NOT NULL
   AND to_regprocedure('public."sp_登记伦理批文续展与修正案"(jsonb)') IS NOT NULL AS ethics_p0_governance_ok;
\gset
\if :ethics_p0_governance_ok
\else
  \echo '伦理 P0 治理迁移 089 校验失败，停止验证'
  SELECT 1 / 0 AS ethics_p0_governance_missing;
\endif

SELECT EXISTS (
    SELECT 1 FROM information_schema.columns
     WHERE table_schema='public' AND table_name='伦理电子档案'
       AND column_name IN ('实际校验状态','校验时间','文件大小','内容类型','校验错误')
    GROUP BY table_schema, table_name HAVING count(*) = 5
) AS ethics_p1_evidence_columns_ok;
\gset
\if :ethics_p1_evidence_columns_ok
\else
  \echo '伦理 P1 档案校验字段缺失，停止验证'
  SELECT 1 / 0 AS ethics_p1_evidence_columns_missing;
\endif

SELECT EXISTS (
    SELECT 1 FROM information_schema.columns
     WHERE table_schema='public' AND table_name='伦理审查会议'
       AND column_name='是否匿名表决'
) AS ethics_p2_anonymous_vote_ok;
\gset
\if :ethics_p2_anonymous_vote_ok
\else
  \echo '伦理 P2 匿名表决字段缺失，停止验证'
  SELECT 1 / 0 AS ethics_p2_anonymous_vote_missing;
\endif

SELECT to_regclass('public."进修年度计划"') IS NOT NULL
   AND to_regclass('public."进修申请"') IS NOT NULL
   AND to_regclass('public."进修审批流程配置"') IS NOT NULL
   AND to_regclass('public."进修审批待办"') IS NOT NULL
   AND to_regclass('public."进修通知"') IS NOT NULL
   AND to_regclass('public."进修操作审计"') IS NOT NULL AS continuing_education_p0_ok;
\gset
\if :continuing_education_p0_ok
\else
  \echo '人员进修 P0 迁移 092 校验失败，停止验证'
  SELECT 1 / 0 AS continuing_education_p0_missing;
\endif

SELECT to_regclass('public."科研平台"') IS NOT NULL
   AND to_regclass('public."科研平台成员"') IS NOT NULL
   AND to_regclass('public."科研服务事项"') IS NOT NULL
   AND to_regclass('public."科研服务申请"') IS NOT NULL
   AND to_regclass('public."科研服务反馈"') IS NOT NULL
   AND to_regclass('public."科研导入导出日志"') IS NOT NULL AS research_platform_service_catalog_ok;
\gset
\if :research_platform_service_catalog_ok
\else
  \echo '科研平台与服务目录迁移 093 校验失败，停止验证'
  SELECT 1 / 0 AS research_platform_service_catalog_missing;
\endif

SELECT to_regclass('public."院级项目申报批次"') IS NOT NULL
   AND EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='院级项目申报批次' AND column_name IN ('资格规则','评审方案') GROUP BY table_schema, table_name HAVING count(*)=2)
   AS internal_project_batch_ok;
\gset
\if :internal_project_batch_ok
\else
  \echo '院级项目申报批次迁移 094 校验失败，停止验证'
  SELECT 1 / 0 AS internal_project_batch_missing;
\endif

SELECT to_regclass('public."到账费用提取方案"') IS NOT NULL
   AND to_regclass('public."财务到款登记"') IS NOT NULL
   AND to_regclass('public."项目经费卡"') IS NOT NULL
   AND to_regprocedure('public."sp_财务到款登记"(jsonb)') IS NOT NULL AS finance_incoming_claim_ok;
\gset
\if :finance_incoming_claim_ok
\else
  \echo '经费到账认领迁移 095 校验失败，停止验证'
  SELECT 1 / 0 AS finance_incoming_claim_missing;
\endif

SELECT to_regclass('public."教育部统计任务"') IS NOT NULL
   AND to_regclass('public."教育部统计数据包"') IS NOT NULL
   AND to_regclass('public."教育部统计字段校验"') IS NOT NULL
   AND to_regclass('public."教育部统计导出任务"') IS NOT NULL AS education_ministry_stats_ok;
\gset
\if :education_ministry_stats_ok
\else
  \echo '教育部统计迁移 096 校验失败，停止验证'
  SELECT 1 / 0 AS education_ministry_stats_missing;
\endif

SELECT to_regclass('public."科研成果扩展记录"') IS NOT NULL
   AND to_regclass('public."知识产权记录"') IS NOT NULL
   AND to_regclass('public."知识产权事件"') IS NOT NULL
   AND to_regclass('public."获奖申报批次"') IS NOT NULL
   AND to_regclass('public."获奖申报评审"') IS NOT NULL AS achievement_ip_award_ok;
\gset
\if :achievement_ip_award_ok
\else
  \echo '成果/IP/获奖迁移 097 校验失败，停止验证'
  SELECT 1 / 0 AS achievement_ip_award_missing;
\endif

SELECT to_regclass('public."科研文档"') IS NOT NULL
   AND to_regclass('public."科研文档版本"') IS NOT NULL
   AND to_regclass('public."科研文档授权"') IS NOT NULL
   AND to_regclass('public."科研文档下载日志"') IS NOT NULL
   AND to_regclass('public."消息发送队列"') IS NOT NULL
   AND to_regclass('public."科研服务催办日志"') IS NOT NULL AS document_service_messaging_ok;
\gset
\if :document_service_messaging_ok
\else
  \echo '文档/服务/消息迁移 098 校验失败，停止验证'
  SELECT 1 / 0 AS document_service_messaging_missing;
\endif

SELECT to_regclass('public."首页组件配置"') IS NOT NULL
   AND to_regclass('public."业务催办"') IS NOT NULL
   AND to_regclass('public."首页配置审计"') IS NOT NULL AS dashboard_components_ok;
\gset
\if :dashboard_components_ok
\else
  \echo '首页组件迁移 099 校验失败，停止验证'
  SELECT 1 / 0 AS dashboard_components_missing;
\endif

SELECT to_regclass('public."横向合同版本"') IS NOT NULL
   AND to_regclass('public."横向合作单位资质"') IS NOT NULL
   AND to_regclass('public."横向合同收入认定"') IS NOT NULL
   AND to_regclass('public."横向合同模板"') IS NOT NULL AS horizontal_contract_governance_ok;
\gset
\if :horizontal_contract_governance_ok
\else
  \echo '横向合同治理迁移 100 校验失败，停止验证'
  SELECT 1 / 0 AS horizontal_contract_governance_missing;
\endif

SELECT to_regclass('public."预算科目模板"') IS NOT NULL
   AND to_regclass('public."预算科目模板明细"') IS NOT NULL
   AND to_regclass('public."预算拆分规则"') IS NOT NULL
   AND to_regclass('public."项目编号规则"') IS NOT NULL
   AND to_regprocedure('public."fn_生成项目编号"(character varying)') IS NOT NULL AS project_classification_budget_templates_ok;
\gset
\if :project_classification_budget_templates_ok
\else
  \echo '项目分类与预算模板迁移 101 校验失败，停止验证'
  SELECT 1 / 0 AS project_classification_budget_templates_missing;
\endif

SELECT to_regclass('public."外拨经费"') IS NOT NULL
   AND to_regclass('public."外拨经费拨付记录"') IS NOT NULL
   AND to_regclass('public."经费结转"') IS NOT NULL
   AND to_regclass('public."经费分割方案"') IS NOT NULL
   AND to_regclass('public."经费分割明细"') IS NOT NULL
   AND to_regclass('public."结项结余处理"') IS NOT NULL
   AND to_regclass('public."经费执行审计"') IS NOT NULL AS fund_execution_extensions_ok;
\gset
\if :fund_execution_extensions_ok
\else
  \echo '经费执行扩展迁移 102 校验失败，停止验证'
  SELECT 1 / 0 AS fund_execution_extensions_missing;
\endif

SELECT to_regclass('public."平台仪器设备"') IS NOT NULL
   AND to_regclass('public."仪器使用资质"') IS NOT NULL
   AND to_regclass('public."仪器预约"') IS NOT NULL
   AND to_regclass('public."仪器使用记录"') IS NOT NULL
   AND to_regclass('public."平台年报"') IS NOT NULL AS platform_instrument_booking_ok;
\gset
\if :platform_instrument_booking_ok
\else
  \echo '平台仪器设备与预约迁移 103 校验失败，停止验证'
  SELECT 1 / 0 AS platform_instrument_booking_missing;
\endif

SELECT to_regclass('public."主办会议申请"') IS NOT NULL
   AND to_regclass('public."学术交流人员"') IS NOT NULL
   AND to_regclass('public."期刊分区库"') IS NOT NULL
   AND to_regclass('public."期刊分区导入批次"') IS NOT NULL
   AND to_regclass('public."论文期刊分区匹配"') IS NOT NULL
   AND to_regprocedure('public."fn_匹配期刊分区"(character varying,character varying,integer)') IS NOT NULL AS academic_exchange_journal_partition_ok;
\gset
\if :academic_exchange_journal_partition_ok
\else
  \echo '学术交流与期刊分区迁移 104 校验失败，停止验证'
  SELECT 1 / 0 AS academic_exchange_journal_partition_missing;
\endif

SELECT to_regclass('public."纵向项目导入批次"') IS NOT NULL
   AND to_regclass('public."纵向项目导入明细"') IS NOT NULL AS vertical_project_import_ok;
\gset
\if :vertical_project_import_ok
\else
  \echo '纵向项目导入迁移 105 校验失败，停止验证'
  SELECT 1 / 0 AS vertical_project_import_missing;
\endif
SQL
