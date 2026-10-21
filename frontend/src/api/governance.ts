import { get, post } from '@/utils/request'

export interface PermissionProxyItem {
  编号: number
  授权人编号: number
  授权人姓名: string
  被代理人编号: number
  被代理人姓名: string
  代理范围: string
  开始时间: string
  结束时间: string
  建立时间: string
  是否撤销: boolean
  撤销时间?: string
  撤销人姓名?: string
  撤销原因?: string
}

export interface ProxyApplyParams {
  被代理人编号: number
  代理范围: string
  开始时间: string
  结束时间: string
  备注?: string
}

export interface BudgetReconciliationItem {
  流程模板编号: number
  项目编号: number
  项目名称: string
  负责人姓名: string
  预算批复金额: number
  经费到账金额: number
  已报销金额: number
  待审报销金额: number
  对账异常差额: number
  处理状态?: string
  责任人姓名?: string
  处理说明?: string
  更新时间?: string
}

// 获取白名单代理业务范围
export function getProxyScopes() {
  return get<string[]>('/Access/获取代理范围')
}

// 获取当前人员关联的代理列表
export function getPersonProxies() {
  return get<PermissionProxyItem[]>('/Access/获取人员代理列表')
}

// 发起权限代理授权
export function grantProxy(data: ProxyApplyParams) {
  return post<number>('/Access/授权代理', data)
}

// 撤销指定代理授权
export function revokeProxy(data: { 代理编号: number; 撤销原因?: string }) {
  return post<void>('/Access/撤销代理', data)
}

// 批量回收某人代理
export function batchRevokeProxies(data: { 人员编号: number; 撤销原因: string }) {
  return post<number>('/Access/回收人员代理', data)
}

// 获取预算异常对账列表
export function getBudgetReconciliations(params?: { 流程模板编号?: number; 项目编号?: number }) {
  return get<BudgetReconciliationItem[]>('/Expense/获取预算异常对账', params)
}

// 处理预算异常对账
export function handleBudgetReconciliation(data: {
  流程模板编号: number
  项目编号: number
  处理状态: string
  处理说明: string
  责任人姓名?: string
}) {
  return post<void>('/Expense/处理预算异常对账', data)
}
