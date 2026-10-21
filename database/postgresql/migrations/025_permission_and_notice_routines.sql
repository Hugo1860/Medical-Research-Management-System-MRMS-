-- Remaining small permission and notice procedures used by the admin portal.
BEGIN;

CREATE OR REPLACE FUNCTION "sp_角色中的人员_增"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO "人员角色" ("角色编号", "人员编号")
    SELECT (p_payload->>'角色编号')::integer, (item->>'编号')::integer
    FROM jsonb_array_elements("pg_first_json_array"(p_payload)) AS item
    ON CONFLICT DO NOTHING;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_角色中的人员_删"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    DELETE FROM "人员角色" r
    WHERE r."角色编号" = (p_payload->>'角色编号')::integer
      AND r."人员编号" IN (
          SELECT (item->>'编号')::integer
          FROM jsonb_array_elements("pg_first_json_array"(p_payload)) AS item
      );
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_角色的权限_增改删"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE
    role_id integer := (p_payload->>'角色编号')::integer;
    affected integer := 0;
BEGIN
    DELETE FROM "角色权限" rp
    WHERE rp."角色编号" = role_id
      AND NOT EXISTS (
          SELECT 1
          FROM jsonb_array_elements("pg_first_json_array"(p_payload)) AS item
          WHERE (item->>'编号')::integer = rp."权限编号"
      );
    INSERT INTO "角色权限" ("角色编号", "权限编号")
    SELECT role_id, (item->>'编号')::integer
    FROM jsonb_array_elements("pg_first_json_array"(p_payload)) AS item
    ON CONFLICT DO NOTHING;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_角色的权限_删"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    DELETE FROM "角色权限" rp
    WHERE rp."角色编号" = (p_payload->>'角色编号')::integer
      AND rp."权限编号" IN (
          SELECT (item->>'编号')::integer
          FROM jsonb_array_elements("pg_first_json_array"(p_payload)) AS item
      );
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_通知公告_接收"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE
    notice_id integer := (p_payload->>'通知公告编号')::integer;
    receiver_id integer := (p_payload->>'接收人编号')::integer;
    affected integer;
BEGIN
    INSERT INTO "通知公告已接收人" ("通知公告编号", "接收人编号")
    SELECT notice_id, receiver_id
    WHERE NOT EXISTS (
        SELECT 1 FROM "通知公告已接收人"
        WHERE "通知公告编号" = notice_id AND "接收人编号" = receiver_id
    );
    GET DIAGNOSTICS affected = ROW_COUNT;
    IF affected = 1 THEN
        UPDATE "通知公告"
        SET "已接收人数" = "已接收人数" + 1
        WHERE "编号" = notice_id;
    END IF;
    RETURN affected;
END;
$$;

COMMIT;
