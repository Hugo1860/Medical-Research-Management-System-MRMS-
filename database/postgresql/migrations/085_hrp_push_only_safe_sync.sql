-- HRP 安全同步收口。
-- 保留 069 的函数签名以兼容已有调用方，但只允许 push_only：
-- 仅把科研项目和预算快照写入 HRP 中间库的“待同步”暂存状态，
-- 不读取或写入财务到账/付款记录，不调用外部地址，也不删除历史中间库记录。
BEGIN;

CREATE OR REPLACE FUNCTION "sp_hrp中间库双向协同"(
    p_project_id integer,
    p_action varchar(50) DEFAULT 'push_only'
)
RETURNS jsonb
LANGUAGE plpgsql
AS $$
DECLARE
    project_rec record;
    budget_rec record;
    existing_project_id integer;
    existing_budget_id integer;
    staged_projects integer := 0;
    staged_budgets integer := 0;
    skipped_projects integer := 0;
BEGIN
    IF p_action IS DISTINCT FROM 'push_only' THEN
        RAISE EXCEPTION 'HRP 协同仅支持 push_only，未执行任何中间库写入（收到动作：%）', p_action
            USING ERRCODE = '22023';
    END IF;
    IF p_project_id IS NOT NULL AND p_project_id <= 0 THEN
        RAISE EXCEPTION '项目编号必须为正整数或省略' USING ERRCODE = '22023';
    END IF;
    IF p_project_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM "纵向项目立项" WHERE "编号" = p_project_id) THEN
        RAISE EXCEPTION '未找到纵向项目：%', p_project_id USING ERRCODE = '22023';
    END IF;
    IF p_project_id IS NOT NULL
       AND EXISTS (SELECT 1 FROM "纵向项目立项" WHERE "编号" = p_project_id AND "负责人编号" IS NULL) THEN
        RAISE EXCEPTION '项目缺少负责人，不能写入 HRP 中间库：%', p_project_id USING ERRCODE = '22023';
    END IF;

    -- 同一事务内串行化暂存，避免两个管理请求同时产生重复待同步快照。
    PERFORM pg_advisory_xact_lock(20180724, 73002);

    -- 只建立科研 -> HRP 的待同步快照；已有历史行保持不变。
    FOR project_rec IN
        SELECT p."编号",
               COALESCE(NULLIF(btrim(p."项目编码"), ''), 'PRJ-' || p."编号"::text) AS project_no,
               COALESCE(NULLIF(btrim(p."项目中文名称"), ''), '未命名项目-' || p."编号"::text) AS project_name,
               p."负责人编号", r."姓名" AS pi_name, r."工号" AS pi_work_no,
               p."成果归属部门编号" AS dept_id, d."名称" AS dept_name,
               COALESCE(p."批准经费", 0.00) AS approved_funds,
               COALESCE(p."配套经费", 0.00) AS matching_funds,
               p."计划开始日期" AS start_date, p."计划完成日期" AS end_date
          FROM "纵向项目立项" p
          LEFT JOIN "人员" r ON r."编号" = p."负责人编号"
          LEFT JOIN "部门" d ON d."编号" = p."成果归属部门编号"
         WHERE (p_project_id IS NULL OR p."编号" = p_project_id)
           AND p."负责人编号" IS NOT NULL
         ORDER BY p."编号"
    LOOP
        existing_project_id := NULL;
        SELECT s."id"
          INTO existing_project_id
          FROM "hrp_sync_project" s
         WHERE s."project_id" = project_rec."编号"
           AND s."sync_status" = '待同步'
         ORDER BY s."id" DESC
         LIMIT 1
         FOR UPDATE;

        IF existing_project_id IS NULL THEN
            INSERT INTO "hrp_sync_project" (
                "project_id", "project_no", "project_name", "pi_id", "pi_name", "pi_work_no",
                "dept_id", "dept_name", "approved_funds", "matching_funds", "start_date", "end_date",
                "sync_status", "sync_time", "error_message"
            ) VALUES (
                project_rec."编号", project_rec.project_no, project_rec.project_name, project_rec."负责人编号",
                project_rec.pi_name, project_rec.pi_work_no, project_rec.dept_id, project_rec.dept_name,
                project_rec.approved_funds, project_rec.matching_funds, project_rec.start_date, project_rec.end_date,
                '待同步', NULL, NULL
            );
        ELSE
            UPDATE "hrp_sync_project"
               SET "project_no" = project_rec.project_no,
                   "project_name" = project_rec.project_name,
                   "pi_id" = project_rec."负责人编号",
                   "pi_name" = project_rec.pi_name,
                   "pi_work_no" = project_rec.pi_work_no,
                   "dept_id" = project_rec.dept_id,
                   "dept_name" = project_rec.dept_name,
                   "approved_funds" = project_rec.approved_funds,
                   "matching_funds" = project_rec.matching_funds,
                   "start_date" = project_rec.start_date,
                   "end_date" = project_rec.end_date,
                   "sync_status" = '待同步',
                   "sync_time" = NULL,
                   "error_message" = NULL
             WHERE "id" = existing_project_id;
        END IF;
        staged_projects := staged_projects + 1;

        FOR budget_rec IN
            SELECT b."编号", b."项目支出类型", b."项目支出内容", b."财务科目",
                   COALESCE(b."批准经费", 0.00) AS amount, b."到账批次"
              FROM "项目经费预算" b
             WHERE b."项目编号" = project_rec."编号"
             ORDER BY b."编号"
        LOOP
            existing_budget_id := NULL;
            SELECT s."id"
              INTO existing_budget_id
              FROM "hrp_sync_budget" s
             WHERE s."project_id" = project_rec."编号"
               AND s."budget_id" IS NOT DISTINCT FROM budget_rec."编号"
               AND s."sync_status" = '待同步'
             ORDER BY s."id" DESC
             LIMIT 1
             FOR UPDATE;

            IF existing_budget_id IS NULL THEN
                INSERT INTO "hrp_sync_budget" (
                    "project_id", "budget_id", "expense_type", "expense_content", "finance_subject",
                    "amount", "batch_no", "sync_status", "sync_time"
                ) VALUES (
                    project_rec."编号", budget_rec."编号", budget_rec."项目支出类型", budget_rec."项目支出内容",
                    budget_rec."财务科目", budget_rec.amount, budget_rec."到账批次", '待同步', NULL
                );
            ELSE
                UPDATE "hrp_sync_budget"
                   SET "expense_type" = budget_rec."项目支出类型",
                       "expense_content" = budget_rec."项目支出内容",
                       "finance_subject" = budget_rec."财务科目",
                       "amount" = budget_rec.amount,
                       "batch_no" = budget_rec."到账批次",
                       "sync_status" = '待同步',
                       "sync_time" = NULL
                 WHERE "id" = existing_budget_id;
            END IF;
            staged_budgets := staged_budgets + 1;
        END LOOP;
    END LOOP;

    SELECT count(*)::integer
      INTO skipped_projects
      FROM "纵向项目立项" p
     WHERE (p_project_id IS NULL OR p."编号" = p_project_id)
       AND p."负责人编号" IS NULL;

    RETURN jsonb_build_object(
        'success', true,
        'action', 'push_only',
        'project_id', p_project_id,
        'staged_projects', staged_projects,
        'skipped_projects_without_pi', skipped_projects,
        'staged_budgets', staged_budgets,
        'pushed_projects', staged_projects,
        'pushed_budgets', staged_budgets,
        'pulled_incomings', 0,
        'pulled_expenses', 0,
        'sync_status', '待同步',
        'message', '科研项目和预算已写入 HRP 中间库待同步暂存；未执行财务回放或付款回写'
    );
END;
$$;

UPDATE "系统迁移版本"
   SET "版本" = 85,
       "脚本" = '085_hrp_push_only_safe_sync.sql',
       "检查摘要" = '收口 HRP 协同为 push_only 科研项目/预算待同步暂存；拒绝财务回放，不删除历史中间库记录',
       "应用时间" = CURRENT_TIMESTAMP
 WHERE "编号" = 1;

COMMIT;
