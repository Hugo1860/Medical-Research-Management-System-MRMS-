#!/usr/bin/env bash
set -euo pipefail

# Apply only migrations newer than the version already registered in a
# non-empty PostgreSQL database. Fresh databases must use Compose init.
database_container="${DATABASE_CONTAINER:-2018-7-24-db-1}"
database_name="${DATABASE_NAME:-ScientificResearch_Model}"
database_user="${DATABASE_USER:-postgres}"
dry_run="${DRY_RUN:-false}"
command_timeout="${PSQL_TIMEOUT_SECONDS:-60}"
lock_key_1="${MIGRATION_LOCK_KEY_1:-20180724}"
lock_key_2="${MIGRATION_LOCK_KEY_2:-73001}"
script_root="$(cd "$(dirname "$0")/.." && pwd)"
migration_dir="${MIGRATION_DIR:-${script_root}/database/postgresql/migrations}"

if [[ ! -d "$migration_dir" ]]; then
  echo "迁移目录不存在: $migration_dir" >&2
  exit 1
fi

run_psql() {
  timeout "$command_timeout" docker exec -i "$database_container" psql \
    -U "$database_user" -d "$database_name" -v ON_ERROR_STOP=1 "$@"
}

run_migration() {
  local migration="$1"
  # Keep the lock in the same psql session as the migration. This prevents two
  # operators from applying the same numbered file concurrently while still
  # releasing the lock automatically when the session exits on an error.
  {
    printf 'SELECT pg_advisory_lock(%s, %s);\n' "$lock_key_1" "$lock_key_2"
    cat "$migration"
    printf 'SELECT pg_advisory_unlock(%s, %s);\n' "$lock_key_1" "$lock_key_2"
  } | run_psql
}

current_version="$(run_psql -Atc 'SELECT COALESCE((SELECT "版本" FROM "系统迁移版本" WHERE "编号" = 1), 0);' 2>/dev/null | tr -d '[:space:]')"
if [[ ! "$current_version" =~ ^[0-9]+$ ]] || (( current_version <= 0 )); then
  echo '无法读取有效的当前迁移版本；请使用全新 PostgreSQL 数据库的 Compose 初始化流程' >&2
  exit 1
fi

requested_target="${TARGET_SCHEMA_VERSION:-}"
if [[ -n "$requested_target" && ! "$requested_target" =~ ^[0-9]+$ ]]; then
  echo "目标版本无效: ${requested_target}" >&2
  exit 1
fi
latest_version="$current_version"
previous_version=-1
previous_file=''
for migration in "$migration_dir"/[0-9][0-9][0-9]_*.sql; do
  [[ -f "$migration" ]] || continue
  filename="${migration##*/}"
  number="${filename%%_*}"
  [[ "$number" =~ ^[0-9]+$ ]] || continue
  numeric_number=$((10#$number))
  if (( numeric_number == previous_version )); then
    echo "迁移版本重复: ${numeric_number} (${previous_file} 与 ${filename})" >&2
    exit 1
  fi
  previous_version=$numeric_number
  previous_file="$filename"
  if (( numeric_number > latest_version )); then latest_version=$numeric_number; fi
done

target_version="${requested_target:-$latest_version}"
pending=()
pending_count=0
for migration in "$migration_dir"/[0-9][0-9][0-9]_*.sql; do
  [[ -f "$migration" ]] || continue
  filename="${migration##*/}"
  number="${filename%%_*}"
  [[ "$number" =~ ^[0-9]+$ ]] || continue
  numeric_number=$((10#$number))
  if (( numeric_number > current_version && numeric_number <= target_version )); then
    pending+=("$migration")
    pending_count=$((pending_count + 1))
  fi
done

if (( target_version < current_version )); then
  echo "目标版本 ${target_version} 低于当前版本 ${current_version}，拒绝降级" >&2
  exit 1
fi
if (( target_version > latest_version )); then
  echo "目标版本 ${target_version} 超过仓库最新迁移 ${latest_version}" >&2
  exit 1
fi

# Do not silently skip a migration file. A version jump can only be applied
# when every intermediate numbered script is present in the repository.
expected_version=$((current_version + 1))
if (( pending_count > 0 )); then
  for migration in "${pending[@]}"; do
    filename="${migration##*/}"
    number="${filename%%_*}"
    numeric_number=$((10#$number))
    if (( numeric_number != expected_version )); then
      echo "迁移序列不连续: 期望 ${expected_version}，实际 ${numeric_number} (${filename})" >&2
      exit 1
    fi
    expected_version=$((expected_version + 1))
  done
fi
if (( expected_version <= target_version )); then
  echo "迁移序列缺少版本: ${expected_version}" >&2
  exit 1
fi

if (( pending_count == 0 )); then
  echo "PostgreSQL 已是目标版本 ${current_version}"
  exit 0
fi

echo "当前版本: ${current_version}; 目标版本: ${target_version}; 待执行: ${pending_count} 个"
for migration in "${pending[@]}"; do
  filename="${migration##*/}"
  number="${filename%%_*}"
  numeric_number=$((10#$number))
  echo "迁移 ${numeric_number}: ${filename}"
  if [[ "$dry_run" == 'true' ]]; then
    continue
  fi
  run_migration "$migration"
  registered="$(run_psql -Atc 'SELECT "版本" FROM "系统迁移版本" WHERE "编号" = 1;' | tr -d '[:space:]')"
  if [[ "$registered" != "$numeric_number" ]]; then
    echo "迁移 ${filename} 执行后版本不匹配: 期望 ${numeric_number}，实际 ${registered}" >&2
    exit 1
  fi
done

if [[ "$dry_run" == 'true' ]]; then
  echo 'dry-run 完成，数据库未修改'
else
  echo "PostgreSQL 迁移完成，当前版本 ${target_version}"
fi
