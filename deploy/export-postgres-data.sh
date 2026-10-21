#!/usr/bin/env bash
set -euo pipefail
umask 077

# Controlled full PostgreSQL export. The source database is read only; no
# application or database volume is restarted, removed, or replaced.
: "${EXPORT_FILE:?请设置 EXPORT_FILE，例如 /secure/backup/scientificresearch-$(date +%Y%m%d-%H%M%S).dump}"

database_container="${DATABASE_CONTAINER:-2018-7-24-db-1}"
database_name="${DATABASE_NAME:-ScientificResearch_Model}"
database_user="${DATABASE_USER:-postgres}"
docker_timeout="${DOCKER_COMMAND_TIMEOUT_SECONDS:-120}"
metadata_file="${EXPORT_METADATA_FILE:-${EXPORT_FILE}.json}"

run_docker() {
  timeout --foreground --kill-after=5 "$docker_timeout" docker "$@"
}
fail() {
  echo "完整数据导出失败: $*" >&2
  exit 1
}

if [[ -e "$EXPORT_FILE" && "${ALLOW_BACKUP_OVERWRITE:-false}" != 'true' ]]; then
  fail "目标文件已存在，为避免覆盖请更换 EXPORT_FILE 或显式设置 ALLOW_BACKUP_OVERWRITE=true: $EXPORT_FILE"
fi
if [[ -L "$EXPORT_FILE" || -L "${EXPORT_FILE}.sha256" || -L "$metadata_file" ]]; then
  fail "备份目标不能是符号链接"
fi
mkdir -p "$(dirname "$EXPORT_FILE")"

temporary_file="${EXPORT_FILE}.tmp.$$"
temporary_checksum="${EXPORT_FILE}.sha256.tmp.$$"
temporary_metadata="${metadata_file}.tmp.$$"
cleanup() {
  rm -f "$temporary_file" "$temporary_checksum" "$temporary_metadata"
}
trap cleanup EXIT

if ! run_docker exec "$database_container" pg_dump \
    --format=custom \
    --no-owner \
    --no-privileges \
    --dbname="$database_name" \
    --username="$database_user" > "$temporary_file"; then
  fail "无法在 ${docker_timeout} 秒内从 ${database_container} 导出"
fi
if [[ ! -s "$temporary_file" ]]; then
  fail '导出文件为空'
fi

checksum="$(shasum -a 256 "$temporary_file" | awk '{print $1}')"
printf '%s  %s\n' "$checksum" "$(basename "$EXPORT_FILE")" > "$temporary_checksum"
schema_version="$(run_docker exec "$database_container" psql -U "$database_user" -d "$database_name" -Atc 'SELECT "版本" FROM "系统迁移版本" WHERE "编号"=1;' | tr -d '[:space:]')" || fail '无法读取数据库迁移版本'
if ! [[ "$schema_version" =~ ^[0-9]+$ ]] || (( schema_version < 1 )); then
  fail "数据库迁移版本无效: ${schema_version:-空值}"
fi

python3 - "$temporary_metadata" "$database_name" "$schema_version" "$(basename "$EXPORT_FILE")" "$checksum" <<'PY'
import json, os, sys
path, database, version, filename, checksum = sys.argv[1:]
payload = {
    'scope': '完整数据库',
    'database': database,
    'schemaVersion': int(version),
    'file': filename,
    'sha256': checksum,
}
with open(path, 'w', encoding='utf-8') as fh:
    json.dump(payload, fh, ensure_ascii=False, indent=2)
    fh.write('\n')
PY

# Commit all three artifacts only after dump, checksum and metadata are valid.
mv -f "$temporary_file" "$EXPORT_FILE"
mv -f "$temporary_checksum" "${EXPORT_FILE}.sha256"
mv -f "$temporary_metadata" "$metadata_file"
trap - EXIT

echo "完整 PostgreSQL 数据导出完成: $EXPORT_FILE"
echo "校验文件: ${EXPORT_FILE}.sha256"
echo "元数据文件: $metadata_file"
