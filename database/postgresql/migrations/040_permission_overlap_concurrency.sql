-- P0: serialize delegation writes so overlapping grants cannot pass a race.
BEGIN;

CREATE OR REPLACE FUNCTION "trg_权限代理_禁止重叠"()
RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
    v_lock_key bigint;
BEGIN
    IF NEW."撤销时间" IS NOT NULL THEN
        RETURN NEW;
    END IF;

    v_lock_key := hashtextextended(
        concat_ws(':', NEW."授权人编号", NEW."被代理人编号", NEW."代理范围"),
        0
    );
    PERFORM pg_advisory_xact_lock(v_lock_key);

    IF EXISTS (
        SELECT 1
          FROM "权限代理" d
         WHERE d."编号" <> COALESCE(NEW."编号", 0)
           AND d."授权人编号" = NEW."授权人编号"
           AND d."被代理人编号" = NEW."被代理人编号"
           AND d."代理范围" = NEW."代理范围"
           AND d."撤销时间" IS NULL
           AND d."开始时间" < NEW."结束时间"
           AND d."结束时间" > NEW."开始时间"
    ) THEN
        RAISE EXCEPTION '同一代理范围存在重叠的有效授权';
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS "trg_权限代理_禁止重叠" ON "权限代理";
CREATE TRIGGER "trg_权限代理_禁止重叠"
BEFORE INSERT OR UPDATE OF "授权人编号", "被代理人编号", "代理范围", "开始时间", "结束时间", "撤销时间"
ON "权限代理"
FOR EACH ROW EXECUTE FUNCTION "trg_权限代理_禁止重叠"();

UPDATE "系统迁移版本"
   SET "版本" = 40,
       "脚本" = '001-040',
       "检查摘要" = '代理授权增加并发串行化，禁止同一范围产生重叠有效授权'
 WHERE "编号" = 1;

COMMIT;
