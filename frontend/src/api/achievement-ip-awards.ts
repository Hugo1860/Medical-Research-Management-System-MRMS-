import { get, post } from '@/utils/request'

export interface AchievementExtension {
  编号?: number
  成果类别: string
  成果名称: string
  年度?: number
  完成日期?: string
  负责人编号?: number
  负责人姓名?: string
  关联成果类型?: number
  关联成果编号?: number
  关联项目编号?: number
  成果摘要?: string
  扩展字段?: Record<string, unknown>
  附件路径?: string
  审核状态?: string
  审核意见?: string
}

export interface IntellectualProperty {
  编号?: number
  知识产权类型: string
  名称: string
  状态?: string
  申请号?: string
  申请日期?: string
  公开编号?: string
  公开日期?: string
  证书号?: string
  授权日期?: string
  法律状态?: string
  权利人?: string
  负责人编号?: number
  负责人姓名?: string
  代理机构?: string
  代理联系人?: string
  关联项目编号?: number
  总费用?: number
  已缴费用?: number
  下次缴费日期?: string
  摘要?: string
  扩展字段?: Record<string, unknown>
  附件路径?: string
  待缴费提醒数?: number
}

export interface AwardBatch {
  编号?: number
  计划年度: number
  批次名称: string
  获奖类别: string
  申报开始日期: string
  申报截止日期: string
  评审开始日期?: string
  评审截止日期?: string
  申报限额?: number
  申报资格规则?: Record<string, unknown>
  评审方案?: Record<string, unknown>
  状态?: string
  批次说明?: string
  申报数?: number
}

export interface AwardApplication {
  编号?: number
  批次编号: number
  批次名称?: string
  申报名称: string
  申报人编号?: number
  申报人姓名?: string
  获奖类别?: string
  拟申报级别?: string
  成果摘要?: string
  关联成果类型?: number
  关联成果编号?: number
  关联项目编号?: number
  申报材料路径?: string
  状态?: string
  提交时间?: string
  平均评分?: number
}

export function listAchievementExtensions(params: { 成果类别?: string; 审核状态?: string } = {}) { return get<AchievementExtension[]>('/AchievementIp/查询成果扩展记录', params) }
export function saveAchievementExtension(data: AchievementExtension) { return post('/AchievementIp/保存成果扩展记录', data) }
export function reviewAchievementExtension(data: { 编号: number; 审核状态: '已通过' | '已退回'; 审核意见?: string }) { return post('/AchievementIp/审核成果扩展记录', data) }

export function listIntellectualProperties(params: { 知识产权类型?: string; 状态?: string; 仅待缴费?: boolean } = {}) { return get<IntellectualProperty[]>('/AchievementIp/查询知识产权', params) }
export function saveIntellectualProperty(data: IntellectualProperty) { return post('/AchievementIp/保存知识产权', data) }
export function listIpEvents(知识产权编号: number) { return get('/AchievementIp/查询知识产权事件', { 知识产权编号 }) }
export function saveIpEvent(data: { 知识产权编号: number; 事件类型: string; 事件日期?: string; 状态?: string; 事件说明?: string; 凭证编号?: string; 金额?: number; 下次缴费日期?: string }) { return post('/AchievementIp/记录知识产权事件', data) }
export function listIpPaymentReminders(仅待处理 = true) { return get('/AchievementIp/查询知识产权缴费提醒', { 仅待处理 }) }
export function updateIpPaymentReminder(data: { 编号: number; 状态: '已提醒' | '已缴费' | '已取消'; 处理说明?: string }) { return post('/AchievementIp/更新知识产权缴费提醒', data) }

export function listAwardBatches(状态?: string) { return get<AwardBatch[]>('/AchievementIp/查询获奖申报批次', { 状态 }) }
export function saveAwardBatch(data: AwardBatch) { return post('/AchievementIp/保存获奖申报批次', data) }
export function listAwardApplications(params: { 状态?: string; 批次编号?: number } = {}) { return get<AwardApplication[]>('/AchievementIp/查询获奖申报', params) }
export function saveAwardApplication(data: AwardApplication) { return post('/AchievementIp/保存获奖申报', data) }
export function submitAwardApplication(申报编号: number) { return post('/AchievementIp/提交获奖申报', undefined, { params: { 申报编号 } }) }
export function assignAwardReviewer(data: { 申报编号: number; 评审专家编号: number }) { return post('/AchievementIp/指派获奖评审', data) }
export function listAwardReviews(申报编号: number) { return get('/AchievementIp/查询获奖评审', { 申报编号 }) }
export function saveAwardReview(data: { 申报编号: number; 评审专家编号?: number; 是否回避: boolean; 回避原因?: string; 评分?: number; 评审意见?: string; 推荐结果?: string }) { return post('/AchievementIp/保存获奖评审', data) }
export function certifyAward(data: { 申报编号: number; 认定结果: '待认定' | '已认定' | '不通过'; 成果获奖编号?: number; 获奖名称?: string; 获奖级别?: string; 颁奖单位?: string; 证书编号?: string; 获奖日期?: string; 认定意见?: string; 证书路径?: string }) { return post('/AchievementIp/认定获奖申报', data) }
export function listAwardCertifications(申报编号?: number) { return get('/AchievementIp/查询获奖认定', { 申报编号 }) }
