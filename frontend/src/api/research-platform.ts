import request, { upload } from '@/utils/request'

export interface ResearchPlatform {
  编号?: number
  平台名称: string
  平台类型?: string
  依托部门编号?: number
  依托部门名称?: string
  负责人编号?: number
  负责人姓名?: string
  平台简介?: string
  开放状态?: string
  联系方式?: string
  规章制度路径?: string
  年度报告年份?: number
  年度报告路径?: string
  是否启用?: boolean
  成员数?: number
}

export interface ResearchService {
  编号?: number
  服务编码: string
  服务名称: string
  服务分类?: string
  服务说明?: string
  所需材料模板路径?: string
  材料模板文件名?: string
  办理角色?: string
  办理人员编号?: number
  办理人员?: string
  办理人员姓名?: string
  办理时限天数?: number
  咨询电话?: string
  是否启用?: boolean
  申请总数?: number
}

export interface ResearchServiceApplication {
  编号?: number
  服务事项编号: number
  服务编码?: string
  服务名称?: string
  服务分类?: string
  办理角色?: string
  办理人员编号?: number
  办理人员姓名?: string
  关联业务类型?: string
  关联业务编号?: number
  申请说明?: string
  附件路径?: string
  状态?: string
  当前处理人姓名?: string
  当前处理角色?: string
  受理时间?: string
  完成时间?: string
  办理期限?: string
  处理意见?: string
  创建时间?: string
  评价星级?: number
  反馈内容?: string
}

export function listResearchPlatforms(params: { 平台类型?: string; 仅启用?: boolean } = {}) {
  return request.get<ResearchPlatform[]>('/ResearchPlatform/查询科研平台', { params })
}

export function saveResearchPlatform(data: ResearchPlatform) {
  return request.post<{ 编号: number }>('/ResearchPlatform/保存科研平台', data)
}

export function listResearchPlatformMembers(平台编号: number) {
  return request.get('/ResearchPlatform/查询科研平台成员', { params: { 平台编号 } })
}

export function listResearchServices(params: { 服务分类?: string; 仅启用?: boolean } = {}) {
  return request.get<ResearchService[]>('/ResearchPlatform/查询科研服务事项', { params })
}

export function saveResearchService(data: ResearchService) {
  return request.post<{ 编号: number }>('/ResearchPlatform/保存科研服务事项', data)
}

/**
 * 管理员上传服务事项材料模板，返回系统生成的访问路径。
 */
export function uploadResearchServiceTemplate(file: File) {
  return upload<string[]>('/ResearchPlatform/上传科研服务材料模板', file, 'fileUpload')
}

export function createResearchServiceApplication(data: {
  服务事项编号: number
  关联业务类型?: string
  关联业务编号?: number
  申请说明?: string
  附件路径?: string
}) {
  return request.post<{ 编号: number; 状态: string }>('/ResearchPlatform/发起科研服务申请', data)
}

export function listMyResearchServiceApplications(状态?: string) {
  return request.get<ResearchServiceApplication[]>('/ResearchPlatform/查询我的科研服务申请', { params: { 状态 } })
}

export function listPendingResearchServices(状态 = '待受理') {
  return request.get<ResearchServiceApplication[]>('/ResearchPlatform/查询待受理科研服务', { params: { 状态 } })
}

export function processResearchServiceApplication(data: { 编号: number; 状态: string; 处理意见?: string }) {
  return request.post('/ResearchPlatform/处理科研服务申请', data)
}

export function rateResearchService(data: { 服务申请编号: number; 评价星级: number; 反馈内容?: string; 是否公开?: boolean }) {
  return request.post('/ResearchPlatform/评价科研服务', data)
}

export function listResearchImportExportLogs(params: { 模块?: string; 操作类型?: string; 数量?: number } = {}) {
  return request.get('/ResearchPlatform/查询科研导入导出日志', { params })
}
