#!/usr/bin/env bash
set -euo pipefail

# Replay repository migrations and run behavior tests in disposable containers.
# No host port, existing database volume, supplier endpoint or real login is used.
root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
cd "$root"
# An inherited production connection or HTTP probe must never escape this run.
unset DATABASE_URL FRESH_DATABASE_URL PGHOST PGPORT PGDATABASE PGUSER PGPASSWORD PGSERVICE PGSERVICEFILE PGOPTIONS
export DATABASE_NAME=ScientificResearch_Model DATABASE_USER=postgres CHECK_HTTP=false APP_BASE_URL=
export EXPECTED_SCHEMA_VERSION="$(./deploy/read-current-schema-version.sh)"
run_id="$(date +%Y%m%d%H%M%S)-$$"
database_container="hrms-regression-db-$run_id"
test_image="scientificresearch:regression-$run_id"
test_container="hrms-regression-tests-$run_id"
log_dir="$root/reports/local-regression-$run_id"
mkdir -p "$log_dir"
chmod 700 "$log_dir"
database_created=false
image_created=false
test_created=false
started_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
phase=build
cleanup() {
  result=$?
  trap - EXIT
  if [ "$test_created" = true ]; then docker rm -f "$test_container" >/dev/null 2>&1 || true; fi
  if [ "$database_created" = true ]; then docker rm -f "$database_container" >/dev/null 2>&1 || true; fi
  if [ "$image_created" = true ]; then docker image rm "$test_image" >/dev/null 2>&1 || true; fi
  python3 - "$log_dir/summary.json" "$result" "$phase" "$started_at" <<'PY'
import json, sys
from datetime import datetime, timezone
from pathlib import Path
Path(sys.argv[1]).write_text(json.dumps({
    'started_at': sys.argv[4], 'finished_at': datetime.now(timezone.utc).isoformat(),
    'status': 'passed' if sys.argv[2] == '0' else 'failed', 'last_phase': sys.argv[3],
    'environment': 'disposable PostgreSQL; production controller/worker code; synthetic fixtures',
    'real_login_tested': False, 'supplier_integration_tested': False, 'production_deployed': False,
}, ensure_ascii=False, indent=2) + '\n')
PY
  echo "Regression evidence: $log_dir (exit $result, phase $phase)"
  exit "$result"
}
trap cleanup EXIT
python3 deploy/release-source-manifest.py write "$log_dir/source-manifest.json"
docker build --build-arg "SDK_IMAGE=${SDK_IMAGE:-mcr.microsoft.com/dotnet/sdk:10.0}" --build-arg "NUGET_REMOTE_SOURCE=${NUGET_REMOTE_SOURCE-https://api.nuget.org/v3/index.json}" -f deploy/Dockerfile.regression --target regression -t "$test_image" . >"$log_dir/build.log" 2>&1
image_created=true
phase=database
docker run -d --name "$database_container" --network none --tmpfs /var/lib/postgresql \
  -e POSTGRES_HOST_AUTH_METHOD=trust -e POSTGRES_DB=ScientificResearch_Model \
  -v "$root/database/postgresql/migrations:/audit-migrations:ro" postgres:18 >/dev/null
database_created=true
ready=false
for attempt in $(seq 1 30); do
  if docker exec "$database_container" pg_isready -U postgres >/dev/null 2>&1; then ready=true; break; fi
  sleep 1
done
if [ "$ready" != true ]; then echo 'Isolated PostgreSQL did not become ready' >&2; exit 1; fi
phase=migrations
for migration in database/postgresql/migrations/[0-9][0-9][0-9]_*.sql; do
  timeout 90 docker exec "$database_container" psql -X -U postgres -d ScientificResearch_Model \
    -v ON_ERROR_STOP=1 -f "/audit-migrations/${migration##*/}" >>"$log_dir/migrations.log" 2>&1
done
DATABASE_CONTAINER="$database_container" ./database/postgresql/verify_migration.sh >"$log_dir/schema.log" 2>&1
echo 'PASS isolated migration replay and schema verification'
phase=menus
DATABASE_CONTAINER="$database_container" ./deploy/verify-menu-paths.sh >"$log_dir/menus.log" 2>&1
echo 'PASS menu source paths (HTTP checks are separate)'
phase=journal
DATABASE_CONTAINER="$database_container" ./deploy/verify-journal-partition-regression.sh >"$log_dir/journal.log" 2>&1
echo 'PASS journal SQL regression'
phase=backend-behavior
connection='Host=127.0.0.1;Port=5432;Database=ScientificResearch_Model;Username=postgres'
docker create --name "$test_container" --network "container:$database_container" \
  -e "EXTERNAL_SYNC_TEST_CONNECTION=$connection" -e EXTERNAL_SYNC_TEST_ALLOW_SCHEMA_CREATE=YES \
  -e "INSTRUMENT_TEST_CONNECTION=$connection" -e INSTRUMENT_TEST_ALLOW_FIXTURES=YES \
  -e "PROPOSAL_IMPORT_TEST_CONNECTION=$connection" -e PROPOSAL_IMPORT_TEST_ALLOW_FIXTURES=YES \
  -e "VERTICAL_SCOPE_TEST_CONNECTION=$connection" -e VERTICAL_SCOPE_TEST_ALLOW_FIXTURES=YES \
  -e "VERTICAL_LIFECYCLE_TEST_CONNECTION=$connection" -e VERTICAL_LIFECYCLE_TEST_ALLOW_FIXTURES=YES \
  -e "VERTICAL_WRITE_TEST_CONNECTION=$connection" -e VERTICAL_WRITE_TEST_ALLOW_FIXTURES=YES \
  -e "MENU_MOVE_TEST_CONNECTION=$connection" -e MENU_MOVE_TEST_ALLOW_FIXTURES=YES -e MENU_MOVE_TEST_MIGRATION=/audit-migration.sql \
  -v "$root/database/postgresql/migrations/120_science_office_menu_relocation.sql:/audit-migration.sql:ro" \
  "$test_image" sh -ec 'for name in ExternalSyncRegression InstrumentRegression ProposalImportRegression VerticalScopeRegression VerticalLifecycleScopeRegression VerticalWriteScopeRegression MenuMoveRegression; do dotnet "/checks/$name/$name.dll"; done' >/dev/null
test_created=true
docker start -a "$test_container" >"$log_dir/backend.log" 2>&1
test_exit="$(docker inspect --format '{{.State.ExitCode}}' "$test_container")"
if [ "$test_exit" != 0 ]; then echo "Backend regression failed; inspect $log_dir/backend.log" >&2; exit "$test_exit"; fi
echo 'PASS backend behavior regressions'
phase=frontend
(cd frontend && pnpm install --frozen-lockfile --prefer-offline && pnpm test) >"$log_dir/frontend.log" 2>&1
python3 deploy/release-source-manifest.py check "$log_dir/source-manifest.json"
phase=complete
echo 'PASS all isolated regressions (does not constitute hospital UAT)'
