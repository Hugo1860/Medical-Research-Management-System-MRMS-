import { get, post } from '@/utils/request'

export interface VerticalApplicationItem {
  编号: number
  名称?: string
  项目名称?: string
  申报人姓名?: string
  姓名?: string
  工号?: string
  申报人编号?: number
  部门编号?: number
  部门名称?: string
  学科名称?: string
  学科门类?: string
  申报日期?: string
  申报时间?: string
  申请经费: number
  项目级别: string
  项目状态?: string
  状态名称?: string
  步骤名称?: string
  审核进度?: string
  是否立项?: boolean | null
  立项状态?: string
  版本号?: number
  计划开始日期?: string
  计划完成日期?: string
  是否可处理?: boolean
}

export interface PreCheckItem {
  代码?: string
  名称?: string
  检查项?: string
  通过?: boolean
  阻断提交?: boolean
  状态?: '通过' | '警告' | '阻断'
  提示?: string
  提示信息?: string
}

export interface PreCheckResult {
  通过: boolean
  材料完整: boolean
  记录编号?: number
  申报计划编号?: number | null
  院级批次编号?: number | null
  阻断提交?: boolean
  检查项?: PreCheckItem[]
  检查项明细?: PreCheckItem[]
}

export interface VersionChangeItem {
  字段: string
  左值: string | null
  右值: string | null
}

export interface VersionDiffResult {
  项目编号: number
  左版本号: number
  右版本号: number
  变化数量: number
  变化项: VersionChangeItem[]
}

export interface ReviewOpinionItem {
  编号: number
  步骤编号?: number | null
  评审人编号?: number
  评审人姓名?: string
  意见类型: string
  意见内容: string
  是否公开给申请人?: boolean
  建立时间?: string
}

export interface ApplicationReviewSummary {
  基本信息: {
    编号: number
    项目名称: string
    申报人编号: number
    申报年度: number
    是否立项?: boolean | null
    流程状态?: number
    流程编号?: number
  }
  版本: Array<{
    编号: number
    版本号: number
    变更类型: string
    变更人编号: number
    建立时间: string
    快照: string
  }>
  评审意见: ReviewOpinionItem[]
  专家评分: any[]
  未获资助复盘: any[]
  是否可发起复盘: boolean
}

export function buildApplicationPayload(data: any, isHold: boolean = false) {
  if (data.基本资料) {
    return {
      基本资料: data.基本资料,
      参与人列表: data.参与人列表 || [],
      申报计划编号: data.申报计划编号 != null ? Number(data.申报计划编号) : null,
      院级批次编号: data.院级批次编号 != null ? Number(data.院级批次编号) : null,
      IsHold: isHold
    }
  }

  const [start, end] = data.dateRange || ['2026-01-01', '2028-12-31']
  const planId = data.申报计划编号 != null ? Number(data.申报计划编号) : null
  const batchId = data.院级批次编号 != null ? Number(data.院级批次编号) : null
  return {
    基本资料: {
      编号: data.编号 || 0,
      分类: data.分类 || 1,
      项目名称: data.项目名称 || data.名称,
      学科门类: data.学科门类 || '医学',
      学科类型: data.学科类型 || '基础医学',
      申报年度: Number(data.申报年度) || new Date().getFullYear(),
      项目类型: data.项目类型 || '国家重点研发计划',
      项目级别: data.项目级别 || '国家级重点',
      项目来源单位: data.项目来源单位 || '科学技术部',
      项目类别: data.项目类别 || '面上重点',
      申请经费: Number(data.申请经费) || 500000,
      计划开始日期: start,
      计划完成日期: end,
      项目等级: data.项目等级 || '一等',
      是否委托外单位进行实验或实验分析: Boolean(data.是否委托外单位进行实验或实验分析),
      实验委托检测内容: data.实验委托检测内容 || '',
      项目摘要: data.项目摘要 || data.摘要 || '针对项目核心科学问题，制定严谨的技术实施路线与临床评价标准。',
      是否需要伦理评审: Boolean(data.伦理要求 ?? data.是否需要伦理评审),
      是否需要伦理批文: Boolean(data.是否需要伦理批文),
      伦理批文编号: data.伦理批文编号 || '',
      伦理批文路径: data.伦理批文路径 || '',
      项目申报书路径: data.项目申报书路径 || '/upload/纵向项目/项目申报书/application.pdf',
      申报计划编号: planId,
      院级批次编号: batchId,
      资助类型: data.资助类型 || '财政资助',
      备注: data.备注 || ''
    },
    参与人列表: Array.isArray(data.参与人列表) && data.参与人列表.length > 0
      ? data.参与人列表
      : [
          {
            参与人编号: data.申报人编号 || 2,
            工作量占比: 60,
            备注: '项目负责人'
          }
        ],
    申报计划编号: planId,
    院级批次编号: batchId,
    IsHold: isHold
  }
}

export interface InternalProjectBatch {
  编号: number
  计划年度: number
  批次名称: string
  项目类别?: string
  发布开始日期: string
  截止日期: string
  个人申报限额: number
  批次总限额?: number | null
  资格规则?: Record<string, any> | string
  评审方案?: Record<string, any> | string
  评审开始日期?: string | null
  评审截止日期?: string | null
  状态: string
  批次说明?: string | null
  是否可申报?: boolean
  已申报数?: number
}

export function getInternalProjectBatches(params: { Index?: number; Size?: number; 计划年度?: number; 状态?: string } = {}) {
  return get<{ total: number; list: InternalProjectBatch[] }>('/YProject/分页获取院级项目申报批次', params)
}

export function saveInternalProjectBatch(data: Partial<InternalProjectBatch> & { 资格规则?: any; 评审方案?: any }) {
  return post<{ 编号: number; 状态: string }>('/YProject/保存院级项目申报批次', {
    ...data,
    资格规则: typeof data.资格规则 === 'string' ? data.资格规则 : JSON.stringify(data.资格规则 || {}),
    评审方案: typeof data.评审方案 === 'string' ? data.评审方案 : JSON.stringify(data.评审方案 || {})
  })
}

export function publishInternalProjectBatch(编号: number, 状态: '发布' | '关闭' = '发布') {
  return post('/YProject/发布院级项目申报批次', undefined, { params: { 编号, 状态 } })
}

export function getInternalProjectReviewPlan(批次编号: number) {
  return get<InternalProjectBatch & { 评审方案: Record<string, any>; 资格规则: Record<string, any> }>('/YProject/获取院级项目评审方案', { 批次编号 })
}

// 分页获取科研项目申报列表
export function getVerticalApplications(
  paging: { index: number; size: number },
  filter?: { Like项目名称?: string; Like名称?: string; Like申报人姓名?: string; 开始日期?: string; 结束日期?: string }
) {
  const query: Record<string, any> = {
    Index: paging.index,
    Size: paging.size
  }
  const nameKeyword = filter?.Like项目名称 || filter?.Like名称
  if (nameKeyword) {
    query.Like项目名称 = nameKeyword
  }
  return get<{ total: number; list: VerticalApplicationItem[] }>(
    '/YProject/分页获取科研项目申报',
    query
  )
}

// 申报提交前预检查
export function preCheckApplication(data: any) {
  const payload = buildApplicationPayload(data, false)
  return post<PreCheckResult>('/YProject/预检查纵向项目申报', payload)
}

// 新增并正式提交纵向项目申报
export function submitVerticalApplication(data: any, isHold: boolean = false) {
  const payload = buildApplicationPayload(data, isHold)
  return post('/YProject/新增科研项目申报', payload)
}

export interface TaskbookItem {
  编号: number
  纵向项目立项编号: number
  版本号: number
  任务书标题: string
  任务书内容?: any
  文件路径?: string | null
  文件校验和?: string | null
  生成方式: string
  生成说明?: string | null
  状态: string
  生成人人员编号?: number
  生效时间?: string | null
  建立时间?: string
  更新时间?: string
}

export interface TaskbookSignatureItem {
  编号: number
  任务书编号: number
  签署人编号: number
  签署人姓名?: string
  签署角色: string
  签署方式: string
  签署声明: string
  状态: string
  签署说明?: string | null
  请求时间?: string
  确认时间?: string | null
}

export interface ProjectTaskbookData {
  任务书: TaskbookItem[]
  签署记录: TaskbookSignatureItem[]
}

export interface ProjectArchiveChecklistItem {
  编号: number
  纵向项目立项编号: number
  材料代码: string
  材料名称: string
  是否必需: boolean
  要求说明?: string | null
  状态: '缺失' | '已上传' | '已核验' | '不适用' | '作废' | string
  关联档案材料编号?: number | null
  核验说明?: string | null
  核验人编号?: number | null
  核验时间?: string | null
}

export interface ProjectArchiveChecklistData {
  项目编号: number
  完整: boolean
  缺失必需材料数: number
  清单: ProjectArchiveChecklistItem[]
}

export function getProjectTaskbook(projectId: number) {
  return get<ProjectTaskbookData>('/YProject/获取纵向项目任务书', { 纵向项目立项编号: projectId })
}

export function generateProjectTaskbook(data: {
  纵向项目立项编号: number
  任务书标题?: string
  任务书内容?: string
  文件路径?: string
  文件校验和?: string
  生成说明?: string
  上传修订?: boolean
}) {
  return post<{ 编号: number; 版本号: number; 状态: string }>('/YProject/生成纵向项目任务书', data)
}

export function requestTaskbookSignature(data: {
  任务书编号: number
  签署人编号: number
  签署角色: string
}) {
  return post('/YProject/发起纵向项目任务书内部签署', data)
}

export function confirmTaskbookSignature(data: { 签署记录编号: number; 签署说明?: string }) {
  return post('/YProject/确认纵向项目任务书内部签署', data)
}

export function activateProjectTaskbook(taskbookId: number) {
  return post('/YProject/生效纵向项目任务书', { 任务书编号: taskbookId })
}

export function getProjectArchiveChecklist(projectId: number) {
  return get<ProjectArchiveChecklistData>('/YProject/获取纵向项目电子档案清单', { 纵向项目立项编号: projectId })
}

export function initializeProjectArchiveChecklist(projectId: number) {
  return post<ProjectArchiveChecklistData>('/YProject/初始化纵向项目电子档案清单', { 纵向项目立项编号: projectId })
}

export function saveProjectArchiveChecklistItem(data: {
  纵向项目立项编号: number
  材料代码: string
  材料名称: string
  是否必需?: boolean
  要求说明?: string
  状态?: string
  核验说明?: string
}) {
  return post('/YProject/保存纵向项目电子档案清单项', data)
}

export function verifyProjectArchiveChecklist(projectId: number) {
  return post<ProjectArchiveChecklistData>('/YProject/核验纵向项目电子档案清单', { 纵向项目立项编号: projectId })
}

// 获取申报评审汇总与版本追踪
export function getApplicationReviewSummary(申报编号: number) {
  return get<ApplicationReviewSummary>('/YProject/获取纵向项目申报评审汇总', {
    纵向项目申报编号: 申报编号
  })
}

// 比较申报版本差异
export function compareApplicationVersions(申报编号: number, leftVersion: number, rightVersion: number) {
  return get<VersionDiffResult>('/YProject/比较纵向项目申报版本', {
    纵向项目申报编号: 申报编号,
    左版本号: leftVersion,
    右版本号: rightVersion
  })
}

// 录入专家评审意见
export function addReviewOpinion(data: {
  流程模板编号?: number
  项目编号: number
  步骤编号?: number | null
  意见类型: string
  意见内容: string
  是否公开给申请人?: boolean
}) {
  return post('/YProject/新增纵向项目评审意见', {
    流程模板编号: data.流程模板编号 || 400,
    项目编号: data.项目编号,
    步骤编号: data.步骤编号 ?? null,
    意见类型: data.意见类型,
    意见内容: data.意见内容,
    是否公开给申请人: data.是否公开给申请人 ?? true
  })
}

// 录入未获资助复盘
export function addUnfundedReview(data: {
  纵向项目申报编号: number
  原因分类: string
  复盘结论: string
  改进措施: string
  负责人编号?: number
}) {
  return post('/YProject/新增未获资助复盘', data)
}

export interface CreateMatchingProjectParams {
  来源项目立项编号: number
  配套金额?: number
  备注?: string
}

export interface CreateBalanceProjectParams {
  来源项目结题编号: number
  结余金额?: number
  备注?: string
}

// 一键建立配套项目 (文献清华长庚实践: 耗时从20分钟降至2秒)
export function createMatchingProject(data: CreateMatchingProjectParams) {
  return post<any>('/YProject/一键建立配套项目', data)
}

// 一键建立结余项目 (文献清华长庚实践: 结题结余资金快速转入发展基金)
export function createBalanceProject(data: CreateBalanceProjectParams) {
  return post<any>('/YProject/一键建立结余项目', data)
}

export interface MilestoneAlertItem {
  编号: number
  项目类型: string
  项目编号: number
  项目名称: string
  负责人编号: number
  负责人姓名: string
  预警类型: string
  预警级别: string
  节点截止日期: string
  剩余天数: number
  当前预算执行率: number
  预警提示内容: string
  处理状态: string
  通知渠道: string
  最近巡检时间: string
  建立时间: string
}

export interface ProjectComplianceResult {
  passed: boolean
  block: boolean
  message: string
  check_type?: string
  ethics_status?: string
  expiry_date?: string
}

// 获取过程预警巡检台账 (文献清华长庚模式: 中检结题半年前预警)
export function getMilestoneAlerts(params?: {
  projectId?: number
  alertType?: string
  status?: string
}) {
  return get<MilestoneAlertItem[]>('/YProject/获取过程预警巡检台账', params)
}

// 手动或定时触发过程主动巡检
export function triggerMilestoneInspection() {
  return post<{
    success: boolean
    total_scanned: number
    alerts_generated: number
    blocked_count: number
    message: string
  }>('/YProject/触发过程主动巡检', {})
}

// 确认并消警过程预警
export function confirmMilestoneAlert(data: {
  预警编号: number
  处理说明?: string
}) {
  return post<{ success: boolean; message: string }>('/YProject/确认过程预警', data)
}

// 过程节点前置合规性检查 (阻断机制: 伦理批件过期/未上传强拦截)
export function checkProjectCompliance(projectId: number, checkType: string = '中检') {
  return get<ProjectComplianceResult>('/YProject/项目过程前置合规检查', {
    projectId,
    checkType
  })
}

// 登记伦理批文续展与修正案 (消除合规阻断)
export function renewEthicsApproval(data: {
  项目编号: number
  新批文编号: string
  申请类型?: string
  新有效期至: string
  批件文件路径?: string
  变更事由与说明?: string
}) {
  return post<{
    success: boolean
    record_id: number
    project_id: number
    new_ethics_no: string
    new_expiry: string
    message: string
  }>('/YProject/登记伦理批文续展', data)
}


