import request from '@/utils/request'

export interface EducationStatisticsTask {
  编号?: number
  统计年度: number
  任务代码: string
  任务名称: string
  截止日期?: string
  数据截止日期?: string
  状态?: string
  当前版本编号?: number
  当前版本号?: number
  说明?: string
  数据包数量?: number
  导出任务数量?: number
}

export interface EducationStatisticsVersion {
  编号?: number
  任务编号: number
  版本号: number
  模板代码: string
  模板名称: string
  字段定义: any[] | string
  状态?: string
  变更说明?: string
}

export interface EducationStatisticsPackage {
  编号?: number
  任务编号: number
  版本编号: number
  包名称: string
  文件类型?: string
  文件路径?: string
  文件校验和?: string
  状态?: string
  记录数?: number
  校验错误数?: number
  校验警告数?: number
  说明?: string
}

export function listEducationStatisticsTasks(params: { 统计年度?: number; 状态?: string } = {}) {
  return request.get<EducationStatisticsTask[]>('/EducationStatistics/查询统计任务', { params })
}

export function saveEducationStatisticsTask(data: EducationStatisticsTask) {
  return request.post('/EducationStatistics/保存统计任务', data)
}

export function listEducationStatisticsVersions(任务编号: number) {
  return request.get<EducationStatisticsVersion[]>('/EducationStatistics/查询统计版本', { params: { 任务编号 } })
}

export function saveEducationStatisticsVersion(data: EducationStatisticsVersion) {
  return request.post('/EducationStatistics/保存统计版本', {
    ...data,
    字段定义: typeof data.字段定义 === 'string' ? data.字段定义 : JSON.stringify(data.字段定义)
  })
}

export function publishEducationStatisticsVersion(版本编号: number) {
  return request.post('/EducationStatistics/发布统计版本', null, { params: { 版本编号 } })
}

export function listEducationStatisticsPackages(params: { 任务编号?: number; 状态?: string } = {}) {
  return request.get<EducationStatisticsPackage[]>('/EducationStatistics/查询统计数据包', { params })
}

export function registerEducationStatisticsPackage(data: EducationStatisticsPackage & { 数据内容?: string }) {
  return request.post('/EducationStatistics/登记统计数据包', data)
}

export function validateEducationStatisticsPackage(数据包编号: number) {
  return request.post('/EducationStatistics/校验统计数据包', null, { params: { 数据包编号 } })
}

export function listEducationStatisticsValidations(数据包编号: number, 严重程度?: string) {
  return request.get('/EducationStatistics/查询统计字段校验', { params: { 数据包编号, 严重程度 } })
}

export function adjustEducationStatisticsField(data: {
  数据包编号: number
  记录序号: number
  记录标识?: string
  字段代码: string
  新值: string
  调整原因: string
  请求编号?: string
}) {
  return request.post('/EducationStatistics/调整统计字段', data)
}

export function listEducationStatisticsAdjustments(数据包编号: number) {
  return request.get('/EducationStatistics/查询统计调整留痕', { params: { 数据包编号 } })
}

export function createEducationStatisticsExport(data: { 任务编号: number; 版本编号: number; 数据包编号: number; 导出格式?: string }) {
  return request.post('/EducationStatistics/创建统计导出任务', data)
}

export function listEducationStatisticsExports(params: { 任务编号?: number; 状态?: string } = {}) {
  return request.get('/EducationStatistics/查询统计导出任务', { params })
}

export function listEducationStatisticsAudit(params: { 任务编号?: number; 数据包编号?: number; 数量?: number } = {}) {
  return request.get('/EducationStatistics/查询统计操作审计', { params })
}

export function getHealthCommissionReport(params: { 年度?: number } = {}) {
  return request.get<any>('/EducationStatistics/获取卫健委科技统计年报', { params })
}

export function getSecondaryHospitalEvidenceDossier(params: { 年度?: number } = {}) {
  return request.get<any>('/EducationStatistics/获取二级公立医院国考与等级评审佐证包', { params })
}
