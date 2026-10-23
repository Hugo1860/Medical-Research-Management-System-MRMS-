BEGIN;

-- 1. 期刊风险名单表
CREATE TABLE IF NOT EXISTS "期刊风险名单" (
    "编号" serial PRIMARY KEY,
    "来源" character varying(50) NOT NULL CHECK ("来源" IN ('中科院预警', '院内黑名单', '院内关注', '其他')),
    "年度" integer NOT NULL,
    "期刊名称" character varying(200) NOT NULL,
    "规范化名称" character varying(200) NOT NULL,
    "ISSN" character varying(50),
    "eISSN" character varying(50),
    "风险等级" character varying(20) NOT NULL CHECK ("风险等级" IN ('高', '中', '低')),
    "风险原因" text,
    "生效状态" boolean NOT NULL DEFAULT TRUE,
    "导入批次" character varying(50),
    "创建人" character varying(50),
    "创建时间" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "uq_期刊风险名单_条目" UNIQUE ("来源", "年度", "规范化名称")
);

CREATE INDEX IF NOT EXISTS "ix_期刊风险名单_规范名称" ON "期刊风险名单" ("规范化名称");
CREATE INDEX IF NOT EXISTS "ix_期刊风险名单_ISSN" ON "期刊风险名单" ("ISSN");

-- 2. 国内正规期刊库
CREATE TABLE IF NOT EXISTS "国内正规期刊库" (
    "编号" serial PRIMARY KEY,
    "期刊名称" character varying(200) NOT NULL,
    "规范化名称" character varying(200) NOT NULL,
    "CN号" character varying(50),
    "ISSN" character varying(50),
    "主管单位" character varying(200),
    "主办单位" character varying(200),
    "是否医学类" boolean NOT NULL DEFAULT TRUE,
    "来源说明" text,
    "更新时间" timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS "ix_国内正规期刊_规范名称" ON "国内正规期刊库" ("规范化名称");
CREATE INDEX IF NOT EXISTS "ix_国内正规期刊_CN" ON "国内正规期刊库" ("CN号");
CREATE INDEX IF NOT EXISTS "ix_国内正规期刊_ISSN" ON "国内正规期刊库" ("ISSN");

-- 3. 序列与生成备案号函数
CREATE SEQUENCE IF NOT EXISTS "seq_论文投稿备案号" START WITH 1 INCREMENT BY 1;

CREATE OR REPLACE FUNCTION public."fn_生成论文投稿备案号"()
RETURNS character varying LANGUAGE plpgsql AS $$
DECLARE
    v_year varchar(4);
    v_seq bigint;
    v_no varchar(50);
BEGIN
    v_year := to_char(CURRENT_DATE, 'YYYY');
    v_seq := nextval('"seq_论文投稿备案号"');
    v_no := 'TG' || v_year || lpad(v_seq::text, 6, '0');
    RETURN v_no;
END;
$$;

-- 4. 论文投稿备案表
CREATE TABLE IF NOT EXISTS "论文投稿备案" (
    "编号" serial PRIMARY KEY,
    "备案号" character varying(50) NOT NULL UNIQUE,
    "申请人编号" integer NOT NULL REFERENCES "人员"("编号"),
    "申请人姓名" character varying(50) NOT NULL,
    "部门编号" integer REFERENCES "部门"("编号"),
    "部门名称" character varying(100),
    "论文题目" character varying(500) NOT NULL,
    "拟投期刊名称" character varying(200) NOT NULL,
    "ISSN" character varying(50),
    "CN号" character varying(50),
    "期刊类型" character varying(20) NOT NULL DEFAULT '国内' CHECK ("期刊类型" IN ('国内', '国外')),
    "作者排序" jsonb NOT NULL DEFAULT '[]'::jsonb,
    "关联项目类型" character varying(50),
    "关联项目编号" integer,
    "稿件附件路径" text,
    "诚信承诺" boolean NOT NULL DEFAULT FALSE,
    "风险检查结果" jsonb,
    "风险等级" character varying(20) NOT NULL DEFAULT '低' CHECK ("风险等级" IN ('高', '中', '低', '无')),
    "状态" character varying(30) NOT NULL DEFAULT '草稿' CHECK ("状态" IN ('草稿', '待科室审核', '待科教科审核', '已备案', '已驳回', '已撤回', '已见刊', '已作废')),
    "科室审核人编号" integer,
    "科室审核人姓名" character varying(50),
    "科室审核时间" timestamp with time zone,
    "科室审核意见" text,
    "科教科审核人编号" integer,
    "科教科审核人姓名" character varying(50),
    "科教科审核时间" timestamp with time zone,
    "科教科审核意见" text,
    "关联论文编号" integer REFERENCES "成果论文"("编号") ON DELETE SET NULL,
    "预计版面费" numeric(18,2) DEFAULT 0,
    "创建时间" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    "更新时间" timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS "ix_论文投稿备案_申请人" ON "论文投稿备案" ("申请人编号");
CREATE INDEX IF NOT EXISTS "ix_论文投稿备案_状态" ON "论文投稿备案" ("状态");
CREATE INDEX IF NOT EXISTS "ix_论文投稿备案_备案号" ON "论文投稿备案" ("备案号");

-- 5. 论文投稿备案审核记录（防篡改审计日志）
CREATE TABLE IF NOT EXISTS "论文投稿备案审核记录" (
    "编号" serial PRIMARY KEY,
    "备案编号" integer NOT NULL REFERENCES "论文投稿备案"("编号") ON DELETE CASCADE,
    "审核节点" character varying(50) NOT NULL,
    "操作人编号" integer NOT NULL,
    "操作人姓名" character varying(50) NOT NULL,
    "操作时间" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    "操作结果" character varying(50) NOT NULL,
    "审核意见" text,
    "快照数据" jsonb
);

CREATE OR REPLACE FUNCTION public."trg_禁止修改删除论文投稿备案审核记录"()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION '论文投稿备案审核记录为不可篡改审计日志，严禁修改或删除';
    RETURN NULL;
END;
$$;

DROP TRIGGER IF EXISTS "trg_防篡改_论文投稿备案审核记录" ON "论文投稿备案审核记录";
CREATE TRIGGER "trg_防篡改_论文投稿备案审核记录"
BEFORE UPDATE OR DELETE ON "论文投稿备案审核记录"
FOR EACH ROW EXECUTE FUNCTION public."trg_禁止修改删除论文投稿备案审核记录"();

-- 6. 关联报销表
ALTER TABLE "报销" ADD COLUMN IF NOT EXISTS "投稿备案编号" character varying(50);
ALTER TABLE "报销详情" ADD COLUMN IF NOT EXISTS "投稿备案编号" character varying(50);

-- 7. 权限注册与授权
INSERT INTO "权限" ("编号", "名称", "上级编号", "路径", "类型", "备注")
VALUES (219, '论文投稿备案', 2, '科研办公/科研办公/论文投稿备案.html', true, '论文投稿事前备案、期刊风控预警核查与版面费报销关联')
ON CONFLICT ("编号") DO UPDATE
SET "名称" = EXCLUDED."名称",
    "上级编号" = EXCLUDED."上级编号",
    "路径" = EXCLUDED."路径",
    "类型" = EXCLUDED."类型",
    "备注" = EXCLUDED."备注";

-- 授权给管理员、科教办、科室负责人、科研秘书及科研人员角色
INSERT INTO "角色权限" ("角色编号", "权限编号")
SELECT r."编号", 219
FROM "角色" r
WHERE r."编号" IN (1, 3, 4, 5, 15, 1031, 1032, 1033)
ON CONFLICT DO NOTHING;

UPDATE "系统迁移版本"
SET "版本" = 112,
    "脚本" = '112_paper_submission_filing_and_journal_risk.sql',
    "检查摘要" = '新增论文投稿备案、期刊风控（中科院预警/黑名单与正规期刊库）及版面费备案关联闭环',
    "应用时间" = CURRENT_TIMESTAMP
WHERE "编号" = 1;

COMMIT;
