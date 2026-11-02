#!/usr/bin/env bash
set -euo pipefail

# Emit the repository's current schema version as one machine-readable integer.
# Keep this check read-only and fail closed when appsettings and migration files
# drift, so release gates cannot silently validate an obsolete version.
root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
python3 - "$root" <<'PY'
import json
import re
import sys
from pathlib import Path

root = Path(sys.argv[1])
migration_dir = root / "database" / "postgresql" / "migrations"
settings_path = root / "ScientificResearch" / "ScientificResearch" / "appsettings.json"
files = sorted(migration_dir.glob("[0-9][0-9][0-9]_*.sql"))
if not files:
    raise SystemExit("迁移目录为空")
numbers = [int(path.name[:3]) for path in files]
expected_sequence = list(range(1, numbers[-1] + 1))
if numbers != expected_sequence:
    raise SystemExit(f"迁移序列不连续: 期望 001-{numbers[-1]:03d}，实际末尾 {numbers[-20:]}")
try:
    settings = json.loads(settings_path.read_text(encoding="utf-8-sig"))
    configured = settings["Postgres"]["ExpectedSchemaVersion"]
    configured = int(configured)
except (OSError, KeyError, TypeError, ValueError, json.JSONDecodeError) as exc:
    raise SystemExit(f"无法读取 Postgres.ExpectedSchemaVersion: {exc}")
latest = numbers[-1]
if configured != latest:
    raise SystemExit(f"appsettings ExpectedSchemaVersion={configured} 与迁移最新版本={latest} 不一致")
print(latest)
PY
