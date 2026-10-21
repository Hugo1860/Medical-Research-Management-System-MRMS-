-- 将剩余历史菜单指向当前可访问的成果、统计、学术活动和流程入口。
BEGIN;

UPDATE "权限" SET "路径" = '成果管理/成果业务台账.html?module=overview'
 WHERE "编号" IN (50201, 50202, 50701, 50801);
UPDATE "权限" SET "路径" = '成果管理/成果业务台账.html?module=work'
 WHERE "编号" = 50301;
UPDATE "权限" SET "路径" = '成果管理/成果业务台账.html?module=patent'
 WHERE "编号" IN (50401, 50402);
UPDATE "权限" SET "路径" = '成果管理/成果业务台账.html?module=award'
 WHERE "编号" IN (50501, 50601);

UPDATE "权限" SET "路径" = '学术活动/参加学术会议/学术活动台账.html'
 WHERE "编号" = 60203;

UPDATE "权限" SET "路径" = '科研统计/科研统计台账.html?module=result'
 WHERE "编号" BETWEEN 70101 AND 70108;
UPDATE "权限" SET "路径" = '科研统计/科研统计台账.html?module=project'
 WHERE "编号" BETWEEN 70201 AND 70502;
UPDATE "权限" SET "路径" = '科研统计/科研统计台账.html?module=people'
 WHERE "编号" BETWEEN 70601 AND 70603;

UPDATE "权限" SET "路径" = '系统管理/权限管理/流程一览.html'
 WHERE "编号" = 90204;

UPDATE "系统迁移版本"
   SET "版本" = 59,
       "脚本" = '001-059',
       "检查摘要" = '补齐成果、统计、学术活动和流程菜单页面路径'
 WHERE "编号" = 1;

COMMIT;
