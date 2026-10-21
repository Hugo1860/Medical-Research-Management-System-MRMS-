-- 迁移 069: 新增一键配套项目、一键结余项目、按到账金额维护预算双模式及 HRP 中间库双向协同表
BEGIN;

-- 1. 扩展纵向项目立项表：支持来源项目、项目衍生类型与自动编码
ALTER TABLE "纵向项目立项" 
    ADD COLUMN IF NOT EXISTS "来源项目编号" integer NULL,
    ADD COLUMN IF NOT EXISTS "项目衍生类型" character varying(50) DEFAULT '原始申报',
    ADD COLUMN IF NOT EXISTS "项目编码" character varying(100) NULL;

-- 2. 扩展纵向项目结题表：支持结余处理状态与结余金额记录
ALTER TABLE "纵向项目结题"
    ADD COLUMN IF NOT EXISTS "结余处理状态" character varying(50) DEFAULT '未结转',
    ADD COLUMN IF NOT EXISTS "结余金额" numeric(18,2) DEFAULT 0.00;

-- 3. 扩展项目经费预算表：支持预算管理模式（按项目/按到账）、到账关联与到账批次
ALTER TABLE "项目经费预算"
    ADD COLUMN IF NOT EXISTS "预算管理模式" integer DEFAULT 1,
    ADD COLUMN IF NOT EXISTS "到账编号" integer NULL,
    ADD COLUMN IF NOT EXISTS "到账批次" character varying(100) NULL;

-- 4. 创建医院科研与财务 HRP 标准中间库协同表 (Intermediary Tables)
-- 4.1 项目与人员基本信息中间表 (科研 -> HRP)
CREATE TABLE IF NOT EXISTS "hrp_sync_project" (
    "id" SERIAL PRIMARY KEY,
    "project_id" integer NOT NULL,
    "project_no" character varying(100),
    "project_name" character varying(200) NOT NULL,
    "pi_id" integer NOT NULL,
    "pi_name" character varying(100),
    "pi_work_no" character varying(50),
    "dept_id" integer,
    "dept_name" character varying(100),
    "approved_funds" numeric(18,2) DEFAULT 0.00,
    "matching_funds" numeric(18,2) DEFAULT 0.00,
    "start_date" date,
    "end_date" date,
    "sync_status" character varying(50) DEFAULT '待同步',
    "sync_time" timestamp without time zone,
    "error_message" text,
    "created_at" timestamp without time zone DEFAULT now()
);

-- 4.2 项目预算科目中间表 (科研 -> HRP)
CREATE TABLE IF NOT EXISTS "hrp_sync_budget" (
    "id" SERIAL PRIMARY KEY,
    "project_id" integer NOT NULL,
    "budget_id" integer,
    "expense_type" character varying(100),
    "expense_content" character varying(100),
    "finance_subject" character varying(100),
    "amount" numeric(18,2) DEFAULT 0.00,
    "batch_no" character varying(100),
    "sync_status" character varying(50) DEFAULT '待同步',
    "sync_time" timestamp without time zone,
    "created_at" timestamp without time zone DEFAULT now()
);

-- 4.3 财务到账缴款中间表 (HRP -> 科研)
CREATE TABLE IF NOT EXISTS "hrp_sync_incoming" (
    "id" SERIAL PRIMARY KEY,
    "project_id" integer NOT NULL,
    "incoming_id" integer,
    "remit_unit" character varying(100),
    "incoming_date" date,
    "amount" numeric(18,2) DEFAULT 0.00,
    "voucher_no" character varying(100),
    "hrp_bill_no" character varying(100),
    "sync_status" character varying(50) DEFAULT '已处理',
    "sync_time" timestamp without time zone DEFAULT now(),
    "created_at" timestamp without time zone DEFAULT now()
);

-- 4.4 财务支出报销明细中间表 (HRP -> 科研)
CREATE TABLE IF NOT EXISTS "hrp_sync_expense" (
    "id" SERIAL PRIMARY KEY,
    "project_id" integer NOT NULL,
    "claim_id" integer,
    "applicant_id" integer,
    "applicant_name" character varying(100),
    "expense_type" character varying(100),
    "finance_subject" character varying(100),
    "amount" numeric(18,2) DEFAULT 0.00,
    "hrp_voucher_no" character varying(100),
    "pay_status" character varying(50) DEFAULT '已付款',
    "pay_date" timestamp without time zone DEFAULT now(),
    "sync_status" character varying(50) DEFAULT '已处理',
    "sync_time" timestamp without time zone DEFAULT now(),
    "created_at" timestamp without time zone DEFAULT now()
);

-- 5. 存储过程：一键建立配套项目
CREATE OR REPLACE FUNCTION "sp_一键建立配套项目"(
    p_source_project_id integer,
    p_matching_funds numeric,
    p_operator_id integer
)
RETURNS jsonb LANGUAGE plpgsql AS $$
DECLARE
    source_rec record;
    v_new_project_id integer;
    v_matching_funds numeric(18,2);
    v_project_code varchar(100);
    v_existing_id integer;
    v_current_year integer := EXTRACT(YEAR FROM CURRENT_DATE)::integer;
BEGIN
    SELECT * INTO source_rec FROM "纵向项目立项" WHERE "编号" = p_source_project_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION '未找到来源纵向项目(编号: %)', p_source_project_id;
    END IF;

    SELECT "编号" INTO v_existing_id FROM "纵向项目立项" 
     WHERE "来源项目编号" = p_source_project_id AND "项目衍生类型" = '院内配套' LIMIT 1;
    IF v_existing_id IS NOT NULL THEN
        RAISE EXCEPTION '该课题已建立院内配套项目(编号: %)，不可重复建立', v_existing_id;
    END IF;

    v_matching_funds := COALESCE(p_matching_funds, source_rec."配套经费", 0.00);
    IF v_matching_funds <= 0 THEN
        RAISE EXCEPTION '配套项目必须提供大于 0 的真实配套经费，不能使用系统默认金额';
    END IF;

    v_project_code := 'PT-' || v_current_year || '-' || LPAD(p_source_project_id::text, 4, '0');

    INSERT INTO "纵向项目立项" (
        "编号", "分类", "项目登记情况", "项目编号", "项目中文名称", "项目英文名称",
        "项目分类", "成果归属部门编号", "负责人编号", "立项年度", "登记日期", "登记人编号",
        "批准日期", "批准经费", "配套经费", "经费卡号", "项目等级", "计划开始日期",
        "计划完成日期", "学科门类", "学科类型", "研究类型", "项目类型", "项目级别",
        "项目来源单位", "项目类别", "档案编号", "财务简称", "亚类说明", "附注说明",
        "纵向项目合同类型", "社会经济目标", "是否需要伦理评审", "是否需要伦理批文",
        "立项文件路径", "备注", "建立时间", "来源项目编号", "项目衍生类型", "项目编码"
    )
    VALUES (
        nextval(pg_get_serial_sequence('"纵向项目立项"', '编号')),
        source_rec."分类", '院内配套立项', v_project_code,
        '[配套] ' || source_rec."项目中文名称", source_rec."项目英文名称",
        source_rec."项目分类", source_rec."成果归属部门编号", source_rec."负责人编号",
        v_current_year, CURRENT_DATE, p_operator_id, CURRENT_DATE,
        v_matching_funds, 0.00, 'CARD-' || v_project_code, source_rec."项目等级",
        CURRENT_DATE, source_rec."计划完成日期", source_rec."学科门类", source_rec."学科类型",
        source_rec."研究类型", '院内配套', '院级', '医院自主资助', '院内配套项目',
        'ARCH-' || v_project_code, '[配套]' || COALESCE(source_rec."财务简称", '科研经费'),
        '源自课题: ' || source_rec."项目中文名称", '一键配套立项自动生成',
        '配套合同', source_rec."社会经济目标", false, false,
        source_rec."立项文件路径", '医院重点资助配套项目', CURRENT_TIMESTAMP,
        p_source_project_id, '院内配套', v_project_code
    )
    RETURNING "编号" INTO v_new_project_id;

    -- 同步项目参与人 (模板 401: 纵向项目立项)
    INSERT INTO "项目参与人" ("流程模板编号", "项目编号", "参与人编号", "工作量占比", "备注", "建立时间")
    SELECT 401, v_new_project_id, "参与人编号", "工作量占比", '院内配套项目成员', CURRENT_TIMESTAMP
      FROM "项目参与人"
     WHERE "流程模板编号" = 401 AND "项目编号" = p_source_project_id;

    -- 初始化默认配套项目预算
    INSERT INTO "项目经费预算" (
        "流程模板编号", "项目编号", "项目支出类型", "项目支出内容", "财务科目",
        "批准经费", "配套经费", "编制依据", "预算管理模式"
    )
    VALUES (
        401, v_new_project_id, '直接费用', '科研业务与试剂材料费', '科研事业支出',
        v_matching_funds, 0.00, '院内配套批复预算', 1
    );

    -- 写入 HRP 中间库待同步记录
    INSERT INTO "hrp_sync_project" (
        "project_id", "project_no", "project_name", "pi_id", "pi_name",
        "approved_funds", "matching_funds", "start_date", "end_date", "sync_status"
    )
    VALUES (
        v_new_project_id, v_project_code, '[配套] ' || source_rec."项目中文名称",
        source_rec."负责人编号", (SELECT "姓名" FROM "人员" WHERE "编号" = source_rec."负责人编号"),
        v_matching_funds, 0.00, CURRENT_DATE, source_rec."计划完成日期", '待同步'
    );

    RETURN jsonb_build_object(
        'success', true,
        'new_project_id', v_new_project_id,
        'project_code', v_project_code,
        'project_name', '[配套] ' || source_rec."项目中文名称",
        'matching_funds', v_matching_funds,
        'message', '一键配套项目建立成功 (耗时 < 1秒)'
    );
END;
$$;

-- 6. 存储过程：一键建立结余项目
CREATE OR REPLACE FUNCTION "sp_一键建立结余项目"(
    p_source_project_id integer,
    p_balance_funds numeric,
    p_operator_id integer
)
RETURNS jsonb LANGUAGE plpgsql AS $$
DECLARE
    source_rec record;
    v_new_project_id integer;
    v_balance_funds numeric(18,2);
    v_project_code varchar(100);
    v_existing_id integer;
    v_total_incoming numeric(18,2);
    v_total_spent numeric(18,2);
    v_current_year integer := EXTRACT(YEAR FROM CURRENT_DATE)::integer;
BEGIN
    SELECT * INTO source_rec FROM "纵向项目立项" WHERE "编号" = p_source_project_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION '未找到来源纵向项目(编号: %)', p_source_project_id;
    END IF;

    SELECT "编号" INTO v_existing_id FROM "纵向项目立项"
     WHERE "来源项目编号" = p_source_project_id AND "项目衍生类型" = '结余结转' LIMIT 1;
    IF v_existing_id IS NOT NULL THEN
        RAISE EXCEPTION '该课题已建立结余项目(编号: %)，不可重复建立', v_existing_id;
    END IF;

    SELECT COALESCE(SUM("到账金额"), 0.00)
      INTO v_total_incoming
      FROM "项目经费到账"
     WHERE "项目编号" = p_source_project_id;
    SELECT COALESCE(SUM("报销金额"), 0.00)
      INTO v_total_spent
      FROM "报销"
     WHERE "项目编号" = p_source_project_id;
    IF v_total_incoming <= 0 THEN
        RAISE EXCEPTION '来源项目没有真实到账记录，不能按批准经费推算结余';
    END IF;
    v_balance_funds := v_total_incoming - v_total_spent;
    IF v_balance_funds <= 0 THEN
        RAISE EXCEPTION '来源项目没有可结转的真实结余经费，不能建立结余项目';
    END IF;
    IF p_balance_funds IS NOT NULL THEN
        IF p_balance_funds <= 0 OR p_balance_funds > v_balance_funds THEN
            RAISE EXCEPTION '申请结转金额必须大于 0 且不超过真实可结转余额（可结转：%）', v_balance_funds;
        END IF;
        v_balance_funds := p_balance_funds;
    END IF;

    v_project_code := 'JY-' || v_current_year || '-' || LPAD(p_source_project_id::text, 4, '0');

    INSERT INTO "纵向项目立项" (
        "编号", "分类", "项目登记情况", "项目编号", "项目中文名称", "项目英文名称",
        "项目分类", "成果归属部门编号", "负责人编号", "立项年度", "登记日期", "登记人编号",
        "批准日期", "批准经费", "配套经费", "经费卡号", "项目等级", "计划开始日期",
        "计划完成日期", "学科门类", "学科类型", "研究类型", "项目类型", "项目级别",
        "项目来源单位", "项目类别", "档案编号", "财务简称", "亚类说明", "附注说明",
        "纵向项目合同类型", "社会经济目标", "是否需要伦理评审", "是否需要伦理批文",
        "立项文件路径", "备注", "建立时间", "来源项目编号", "项目衍生类型", "项目编码"
    )
    VALUES (
        nextval(pg_get_serial_sequence('"纵向项目立项"', '编号')),
        source_rec."分类", '结余结转立项', v_project_code,
        '[结余] ' || source_rec."项目中文名称", source_rec."项目英文名称",
        source_rec."项目分类", source_rec."成果归属部门编号", source_rec."负责人编号",
        v_current_year, CURRENT_DATE, p_operator_id, CURRENT_DATE,
        v_balance_funds, 0.00, 'CARD-' || v_project_code, source_rec."项目等级",
        CURRENT_DATE, (CURRENT_DATE + INTERVAL '2 year')::date, source_rec."学科门类", source_rec."学科类型",
        source_rec."研究类型", '结余结转', '院级结转', '科研结余发展基金', '科研结余发展项目',
        'ARCH-' || v_project_code, '[结余]' || COALESCE(source_rec."财务简称", '结余经费'),
        '源自已结题课题: ' || source_rec."项目中文名称", '结题验收结余资金合规结转',
        '结余协议', source_rec."社会经济目标", false, false,
        source_rec."立项文件路径", '结题结余继续支持后续科研', CURRENT_TIMESTAMP,
        p_source_project_id, '结余结转', v_project_code
    )
    RETURNING "编号" INTO v_new_project_id;

    -- 更新来源结题记录中的结余处理状态
    UPDATE "纵向项目结题"
       SET "结余处理状态" = '已结转结余项目',
           "结余金额" = v_balance_funds
     WHERE "纵向项目编号" = p_source_project_id;

    -- 同步项目参与人
    INSERT INTO "项目参与人" ("流程模板编号", "项目编号", "参与人编号", "工作量占比", "备注", "建立时间")
    SELECT 401, v_new_project_id, "参与人编号", "工作量占比", '结余课题成员', CURRENT_TIMESTAMP
      FROM "项目参与人"
     WHERE "流程模板编号" = 401 AND "项目编号" = p_source_project_id;

    -- 初始化结余项目预算
    INSERT INTO "项目经费预算" (
        "流程模板编号", "项目编号", "项目支出类型", "项目支出内容", "财务科目",
        "批准经费", "配套经费", "编制依据", "预算管理模式"
    )
    VALUES (
        401, v_new_project_id, '直接费用', '结余后续科研探索支出', '科研事业支出',
        v_balance_funds, 0.00, '结余课题核定额度', 1
    );

    -- 写入 HRP 中间库待同步记录
    INSERT INTO "hrp_sync_project" (
        "project_id", "project_no", "project_name", "pi_id", "pi_name",
        "approved_funds", "matching_funds", "start_date", "end_date", "sync_status"
    )
    VALUES (
        v_new_project_id, v_project_code, '[结余] ' || source_rec."项目中文名称",
        source_rec."负责人编号", (SELECT "姓名" FROM "人员" WHERE "编号" = source_rec."负责人编号"),
        v_balance_funds, 0.00, CURRENT_DATE, (CURRENT_DATE + INTERVAL '2 year')::date, '待同步'
    );

    RETURN jsonb_build_object(
        'success', true,
        'new_project_id', v_new_project_id,
        'project_code', v_project_code,
        'project_name', '[结余] ' || source_rec."项目中文名称",
        'balance_funds', v_balance_funds,
        'message', '一键结余项目建立成功 (耗时 < 1秒)'
    );
END;
$$;

-- 7. 存储过程：按到账维护项目预算
CREATE OR REPLACE FUNCTION "sp_按到账维护项目预算"(p_payload jsonb)
RETURNS jsonb LANGUAGE plpgsql AS $$
DECLARE
    v_project_id integer := (p_payload->>'项目编号')::integer;
    v_incoming_id integer := (p_payload->>'到账编号')::integer;
    v_batch_name varchar(100) := COALESCE(p_payload->>'到账批次', '按到账批次预算');
    v_items jsonb := p_payload->'预算明细';
    v_item jsonb;
    v_count integer := 0;
BEGIN
    IF v_project_id IS NULL THEN
        RAISE EXCEPTION '项目编号不能为空';
    END IF;

    -- 删除该到账批次的原预算项
    IF v_incoming_id IS NOT NULL THEN
        DELETE FROM "项目经费预算" 
         WHERE "项目编号" = v_project_id AND "到账编号" = v_incoming_id;
    END IF;

    -- 循环插入各科目预算
    IF v_items IS NOT NULL AND jsonb_typeof(v_items) = 'array' THEN
        FOR v_item IN SELECT * FROM jsonb_array_elements(v_items)
        LOOP
            INSERT INTO "项目经费预算" (
                "流程模板编号", "项目编号", "项目支出类型", "项目支出内容",
                "财务科目", "批准经费", "配套经费", "编制依据", "预算管理模式",
                "到账编号", "到账批次"
            )
            VALUES (
                401, v_project_id,
                COALESCE(v_item->>'项目支出类型', '直接费用'),
                COALESCE(v_item->>'项目支出内容', '业务费'),
                COALESCE(v_item->>'财务科目', '科研事业支出'),
                COALESCE((v_item->>'批准经费')::numeric, 0.00),
                0.00,
                COALESCE(v_item->>'编制依据', v_batch_name || '分配依据'),
                2, -- 2: 按到账金额管理预算
                v_incoming_id,
                v_batch_name
            );
            v_count := v_count + 1;
        END LOOP;
    END IF;

    RETURN jsonb_build_object(
        'success', true,
        'project_id', v_project_id,
        'incoming_id', v_incoming_id,
        'batch_name', v_batch_name,
        'inserted_items', v_count,
        'message', '按到账金额维护预算成功'
    );
END;
$$;

-- 8. 存储过程：HRP 中间库双向协同与回放
CREATE OR REPLACE FUNCTION "sp_hrp中间库双向协同"(
    p_project_id integer,
    p_action varchar(50) DEFAULT 'push_only'
)
RETURNS jsonb LANGUAGE plpgsql AS $$
DECLARE
    proj_rec record;
    budget_rec record;
    v_project_id integer;
    v_budget_id integer;
    v_pushed_projects integer := 0;
    v_pushed_budgets integer := 0;
    v_skipped_projects integer := 0;
BEGIN
    IF p_action IS DISTINCT FROM 'push_only' THEN
        RAISE EXCEPTION 'HRP 协同仅支持 push_only；未执行财务回放或付款写入（收到动作：%）', p_action
            USING ERRCODE = '22023';
    END IF;
    IF p_project_id IS NOT NULL AND p_project_id <= 0 THEN
        RAISE EXCEPTION '项目编号必须为正整数或省略' USING ERRCODE = '22023';
    END IF;
    IF p_project_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM "纵向项目立项" WHERE "编号" = p_project_id) THEN
        RAISE EXCEPTION '未找到纵向项目：%', p_project_id USING ERRCODE = '22023';
    END IF;

    PERFORM pg_advisory_xact_lock(20180724, 69001);

    FOR proj_rec IN
        SELECT p."编号", COALESCE(NULLIF(btrim(p."项目编码"), ''), 'PRJ-' || p."编号"::text) AS project_no,
               NULLIF(btrim(p."项目中文名称"), '') AS project_name,
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
           AND NULLIF(btrim(p."项目中文名称"), '') IS NOT NULL
         ORDER BY p."编号"
    LOOP
        SELECT s."id" INTO v_project_id
          FROM "hrp_sync_project" s
         WHERE s."project_id" = proj_rec."编号" AND s."sync_status" = '待同步'
         ORDER BY s."id" DESC LIMIT 1 FOR UPDATE;
        IF v_project_id IS NULL THEN
            INSERT INTO "hrp_sync_project" (
                "project_id", "project_no", "project_name", "pi_id", "pi_name", "pi_work_no",
                "dept_id", "dept_name", "approved_funds", "matching_funds", "start_date", "end_date",
                "sync_status", "sync_time", "error_message"
            ) VALUES (
                proj_rec."编号", proj_rec.project_no, proj_rec.project_name, proj_rec."负责人编号",
                proj_rec.pi_name, proj_rec.pi_work_no, proj_rec.dept_id, proj_rec.dept_name,
                proj_rec.approved_funds, proj_rec.matching_funds, proj_rec.start_date, proj_rec.end_date,
                '待同步', NULL, NULL
            );
        ELSE
            UPDATE "hrp_sync_project"
               SET "project_no" = proj_rec.project_no, "project_name" = proj_rec.project_name,
                   "pi_id" = proj_rec."负责人编号", "pi_name" = proj_rec.pi_name,
                   "pi_work_no" = proj_rec.pi_work_no, "dept_id" = proj_rec.dept_id,
                   "dept_name" = proj_rec.dept_name, "approved_funds" = proj_rec.approved_funds,
                   "matching_funds" = proj_rec.matching_funds, "start_date" = proj_rec.start_date,
                   "end_date" = proj_rec.end_date, "sync_status" = '待同步',
                   "sync_time" = NULL, "error_message" = NULL
             WHERE "id" = v_project_id;
        END IF;
        v_pushed_projects := v_pushed_projects + 1;

        FOR budget_rec IN
            SELECT b."编号", b."项目支出类型", b."项目支出内容", b."财务科目",
                   COALESCE(b."批准经费", 0.00) AS amount, b."到账批次"
              FROM "项目经费预算" b
             WHERE b."项目编号" = proj_rec."编号"
             ORDER BY b."编号"
        LOOP
            SELECT s."id" INTO v_budget_id
              FROM "hrp_sync_budget" s
             WHERE s."project_id" = proj_rec."编号"
               AND s."budget_id" IS NOT DISTINCT FROM budget_rec."编号"
               AND s."sync_status" = '待同步'
             ORDER BY s."id" DESC LIMIT 1 FOR UPDATE;
            IF v_budget_id IS NULL THEN
                INSERT INTO "hrp_sync_budget" (
                    "project_id", "budget_id", "expense_type", "expense_content", "finance_subject",
                    "amount", "batch_no", "sync_status", "sync_time"
                ) VALUES (
                    proj_rec."编号", budget_rec."编号", budget_rec."项目支出类型",
                    budget_rec."项目支出内容", budget_rec."财务科目", budget_rec.amount,
                    budget_rec."到账批次", '待同步', NULL
                );
            ELSE
                UPDATE "hrp_sync_budget"
                   SET "expense_type" = budget_rec."项目支出类型",
                       "expense_content" = budget_rec."项目支出内容",
                       "finance_subject" = budget_rec."财务科目",
                       "amount" = budget_rec.amount, "batch_no" = budget_rec."到账批次",
                       "sync_status" = '待同步', "sync_time" = NULL
                 WHERE "id" = v_budget_id;
            END IF;
            v_pushed_budgets := v_pushed_budgets + 1;
        END LOOP;
    END LOOP;

    SELECT count(*)::integer INTO v_skipped_projects
      FROM "纵向项目立项" p
     WHERE (p_project_id IS NULL OR p."编号" = p_project_id)
       AND (p."负责人编号" IS NULL OR NULLIF(btrim(p."项目中文名称"), '') IS NULL);

    RETURN jsonb_build_object(
        'success', true, 'action', 'push_only', 'project_id', p_project_id,
        'pushed_projects', v_pushed_projects, 'pushed_budgets', v_pushed_budgets,
        'skipped_projects_without_pi', v_skipped_projects,
        'pulled_incomings', 0, 'pulled_expenses', 0,
        'sync_status', '待同步',
        'message', '仅写入 HRP 项目和预算待同步暂存，未读取或写入到账/付款流水'
    );
END;
$$;

-- 9. 登记迁移版本 69
UPDATE "系统迁移版本"
   SET "版本" = 69,
       "脚本" = '001-069',
       "检查摘要" = '新增一键配套、一键结余、按到账维护预算及HRP中间库协同'
 WHERE "编号" = 1;

COMMIT;
