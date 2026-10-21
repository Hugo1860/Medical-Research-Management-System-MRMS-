#!/usr/bin/env bash
set -euo pipefail

# Bounded read-only load smoke. This is a staging/release check, not a full
# capacity study: it only calls health probes and public HTML routes.
app_base_url="${APP_BASE_URL:-http://127.0.0.1:16379}"
requests="${LOAD_REQUESTS:-80}"
concurrency="${LOAD_CONCURRENCY:-8}"
max_p95_ms="${LOAD_MAX_P95_MS:-1000}"
max_error_rate="${LOAD_MAX_ERROR_RATE:-0}"
paths="${LOAD_PATHS:-/health/live,/health/ready,/dashboard,/project/vertical-application}"

python3 - "$app_base_url" "$requests" "$concurrency" "$max_p95_ms" "$max_error_rate" "$paths" <<'PY'
import concurrent.futures
import json
import sys
import time
import urllib.request

base, raw_requests, raw_concurrency, raw_max_p95, raw_max_error, raw_paths = sys.argv[1:]
requests_count = int(raw_requests)
concurrency = int(raw_concurrency)
max_p95_ms = float(raw_max_p95)
max_error_rate = float(raw_max_error)
paths = [p.strip() for p in raw_paths.split(',') if p.strip()]
if requests_count < 1 or requests_count > 5000:
    raise SystemExit('LOAD_REQUESTS 必须为 1-5000')
if concurrency < 1 or concurrency > 100:
    raise SystemExit('LOAD_CONCURRENCY 必须为 1-100')
if not paths:
    raise SystemExit('LOAD_PATHS 不能为空')
if max_p95_ms <= 0 or not 0 <= max_error_rate <= 100:
    raise SystemExit('LOAD_MAX_P95_MS 或 LOAD_MAX_ERROR_RATE 无效')

def request(path):
    url = base.rstrip('/') + path
    started = time.perf_counter()
    try:
        req = urllib.request.Request(url, headers={'Accept': 'text/html'})
        with urllib.request.urlopen(req, timeout=15) as response:
            code = response.status
            response.read(256)
        error = None if code == 200 else f'HTTP {code}'
    except Exception as exc:
        code = 0
        error = str(exc)
    return {'path': path, 'status': code, 'ms': (time.perf_counter() - started) * 1000, 'error': error}

jobs = [paths[i % len(paths)] for i in range(requests_count)]
with concurrent.futures.ThreadPoolExecutor(max_workers=concurrency) as pool:
    results = list(pool.map(request, jobs))

durations = sorted(item['ms'] for item in results)
index = min(len(durations) - 1, int((len(durations) - 1) * 0.95 + 0.999999))
p95 = durations[index]
errors = [item for item in results if item['error']]
error_rate = len(errors) * 100 / len(results)
summary = {
    'baseUrl': base,
    'requests': len(results),
    'concurrency': concurrency,
    'paths': paths,
    'p95Ms': round(p95, 1),
    'errorRate': round(error_rate, 2),
    'errors': errors[:10],
}
print(json.dumps(summary, ensure_ascii=False))
if p95 > max_p95_ms:
    raise SystemExit(f'p95 超过门槛: {p95:.1f}ms > {max_p95_ms:.1f}ms')
if error_rate > max_error_rate:
    raise SystemExit(f'错误率超过门槛: {error_rate:.2f}% > {max_error_rate:.2f}%')
print('只读并发压力烟测通过（不代表正式容量/稳定性测试）')
PY
