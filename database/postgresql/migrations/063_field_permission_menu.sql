-- 将字段级授权配置纳入系统管理菜单。
BEGIN;

INSERT INTO "权限" ("编号", "上级编号", "名称", "路径", "类型", "备注")
VALUES (90205, 902, '字段级授权', '系统管理/权限管理/字段授权.html', TRUE, '配置资源字段读取和写入权限')
ON CONFLICT ("编号") DO UPDATE
SET "上级编号"=EXCLUDED."上级编号", "名称"=EXCLUDED."名称", "路径"=EXCLUDED."路径", "类型"=EXCLUDED."类型", "备注"=EXCLUDED."备注";

INSERT INTO "角色权限" ("角色编号", "权限编号")
SELECT r."编号", 90205 FROM "角色" r
 WHERE r."名称"='系统管理员'
   AND NOT EXISTS (SELECT 1 FROM "角色权限" rp WHERE rp."角色编号"=r."编号" AND rp."权限编号"=90205);

UPDATE "系统迁移版本"
   SET "版本"=63, "脚本"='001-063', "检查摘要"='增加字段级授权系统管理菜单入口'
 WHERE "编号"=1;

COMMIT;
