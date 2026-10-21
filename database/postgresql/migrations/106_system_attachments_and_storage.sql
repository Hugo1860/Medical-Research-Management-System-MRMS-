-- 106_system_attachments_and_storage.sql
-- 方案B改造：建立系统附件元数据表、等保防篡改访问审计日志、对象存储支持与权限配置

BEGIN;

-- 1. 系统附件元数据表
CREATE TABLE IF NOT EXISTS "系统附件" (
    "编号" BIGSERIAL PRIMARY KEY,
    "业务类型" VARCHAR(64) NOT NULL,
    "业务主键" VARCHAR(64),
    "原始文件名" VARCHAR(255) NOT NULL,
    "扩展名" VARCHAR(32) NOT NULL,
    "MIME类型" VARCHAR(128) NOT NULL,
    "文件大小" BIGINT NOT NULL,
    "SHA256" CHAR(64) NOT NULL,
    "存储驱动" VARCHAR(32) NOT NULL DEFAULT 'Minio',
    "存储桶" VARCHAR(64) NOT NULL,
    "对象键名" VARCHAR(512) NOT NULL,
    "上传人编号" INTEGER REFERENCES "人员"("编号") ON DELETE SET NULL,
    "建立时间" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "有效状态" BOOLEAN NOT NULL DEFAULT FALSE,
    "引用计数" INTEGER NOT NULL DEFAULT 1,
    "备注" TEXT
);

CREATE INDEX IF NOT EXISTS "ix_系统附件_业务" ON "系统附件"("业务类型", "业务主键");
CREATE INDEX IF NOT EXISTS "ix_系统附件_SHA256" ON "系统附件"("SHA256");
CREATE INDEX IF NOT EXISTS "ix_系统附件_有效状态_建立时间" ON "系统附件"("有效状态", "建立时间");
CREATE INDEX IF NOT EXISTS "ix_系统附件_上传人" ON "系统附件"("上传人编号");

-- 2. 系统附件访问日志表 (等保三级不可变安全审计)
CREATE TABLE IF NOT EXISTS "系统附件访问日志" (
    "编号" BIGSERIAL PRIMARY KEY,
    "附件编号" BIGINT NOT NULL REFERENCES "系统附件"("编号") ON DELETE CASCADE,
    "访问动作" VARCHAR(32) NOT NULL,
    "操作人编号" INTEGER REFERENCES "人员"("编号") ON DELETE SET NULL,
    "客户端IP" VARCHAR(64),
    "UserAgent" VARCHAR(512),
    "访问时间" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "鉴权结果" VARCHAR(32) NOT NULL,
    "摘要" TEXT
);

CREATE INDEX IF NOT EXISTS "ix_系统附件访问日志_附件" ON "系统附件访问日志"("附件编号");
CREATE INDEX IF NOT EXISTS "ix_系统附件访问日志_操作人时间" ON "系统附件访问日志"("操作人编号", "访问时间");

-- 防篡改触发器函数
CREATE OR REPLACE FUNCTION fn_防篡改_系统附件访问日志()
RETURNS TRIGGER AS $$
BEGIN
    RAISE EXCEPTION '等保合规要求：系统附件访问日志不允许修改或删除';
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_防篡改_系统附件访问日志 ON "系统附件访问日志";
CREATE TRIGGER trg_防篡改_系统附件访问日志
BEFORE UPDATE OR DELETE ON "系统附件访问日志"
FOR EACH ROW EXECUTE FUNCTION fn_防篡改_系统附件访问日志();

-- 3. 权限配置 (挂在系统管理目录下)
INSERT INTO "权限" ("编号", "上级编号", "名称", "路径", "类型", "备注")
VALUES (218, 9, '附件与对象存储管理', '系统管理/系统管理/附件与对象存储管理.html', TRUE, '系统附件元数据管理、MinIO对象存储配置、受控下载与安全审计')
ON CONFLICT ("编号") DO UPDATE SET "名称"=EXCLUDED."名称", "路径"=EXCLUDED."路径", "备注"=EXCLUDED."备注";

INSERT INTO "角色权限" ("角色编号", "权限编号")
SELECT r."编号", 218 FROM "角色" r
WHERE r."名称" IN ('系统管理员','科研管理员','科技办')
  AND NOT EXISTS (SELECT 1 FROM "角色权限" rp WHERE rp."角色编号"=r."编号" AND rp."权限编号"=218);

-- 4. 更新迁移版本记录
UPDATE "系统迁移版本"
SET "版本"=106,
    "脚本"='106_system_attachments_and_storage.sql',
    "检查摘要"='新增系统附件元数据表、等保防篡改访问审计日志、对象存储配置与管理权限',
    "应用时间"=CURRENT_TIMESTAMP
WHERE "编号"=1;

COMMIT;
