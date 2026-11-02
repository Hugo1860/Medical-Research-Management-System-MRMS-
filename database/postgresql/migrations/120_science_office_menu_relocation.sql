-- Move existing menu entries only; do not create permissions or grant roles.
BEGIN;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM "权限" WHERE "编号"=1 AND "上级编号"=0 AND "名称"='科研办公' AND COALESCE("路径",'')='') THEN
    RAISE EXCEPTION '科研办公根菜单身份不符，停止迁移';
  END IF;
  IF (SELECT count(*) FROM "权限" WHERE ("编号","名称","路径") IN (
    (207,'教育部科研统计','科研办公/科研办公/教育部科研统计.html'),
    (209,'首页组件与业务催办','科研办公/科研办公/首页组件与业务催办.html'),
    (210,'科研文档共享与消息中心','科研办公/科研办公/科研文档共享与消息中心.html')
  )) <> 3 THEN
    RAISE EXCEPTION '目标菜单身份不符或缺失，停止迁移';
  END IF;
END $$;

UPDATE "权限" SET "上级编号"=1 WHERE "编号" IN (207,209,210) AND "上级编号"<>1;

UPDATE "系统迁移版本" SET "版本"=120,"脚本"='120_science_office_menu_relocation.sql',
  "检查摘要"='将教育部科研统计、文档消息和首页催办移至科研办公，保留原权限授权',"应用时间"=CURRENT_TIMESTAMP WHERE "编号"=1 AND "版本" <= 120;
COMMIT;
