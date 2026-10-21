-- 补齐伦理审查工作台菜单权限。
-- 074 已在部分环境先执行表结构，后续修订才加入菜单插入；本迁移仅做幂等
-- 权限/角色补丁，不重复修改伦理表结构。
BEGIN;

INSERT INTO "权限" ("编号", "上级编号", "名称", "路径", "类型", "备注")
VALUES (806, 1, '科研伦理审查', '科研办公/科研办公/科研伦理审查.html', TRUE,
        '伦理申请、委员会决定和持续审查记录')
ON CONFLICT ("编号") DO UPDATE
   SET "上级编号" = EXCLUDED."上级编号",
       "名称" = EXCLUDED."名称",
       "路径" = EXCLUDED."路径",
       "类型" = EXCLUDED."类型",
       "备注" = EXCLUDED."备注";

INSERT INTO "角色权限" ("角色编号", "权限编号")
SELECT r."编号", 806
  FROM "角色" r
 WHERE r."名称" IN ('系统管理员', '科研管理员', '科技办', '科研科负责人', '科研处处长', '伦理秘书', '伦理委员会秘书', '伦理委员', '伦理委员会委员')
   AND NOT EXISTS (
       SELECT 1
         FROM "角色权限" rp
        WHERE rp."角色编号" = r."编号"
          AND rp."权限编号" = 806
   );

UPDATE "系统迁移版本"
   SET "版本" = 76,
       "脚本" = '076_ethics_menu_permission_patch.sql',
       "检查摘要" = '补齐科研伦理审查工作台菜单权限及治理角色授权',
       "应用时间" = CURRENT_TIMESTAMP
 WHERE "编号" = 1;

COMMIT;
