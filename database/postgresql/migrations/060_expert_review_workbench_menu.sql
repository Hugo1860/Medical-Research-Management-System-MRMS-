-- 增加专家评审工作台菜单，并将其授权给科研管理相关角色。
BEGIN;

INSERT INTO "权限" ("编号", "上级编号", "名称", "路径", "类型", "备注")
VALUES (805, 8, '专家评审工作台', '组织架构/专家评审工作台.html', TRUE, '匿名评审待办与利益冲突检查')
ON CONFLICT ("编号") DO UPDATE
SET "上级编号" = EXCLUDED."上级编号",
    "名称" = EXCLUDED."名称",
    "路径" = EXCLUDED."路径",
    "类型" = EXCLUDED."类型",
    "备注" = EXCLUDED."备注";

INSERT INTO "角色权限" ("角色编号", "权限编号")
SELECT r."编号", 805
  FROM "角色" r
 WHERE r."名称" IN ('系统管理员', '科研管理员', '科技办')
   AND NOT EXISTS (
       SELECT 1 FROM "角色权限" rp
        WHERE rp."角色编号" = r."编号" AND rp."权限编号" = 805
   );

UPDATE "系统迁移版本"
   SET "版本" = 60,
       "脚本" = '001-060',
       "检查摘要" = '增加匿名专家评审工作台菜单和科研管理角色授权'
 WHERE "编号" = 1;

COMMIT;
