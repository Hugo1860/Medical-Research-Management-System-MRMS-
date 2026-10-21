import { get, post } from '@/utils/request'

export function getHostedConferences(status?: string, year?: number) {
  return get<any[]>('/AcademicExchange/查询主办会议', { 状态: status, 年度: year })
}

export function saveHostedConference(payload: any) {
  return post('/AcademicExchange/保存主办会议申请', payload)
}

export function auditHostedConference(payload: any) {
  return post('/AcademicExchange/审核主办会议', payload)
}

export function getAcademicExchangePersons(direction?: string, status?: string) {
  return get<any[]>('/AcademicExchange/查询学术交流人员', { 交流方向: direction, 状态: status })
}

export function saveAcademicExchangePerson(payload: any) {
  return post('/AcademicExchange/保存学术交流人员', payload)
}

export function getAcademicExchangeStatistics(year?: number) {
  return get<any>('/AcademicExchange/统计学术交流', { 年度: year })
}
