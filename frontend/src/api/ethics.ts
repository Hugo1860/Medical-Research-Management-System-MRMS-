import { get, post } from '@/utils/request'

export interface EthicsApplication {
  编号: number
  流程模板编号: number
  项目编号: number
  申请人编号: number
  申请人姓名?: string
  项目名称?: string
  申请类型: '初始审查' | '修正案' | '年度跟踪审查' | '延期续展'
  申请标题: string
  研究方案摘要?: string
  受试者风险等级: '低风险' | '一般风险' | '高风险'
  知情同意说明?: string
  附件路径?: string
  状态: '草稿' | '待受理' | '审查中' | '需补正' | '已批准' | '已拒绝' | '已撤回'
  提交时间?: string
  受理时间?: string
  决定时间?: string
  批件编号?: string
  有效期至?: string
  退回原因?: string
  /** 正式决定书或批件的受控文件路径；服务端未返回时保持 undefined。 */
  批件路径?: string
  /** 申请备注，可能只在详情接口中返回。 */
  备注?: string
  /** 服务端材料预检结果（兼容尚未升级的接口）。 */
  材料完整?: boolean
  缺失材料?: string[]
  /** P1: 服务端根据当前登录人员计算的指派/可见范围。缺失时前端按最小权限处理。 */
  可见范围?: '本人' | '指派委员' | '项目参与人' | '委员会管理' | string
  是否指派?: boolean
  本人已指派?: boolean
  当前用户已指派?: boolean
  委员可见?: boolean
  可提交意见?: boolean
}

export interface EthicsArchiveRecord {
  编号?: number
  申请编号?: number
  会议编号?: number
  决定编号?: number
  档案类型?: string
  文件名称?: string
  文件路径?: string
  文件校验值?: string
  状态?: string
  /** P1: 由服务端/对象存储返回的实际文件校验结果。 */
  实际校验状态?: '已验证' | '不匹配' | '文件不存在' | '未验证' | string
  校验时间?: string
  文件大小?: number
  内容类型?: string
  校验错误?: string
}

export interface EthicsArchiveVerification {
  实际校验状态?: string
  校验时间?: string
  文件大小?: number
  内容类型?: string
  校验错误?: string
}

export function getEthicsApplications(params?: { 状态?: string; 申请类型?: string; 关键词?: string; 项目编号?: number; 数量?: number }) {
  return get<EthicsApplication[]>('/Ethics/查询伦理申请', params)
}

export function saveEthicsApplication(data: Partial<EthicsApplication> & {
  流程模板编号: number
  项目编号: number
  申请类型: string
  申请标题: string
  研究方案摘要: string
  受试者风险等级?: string
  知情同意说明?: string
  附件路径?: string
  备注?: string
}) {
  return post<{ 编号: number; 状态: string; message: string }>('/Ethics/提交伦理申请', data)
}

export function submitEthicsApplication(id: number) {
  return post<{ 编号: number; 状态: string; message: string }>('/Ethics/提交伦理审查', id)
}

export function acceptEthicsApplication(data: { 申请编号: number; 结果: '受理' | '退回补正'; 退回原因?: string }) {
  return post<{ 编号: number; 状态: string }>('/Ethics/受理伦理申请', data)
}

export function saveEthicsOpinion(data: {
  申请编号: number
  会议编号?: number
  是否利益冲突: boolean
  意见结论: '同意' | '修改后同意' | '不同意' | '回避'
  意见内容?: string
  评分?: number
}) {
  return post<{ success: boolean; 申请编号: number; message: string }>('/Ethics/登记伦理委员意见', data)
}

export function saveEthicsDecision(data: {
  申请编号: number
  决定: '批准' | '修改后批准' | '补正后再审' | '拒绝' | '终止'
  决定说明: string
  批件编号?: string
  有效期至?: string
  决定书路径?: string
}) {
  return post<{ success: boolean; 编号: number; 状态: string; message: string }>('/Ethics/登记伦理决定', data)
}

export function saveEthicsFollowup(data: {
  申请编号: number
  记录类型: '年度跟踪审查' | '方案修正案' | '知情同意修订' | '不良事件' | '偏离方案'
  发生日期?: string
  下次审查日期?: string
  严重程度?: '一般' | '严重' | '危及生命'
  事件说明: string
  处理结论?: string
  附件路径?: string
}) {
  return post<{ 编号: number; 状态: string; message: string }>('/Ethics/提交伦理持续记录', data)
}

export function reviewEthicsFollowup(data: { 记录编号: number; 结果: '审查中' | '已批准' | '需补正' | '已关闭'; 处理结论?: string }) {
  return post<{ success: boolean; 编号: number; 状态: string }>('/Ethics/审核伦理持续记录', data)
}

export function getEthicsApplicationDetail(id: number) {
  return get<{
    申请: EthicsApplication
    委员意见: any[]
    决定: any[]
    持续记录: any[]
    可见范围?: string
    是否指派?: boolean
    委员可见?: boolean
    可提交意见?: boolean
  }>('/Ethics/获取伦理申请详情', { 申请编号: id })
}

export function getEthicsExpiryReminders(提前天数 = 60) {
  return get<{ 提前天数: number; 批件提醒: any[]; 持续审查提醒: any[] }>('/Ethics/查询伦理到期提醒', { 提前天数 })
}

export interface EthicsMeeting {
  编号: number
  会议序号: string
  会议时间: string
  会议地点?: string
  主持人编号?: number
  主持人姓名?: string
  状态: '计划中' | '进行中' | '已完成' | '已取消'
  议程说明?: string
  议程路径?: string
  会议纪要路径?: string
  纪要状态: '未开始' | '草稿' | '待确认' | '已确认' | '已归档'
  是否匿名表决?: boolean
  议程数量?: number
}

export function getEthicsMeetings(params?: { 状态?: string; 开始时间?: string; 结束时间?: string }) {
  return get<EthicsMeeting[]>('/Ethics/查询伦理会议', params)
}

export function saveEthicsMeeting(data: {
  编号?: number
  会议序号: string
  会议时间: string
  会议地点?: string
  主持人编号?: number
  状态?: EthicsMeeting['状态']
  议程说明?: string
  议程路径?: string
  备注?: string
  是否匿名表决?: boolean
}) {
  return post<{ 编号: number; 状态: string; message: string }>('/Ethics/保存伦理会议', data)
}

export function saveEthicsAgenda(data: { 会议编号: number; 申请编号: number; 排序号?: number; 议题说明?: string; 状态?: '已排期' | '已审查' | '已撤回' }) {
  return post<{ success: boolean; 会议编号: number; 申请编号: number; 状态: string }>('/Ethics/维护伦理会议议程', data)
}

export function getEthicsMeetingDetail(id: number) {
  return get<{ 会议: EthicsMeeting; 议程: any[]; 纪要: any; 电子档案: EthicsArchiveRecord[]; 可见范围?: string; 是否委员指派?: boolean }>('/Ethics/获取伦理会议详情', { 会议编号: id })
}

export function getEthicsAnonymousVote(id: number) {
  return get<{ 会议编号: number; 是否匿名表决: boolean; 匿名: boolean; 表决: any[]; message: string }>('/Ethics/获取伦理匿名表决', { 会议编号: id })
}

export function saveEthicsMinutes(data: { 会议编号: number; 纪要正文: string; 出席委员摘要?: string; 决议摘要?: string; 附件路径?: string; 状态?: '草稿' | '待确认' }) {
  return post<{ success: boolean; 会议编号: number; 状态: string; message: string }>('/Ethics/保存伦理会议纪要', data)
}

export function confirmEthicsMinutes(id: number) {
  return post<{ success: boolean; 会议编号: number; 状态: string; message: string }>('/Ethics/确认伦理会议纪要', id)
}

export function getEthicsArchives(params?: { 申请编号?: number; 会议编号?: number; 档案类型?: string; 状态?: string }) {
  return get<EthicsArchiveRecord[]>('/Ethics/查询伦理电子档案', params)
}

export function archiveEthicsMaterial(data: { 申请编号?: number; 会议编号?: number; 决定编号?: number; 档案类型: string; 文件名称?: string; 文件路径: string; 文件校验值?: string; 备注?: string }) {
  return post<{
    success: boolean
    编号: number
    状态: string
    message: string
    实际校验状态?: string
    校验时间?: string
    文件大小?: number
    内容类型?: string
    校验错误?: string
  }>('/Ethics/归档伦理电子材料', data)
}
