-- 110: 科研服务事项从人员库配置办理人员。
-- 材料模板继续保存系统上传后生成的访问路径，管理端不再要求手工填写路径。
BEGIN;

ALTER TABLE "科研服务事项"
    ADD COLUMN IF NOT EXISTS "办理人员编号" integer REFERENCES "人员"("编号") ON DELETE SET NULL;

CREATE INDEX IF NOT EXISTS "ix_科研服务事项_办理人员"
    ON "科研服务事项" ("办理人员编号");

UPDATE "系统迁移版本"
SET "版本" = 110,
    "脚本" = '110_research_service_handler.sql',
    "检查摘要" = '科研服务事项支持从人员库选择办理人员和上传材料模板',
    "应用时间" = CURRENT_TIMESTAMP
WHERE "编号" = 1;

COMMIT;
