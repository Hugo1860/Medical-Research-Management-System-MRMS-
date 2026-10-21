-- Small dictionary and budget-template routines still referenced by admin pages.
BEGIN;

CREATE OR REPLACE FUNCTION "sp_学科_删"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    DELETE FROM "学科" s
    WHERE s."编号" IN (
        SELECT (item->>'编号')::integer
        FROM jsonb_array_elements("pg_first_json_array"(p_payload)) AS item
    );
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_经费模版项目支出类型_增改_作废"(p_payload jsonb)
RETURNS integer LANGUAGE sql AS $$
    SELECT "sp_经费模版项目支出类型_增改"(p_payload);
$$;

COMMIT;
