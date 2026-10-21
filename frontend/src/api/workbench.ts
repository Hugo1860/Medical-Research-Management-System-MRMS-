import { get } from '@/utils/request'

export interface WorkbenchSummaryItem {
  类型: string
  分类: string
  标题: string
  时间: string
  下一步: string
  办理提示: string
  业务编号: number
  步骤链接路径?: string
  办理人?: string
  办理角色?: string
}

export interface WorkbenchSummary {
  人员姓名?: string
  角色名称?: string
  待办数量?: number
  未读通知数量?: number
  已发起流程数量?: number
  最近待办事项?: string
  缺件数?: number
  缺件提示?: string
  最近待办路径?: string
  最近待办项目编号?: number
  最近待办创建时间?: string
  下一步办理人?: string
  下一步办理角色?: string
  办理提示?: string
  下一步流程名称?: string
  下一步状态说明?: string
}

// 获取科研工作台摘要：待办、未读通知、进行中流程与下一步事项
export function getWorkbenchSummary() {
  return get<WorkbenchSummary>('/Office/获取科研工作台摘要')
}

// 获取决策统计摘要
export function getDecisionSummary() {
  return get<any>('/Analytics/获取科研决策摘要')
}

// 获取统计概览（纵向项目/横向项目/成果）
export function getAnalyticsOverview(dimension?: string) {
  return get<any>('/Analytics/获取科研统计概览', { 维度: dimension })
}
