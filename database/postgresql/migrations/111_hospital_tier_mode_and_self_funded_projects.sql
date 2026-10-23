BEGIN;

-- 1. 医院运营配置表
CREATE TABLE IF NOT EXISTS "医院运营配置" (
    "编号" integer NOT NULL PRIMARY KEY DEFAULT 1,
    "运营模式" character varying(32) NOT NULL DEFAULT '三级综合' CHECK ("运营模式" IN ('三级综合', '二级精简')),
    "隐藏菜单" jsonb NOT NULL DEFAULT '[]'::jsonb,
    "说明" text,
    "更新人" character varying(64),
    "更新时间" timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO "医院运营配置" ("编号", "运营模式", "隐藏菜单", "说明")
VALUES (
    1,
    '三级综合',
    '["/office/biobank-ledger", "/office/education-statistics", "/governance/proxy-reconciliation", "/operations/external-sync", "/project/vertical-import", "/finance/fund-execution"]'::jsonb,
    '医院运营模式配置（三级综合/二级精简）'
)
ON CONFLICT ("编号") DO NOTHING;

-- 2. 纵向项目立项与申报增加资助类型
ALTER TABLE "纵向项目立项" ADD COLUMN IF NOT EXISTS "资助类型" character varying(50) DEFAULT '财政资助';
ALTER TABLE "纵向项目申报" ADD COLUMN IF NOT EXISTS "资助类型" character varying(50) DEFAULT '财政资助';

-- 3. 更新 sp_纵向项目立项_增改 支持资助类型
CREATE OR REPLACE FUNCTION public."sp_纵向项目立项_增改"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE
    v_new_id integer;
BEGIN
    INSERT INTO "纵向项目立项" (
        "编号", "分类", "项目登记情况", "项目编号", "项目中文名称", "项目英文名称", "项目分类", 
        "成果归属部门编号", "负责人编号", "立项年度", "登记日期", "登记人编号", "批准日期", 
        "批准经费", "配套经费", "经费卡号", "项目等级", "计划开始日期", "计划完成日期", 
        "学科门类", "学科类型", "研究类型", "项目类型", "项目级别", "项目来源单位", 
        "项目类别", "档案编号", "财务简称", "亚类说明", "附注说明", "纵向项目合同类型", 
        "社会经济目标", "是否需要伦理评审", "是否需要伦理批文", "伦理批文编号", 
        "伦理批文时间", "伦理批文路径", "立项文件路径", "备注", "建立时间", "资助类型"
    )
    SELECT 
        CASE WHEN COALESCE(r."编号", 0) = 0 THEN nextval(pg_get_serial_sequence('"纵向项目立项"', '编号')) ELSE r."编号" END,
        r."分类", r."项目登记情况", r."项目编号", r."项目中文名称", r."项目英文名称", r."项目分类",
        r."成果归属部门编号", r."负责人编号", r."立项年度", r."登记日期", r."登记人编号", r."批准日期",
        r."批准经费", r."配套经费", r."经费卡号", r."项目等级", r."计划开始日期", r."计划完成日期",
        r."学科门类", r."学科类型", r."研究类型", r."项目类型", r."项目级别", r."项目来源单位",
        r."项目类别", r."档案编号", r."财务简称", r."亚类说明", r."附注说明", r."纵向项目合同类型",
        r."社会经济目标", r."是否需要伦理评审", r."是否需要伦理批文", r."伦理批文编号",
        r."伦理批文时间", r."伦理批文路径", r."立项文件路径", r."备注",
        COALESCE(r."建立时间", CURRENT_TIMESTAMP),
        COALESCE(r."资助类型", '财政资助')
    FROM jsonb_to_recordset("pg_first_json_array"(p_payload)) AS r(
        "编号" integer, "分类" integer, "项目登记情况" varchar(50), "项目编号" varchar(50),
        "项目中文名称" varchar(100), "项目英文名称" varchar(100), "项目分类" varchar(50),
        "成果归属部门编号" integer, "负责人编号" integer, "立项年度" integer,
        "登记日期" date, "登记人编号" integer, "批准日期" date,
        "批准经费" numeric(18,2), "配套经费" numeric(18,2), "经费卡号" varchar(50),
        "项目等级" varchar(50), "计划开始日期" date, "计划完成日期" date,
        "学科门类" varchar(50), "学科类型" varchar(50), "研究类型" varchar(50),
        "项目类型" varchar(50), "项目级别" varchar(50), "项目来源单位" varchar(50),
        "项目类别" varchar(50), "档案编号" varchar(50), "财务简称" varchar(100),
        "亚类说明" varchar(500), "附注说明" varchar(500), "纵向项目合同类型" varchar(50),
        "社会经济目标" varchar(1000), "是否需要伦理评审" boolean, "是否需要伦理批文" boolean,
        "伦理批文编号" varchar(50), "伦理批文时间" timestamp without time zone,
        "伦理批文路径" text, "立项文件路径" text, "备注" varchar(500),
        "建立时间" timestamp without time zone, "资助类型" varchar(50)
    )
    ON CONFLICT ("编号") DO UPDATE SET
        "分类" = EXCLUDED."分类",
        "项目登记情况" = EXCLUDED."项目登记情况",
        "项目编号" = EXCLUDED."项目编号",
        "项目中文名称" = EXCLUDED."项目中文名称",
        "项目英文名称" = EXCLUDED."项目英文名称",
        "项目分类" = EXCLUDED."项目分类",
        "成果归属部门编号" = EXCLUDED."成果归属部门编号",
        "负责人编号" = EXCLUDED."负责人编号",
        "立项年度" = EXCLUDED."立项年度",
        "登记日期" = EXCLUDED."登记日期",
        "登记人编号" = EXCLUDED."登记人编号",
        "批准日期" = EXCLUDED."批准日期",
        "批准经费" = EXCLUDED."批准经费",
        "配套经费" = EXCLUDED."配套经费",
        "经费卡号" = EXCLUDED."经费卡号",
        "项目等级" = EXCLUDED."项目等级",
        "计划开始日期" = EXCLUDED."计划开始日期",
        "计划完成日期" = EXCLUDED."计划完成日期",
        "学科门类" = EXCLUDED."学科门类",
        "学科类型" = EXCLUDED."学科类型",
        "研究类型" = EXCLUDED."研究类型",
        "项目类型" = EXCLUDED."项目类型",
        "项目级别" = EXCLUDED."项目级别",
        "项目来源单位" = EXCLUDED."项目来源单位",
        "项目类别" = EXCLUDED."项目类别",
        "档案编号" = EXCLUDED."档案编号",
        "财务简称" = EXCLUDED."财务简称",
        "亚类说明" = EXCLUDED."亚类说明",
        "附注说明" = EXCLUDED."附注说明",
        "纵向项目合同类型" = EXCLUDED."纵向项目合同类型",
        "社会经济目标" = EXCLUDED."社会经济目标",
        "是否需要伦理评审" = EXCLUDED."是否需要伦理评审",
        "是否需要伦理批文" = EXCLUDED."是否需要伦理批文",
        "伦理批文编号" = EXCLUDED."伦理批文编号",
        "伦理批文时间" = EXCLUDED."伦理批文时间",
        "伦理批文路径" = EXCLUDED."伦理批文路径",
        "立项文件路径" = EXCLUDED."立项文件路径",
        "备注" = EXCLUDED."备注",
        "资助类型" = COALESCE(EXCLUDED."资助类型", '财政资助')
    RETURNING "编号" INTO v_new_id;
    RETURN COALESCE(v_new_id, 0);
END;
$$;

UPDATE "系统迁移版本"
SET "版本" = 111,
    "脚本" = '111_hospital_tier_mode_and_self_funded_projects.sql',
    "检查摘要" = '新增医院运营模式配置与自筹/立项不资助课题简易通道',
    "应用时间" = CURRENT_TIMESTAMP
WHERE "编号" = 1;

COMMIT;
