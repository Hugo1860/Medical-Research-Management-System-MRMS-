#!/usr/bin/env bash
set -euo pipefail

# 只读迁移差异报告：按 source-sqlserver/columns.tsv 与 data/001.json...
# 的原始顺序逐表比较源记录数和 PostgreSQL 记录数，并检查关键业务绑定。
: "${DATABASE_CONTAINER:=2018-7-24-db-1}"
: "${DATABASE_NAME:=ScientificResearch_Model}"
: "${DATABASE_USER:=postgres}"
: "${DOCKER_COMMAND_TIMEOUT_SECONDS:=60}"
: "${STRICT_MIGRATION_DIFF:=false}"
: "${STRICT_MIGRATION_DIFF_ALL:=false}"
report_file="${REPORT_FILE:-reports/migration-diff-$(date +%Y%m%d-%H%M%S).tsv}"
root_dir="$(cd "$(dirname "$0")/.." && pwd)"
mkdir -p "$(dirname "$root_dir/$report_file")"
summary_file="${REPORT_SUMMARY_FILE:-${report_file%.tsv}.summary.json}"
mkdir -p "$(dirname "$root_dir/$summary_file")"

source_dir="$root_dir/database/postgresql/source-sqlserver"
if ! command -v timeout >/dev/null 2>&1; then
    echo '迁移差异校验需要 timeout 命令，以避免 Docker/psql 无限等待' >&2
    exit 1
fi
run_psql() {
    timeout --foreground --kill-after=5 "$DOCKER_COMMAND_TIMEOUT_SECONDS" docker exec -i "$DATABASE_CONTAINER" \
        psql -q -v ON_ERROR_STOP=1 -U "$DATABASE_USER" -d "$DATABASE_NAME" "$@"
}
runtime_schema_version="$(run_psql -Atc 'SELECT COALESCE((SELECT "版本" FROM "系统迁移版本" WHERE "编号"=1),0);' | tr -d '[:space:]')"
if [[ ! "$runtime_schema_version" =~ ^[0-9]+$ ]]; then
    echo "无法读取运行库迁移版本: $runtime_schema_version" >&2
    exit 1
fi
tmp_expected="$(mktemp)"
tmp_sql="$(mktemp)"
tmp_result="$(mktemp)"
tmp_project_sql="$(mktemp)"
trap 'rm -f "$tmp_expected" "$tmp_sql" "$tmp_result" "$tmp_project_sql"' EXIT

python3 - "$source_dir" > "$tmp_expected" <<'PY'
import json, sys
from pathlib import Path
root = Path(sys.argv[1])
names = []
for line in (root / 'columns.tsv').read_text(encoding='utf-8').splitlines():
    parts = line.split('\t')
    if not parts or not parts[0] or parts[0].startswith('(') or parts[0] in names:
        continue
    names.append(parts[0])
for index, name in enumerate(names, 1):
    path = root / 'data' / f'{index:03d}.json'
    if not path.exists():
        continue
    raw = path.read_bytes()
    text = raw.decode('utf-16') if raw.startswith((b'\xff\xfe', b'\xfe\xff')) else raw.decode('utf-8')
    rows = json.loads(text) if text.strip() else []
    print(f'{index:03d}\t{name}\t{len(rows)}')
PY

python3 - "$tmp_expected" > "$tmp_sql" <<'PY'
import sys
rows=[]
print("SET default_transaction_read_only = on;")
for line in open(sys.argv[1], encoding='utf-8'):
    index, name, count = line.rstrip('\n').split('\t')
    ident = '"' + name.replace('"', '""') + '"'
    label = name.replace("'", "''")
    rows.append(f"SELECT '{index}' AS source_file, '{label}' AS table_name, {int(count)}::bigint AS source_count, count(*)::bigint AS postgres_count FROM {ident}")
print('\nUNION ALL\n'.join(rows) + '\nORDER BY source_file;')
PY

run_psql -AtF $'\t' < "$tmp_sql" > "$tmp_result"

# 逐项目核对迁移后的主键和预算批准金额。总行数一致不能证明项目明细没有错位。
python3 - "$source_dir" > "$tmp_project_sql" <<'PY'
import json, sys
from decimal import Decimal
from pathlib import Path
root = Path(sys.argv[1])
print("SET default_transaction_read_only = on;")
def load(index):
    p = root / 'data' / f'{index:03d}.json'
    if not p.exists(): return []
    raw = p.read_bytes()
    text = raw.decode('utf-16') if raw.startswith((b'\xff\xfe', b'\xfe\xff')) else raw.decode('utf-8')
    return json.loads(text) if text.strip() else []
def ident(value):
    return '"' + str(value).replace('"','""') + '"'
checks = [
    ('056','纵向项目立项','纵向项目立项'), ('021','横向项目','横向项目'),
    ('048','项目经费预算','项目经费预算'), ('047','项目经费到账','项目经费到账'),
    ('001','报销','报销'), ('044','项目参与人','项目参与人'),
    ('057','纵向项目申报','纵向项目申报'), ('058','纵向项目中检','纵向项目中检')
]
for source_file, label, table in checks:
    ids = sorted({int(row['编号']) for row in load(int(source_file)) if str(row.get('编号','')).isdigit()})
    if not ids: continue
    values = ','.join(str(x) for x in ids)
    safe = label.replace("'", "''")
    print(f"SELECT 'project' AS source_file, '{safe}-编号核对' AS table_name, {len(ids)}::bigint AS source_count, count(*)::bigint AS postgres_count, (count(*)-{len(ids)})::bigint AS delta, CASE WHEN count(*)={len(ids)} THEN '一致' ELSE '差异' END AS status, '逐项目主键核对，人工复核异常' AS review_action FROM {ident(table)} WHERE \"编号\" IN ({values});")
budget = load(48)
groups = {}
for row in budget:
    try: key = (int(row['流程模板编号']), int(row['项目编号']))
    except (KeyError, TypeError, ValueError): continue
    groups[key] = groups.get(key, Decimal('0')) + Decimal(str(row.get('批准经费') or 0))
if groups:
    values = ','.join(f"({flow},{project},{amount})" for (flow, project), amount in sorted(groups.items()))
    print("WITH source_budget(\"流程模板编号\",\"项目编号\",\"源批准经费\") AS (VALUES " + values + ")")
    print("SELECT 'project-budget' AS source_file, '预算批准经费逐项目核对' AS table_name, count(*)::bigint AS source_count,")
    print("       count(*) FILTER (WHERE coalesce(db.\"批准经费\",0)=source_budget.\"源批准经费\")::bigint AS postgres_count,")
    print("       (count(*) FILTER (WHERE coalesce(db.\"批准经费\",0)=source_budget.\"源批准经费\")-count(*))::bigint AS delta,")
    print("       CASE WHEN count(*) FILTER (WHERE coalesce(db.\"批准经费\",0)=source_budget.\"源批准经费\")=count(*) THEN '一致' ELSE '差异' END AS status,")
    print("       '逐项目预算金额核对，人工复核异常' AS review_action")
    print("  FROM source_budget LEFT JOIN (SELECT \"流程模板编号\",\"项目编号\",sum(\"批准经费\") AS \"批准经费\" FROM \"项目经费预算\" GROUP BY 1,2) db USING (\"流程模板编号\",\"项目编号\");")
PY

tmp_raw_report="$(mktemp)"
trap 'rm -f "$tmp_expected" "$tmp_sql" "$tmp_result" "$tmp_project_sql" "$tmp_raw_report"' EXIT

{
    printf 'source_file\ttable_name\tsource_count\tpostgres_count\tdelta\tstatus\treview_action\n'
    awk -F '\t' 'BEGIN { OFS="\t" }
      {
        delta=$4-$3; status=(delta==0 ? "一致" : "差异"); action="无需处理"
        if ($2=="角色权限") action="治理扩展：人工确认新增菜单权限"
        else if ($2=="权限") action="治理扩展：人工确认新增权限菜单"
        else if ($2=="人员角色") action="治理扩展：人工确认新增人员角色授权"
        print $1,$2,$3,$4,delta,status,action
      }' "$tmp_result"
    run_psql -AtF $'\t' <<'SQL'
SET default_transaction_read_only = on;
SELECT 'binding' AS source_file, '预算无对应流程' AS table_name, 0::bigint AS source_count,
       count(*)::bigint AS postgres_count,
       '关键绑定检查' AS delta,
       CASE WHEN count(*)=0 THEN '一致' ELSE '差异' END AS status,
       CASE WHEN count(*)=0 THEN '无需处理' ELSE '业务数据人工核查，不自动删除' END AS review_action
  FROM "项目经费预算" b
 WHERE NOT (
       (b."流程模板编号"=410 AND EXISTS (SELECT 1 FROM "纵向项目立项" p WHERE p."编号"=b."项目编号"))
    OR (b."流程模板编号"=450 AND EXISTS (SELECT 1 FROM "横向项目" p WHERE p."编号"=b."项目编号"))
    OR (b."流程模板编号"=210 AND EXISTS (SELECT 1 FROM "参加会议" p WHERE p."编号"=b."项目编号"))
    OR EXISTS (SELECT 1 FROM "流程" f WHERE f."流程模板编号"=b."流程模板编号" AND f."项目编号"=b."项目编号"));
SELECT 'binding', '到账无对应流程', 0::bigint, count(*)::bigint, '关键绑定检查', CASE WHEN count(*)=0 THEN '一致' ELSE '差异' END,
       CASE WHEN count(*)=0 THEN '无需处理' ELSE '业务数据人工核查，不自动删除' END
  FROM "项目经费到账" a
 WHERE NOT (
       (a."流程模板编号"=410 AND EXISTS (SELECT 1 FROM "纵向项目立项" p WHERE p."编号"=a."项目编号"))
    OR (a."流程模板编号"=450 AND EXISTS (SELECT 1 FROM "横向项目" p WHERE p."编号"=a."项目编号"))
    OR (a."流程模板编号"=210 AND EXISTS (SELECT 1 FROM "参加会议" p WHERE p."编号"=a."项目编号"))
    OR EXISTS (SELECT 1 FROM "流程" f WHERE f."流程模板编号"=a."流程模板编号" AND f."项目编号"=a."项目编号"));
SELECT 'binding', '报销无对应流程', 0::bigint, count(*)::bigint, '关键绑定检查', CASE WHEN count(*)=0 THEN '一致' ELSE '差异' END,
       CASE WHEN count(*)=0 THEN '无需处理' ELSE '业务数据人工核查，不自动删除' END
  FROM "报销" r
 WHERE NOT (
       (r."流程模板编号"=410 AND EXISTS (SELECT 1 FROM "纵向项目立项" p WHERE p."编号"=r."项目编号"))
    OR (r."流程模板编号"=450 AND EXISTS (SELECT 1 FROM "横向项目" p WHERE p."编号"=r."项目编号"))
    OR (r."流程模板编号"=210 AND EXISTS (SELECT 1 FROM "参加会议" p WHERE p."编号"=r."项目编号"))
    OR EXISTS (SELECT 1 FROM "流程" f WHERE f."流程模板编号"=r."流程模板编号" AND f."项目编号"=r."项目编号"));
SELECT 'binding', '参与人无对应流程', 0::bigint, count(*)::bigint, '关键绑定检查', CASE WHEN count(*)=0 THEN '一致' ELSE '差异' END,
       CASE WHEN count(*)=0 THEN '无需处理' ELSE '业务数据人工核查，不自动删除' END
  FROM "项目参与人" p
 WHERE NOT (
       (p."流程模板编号"=400 AND EXISTS (SELECT 1 FROM "纵向项目申报" x WHERE x."编号"=p."项目编号"))
    OR (p."流程模板编号"=410 AND EXISTS (SELECT 1 FROM "纵向项目立项" x WHERE x."编号"=p."项目编号"))
    OR (p."流程模板编号"=450 AND EXISTS (SELECT 1 FROM "横向项目" x WHERE x."编号"=p."项目编号"))
    OR EXISTS (SELECT 1 FROM "流程" f WHERE f."流程模板编号"=p."流程模板编号" AND f."项目编号"=p."项目编号"));
SELECT 'project-binding', '预算调整无对应项目' AS table_name, 0::bigint AS source_count,
       count(*)::bigint AS postgres_count, '项目关联检查' AS delta,
       CASE WHEN count(*)=0 THEN '一致' ELSE '差异' END AS status,
       CASE WHEN count(*)=0 THEN '无需处理' ELSE '业务数据人工核查，不自动删除' END AS review_action
  FROM "项目经费预算调整申请" a
 WHERE NOT (
       (a."流程模板编号"=410 AND EXISTS (SELECT 1 FROM "纵向项目立项" p WHERE p."编号"=a."项目编号"))
    OR (a."流程模板编号"=450 AND EXISTS (SELECT 1 FROM "横向项目" p WHERE p."编号"=a."项目编号"))
    OR (a."流程模板编号"=210 AND EXISTS (SELECT 1 FROM "参加会议" p WHERE p."编号"=a."项目编号"))
    OR EXISTS (SELECT 1 FROM "流程" f WHERE f."流程模板编号"=a."流程模板编号" AND f."项目编号"=a."项目编号"));
SELECT 'project-binding', '年度结算无对应项目' AS table_name, 0::bigint AS source_count,
       count(*)::bigint AS postgres_count, '项目关联检查' AS delta,
       CASE WHEN count(*)=0 THEN '一致' ELSE '差异' END AS status,
       CASE WHEN count(*)=0 THEN '无需处理' ELSE '业务数据人工核查，不自动删除' END AS review_action
  FROM "项目经费年度结算" a
 WHERE NOT (
       (a."流程模板编号"=410 AND EXISTS (SELECT 1 FROM "纵向项目立项" p WHERE p."编号"=a."项目编号"))
    OR (a."流程模板编号"=450 AND EXISTS (SELECT 1 FROM "横向项目" p WHERE p."编号"=a."项目编号"))
    OR (a."流程模板编号"=210 AND EXISTS (SELECT 1 FROM "参加会议" p WHERE p."编号"=a."项目编号"))
    OR EXISTS (SELECT 1 FROM "流程" f WHERE f."流程模板编号"=a."流程模板编号" AND f."项目编号"=a."项目编号"));
SELECT 'project-binding', '任务书无对应纵向项目' AS table_name, 0::bigint AS source_count,
       count(*)::bigint AS postgres_count, '项目关联检查' AS delta,
       CASE WHEN count(*)=0 THEN '一致' ELSE '差异' END AS status,
       CASE WHEN count(*)=0 THEN '无需处理' ELSE '业务数据人工核查，不自动删除' END AS review_action
  FROM "纵向项目任务书" t
 WHERE NOT EXISTS (SELECT 1 FROM "纵向项目立项" p WHERE p."编号"=t."纵向项目立项编号");
SELECT 'project-binding', '电子档案无对应纵向项目' AS table_name, 0::bigint AS source_count,
       count(*)::bigint AS postgres_count, '项目关联检查' AS delta,
       CASE WHEN count(*)=0 THEN '一致' ELSE '差异' END AS status,
       CASE WHEN count(*)=0 THEN '无需处理' ELSE '业务数据人工核查，不自动删除' END AS review_action
  FROM "纵向项目电子档案清单" a
 WHERE NOT EXISTS (SELECT 1 FROM "纵向项目立项" p WHERE p."编号"=a."纵向项目立项编号");
SELECT 'runtime-policy', '移动通道示例凭据或模拟成功' AS table_name, 0::bigint AS source_count,
       count(*)::bigint AS postgres_count, '真实通道状态检查' AS delta,
       CASE WHEN count(*)=0 THEN '一致' ELSE '差异' END AS status,
       CASE WHEN count(*)=0 THEN '无需处理' ELSE '移动通道不得使用示例凭据或模拟成功' END AS review_action
  FROM (
       SELECT 1 FROM "移动协同消息配置"
        WHERE "通道名称" IN ('北京清华长庚移动协同工作台-待办推送通道','医院科研管理系统-钉钉移动待办网关')
          AND ("Webhook地址" IS NOT NULL OR "应用ID" IS NOT NULL OR "应用密钥" IS NOT NULL)
           OR "Webhook地址" IN ('https://qyapi.weixin.qq.com/cgi-bin/message/send','https://oapi.dingtalk.com/topapi/message/corpconversation/asyncsend_v2')
           OR "应用ID" IN ('wx_corp_thcg_2026','ding_app_ky_2026')
       UNION ALL
       SELECT 1 FROM "移动待办推送日志" WHERE "推送状态"='模拟成功'
  ) policy;
SELECT 'governance-policy', '成果治理与专家邀请审计结构' AS table_name, 0::bigint AS source_count,
       CASE WHEN to_regclass('public."科研成果治理历史"') IS NOT NULL
                  AND to_regclass('public."科研成果合并审计"') IS NOT NULL
                  AND to_regclass('public."专家可用时间"') IS NOT NULL
                  AND to_regclass('public."外部专家邀请"') IS NOT NULL
                  AND to_regclass('public."科研成果状态凭证"') IS NOT NULL
                  AND to_regclass('public."科研成果状态凭证核验"') IS NOT NULL
                  AND to_regclass('public."科研评价奖励支付批次"') IS NOT NULL
                  AND to_regclass('public."科研评价奖励支付流水"') IS NOT NULL
                  AND to_regclass('public."横向项目生命周期申请"') IS NOT NULL
                  AND to_regclass('public."横向项目研究报告"') IS NOT NULL
                  AND to_regclass('public."横向项目档案材料"') IS NOT NULL
                  AND to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)') IS NOT NULL
                  AND COALESCE(pg_get_functiondef(to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)')), '') ILIKE '%push_only%'
                  AND COALESCE(pg_get_functiondef(to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)')), '') NOT ILIKE '%hrp_sync_incoming%'
                  AND COALESCE(pg_get_functiondef(to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)')), '') NOT ILIKE '%hrp_sync_expense%'
                  AND COALESCE(pg_get_functiondef(to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)')), '') NOT ILIKE '%hrp-voucher%'
                  AND COALESCE(pg_get_functiondef(to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)')), '') NOT ILIKE '%push_and_replay%'
                  AND COALESCE(pg_get_functiondef(to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)')), '') NOT ILIKE '%replay%'
            THEN 0 ELSE 1 END::bigint AS postgres_count,
       '083–088 结构与 HRP/成果/奖励/横向生命周期安全函数检查' AS delta,
       CASE WHEN to_regclass('public."科研成果治理历史"') IS NOT NULL
                  AND to_regclass('public."科研成果合并审计"') IS NOT NULL
                  AND to_regclass('public."专家可用时间"') IS NOT NULL
                  AND to_regclass('public."外部专家邀请"') IS NOT NULL
                  AND to_regclass('public."科研成果状态凭证"') IS NOT NULL
                  AND to_regclass('public."科研成果状态凭证核验"') IS NOT NULL
                  AND to_regclass('public."科研评价奖励支付批次"') IS NOT NULL
                  AND to_regclass('public."科研评价奖励支付流水"') IS NOT NULL
                  AND to_regclass('public."横向项目生命周期申请"') IS NOT NULL
                  AND to_regclass('public."横向项目研究报告"') IS NOT NULL
                  AND to_regclass('public."横向项目档案材料"') IS NOT NULL
                  AND to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)') IS NOT NULL
                  AND COALESCE(pg_get_functiondef(to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)')), '') ILIKE '%push_only%'
                  AND COALESCE(pg_get_functiondef(to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)')), '') NOT ILIKE '%hrp_sync_incoming%'
                  AND COALESCE(pg_get_functiondef(to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)')), '') NOT ILIKE '%hrp_sync_expense%'
                  AND COALESCE(pg_get_functiondef(to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)')), '') NOT ILIKE '%hrp-voucher%'
                  AND COALESCE(pg_get_functiondef(to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)')), '') NOT ILIKE '%push_and_replay%'
                  AND COALESCE(pg_get_functiondef(to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)')), '') NOT ILIKE '%replay%'
            THEN '一致' ELSE '差异' END AS status,
       CASE WHEN to_regclass('public."科研成果治理历史"') IS NOT NULL
                  AND to_regclass('public."科研成果合并审计"') IS NOT NULL
                  AND to_regclass('public."专家可用时间"') IS NOT NULL
                  AND to_regclass('public."外部专家邀请"') IS NOT NULL
                  AND to_regclass('public."科研成果状态凭证"') IS NOT NULL
                  AND to_regclass('public."科研成果状态凭证核验"') IS NOT NULL
                  AND to_regclass('public."科研评价奖励支付批次"') IS NOT NULL
                  AND to_regclass('public."科研评价奖励支付流水"') IS NOT NULL
                  AND to_regclass('public."横向项目生命周期申请"') IS NOT NULL
                  AND to_regclass('public."横向项目研究报告"') IS NOT NULL
                  AND to_regclass('public."横向项目档案材料"') IS NOT NULL
                  AND to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)') IS NOT NULL
                  AND COALESCE(pg_get_functiondef(to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)')), '') ILIKE '%push_only%'
                  AND COALESCE(pg_get_functiondef(to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)')), '') NOT ILIKE '%hrp_sync_incoming%'
                  AND COALESCE(pg_get_functiondef(to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)')), '') NOT ILIKE '%hrp_sync_expense%'
                  AND COALESCE(pg_get_functiondef(to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)')), '') NOT ILIKE '%hrp-voucher%'
                  AND COALESCE(pg_get_functiondef(to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)')), '') NOT ILIKE '%push_and_replay%'
                  AND COALESCE(pg_get_functiondef(to_regprocedure('public."sp_hrp中间库双向协同"(integer,character varying)')), '') NOT ILIKE '%replay%'
            THEN '无需处理' ELSE '应用 083–088 迁移后再验收成果治理、专家邀请、HRP 安全协同、状态凭证、奖励支付和横向生命周期结构' END AS review_action;
SQL
    run_psql -AtF $'\t' < "$tmp_project_sql"
} > "$tmp_raw_report"

# Count differences are not all migration failures.  A running system may
# append login/audit rows, while migrations intentionally add governance
# permissions.  Classify each row so release gates can block only source
# shortfalls, amount/key mismatches, and orphan bindings.  No row is deleted
# or modified by this report.
python3 - "$tmp_raw_report" "$root_dir/$report_file" "$root_dir/$summary_file" "$runtime_schema_version" <<'PY'
import json
import sys
from collections import Counter
from datetime import datetime, timezone

raw_path, report_path, summary_path, runtime_schema_version = sys.argv[1:]
governance = {"角色权限", "权限", "人员角色", "步骤", "步骤角色"}
runtime = {"登录日志"}

def classify(source_file, table_name, source_count, postgres_count, status):
    if status == "一致":
        return "一致", "无需处理"
    if source_file == "runtime-policy":
        return "运行策略违规", "阻断发布并清理示例凭据或模拟成功状态；不发送外部请求"
    if source_file == "governance-policy":
        return "治理结构缺失", "阻断发布并应用 083–088 迁移；不写入或删除历史成果或支付记录"
    if source_file == "binding":
        if "孤立" in table_name or "无对应" in table_name:
            return "流程绑定孤立", "业务负责人确认绑定后再修复；不自动删除"
        return "流程绑定差异", "人工核查流程模板、项目编号和历史业务范围"
    if source_file == "project-binding":
        return "业务绑定孤立", "业务负责人确认项目、任务书或预算关联后再修复；不自动删除"
    if source_file == "project":
        return "主键核对差异", "阻断发布并逐项目核查主键；不自动改写"
    if source_file == "project-budget":
        return "预算金额差异", "阻断发布并逐项目核查批准金额；不自动改写"
    if source_count > postgres_count:
        return "源数据缺失", "阻断发布并核对源库导出、编码和导入日志"
    if table_name in runtime:
        return "运行时追加", "确认新增记录来自本次运行的审计或登录活动"
    if table_name in governance:
        return "治理扩展", "人工确认新增菜单、权限或流程步骤授权"
    return "目标库追加", "人工确认新增记录来源和导入批次"

with open(raw_path, encoding="utf-8") as src:
    lines = src.read().splitlines()
if not lines:
    raise SystemExit("迁移差异报告为空")
header = lines[0].split("\t")
if header != ["source_file", "table_name", "source_count", "postgres_count", "delta", "status", "review_action"]:
    raise SystemExit("迁移差异报告列结构异常")
out = ["\t".join(header[:6] + ["difference_class", "review_action"])]
class_counts = Counter()
all_differences = 0
blocking = 0
rows = []
for line in lines[1:]:
    if not line.strip():
        continue
    # psql versions that ignore -q may still echo a command status for the
    # read-only session setup; it is not a data row.
    if line.strip() in {"SET", "BEGIN", "COMMIT"}:
        continue
    fields = line.split("\t")
    if len(fields) != 7:
        raise SystemExit(f"迁移差异报告行列数异常: {line}")
    source_file, table_name, source_count, postgres_count, delta, status, action = fields
    try:
        src_count = int(source_count)
        pg_count = int(postgres_count)
    except ValueError:
        src_count = pg_count = 0
    category, classified_action = classify(source_file, table_name, src_count, pg_count, status)
    if status == "差异":
        all_differences += 1
        class_counts[category] += 1
        if category in {"源数据缺失", "主键核对差异", "预算金额差异", "流程绑定孤立", "流程绑定差异", "业务绑定孤立", "运行策略违规", "治理结构缺失"}:
            blocking += 1
    # Keep the SQL-generated action for matching rows, but use the safer
    # classification action for every difference.
    final_action = classified_action if status == "差异" else "无需处理"
    out.append("\t".join([source_file, table_name, source_count, postgres_count, delta, status, category, final_action]))
    rows.append({
        "source_file": source_file,
        "table_name": table_name,
        "source_count": src_count,
        "postgres_count": pg_count,
        "delta": delta,
        "status": status,
        "difference_class": category,
        "review_action": final_action,
    })
with open(report_path, "w", encoding="utf-8") as dst:
    dst.write("\n".join(out) + "\n")
summary = {
    "generated_at": datetime.now(timezone.utc).isoformat(),
    "report": report_path,
    "runtime_schema_version": int(runtime_schema_version),
    "all_difference_count": all_differences,
    "blocking_difference_count": blocking,
    "classification_counts": dict(sorted(class_counts.items())),
    "blocking_classes": ["源数据缺失", "主键核对差异", "预算金额差异", "流程绑定孤立", "流程绑定差异", "业务绑定孤立", "运行策略违规"],
    "rows": rows,
}
with open(summary_path, "w", encoding="utf-8") as dst:
    json.dump(summary, dst, ensure_ascii=False, indent=2)
    dst.write("\n")
PY

# Keep a stable, review-friendly pointer alongside timestamped audit reports.
# An explicitly supplied REPORT_FILE remains untouched; the current pointer is
# only refreshed for the default report path.
if [[ -z "${REPORT_FILE:-}" ]]; then
    cp "$root_dir/$report_file" "$root_dir/reports/migration-diff-current.tsv"
    cp "$root_dir/$summary_file" "$root_dir/reports/migration-diff-current.summary.json"
fi

diff_count="$(awk -F '\t' 'NR>1 && $6=="差异" {c++} END {print c+0}' "$root_dir/$report_file")"
blocking_count="$(awk -F '\t' 'NR>1 && $6=="差异" && ($7=="源数据缺失" || $7=="主键核对差异" || $7=="预算金额差异" || $7=="流程绑定孤立" || $7=="流程绑定差异" || $7=="业务绑定孤立" || $7=="运行策略违规") {c++} END {print c+0}' "$root_dir/$report_file")"
echo "迁移差异报告已生成: $report_file"
echo "差异记录数: $diff_count"
echo "阻断性差异数: $blocking_count"
echo "差异分类摘要: $summary_file"
if [[ "$STRICT_MIGRATION_DIFF_ALL" == 'true' && "$diff_count" -gt 0 ]]; then
    echo '迁移差异校验失败: STRICT_MIGRATION_DIFF_ALL=true' >&2
    exit 1
fi
if [[ "$STRICT_MIGRATION_DIFF" == 'true' && "$blocking_count" -gt 0 ]]; then
    echo '迁移差异校验失败: 存在源数据缺失、绑定或金额差异（STRICT_MIGRATION_DIFF=true）' >&2
    exit 1
fi
