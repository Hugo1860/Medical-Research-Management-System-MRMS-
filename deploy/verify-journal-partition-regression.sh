#!/usr/bin/env bash
set -euo pipefail

# Execute the controller's production SQL against temporary copies of the real
# schema and the real matching function. No production rows or sequences change.
: "${DATABASE_CONTAINER:?Set DATABASE_CONTAINER to an isolated acceptance PostgreSQL container}"
database_name="${DATABASE_NAME:-ScientificResearch_Model}"
database_user="${DATABASE_USER:-postgres}"
root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"

python3 - "$root" <<'PY' | docker exec -i "$DATABASE_CONTAINER" psql -X -q -v ON_ERROR_STOP=1 -U "$database_user" -d "$database_name"
import re
import sys
from pathlib import Path

root = Path(sys.argv[1])
source = (root / 'ScientificResearch/ScientificResearch/Controllers/JournalPartitionController.cs').read_text()
match = re.search(r'internal const string BatchMatchSql = @"(.*?)";', source, re.S)
if not match:
    raise SystemExit('Cannot locate production BatchMatchSql')
query = match.group(1).replace('""', '"')
for parameter, replacement in [('@截止论文编号', '$3'), ('@起始论文编号', '$2'), ('@年度', '$1')]:
    query = query.replace(parameter, replacement)
template = (root / 'deploy/tests/journal-partition-regression.sql').read_text()
print(template.replace('-- PRODUCTION_BATCH_QUERY', query))
PY
