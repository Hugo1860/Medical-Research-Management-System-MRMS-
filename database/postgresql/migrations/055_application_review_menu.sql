-- P1: 为纵向项目申报菜单提供可提交、可预检查和可查询评审汇总的前端入口。
BEGIN;

UPDATE "权限"
   SET "路径" = '科研办公/科研办公/纵向项目申报.html?category=1'
 WHERE "编号" = 20101;
UPDATE "权限"
   SET "路径" = '科研办公/科研办公/纵向项目申报.html?category=2'
 WHERE "编号" = 20102;
UPDATE "权限"
   SET "路径" = '科研办公/科研办公/纵向项目申报.html?category=3'
 WHERE "编号" = 20103;

UPDATE "系统迁移版本"
   SET "版本" = 55,
       "脚本" = '001-055',
       "检查摘要" = '增加纵向项目申报预检查、提交和评审汇总前端入口'
 WHERE "编号" = 1;

COMMIT;
