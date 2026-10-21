#!/usr/bin/env bash
set -euo pipefail
umask 077

# Verify an existing custom-format PostgreSQL backup by restoring it into an
# isolated PostgreSQL 18 container. The source database and its data volume are
# never modified; temporary objects are removed only when created by this run.
: "${BACKUP_FILE:?请设置 BACKUP_FILE，例如 /private/tmp/scientificresearch-backup.dump}"

database_container="${DATABASE_CONTAINER:-2018-7-24-db-1}"
database_name="${DATABASE_NAME:-ScientificResearch_Model}"
database_user="${DATABASE_USER:-postgres}"
script_root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
expected_version="${EXPECTED_SCHEMA_VERSION:-106}"
docker_timeout="${DOCKER_COMMAND_TIMEOUT_SECONDS:-60}"
restore_ready_timeout="${RESTORE_READY_TIMEOUT_SECONDS:-120}"
restore_password="${RESTORE_PASSWORD:-acceptance-only}"
report_file="${RESTORE_REPORT_FILE:-${BACKUP_FILE}.restore-report.json}"

# A unique default prevents collisions with an operator's existing container or
# volume. Explicit names are accepted but are never replaced silently.
restore_run_id="${RESTORE_RUN_ID:-$(date +%Y%m%d%H%M%S)-$$}"
explicit_restore_container="${RESTORE_CONTAINER:-}"
explicit_restore_volume="${RESTORE_VOLUME:-}"
restore_name="${explicit_restore_container:-scientificresearch-postgres-restore-acceptance-${restore_run_id}}"
restore_volume="$explicit_restore_volume"
restore_tmpfs_size="${RESTORE_TMPFS_SIZE:-512m}"

run_docker() {
  timeout --foreground --kill-after=5 "$docker_timeout" docker "$@"
}

fail() {
  echo "备份恢复验收失败: $*" >&2
  exit 1
}

if [[ -e "$BACKUP_FILE" && ! -f "$BACKUP_FILE" ]]; then
  fail "BACKUP_FILE 不是普通文件: $BACKUP_FILE"
fi
if [[ -L "$BACKUP_FILE" || -L "${BACKUP_FILE}.sha256" || -L "${BACKUP_FILE}.json" || -L "$report_file" ]]; then
  fail '备份文件、校验和、元数据和恢复报告不能是符号链接'
fi
if [[ ! -s "$BACKUP_FILE" ]]; then
  fail "备份文件不存在或为空: ${BACKUP_FILE}；请先执行 deploy/export-postgres-data.sh"
fi
if ! [[ "$expected_version" =~ ^[0-9]+$ ]] || (( expected_version < 1 )); then
  fail "EXPECTED_SCHEMA_VERSION 必须为正整数: $expected_version"
fi
if ! [[ "$restore_tmpfs_size" =~ ^[1-9][0-9]*[kmgt]$ ]]; then
  fail "RESTORE_TMPFS_SIZE 必须使用类似 512m 或 1g 的值: $restore_tmpfs_size"
fi

# Export creates these sidecars. A restore drill is an integrity acceptance
# event, so it fails closed when either the checksum or schema metadata is
# missing instead of silently accepting an unverified dump.
actual_checksum="$(shasum -a 256 "$BACKUP_FILE" | awk '{print $1}')"
checksum_file="${BACKUP_FILE}.sha256"
if [[ ! -f "$checksum_file" ]]; then
  fail "备份校验文件缺失: $checksum_file；请使用 export-postgres-data.sh 生成完整备份"
fi
expected_checksum="$(awk 'NF >= 1 { print $1; exit }' "$checksum_file")"
if [[ ! "$expected_checksum" =~ ^[[:xdigit:]]{64}$ || "$expected_checksum" != "$actual_checksum" ]]; then
  fail "备份校验和不匹配: ${checksum_file}"
fi
echo "备份校验和 OK: $actual_checksum"

metadata_file="${BACKUP_FILE}.json"
if [[ ! -f "$metadata_file" ]]; then
  fail "备份元数据缺失: $metadata_file；请使用 export-postgres-data.sh 生成完整备份"
fi
if ! python3 - "$metadata_file" "$actual_checksum" "$expected_version" <<'PY'
import json, sys
path, actual_checksum, expected_version = sys.argv[1:]
try:
    with open(path, encoding='utf-8') as fh:
        data = json.load(fh)
except Exception as exc:
    raise SystemExit(f'元数据不是有效 JSON: {exc}')
value = data.get('schemaVersion', '')
if value not in ('', None) and (not isinstance(value, int) or value < 1):
    raise SystemExit('元数据 schemaVersion 必须为正整数')
if str(value) != str(expected_version):
    raise SystemExit(f'备份元数据 schemaVersion={value} 与 EXPECTED_SCHEMA_VERSION={expected_version} 不一致')
if data.get('sha256') != actual_checksum:
    raise SystemExit('备份元数据 sha256 与实际文件不匹配')
PY
then
  fail "备份元数据校验失败: $metadata_file"
fi

# Generated names include the process id and are collision-resistant, so avoid
# two extra Docker API calls in the normal path. Explicit operator names still
# get a collision check and fail closed; no existing resource is replaced.
if [[ -n "$explicit_restore_container" ]] && run_docker container inspect "$restore_name" >/dev/null 2>&1; then
  fail "恢复容器已存在，为避免误删请指定新的 RESTORE_CONTAINER: $restore_name"
fi
if [[ -n "$explicit_restore_volume" ]] && run_docker volume inspect "$restore_volume" >/dev/null 2>&1; then
  fail "恢复卷已存在，为避免误删请指定新的 RESTORE_VOLUME: $restore_volume"
fi

container_created=false
volume_created=false
cleanup() {
  local status=$?
  if [[ "$container_created" == 'true' ]]; then
    run_docker container rm --force "$restore_name" >/dev/null 2>&1 || true
  fi
  if [[ "$volume_created" == 'true' ]]; then
    run_docker volume rm "$restore_volume" >/dev/null 2>&1 || true
  fi
  exit "$status"
}
trap cleanup EXIT

restore_storage_label="tmpfs:${restore_tmpfs_size}"
if [[ -n "$restore_volume" ]]; then
  run_docker volume create "$restore_volume" >/dev/null || fail "无法创建隔离恢复卷"
  volume_created=true
  restore_storage_label="volume:${restore_volume}"
  # PostgreSQL 18 stores data below a major-version directory; mount the
  # parent path so the official image can initialize /var/lib/postgresql/18.
  restore_storage_args=(--volume "$restore_volume:/var/lib/postgresql")
else
  # tmpfs keeps the drill self-contained and avoids allocating a persistent
  # Docker data volume. It is discarded with the container on every exit.
  restore_storage_args=(--tmpfs "/var/lib/postgresql:rw,noexec,nosuid,size=${restore_tmpfs_size}")
fi
run_docker run --detach --name "$restore_name" \
  --env POSTGRES_DB="$database_name" \
  --env POSTGRES_USER="$database_user" \
  --env POSTGRES_PASSWORD="$restore_password" \
  "${restore_storage_args[@]}" \
  postgres:18 >/dev/null || fail "无法启动隔离 PostgreSQL 容器"
container_created=true

restore_ready=false
for ((elapsed = 0; elapsed < restore_ready_timeout; elapsed++)); do
  if run_docker exec "$restore_name" pg_isready -U "$database_user" -d "$database_name" >/dev/null 2>&1 \
      && run_docker exec "$restore_name" psql -U "$database_user" -d "$database_name" -Atc 'SELECT 1' >/dev/null 2>&1; then
    restore_ready=true
    break
  fi
  sleep 1
done
if [[ "$restore_ready" != 'true' ]]; then
  fail "恢复 PostgreSQL 容器在 ${restore_ready_timeout} 秒内未就绪"
fi

# Validate the archive before applying it. This catches a truncated or
# non-custom-format file without touching the restored database.
run_docker exec -i "$restore_name" pg_restore --list < "$BACKUP_FILE" >/dev/null \
  || fail "备份文件不是可读的 PostgreSQL custom-format archive"

run_docker exec -i "$restore_name" pg_restore \
  --exit-on-error \
  --clean \
  --if-exists \
  --no-owner \
  --no-privileges \
  --dbname="$database_name" \
  --username="$database_user" < "$BACKUP_FILE" \
  || fail "pg_restore 恢复失败"

verification_output="$(run_docker exec "$restore_name" psql \
  -U "$database_user" \
  -d "$database_name" \
  -v ON_ERROR_STOP=1 \
  -Atc "SELECT \"版本\"||chr(124)||\"脚本\" FROM \"系统迁移版本\" WHERE \"编号\" = 1; SELECT count(*) >= 120 FROM pg_class WHERE relkind = 'r' AND relnamespace = 'public'::regnamespace; SELECT count(*) >= 55 FROM pg_views WHERE schemaname = 'public'; SELECT count(*) = 8 FROM \"fn_迁移遗留例程状态\"() WHERE \"状态\" = '已退休'; SELECT (${expected_version}::integer < 85 OR to_regprocedure('public.\"sp_权限代理_增\"(integer,integer,character varying,timestamp without time zone,timestamp without time zone,character varying)') IS NOT NULL AND to_regprocedure('public.\"sp_预算异常对账_处理\"(jsonb)') IS NOT NULL AND EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_权限代理_禁止重叠') AND to_regprocedure('public.\"sp_hrp中间库双向协同\"(integer,character varying)') IS NOT NULL AND COALESCE(pg_get_functiondef(to_regprocedure('public.\"sp_hrp中间库双向协同\"(integer,character varying)')), '') ILIKE '%push_only%' AND COALESCE(pg_get_functiondef(to_regprocedure('public.\"sp_hrp中间库双向协同\"(integer,character varying)')), '') NOT ILIKE '%hrp_sync_incoming%' AND COALESCE(pg_get_functiondef(to_regprocedure('public.\"sp_hrp中间库双向协同\"(integer,character varying)')), '') NOT ILIKE '%hrp_sync_expense%' AND COALESCE(pg_get_functiondef(to_regprocedure('public.\"sp_hrp中间库双向协同\"(integer,character varying)')), '') NOT ILIKE '%hrp-voucher%' AND COALESCE(pg_get_functiondef(to_regprocedure('public.\"sp_hrp中间库双向协同\"(integer,character varying)')), '') NOT ILIKE '%push_and_replay%' AND COALESCE(pg_get_functiondef(to_regprocedure('public.\"sp_hrp中间库双向协同\"(integer,character varying)')), '') NOT ILIKE '%replay%'); SELECT (${expected_version}::integer < 88 OR to_regclass('public.\"纵向项目申报预检查记录\"') IS NOT NULL AND to_regclass('public.\"科研经费发票\"') IS NOT NULL AND to_regclass('public.\"人员能力标签\"') IS NOT NULL AND to_regclass('public.\"科研评价年度批次\"') IS NOT NULL AND to_regclass('public.\"伦理电子档案\"') IS NOT NULL AND to_regclass('public.\"移动协同消息配置\"') IS NOT NULL AND to_regclass('public.\"科研成果治理历史\"') IS NOT NULL AND to_regclass('public.\"科研成果合并审计\"') IS NOT NULL AND to_regclass('public.\"专家可用时间\"') IS NOT NULL AND to_regclass('public.\"外部专家邀请\"') IS NOT NULL AND to_regclass('public.\"科研成果状态凭证\"') IS NOT NULL AND to_regclass('public.\"科研成果状态凭证核验\"') IS NOT NULL AND to_regclass('public.\"科研评价奖励支付批次\"') IS NOT NULL AND to_regclass('public.\"科研评价奖励支付流水\"') IS NOT NULL AND to_regclass('public.\"横向项目生命周期申请\"') IS NOT NULL AND to_regclass('public.\"横向项目研究报告\"') IS NOT NULL AND to_regclass('public.\"横向项目档案材料\"') IS NOT NULL AND EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='移动协同消息配置' AND column_name='配置状态'));")" \
  || fail "恢复后的对象查询失败"

printf '%s\n' "$verification_output"
expected_version_padded="$(printf '%03d' "$expected_version")"
expected_migration_script="$(find "$script_root/database/postgresql/migrations" -maxdepth 1 -type f -name "${expected_version_padded}_*.sql" -print | sort | head -n 1)"
expected_migration_script="${expected_migration_script##*/}"
if [[ -z "$expected_migration_script" ]]; then
  fail "仓库缺少迁移 ${expected_version_padded} 对应脚本"
fi
verification_line_count="$(printf '%s\n' "$verification_output" | awk 'END { print NR }')"
verification_line_1="$(printf '%s\n' "$verification_output" | sed -n '1p')"
verification_version="${verification_line_1%%|*}"
verification_script="${verification_line_1#*|}"
legacy_migration_script="001-${expected_version_padded}"
if [[ "$verification_line_count" != '6' || "$verification_version" != "$expected_version" || ("$verification_script" != "$expected_migration_script" && "$verification_script" != "$legacy_migration_script") ]]; then
  fail "迁移版本校验不匹配，期望 ${expected_version}|${expected_migration_script}"
fi
for verification_index in 2 3 4 5 6; do
  verification_value="$(printf '%s\n' "$verification_output" | sed -n "${verification_index}p")"
  if [[ "$verification_value" != 't' ]]; then
    fail "恢复对象校验第 ${verification_index} 项为 ${verification_value}"
  fi
done

mkdir -p "$(dirname "$report_file")"
python3 - "$report_file" "$BACKUP_FILE" "$actual_checksum" "$expected_version" "$restore_name" "$restore_storage_label" <<'PY'
import json, os, sys
report_path, backup_path, checksum, version, container, volume = sys.argv[1:]
payload = {
    'status': 'passed',
    'backupFile': os.path.abspath(backup_path),
    'sha256': checksum,
    'expectedSchemaVersion': int(version),
    'restoreContainer': container,
    'restoreVolume': volume,
    'sourceDatabase': os.environ.get('DATABASE_NAME', 'ScientificResearch_Model'),
    'verification': 'schema, objects, routines, triggers and feature tables',
}
temporary = report_path + '.tmp'
with open(temporary, 'w', encoding='utf-8') as fh:
    json.dump(payload, fh, ensure_ascii=False, indent=2)
    fh.write('\n')
os.replace(temporary, report_path)
PY

echo "PostgreSQL 备份恢复验收通过: $BACKUP_FILE"
echo "恢复演练报告: $report_file"
