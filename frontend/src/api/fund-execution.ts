import { get, post } from '@/utils/request'

export function getOutsourceFunds(templateId: number, projectId: number) {
  return get<any[]>('/FundExecution/查询外拨经费', { 流程模板编号: templateId, 项目编号: projectId })
}

export function saveOutsourceFund(payload: any) {
  return post('/FundExecution/保存外拨经费', payload)
}

export function auditOutsourceFund(payload: any) {
  return post('/FundExecution/审核外拨经费', payload)
}

export function getFundCarryovers(templateId: number, projectId: number) {
  return get<any[]>('/FundExecution/查询经费结转', { 流程模板编号: templateId, 项目编号: projectId })
}

export function applyFundCarryover(payload: any) {
  return post('/FundExecution/申请经费结转', payload)
}

export function getFundSplitSchemes(templateId: number, projectId: number) {
  return get<any[]>('/FundExecution/查询经费分割方案', { 流程模板编号: templateId, 项目编号: projectId })
}

export function saveFundSplitScheme(payload: any) {
  return post('/FundExecution/保存经费分割方案', payload)
}

export function getProjectBalanceDisposals(templateId: number, projectId: number) {
  return get<any[]>('/FundExecution/查询结项结余', { 流程模板编号: templateId, 项目编号: projectId })
}

export function registerProjectBalanceDisposal(payload: any) {
  return post('/FundExecution/登记结项结余', payload)
}
