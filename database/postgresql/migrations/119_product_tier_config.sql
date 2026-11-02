BEGIN;

-- 三层产品配置切片：保留旧运营模式，并将二级精简兼容映射到基层核心版。
ALTER TABLE "医院运营配置"
    ADD COLUMN IF NOT EXISTS "产品层级" varchar(32) NOT NULL DEFAULT '综合科研平台版',
    ADD COLUMN IF NOT EXISTS "字段策略" jsonb NOT NULL DEFAULT '{
      "required": ["项目中文名称", "负责人编号"],
      "optional": ["项目英文名称", "学科类型", "研究类型"],
      "hidden": []
    }'::jsonb,
    ADD COLUMN IF NOT EXISTS "流程策略" jsonb NOT NULL DEFAULT '{
      "enabled": ["立项申请", "伦理审查", "经费执行", "成果归档"],
      "approval": "分级审批"
    }'::jsonb,
    ADD COLUMN IF NOT EXISTS "统计指标策略" jsonb NOT NULL DEFAULT '{
      "enabled": ["纵向项目", "横向项目", "科研成果", "科研人员"],
      "labels": {}
    }'::jsonb;

ALTER TABLE "医院运营配置"
    DROP CONSTRAINT IF EXISTS "ck_医院运营配置_产品层级";
ALTER TABLE "医院运营配置"
    ADD CONSTRAINT "ck_医院运营配置_产品层级"
    CHECK ("产品层级" IN ('基层核心版', '医院增强版', '综合科研平台版'));

UPDATE "医院运营配置"
   SET "产品层级" = CASE WHEN "运营模式" = '二级精简' THEN '基层核心版' ELSE COALESCE("产品层级", '综合科研平台版') END,
       "字段策略" = CASE WHEN "运营模式" = '二级精简' THEN
         '{"required":["项目中文名称","负责人编号"],"optional":["项目分类","研究类型"],"hidden":["项目英文名称","国际合作信息"]}'::jsonb
         ELSE "字段策略" END,
       "流程策略" = CASE WHEN "运营模式" = '二级精简' THEN
         '{"enabled":["立项申请","成果归档"],"approval":"简化审批"}'::jsonb
         ELSE "流程策略" END,
       "统计指标策略" = CASE WHEN "运营模式" = '二级精简' THEN
         '{"enabled":["纵向项目","科研成果","科研人员"],"labels":{}}'::jsonb
         ELSE "统计指标策略" END;

UPDATE "系统迁移版本"
   SET "版本" = 119,
       "脚本" = '119_product_tier_config.sql',
       "检查摘要" = '新增三层产品层级及字段、流程、统计指标策略，兼容二级精简映射基层核心版',
       "应用时间" = CURRENT_TIMESTAMP
 WHERE "编号" = 1;

COMMIT;
