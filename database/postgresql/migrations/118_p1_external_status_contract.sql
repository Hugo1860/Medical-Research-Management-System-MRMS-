-- P1：统一外部系统的可审计状态代码。
-- 状态代码表达接入事实；最近连接状态保留为既有中文展示/兼容字段。
BEGIN;

ALTER TABLE "外部系统"
    ADD COLUMN IF NOT EXISTS "状态代码" varchar(20) NOT NULL DEFAULT 'unconfigured';

UPDATE "外部系统"
   SET "状态代码" = CASE
       WHEN "接入方式" = '人工导入' THEN 'manual_import'
       WHEN "最近连接状态" = '已连接' THEN 'connected'
       WHEN "最近连接状态" = '失败' THEN 'failed'
       ELSE 'unconfigured'
   END;

ALTER TABLE "外部系统"
    ADD CONSTRAINT "ck_外部系统_状态代码"
    CHECK ("状态代码" IN ('connected', 'unconfigured', 'failed', 'manual_import'));

CREATE INDEX IF NOT EXISTS "ix_外部系统_状态代码"
    ON "外部系统" ("系统类型", "状态代码", "是否启用");

UPDATE "系统迁移版本"
   SET "版本" = 118,
       "脚本" = '118_p1_external_status_contract.sql',
       "检查摘要" = '统一外部系统 connected、unconfigured、failed、manual_import 四态，并保留连接审计时间与错误',
       "应用时间" = CURRENT_TIMESTAMP
 WHERE "编号" = 1;

COMMIT;
