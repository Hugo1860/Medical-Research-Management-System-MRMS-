#!/usr/bin/env bash
set -euo pipefail

# Read-only response-time baseline for local/staging release checks.
# This is a smoke baseline, not a load test and never calls authenticated APIs.
app_base_url="${APP_BASE_URL:-http://127.0.0.1:16379}"
iterations="${PERF_ITERATIONS:-8}"
max_health_ms="${PERF_MAX_HEALTH_MS:-2000}"
max_page_ms="${PERF_MAX_PAGE_MS:-3000}"

if ! [[ "$iterations" =~ ^[0-9]+$ ]] || (( iterations < 1 || iterations > 100 )); then
  echo "PERF_ITERATIONS 必须为 1-100" >&2
  exit 2
fi

measure() {
  local path="$1"
  local output_file="$2"
  : > "$output_file"
  for _ in $(seq 1 "$iterations"); do
    # SPA history routes are served by the fallback only for HTML requests;
    # send the same Accept header a browser sends so this measures the real
    # page path instead of a JSON/404 negotiation response.
    curl --silent --show-error --max-time 10 -H 'Accept: text/html' -o /dev/null \
      -w '%{http_code} %{time_total}\n' "${app_base_url%/}${path}" >> "$output_file"
  done
}

p95_ms() {
  python3 - "$1" <<'PY'
import sys
rows=[]
for line in open(sys.argv[1], encoding='utf-8'):
    code, seconds = line.split()
    if code != '200':
        raise SystemExit(f'HTTP {code}')
    rows.append(float(seconds) * 1000)
rows.sort()
index=max(0, min(len(rows)-1, int((len(rows)-1) * 0.95 + 0.999999)))
print(f'{rows[index]:.1f}')
PY
}

tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT

declare -a health_paths=(
  '/health/live'
  '/health/ready'
)
declare -a page_paths=(
  '/dashboard'
  '/project/vertical-application'
  '/IMIS/views/科研办公/科研办公/项目生命周期.html'
)

echo "性能基线：${app_base_url}，每个路径 ${iterations} 次（只读）"
for path in "${health_paths[@]}"; do
  file="$tmp_dir/health-${RANDOM}"
  measure "$path" "$file"
  p95="$(p95_ms "$file")"
  echo "${path}: p95 ${p95} ms"
  python3 - "$p95" "$max_health_ms" <<'PY'
import sys
if float(sys.argv[1]) > float(sys.argv[2]):
    raise SystemExit(f'p95 超过健康检查门槛: {sys.argv[1]}ms > {sys.argv[2]}ms')
PY
done

for path in "${page_paths[@]}"; do
  file="$tmp_dir/page-${RANDOM}"
  measure "$path" "$file"
  p95="$(p95_ms "$file")"
  echo "${path}: p95 ${p95} ms"
  python3 - "$p95" "$max_page_ms" <<'PY'
import sys
if float(sys.argv[1]) > float(sys.argv[2]):
    raise SystemExit(f'p95 超过页面门槛: {sys.argv[1]}ms > {sys.argv[2]}ms')
PY
done

echo '性能基线通过（不代表正式压力测试通过）'
