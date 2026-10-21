-- P1: make the built-in 科研管理员 role usable after a fresh migration.
-- Keep organization and system administration menus restricted to their own roles.
BEGIN;

WITH RECURSIVE roots AS (
    SELECT "编号"
      FROM "权限"
     WHERE "上级编号" = 0
       AND "名称" IN ('科研办公', '纵向项目', '横向项目', '项目经费管理', '成果管理', '学术活动', '科研统计')
), menu_tree AS (
    SELECT p."编号", p."上级编号"
      FROM "权限" p
      JOIN roots r ON r."编号" = p."编号"
    UNION ALL
    SELECT child."编号", child."上级编号"
      FROM "权限" child
      JOIN menu_tree parent ON child."上级编号" = parent."编号"
)
INSERT INTO "角色权限" ("角色编号", "权限编号")
SELECT role."编号", menu."编号"
  FROM "角色" role
  CROSS JOIN menu_tree menu
 WHERE role."名称" = '科研管理员'
   AND NOT EXISTS (
       SELECT 1
         FROM "角色权限" existing
        WHERE existing."角色编号" = role."编号"
          AND existing."权限编号" = menu."编号"
   );

DO $$
DECLARE
    role_id integer;
    permission_count integer;
BEGIN
    SELECT "编号" INTO role_id FROM "角色" WHERE "名称" = '科研管理员' LIMIT 1;
    IF role_id IS NULL THEN
        RAISE EXCEPTION '科研管理员角色不存在，拒绝登记迁移';
    END IF;

    SELECT count(*) INTO permission_count FROM "角色权限" WHERE "角色编号" = role_id;
    IF permission_count = 0 THEN
        RAISE EXCEPTION '科研管理员角色仍没有菜单权限，拒绝登记迁移';
    END IF;

    IF EXISTS (
        SELECT 1
          FROM "角色权限" rp
          JOIN "权限" p ON p."编号" = rp."权限编号"
         WHERE rp."角色编号" = role_id
           AND p."名称" IN ('组织架构', '系统管理')
    ) THEN
        RAISE EXCEPTION '科研管理员不应获得组织架构或系统管理根菜单';
    END IF;
END;
$$;

UPDATE "系统迁移版本"
   SET "版本" = 42,
       "脚本" = '001-042',
       "检查摘要" = '为科研管理员补齐科研业务菜单权限，保留组织架构和系统管理边界'
 WHERE "编号" = 1;

COMMIT;
