import type { EthicsApplication } from '@/api/ethics'

export interface EthicsMobileSummaryInput {
  applications: EthicsApplication[]
  expiryReminders?: {
    批件提醒?: unknown[]
    持续审查提醒?: unknown[]
  } | null
  followups?: Array<{
    记录类型?: string
    严重程度?: string
  }>
  reviewerActions?: number
}

export interface EthicsMobileSummary {
  initialReview: number
  amendments: number
  followups: number
  seriousEvents: number
  expiryReminders: number
  reviewerActions: number
  archivePending: number
  visibleApplications: number
}

/**
 * 根据服务端已过滤的伦理数据生成移动摘要。该函数不补充任何默认业务记录，
 * 只对接口返回的数据做分类统计，避免移动端扩大权限范围或显示演示数据。
 */
export function buildEthicsMobileSummary(input: EthicsMobileSummaryInput): EthicsMobileSummary {
  const applications = input.applications || []
  const activeStatuses = new Set(['待受理', '审查中', '需补正'])
  const initialReview = applications.filter((item) => item.申请类型 === '初始审查' && activeStatuses.has(item.状态)).length
  const amendments = applications.filter((item) => item.申请类型 === '修正案' && activeStatuses.has(item.状态)).length
  const expiryReminders = (input.expiryReminders?.批件提醒?.length || 0) + (input.expiryReminders?.持续审查提醒?.length || 0)
  const seriousEvents = (input.followups || []).filter((item) => item.记录类型 === '不良事件' && ['严重', '危及生命'].includes(item.严重程度 || '')).length
  const archivePending = applications.filter((item) => item.状态 === '已批准' && !item.批件路径).length
  return {
    initialReview,
    amendments,
    followups: input.expiryReminders?.持续审查提醒?.length || 0,
    seriousEvents,
    expiryReminders,
    reviewerActions: input.reviewerActions || 0,
    archivePending,
    visibleApplications: applications.length
  }
}
