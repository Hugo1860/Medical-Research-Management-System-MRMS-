BEGIN;

-- 1. 纠正 219 (论文投稿备案) 上级编号从 2 (纵向项目) 改为 5 (成果管理)
UPDATE "权限"
SET "上级编号" = 5
WHERE "编号" = 219;

-- 2. 纠正 220 (继续医学教育学分) 上级编号从 2 改为 1 (科研办公)
UPDATE "权限"
SET "上级编号" = 1
WHERE "编号" = 220;

-- 3. 纠正 217 (期刊分区库) 上级编号从 2 改为 5 (成果管理)
UPDATE "权限"
SET "上级编号" = 5
WHERE "编号" = 217;

-- 4. 纠正 216 (学术交流治理) 上级编号从 2 改为 6 (学术活动)
UPDATE "权限"
SET "上级编号" = 6
WHERE "编号" = 216;

-- 5. 纠正 214 (经费执行治理) 上级编号从 2 改为 4 (项目经费管理)
UPDATE "权限"
SET "上级编号" = 4
WHERE "编号" = 214;

-- 6. 纠正 215 (仪器设备与开放预约) 上级编号从 2 改为 1 (科研办公)
UPDATE "权限"
SET "上级编号" = 1
WHERE "编号" = 215;

-- 7. 新增 221 医院运营模式 权限节点 (基础信息维护)
INSERT INTO "权限" ("编号", "上级编号", "名称", "路径", "类型", "图标", "备注")
VALUES (221, 10, '医院运营模式', '基础信息维护/医院运营模式.html', true, '', '医院运营模式配置（三级综合 vs 二级/基层精简版）')
ON CONFLICT ("编号") DO UPDATE
SET "上级编号" = EXCLUDED."上级编号",
    "名称" = EXCLUDED."名称",
    "路径" = EXCLUDED."路径",
    "备注" = EXCLUDED."备注";

-- 8. 授权 221 权限给系统管理员、科研管理员、科技办
INSERT INTO "角色权限" ("角色编号", "权限编号")
SELECT r."编号", 221
FROM "角色" r
WHERE r."编号" IN (1, 5, 15)
ON CONFLICT DO NOTHING;

-- 9. 确保基层科研人员角色具备论文投稿备案 (219) 与 CME学分 (220) 权限
INSERT INTO "角色权限" ("角色编号", "权限编号")
SELECT r."编号", p."编号"
FROM "角色" r
CROSS JOIN "权限" p
WHERE r."编号" IN (1, 3, 4, 5, 15, 1031, 1032, 1033)
  AND p."编号" IN (219, 220)
ON CONFLICT DO NOTHING;

UPDATE "系统迁移版本"
SET "版本" = 114,
    "脚本" = '114_correct_permission_hierarchy_and_hospital_mode_menu.sql',
    "检查摘要" = '归位论文备案、继教学分等新功能上级菜单，新增医院运营模式独立菜单项',
    "应用时间" = CURRENT_TIMESTAMP
WHERE "编号" = 1;

COMMIT;
