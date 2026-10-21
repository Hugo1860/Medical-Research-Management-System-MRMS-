-- 决策统计指标口径：将统计指标名称、维度、单位和口径说明纳入版本化管理。
BEGIN;

CREATE TABLE IF NOT EXISTS "科研统计指标口径" (
    "编号" bigserial NOT NULL,
    "指标代码" varchar(80) NOT NULL,
    "指标名称" varchar(100) NOT NULL,
    "维度" varchar(30) NOT NULL,
    "统计单位" varchar(50) NOT NULL DEFAULT '条',
    "口径说明" varchar(2000) NOT NULL,
    "版本号" integer NOT NULL DEFAULT 1,
    "状态" varchar(20) NOT NULL DEFAULT '生效',
    "建立人编号" integer,
    "建立时间" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "更新时间" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "pk_科研统计指标口径" PRIMARY KEY ("编号"),
    CONSTRAINT "uq_科研统计指标口径_代码版本" UNIQUE ("指标代码", "版本号"),
    CONSTRAINT "ck_科研统计指标口径_维度" CHECK ("维度" IN ('overview','project','result','people')),
    CONSTRAINT "ck_科研统计指标口径_状态" CHECK ("状态" IN ('草稿','生效','停用')),
    CONSTRAINT "ck_科研统计指标口径_版本" CHECK ("版本号" > 0)
);

INSERT INTO "科研统计指标口径" ("指标代码","指标名称","维度","统计单位","口径说明","版本号","状态")
VALUES
    ('vertical-project','纵向项目','overview','个','已立项纵向项目视图中的项目数量，数值为批准经费，金额单位为元。',1,'生效'),
    ('horizontal-project','横向项目','overview','个','已登记横向项目合同视图中的项目数量，数值为合同金额，金额单位为元。',1,'生效'),
    ('research-result','科研成果','result','条','成果总览视图中的论文、著作、专利和获奖记录数量。',1,'生效'),
    ('research-people','科研人员','people','人','人员主数据总数；钻取结果按部门展示总人数和启用人数。',1,'生效')
ON CONFLICT ("指标代码","版本号") DO UPDATE SET
    "指标名称"=EXCLUDED."指标名称",
    "维度"=EXCLUDED."维度",
    "统计单位"=EXCLUDED."统计单位",
    "口径说明"=EXCLUDED."口径说明",
    "状态"=EXCLUDED."状态",
    "更新时间"=CURRENT_TIMESTAMP;

UPDATE "系统迁移版本"
   SET "版本"=67,"脚本"='001-067',"检查摘要"='增加科研统计指标口径版本管理'
 WHERE "编号"=1;

COMMIT;
