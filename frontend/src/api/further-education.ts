import { get, post } from '@/utils/request'

/** 进修年度计划 */
export interface FurtherEducationPlan {
  编号?: number
  计划年度: number
  科室编号?: number
  科室名称?: string
  专业方向?: string
  目标医院?: string
  拟派人数: number
  培训费预算: number
  交通费预算: number
  住宿费预算: number
  补助预算: number
  总预算?: number
  是否允许计划外?: boolean
  状态?: string
  备注?: string
  建立时间?: string
  更新时间?: string
}

/** 进修个人申请 */
export interface FurtherEducationApplication {
  编号?: number
  计划编号?: number
  计划年度?: number
  申请人编号?: number
  申请人工号?: string
  申请人姓名?: string
  申请人部门名称?: string
  部门编号?: number
  科室编号?: number
  进修医院: string
  进修基地?: string
  专业方向: string
  导师?: string
  开始日期: string
  结束日期: string
  进修天数?: number
  培训费: number
  交通费: number
  住宿费: number
  补助费: number
  预算金额?: number
  预算状态?: string
  经费来源?: string
  是否计划外?: boolean
  计划外原因?: string
  申请理由: string
  状态?: string
  当前步骤?: string
  流程编号?: number
  建立时间?: string
  更新时间?: string
}

export interface FurtherEducationWorkflowNode {
  编号?: number
  顺序: number
  节点名称: string
  节点类型?: string
  审批角色: string
  是否启用: boolean
  办理期限天数?: number
  条件说明?: string
}

export interface FurtherEducationWorkflowTemplate {
  编号?: number
  名称: string
  版本号?: number
  是否启用: boolean
  是否已发布?: boolean
  说明?: string
  节点列表: FurtherEducationWorkflowNode[]
  更新时间?: string
}

export interface FurtherEducationBudgetLedger {
  计划编号?: number
  计划年度: number
  批准预算: number
  已预占金额: number
  审批中金额: number
  已核销金额: number
  可用余额: number
  明细?: Array<{
    申请编号?: number
    申请人姓名?: string
    金额: number
    状态?: string
    更新时间?: string
  }>
}

export interface FurtherEducationAuditItem {
  编号?: number
  业务类型?: string
  业务编号?: number
  操作类型?: string
  操作人姓名?: string
  操作时间?: string
  原状态?: string
  新状态?: string
  说明?: string
  请求编号?: string
}

export interface FurtherEducationTodo {
  编号: number
  进修申请编号: number
  节点序号?: number
  接收角色?: string
  标题?: string
  状态?: string
  创建时间?: string
  到期时间?: string
  目标医院?: string
  进修专业?: string
  申请人姓名?: string
  预算金额?: number
}

export interface FurtherEducationNotification {
  编号: number
  进修申请编号: number
  通知类型?: string
  标题: string
  内容?: string
  是否已读?: boolean
  创建时间?: string
}

const base = '/FurtherEducation'

export async function listFurtherEducationPlans(params?: { 计划年度?: number; 科室编号?: number; 状态?: string }) {
  const result = await get<any>(`${base}/查询进修年度计划`, {
    年度: params?.计划年度,
    部门编号: params?.科室编号,
    状态: params?.状态
  })
  const rows = Array.isArray(result) ? result : (result?.list || result?.items || [])
  return rows.map((row: any): FurtherEducationPlan => ({
    ...row,
    计划年度: row.计划年度 ?? row.年度,
    科室编号: row.科室编号 ?? row.部门编号,
    科室名称: row.科室名称 ?? row.部门名称,
    拟派人数: row.拟派人数 ?? row.计划人数 ?? 0,
    总预算: row.总预算 ?? row.预算总额 ?? 0
  }))
}

export function saveFurtherEducationPlan(data: FurtherEducationPlan) {
  return post<{ 编号: number }>(`${base}/保存进修年度计划`, {
    ...data,
    年度: data.计划年度,
    部门编号: data.科室编号,
    计划人数: data.拟派人数,
    预算总额: data.总预算 ?? Number(data.培训费预算 || 0) + Number(data.交通费预算 || 0) + Number(data.住宿费预算 || 0) + Number(data.补助预算 || 0)
  })
}

export function publishFurtherEducationPlan(编号: number) {
  return post(`${base}/发布进修年度计划`, undefined, { params: { 计划编号: 编号 } })
}

export async function listFurtherEducationApplications(params?: {
  状态?: string
  申请人编号?: number
  计划年度?: number
  Index?: number
  Size?: number
}) {
  const result = await get<any>(`${base}/查询进修申请`, {
    年度: params?.计划年度,
    状态: params?.状态,
    Index: params?.Index,
    Size: params?.Size
  })
  const rows = Array.isArray(result) ? result : (result?.list || result?.items || [])
  return rows.map((row: any): FurtherEducationApplication => ({
    ...row,
    计划编号: row.计划编号 ?? row.年度计划编号,
    申请人部门名称: row.申请人部门名称 ?? row.部门名称,
    进修医院: row.进修医院 ?? row.目标医院,
    专业方向: row.专业方向 ?? row.进修专业,
    开始日期: row.开始日期 ?? row.进修开始日期,
    结束日期: row.结束日期 ?? row.进修结束日期,
    申请理由: row.申请理由 ?? row.申请说明,
    预算金额: row.预算金额 ?? 0
  }))
}

export function saveFurtherEducationApplication(data: FurtherEducationApplication) {
  return post<{ 编号: number }>(`${base}/保存进修申请`, {
    ...data,
    年度计划编号: data.计划编号,
    部门编号: (data as any).部门编号 ?? (data as any).科室编号,
    目标医院: data.进修医院,
    进修专业: data.专业方向,
    进修开始日期: data.开始日期,
    进修结束日期: data.结束日期,
    申请说明: data.申请理由
  })
}

export function submitFurtherEducationApplication(编号: number) {
  return post<{ 流程编号?: number }>(`${base}/提交进修申请`, { 申请编号: 编号 })
}

export function lockFurtherEducationBudget(申请编号: number) {
  return post(`${base}/锁定进修预算`, { 申请编号 })
}

export function releaseFurtherEducationBudget(申请编号: number) {
  return post(`${base}/释放进修预算`, { 申请编号 })
}

export async function listFurtherEducationWorkflowTemplates(): Promise<FurtherEducationWorkflowTemplate[]> {
  const result = await get<any>(`${base}/查询流程模板`)
  const rows = Array.isArray(result) ? result : (result?.list || result?.items || [])
  return rows.map((config: any): FurtherEducationWorkflowTemplate => ({
    编号: config.编号,
    名称: config.名称 || '人员进修审批流程',
    版本号: config.版本号,
    是否启用: config.是否启用 ?? config.状态 === '生效',
    是否已发布: config.是否已发布 ?? config.状态 === '生效',
    说明: config.说明 ?? config.变更说明,
    节点列表: (config.节点列表 || config.节点 || []).map((node: any) => ({
      编号: node.编号,
      顺序: node.顺序 ?? node.节点序号 ?? node.序号,
      节点名称: node.节点名称 ?? node.名称,
      节点类型: node.节点类型 ?? node.类型,
      审批角色: node.审批角色 ?? node.处理角色 ?? node.角色,
      是否会签: Boolean(node.是否会签),
      是否启用: node.是否启用 !== false,
      办理期限天数: node.办理期限天数
    })),
    更新时间: config.更新时间 ?? config.发布时间 ?? config.创建时间
  }))
}

export function saveFurtherEducationWorkflowTemplate(data: FurtherEducationWorkflowTemplate) {
  return post<{ 编号: number }>(`${base}/保存流程配置`, {
    ...data,
    节点: (data.节点列表 || []).map((node) => ({
      ...node,
      序号: node.顺序,
      名称: node.节点名称,
      类型: node.节点类型 || '审批',
      角色: node.审批角色
    }))
  })
}

export function publishFurtherEducationWorkflowTemplate(编号: number) {
  return post(`${base}/发布进修流程配置`, undefined, { params: { 配置编号: 编号 } })
}

export function getFurtherEducationBudgetLedger(params?: { 计划编号?: number; 计划年度?: number }) {
  return get<FurtherEducationBudgetLedger>(`${base}/查询预算台账`, { ...params, 年度: params?.计划年度 })
}

export function listFurtherEducationAudit(params?: { 业务编号?: number; Limit?: number }) {
  return get<FurtherEducationAuditItem[]>(`${base}/查询审计记录`, params)
}

export function listFurtherEducationTodos(仅未处理 = true) {
  return get<FurtherEducationTodo[]>(`${base}/获取进修待办`, { 仅未处理 })
}

export function listFurtherEducationNotifications(仅未读 = true) {
  return get<FurtherEducationNotification[]>(`${base}/查询进修通知`, { 仅未读 })
}

export function markFurtherEducationNotificationRead(通知编号: number) {
  return post(`${base}/标记进修通知已读`, undefined, { params: { 通知编号 } })
}
