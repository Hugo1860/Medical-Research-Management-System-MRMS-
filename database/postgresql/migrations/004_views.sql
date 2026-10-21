-- Translated from SQL Server views with sqlglot.
-- Review this file against the original view definitions before production use.
BEGIN;

CREATE OR REPLACE VIEW "v1_技术领域" AS SELECT "a"."编号", "a"."上级编号", "a"."名称", "a"."级别", "a"."排序", CASE WHEN EXISTS(SELECT 1 FROM "技术领域" AS "b" WHERE "b"."上级编号" = "a"."编号") THEN 1 ELSE 0 END AS "是否存在下级", "a"."备注" FROM "技术领域" AS "a";

CREATE OR REPLACE VIEW "v1_区域" AS SELECT "a"."编号", "a"."上级编号", "a"."名称", "a"."级别", "a"."排序", CASE WHEN EXISTS(SELECT 1 FROM "区域" AS "b" WHERE "b"."上级编号" = "a"."编号") THEN 1 ELSE 0 END AS "是否存在下级", "a"."备注" FROM "区域" AS "a";

CREATE OR REPLACE VIEW "v1_人员" AS SELECT "编号", "工号", "姓名" /* 密码 , */, "部门编号", "英文名", "性别", "身份证", "出生日期", "入职日期", "手机", "QQ", "邮箱", "籍贯", "毕业日期", "合同开始日期", "合同结束日期", "参加工作日期", "专业技术获得日期", "职务任职日期", "学位", "学历", "政治面貌", "技术职称类别", "技术职称", "第二技术职称类别", "第二技术职称", "婚姻情况", "学制", "学历性质", "学习形式", "人员类别", "个人身份", "民族", "专业技术级别", "所学专业名称", "岗位性质", "职务名称", "职务级别", "岗位名称", "岗位类型", "学科门类", "一级学科", "二级学科", "三级学科", "人员类型", "获得最高学历的院校", "获得最高学位的院校", "住宅地址", "学术特长", "研究方向", "个人获得的荣誉和奖项", "备注", "是否启用" FROM "人员";

CREATE OR REPLACE VIEW "v1_角色" AS SELECT * FROM "角色" AS "a" LEFT JOIN LATERAL (SELECT COUNT(1) AS "人数" FROM "人员角色" AS "b" WHERE "b"."角色编号" = "a"."编号") AS "c" ON TRUE LEFT JOIN LATERAL (SELECT COUNT(1) AS "功能数" FROM "角色权限" AS "d" JOIN "权限" AS "d1" ON "d"."权限编号" = "d1"."编号" WHERE "d"."角色编号" = "a"."编号") AS "e" ON TRUE;

CREATE OR REPLACE VIEW "v1_预算已报销详情" AS SELECT "x".*, "x"."批准经费" + "x"."配套经费" AS "经费", COALESCE("c"."已报销金额", 0) AS "已报销金额" FROM "项目经费预算" AS "x" LEFT JOIN LATERAL (SELECT SUM("b"."报销金额") AS "已报销金额" FROM "报销" AS "a" JOIN "流程" AS "a1" ON "a1"."流程模板编号" = 600 AND "a"."编号" = "a1"."项目编号" AND "a1"."状态" IN (0, 1) JOIN "报销详情" AS "b" ON "a"."编号" = "b"."报销编号" WHERE "a"."流程模板编号" = "x"."流程模板编号" AND "a"."项目编号" = "x"."项目编号" AND "b"."项目支出类型" = "x"."项目支出类型" AND "b"."项目支出内容" = "x"."项目支出内容") AS "c" ON TRUE;

CREATE OR REPLACE VIEW "v1_成果论文_列表" AS SELECT "a"."编号", "a"."论文标题", "a"."论文Online日期", "a"."论文正式出版日期", COALESCE("b"."名称", "b"."英文刊名") AS "刊物名称", "a"."认领人编号", fn_成果作者(1, "a"."编号", '第一作者') AS "第一作者" FROM "成果论文" AS "a" LEFT JOIN "期刊" AS "b" ON "a"."刊物编号" = "b"."编号";

CREATE OR REPLACE VIEW "v1_学科" AS SELECT "a"."编号", "a"."上级编号", "a"."名称", "a"."级别", "a"."排序", CASE WHEN EXISTS(SELECT 1 FROM "学科" AS "b" WHERE "b"."上级编号" = "a"."编号") THEN 1 ELSE 0 END AS "是否存在下级", "a"."备注" FROM "学科" AS "a";

CREATE OR REPLACE VIEW "v1_成果论文_详情" AS SELECT "a"."编号", "a"."认领人编号", "a"."论文标题", "a"."论文标题类型", "a"."论文Online日期", "a"."论文正式出版日期", "a"."他引次数", "a"."年度", "a"."收稿日期", "a"."论文所属栏目", "a"."版面费", "a"."超额版面费", "a"."超额费用来源", "a"."奖励情况", "a"."论文字数", "a"."计划来源", "a"."论文版面", "a"."学科门类", "a"."一级学科", "a"."二级学科", "a"."三级学科", "a"."作者人数", "a"."关键字", "a"."论文摘要", "a"."备注", "a"."刊物编号", "a"."卷号", "a"."期号", "a"."页码范围起", "a"."页码范围止", "a"."论文类型", "a"."是否与行业联合发表", "a"."是否与地方联合发表", "a"."是否与国际联合发表", "a"."是否跨学科", "a"."是否与本专业相关", "a"."检索信息页相关文件路径", "a"."论文正文路径", "a"."其他相关文件路径", "b"."代码", COALESCE("b"."名称", "b"."英文刊名") AS "刊物名称", "b"."主办单位", "b"."期刊系列", "b"."JCR分区", "b"."编目名称", "b"."编目类别", "b"."创刊日期", "b"."被引用次数", "b"."影响因子", "b"."特征因子分值", "b"."ISSN号", "b"."CN号" FROM "成果论文" AS "a" LEFT JOIN "期刊" AS "b" ON "a"."刊物编号" = "b"."编号";

CREATE OR REPLACE VIEW "v1_成果著作_列表" AS SELECT "a"."编号", "a"."著作名称", "a"."著作出版日期", "a"."年度", "a"."出版社名称", "a"."著作类型", fn_成果作者(2, "a"."编号", '主编') AS "主编" FROM "成果著作" AS "a";

CREATE OR REPLACE VIEW "v1_成果著作_详情" AS SELECT "a"."编号", "a"."著作名称", "a"."出版社名称", "a"."著作形式", "a"."年度", "a"."著作出版日期", "a"."ISBN号", "a"."著作字数", "a"."奖励情况", "a"."出版地", "a"."出版社级别", "a"."著作类型", "a"."是否译为外文", "a"."语种", "a"."学科门类", "a"."一级学科", "a"."二级学科", "a"."三级学科", "a"."作者人数", "a"."发行价", "a"."备注", "a"."封面相关文件路径", "a"."版权页相关文件路径", "a"."前言页相关文件路径", "a"."完整目录页相关文件路径", "a"."其他相关文件路径" FROM "成果著作" AS "a";

CREATE OR REPLACE VIEW "v1_部门" AS SELECT "a"."编号", "a"."上级部门编号", "a"."名称", "a"."负责人编号", "b"."姓名" AS "负责人姓名", "a"."排序值", "a"."备注", "a"."是否启用", CASE WHEN EXISTS(SELECT 1 FROM "部门" AS "b" WHERE "b"."上级部门编号" = "a"."编号") THEN 1 ELSE 0 END AS "是否存在下级" FROM "部门" AS "a" LEFT JOIN "人员" AS "b" ON "a"."负责人编号" = "b"."编号";

CREATE OR REPLACE VIEW "v1_成果专利_列表" AS SELECT "a"."编号", "a"."专利名称", "a"."专利权人名称", "a"."专利类型", "a"."是否授权", "a"."年度", fn_成果作者(3, "a"."编号", '第一') AS "第一作者" FROM "成果专利" AS "a";

CREATE OR REPLACE VIEW "v1_成果专利_详情" AS SELECT "a".* FROM "成果专利" AS "a";

CREATE OR REPLACE VIEW "v1_成果获奖_详情" AS SELECT "a".* FROM "成果获奖" AS "a";

CREATE OR REPLACE VIEW "v1_项目分类" AS SELECT "a"."编号", "a"."上级编号", "a"."名称", "a"."级别", "a"."排序", "a"."备注", CASE WHEN EXISTS(SELECT 1 FROM "项目分类" AS "b" WHERE "b"."上级编号" = "a"."编号") THEN 1 ELSE 0 END AS "是否存在下级" FROM "项目分类" AS "a";

CREATE OR REPLACE VIEW "v1_通知公告接收条件" AS SELECT "编号", "通知公告编号", "接收者类型", CASE "接收者类型" WHEN 0 THEN '所有人' WHEN 10 THEN '人员' WHEN 20 THEN '部门' WHEN 30 THEN '角色' END AS "接收者类型名称", "接收者编号", CASE "接收者类型" WHEN 0 THEN '所有人' WHEN 10 THEN (SELECT "姓名" FROM "人员" WHERE "编号" = "接收者编号") WHEN 20 THEN (SELECT "名称" FROM "部门" WHERE "编号" = "接收者编号") WHEN 30 THEN (SELECT "名称" FROM "角色" WHERE "编号" = "接收者编号") END AS "接收者名称" FROM "通知公告接收条件";

CREATE OR REPLACE VIEW "v1_国民行业代码" AS SELECT "a"."编号", "a"."上级编号", "a"."名称", "a"."代码", "a"."级别", "a"."排序", CASE WHEN EXISTS(SELECT 1 FROM "国民行业代码" AS "b" WHERE "b"."上级编号" = "a"."编号") THEN 1 ELSE 0 END AS "是否存在下级", "a"."备注" FROM "国民行业代码" AS "a";

CREATE OR REPLACE VIEW "v1_期刊" AS SELECT *, CONCAT("名称", '/', "英文刊名", '/', "主办单位", '/', "年度") AS "AllName" FROM "期刊";

CREATE OR REPLACE VIEW "v1_合同买方性质" AS SELECT "a"."编号", "a"."上级编号", "a"."名称", "a"."级别", "a"."排序", CASE WHEN EXISTS(SELECT 1 FROM "合同买方性质" AS "b" WHERE "b"."上级编号" = "a"."编号") THEN 1 ELSE 0 END AS "是否存在下级", "a"."备注" FROM "合同买方性质" AS "a";

CREATE OR REPLACE VIEW "v1_角色的权限" AS SELECT *, CAST(1 AS BIT) AS "拥有" FROM "权限";

CREATE OR REPLACE VIEW "v1_人员_带部门名" AS SELECT "a"."编号", "a"."工号", "a"."密码", "a"."姓名", "a"."部门编号", "a"."英文名", "a"."性别", "a"."身份证", "a"."出生日期", "a"."入职日期", "a"."手机", "a"."QQ", "a"."邮箱", "a"."籍贯", "a"."毕业日期", "a"."合同开始日期", "a"."合同结束日期", "a"."参加工作日期", "a"."专业技术获得日期", "a"."职务任职日期", "a"."学位", "a"."学历", "a"."政治面貌", "a"."技术职称类别", "a"."技术职称", "a"."第二技术职称类别", "a"."第二技术职称", "a"."婚姻情况", "a"."学制", "a"."学历性质", "a"."学习形式", "a"."人员类别", "a"."个人身份", "a"."民族", "a"."专业技术级别", "a"."所学专业名称", "a"."岗位性质", "a"."职务名称", "a"."职务级别", "a"."岗位名称", "a"."岗位类型", "a"."学科门类", "a"."一级学科", "a"."二级学科", "a"."三级学科", "a"."人员类型", "a"."获得最高学历的院校", "a"."获得最高学位的院校", "a"."住宅地址", "a"."学术特长", "a"."研究方向", "a"."个人获得的荣誉和奖项", "a"."备注", "a"."是否启用", "b"."名称" AS "部门名称", CONCAT("a"."工号", '/', "a"."姓名", '/', "b"."名称") AS "AllName" FROM "人员" AS "a" LEFT JOIN "部门" AS "b" ON "a"."部门编号" = "b"."编号";

CREATE OR REPLACE VIEW "v2_主办讲座" AS SELECT "a"."编号", "a"."活动名称", "a"."所属部门编号", "c"."名称" AS "所属部门名称", "a"."活动经费", "a"."会议类型", "a"."活动对象", "a"."开始时间", "a"."结束时间", "a"."主办人编号", "a"."主讲人姓名", "a"."主讲人国别", "a"."主讲人单位", "a"."主讲人学位", "a"."主讲人职称", "a"."主讲人简历路径", "a"."讲座地点", "a"."内容概述", "b"."工号" AS "主办人工号", "b"."姓名" AS "主办人姓名", "b"."部门名称" AS "主办人部门名称" FROM "主办讲座" AS "a" LEFT JOIN "v1_人员_带部门名" AS "b" ON "a"."主办人编号" = "b"."编号" LEFT JOIN "部门" AS "c" ON "a"."所属部门编号" = "c"."编号";

CREATE OR REPLACE VIEW "v2_通知公告" AS SELECT "a"."编号", "a"."通知类型", "a"."通知标识", "a"."发送人编号", "a"."通知名称", "a"."关闭时间", "a"."相关文件路径", "a"."通知内容", "a"."接收人数", "a"."已接收人数", "a"."是否启用", "a"."建立时间", "a"."备注", "b"."工号" AS "发送人工号", "b"."姓名" AS "发送人姓名", "b"."部门编号" AS "发送人部门编号", "b"."部门名称" AS "发送人部门名称" FROM "通知公告" AS "a" LEFT JOIN "v1_人员_带部门名" AS "b" ON "a"."发送人编号" = "b"."编号";

CREATE OR REPLACE VIEW "v2_项目参与人" AS SELECT "a"."编号", "a"."流程模板编号", "a"."项目编号", "a"."参与人编号", "b"."工号", "b"."姓名", "b"."部门编号", "b"."部门名称", "a"."工作量占比", "a"."备注", "a"."建立时间" FROM "项目参与人" AS "a" JOIN "v1_人员_带部门名" AS "b" ON "a"."参与人编号" = "b"."编号";

CREATE OR REPLACE VIEW "v2_纵向项目申报" AS SELECT "a"."编号", "a"."分类", "a"."项目名称", "a"."申报人编号", "b"."工号", "b"."姓名", "b"."部门编号", "b"."部门名称", "a"."学科门类", "a"."学科类型", "a"."申报年度", "a"."项目类型", "a"."项目级别", "a"."项目来源单位", "a"."项目类别", "a"."申报时间", "a"."申请经费", "a"."计划开始日期", "a"."计划完成日期", "a"."项目等级", "a"."是否委托外单位进行实验或实验分析", "a"."实验委托检测内容", "a"."项目摘要", "a"."是否需要伦理评审", "a"."是否需要伦理批文", "a"."伦理批文编号", "a"."伦理批文路径", "a"."项目申报书路径", "a"."备注", "a"."建立时间", "a"."是否立项" FROM "纵向项目申报" AS "a" LEFT JOIN "v1_人员_带部门名" AS "b" ON "a"."申报人编号" = "b"."编号";

CREATE OR REPLACE VIEW "v2_纵向项目申报_列表" AS SELECT "a"."编号", "a"."分类", CASE "a"."分类" WHEN 1 THEN '科研项目' WHEN 2 THEN '重点学科' WHEN 3 THEN '人才培养' END AS "分类名称", "a"."项目名称", "a"."申报人编号", "b"."工号", "b"."姓名", "b"."部门编号", "b"."部门名称", "a"."学科门类", "a"."项目类型", "a"."项目级别", "a"."项目来源单位", "a"."项目类别", "a"."申报时间", "a"."申请经费", "a"."计划开始日期", "a"."计划完成日期", "a"."是否立项" FROM "纵向项目申报" AS "a" LEFT JOIN "v1_人员_带部门名" AS "b" ON "a"."申报人编号" = "b"."编号";

CREATE OR REPLACE VIEW "v2_下载中心" AS SELECT "a".*, "b"."工号" AS "上传人工号", "b"."姓名" AS "上传人姓名", "b"."部门编号" AS "上传人部门编号", "b"."部门名称" AS "上传人部门名称" FROM "下载中心" AS "a" LEFT JOIN "v1_人员_带部门名" AS "b" ON "a"."上传人编号" = "b"."编号";

CREATE OR REPLACE VIEW "v2_参加会议" AS SELECT "a".*, "b"."工号" AS "申请人工号", "b"."姓名" AS "申请人姓名", "b"."部门名称" AS "申请人部门名称" FROM "参加会议" AS "a" LEFT JOIN "v1_人员_带部门名" AS "b" ON "a"."申请人编号" = "b"."编号";

CREATE OR REPLACE VIEW "v2_纵向项目立项_详情" AS SELECT "a".*, "b"."名称" AS "成果归属部门名称", "c"."工号" AS "负责人工号", "c"."姓名" AS "负责人姓名", "c"."部门编号" AS "负责人部门编号", "c"."部门名称" AS "负责人部门名称", "d"."姓名" AS "登记人姓名", "d"."部门编号" AS "登记人部门编号", "d"."部门名称" AS "登记人部门名称" FROM "纵向项目立项" AS "a" LEFT JOIN "部门" AS "b" ON "a"."成果归属部门编号" = "b"."编号" LEFT JOIN "v1_人员_带部门名" AS "c" ON "a"."负责人编号" = "c"."编号" LEFT JOIN "v1_人员_带部门名" AS "d" ON "a"."登记人编号" = "c"."编号";

CREATE OR REPLACE VIEW "v2_纵向项目立项_列表" AS SELECT "a"."编号", "a"."分类", CASE "a"."分类" WHEN 1 THEN '科研项目' WHEN 2 THEN '重点学科' WHEN 3 THEN '人才培养' END AS "分类名称", "a"."项目中文名称", "a"."项目英文名称", "a"."项目类型", "a"."项目级别", "a"."项目来源单位", "a"."项目类别", "a"."批准经费", "a"."配套经费", "a"."计划开始日期", "a"."计划完成日期", "b"."名称" AS "成果归属部门名称", "a"."负责人编号", "c"."工号" AS "负责人工号", "c"."姓名" AS "负责人姓名", "c"."部门编号" AS "负责人部门编号", "c"."部门名称" AS "负责人部门名称", "a"."登记人编号", "d"."姓名" AS "登记人姓名" FROM "纵向项目立项" AS "a" LEFT JOIN "部门" AS "b" ON "a"."成果归属部门编号" = "b"."编号" LEFT JOIN "v1_人员_带部门名" AS "c" ON "a"."负责人编号" = "c"."编号" LEFT JOIN "人员" AS "d" ON "a"."登记人编号" = "d"."编号";

CREATE OR REPLACE VIEW "v2_项目经费到账" AS SELECT "a"."编号", "a"."流程模板编号", "a"."项目编号", "a"."拨款单位", "a"."到账日期", "a"."到账金额", "a"."技术交易额", "a"."管理费用", "a"."技术合同认定服务费", "a"."增值税", "a"."到账单位", "a"."经办人编号", "a"."到账凭证路径", "a"."建立时间", "a"."备注", "b"."工号" AS "经办人工号", "b"."姓名" AS "经办人姓名", "b"."部门编号" AS "经办人部门编号", "b"."部门名称" AS "经办人部门名称" FROM "项目经费到账" AS "a" LEFT JOIN "v1_人员_带部门名" AS "b" ON "a"."经办人编号" = "b"."编号";

CREATE OR REPLACE VIEW "v2_项目经费报销" AS SELECT "a".*, "b"."工号" AS "报销人工号", "b"."姓名" AS "报销人姓名", "b"."部门编号" AS "报销人部门编号", "b"."部门名称" AS "报销人部门名称" FROM "报销" AS "a" LEFT JOIN "v1_人员_带部门名" AS "b" ON "a"."报销人编号" = "b"."编号";

CREATE OR REPLACE VIEW "v2_纵向项目变更_详情" AS SELECT "a".*, "b"."名称" AS "成果归属部门名称", "c"."工号" AS "负责人工号", "c"."姓名" AS "负责人姓名", "c"."部门编号" AS "负责人部门编号", "c"."部门名称" AS "负责人部门名称", "d"."姓名" AS "登记人姓名", "d"."部门编号" AS "登记人部门编号", "d"."部门名称" AS "登记人部门名称" FROM "纵向项目变更" AS "a" LEFT JOIN "部门" AS "b" ON "a"."成果归属部门编号" = "b"."编号" LEFT JOIN "v1_人员_带部门名" AS "c" ON "a"."负责人编号" = "c"."编号" LEFT JOIN "v1_人员_带部门名" AS "d" ON "a"."登记人编号" = "c"."编号";

CREATE OR REPLACE VIEW "v2_横向项目_列表" AS SELECT "a"."编号", "a"."合同编号", "a"."合同名称", "a"."合同金额", "a"."签订时间", "a"."项目负责人编号", "c"."工号" AS "项目负责人工号", "c"."姓名" AS "项目负责人姓名", "c"."部门编号" AS "项目负责人部门编号", "c"."部门名称" AS "项目负责人部门名称", "a"."项目归属部门编号", "b"."名称" AS "项目归属部门名称" FROM "横向项目" AS "a" LEFT JOIN "部门" AS "b" ON "a"."项目归属部门编号" = "b"."编号" LEFT JOIN "v1_人员_带部门名" AS "c" ON "a"."项目负责人编号" = "c"."编号";

CREATE OR REPLACE VIEW "v2_横向项目_详情" AS SELECT "a".*, "c"."工号" AS "项目负责人工号", "c"."姓名" AS "项目负责人姓名", "c"."部门编号" AS "项目负责人部门编号", "c"."部门名称" AS "项目负责人部门名称", "b"."名称" AS "项目归属部门名称" FROM "横向项目" AS "a" LEFT JOIN "部门" AS "b" ON "a"."项目归属部门编号" = "b"."编号" LEFT JOIN "v1_人员_带部门名" AS "c" ON "a"."项目负责人编号" = "c"."编号";

CREATE OR REPLACE VIEW "v2_横向项目变更_详情" AS SELECT "a".*, "c"."工号" AS "项目负责人工号", "c"."姓名" AS "项目负责人姓名", "c"."部门编号" AS "项目负责人部门编号", "c"."部门名称" AS "项目负责人部门名称", "b"."名称" AS "项目归属部门名称" FROM "横向项目变更" AS "a" LEFT JOIN "部门" AS "b" ON "a"."项目归属部门编号" = "b"."编号" LEFT JOIN "v1_人员_带部门名" AS "c" ON "a"."项目负责人编号" = "c"."编号";

CREATE OR REPLACE VIEW "v2_步骤" AS SELECT "a"."编号", "b"."名称", "a"."步骤模板编号", "a"."流程编号", "a"."状态", "a"."建立时间", "b1"."名称" AS "链接名称", "b1"."路径" AS "链接路径", "b1"."样式" AS "链接样式", "a"."操作人编号", "c"."工号", "c"."姓名", "c"."部门名称", "a"."执行时间", "b2"."操作说明", "b2"."操作提示", "b2"."状态说明", "a"."备注" FROM "步骤" AS "a" JOIN "步骤模板" AS "b" ON "a"."步骤模板编号" = "b"."编号" LEFT JOIN "步骤类型" AS "b1" ON "b"."步骤类型编号" = "b1"."编号" LEFT JOIN "步骤类型状态" AS "b2" ON "b1"."编号" = "b2"."步骤类型编号" AND "a"."状态" = "b2"."状态值" LEFT JOIN "v1_人员_带部门名" AS "c" ON "a"."操作人编号" = "c"."编号";

CREATE OR REPLACE VIEW "v2_任职" AS SELECT "a"."编号", "a"."任职开始时间", "a"."任职截止时间", "a"."任职人员编号", "b"."工号", "b"."姓名", "b"."部门名称", "a"."任职团体名称", "a"."所任职务", "a"."相关文件路径", "a"."建立时间", "a"."备注" FROM "任职" AS "a" LEFT JOIN "v1_人员_带部门名" AS "b" ON "a"."任职人员编号" = "b"."编号";

CREATE OR REPLACE VIEW "v3_讲座反馈" AS SELECT "b"."编号", "b"."讲座编号", "a"."活动名称", "a"."所属部门编号", "a"."所属部门名称", "a"."活动经费", "a"."会议类型", "a"."活动对象", "a"."开始时间", "a"."结束时间", "a"."主办人编号", "a"."主讲人姓名", "a"."主讲人国别", "a"."主讲人单位", "a"."主讲人学位", "a"."主讲人职称", "a"."主讲人简历路径", "a"."讲座地点", "a"."内容概述", "a"."主办人工号", "a"."主办人姓名", "a"."主办人部门名称", "b"."专家姓名", "b"."专家身份证号码", "b"."专家护照号", "b"."专家联系方式", "b"."专家讲课费金额", "b"."结算方式", "b"."活动持续分钟", "b"."参会人数", "b"."讲座附件路径", "b"."讲座效果" FROM "讲座反馈" AS "b" JOIN "v2_主办讲座" AS "a" ON "b"."讲座编号" = "a"."编号";

CREATE OR REPLACE VIEW "v3_参加会议反馈" AS SELECT "b"."编号", "b"."参加会议编号", "a"."会议名称", "a"."会议主办单位", "a"."会议承办单位", "a"."研讨领域", "a"."学科类型", "a"."会议地址", "a"."会议开始时间", "a"."会议结束时间", "a"."往返时间", "a"."会议网址", "a"."会议简介及申请理由", "a"."申请人编号", "a"."参会相关文件路径", "a"."申请人工号", "a"."申请人姓名", "a"."申请人部门名称", "b"."活动持续分钟", "b"."参会人数", "b"."参会总结", "b"."会议反馈文件路径" FROM "v2_参加会议" AS "a" JOIN "参加会议反馈" AS "b" ON "a"."编号" = "b"."参加会议编号";

CREATE OR REPLACE VIEW "v3_项目经费到账_纵向" AS SELECT "a"."编号", "a"."分类", "a"."分类名称", "a"."项目中文名称", "a"."项目英文名称", "a"."项目类型", "a"."项目级别", "a"."项目来源单位", "a"."项目类别", "a"."批准经费", "a"."配套经费", "a"."计划开始日期", "a"."计划完成日期", "a"."成果归属部门名称", "a"."负责人编号", "a"."负责人工号", "a"."负责人姓名", "a"."负责人部门编号", "a"."负责人部门名称", "a"."登记人编号", "a"."登记人姓名", "a"."批准经费" + "a"."配套经费" AS "经费", CASE WHEN "a"."批准经费" + "a"."配套经费" - "b"."待审核到账" - "b"."已审核到账" > 0 THEN "a"."批准经费" + "a"."配套经费" - "b"."待审核到账" - "b"."已审核到账" ELSE 0 END AS "未到经费", COALESCE("b"."待审核到账", 0) AS "待审核到账", COALESCE("b"."已审核到账", 0) AS "已审核到账" FROM "v2_纵向项目立项_列表" AS "a" LEFT JOIN LATERAL (SELECT SUM(CASE "b2"."状态" WHEN 0 THEN "b1"."到账金额" ELSE 0 END) AS "待审核到账", SUM(CASE "b2"."状态" WHEN 1 THEN "b1"."到账金额" ELSE 0 END) AS "已审核到账" FROM "项目经费到账" AS "b1" JOIN "流程" AS "b2" ON "b2"."流程模板编号" = 500 AND "b1"."编号" = "b2"."项目编号" WHERE "b1"."流程模板编号" = 410 AND "b1"."项目编号" = "a"."编号") AS "b" ON TRUE;

CREATE OR REPLACE VIEW "v3_项目经费到账_横向" AS SELECT "a".*, "a"."合同金额" AS "经费", CASE WHEN "a"."合同金额" - "b"."待审核到账" - "b"."已审核到账" > 0 THEN "a"."合同金额" - "b"."待审核到账" - "b"."已审核到账" ELSE 0 END AS "未到经费", COALESCE("b"."待审核到账", 0) AS "待审核到账", COALESCE("b"."已审核到账", 0) AS "已审核到账" FROM "v2_横向项目_列表" AS "a" LEFT JOIN LATERAL (SELECT SUM(CASE "b2"."状态" WHEN 0 THEN "b1"."到账金额" ELSE 0 END) AS "待审核到账", SUM(CASE "b2"."状态" WHEN 1 THEN "b1"."到账金额" ELSE 0 END) AS "已审核到账" FROM "项目经费到账" AS "b1" JOIN "流程" AS "b2" ON "b2"."流程模板编号" = 500 AND "b1"."编号" = "b2"."项目编号" WHERE "b1"."流程模板编号" = 450 AND "b1"."项目编号" = "a"."编号") AS "b" ON TRUE;

CREATE OR REPLACE VIEW "v3_项目经费报销_横向" AS SELECT "a".*, "a"."合同金额" AS "经费", CASE WHEN "a"."合同金额" - "b"."待审核报销" - "b"."已审核报销" > 0 THEN "a"."合同金额" - "b"."待审核报销" - "b"."已审核报销" ELSE 0 END AS "剩余经费", COALESCE("b"."待审核报销", 0) AS "待审核报销", COALESCE("b"."已审核报销", 0) AS "已审核报销" FROM "v2_横向项目_列表" AS "a" LEFT JOIN LATERAL (SELECT SUM(CASE "b2"."状态" WHEN 0 THEN "b1"."报销金额" ELSE 0 END) AS "待审核报销", SUM(CASE "b2"."状态" WHEN 1 THEN "b1"."报销金额" ELSE 0 END) AS "已审核报销" FROM "报销" AS "b1" JOIN "流程" AS "b2" ON "b2"."流程模板编号" = 600 AND "b1"."编号" = "b2"."项目编号" WHERE "b1"."流程模板编号" = 450 AND "b1"."项目编号" = "a"."编号") AS "b" ON TRUE;

CREATE OR REPLACE VIEW "v3_项目经费报销_纵向" AS SELECT "a"."编号", "a"."分类", "a"."分类名称", "a"."项目中文名称", "a"."项目英文名称", "a"."项目类型", "a"."项目级别", "a"."项目来源单位", "a"."项目类别", "a"."批准经费", "a"."配套经费", "a"."计划开始日期", "a"."计划完成日期", "a"."成果归属部门名称", "a"."负责人编号", "a"."负责人工号", "a"."负责人姓名", "a"."负责人部门编号", "a"."负责人部门名称", "a"."登记人编号", "a"."登记人姓名", "a"."批准经费" + "a"."配套经费" AS "经费", CASE WHEN "a"."批准经费" + "a"."配套经费" - "b"."待审核报销" - "b"."已审核报销" > 0 THEN "a"."批准经费" + "a"."配套经费" - "b"."待审核报销" - "b"."已审核报销" ELSE 0 END AS "剩余经费", COALESCE("b"."待审核报销", 0) AS "待审核报销", COALESCE("b"."已审核报销", 0) AS "已审核报销" FROM "v2_纵向项目立项_列表" AS "a" LEFT JOIN LATERAL (SELECT SUM(CASE "b2"."状态" WHEN 0 THEN "b1"."报销金额" ELSE 0 END) AS "待审核报销", SUM(CASE "b2"."状态" WHEN 1 THEN "b1"."报销金额" ELSE 0 END) AS "已审核报销" FROM "报销" AS "b1" JOIN "流程" AS "b2" ON "b2"."流程模板编号" = 600 AND "b1"."编号" = "b2"."项目编号" WHERE "b1"."流程模板编号" = 410 AND "b1"."项目编号" = "a"."编号") AS "b" ON TRUE;

CREATE OR REPLACE VIEW "v3_参加会议报销" AS SELECT "a".*, COALESCE("b"."待审核报销", 0) AS "待审核报销", COALESCE("b"."已审核报销", 0) AS "已审核报销" FROM "v2_参加会议" AS "a" LEFT JOIN LATERAL (SELECT SUM(CASE "b2"."状态" WHEN 0 THEN "b1"."报销金额" ELSE 0 END) AS "待审核报销", SUM(CASE "b2"."状态" WHEN 1 THEN "b1"."报销金额" ELSE 0 END) AS "已审核报销" FROM "报销" AS "b1" JOIN "流程" AS "b2" ON "b2"."流程模板编号" = 600 AND "b1"."编号" = "b2"."项目编号" WHERE "b1"."流程模板编号" = 210 AND "b1"."项目编号" = "a"."编号") AS "b" ON TRUE;

CREATE OR REPLACE VIEW "v2_通知公告_列表" AS SELECT "a"."编号", "a"."通知类型", "a"."通知标识", "a"."发送人编号", "a"."通知名称", "a"."关闭时间", "a"."相关文件路径", "a"."接收人数" /* a.通知内容 , */, "a"."已接收人数", "a"."是否启用", "a"."建立时间", "a"."备注", "b"."工号" AS "发送人工号", "b"."姓名" AS "发送人姓名", "b"."部门编号" AS "发送人部门编号", "b"."部门名称" AS "发送人部门名称" FROM "通知公告" AS "a" LEFT JOIN "v1_人员_带部门名" AS "b" ON "a"."发送人编号" = "b"."编号";

CREATE OR REPLACE VIEW "v3_流程" AS SELECT "a"."编号", "a"."流程模板编号", "c"."名称" /* CASE a.流程模板编号 WHEN 100 THEN '人员任职申请' END AS 流程, */, "c"."路径", "a"."项目编号", "a"."发起人编号", "b"."工号", "b"."姓名", "b"."部门名称", "a"."状态", CASE "a"."状态" WHEN 0 THEN '未完成' WHEN 1 THEN '已完成' WHEN -2 THEN '被终止' WHEN -3 THEN '已过期' END AS "状态名称", "a"."创建时间", "a"."备注", "d"."流程编号", "d"."编号" AS "步骤编号", "d"."名称" AS "步骤名称", "d"."状态" AS "步骤状态", "d"."链接名称" AS "步骤链接名称", "d"."链接路径" AS "步骤链接路径", "d"."链接样式" AS "步骤链接样式", "d"."操作人编号" AS "步骤操作人编号", "d"."工号" AS "步骤操作人工号", "d"."姓名" AS "步骤操作人姓名", "d"."部门名称" AS "步骤操作人部门名称", "d"."执行时间" AS "步骤执行时间", "d"."状态说明" AS "步骤状态说明" /* c.* */ FROM "流程" AS "a" LEFT JOIN "v1_人员_带部门名" AS "b" ON "a"."发起人编号" = "b"."编号" JOIN "流程模板" AS "c" ON "a"."流程模板编号" = "c"."编号" LEFT JOIN LATERAL (SELECT * FROM "v2_步骤" WHERE "流程编号" = "a"."编号" ORDER BY "编号" DESC NULLS LAST LIMIT 1) AS "d" ON TRUE;

CREATE OR REPLACE VIEW "v4_纵向项目总览" AS SELECT "a".*, "b"."名称", "b"."项目编号", "b"."路径", "b"."发起人编号", CONCAT("b"."状态名称", '-', "b"."步骤状态说明") AS "审核进度" FROM "v2_纵向项目立项_列表" AS "a" JOIN "v3_流程" AS "b" ON "b"."流程模板编号" = 410 AND "a"."编号" = "b"."项目编号" UNION ALL SELECT "a".*, "c"."名称", "c"."项目编号", "c"."路径", "c"."发起人编号", CONCAT("c"."状态名称", '-', "c"."步骤状态说明") AS "审核进度" FROM "v2_纵向项目立项_列表" AS "a" JOIN "纵向项目中检" AS "b" ON "a"."编号" = "b"."纵向项目编号" JOIN "v3_流程" AS "c" ON "c"."流程模板编号" = 420 AND "b"."编号" = "c"."项目编号" UNION ALL SELECT "a".*, "c"."名称", "c"."项目编号", "c"."路径", "c"."发起人编号", CONCAT("c"."状态名称", '-', "c"."步骤状态说明") AS "审核进度" FROM "v2_纵向项目立项_列表" AS "a" JOIN "纵向项目变更" AS "b" ON "a"."编号" = "b"."纵向项目编号" JOIN "v3_流程" AS "c" ON "c"."流程模板编号" = 430 AND "b"."编号" = "c"."项目编号" UNION ALL SELECT "a".*, "c"."名称", "c"."项目编号", "c"."路径", "c"."发起人编号", CONCAT("c"."状态名称", '-', "c"."步骤状态说明") AS "审核进度" FROM "v2_纵向项目立项_列表" AS "a" JOIN "纵向项目结题" AS "b" ON "a"."编号" = "b"."纵向项目编号" JOIN "v3_流程" AS "c" ON "c"."流程模板编号" = 440 AND "b"."编号" = "c"."项目编号";

CREATE OR REPLACE VIEW "v4_横向项目总览" AS SELECT "a".*, "b"."名称", "b"."项目编号", "b"."路径", "b"."发起人编号", CONCAT("b"."状态名称", '-', "b"."步骤状态说明") AS "审核进度" FROM "v2_横向项目_列表" AS "a" JOIN "v3_流程" AS "b" ON "b"."流程模板编号" = 450 AND "a"."编号" = "b"."项目编号" UNION ALL SELECT "a".*, "c"."名称", "c"."项目编号", "c"."路径", "c"."发起人编号", CONCAT("c"."状态名称", '-', "c"."步骤状态说明") AS "审核进度" FROM "v2_横向项目_列表" AS "a" JOIN "横向项目变更" AS "b" ON "a"."编号" = "b"."横向项目编号" JOIN "v3_流程" AS "c" ON "c"."流程模板编号" = 460 AND "b"."编号" = "c"."项目编号" UNION ALL SELECT "a".*, "c"."名称", "c"."项目编号", "c"."路径", "c"."发起人编号", CONCAT("c"."状态名称", '-', "c"."步骤状态说明") AS "审核进度" FROM "v2_横向项目_列表" AS "a" JOIN "横向项目结题" AS "b" ON "a"."编号" = "b"."横向项目编号" JOIN "v3_流程" AS "c" ON "c"."流程模板编号" = 470 AND "b"."编号" = "c"."项目编号";

CREATE OR REPLACE VIEW "v4_成果总览" AS /* 这个东西很渣,初步用意是提现所有成果的列表+审核状态的信息,但是不包括"当前登录人是否可以操作"也就是不需要tfn_流程,这样一个介于v2_xxx列表和tfn_xxx之间的东西 */ /* 但是union all或者如下这样语法很渣,而且前台完全可以分tab显示,也就是会分成四个xxx总览,但是效率很高; */ SELECT "a"."编号", CASE WHEN "a"."流程模板编号" = 300 OR "a"."流程模板编号" = 310 THEN "b"."论文标题" WHEN "a"."流程模板编号" = 301 THEN "c"."著作名称" WHEN "a"."流程模板编号" = 302 THEN "d"."专利名称" WHEN "a"."流程模板编号" = 303 THEN "e"."获奖名称" END AS "成果名称", "a"."名称" AS "成果类型", "a"."路径", "a"."工号", "a"."发起人编号", "a"."姓名" AS "发起人姓名", "a"."部门名称" AS "发起人部门名称" FROM "v3_流程" AS "a" LEFT JOIN "成果论文" AS "b" ON "a"."项目编号" = "b"."编号" LEFT JOIN "成果著作" AS "c" ON "a"."项目编号" = "c"."编号" LEFT JOIN "成果专利" AS "d" ON "a"."项目编号" = "d"."编号" LEFT JOIN "成果获奖" AS "e" ON "a"."项目编号" = "e"."编号" WHERE "a"."状态" = 1 AND "a"."流程模板编号" BETWEEN 300 AND 399;

CREATE OR REPLACE VIEW "v1_成果获奖_列表" AS SELECT a."编号", a."获奖名称", a."获奖日期", a."获奖级别", a."获奖类别", a."颁奖单位", a."年度", "fn_成果作者"(4, a."编号", '第一') AS "第一完成人" FROM "成果获奖" AS a;

COMMIT;
