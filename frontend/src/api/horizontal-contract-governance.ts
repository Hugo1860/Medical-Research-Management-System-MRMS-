import { get, post } from '@/utils/request'

export function getContractGovernance(projectId: number) {
  return get<any>('/HorizontalContractGovernance/查询合同治理', { 横向项目编号: projectId })
}

export function getContractTemplates(onlyEnabled = true) {
  return get<any[]>('/HorizontalContractGovernance/查询合同模板', { 仅启用: onlyEnabled })
}

export function saveContractVersion(payload: any) { return post('/HorizontalContractGovernance/保存合同版本', payload) }
export function reviewPartnerQualification(payload: any) { return post('/HorizontalContractGovernance/核验合作方资质', payload) }
export function savePartnerQualification(payload: any) { return post('/HorizontalContractGovernance/保存合作方资质', payload) }
export function saveIncomeRecognition(payload: any) { return post('/HorizontalContractGovernance/保存收入认定', payload) }
