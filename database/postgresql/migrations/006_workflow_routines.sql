-- PostgreSQL implementations for the workflow routines that are used by the
-- application at runtime.  They accept the JSONB payload used by Npgsql.
BEGIN;

CREATE OR REPLACE FUNCTION "sp_流程_发起"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE
    p record;
    workflow_id integer;
    first_step_id integer;
BEGIN
    SELECT * INTO p FROM jsonb_to_record(p_payload) AS x("流程模板编号" integer, "项目编号" integer, "发起人编号" integer);
    INSERT INTO "流程" ("流程模板编号", "项目编号", "发起人编号", "状态", "创建时间", "备注")
    VALUES (p."流程模板编号", p."项目编号", p."发起人编号", 0, CURRENT_TIMESTAMP, '')
    RETURNING "编号" INTO workflow_id;
    INSERT INTO "步骤" ("步骤模板编号", "流程编号", "状态", "建立时间")
    SELECT "编号", workflow_id, 0, CURRENT_TIMESTAMP
    FROM "步骤模板"
    WHERE "流程模板编号" = p."流程模板编号" AND "上级编号" = 0
    ORDER BY "编号"
    LIMIT 1
    RETURNING "编号" INTO first_step_id;
    RETURN COALESCE(first_step_id, 0);
END;
$$;

CREATE OR REPLACE FUNCTION "sp_流程_完成步骤"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE
    p record;
    step_template_id integer;
    workflow_id integer;
    next_step_id integer := 0;
    next_template_id integer;
BEGIN
    SELECT * INTO p FROM jsonb_to_record(p_payload) AS x("步骤编号" integer, "状态" integer, "操作人" integer, "备注" varchar(1000));
    IF NOT EXISTS (SELECT 1 FROM "步骤" WHERE "编号" = p."步骤编号" AND "状态" = 0) THEN
        RAISE EXCEPTION '此步骤不存在或已完成';
    END IF;
    IF p."状态" = 0 THEN
        RAISE EXCEPTION '操作状态错误';
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM "步骤" a
        JOIN "步骤模板" b ON a."步骤模板编号" = b."编号"
        JOIN "步骤类型状态" c ON b."步骤类型编号" = c."步骤类型编号"
        WHERE a."编号" = p."步骤编号" AND c."状态值" = p."状态"
    ) THEN
        RAISE EXCEPTION '该步骤不存在指定的操作';
    END IF;
    SELECT "步骤模板编号", "流程编号" INTO step_template_id, workflow_id
    FROM "步骤" WHERE "编号" = p."步骤编号";
    UPDATE "步骤"
    SET "状态" = p."状态", "操作人编号" = p."操作人", "执行时间" = CURRENT_TIMESTAMP, "备注" = p."备注"
    WHERE "编号" = p."步骤编号";
    IF p."状态" = -1 THEN
        INSERT INTO "步骤" ("步骤模板编号", "流程编号", "状态", "建立时间")
        SELECT c."编号", workflow_id, 0, CURRENT_TIMESTAMP
        FROM "流程" b JOIN "步骤模板" c ON c."流程模板编号" = b."流程模板编号"
        WHERE b."编号" = workflow_id AND c."上级编号" = 0
        ORDER BY c."编号" LIMIT 1
        RETURNING "编号" INTO next_step_id;
    ELSIF p."状态" = -2 THEN
        UPDATE "流程" SET "状态" = -2 WHERE "编号" = workflow_id;
    ELSIF p."状态" = 1 THEN
        SELECT "编号" INTO next_template_id FROM "步骤模板"
        WHERE "上级编号" = step_template_id ORDER BY "编号" LIMIT 1;
        IF next_template_id IS NULL THEN
            UPDATE "流程" SET "状态" = 1 WHERE "编号" = workflow_id;
        ELSE
            INSERT INTO "步骤" ("步骤模板编号", "流程编号", "状态", "建立时间")
            VALUES (next_template_id, workflow_id, 0, CURRENT_TIMESTAMP)
            RETURNING "编号" INTO next_step_id;
        END IF;
    END IF;
    RETURN COALESCE(next_step_id, 0);
END;
$$;

CREATE OR REPLACE FUNCTION "sp_人员_修改密码"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE
    p record;
    affected integer;
BEGIN
    SELECT * INTO p FROM jsonb_to_record(p_payload) AS x("工号" varchar(50), "旧密码" varchar(50), "新密码" varchar(50));
    UPDATE "人员" SET "密码" = p."新密码" WHERE "工号" = p."工号" AND "密码" = p."旧密码";
    GET DIAGNOSTICS affected = ROW_COUNT;
    IF affected = 0 THEN RAISE EXCEPTION '旧密码错误'; END IF;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_纵向项目申报_设为已立项"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    UPDATE "纵向项目申报" SET "是否立项" = TRUE
    WHERE "编号" = (p_payload->>'项目申报编号')::integer;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

CREATE OR REPLACE FUNCTION "sp_角色中的人员_增改删"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE
    role_id integer := (p_payload->>'角色编号')::integer;
    affected integer := 0;
BEGIN
    DELETE FROM "人员角色" WHERE "角色编号" = role_id
      AND "人员编号" NOT IN (
        SELECT (value->>'编号')::integer FROM jsonb_array_elements("pg_first_json_array"(p_payload))
      );
    INSERT INTO "人员角色" ("角色编号", "人员编号")
    SELECT role_id, (value->>'编号')::integer
    FROM jsonb_array_elements("pg_first_json_array"(p_payload))
    ON CONFLICT DO NOTHING;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END;
$$;

COMMIT;
