#!/usr/bin/env bash
set -euo pipefail

# Static-only worker contract. It does not connect to PostgreSQL, start the
# worker, or call a supplier endpoint. Runtime queue/adapter checks remain in
# verify-external-adapter-contract.sh.
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source_file="$root/ScientificResearch/ScientificResearch/Infrastucture/ExternalSyncWorker.cs"
settings="$root/ScientificResearch/ScientificResearch/appsettings.json"

python3 - "$source_file" "$settings" <<'PY'
import json
import sys
from pathlib import Path

source_path, settings_path = map(Path, sys.argv[1:])
source = source_path.read_text(encoding='utf-8-sig')
settings = json.loads(settings_path.read_text(encoding='utf-8-sig'))

required = {
    'Idempotency-Key header': 'Idempotency-Key',
    'request id header': 'X-Request-ID',
    'retry on 408/429/5xx': 'statusCode is 408 or 429',
    'retry on 5xx': 'statusCode >= 500',
    'retry limit handling': 'work.尝试次数 >= work.最大重试次数',
    'stale lease recovery': 'RequeueStaleAsync',
}
missing = [label for label, marker in required.items() if marker not in source]
if missing:
    raise SystemExit(f'外部 worker 契约失败: 缺少 {", ".join(missing)}')

claim_marker = 'ORDER BY q.""创建时间"", q.""编号""'
limit_marker = 'LIMIT 1\n     FOR UPDATE SKIP LOCKED'
if claim_marker not in source or limit_marker not in source:
    raise SystemExit('外部 worker 契约失败: 队列领取必须先 LIMIT 1 再 FOR UPDATE SKIP LOCKED')
if 'FOR UPDATE SKIP LOCKED LIMIT 1' in source:
    raise SystemExit('外部 worker 契约失败: 检测到不兼容的 FOR UPDATE SKIP LOCKED LIMIT 顺序')

external_sync = settings.get('ExternalSync', {})
if external_sync.get('Enabled', True) is not False:
    raise SystemExit('外部 worker 契约失败: appsettings 默认必须关闭 ExternalSync.Enabled')
print('外部 worker 契约静态校验通过: 幂等键、请求追踪、租约回收、重试分类和 PostgreSQL 锁顺序齐全，默认关闭')
PY
