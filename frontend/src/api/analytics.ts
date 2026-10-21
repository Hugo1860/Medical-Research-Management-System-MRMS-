import { get } from '@/utils/request'

/** P2 决策看板信号。所有接口均由后端按当前账号的管理权限过滤。 */
export interface AnalyticsRiskSignals {
  bottlenecks: any[]
  budgetAnomalies: any[]
  ethicsReminders: {
    提前天数?: number
    批件提醒?: any[]
    持续审查提醒?: any[]
  }
  severeAdverseEvents: any[]
}

export interface EthicsFollowupSummary {
  汇总?: {
    总记录数?: number
    严重不良事件数?: number
    待处理不良事件数?: number
    逾期持续审查数?: number
    修正案数?: number
  }
  严重事件?: any[]
  生成时间?: string
}

export function getBottleneckSummary(params?: { 事件类型?: string; 状态?: string; 严重程度?: string }) {
  return get<any[]>('/Analytics/获取科研瓶颈汇总', params)
}

export function getBudgetAnomalies(params?: { 流程模板编号?: number; 项目编号?: number }) {
  return get<any[]>('/Expense/获取预算异常对账', params)
}

export function getEthicsExpiryReminders(提前天数 = 60) {
  return get<AnalyticsRiskSignals['ethicsReminders']>('/Ethics/查询伦理到期提醒', { 提前天数 })
}

/**
 * P2 后端预留的伦理持续记录汇总接口。旧版本没有此接口时由看板降级为空，
 * 不会伪造严重不良事件数量；接入后端后即可显示真实计数。
 */
export function getEthicsFollowupSummary() {
  return get<EthicsFollowupSummary>('/Ethics/获取伦理持续记录汇总')
}

export function getAnalyticsDrilldown(indicator: string, quantity = 100) {
  return get<any>('/Analytics/获取科研统计钻取', { 指标: indicator, 数量: quantity })
}
