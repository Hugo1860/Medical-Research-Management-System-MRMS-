-- PostgreSQL functions required by the translated views.
-- Remaining SQL Server routines are tracked in ROUTINE_MIGRATION_REPORT.md.
BEGIN;

CREATE OR REPLACE FUNCTION "fn_成果作者"(
    p_成果类型 integer,
    p_成果编号 integer,
    p_作者级别 varchar
)
RETURNS varchar
LANGUAGE sql
STABLE
AS $$
    SELECT NULLIF(
        string_agg(d."名称" || '-' || p."姓名", ',' ORDER BY a."编号"),
        ''
    )
    FROM "成果作者" AS a
    JOIN "人员" AS p ON p."编号" = a."作者编号"
    LEFT JOIN "部门" AS d ON d."编号" = p."部门编号"
    WHERE a."成果类型" = p_成果类型
      AND a."成果编号" = p_成果编号
      AND a."作者级别" = p_作者级别;
$$;

COMMIT;
