-- P0: 将项目和经费菜单指向实际存在的统一业务台账页面。
-- 历史菜单路径对应的前端页面在迁移后未随发布包保留，点击后会落到 404。
BEGIN;

UPDATE "权限" SET "路径" = '科研办公/科研办公/科研业务台账.html?module=y-application'
 WHERE "编号" IN (20101, 20102, 20103);

UPDATE "权限" SET "路径" = '科研办公/科研办公/科研业务台账.html?module=y-approval'
 WHERE "编号" IN (20201, 20202, 20203, 20301, 20302, 20303);

UPDATE "权限" SET "路径" = '科研办公/科研办公/科研业务台账.html?module=y-midterm'
 WHERE "编号" = 20401;
UPDATE "权限" SET "路径" = '科研办公/科研办公/科研业务台账.html?module=y-change'
 WHERE "编号" = 20402;
UPDATE "权限" SET "路径" = '科研办公/科研办公/科研业务台账.html?module=y-close'
 WHERE "编号" = 20403;
UPDATE "权限" SET "路径" = '科研办公/科研办公/科研业务台账.html?module=y-finished'
 WHERE "编号" = 20501;

UPDATE "权限" SET "路径" = '科研办公/科研办公/科研业务台账.html?module=x-list'
 WHERE "编号" = 30101;
UPDATE "权限" SET "路径" = '科研办公/科研办公/科研业务台账.html?module=x-change'
 WHERE "编号" = 30102;
UPDATE "权限" SET "路径" = '科研办公/科研办公/科研业务台账.html?module=x-close'
 WHERE "编号" = 30103;

UPDATE "权限" SET "路径" = '科研办公/科研办公/科研业务台账.html?module=y-income'
 WHERE "编号" IN (40101, 40102);
UPDATE "权限" SET "路径" = '科研办公/科研办公/科研业务台账.html?module=y-expense'
 WHERE "编号" IN (40103, 40104);
UPDATE "权限" SET "路径" = '科研办公/科研办公/科研业务台账.html?module=x-income'
 WHERE "编号" IN (40201, 40202);
UPDATE "权限" SET "路径" = '科研办公/科研办公/科研业务台账.html?module=x-expense'
 WHERE "编号" IN (40203, 40205);
UPDATE "权限" SET "路径" = '科研办公/科研办公/权限代理与预算对账.html'
 WHERE "编号" IN (40204, 40301, 40302);
UPDATE "权限" SET "路径" = '科研办公/科研办公/科研业务台账.html?module=bill-search'
 WHERE "编号" = 40401;

UPDATE "系统迁移版本"
   SET "版本" = 53,
       "脚本" = '001-053',
       "检查摘要" = '修复项目和经费菜单路径，统一指向可访问的业务台账页面'
 WHERE "编号" = 1;

COMMIT;
