#!/usr/bin/env bash
set -euo pipefail

: "${DATABASE_CONTAINER:=2018-7-24-db-1}"
: "${DATABASE_NAME:=ScientificResearch_Model}"
: "${DATABASE_USER:=postgres}"
: "${APP_BASE_URL:=}"
: "${CHECK_HTTP:=false}"
root_dir="$(cd "$(dirname "$0")/.." && pwd)"
tmp_paths="$(mktemp)"
trap 'rm -f "$tmp_paths"' EXIT

docker exec "$DATABASE_CONTAINER" psql -U "$DATABASE_USER" -d "$DATABASE_NAME" -AtF '|' -c "SELECT \"编号\",\"名称\",\"路径\" FROM \"权限\" WHERE COALESCE(\"路径\", '') <> '' ORDER BY \"编号\";" > "$tmp_paths"

python3 - "$root_dir" "$tmp_paths" "$APP_BASE_URL" "$CHECK_HTTP" <<'PY'
from pathlib import Path
from urllib.error import HTTPError, URLError
from urllib.parse import parse_qsl, quote, urlencode, urlsplit
from urllib.request import urlopen
import sys

root = Path(sys.argv[1])
rows_file = Path(sys.argv[2])
base = sys.argv[3].rstrip('/')
check_http = sys.argv[4].lower() == 'true'
views = root / 'ScientificResearch' / 'ScientificResearch' / 'wwwroot' / 'IMIS' / 'views'
files = {p.relative_to(views).as_posix() for p in views.rglob('*.html')}
missing=[]; http_fail=[]; total=0
for line in rows_file.read_text(encoding='utf-8').splitlines():
    parts=line.split('|',2)
    if len(parts)!=3: continue
    _, _, raw=parts
    parsed=urlsplit(raw)
    relative=parsed.path.lstrip('/')
    total += 1
    if relative not in files:
        missing.append(raw); continue
    if check_http:
        url=base + '/IMIS/views/' + quote(relative, safe='/')
        if parsed.query:
            url += '?' + urlencode(parse_qsl(parsed.query, keep_blank_values=True), encoding='utf-8')
        try:
            with urlopen(url, timeout=10) as response:
                if response.status != 200: http_fail.append(f'{raw} ({response.status})')
        except (HTTPError, URLError, TimeoutError) as exc:
            http_fail.append(f'{raw} ({exc})')
if missing:
    print('菜单路径校验失败：缺少 HTML 文件', file=sys.stderr)
    print('\n'.join(missing), file=sys.stderr)
    raise SystemExit(1)
if http_fail:
    print('菜单路径校验失败：HTTP 不可访问', file=sys.stderr)
    print('\n'.join(http_fail), file=sys.stderr)
    raise SystemExit(1)
print(f'菜单路径校验通过: {total} 个路径，源码 HTML 全部存在' + ('，HTTP 全部 200' if check_http else ''))
PY
