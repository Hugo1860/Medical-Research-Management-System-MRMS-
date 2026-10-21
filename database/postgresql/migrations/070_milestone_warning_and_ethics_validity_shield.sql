-- 迁移 070: 新增过程主动预警巡检引擎与伦理批件有效期合规阻断中枢 (清华长庚医院实践)
BEGIN;

-- 1. 扩展纵向项目立项表：支持伦理批文有效期至与动态审查状态
ALTER TABLE "纵向项目立项"
    ADD COLUMN IF NOT EXISTS "伦理批文有效期至" date NULL,
    ADD COLUMN IF NOT EXISTS "伦理审查状态" character varying(50) DEFAULT '待核验';

-- 历史数据修补：需要伦理批文的项目只能进入待核验，禁止用计划日期伪造有效期或有效状态。
UPDATE "纵向项目立项"
   SET "伦理审查状态" = CASE 
           WHEN "是否需要伦理批文" = true THEN '待核验'
           ELSE '无需伦理'
       END,
       "伦理批文有效期至" = CASE
           WHEN "是否需要伦理批文" = true THEN NULL
           ELSE NULL
       END
 WHERE "伦理审查状态" IS NULL OR "伦理审查状态" = '无需伦理';

-- 2. 创建科研项目过程预警记录表 (文献核心：中检/结题提前半年定时巡检与预算预警)
CREATE TABLE IF NOT EXISTS "科研项目过程预警记录" (
    "编号" SERIAL PRIMARY KEY,
    "项目类型" character varying(50) NOT NULL DEFAULT '纵向项目',
    "项目编号" integer NOT NULL,
    "项目名称" character varying(200),
    "负责人编号" integer,
    "负责人姓名" character varying(50),
    "预警类型" character varying(50) NOT NULL, -- '中检临期预警', '结题半年前预算执行提醒', '结题倒计时预警', '结题紧急督办', '伦理批件临期预警', '伦理批件已过期阻断'
    "预警级别" character varying(20) NOT NULL, -- '普通提醒', '黄色预警', '红色督办/阻断'
    "节点截止日期" date NOT NULL,
    "剩余天数" integer NOT NULL,
    "当前预算执行率" numeric(5,2) DEFAULT 0.00,
    "预警提示内容" text NOT NULL,
    "处理状态" character varying(20) NOT NULL DEFAULT '待处理', -- '待处理', '已确认', '已处理/归档'
    "通知渠道" character varying(100) DEFAULT '站内消息,移动科研工作台',
    "最近巡检时间" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    "建立时间" timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS "ix_过程预警_项目与状态" 
    ON "科研项目过程预警记录" ("项目编号", "预警类型", "处理状态");

CREATE INDEX IF NOT EXISTS "ix_过程预警_负责人" 
    ON "科研项目过程预警记录" ("负责人编号", "处理状态");

-- 3. 创建伦理批件续展与修正案记录表 (文献核心：伦理全生命周期动态监控)
CREATE TABLE IF NOT EXISTS "伦理批件续展与修正案记录" (
    "编号" SERIAL PRIMARY KEY,
    "项目编号" integer NOT NULL,
    "原批文编号" character varying(100),
    "新批文编号" character varying(100) NOT NULL,
    "申请类型" character varying(50) NOT NULL, -- '延期续展', '方案修正案', '知情同意书修订', '年度跟踪审查'
    "新有效期至" date NOT NULL,
    "批准时间" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    "批件文件路径" text,
    "变更事由与说明" text,
    "登记人编号" integer NOT NULL,
    "建立时间" timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS "ix_伦理续展_项目" 
    ON "伦理批件续展与修正案记录" ("项目编号");

-- 4. 存储过程：sp_科研过程主动巡检与预警生成
-- 定时或手动执行全院课题巡检，生成中检/结题/预算进度/伦理临期预警
CREATE OR REPLACE FUNCTION "sp_科研过程主动巡检与预警生成"()
RETURNS jsonb
LANGUAGE plpgsql
AS $$
DECLARE
    proj_rec RECORD;
    v_total_scanned integer := 0;
    v_alerts_generated integer := 0;
    v_blocked_count integer := 0;
    v_days_to_end integer;
    v_days_to_ethics integer;
    v_spent numeric(18,2) := 0;
    v_rate numeric(5,2) := 0;
    v_pi_name character varying(50);
BEGIN
    FOR proj_rec IN
        SELECT p."编号", p."项目中文名称", p."负责人编号", p."批准经费",
               p."计划开始日期", p."计划完成日期", p."是否需要伦理批文",
               p."伦理批文编号", p."伦理批文有效期至", p."伦理审查状态"
          FROM "纵向项目立项" p
         WHERE NOT EXISTS (
             SELECT 1 FROM "纵向项目结题" c 
              WHERE c."纵向项目编号" = p."编号" AND c."结题验收是否合格" = true
         )
    LOOP
        v_total_scanned := v_total_scanned + 1;
        v_pi_name := NULL;

        -- 获取负责人姓名
        IF proj_rec."负责人编号" IS NOT NULL AND proj_rec."负责人编号" > 0 THEN
            SELECT r."姓名"
              INTO v_pi_name
              FROM "人员" r
             WHERE r."编号" = proj_rec."负责人编号";
        END IF;

        -- 计算当前已报销支出
        SELECT COALESCE(SUM(b."报销金额"), 0)
          INTO v_spent
          FROM "报销" b
         WHERE b."项目编号" = proj_rec."编号";

        IF COALESCE(proj_rec."批准经费", 0) > 0 THEN
            v_rate := LEAST(100.00, ROUND((v_spent / proj_rec."批准经费") * 100, 2));
        ELSE
            v_rate := 0.00;
        END IF;

        -- 1. 结题周期巡检 (文献清华长庚医院实践：半年前每月提醒一次预算执行情况与进度)
        v_days_to_end := proj_rec."计划完成日期" - CURRENT_DATE;

        -- 结题前 180 天 (半年) 内
        IF v_days_to_end <= 180 AND v_days_to_end > 90 THEN
            IF NOT EXISTS (
                SELECT 1 FROM "科研项目过程预警记录"
                 WHERE "项目编号" = proj_rec."编号" 
                   AND "预警类型" = '结题半年前预算执行提醒' 
                   AND "处理状态" = '待处理'
            ) THEN
                INSERT INTO "科研项目过程预警记录" (
                    "项目编号", "项目名称", "负责人编号", "负责人姓名",
                    "预警类型", "预警级别", "节点截止日期", "剩余天数",
                    "当前预算执行率", "预警提示内容"
                ) VALUES (
                    proj_rec."编号", proj_rec."项目中文名称", proj_rec."负责人编号", v_pi_name,
                    '结题半年前预算执行提醒', '黄色预警', proj_rec."计划完成日期", v_days_to_end,
                    v_rate,
                    '该项目距离合同计划结题日尚余 ' || v_days_to_end || ' 天（不足半年），当前经费执行率为 ' || v_rate || '%。请合理规划科研进度与经费支出节奏，避免突击花钱。'
                );
                v_alerts_generated := v_alerts_generated + 1;
            END IF;
        ELSIF v_days_to_end <= 90 AND v_days_to_end > 30 THEN
            IF NOT EXISTS (
                SELECT 1 FROM "科研项目过程预警记录"
                 WHERE "项目编号" = proj_rec."编号" 
                   AND "预警类型" = '结题倒计时预警' 
                   AND "处理状态" = '待处理'
            ) THEN
                INSERT INTO "科研项目过程预警记录" (
                    "项目编号", "项目名称", "负责人编号", "负责人姓名",
                    "预警类型", "预警级别", "节点截止日期", "剩余天数",
                    "当前预算执行率", "预警提示内容"
                ) VALUES (
                    proj_rec."编号", proj_rec."项目中文名称", proj_rec."负责人编号", v_pi_name,
                    '结题倒计时预警', '黄色预警', proj_rec."计划完成日期", v_days_to_end,
                    v_rate,
                    '该项目已进入结题冲刺阶段，距合同完成日期仅剩 ' || v_days_to_end || ' 天，请尽快完成技术总结与财务对账决算。'
                );
                v_alerts_generated := v_alerts_generated + 1;
            END IF;
        ELSIF v_days_to_end <= 30 THEN
            IF NOT EXISTS (
                SELECT 1 FROM "科研项目过程预警记录"
                 WHERE "项目编号" = proj_rec."编号" 
                   AND "预警类型" = '结题紧急督办' 
                   AND "处理状态" = '待处理'
            ) THEN
                INSERT INTO "科研项目过程预警记录" (
                    "项目编号", "项目名称", "负责人编号", "负责人姓名",
                    "预警类型", "预警级别", "节点截止日期", "剩余天数",
                    "当前预算执行率", "预警提示内容"
                ) VALUES (
                    proj_rec."编号", proj_rec."项目中文名称", proj_rec."负责人编号", v_pi_name,
                    '结题紧急督办', '红色督办/阻断', proj_rec."计划完成日期", v_days_to_end,
                    v_rate,
                    '【红色紧急督办】课题计划完成期仅剩 ' || v_days_to_end || ' 天或已逾期！请立即提交结题验收申请或合规延期申请。'
                );
                v_alerts_generated := v_alerts_generated + 1;
                v_blocked_count := v_blocked_count + 1;
            END IF;
        END IF;

        -- 2. 伦理批件全生命周期合规巡检 (文献清华长庚医院实践：防范伦理过期导致科研停滞)
        IF proj_rec."是否需要伦理批文" = true THEN
            IF proj_rec."伦理批文有效期至" IS NOT NULL THEN
                v_days_to_ethics := proj_rec."伦理批文有效期至" - CURRENT_DATE;

                IF v_days_to_ethics < 0 THEN
                    -- 已过期
                    UPDATE "纵向项目立项"
                       SET "伦理审查状态" = '已过期'
                     WHERE "编号" = proj_rec."编号";

                    IF NOT EXISTS (
                        SELECT 1 FROM "科研项目过程预警记录"
                         WHERE "项目编号" = proj_rec."编号" 
                           AND "预警类型" = '伦理批件已过期阻断' 
                           AND "处理状态" = '待处理'
                    ) THEN
                        INSERT INTO "科研项目过程预警记录" (
                            "项目编号", "项目名称", "负责人编号", "负责人姓名",
                            "预警类型", "预警级别", "节点截止日期", "剩余天数",
                            "当前预算执行率", "预警提示内容"
                        ) VALUES (
                            proj_rec."编号", proj_rec."项目中文名称", proj_rec."负责人编号", v_pi_name,
                            '伦理批件已过期阻断', '红色督办/阻断', proj_rec."伦理批文有效期至", v_days_to_ethics,
                            v_rate,
                            '【合规阻断】该项目伦理批件已过期（' || to_char(proj_rec."伦理批文有效期至", 'YYYY-MM-DD') || '），已依法触发中检及临床报销阻断！请尽快办理伦理延期续展或修正案登记。'
                        );
                        v_alerts_generated := v_alerts_generated + 1;
                        v_blocked_count := v_blocked_count + 1;
                    END IF;
                ELSIF v_days_to_ethics <= 60 THEN
                    -- 即将到期 (60天以内)
                    UPDATE "纵向项目立项"
                       SET "伦理审查状态" = '即将到期'
                     WHERE "编号" = proj_rec."编号";

                    IF NOT EXISTS (
                        SELECT 1 FROM "科研项目过程预警记录"
                         WHERE "项目编号" = proj_rec."编号" 
                           AND "预警类型" = '伦理批件临期预警' 
                           AND "处理状态" = '待处理'
                    ) THEN
                        INSERT INTO "科研项目过程预警记录" (
                            "项目编号", "项目名称", "负责人编号", "负责人姓名",
                            "预警类型", "预警级别", "节点截止日期", "剩余天数",
                            "当前预算执行率", "预警提示内容"
                        ) VALUES (
                            proj_rec."编号", proj_rec."项目中文名称", proj_rec."负责人编号", v_pi_name,
                            '伦理批件临期预警', '黄色预警', proj_rec."伦理批文有效期至", v_days_to_ethics,
                            v_rate,
                            '【伦理临期】伦理审查批件尚余 ' || v_days_to_ethics || ' 天到期，请及时向医院伦理委员会申请年度跟踪审查或延期续展批件。'
                        );
                        v_alerts_generated := v_alerts_generated + 1;
                    END IF;
                ELSE
                    UPDATE "纵向项目立项"
                       SET "伦理审查状态" = '有效'
                     WHERE "编号" = proj_rec."编号";
                END IF;
            ELSE
                -- 未配置有效期但要求伦理批文
                UPDATE "纵向项目立项"
                   SET "伦理审查状态" = '待补录有效期'
                 WHERE "编号" = proj_rec."编号";
            END IF;
        END IF;
    END LOOP;

    RETURN jsonb_build_object(
        'success', true,
        'total_scanned', v_total_scanned,
        'alerts_generated', v_alerts_generated,
        'blocked_count', v_blocked_count,
        'message', '科研过程主动巡检与预警生成完成'
    );
END;
$$;

-- 5. 存储过程：sp_项目过程前置合规检查 (支持中检与报销阻断)
CREATE OR REPLACE FUNCTION "sp_项目过程前置合规检查"(
    p_project_id integer,
    p_check_type character varying DEFAULT '中检'
)
RETURNS jsonb
LANGUAGE plpgsql
AS $$
DECLARE
    v_need_ethics boolean;
    v_ethics_no character varying(100);
    v_ethics_expiry date;
    v_ethics_status character varying(50);
    v_days_left integer;
BEGIN
    SELECT "是否需要伦理批文", "伦理批文编号", "伦理批文有效期至", "伦理审查状态"
      INTO v_need_ethics, v_ethics_no, v_ethics_expiry, v_ethics_status
      FROM "纵向项目立项"
     WHERE "编号" = p_project_id;

    IF NOT FOUND THEN
        RETURN jsonb_build_object(
            'passed', false,
            'block', true,
            'message', '未找到指定的科研课题记录'
        );
    END IF;

    -- 伦理强规则检查
    IF v_need_ethics = true THEN
        IF v_ethics_expiry IS NOT NULL AND v_ethics_expiry < CURRENT_DATE THEN
            RETURN jsonb_build_object(
                'passed', false,
                'block', true,
                'check_type', p_check_type,
                'ethics_status', '已过期',
                'expiry_date', to_char(v_ethics_expiry, 'YYYY-MM-DD'),
                'message', '合规阻断：该课题伦理批件已于 ' || to_char(v_ethics_expiry, 'YYYY-MM-DD') || ' 过期！依据国家临床研究管理法规及医院伦理质控规范，当前' || p_check_type || '已被系统自动阻断。请先上传伦理续展批件或修正案批件解除阻断！'
            );
        END IF;

        IF v_ethics_no IS NULL OR TRIM(v_ethics_no) = '' THEN
            RETURN jsonb_build_object(
                'passed', false,
                'block', true,
                'check_type', p_check_type,
                'ethics_status', '未上传',
                'message', '合规阻断：该课题为需要伦理批文的临床研究，当前尚未上传生效伦理批文，系统已阻断' || p_check_type || '操作。'
            );
        END IF;
    END IF;

    RETURN jsonb_build_object(
        'passed', true,
        'block', false,
        'check_type', p_check_type,
        'message', '前置合规性检查通过，允许继续执行'
    );
END;
$$;

-- 6. 存储过程：sp_登记伦理批文续展与修正案 (消除过期阻断)
CREATE OR REPLACE FUNCTION "sp_登记伦理批文续展与修正案"(p_payload jsonb)
RETURNS jsonb
LANGUAGE plpgsql
AS $$
DECLARE
    v_project_id integer;
    v_old_no character varying(100);
    v_new_no character varying(100);
    v_type character varying(50);
    v_new_expiry date;
    v_file_path text;
    v_remark text;
    v_operator_id integer;
    v_record_id integer;
BEGIN
    v_project_id := (p_payload->>'项目编号')::integer;
    v_new_no := p_payload->>'新批文编号';
    v_type := COALESCE(p_payload->>'申请类型', '延期续展');
    v_new_expiry := (p_payload->>'新有效期至')::date;
    v_file_path := p_payload->>'批件文件路径';
    v_remark := p_payload->>'变更事由与说明';
    v_operator_id := (p_payload->>'登记人编号')::integer;

    IF v_project_id IS NULL OR v_new_no IS NULL OR v_new_expiry IS NULL OR v_operator_id IS NULL OR v_operator_id <= 0 THEN
        RAISE EXCEPTION '项目编号、新批文编号、新有效期至和登记人编号为必填项';
    END IF;

    -- 获取当前原批文编号
    SELECT "伦理批文编号"
      INTO v_old_no
      FROM "纵向项目立项"
     WHERE "编号" = v_project_id;

    -- 插入修正案/续展记录
    INSERT INTO "伦理批件续展与修正案记录" (
        "项目编号", "原批文编号", "新批文编号", "申请类型",
        "新有效期至", "批件文件路径", "变更事由与说明", "登记人编号"
    ) VALUES (
        v_project_id, v_old_no, v_new_no, v_type,
        v_new_expiry, v_file_path, v_remark, v_operator_id
    ) RETURNING "编号" INTO v_record_id;

    -- 更新纵向项目立项中的批文主数据
    UPDATE "纵向项目立项"
       SET "伦理批文编号" = v_new_no,
           "伦理批文有效期至" = v_new_expiry,
           "伦理批文时间" = CURRENT_TIMESTAMP,
           "伦理批文路径" = COALESCE(v_file_path, "伦理批文路径"),
           "伦理审查状态" = '有效'
     WHERE "编号" = v_project_id;

    -- 同步更新外部伦理同步表 (若存在对应记录)
    UPDATE "项目伦理同步记录"
       SET "状态" = '有效',
           "批文编号" = v_new_no,
           "有效期至" = v_new_expiry,
           "最后同步时间" = CURRENT_TIMESTAMP
     WHERE "项目编号" = v_project_id;

    -- 自动将该项目当前未结案的伦理过期/临期预警转为已处理
    UPDATE "科研项目过程预警记录"
       SET "处理状态" = '已处理/归档',
           "最近巡检时间" = CURRENT_TIMESTAMP
     WHERE "项目编号" = v_project_id
       AND "预警类型" IN ('伦理批件已过期阻断', '伦理批件临期预警');

    RETURN jsonb_build_object(
        'success', true,
        'record_id', v_record_id,
        'project_id', v_project_id,
        'new_ethics_no', v_new_no,
        'new_expiry', to_char(v_new_expiry, 'YYYY-MM-DD'),
        'message', '伦理批件续展/修正案登记成功，合规阻断已解除'
    );
END;
$$;

-- 7. 初始执行一次主动巡检以填充初始预警数据
SELECT "sp_科研过程主动巡检与预警生成"();

-- 8. 登记迁移版本 70
UPDATE "系统迁移版本"
   SET "版本" = 70,
       "脚本" = '001-070',
       "检查摘要" = '新增过程主动预警巡检引擎与伦理批件有效期合规阻断中枢'
 WHERE "编号" = 1;

COMMIT;
