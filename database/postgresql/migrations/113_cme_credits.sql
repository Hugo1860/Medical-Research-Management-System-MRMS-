BEGIN;

-- 1. 继教学分标准表
CREATE TABLE IF NOT EXISTS "继教学分标准" (
    "编号" serial PRIMARY KEY,
    "年度" integer NOT NULL,
    "适用人群" character varying(50) NOT NULL DEFAULT '卫生技术人员',
    "职称级别" character varying(50) DEFAULT '不限',
    "年度总分要求" numeric(5,1) NOT NULL DEFAULT 25.0,
    "I类最低分" numeric(5,1) NOT NULL DEFAULT 10.0,
    "II类最低分" numeric(5,1) NOT NULL DEFAULT 0.0,
    "说明" text,
    "是否启用" boolean NOT NULL DEFAULT TRUE,
    "创建时间" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "uq_继教学分标准" UNIQUE ("年度", "适用人群", "职称级别")
);

-- 初始化当前年度与次年标准
INSERT INTO "继教学分标准" ("年度", "适用人群", "职称级别", "年度总分要求", "I类最低分", "II类最低分", "说明")
VALUES 
(EXTRACT(YEAR FROM CURRENT_DATE)::integer, '卫生技术人员', '高级', 25.0, 10.0, 0.0, '高级卫技人员年度达标要求（总分≥25，I类≥10）'),
(EXTRACT(YEAR FROM CURRENT_DATE)::integer, '卫生技术人员', '中级', 25.0, 8.0, 0.0, '中级卫技人员年度达标要求（总分≥25，I类≥8）'),
(EXTRACT(YEAR FROM CURRENT_DATE)::integer, '卫生技术人员', '初级', 20.0, 5.0, 0.0, '初级卫技人员年度达标要求（总分≥20，I类≥5）'),
(EXTRACT(YEAR FROM CURRENT_DATE)::integer, '卫生技术人员', '不限', 25.0, 10.0, 0.0, '全院医护通用最低达标标准')
ON CONFLICT ("年度", "适用人群", "职称级别") DO NOTHING;

-- 2. 讲座学分设置表
CREATE TABLE IF NOT EXISTS "讲座学分设置" (
    "编号" serial PRIMARY KEY,
    "讲座编号" integer NOT NULL REFERENCES "主办讲座"("编号") ON DELETE CASCADE UNIQUE,
    "学分类别" character varying(20) NOT NULL DEFAULT 'II类' CHECK ("学分类别" IN ('I类', 'II类')),
    "学分值" numeric(5,1) NOT NULL DEFAULT 1.0 CHECK ("学分值" > 0 AND "学分值" <= 20),
    "签到开始时间" timestamp with time zone NOT NULL,
    "签到结束时间" timestamp with time zone NOT NULL,
    "签到令牌" character varying(64) NOT NULL UNIQUE,
    "是否已授分" boolean NOT NULL DEFAULT FALSE,
    "创建人编号" integer,
    "创建时间" timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS "ix_讲座学分设置_令牌" ON "讲座学分设置" ("签到令牌");

-- 3. 院内讲座签到表
CREATE TABLE IF NOT EXISTS "院内讲座签到" (
    "编号" serial PRIMARY KEY,
    "讲座编号" integer NOT NULL REFERENCES "主办讲座"("编号") ON DELETE CASCADE,
    "人员编号" integer NOT NULL REFERENCES "人员"("编号"),
    "签到时间" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    "签到方式" character varying(20) NOT NULL DEFAULT '扫码' CHECK ("签到方式" IN ('扫码', '管理员登记', '导入')),
    "备注" text,
    CONSTRAINT "uq_讲座签到_人员" UNIQUE ("讲座编号", "人员编号")
);

CREATE INDEX IF NOT EXISTS "ix_院内讲座签到_人员" ON "院内讲座签到" ("人员编号");
CREATE INDEX IF NOT EXISTS "ix_院内讲座签到_讲座" ON "院内讲座签到" ("讲座编号");

-- 4. 继教学分记录表
CREATE TABLE IF NOT EXISTS "继教学分记录" (
    "编号" serial PRIMARY KEY,
    "人员编号" integer NOT NULL REFERENCES "人员"("编号"),
    "年度" integer NOT NULL,
    "学分类别" character varying(20) NOT NULL CHECK ("学分类别" IN ('I类', 'II类')),
    "学分" numeric(5,1) NOT NULL CHECK ("学分" > 0 AND "学分" <= 50),
    "项目名称" character varying(200) NOT NULL,
    "项目编号" character varying(100),
    "授予单位" character varying(200) NOT NULL,
    "获得日期" date NOT NULL,
    "来源" character varying(30) NOT NULL CHECK ("来源" IN ('个人申报', '院内讲座签到', '外出进修折算', '学术会议', '线上学习', '管理员导入')),
    "来源业务类型" character varying(50),
    "来源业务编号" integer,
    "证书附件路径" text,
    "审核状态" character varying(20) NOT NULL DEFAULT '待审核' CHECK ("审核状态" IN ('待审核', '已认定', '已驳回')),
    "审核人编号" integer,
    "审核人姓名" character varying(50),
    "审核时间" timestamp with time zone,
    "审核意见" text,
    "创建时间" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    "更新时间" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "uq_继教学分记录_来源幂等" UNIQUE ("来源业务类型", "来源业务编号", "人员编号")
);

CREATE INDEX IF NOT EXISTS "ix_继教学分记录_人员年度" ON "继教学分记录" ("人员编号", "年度");
CREATE INDEX IF NOT EXISTS "ix_继教学分记录_状态" ON "继教学分记录" ("审核状态");

-- 5. 继教催办记录表
CREATE TABLE IF NOT EXISTS "继教催办记录" (
    "编号" serial PRIMARY KEY,
    "年度" integer NOT NULL,
    "人员编号" integer NOT NULL REFERENCES "人员"("编号"),
    "催办方式" character varying(30) NOT NULL DEFAULT '系统消息',
    "当前总分" numeric(5,1) NOT NULL DEFAULT 0,
    "当前I类分" numeric(5,1) NOT NULL DEFAULT 0,
    "要求总分" numeric(5,1) NOT NULL DEFAULT 25,
    "要求I类分" numeric(5,1) NOT NULL DEFAULT 10,
    "催办内容" text NOT NULL,
    "催办人编号" integer,
    "催办人姓名" character varying(50),
    "催办时间" timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS "ix_继教催办记录_人员" ON "继教催办记录" ("人员编号", "年度");

-- 6. 权限注册与授权
INSERT INTO "权限" ("编号", "名称", "上级编号", "路径", "类型", "备注")
VALUES (220, '继续医学教育学分', 2, '科研办公/科研办公/继续医学教育学分.html', true, 'CME继教学分登记、院内讲座扫码授分、年度达标统计与催办')
ON CONFLICT ("编号") DO UPDATE
SET "名称" = EXCLUDED."名称",
    "上级编号" = EXCLUDED."上级编号",
    "路径" = EXCLUDED."路径",
    "类型" = EXCLUDED."类型",
    "备注" = EXCLUDED."备注";

INSERT INTO "角色权限" ("角色编号", "权限编号")
SELECT r."编号", 220
FROM "角色" r
WHERE r."编号" IN (1, 3, 4, 5, 15, 1031, 1032, 1033)
ON CONFLICT DO NOTHING;

UPDATE "系统迁移版本"
SET "版本" = 113,
    "脚本" = '113_cme_credits.sql',
    "检查摘要" = '新增继续医学教育CME学分登记、讲座令牌授分、年度达标统计与催办',
    "应用时间" = CURRENT_TIMESTAMP
WHERE "编号" = 1;

COMMIT;
