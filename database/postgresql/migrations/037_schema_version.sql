-- P0: record and validate the schema level reached by the ordered migrations.
BEGIN;

CREATE TABLE IF NOT EXISTS "系统迁移版本" (
    "编号" integer PRIMARY KEY CHECK ("编号" = 1),
    "版本" integer NOT NULL,
    "脚本" varchar(255) NOT NULL,
    "检查摘要" varchar(1000),
    "应用时间" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

DO $$
DECLARE
    v_tables integer;
    v_views integer;
BEGIN
    SELECT count(*) INTO v_tables
      FROM pg_class
     WHERE relkind = 'r' AND relnamespace = 'public'::regnamespace;
    SELECT count(*) INTO v_views
      FROM pg_views
     WHERE schemaname = 'public';

    IF v_tables < 62 OR v_views < 52 THEN
        RAISE EXCEPTION '数据库对象数量不足，拒绝登记迁移版本：tables=%, views=%', v_tables, v_views;
    END IF;
    IF to_regclass('public."v1_数据完整性异常"') IS NULL
       OR to_regclass('public."预算异常对账处理"') IS NULL
       OR to_regprocedure('public."tfn_预算异常对账"(integer,integer)') IS NULL THEN
        RAISE EXCEPTION '关键 PostgreSQL 迁移对象缺失，拒绝登记迁移版本';
    END IF;
END;
$$;

INSERT INTO "系统迁移版本" ("编号", "版本", "脚本", "检查摘要")
VALUES (1, 37, '001-037', '62 张表、52 个视图、预算对账、权限代理和数据完整性校验已登记')
ON CONFLICT ("编号") DO UPDATE SET
    "版本" = EXCLUDED."版本",
    "脚本" = EXCLUDED."脚本",
    "检查摘要" = EXCLUDED."检查摘要",
    "应用时间" = CURRENT_TIMESTAMP;

COMMIT;
