-- 工作台缺件提示：服务端按当前待办项目计算缺件，供首页和移动端直接展示。
BEGIN;

DROP FUNCTION IF EXISTS "tfn_科研工作台摘要_v2"(integer);

CREATE FUNCTION "tfn_科研工作台摘要_v2"(p_person_id integer)
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
    "下一步状态说明" varchar,
    "缺件提示" text,
    "缺件数" integer
)
LANGUAGE sql STABLE AS $$
WITH pending AS (
    SELECT v."流程编号", v."流程模板编号", v."名称" AS "流程名称", v."项目编号",
           v."创建时间" AS "流程创建时间", v."步骤编号", v."步骤名称",
           v."步骤链接路径", v."步骤状态说明",
           assignees."办理人", roles."办理角色",
           CASE
             WHEN v."流程模板编号"=400 THEN concat_ws('、',
               CASE WHEN COALESCE(btrim(y."项目摘要"),'')='' THEN '项目摘要' END,
               CASE WHEN COALESCE(btrim(y."项目申报书路径"),'')='' THEN '项目申报书' END,
               CASE WHEN y."是否需要伦理批文"=TRUE AND (COALESCE(btrim(y."伦理批文编号"),'')='' OR COALESCE(btrim(y."伦理批文路径"),'')='') THEN '伦理批文' END,
               CASE WHEN y."是否委托外单位进行实验或实验分析"=TRUE AND COALESCE(btrim(y."实验委托检测内容"),'')='' THEN '外委检测内容' END)
             WHEN v."流程模板编号"=410 THEN concat_ws('、',
               CASE WHEN COALESCE(btrim(l."立项文件路径"),'')='' THEN '立项文件' END,
               CASE WHEN COALESCE(btrim(l."经费卡号"),'')='' THEN '经费卡号' END)
             WHEN v."流程模板编号"=450 THEN concat_ws('、',
               CASE WHEN COALESCE(btrim(h."合同文件路径"),'')='' THEN '合同文件' END,
               CASE WHEN COALESCE(btrim(h."经费本编号"),'')='' THEN '经费本编号' END)
             ELSE NULL
           END AS "缺件提示",
           CASE
             WHEN v."流程模板编号"=400 THEN
               (CASE WHEN COALESCE(btrim(y."项目摘要"),'')='' THEN 1 ELSE 0 END)+
               (CASE WHEN COALESCE(btrim(y."项目申报书路径"),'')='' THEN 1 ELSE 0 END)+
               (CASE WHEN y."是否需要伦理批文"=TRUE AND (COALESCE(btrim(y."伦理批文编号"),'')='' OR COALESCE(btrim(y."伦理批文路径"),'')='') THEN 1 ELSE 0 END)+
               (CASE WHEN y."是否委托外单位进行实验或实验分析"=TRUE AND COALESCE(btrim(y."实验委托检测内容"),'')='' THEN 1 ELSE 0 END)
             WHEN v."流程模板编号"=410 THEN
               (CASE WHEN COALESCE(btrim(l."立项文件路径"),'')='' THEN 1 ELSE 0 END)+
               (CASE WHEN COALESCE(btrim(l."经费卡号"),'')='' THEN 1 ELSE 0 END)
             WHEN v."流程模板编号"=450 THEN
               (CASE WHEN COALESCE(btrim(h."合同文件路径"),'')='' THEN 1 ELSE 0 END)+
               (CASE WHEN COALESCE(btrim(h."经费本编号"),'')='' THEN 1 ELSE 0 END)
             ELSE 0
           END AS "缺件数"
      FROM "v3_流程" AS v
      LEFT JOIN "纵向项目申报" y ON v."流程模板编号"=400 AND y."编号"=v."项目编号"
      LEFT JOIN "纵向项目立项" l ON v."流程模板编号"=410 AND l."编号"=v."项目编号"
      LEFT JOIN "横向项目" h ON v."流程模板编号"=450 AND h."编号"=v."项目编号"
      LEFT JOIN LATERAL (
          SELECT string_agg(DISTINCT p."姓名", '、' ORDER BY p."姓名") AS "办理人"
            FROM "步骤指定人" AS sa JOIN "人员" AS p ON p."编号" = sa."指定人编号"
           WHERE sa."步骤编号" = v."步骤编号"
      ) AS assignees ON TRUE
      LEFT JOIN LATERAL (
          SELECT string_agg(DISTINCT CASE WHEN sr."角色编号" = 0 THEN '流程发起人' ELSE r."名称" END,
                            '、' ORDER BY CASE WHEN sr."角色编号" = 0 THEN '流程发起人' ELSE r."名称" END) AS "办理角色"
            FROM "步骤角色" AS sr LEFT JOIN "角色" AS r ON r."编号" = sr."角色编号"
           WHERE sr."步骤模板编号" = (SELECT s."步骤模板编号" FROM "步骤" AS s WHERE s."编号" = v."步骤编号")
      ) AS roles ON TRUE
     WHERE v."步骤编号" IS NOT NULL AND COALESCE(v."步骤状态", 0) = 0
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
               SELECT 1 FROM "通知公告接收条件" AS c
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
    SELECT p.* FROM pending p ORDER BY p."流程创建时间" NULLS LAST,p."流程编号" LIMIT 1
), person_roles AS (
    SELECT string_agg(DISTINCT r."名称", '、' ORDER BY r."名称") AS "角色名称" FROM "人员角色" pr JOIN "角色" r ON r."编号"=pr."角色编号" WHERE pr."人员编号"=p_person_id
)
SELECT
 (SELECT count(*)::integer FROM pending),
 (SELECT count(*)::integer FROM visible_notice),
 (SELECT count(*)::integer FROM "流程" f WHERE f."发起人编号"=p_person_id AND f."状态"=0),
 (SELECT n."步骤名称" FROM next_item n),(SELECT n."步骤链接路径" FROM next_item n),(SELECT n."项目编号" FROM next_item n),(SELECT n."流程创建时间" FROM next_item n),
 (SELECT "角色名称" FROM person_roles),(SELECT COALESCE(n."办理人",n."办理角色") FROM next_item n),(SELECT n."办理角色" FROM next_item n),
 (SELECT CASE WHEN n."步骤名称" IS NULL THEN '当前没有待办事项' WHEN n."缺件数">0 THEN '缺少：'||n."缺件提示" WHEN n."步骤状态说明" IS NULL THEN '请打开事项查看办理要求' ELSE '当前状态：'||n."步骤状态说明" END FROM next_item n),
 (SELECT n."流程名称" FROM next_item n),(SELECT n."步骤状态说明" FROM next_item n),(SELECT n."缺件提示" FROM next_item n),(SELECT n."缺件数" FROM next_item n);
$$;

UPDATE "系统迁移版本" SET "版本"=65,"脚本"='001-065',"检查摘要"='为科研首页和移动工作台增加服务端缺件提示与缺件数量' WHERE "编号"=1;
COMMIT;
