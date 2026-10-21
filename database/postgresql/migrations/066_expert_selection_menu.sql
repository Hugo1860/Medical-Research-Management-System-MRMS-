-- 专家遴选页面：复用公开能力画像查询，供科研管理人员筛选合作专家。
BEGIN;
INSERT INTO "权限" ("编号","上级编号","名称","路径","类型","备注")
VALUES (204, 3, '专家遴选', '组织架构/专家遴选.html', TRUE, '按研究专长和技术服务能力筛选专家')
ON CONFLICT ("编号") DO UPDATE SET "上级编号"=EXCLUDED."上级编号","名称"=EXCLUDED."名称","路径"=EXCLUDED."路径","类型"=EXCLUDED."类型","备注"=EXCLUDED."备注";
INSERT INTO "角色权限" ("角色编号","权限编号")
SELECT r."编号",204 FROM "角色" r WHERE r."名称" IN ('系统管理员','科研管理员','科技办')
  AND NOT EXISTS (SELECT 1 FROM "角色权限" rp WHERE rp."角色编号"=r."编号" AND rp."权限编号"=204);
UPDATE "系统迁移版本" SET "版本"=66,"脚本"='001-066',"检查摘要"='增加专家遴选筛选页面和菜单入口' WHERE "编号"=1;
COMMIT;
