#!/usr/bin/env python3
"""Translate SQL Server views to PostgreSQL SQL with sqlglot.

sqlglot is a build-time dependency for this conversion step. Unsupported views
are listed in the migration report instead of being emitted as invalid SQL.
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

try:
    import sqlglot
    from sqlglot import exp
except ImportError as error:  # pragma: no cover - conversion environment only
    raise SystemExit("Install sqlglot to generate views: python -m pip install sqlglot") from error


ROOT = Path(__file__).resolve().parent
OBJECTS = ROOT / "source-sqlserver" / "objects.json"
OUT = ROOT / "migrations" / "004_views.sql"
REPORT = ROOT / "VIEW_MIGRATION_REPORT.md"


def main() -> None:
    objects = json.loads(OBJECTS.read_text(encoding="utf-8"))
    output = [
        "-- Translated from SQL Server views with sqlglot.",
        "-- Review this file against the original view definitions before production use.",
        "BEGIN;",
        "",
    ]
    report = ["# 视图迁移报告", "", "自动转换使用 SQL Server 方言解析，再输出 PostgreSQL 方言。每个视图仍需与业务查询结果做对照。", ""]
    converted = 0
    skipped = []
    converted_sql: dict[str, str] = {}
    for obj in objects:
        if obj.get("type_desc") != "VIEW":
            continue
        name = obj["name"]
        try:
            parsed = sqlglot.parse_one(obj["definition"], dialect="tsql")
            if not isinstance(parsed, exp.Create) or parsed.kind.upper() != "VIEW":
                raise ValueError(f"parsed expression is {type(parsed).__name__}")
            sql = parsed.sql(dialect="postgres", identify=True)
            sql = sql.replace('"dbo".', "").replace("dbo.", "")
            sql = sql.replace("CREATE VIEW ", "CREATE OR REPLACE VIEW ", 1)
            converted_sql[name] = sql + ";\n"
            converted += 1
        except Exception as error:
            skipped.append((name, str(error)))
            report.append(f"- `{name}`：未自动转换，原因：`{error}`")

    # SQL Server source order is arbitrary, while PostgreSQL requires a
    # referenced view to exist before its dependent view is created.
    pending = dict(converted_sql)
    while pending:
        ready = []
        for name, sql in pending.items():
            dependencies = [other for other in pending if other != name and other in sql]
            if not dependencies:
                ready.append(name)
        if not ready:
            # Keep a deterministic order for a recursive cycle; PostgreSQL will
            # report the exact cycle if one remains after manual review.
            ready = [next(iter(pending))]
        for name in ready:
            output.append(pending.pop(name))

    # The source backup contains a line-wrap artifact in this view definition
    # (",r" between two select items).  Recreate the intended projection
    # explicitly and keep the same SQL Server view contract.
    output.append(
        'CREATE OR REPLACE VIEW "v1_成果获奖_列表" AS '
        'SELECT a."编号", a."获奖名称", a."获奖日期", a."获奖级别", '
        'a."获奖类别", a."颁奖单位", a."年度", '
        '"fn_成果作者"(4, a."编号", \'第一\') AS "第一完成人" '
        'FROM "成果获奖" AS a;\n'
    )

    output.extend(["COMMIT;", ""])
    OUT.write_text("\n".join(output), encoding="utf-8")
    report.insert(3, f"已生成：{converted} 个；需人工处理：{len(skipped)} 个。")
    REPORT.write_text("\n".join(report) + "\n", encoding="utf-8")
    print(f"generated {OUT}: converted={converted}, skipped={len(skipped)}")
    if skipped:
        print("skipped:", ", ".join(name for name, _ in skipped), file=sys.stderr)


if __name__ == "__main__":
    main()
