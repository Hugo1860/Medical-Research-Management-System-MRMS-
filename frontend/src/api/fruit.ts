import { get, post, upload } from '@/utils/request'

export interface PagingResult<T> {
  total: number
  list: T[]
}

// ================= 1. 成果总览与跨库索引 =================
export interface AchievementOverviewItem {
  编号: number
  成果名称: string
  成果类型: string
  路径?: string
  工号?: string
  发起人编号?: number
  发起人姓名?: string
  发起人部门名称?: string
}

export interface AchievementFilter {
  Like成果名称?: string
  成果类型?: string
  Index?: number
  Size?: number
  OrderType?: boolean
}

// 分页获取成果总览
export function getPagingAchievements(
  paging: { Index: number; Size: number; OrderType?: boolean },
  filter?: { Like成果名称?: string; 成果类型?: string }
) {
  return get<PagingResult<AchievementOverviewItem>>('/Fruit/分页获取成果总览', {
    ...paging,
    ...filter
  })
}

// 获取成果治理详情 (关联、作者核验、治理状态)
export function getAchievementGovernanceDetail(成果类型: number, 成果编号: number) {
  return get<{
    成果类型: number
    成果编号: number
    原作者: any[]
    作者身份核验: any[]
    成果关联: any[]
    成果治理: any
    治理历史: any[]
    合并审计: any[]
  }>('/Fruit/获取成果治理详情', {
    成果类型,
    成果编号
  })
}

// 获取不可变治理历史和重复成果合并映射（用于审计复核）
export function getAchievementGovernanceAudit(成果类型: number, 成果编号: number) {
  return get<{ 成果类型: number; 成果编号: number; 治理历史: any[]; 合并审计: any[] }>('/Fruit/获取成果治理审计', {
    成果类型,
    成果编号
  })
}

// 新增科研成果关联
export function addAchievementRelation(data: {
  成果类型: number
  成果编号: number
  关联类型: '研究' | '资助' | '伦理' | '数据' | '其他'
  流程模板编号?: number
  项目编号?: number
  外部对象类型?: string
  外部对象编号?: string
  关联说明?: string
}) {
  return post('/Fruit/新增科研成果关联', data)
}

// 核验科研成果作者
export function verifyAchievementAuthor(data: {
  成果类型: number
  成果编号: number
  作者编号: number
  作者顺序?: number
  作者显示姓名?: string
  单位名称?: string
  ORCID?: string
  身份状态?: '待核验' | '已核验' | '需更正' | '无法匹配'
  核验说明?: string
}) {
  return post('/Fruit/核验科研成果作者', data)
}

// 更新科研成果治理状态
export function updateAchievementGovernance(data: {
  成果类型: number
  成果编号: number
  状态: '有效' | '待更正' | '已更正' | '已撤稿' | '重复'
  规范成果类型?: number
  规范成果编号?: number
  治理原因?: string
  来源类型?: string
  来源凭证编号?: string
  来源凭证摘要?: string
  来源文件路径?: string
  来源文件SHA256?: string
  来源系统?: string
}) {
  return post('/Fruit/更新科研成果治理状态', data)
}

export function verifyAchievementStatusEvidence(data: {
  状态凭证编号: number
  核验结果: '已核验' | '不通过'
  核验说明: string
}) {
  return post('/Fruit/核验科研成果状态凭证', data)
}

// ================= 2. 著作管理 =================
export interface BookItem {
  编号: number
  著作名称: string
  出版社名称?: string
  著作形式?: string
  年度?: number
  著作出版日期?: string
  ISBN号?: string
  著作字数?: number
  奖励情况?: string
  出版地?: string
  出版社级别?: string
  著作类型?: string
  是否译为外文?: boolean
  语种?: string
  主编?: string
  作者人数?: number
  发行价?: number
  备注?: string
  封面相关文件路径?: string
  版权页相关文件路径?: string
}

// 分页获取著作
export function getPagingBooks(
  paging: { Index: number; Size: number; OrderType?: boolean },
  filter?: { Like著作名称?: string }
) {
  return get<PagingResult<BookItem>>('/Fruit/分页获取著作', {
    ...paging,
    ...filter
  })
}

// 获取著作详情
export function getBookDetail(著作编号: number) {
  return get<{ 著作详情: BookItem; 成果项目: any[]; 成果作者: any[]; 审核情况: any[] }>('/Fruit/获取著作详情', {
    著作编号
  })
}

// 新增著作
export function addBook(data: {
  基本资料: Partial<BookItem>
  相关项目?: any[]
  作者?: any[]
  IsHold?: boolean
}) {
  return post('/Fruit/新增著作', data)
}

// ================= 3. 专利管理 =================
export interface PatentItem {
  编号: number
  专利名称: string
  专利国别?: string
  专利类型?: string
  专利范围?: string
  是否授权?: boolean
  代理机构?: string
  年度?: number
  专利权人名称?: string
  申请号?: string
  申请日期?: string
  公开号?: string
  公开日期?: string
  授权机构?: string
  专利号?: string
  国际专利主分类号?: string
  授权公告日?: string
  证书号?: string
  是否职务发明?: boolean
  是否生效?: boolean
  发明人数?: number
  第一作者?: string
  备注?: string
  相关文件路径?: string
}

// 分页获取专利
export function getPagingPatents(
  paging: { Index: number; Size: number; OrderType?: boolean },
  filter?: { Like专利名称?: string }
) {
  return get<PagingResult<PatentItem>>('/Fruit/分页获取专利', {
    ...paging,
    ...filter
  })
}

// 获取专利详情
export function getPatentDetail(专利编号: number) {
  return get<{ 专利详情: PatentItem; 成果项目: any[]; 成果作者: any[]; 审核情况: any[] }>('/Fruit/获取专利详情', {
    专利编号
  })
}

// 新增专利
export function addPatent(data: {
  基本资料: Partial<PatentItem>
  相关项目?: any[]
  作者?: any[]
  IsHold?: boolean
}) {
  return post('/Fruit/新增专利', data)
}

// 通知专利代理机构
export function notifyPatentAgent(data: {
  专利编号: number
  外部系统编号: number
  事件类型: '申请' | '授权' | '变更' | '年费' | '撤回'
  说明?: string
}) {
  return post<{ 编号: number; 状态: string; 幂等键: string }>('/Fruit/通知专利代理机构', data)
}

// ================= 4. 获奖管理 =================
export interface AwardItem {
  编号: number
  获奖名称: string
  获奖级别?: string
  获奖日期?: string
  获奖类别?: string
  奖励情况?: string
  联系人?: string
  联系人手机?: string
  年度?: number
  颁奖单位?: string
  完成人数?: number
  获奖证书编号?: string
  完成单位数量?: number
  获奖摘要?: string
  备注?: string
  相关文件路径?: string
}

// 分页获取获奖
export function getPagingAwards(
  paging: { Index: number; Size: number; OrderType?: boolean },
  filter?: { Like获奖名称?: string }
) {
  return get<PagingResult<AwardItem>>('/Fruit/分页获取获奖', {
    ...paging,
    ...filter
  })
}

// 获取获奖详情
export function getAwardDetail(获奖编号: number) {
  return get<{ 获奖详情: AwardItem; 成果项目: any[]; 成果作者: any[]; 审核情况: any[] }>('/Fruit/获取获奖详情', {
    获奖编号
  })
}

// 新增获奖
export function addAward(data: {
  基本资料: Partial<AwardItem>
  相关项目?: any[]
  作者?: any[]
  IsHold?: boolean
}) {
  return post('/Fruit/新增获奖', data)
}

// ================= 5. 论文认领与外部采集 (保留既有接口兼容) =================
export interface ClaimablePaperItem {
  编号: number
  中文名称: string
  英文名称?: string
  刊物名称: string
  发表时间: string
  第一作者: string
  通讯作者: string
  卷号?: string
  期号?: string
  页码范围?: string
  DOI?: string
  PMID?: string
  认领状态: string
  认领人姓名?: string
}

// 分页获取可认领论文列表
export function getClaimablePapers(
  paging: { index: number; size: number },
  filter?: { Like中文名称?: string; Like第一作者?: string }
) {
  return get<{ total: number; list: ClaimablePaperItem[] }>('/Fruit/分页获取可领取论文', {
    ...paging,
    ...filter
  })
}

// 认领论文
export function claimPaper(data: {
  论文编号: number
  作者位次: string
  认领说明?: string
}) {
  return post('/Fruit/认领论文', data)
}

// 从 Crossref 采集论文
export function harvestFromCrossref(data: {
  外部系统编号?: number
  关键词?: string
  DOI?: string
  作者?: string
  从日期?: string
  到日期?: string
  行数?: number
  最大记录数?: number
}) {
  return post('/Fruit/从Crossref采集论文', data)
}

// 从 PubMed 采集论文
export function harvestFromPubMed(data: {
  外部系统编号?: number
  关键词?: string
  作者?: string
  从日期?: string
  到日期?: string
  行数?: number
  检索词?: string
  最大记录数?: number
}) {
  return post('/Fruit/从PubMed采集论文', data)
}

// ================= 6. 学术论文全生命周期管理 =================
export interface PaperItem {
  编号: number
  论文标题: string
  刊物名称?: string
  第一作者?: string
  通讯作者?: string
  所有作者?: string
  作者人数?: number
  论文Online日期?: string
  论文正式出版日期?: string
  年度?: number
  卷号?: string
  期号?: string
  页码范围?: string
  DOI?: string
  PMID?: string
  影响因子?: number
  刊物编号?: number
  关键字?: string
  论文摘要?: string
  认领人编号?: number
  认领人姓名?: string
  认领状态?: string
  备注?: string
  检索信息页相关文件路径?: string
  论文正文路径?: string
  其他相关文件路径?: string
  发起人姓名?: string
  发起人部门名称?: string
}

export interface PaperFilter {
  Like论文标题?: string
  Like第一作者?: string
  Like刊物名称?: string
  年度?: number
  Index?: number
  Size?: number
  OrderType?: boolean
}

export interface MatchedJournal {
  编号: number
  名称: string
  英文刊名?: string
  ISSN号?: string
  CN号?: string
  级别?: string
  年度?: number
  匹配分数: number
}

// 分页获取全院学术论文
export function getPagingPapers(
  paging: { Index: number; Size: number; OrderType?: boolean },
  filter?: PaperFilter
) {
  return get<PagingResult<PaperItem>>('/Fruit/分页获取论文', {
    ...paging,
    ...filter
  })
}

// 获取论文详情与流转审批步骤
export function getPaperDetail(论文编号: number) {
  return get<{
    论文详情: PaperItem
    成果项目: any[]
    成果作者: any[]
    审核情况: any[]
  }>('/Fruit/获取论文详情', {
    论文编号
  })
}

// 新增/登记学术论文
export function addPaper(data: {
  基本资料: Partial<PaperItem>
  相关项目?: any[]
  作者?: any[]
  IsHold?: boolean
}) {
  return post('/Fruit/新增论文', data)
}

// 完善论文资料
export function completePaper(data: any) {
  return post('/Fruit/完善论文资料', data)
}

// 识别期刊库信息并智能计算匹配度
export function identifyJournal(params: {
  刊物名称?: string
  ISSN号?: string
  CN号?: string
}) {
  return get<MatchedJournal[]>('/Fruit/识别论文期刊信息', params)
}

// 批量导入知网论文
export function importCNKIPapers(file: File) {
  return upload('/Fruit/导入知网论文', file, 'files')
}

// 批量导入自定义格式论文
export function importCustomPapers(file: File) {
  return upload('/Fruit/导入自定义论文', file, 'files')
}

// ================= 7. 论文主动大数据网推、机构别名消歧与中科院分区自动打标 (清华长庚医院实践) =================
export interface ActivePushPaperItem {
  编号: number
  外部论文编号: string
  论文编号?: number | null
  论文标题: string
  作者: string
  刊物名称: string
  issN号?: string
  cN号?: string
  中科院分区?: string
  中科院大类学科?: string
  是否Top期刊?: boolean
  影响因子?: number
  jcR分区?: string
  推荐认领人编号?: number
  推荐认领人姓名?: string
  消歧置信度?: number
  消歧命中依据?: string
  网推状态?: string
  状态: string
  最后同步时间?: string
}

export interface DisambiguationConfigData {
  机构别名: Array<{
    编号: number
    规范单位编号: number
    规范单位名称: string
    别名: string
    来源: string
    是否启用: boolean
  }>
  学者消歧: Array<{
    编号: number
    人员编号: number
    工号: string
    姓名: string
    英文拼音署名: string
    orcid?: string
    科室名称: string
    是否主要署名: boolean
  }>
}

// 获取论文主动网推台账 (包含中科院分区、Top期刊标识、影响因子、AI消歧置信度与命中依据)
export function getActivePushPapers(params?: {
  status?: string
  claimantId?: number
  casPartition?: string
  keyword?: string
}) {
  return get<ActivePushPaperItem[]>('/Fruit/获取论文主动网推台账', params)
}

// 触发全网论文主动网推巡检扫描与消歧打标
export function triggerActivePaperPush(data?: { 外部系统编号?: number }) {
  return post<{
    success: boolean
    total_scanned: number
    high_confidence_pushed: number
    partition_matched: number
    message: string
  }>('/Fruit/触发论文全网主动网推', data || {})
}

// 一键确认认领网推论文
export function confirmClaimPushPaper(data: {
  采集结果编号: number
  认领人编号?: number
  作者位次?: string
  认领说明?: string
}) {
  return post<{
    success: boolean
    paper_id: number
    claimant_id: number
    claimant_name: string
    message: string
  }>('/Fruit/一键确认认领网推论文', data)
}

// 获取医院机构别名与学者消歧知识库配置
export function getDisambiguationConfigs() {
  return get<DisambiguationConfigData>('/Fruit/获取机构与学者消歧配置')
}

// 新增医院机构别名
export function addInstitutionalAlias(data: { 别名: string; 来源?: string }) {
  return post<{ success: boolean; message: string }>('/Fruit/新增机构别名', data)
}

// 新增学者消歧署名
export function addScholarDisambiguation(data: {
  人员编号: number
  英文拼音署名: string
  ORCID?: string
  科室名称?: string
}) {
  return post<{ success: boolean; message: string }>('/Fruit/新增学者消歧署名', data)
}

// ================= 8. 成果转化与收益分配 =================
export interface AchievementConversion {
  编号: number
  成果类型: number
  成果编号: number
  成果名称?: string
  转化名称: string
  转化方式: string
  合作单位?: string
  合同编号?: string
  合同签署日期?: string
  合同金额: number
  已收款金额?: number
  收益记录数?: number
  申请说明?: string
  状态: string
  审批意见?: string
  申请人姓名?: string
  审批人姓名?: string
  审批时间?: string
}

export interface AchievementConversionRevenue {
  编号?: number
  转化编号: number
  收益类型: string
  计划金额: number
  实际金额: number
  收款日期?: string
  收款凭证号?: string
  分配状态?: string
  已分配金额?: number
  备注?: string
}

export interface AchievementConversionDistribution {
  编号?: number
  收益编号: number
  人员编号: number
  姓名?: string
  分配比例: number
  分配金额: number
  财务回写状态?: string
  财务凭证号?: string
  财务回写说明?: string
}

export function getAchievementConversions(params?: { 成果类型?: number; 成果编号?: number; 状态?: string }) {
  return get<AchievementConversion[]>('/Fruit/获取成果转化台账', params)
}

export function getAchievementConversionDetail(转化编号: number) {
  return get<{ 转化: AchievementConversion; 收益: AchievementConversionRevenue[]; 分配: AchievementConversionDistribution[] }>('/Fruit/获取成果转化详情', { 转化编号 })
}

export function saveAchievementConversion(data: Partial<AchievementConversion>) {
  return post<{ 编号: number; 状态: string }>('/Fruit/登记成果转化', data)
}

export function approveAchievementConversion(data: { 转化编号: number; 状态: string; 审批意见?: string }) {
  return post('/Fruit/审批成果转化', data)
}

export function saveAchievementConversionRevenue(data: Partial<AchievementConversionRevenue>) {
  return post('/Fruit/登记成果转化收益', data)
}

export function saveAchievementConversionDistribution(data: Partial<AchievementConversionDistribution>) {
  return post('/Fruit/保存成果转化收益分配', data)
}

export function updateAchievementConversionFinance(data: { 分配编号: number; 财务回写状态: string; 财务凭证号?: string; 财务回写说明?: string }) {
  return post('/Fruit/回写成果转化财务状态', data)
}


