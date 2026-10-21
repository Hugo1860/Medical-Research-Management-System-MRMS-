-- P0: role-aware workbench summary.  All counts are scoped to the caller.
BEGIN;

CREATE OR REPLACE FUNCTION "tfn_科研工作台摘要"(p_person_id integer)
RETURNS TABLE (
    "待办数量" integer,
    "未读通知数量" integer,
    "已发起流程数量" integer,
    "最近待办事项" varchar,
    "最近待办路径" varchar,
    "最近待办项目编号" integer,
    "最近待办创建时间" timestamp
)
LANGUAGE sql STABLE AS $$
WITH pending AS (
    SELECT v."流程编号", v."项目编号", v."创建时间" AS "流程创建时间", v."步骤名称", v."步骤链接路径"
    FROM "v3_流程" AS v
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
    SELECT p."步骤名称", p."步骤链接路径", p."项目编号", p."流程创建时间"
    FROM pending AS p
    ORDER BY p."流程创建时间" NULLS LAST, p."流程编号"
    LIMIT 1
)
SELECT
    (SELECT COUNT(*)::integer FROM pending),
    (SELECT COUNT(*)::integer FROM visible_notice),
    (SELECT COUNT(*)::integer FROM "流程" AS f WHERE f."发起人编号" = p_person_id AND f."状态" = 0),
    (SELECT n."步骤名称" FROM next_item AS n),
    (SELECT n."步骤链接路径" FROM next_item AS n),
    (SELECT n."项目编号" FROM next_item AS n),
    (SELECT n."流程创建时间" FROM next_item AS n);
$$;

COMMIT;
