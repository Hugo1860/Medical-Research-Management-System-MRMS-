#!/usr/bin/env bash
set -euo pipefail

# Static-only disaster-recovery contract.  It does not invoke Docker, connect
# to PostgreSQL, read a dump, or contact an external storage provider.  The
# runtime drill remains deploy/verify-postgres-backup.sh.
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
export_script="$root/deploy/export-postgres-data.sh"
restore_script="$root/deploy/verify-postgres-backup.sh"
runbook="$root/docs/production-uat-runbook.md"
deploy_readme="$root/deploy/README.md"
database_readme="$root/database/postgresql/README.md"

for path in "$export_script" "$restore_script" "$runbook" "$deploy_readme" "$database_readme"; do
  [[ -f "$path" ]] || { echo "备份恢复契约失败: 缺少 $path" >&2; exit 1; }
done
for path in "$export_script" "$restore_script"; do
  [[ -x "$path" ]] || { echo "备份恢复契约失败: 脚本不可执行 $path" >&2; exit 1; }
  bash -n "$path" || { echo "备份恢复契约失败: shell 语法错误 $path" >&2; exit 1; }
done

for marker in \
  'set -euo pipefail' \
  'umask 077' \
  '--format=custom' \
  '--no-owner' \
  '--no-privileges' \
  'temporary_file="${EXPORT_FILE}.tmp.$$"' \
  'shasum -a 256' \
  "'schemaVersion': int(version)" \
  'mv -f "$temporary_file" "$EXPORT_FILE"' \
  'mv -f "$temporary_checksum" "${EXPORT_FILE}.sha256"' \
  'mv -f "$temporary_metadata" "$metadata_file"'; do
  grep -Fq -- "$marker" "$export_script" || {
    echo "备份恢复契约失败: 导出脚本缺少 $marker" >&2
    exit 1
  }
done

for marker in \
  'set -euo pipefail' \
  'umask 077' \
  'expected_version="${EXPECTED_SCHEMA_VERSION:-106}"' \
  'pg_restore --list' \
  "psql -U \"\$database_user\" -d \"\$database_name\" -Atc 'SELECT 1'" \
  '--exit-on-error' \
  '--clean' \
  '--if-exists' \
  'postgres:18' \
  'restore_storage_args=(--volume "$restore_volume:/var/lib/postgresql")' \
  'restore_storage_args=(--tmpfs "/var/lib/postgresql:rw,noexec,nosuid,size=${restore_tmpfs_size}")' \
  'restore_run_id="${RESTORE_RUN_ID:-$(date +%Y%m%d%H%M%S)-$$}"' \
  'RESTORE_CONTAINER' \
  'RESTORE_VOLUME' \
  'trap cleanup EXIT' \
  '系统迁移版本' \
  '科研成果状态凭证' \
  '科研评价奖励支付批次' \
  'restore-report.json'; do
  grep -Fq -- "$marker" "$restore_script" || {
    echo "备份恢复契约失败: 恢复脚本缺少 $marker" >&2
    exit 1
  }
done

for marker in \
  '备份校验文件缺失' \
  '备份元数据缺失' \
  '备份文件、校验和、元数据和恢复报告不能是符号链接' \
  '备份文件不存在或为空: ${BACKUP_FILE}；请先执行 deploy/export-postgres-data.sh'; do
  grep -Fq -- "$marker" "$restore_script" || {
    echo "备份恢复契约失败: 恢复脚本缺少完整性失败关闭标记 $marker" >&2
    exit 1
  }
done

# The scripts must never treat an existing named restore resource as their
# own.  This is the safety boundary that prevents a drill from deleting an
# operator-owned container or volume.
for marker in \
  '恢复容器已存在，为避免误删请指定新的 RESTORE_CONTAINER' \
  '恢复卷已存在，为避免误删请指定新的 RESTORE_VOLUME' \
  'container rm --force "$restore_name"' \
  'volume rm "$restore_volume"'; do
  grep -Fq -- "$marker" "$restore_script" || {
    echo "备份恢复契约失败: 缺少隔离资源安全边界 $marker" >&2
    exit 1
  }
done

python3 - "$root" "$runbook" "$deploy_readme" "$database_readme" <<'PY'
import json
import pathlib
import sys

root = pathlib.Path(sys.argv[1])
docs = [pathlib.Path(value).read_text(encoding='utf-8') for value in sys.argv[2:]]
for path in (
    root / 'ScientificResearch' / 'ScientificResearch' / 'appsettings.json',
    root / 'ScientificResearch' / 'ScientificResearch' / 'appsettings.Production.json',
):
    if not path.exists():
        continue
    data = json.loads(path.read_text(encoding='utf-8-sig'))
    expected = data.get('Postgres', {}).get('ExpectedSchemaVersion')
    if str(expected) != '106':
        raise SystemExit(f'备份恢复契约失败: {path} ExpectedSchemaVersion={expected!r}，期望 106')

joined = '\n'.join(docs)
required = {
    'runbook restore command': 'EXPECTED_SCHEMA_VERSION=106 BACKUP_FILE=',
    'runbook export command': 'EXPORT_FILE="$backup_file" ./deploy/export-postgres-data.sh',
    'runbook report evidence': 'restore-report.json',
    'deployment README restore command': 'EXPECTED_SCHEMA_VERSION=106 ./deploy/verify-postgres-backup.sh',
    'deployment README schema': 'schema 版本 106',
    'database README schema': 'Postgres__ExpectedSchemaVersion` 默认值为 `106`',
    'database README isolated restore': '临时 PostgreSQL 18 容器恢复',
}
missing = [label for label, marker in required.items() if marker not in joined]
if missing:
    raise SystemExit('备份恢复契约失败: 文档缺少 ' + ', '.join(missing))

# The runbook must state that the source and production volume stay untouched,
# and that the report is retained as evidence rather than implying runtime
# success from the static checks.
for marker in ('不会操作正式数据库卷', '恢复演练报告'):
    if marker not in joined:
        raise SystemExit(f'备份恢复契约失败: 文档缺少边界说明 {marker}')
print('PostgreSQL 备份恢复静态契约通过: Schema106、custom dump、校验元数据、隔离恢复和演练证据齐全')
PY

if ! grep -Fq './deploy/verify-postgres-backup-contract.sh' "$root/deploy/verify-local-release.sh"; then
  echo '备份恢复契约失败: verify-local-release.sh 未纳入静态门禁' >&2
  exit 1
fi

echo 'PostgreSQL 备份恢复静态契约通过: 不连接 Docker/数据库，运行时恢复需另行执行'
