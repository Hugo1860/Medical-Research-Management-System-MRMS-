-- Query functions needed by the workflow inbox and detail pages.
BEGIN;

CREATE OR REPLACE FUNCTION "fn_某人是否可以操作某步骤"(p_step_id integer, p_person_id integer)
RETURNS boolean LANGUAGE plpgsql STABLE AS $$
BEGIN
    IF EXISTS (SELECT 1 FROM "步骤" s WHERE s."编号" = p_step_id AND s."状态" <> 0) THEN
        RETURN FALSE;
    END IF;
    IF EXISTS (SELECT 1 FROM "步骤指定人" s WHERE s."步骤编号" = p_step_id) THEN
        RETURN EXISTS (SELECT 1 FROM "步骤指定人" s WHERE s."步骤编号" = p_step_id AND s."指定人编号" = p_person_id);
    END IF;
    IF EXISTS (
        SELECT 1 FROM "步骤" a
        JOIN "步骤角色" b ON a."步骤模板编号" = b."步骤模板编号"
        JOIN "人员角色" c ON c."角色编号" = b."角色编号"
        WHERE a."编号" = p_step_id AND c."人员编号" = p_person_id
    ) THEN RETURN TRUE; END IF;
    RETURN EXISTS (
        SELECT 1 FROM "步骤" a
        JOIN "流程" f ON f."编号" = a."流程编号" AND f."发起人编号" = p_person_id
        JOIN "步骤角色" b ON b."步骤模板编号" = a."步骤模板编号" AND b."角色编号" = 0
        WHERE a."编号" = p_step_id
    );
END;
$$;

CREATE OR REPLACE FUNCTION "tfn_流程"(p_template_id integer, p_project_id integer, p_person_id integer)
RETURNS TABLE (
    "流程编号" integer, "流程模板编号" integer, "流程名称" varchar(100), "流程相关项目路径" varchar(100),
    "项目编号" integer, "发起人编号" integer, "发起人工号" varchar(50), "发起人姓名" varchar(50),
    "发起人部门名称" varchar(50), "状态" integer, "状态名称" text, "流程创建时间" timestamp,
    "步骤编号" integer, "步骤名称" varchar(100), "步骤状态" integer, "步骤链接名称" varchar(100),
    "步骤链接路径" varchar(100), "步骤链接样式" varchar(100), "步骤操作人编号" integer,
    "步骤操作人工号" varchar(50), "步骤操作人姓名" varchar(50), "步骤操作人部门名称" varchar(50),
    "步骤执行时间" timestamp, "步骤状态说明" varchar(100), "是否可处理" boolean
)
LANGUAGE sql STABLE AS $$
    SELECT v."流程编号", v."流程模板编号", v."名称", v."路径", v."项目编号", v."发起人编号", v."工号", v."姓名", v."部门名称",
           v."状态", v."状态名称", v."创建时间", v."步骤编号", v."步骤名称", v."步骤状态", v."步骤链接名称",
           v."步骤链接路径", v."步骤链接样式", v."步骤操作人编号", v."步骤操作人工号", v."步骤操作人姓名",
           v."步骤操作人部门名称", v."步骤执行时间", v."步骤状态说明",
           "fn_某人是否可以操作某步骤"(v."步骤编号", p_person_id)
    FROM "v3_流程" AS v
    WHERE v."流程模板编号" = COALESCE(p_template_id, v."流程模板编号")
      AND v."项目编号" = COALESCE(p_project_id, v."项目编号")
$$;

COMMIT;
