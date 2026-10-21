-- P0: expose role-aware, actionable workbench context without widening data scope.
BEGIN;

CREATE OR REPLACE FUNCTION "tfn_科研工作台摘要_v2"(p_person_id integer)
RETURNS TABLE (
    "待办数量" integer,
    "未读通知数量" integer,
    "已发起流程数量" integer,
    "最近待办事项" varchar,
    "最近待办路径" varchar,
    "最近待办项目编号" integer,
    "最近待办创建时间" timestamp,
    "角色名称" text,
    "下一步办理人" text,
    "下一步办理角色" text,
    "待办提示" text,
    "下一步流程名称" varchar,
    "下一步状态说明" varchar
)
LANGUAGE sql STABLE AS $$
WITH pending AS (
    SELECT v."流程编号", v."流程模板编号", v."名称" AS "流程名称", v."项目编号",
           v."创建时间" AS "流程创建时间", v."步骤编号", v."步骤名称",
           v."步骤链接路径", v."步骤状态说明",
           assignees."办理人", roles."办理角色"
      FROM "v3_流程" AS v
      LEFT JOIN LATERAL (
          SELECT string_agg(DISTINCT p."姓名", '、' ORDER BY p."姓名") AS "办理人"
            FROM "步骤指定人" AS sa
            JOIN "人员" AS p ON p."编号" = sa."指定人编号"
           WHERE sa."步骤编号" = v."步骤编号"
      ) AS assignees ON TRUE
      LEFT JOIN LATERAL (
          SELECT string_agg(DISTINCT CASE WHEN sr."角色编号" = 0 THEN '流程发起人' ELSE r."名称" END,
                            '、' ORDER BY CASE WHEN sr."角色编号" = 0 THEN '流程发起人' ELSE r."名称" END) AS "办理角色"
            FROM "步骤角色" AS sr
            LEFT JOIN "角色" AS r ON r."编号" = sr."角色编号"
           WHERE sr."步骤模板编号" = (
               SELECT s."步骤模板编号" FROM "步骤" AS s WHERE s."编号" = v."步骤编号"
           )
      ) AS roles ON TRUE
     WHERE v."步骤编号" IS NOT NULL
       AND COALESCE(v."步骤状态", 0) = 0
       AND "fn_某人是否可以操作某步骤"(v."步骤编号", p_person_id)
), visible_notice AS (
    SELECT DISTINCT n."编号"
      FROM "通知公告" AS n
     WHERE n."是否启用" = TRUE
       AND (n."关闭时间" IS NULL OR n."关闭时间" > CURRENT_TIMESTAMP)
       AND NOT EXISTS (
           SELECT 1 FROM "通知公告已接收人" AS r
            WHERE r."通知公告编号" = n."编号" AND r."接收人编号" = p_person_id
       )
       AND (
           NOT EXISTS (SELECT 1 FROM "通知公告接收条件" AS c0 WHERE c0."通知公告编号" = n."编号")
           OR EXISTS (
               SELECT 1
                 FROM "通知公告接收条件" AS c
                 JOIN "人员" AS p ON p."编号" = p_person_id
                WHERE c."通知公告编号" = n."编号"
                  AND (
                      c."接收者类型" = 0
                      OR (c."接收者类型" = 10 AND c."接收者编号" = p_person_id)
                      OR (c."接收者类型" = 20 AND c."接收者编号" = p."部门编号")
                      OR (c."接收者类型" = 30 AND EXISTS (
                          SELECT 1 FROM "人员角色" AS pr
                           WHERE pr."人员编号" = p_person_id AND pr."角色编号" = c."接收者编号"
                      ))
                  )
           )
       )
), next_item AS (
    SELECT p.*
      FROM pending AS p
     ORDER BY p."流程创建时间" NULLS LAST, p."流程编号"
     LIMIT 1
), person_roles AS (
    SELECT string_agg(DISTINCT r."名称", '、' ORDER BY r."名称") AS "角色名称"
      FROM "人员角色" AS pr
      JOIN "角色" AS r ON r."编号" = pr."角色编号"
     WHERE pr."人员编号" = p_person_id
)
SELECT
    (SELECT COUNT(*)::integer FROM pending),
    (SELECT COUNT(*)::integer FROM visible_notice),
    (SELECT COUNT(*)::integer FROM "流程" AS f WHERE f."发起人编号" = p_person_id AND f."状态" = 0),
    (SELECT n."步骤名称" FROM next_item AS n),
    (SELECT n."步骤链接路径" FROM next_item AS n),
    (SELECT n."项目编号" FROM next_item AS n),
    (SELECT n."流程创建时间" FROM next_item AS n),
    (SELECT "角色名称" FROM person_roles),
    (SELECT COALESCE(n."办理人", n."办理角色") FROM next_item AS n),
    (SELECT n."办理角色" FROM next_item AS n),
    (SELECT CASE
        WHEN n."步骤名称" IS NULL THEN '当前没有待办事项'
        WHEN n."步骤状态说明" IS NULL THEN '请打开事项查看所需材料和办理要求'
        ELSE '当前状态：' || n."步骤状态说明"
     END FROM next_item AS n),
    (SELECT n."流程名称" FROM next_item AS n),
    (SELECT n."步骤状态说明" FROM next_item AS n);
$$;

UPDATE "系统迁移版本"
   SET "版本" = 43,
       "脚本" = '001-043',
       "检查摘要" = '为统一工作台增加角色、下一步责任人和办理提示，沿用原有人员与流程权限过滤'
 WHERE "编号" = 1;

COMMIT;
