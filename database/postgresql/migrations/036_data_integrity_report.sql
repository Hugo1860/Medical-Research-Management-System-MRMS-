-- P1: expose historical duplicate keys for controlled data cleanup.
BEGIN;

CREATE OR REPLACE VIEW "v1_数据完整性异常" AS
SELECT '区域'::text AS "对象", '上级编号|名称'::text AS "约束", concat_ws('|', COALESCE("上级编号"::text, '<NULL>'), COALESCE("名称"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "区域" GROUP BY "上级编号", "名称" HAVING count(*) > 1
UNION ALL
SELECT '技术领域'::text AS "对象", '上级编号|名称'::text AS "约束", concat_ws('|', COALESCE("上级编号"::text, '<NULL>'), COALESCE("名称"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "技术领域" GROUP BY "上级编号", "名称" HAVING count(*) > 1
UNION ALL
SELECT '步骤指定人'::text AS "对象", '步骤编号|指定人编号'::text AS "约束", concat_ws('|', COALESCE("步骤编号"::text, '<NULL>'), COALESCE("指定人编号"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "步骤指定人" GROUP BY "步骤编号", "指定人编号" HAVING count(*) > 1
UNION ALL
SELECT '通知公告'::text AS "对象", '通知名称'::text AS "约束", concat_ws('|', COALESCE("通知名称"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "通知公告" GROUP BY "通知名称" HAVING count(*) > 1
UNION ALL
SELECT '主办讲座'::text AS "对象", '活动名称'::text AS "约束", concat_ws('|', COALESCE("活动名称"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "主办讲座" GROUP BY "活动名称" HAVING count(*) > 1
UNION ALL
SELECT '国民行业代码'::text AS "对象", '上级编号|名称'::text AS "约束", concat_ws('|', COALESCE("上级编号"::text, '<NULL>'), COALESCE("名称"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "国民行业代码" GROUP BY "上级编号", "名称" HAVING count(*) > 1
UNION ALL
SELECT '项目参与人'::text AS "对象", '流程模板编号|项目编号|参与人编号'::text AS "约束", concat_ws('|', COALESCE("流程模板编号"::text, '<NULL>'), COALESCE("项目编号"::text, '<NULL>'), COALESCE("参与人编号"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "项目参与人" GROUP BY "流程模板编号", "项目编号", "参与人编号" HAVING count(*) > 1
UNION ALL
SELECT '纵向项目申报'::text AS "对象", '项目名称'::text AS "约束", concat_ws('|', COALESCE("项目名称"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "纵向项目申报" GROUP BY "项目名称" HAVING count(*) > 1
UNION ALL
SELECT '下载中心'::text AS "对象", '文件名称'::text AS "约束", concat_ws('|', COALESCE("文件名称"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "下载中心" GROUP BY "文件名称" HAVING count(*) > 1
UNION ALL
SELECT '讲座反馈'::text AS "对象", '讲座编号'::text AS "约束", concat_ws('|', COALESCE("讲座编号"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "讲座反馈" GROUP BY "讲座编号" HAVING count(*) > 1
UNION ALL
SELECT '经费模版'::text AS "对象", '名称'::text AS "约束", concat_ws('|', COALESCE("名称"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "经费模版" GROUP BY "名称" HAVING count(*) > 1
UNION ALL
SELECT '经费模版项目支出类型'::text AS "对象", '所属经费模版编号|项目支出类型'::text AS "约束", concat_ws('|', COALESCE("所属经费模版编号"::text, '<NULL>'), COALESCE("项目支出类型"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "经费模版项目支出类型" GROUP BY "所属经费模版编号", "项目支出类型" HAVING count(*) > 1
UNION ALL
SELECT '参加会议'::text AS "对象", '会议名称'::text AS "约束", concat_ws('|', COALESCE("会议名称"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "参加会议" GROUP BY "会议名称" HAVING count(*) > 1
UNION ALL
SELECT '经费模版项目支出内容'::text AS "对象", '所属经费模版项目支出类型编号|项目支出内容'::text AS "约束", concat_ws('|', COALESCE("所属经费模版项目支出类型编号"::text, '<NULL>'), COALESCE("项目支出内容"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "经费模版项目支出内容" GROUP BY "所属经费模版项目支出类型编号", "项目支出内容" HAVING count(*) > 1
UNION ALL
SELECT '专家评分'::text AS "对象", '流程模板编号|项目编号|评分项'::text AS "约束", concat_ws('|', COALESCE("流程模板编号"::text, '<NULL>'), COALESCE("项目编号"::text, '<NULL>'), COALESCE("评分项"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "专家评分" GROUP BY "流程模板编号", "项目编号", "评分项" HAVING count(*) > 1
UNION ALL
SELECT '参加会议反馈'::text AS "对象", '参加会议编号'::text AS "约束", concat_ws('|', COALESCE("参加会议编号"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "参加会议反馈" GROUP BY "参加会议编号" HAVING count(*) > 1
UNION ALL
SELECT '部门'::text AS "对象", '名称'::text AS "约束", concat_ws('|', COALESCE("名称"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "部门" GROUP BY "名称" HAVING count(*) > 1
UNION ALL
SELECT '成果项目'::text AS "对象", '项目编号'::text AS "约束", concat_ws('|', COALESCE("项目编号"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "成果项目" GROUP BY "项目编号" HAVING count(*) > 1
UNION ALL
SELECT '角色'::text AS "对象", '名称'::text AS "约束", concat_ws('|', COALESCE("名称"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "角色" GROUP BY "名称" HAVING count(*) > 1
UNION ALL
SELECT '成果作者'::text AS "对象", '成果类型|成果编号|作者编号'::text AS "约束", concat_ws('|', COALESCE("成果类型"::text, '<NULL>'), COALESCE("成果编号"::text, '<NULL>'), COALESCE("作者编号"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "成果作者" GROUP BY "成果类型", "成果编号", "作者编号" HAVING count(*) > 1
UNION ALL
SELECT '期刊'::text AS "对象", '名称'::text AS "约束", concat_ws('|', COALESCE("名称"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "期刊" GROUP BY "名称" HAVING count(*) > 1
UNION ALL
SELECT '期刊'::text AS "对象", '英文刊名'::text AS "约束", concat_ws('|', COALESCE("英文刊名"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "期刊" GROUP BY "英文刊名" HAVING count(*) > 1
UNION ALL
SELECT '字典'::text AS "对象", '上级编号|分类|值'::text AS "约束", concat_ws('|', COALESCE("上级编号"::text, '<NULL>'), COALESCE("分类"::text, '<NULL>'), COALESCE("值"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "字典" GROUP BY "上级编号", "分类", "值" HAVING count(*) > 1
UNION ALL
SELECT '纵向项目结题'::text AS "对象", '纵向项目编号'::text AS "约束", concat_ws('|', COALESCE("纵向项目编号"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "纵向项目结题" GROUP BY "纵向项目编号" HAVING count(*) > 1
UNION ALL
SELECT '成果著作'::text AS "对象", '著作名称'::text AS "约束", concat_ws('|', COALESCE("著作名称"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "成果著作" GROUP BY "著作名称" HAVING count(*) > 1
UNION ALL
SELECT '人员'::text AS "对象", '工号'::text AS "约束", concat_ws('|', COALESCE("工号"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "人员" GROUP BY "工号" HAVING count(*) > 1
UNION ALL
SELECT '成果专利'::text AS "对象", '专利名称'::text AS "约束", concat_ws('|', COALESCE("专利名称"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "成果专利" GROUP BY "专利名称" HAVING count(*) > 1
UNION ALL
SELECT '横向项目'::text AS "对象", '合同编号'::text AS "约束", concat_ws('|', COALESCE("合同编号"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "横向项目" GROUP BY "合同编号" HAVING count(*) > 1
UNION ALL
SELECT '成果获奖'::text AS "对象", '获奖名称'::text AS "约束", concat_ws('|', COALESCE("获奖名称"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "成果获奖" GROUP BY "获奖名称" HAVING count(*) > 1
UNION ALL
SELECT '成果论文'::text AS "对象", '论文标题'::text AS "约束", concat_ws('|', COALESCE("论文标题"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "成果论文" GROUP BY "论文标题" HAVING count(*) > 1
UNION ALL
SELECT '流程'::text AS "对象", '项目编号|流程模板编号'::text AS "约束", concat_ws('|', COALESCE("项目编号"::text, '<NULL>'), COALESCE("流程模板编号"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "流程" GROUP BY "项目编号", "流程模板编号" HAVING count(*) > 1
UNION ALL
SELECT '横向项目结题'::text AS "对象", '横向项目编号'::text AS "约束", concat_ws('|', COALESCE("横向项目编号"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "横向项目结题" GROUP BY "横向项目编号" HAVING count(*) > 1
UNION ALL
SELECT '合同买方性质'::text AS "对象", '上级编号|名称'::text AS "约束", concat_ws('|', COALESCE("上级编号"::text, '<NULL>'), COALESCE("名称"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "合同买方性质" GROUP BY "上级编号", "名称" HAVING count(*) > 1
UNION ALL
SELECT '学科'::text AS "对象", '上级编号|名称'::text AS "约束", concat_ws('|', COALESCE("上级编号"::text, '<NULL>'), COALESCE("名称"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "学科" GROUP BY "上级编号", "名称" HAVING count(*) > 1
UNION ALL
SELECT '项目分类'::text AS "对象", '上级编号|名称'::text AS "约束", concat_ws('|', COALESCE("上级编号"::text, '<NULL>'), COALESCE("名称"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "项目分类" GROUP BY "上级编号", "名称" HAVING count(*) > 1
UNION ALL
SELECT '通知公告接收条件'::text AS "对象", '通知公告编号|接收者类型|接收者编号'::text AS "约束", concat_ws('|', COALESCE("通知公告编号"::text, '<NULL>'), COALESCE("接收者类型"::text, '<NULL>'), COALESCE("接收者编号"::text, '<NULL>')) AS "重复键", count(*)::bigint AS "记录数" FROM "通知公告接收条件" GROUP BY "通知公告编号", "接收者类型", "接收者编号" HAVING count(*) > 1
;

COMMIT;
