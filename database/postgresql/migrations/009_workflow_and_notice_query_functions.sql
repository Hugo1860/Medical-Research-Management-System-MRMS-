-- Additional simple query functions used by workflow, notice and download pages.
BEGIN;

CREATE OR REPLACE FUNCTION "tfn_人员的权限"(p_employee_no varchar(50))
RETURNS SETOF "权限"
LANGUAGE sql
STABLE
AS $$
    SELECT DISTINCT c.*
    FROM "人员" AS a1
    JOIN "人员角色" AS a ON a."人员编号" = a1."编号"
    JOIN "角色权限" AS b ON b."角色编号" = a."角色编号"
    JOIN "权限" AS c ON c."编号" = b."权限编号"
    WHERE a1."工号" = p_employee_no;
$$;

CREATE OR REPLACE FUNCTION "tfn_步骤"(p_template_id integer, p_project_id integer)
RETURNS SETOF "v2_步骤"
LANGUAGE sql
STABLE
AS $$
    SELECT b.*
    FROM "流程" AS a
    JOIN "v2_步骤" AS b ON b."流程编号" = a."编号"
    WHERE a."流程模板编号" = p_template_id
      AND a."项目编号" = p_project_id;
$$;

CREATE OR REPLACE FUNCTION "tfn_步骤_可用操作"(p_step_id integer)
RETURNS TABLE (
    "编号" integer,
    "状态值" integer,
    "操作说明" varchar,
    "操作提示" varchar
)
LANGUAGE sql
STABLE
AS $$
    SELECT c."编号", c."状态值", c."操作说明", c."操作提示"
    FROM "步骤" AS a
    JOIN "步骤模板" AS b ON b."编号" = a."步骤模板编号"
    JOIN "步骤类型状态" AS c ON c."步骤类型编号" = b."步骤类型编号"
    WHERE a."编号" = p_step_id
      AND c."状态值" <> 0;
$$;

CREATE OR REPLACE FUNCTION "tfn_某人员可查看的下载中心"(p_employee_id integer)
RETURNS SETOF "v2_下载中心"
LANGUAGE sql
STABLE
AS $$
    SELECT a.*
    FROM "v2_下载中心" AS a
    WHERE a."上传人编号" = p_employee_id
       OR a."是否启用" = TRUE;
$$;

CREATE OR REPLACE FUNCTION "tfn_某人员可查看的通知公告"(p_employee_id integer)
RETURNS TABLE (
    "编号" integer,
    "通知类型" varchar,
    "通知标识" varchar,
    "发送人编号" integer,
    "通知名称" varchar,
    "关闭时间" timestamp,
    "相关文件路径" varchar,
    "接收人数" integer,
    "已接收人数" integer,
    "是否启用" boolean,
    "建立时间" timestamp,
    "备注" varchar,
    "发送人工号" varchar,
    "发送人姓名" varchar,
    "发送人部门编号" integer,
    "发送人部门名称" varchar,
    "是否已接收" integer
)
LANGUAGE sql
STABLE
AS $$
    WITH recipients("接收者类型", "接收者编号") AS (
        VALUES
            (0, 0),
            (10, p_employee_id),
            (20, (SELECT "部门编号" FROM "人员" WHERE "编号" = p_employee_id))
        UNION ALL
        SELECT 30, "角色编号"
        FROM "人员角色"
        WHERE "人员编号" = p_employee_id
    )
    SELECT a."编号", a."通知类型", a."通知标识", a."发送人编号", a."通知名称",
           a."关闭时间", a."相关文件路径", a."接收人数", a."已接收人数", a."是否启用",
           a."建立时间", a."备注", a."发送人工号", a."发送人姓名", a."发送人部门编号",
           a."发送人部门名称",
           CASE WHEN EXISTS (
               SELECT 1 FROM "通知公告已接收人" AS r
               WHERE r."通知公告编号" = a."编号"
                 AND r."接收人编号" = p_employee_id
           ) THEN 1 ELSE 0 END
    FROM "v2_通知公告" AS a
    WHERE EXISTS (
        SELECT 1
        FROM recipients AS r
        JOIN "通知公告接收条件" AS c
          ON c."接收者类型" = r."接收者类型"
         AND c."接收者编号" = r."接收者编号"
         AND c."通知公告编号" = a."编号"
    );
$$;

COMMIT;
