-- 迁移 090：伦理 P1 访问范围、受控文件实际校验和归档元数据。
-- 只增加约束和审计元数据，不调用外部对象存储；外部适配器保持禁用时不伪造成功。
BEGIN;

ALTER TABLE "伦理电子档案"
    ADD COLUMN IF NOT EXISTS "实际校验状态" varchar(20) NOT NULL DEFAULT '待校验',
    ADD COLUMN IF NOT EXISTS "文件大小" bigint,
    ADD COLUMN IF NOT EXISTS "内容类型" varchar(200),
    ADD COLUMN IF NOT EXISTS "校验时间" timestamp without time zone,
    ADD COLUMN IF NOT EXISTS "校验错误" varchar(1000);

ALTER TABLE "伦理电子档案"
    DROP CONSTRAINT IF EXISTS "ck_伦理电子档案_实际校验状态";
ALTER TABLE "伦理电子档案"
    ADD CONSTRAINT "ck_伦理电子档案_实际校验状态"
    CHECK ("实际校验状态" IN ('已核验','失败','待校验'));

ALTER TABLE "伦理电子档案"
    DROP CONSTRAINT IF EXISTS "ck_伦理电子档案_已归档必须核验";
ALTER TABLE "伦理电子档案"
    ADD CONSTRAINT "ck_伦理电子档案_已归档必须核验"
    CHECK ("状态" <> '已归档' OR "实际校验状态" = '已核验');

CREATE INDEX IF NOT EXISTS "ix_伦理电子档案_校验状态"
    ON "伦理电子档案" ("实际校验状态", "建立时间" DESC);

-- 委员只能通过会议正式指派或回避记录获得申请范围；撤销/替补不产生披露权限。
CREATE OR REPLACE FUNCTION "fn_伦理委员可见申请"(p_人员编号 integer, p_申请编号 bigint)
RETURNS boolean LANGUAGE sql STABLE AS $$
    SELECT EXISTS (
        SELECT 1
          FROM "伦理审查会议议程" g
          JOIN "伦理审查会议委员" c ON c."会议编号" = g."会议编号"
         WHERE g."申请编号" = p_申请编号
           AND c."委员编号" = p_人员编号
           AND c."状态" IN ('正式指派','回避')
    );
$$;

UPDATE "系统迁移版本"
   SET "版本" = 90,
       "脚本" = '090_ethics_p1_scope_file_integrity.sql',
       "检查摘要" = '伦理委员指派范围、受控文件实际校验元数据与归档核验约束',
       "应用时间" = CURRENT_TIMESTAMP
 WHERE "编号" = 1;

COMMIT;
