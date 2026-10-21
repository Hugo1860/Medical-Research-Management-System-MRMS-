#!/usr/bin/env python3
"""Generate JSONB-backed PostgreSQL wrappers for simple SQL Server CRUD procs."""

from __future__ import annotations

import csv
import re
from collections import defaultdict
from pathlib import Path

from generate_schema import pg_type


ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / "source-sqlserver"
OBJECTS = SOURCE / "objects.json"
OUT = ROOT / "migrations" / "005_crud_routines.sql"


def q(value: str) -> str:
    return '"' + value.replace('"', '""') + '"'


def main() -> None:
    columns: dict[str, list[tuple[str, str]]] = defaultdict(list)
    primary_keys: dict[str, list[str]] = defaultdict(list)
    for row in csv.reader((SOURCE / "columns.tsv").read_text(encoding="utf-8").splitlines(), delimiter="\t"):
        if len(row) >= 9 and not row[0].startswith("("):
            columns[row[0]].append((row[2], pg_type(row[3], int(row[4]), int(row[5]), int(row[6]))))
    for row in csv.reader((SOURCE / "primary-keys.tsv").read_text(encoding="utf-8").splitlines(), delimiter="\t"):
        if len(row) >= 6 and not row[0].startswith("("):
            primary_keys[row[0]].append(row[4])

    table_names = set(columns)
    procs = [o["name"] for o in __import__("json").loads(OBJECTS.read_text(encoding="utf-8")) if o.get("type_desc") == "SQL_STORED_PROCEDURE"]
    emitted: list[str] = []
    skipped: list[str] = []
    out = [
        "-- JSONB compatibility wrappers for simple CRUD procedures.",
        "-- Complex workflow and multi-table procedures remain in ROUTINE_MIGRATION_REPORT.md.",
        "BEGIN;",
        "",
        "CREATE OR REPLACE FUNCTION \"pg_first_json_array\"(p_payload jsonb)",
        "RETURNS jsonb LANGUAGE sql IMMUTABLE AS $$",
        "    SELECT COALESCE((SELECT value FROM jsonb_each(p_payload) WHERE jsonb_typeof(value) = 'array' LIMIT 1), '[]'::jsonb);",
        "$$;",
        "",
    ]

    for proc in procs:
        match = re.fullmatch(r"sp_(.+)_(增改|删|启禁|增)", proc)
        if not match or match.group(1) not in table_names:
            skipped.append(proc)
            continue
        table, action = match.groups()
        cols = columns[table]
        defs = ", ".join(f"{q(name)} {typ}" for name, typ in cols)
        insert_cols = ", ".join(q(name) for name, _ in cols)
        identity = next((name for row in csv.reader((SOURCE / "columns.tsv").read_text(encoding="utf-8").splitlines(), delimiter="\t")
                         if len(row) >= 9 and row[0] == table and row[8] == "1" for name in [row[2]]), None)
        select_cols = ", ".join(
            (f"CASE WHEN COALESCE(r.{q(name)}, 0) = 0 THEN nextval(pg_get_serial_sequence('{q(table)}', '{name}')) ELSE r.{q(name)} END"
             if name == identity else f"r.{q(name)}")
            for name, _ in cols
        )
        pk = primary_keys.get(table, [])
        body: list[str]
        if action in {"增改", "增"}:
            sql = [
                f"    INSERT INTO {q(table)} ({insert_cols})",
                f"    SELECT {select_cols}",
                f"    FROM jsonb_to_recordset(\"pg_first_json_array\"(p_payload)) AS r({defs})",
            ]
            if action == "增改" and pk:
                assignments = ", ".join(f"{q(name)} = EXCLUDED.{q(name)}" for name, _ in cols if name not in pk)
                if assignments:
                    sql.append(f"    ON CONFLICT ({', '.join(q(name) for name in pk)}) DO UPDATE SET {assignments}")
                else:
                    sql.append(f"    ON CONFLICT ({', '.join(q(name) for name in pk)}) DO NOTHING")
            body = sql
        elif action == "删" and pk:
            body = [
                f"    DELETE FROM {q(table)} AS t",
                f"    WHERE t.{q(pk[0])} IN (SELECT (r.{q(pk[0])}) FROM jsonb_to_recordset(\"pg_first_json_array\"(p_payload)) AS r({defs}));",
            ]
        elif action == "启禁":
            enabled = next((name for name, _ in cols if name == "是否启用"), None)
            key = pk[0] if pk else "编号"
            if not enabled or not pk:
                skipped.append(proc)
                continue
            body = [
                f"    UPDATE {q(table)} SET {q(enabled)} = COALESCE((p_payload->>'是否启用')::boolean, FALSE)",
                f"    WHERE {q(key)} = (p_payload->>'{key}')::integer;",
            ]
        else:
            skipped.append(proc)
            continue

        if body and not body[-1].rstrip().endswith(";"):
            body[-1] += ";"
        out.extend([
            f"CREATE OR REPLACE FUNCTION {q(proc)}(p_payload jsonb)",
            "RETURNS integer LANGUAGE plpgsql AS $$",
            "DECLARE affected integer;",
            "BEGIN",
            *body,
            "    GET DIAGNOSTICS affected = ROW_COUNT;",
            "    RETURN affected;",
            "END;",
            "$$;",
            "",
        ])
        emitted.append(proc)

    out.extend(["COMMIT;", ""])
    OUT.write_text("\n".join(out), encoding="utf-8")
    print(f"generated {OUT}: emitted={len(emitted)}, skipped={len(skipped)}")
    print("skipped:", ", ".join(skipped))


if __name__ == "__main__":
    main()
