-- PostgreSQL implementations for applying approved vertical and horizontal
-- project changes. The change row keeps a copy of the previous project data
-- so the existing review workflow can still display a rollback snapshot.
BEGIN;

CREATE OR REPLACE FUNCTION "sp_纵向项目变更_应用变更"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE
    change_id integer := (p_payload->>'纵向项目变更编号')::integer;
    old_project "纵向项目立项";
    requested_change "纵向项目变更";
BEGIN
    SELECT p.* INTO STRICT old_project
    FROM "纵向项目立项" p
    JOIN "纵向项目变更" c ON c."纵向项目编号" = p."编号"
    WHERE c."编号" = change_id;
    SELECT c.* INTO STRICT requested_change
    FROM "纵向项目变更" c
    WHERE c."编号" = change_id;

    UPDATE "纵向项目立项" p
    SET ("分类", "项目登记情况", "项目编号", "项目中文名称", "项目英文名称", "项目分类",
         "成果归属部门编号", "负责人编号", "立项年度", "登记日期", "登记人编号", "批准日期",
         "批准经费", "配套经费", "经费卡号", "项目等级", "计划开始日期", "计划完成日期",
         "学科门类", "学科类型", "研究类型", "项目类型", "项目级别", "项目来源单位", "项目类别",
         "档案编号", "财务简称", "亚类说明", "附注说明", "纵向项目合同类型", "社会经济目标",
         "是否需要伦理评审", "是否需要伦理批文", "伦理批文编号", "伦理批文时间", "伦理批文路径",
         "立项文件路径", "备注", "建立时间") =
        (requested_change."分类", requested_change."项目登记情况", requested_change."项目编号",
         requested_change."项目中文名称", requested_change."项目英文名称", requested_change."项目分类",
         requested_change."成果归属部门编号", requested_change."负责人编号", requested_change."立项年度",
         requested_change."登记日期", requested_change."登记人编号", requested_change."批准日期",
         requested_change."批准经费", requested_change."配套经费", requested_change."经费卡号",
         requested_change."项目等级", requested_change."计划开始日期", requested_change."计划完成日期",
         requested_change."学科门类", requested_change."学科类型", requested_change."研究类型",
         requested_change."项目类型", requested_change."项目级别", requested_change."项目来源单位",
         requested_change."项目类别", requested_change."档案编号", requested_change."财务简称",
         requested_change."亚类说明", requested_change."附注说明", requested_change."纵向项目合同类型",
         requested_change."社会经济目标", requested_change."是否需要伦理评审", requested_change."是否需要伦理批文",
         requested_change."伦理批文编号", requested_change."伦理批文时间", requested_change."伦理批文路径",
         requested_change."立项文件路径", requested_change."备注", requested_change."建立时间")
    WHERE p."编号" = requested_change."纵向项目编号";

    UPDATE "纵向项目变更" c
    SET ("分类", "项目登记情况", "项目编号", "项目中文名称", "项目英文名称", "项目分类",
         "成果归属部门编号", "负责人编号", "立项年度", "登记日期", "登记人编号", "批准日期",
         "批准经费", "配套经费", "经费卡号", "项目等级", "计划开始日期", "计划完成日期",
         "学科门类", "学科类型", "研究类型", "项目类型", "项目级别", "项目来源单位", "项目类别",
         "档案编号", "财务简称", "亚类说明", "附注说明", "纵向项目合同类型", "社会经济目标",
         "是否需要伦理评审", "是否需要伦理批文", "伦理批文编号", "伦理批文时间", "伦理批文路径",
         "立项文件路径", "备注", "建立时间") =
        (old_project."分类", old_project."项目登记情况", old_project."项目编号", old_project."项目中文名称",
         old_project."项目英文名称", old_project."项目分类", old_project."成果归属部门编号", old_project."负责人编号",
         old_project."立项年度", old_project."登记日期", old_project."登记人编号", old_project."批准日期",
         old_project."批准经费", old_project."配套经费", old_project."经费卡号", old_project."项目等级",
         old_project."计划开始日期", old_project."计划完成日期", old_project."学科门类", old_project."学科类型",
         old_project."研究类型", old_project."项目类型", old_project."项目级别", old_project."项目来源单位",
         old_project."项目类别", old_project."档案编号", old_project."财务简称", old_project."亚类说明",
         old_project."附注说明", old_project."纵向项目合同类型", old_project."社会经济目标", old_project."是否需要伦理评审",
         old_project."是否需要伦理批文", old_project."伦理批文编号", old_project."伦理批文时间", old_project."伦理批文路径",
         old_project."立项文件路径", old_project."备注", old_project."建立时间")
    WHERE c."编号" = change_id;
    RETURN 1;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RAISE EXCEPTION '纵向项目变更不存在或对应立项不存在';
END;
$$;

CREATE OR REPLACE FUNCTION "sp_横向项目变更_应用变更"(p_payload jsonb)
RETURNS integer LANGUAGE plpgsql AS $$
DECLARE
    change_id integer := (p_payload->>'横向项目变更编号')::integer;
    old_project "横向项目";
    requested_change "横向项目变更";
BEGIN
    SELECT p.* INTO STRICT old_project
    FROM "横向项目" p
    JOIN "横向项目变更" c ON c."横向项目编号" = p."编号"
    WHERE c."编号" = change_id;
    SELECT c.* INTO STRICT requested_change
    FROM "横向项目变更" c
    WHERE c."编号" = change_id;

    UPDATE "横向项目" p
    SET ("项目分类", "项目类型", "合同编号", "合同名称", "是否为第一承建单位", "项目负责人编号",
         "项目归属部门编号", "单位名称", "邮政编码", "法定代表人", "经费本编号", "联系人", "联系人电话",
         "通讯地址", "是否需要伦理评审", "档案编号", "财务简称", "课题来源", "合作方关系", "合作方契约关系",
         "合作方单位名称", "合作方法人或组织机构代码", "合作方法定代表人", "合作方单位联系人", "合作方联系电话",
         "合作方邮箱", "合作方邮政编码", "合作方所在省", "合作方所在市", "合作方所在地区", "合同金额",
         "签订时间", "付款方式", "合同开始时间", "合同结束时间", "合作方式", "研究类型", "技术领域", "合同类型",
         "技术合同类型", "是否申请技术合同认定", "合同技术交易额", "知识产权", "计划来源", "合作方所属国民经济行业",
         "技术服务的国民经济行业", "合同的社会经济目标", "合作方性质", "是否高新性质", "合同认定登记承诺书路径",
         "合同文件路径", "备注", "建立时间") =
        (requested_change."项目分类", requested_change."项目类型", requested_change."合同编号", requested_change."合同名称",
         requested_change."是否为第一承建单位", requested_change."项目负责人编号", requested_change."项目归属部门编号",
         requested_change."单位名称", requested_change."邮政编码", requested_change."法定代表人", requested_change."经费本编号",
         requested_change."联系人", requested_change."联系人电话", requested_change."通讯地址", requested_change."是否需要伦理评审",
         requested_change."档案编号", requested_change."财务简称", requested_change."课题来源", requested_change."合作方关系",
         requested_change."合作方契约关系", requested_change."合作方单位名称", requested_change."合作方法人或组织机构代码",
         requested_change."合作方法定代表人", requested_change."合作方单位联系人", requested_change."合作方联系电话",
         requested_change."合作方邮箱", requested_change."合作方邮政编码", requested_change."合作方所在省", requested_change."合作方所在市",
         requested_change."合作方所在地区", requested_change."合同金额", requested_change."签订时间", requested_change."付款方式",
         requested_change."合同开始时间", requested_change."合同结束时间", requested_change."合作方式", requested_change."研究类型",
         requested_change."技术领域", requested_change."合同类型", requested_change."技术合同类型", requested_change."是否申请技术合同认定",
         requested_change."合同技术交易额", requested_change."知识产权", requested_change."计划来源", requested_change."合作方所属国民经济行业",
         requested_change."技术服务的国民经济行业", requested_change."合同的社会经济目标", requested_change."合作方性质",
         requested_change."是否高新性质", requested_change."合同认定登记承诺书路径", requested_change."合同文件路径",
         requested_change."备注", requested_change."建立时间")
    WHERE p."编号" = requested_change."横向项目编号";

    UPDATE "横向项目变更" c
    SET ("项目分类", "项目类型", "合同编号", "合同名称", "是否为第一承建单位", "项目负责人编号",
         "项目归属部门编号", "单位名称", "邮政编码", "法定代表人", "经费本编号", "联系人", "联系人电话",
         "通讯地址", "是否需要伦理评审", "档案编号", "财务简称", "课题来源", "合作方关系", "合作方契约关系",
         "合作方单位名称", "合作方法人或组织机构代码", "合作方法定代表人", "合作方单位联系人", "合作方联系电话",
         "合作方邮箱", "合作方邮政编码", "合作方所在省", "合作方所在市", "合作方所在地区", "合同金额", "签订时间",
         "付款方式", "合同开始时间", "合同结束时间", "合作方式", "研究类型", "技术领域", "合同类型", "技术合同类型",
         "是否申请技术合同认定", "合同技术交易额", "知识产权", "计划来源", "合作方所属国民经济行业", "技术服务的国民经济行业",
         "合同的社会经济目标", "合作方性质", "是否高新性质", "合同认定登记承诺书路径", "合同文件路径", "备注", "建立时间") =
        (old_project."项目分类", old_project."项目类型", old_project."合同编号", old_project."合同名称", old_project."是否为第一承建单位",
         old_project."项目负责人编号", old_project."项目归属部门编号", old_project."单位名称", old_project."邮政编码", old_project."法定代表人",
         old_project."经费本编号", old_project."联系人", old_project."联系人电话", old_project."通讯地址", old_project."是否需要伦理评审",
         old_project."档案编号", old_project."财务简称", old_project."课题来源", old_project."合作方关系", old_project."合作方契约关系",
         old_project."合作方单位名称", old_project."合作方法人或组织机构代码", old_project."合作方法定代表人", old_project."合作方单位联系人",
         old_project."合作方联系电话", old_project."合作方邮箱", old_project."合作方邮政编码", old_project."合作方所在省", old_project."合作方所在市",
         old_project."合作方所在地区", old_project."合同金额", old_project."签订时间", old_project."付款方式", old_project."合同开始时间",
         old_project."合同结束时间", old_project."合作方式", old_project."研究类型", old_project."技术领域", old_project."合同类型",
         old_project."技术合同类型", old_project."是否申请技术合同认定", old_project."合同技术交易额", old_project."知识产权", old_project."计划来源",
         old_project."合作方所属国民经济行业", old_project."技术服务的国民经济行业", old_project."合同的社会经济目标", old_project."合作方性质",
         old_project."是否高新性质", old_project."合同认定登记承诺书路径", old_project."合同文件路径", old_project."备注", old_project."建立时间")
    WHERE c."编号" = change_id;
    RETURN 1;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RAISE EXCEPTION '横向项目变更不存在或对应项目不存在';
END;
$$;

COMMIT;
