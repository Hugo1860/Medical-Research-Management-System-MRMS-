#!/usr/bin/env python3
"""Generate PostgreSQL INSERT statements from table JSON exports."""

from __future__ import annotations

import csv
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / "source-sqlserver"
DATA = SOURCE / "data"
OUT = ROOT / "migrations" / "002_data.sql"


def q(value: str) -> str:
    return '"' + value.replace('"', '""') + '"'


def literal(value: object) -> str:
    if value is None:
        return "NULL"
    if isinstance(value, bool):
        return "TRUE" if value else "FALSE"
    if isinstance(value, (int, float)):
        return str(value)
    text = str(value)
    text = (
        text.replace("\\", "\\\\")
        .replace("'", "\\'")
        .replace("\r", "\\r")
        .replace("\n", "\\n")
    )
    return "E'" + text + "'"


def main() -> None:
    tables: list[str] = []
    for line in (SOURCE / "columns.tsv").read_text(encoding="utf-8").splitlines():
        if not line or line.startswith("("):
            continue
        table = line.split("\t", 1)[0]
        if table not in tables:
            tables.append(table)

    output = [
        "-- Generated from the restored SQL Server backup.",
        "-- Run after 001_schema.sql. Explicit identity values are preserved.",
        "BEGIN;",
        "SET client_encoding = 'UTF8';",
        "",
    ]
    total_rows = 0
    for index, table in enumerate(tables, 1):
        data_path = DATA / f"{index:03d}.json"
        raw = data_path.read_text(encoding="utf-16").strip() if data_path.exists() else ""
        records = json.loads(raw) if raw else []
        if not records:
            continue
        columns = list(records[0].keys())
        output.append(f"-- {table}: {len(records)} rows")
        target = f"INSERT INTO {q(table)} (" + ", ".join(q(c) for c in columns) + ") VALUES\n"
        for start in range(0, len(records), 250):
            batch = records[start : start + 250]
            values = []
            for record in batch:
                values.append("(" + ", ".join(literal(record.get(c)) for c in columns) + ")")
            output.append(target + ",\n".join(values) + ";\n")
        total_rows += len(records)

        # Reset every identity sequence after its explicit values are loaded.
        identity = next(
            (
                row[2]
                for row in csv.reader((SOURCE / "columns.tsv").read_text(encoding="utf-8").splitlines(), delimiter="\t")
                if len(row) >= 9 and row[0] == table and row[8] == "1"
            ),
            None,
        )
        if identity:
            output.append(
                f"SELECT setval(pg_get_serial_sequence('{table.replace(chr(39), chr(39) * 2)}', "
                f"'{identity.replace(chr(39), chr(39) * 2)}'), "
                f"COALESCE((SELECT MAX({q(identity)}) FROM {q(table)}), 1), "
                f"EXISTS (SELECT 1 FROM {q(table)}));\n"
            )

    output.extend(["COMMIT;", ""])
    OUT.write_text("\n".join(output), encoding="utf-8")
    print(f"generated {OUT} ({total_rows} rows)")


if __name__ == "__main__":
    main()
