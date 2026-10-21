#!/usr/bin/env bash
set -euo pipefail

# Read-only gate for the checked-in publish directory. It catches stale
# frontend/config artifacts before an offline Docker image is built.
script_dir="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
project_root="$(cd "$script_dir/.." && pwd)"

python3 - "$project_root" <<'PY'
import json
import sys
from pathlib import Path

root = Path(sys.argv[1])
source_wwwroot = root / "ScientificResearch" / "ScientificResearch" / "wwwroot"
publish_wwwroot = root / "publish" / "linux-x64" / "wwwroot"
frontend_dist = root / "frontend" / "dist"
source_config_path = root / "ScientificResearch" / "ScientificResearch" / "appsettings.json"
publish_config_path = root / "publish" / "linux-x64" / "appsettings.json"
publish_root = root / "publish" / "linux-x64"

for path in (source_wwwroot, publish_wwwroot, frontend_dist, source_config_path, publish_config_path):
    if not path.exists():
        raise SystemExit(f"发布一致性校验失败: 缺少 {path}")

source_config = json.load(source_config_path.open(encoding="utf-8-sig"))
publish_config = json.load(publish_config_path.open(encoding="utf-8-sig"))
if source_config != publish_config:
    raise SystemExit("发布一致性校验失败: appsettings.json 与源码不一致")

required_runtime_files = (
    "ScientificResearch.dll",
    "ScientificResearch.deps.json",
    "ScientificResearch.runtimeconfig.json",
    "appsettings.json",
)
for name in required_runtime_files:
    path = publish_root / name
    if not path.is_file() or path.stat().st_size == 0:
        raise SystemExit(f"发布一致性校验失败: 运行文件缺失或为空: {path}")
try:
    runtime_config = json.load((publish_root / "ScientificResearch.runtimeconfig.json").open(encoding="utf-8-sig"))
except (OSError, json.JSONDecodeError) as exc:
    raise SystemExit(f"发布一致性校验失败: runtimeconfig.json 无法解析: {exc}") from exc
if runtime_config.get("runtimeOptions", {}).get("tfm") != "net10.0":
    raise SystemExit("发布一致性校验失败: runtimeconfig.json 未声明 net10.0")

missing = []
different = []
for source_file in source_wwwroot.rglob("*"):
    if not source_file.is_file() or ".idea" in source_file.parts:
        continue
    relative = source_file.relative_to(source_wwwroot)
    # The modern SPA owns the root index.html. Keep the legacy shell under
    # index.legacy.html so container recreation cannot silently lose it.
    publish_relative = Path("index.legacy.html") if relative == Path("index.html") else relative
    publish_file = publish_wwwroot / publish_relative
    if not publish_file.exists():
        missing.append(str(relative))
    elif source_file.read_bytes() != publish_file.read_bytes():
        different.append(str(relative))
if missing or different:
    if missing:
        print("缺少发布静态文件:", ", ".join(missing), file=sys.stderr)
    if different:
        print("发布静态文件不一致:", ", ".join(different), file=sys.stderr)
    raise SystemExit("发布一致性校验失败: 静态资源漂移")

# Modern SPA files are copied to the publish wwwroot by sync-frontend.sh and
# then into the offline image by Dockerfile.published.
spa_missing = []
spa_different = []
for source_file in frontend_dist.rglob("*"):
    if not source_file.is_file():
        continue
    relative = source_file.relative_to(frontend_dist)
    publish_file = publish_wwwroot / relative
    if not publish_file.exists():
        spa_missing.append(str(relative))
    elif source_file.read_bytes() != publish_file.read_bytes():
        spa_different.append(str(relative))
if spa_missing or spa_different:
    if spa_missing:
        print("缺少发布现代 SPA 文件:", ", ".join(spa_missing), file=sys.stderr)
    if spa_different:
        print("发布现代 SPA 文件不一致:", ", ".join(spa_different), file=sys.stderr)
    raise SystemExit("发布一致性校验失败: 现代 SPA 静态资源漂移，请先运行 deploy/sync-frontend.sh")

migrations = sorted((root / "database" / "postgresql" / "migrations").glob("[0-9][0-9][0-9]_*.sql"))
numbers = [int(path.name[:3]) for path in migrations]
if not numbers or numbers != list(range(1, max(numbers) + 1)):
    raise SystemExit(f"发布一致性校验失败: PostgreSQL 迁移序列不连续: {numbers}")
expected = source_config.get("Postgres", {}).get("ExpectedSchemaVersion")
if expected != max(numbers):
    raise SystemExit(
        f"发布一致性校验失败: ExpectedSchemaVersion={expected}，迁移最新版本={max(numbers)}"
    )

legacy_count = len([p for p in source_wwwroot.rglob('*') if p.is_file() and '.idea' not in p.parts])
spa_count = len([p for p in frontend_dist.rglob('*') if p.is_file()])
print(f"发布一致性校验通过: 配置、{legacy_count + spa_count} 个静态文件（旧版 {legacy_count} + SPA {spa_count}）、迁移 001-{max(numbers):03d}")
PY
