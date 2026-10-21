-- P0: normalize reimbursement workflow rows to their owning project scope.
BEGIN;

CREATE OR REPLACE FUNCTION "tfn_预算异常对账"(
    p_flow_template_id integer DEFAULT NULL,
    p_project_id integer DEFAULT NULL
)
RETURNS TABLE (
    "流程模板编号" integer,
    "流程名称" varchar,
    "项目编号" integer,
    "批准预算" numeric,
    "到账金额" numeric,
    "已审核报销" numeric,
    "待审核报销" numeric,
    "预算余额" numeric,
    "到账余额" numeric,
    "异常类型" text,
    "异常说明" text
)
LANGUAGE sql STABLE AS $$
WITH budgets AS (
    SELECT b."流程模板编号", b."项目编号", SUM(COALESCE(b."批准经费", 0)) AS approved_budget
    FROM "项目经费预算" b
    WHERE b."流程模板编号" = COALESCE(p_flow_template_id, b."流程模板编号")
      AND b."项目编号" = COALESCE(p_project_id, b."项目编号")
    GROUP BY b."流程模板编号", b."项目编号"
), arrivals AS (
    SELECT a."流程模板编号", a."项目编号", SUM(COALESCE(a."到账金额", 0)) AS arrival_amount
    FROM "项目经费到账" a
    WHERE a."流程模板编号" = COALESCE(p_flow_template_id, a."流程模板编号")
      AND a."项目编号" = COALESCE(p_project_id, a."项目编号")
    GROUP BY a."流程模板编号", a."项目编号"
), reimbursement_scope AS (
    SELECT r."编号" AS reimbursement_id,
           r."流程模板编号" AS source_template_id,
           CASE
             WHEN r."流程模板编号" = 600 AND EXISTS (
                 SELECT 1 FROM "纵向项目立项" y WHERE y."编号" = r."项目编号"
             ) THEN 410
             WHEN r."流程模板编号" = 600 AND EXISTS (
                 SELECT 1 FROM "横向项目" x WHERE x."编号" = r."项目编号"
             ) THEN 450
             WHEN r."流程模板编号" = 600 AND EXISTS (
                 SELECT 1 FROM "参加会议" m WHERE m."编号" = r."项目编号"
             ) THEN 210
             ELSE r."流程模板编号"
           END AS project_template_id,
           r."项目编号" AS project_id,
           r."报销金额"
    FROM "报销" r
), reimbursements AS (
    SELECT s.project_template_id AS "流程模板编号", s.project_id AS "项目编号",
           SUM(CASE WHEN f."状态" = 1 THEN COALESCE(s."报销金额", 0) ELSE 0 END) AS approved_expense,
           SUM(CASE WHEN COALESCE(f."状态", 0) = 0 THEN COALESCE(s."报销金额", 0) ELSE 0 END) AS pending_expense
    FROM reimbursement_scope s
    LEFT JOIN LATERAL (
        SELECT f1."状态"
        FROM "流程" f1
        WHERE f1."流程模板编号" = 600
          AND f1."项目编号" = s.reimbursement_id
        ORDER BY f1."编号" DESC
        LIMIT 1
    ) f ON TRUE
    WHERE (p_flow_template_id IS NULL
           OR s.project_template_id = p_flow_template_id
           OR s.source_template_id = p_flow_template_id)
      AND s.project_id = COALESCE(p_project_id, s.project_id)
    GROUP BY s.project_template_id, s.project_id
), reconciled AS (
    SELECT COALESCE(b."流程模板编号", a."流程模板编号", r."流程模板编号") AS template_id,
           COALESCE(b."项目编号", a."项目编号", r."项目编号") AS project_id,
           COALESCE(b.approved_budget, 0)::numeric AS approved_budget,
           COALESCE(a.arrival_amount, 0)::numeric AS arrival_amount,
           COALESCE(r.approved_expense, 0)::numeric AS approved_expense,
           COALESCE(r.pending_expense, 0)::numeric AS pending_expense
    FROM budgets b
    FULL JOIN arrivals a ON a."流程模板编号" = b."流程模板编号" AND a."项目编号" = b."项目编号"
    FULL JOIN reimbursements r ON r."流程模板编号" = COALESCE(b."流程模板编号", a."流程模板编号")
                              AND r."项目编号" = COALESCE(b."项目编号", a."项目编号")
), marked AS (
    SELECT x.*, t."名称"::varchar AS template_name,
       concat_ws('；',
           CASE WHEN x.approved_budget = 0 AND (x.arrival_amount > 0 OR x.approved_expense > 0 OR x.pending_expense > 0) THEN '预算未配置' END,
           CASE WHEN x.approved_budget > 0 AND x.approved_expense > x.approved_budget THEN '已审核报销超预算' END,
           CASE WHEN x.approved_budget > 0 AND x.arrival_amount > x.approved_budget THEN '到账超批准预算' END,
           CASE WHEN x.arrival_amount > 0 AND x.approved_expense > x.arrival_amount THEN '已审核报销超到账' END,
           CASE WHEN x.pending_expense > 0 THEN '存在待审核报销' END
       ) AS anomaly_type,
       concat_ws('；',
           CASE WHEN x.approved_budget = 0 AND (x.arrival_amount > 0 OR x.approved_expense > 0 OR x.pending_expense > 0) THEN '存在到账或报销记录，但项目经费预算没有配置' END,
           CASE WHEN x.approved_budget > 0 AND x.approved_expense > x.approved_budget THEN '已审核报销金额超过批准经费' END,
           CASE WHEN x.approved_budget > 0 AND x.arrival_amount > x.approved_budget THEN '累计到账金额超过批准经费' END,
           CASE WHEN x.arrival_amount > 0 AND x.approved_expense > x.arrival_amount THEN '已审核报销金额超过累计到账金额' END,
           CASE WHEN x.pending_expense > 0 THEN '仍有报销流程未完成审核' END
       ) AS anomaly_description
    FROM reconciled x
    LEFT JOIN "流程模板" t ON t."编号" = x.template_id
)
SELECT m.template_id, m.template_name, m.project_id,
       m.approved_budget, m.arrival_amount, m.approved_expense, m.pending_expense,
       (m.approved_budget - m.approved_expense)::numeric,
       (m.arrival_amount - m.approved_expense)::numeric,
       m.anomaly_type, m.anomaly_description
FROM marked m
WHERE m.anomaly_type <> ''
ORDER BY m.template_id, m.project_id;
$$;

UPDATE "系统迁移版本"
   SET "版本" = 41,
       "脚本" = '001-041',
       "检查摘要" = '预算异常对账按项目归一化报销流程范围，避免 600 报销模板与 410/450 项目预算拆行'
 WHERE "编号" = 1;

COMMIT;
