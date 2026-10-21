-- Compatibility helpers for legacy notification predicates and string splitting.
BEGIN;

CREATE OR REPLACE FUNCTION "fn_满足通知公告接收条件人数"(p_payload jsonb)
RETURNS integer LANGUAGE sql STABLE AS $$
    WITH conditions AS (
        SELECT (item->>'接收者类型')::integer AS receiver_type,
               (item->>'接收者编号')::integer AS receiver_id
        FROM jsonb_array_elements("pg_first_json_array"(p_payload)) AS item
    ), recipients AS (
        SELECT p."编号" AS person_id, 0 AS receiver_type, 0 AS receiver_id FROM "人员" p
        UNION ALL SELECT p."编号", 10, p."编号" FROM "人员" p
        UNION ALL SELECT p."编号", 20, p."部门编号" FROM "人员" p
        UNION ALL SELECT p."编号", 30, pr."角色编号"
        FROM "人员" p JOIN "人员角色" pr ON pr."人员编号" = p."编号"
    )
    SELECT count(DISTINCT recipients.person_id)::integer
    FROM recipients JOIN conditions
      ON conditions.receiver_type = recipients.receiver_type
     AND conditions.receiver_id = recipients.receiver_id;
$$;

CREATE OR REPLACE FUNCTION "fn_某人员是否满足通知公告接收条件_作废"(p_person_id integer, p_notice_id integer)
RETURNS integer LANGUAGE sql STABLE AS $$
    WITH person_conditions AS (
        SELECT 0 AS receiver_type, 0 AS receiver_id
        UNION ALL SELECT 10, p_person_id
        UNION ALL SELECT 20, p."部门编号" FROM "人员" p WHERE p."编号" = p_person_id
        UNION ALL SELECT 30, pr."角色编号" FROM "人员角色" pr WHERE pr."人员编号" = p_person_id
    )
    SELECT count(*)::integer
    FROM person_conditions pc
    JOIN "通知公告接收条件" c
      ON c."接收者类型" = pc.receiver_type AND c."接收者编号" = pc.receiver_id
    WHERE c."通知公告编号" = p_notice_id;
$$;

CREATE OR REPLACE FUNCTION "SplitStringToTable"(p_string text, p_split_char text)
RETURNS TABLE ("ID" text) LANGUAGE sql IMMUTABLE AS $$
    SELECT value
    FROM unnest(string_to_array(COALESCE(p_string, ''), COALESCE(NULLIF(p_split_char, ''), ','))) AS value
    WHERE value <> '';
$$;

COMMIT;
