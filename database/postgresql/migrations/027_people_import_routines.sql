-- Import helpers that normalize the Excel department name to a department id
-- before reusing the validated personnel upsert routine.
BEGIN;

CREATE OR REPLACE FUNCTION "sp_人员_导入"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE
    rows jsonb := "pg_first_json_array"(p_payload);
    normalized jsonb;
    input_count integer;
    matched_count integer;
BEGIN
    SELECT count(*) INTO input_count FROM jsonb_array_elements(rows);
    SELECT jsonb_agg(
        (item - '部门名称') || jsonb_build_object('部门编号', d."编号", '是否启用', TRUE)
    ), count(*)
    INTO normalized, matched_count
    FROM jsonb_array_elements(rows) AS source(item)
    JOIN "部门" d ON d."名称" = item->>'部门名称';
    IF COALESCE(matched_count, 0) <> input_count THEN
        RAISE EXCEPTION '导入人员包含不存在的部门名称';
    END IF;
    IF input_count = 0 THEN RETURN 0; END IF;
    RETURN "sp_人员_增改"(jsonb_build_object('tt', normalized));
END;
$$;

CREATE OR REPLACE FUNCTION "sp_角色中的人员_新增人员同时到角色"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE
    role_id integer := (p_payload->>'角色编号')::integer;
    people jsonb;
    affected integer;
BEGIN
    SELECT jsonb_agg(item || jsonb_build_object('是否启用', COALESCE(item->'是否启用', 'true'::jsonb)))
    INTO people
    FROM jsonb_array_elements("pg_first_json_array"(p_payload)) AS source(item);
    people := COALESCE(people, '[]'::jsonb);
    PERFORM "sp_人员_增改"(jsonb_build_object('tt', people));
    INSERT INTO "人员角色" ("角色编号", "人员编号")
    SELECT role_id, p."编号"
    FROM "人员" p
    JOIN jsonb_array_elements(people) AS item ON item->>'工号' = p."工号"
    ON CONFLICT DO NOTHING;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

COMMIT;
