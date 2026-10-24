import request from '@/utils/request'

// ==================== 1. 医疗新技术新项目与适宜技术 ====================

export interface NewTechProjectQuery {
  申报年度?: number
  申报类别?: string
  难度等级?: string
  审批状态?: string
  科室编号?: number
  关键词?: string
  pageIndex?: number
  pageSize?: number
}

export function listNewTechProjects(params: NewTechProjectQuery = {}) {
  return request.get<{ total: number; list: any[] }>('/NewTechnology/获取新技术新项目列表', { params })
}

export function getNewTechDetail(id: number) {
  return request.get<any>('/NewTechnology/获取新技术详情', { params: { 编号: id } })
}

export function saveNewTechProject(data: any) {
  return request.post<{ success: boolean; 编号: number; 状态: string; message: string }>('/NewTechnology/保存新技术申报', data)
}

export function reviewNewTechProject(data: {
  编号: number
  审核部门: '医务处' | '科教处'
  通过: boolean
  意见?: string
  伦理审查编号?: string
  伦理审查状态?: string
}) {
  return request.post<{ success: boolean; 审批状态: string; message: string }>('/NewTechnology/医务科教准入审查', data)
}

export function listNewTechFollowups(params: { 新技术编号?: number; 是否发生不良反应?: boolean; pageIndex?: number; pageSize?: number }) {
  return request.get<{ total: number; list: any[] }>('/NewTechnology/获取随访例数列表', { params })
}

export function saveNewTechFollowup(data: any) {
  return request.post<{ success: boolean; 编号: number; message: string }>('/NewTechnology/登记随访例数', data)
}

export function applyNewTechRegularOrAward(data: any) {
  return request.post<{ success: boolean; 编号: number; message: string }>('/NewTechnology/申请转常规或评奖', data)
}

export function reviewNewTechRegularOrAward(data: { 编号: number; 通过: boolean; 审批意见?: string }) {
  return request.post<{ success: boolean; message: string }>('/NewTechnology/审批转常规或评奖', data)
}

export function listAppropriateTechs(params: { 牵头科室编号?: number; 受援基层单位?: string; 推广形式?: string; pageIndex?: number; pageSize?: number } = {}) {
  return request.get<{ total: number; list: any[] }>('/NewTechnology/获取适宜技术推广列表', { params })
}

export function saveAppropriateTech(data: any) {
  return request.post<{ success: boolean; 编号: number; message: string }>('/NewTechnology/登记适宜技术推广', data)
}

export function getMedicalAdvancementDashboard() {
  return request.get<any>('/NewTechnology/获取新技术与适宜技术大盘统计')
}

// ==================== 2. 科普成果与典型案例库 ====================

export function listPopularScience(params: { 分类?: string; 形式类型?: string; 审核状态?: string; 第一作者编号?: number; 关键词?: string; pageIndex?: number; pageSize?: number } = {}) {
  return request.get<{ total: number; list: any[] }>('/PopularScience/获取成果列表', { params })
}

export function savePopularScience(data: any) {
  return request.post<{ success: boolean; 编号: number; message: string }>('/PopularScience/保存成果登记', data)
}

export function auditPopularScience(data: { 编号: number; 通过: boolean; 审核意见?: string }) {
  return request.post<{ success: boolean; 审核状态: string; message: string }>('/PopularScience/审核成果入库', data)
}

export function getPopularScienceStats() {
  return request.get<any>('/PopularScience/获取科普与案例成效统计')
}

// ==================== 3. 职称晋升科教审查 ====================

export function getTitlePromotionPortfolio(params: { 人员编号: number; 拟晋升职称?: string; 现职称任职日期?: string }) {
  return request.get<any>('/People/获取职称晋升科教业绩', { params })
}

export function exportTitlePromotionFormHtml(data: { 人员编号: number; 拟晋升职称?: string; 现职称任职日期?: string }) {
  return request.post<{ 审核单编号: string; 防伪码: string; 人员姓名: string; html: string }>('/People/导出职称审查表HTML', data)
}

export function verifyTitlePromotionCode(code: string) {
  return request.get<any>('/People/验真职称审查单', { params: { 防伪码: code } })
}

// ==================== 4. 县域医共体协同 ====================

export function listCommunityPartners(params: { 单位类型?: string; 合作状态?: string } = {}) {
  return request.get<any[]>('/CommunityHealthcare/获取医共体合作单位列表', { params })
}

export function saveCommunityPartner(data: any) {
  return request.post<{ success: boolean; 编号: number; message: string }>('/CommunityHealthcare/保存医共体合作单位', data)
}

export function listCommunityTrainings(params: { 合作单位编号?: number; 业务类型?: string; 满六个月?: boolean; pageIndex?: number; pageSize?: number } = {}) {
  return request.get<{ total: number; list: any[] }>('/CommunityHealthcare/获取进修与帮扶记录列表', { params })
}

export function saveCommunityTraining(data: any) {
  return request.post<{ success: boolean; 编号: number; message: string }>('/CommunityHealthcare/保存进修与帮扶记录', data)
}
