import { get, post } from '@/utils/request'

export interface MobilePushConfig {
  编号?: number
  通道类型: '企业微信应用' | '企业微信机器人' | '钉钉工作通知' | '钉钉机器人'
  通道名称: string
  Webhook地址?: string
  应用ID?: string
  应用密钥?: string
  基础配置JSON?: string
  是否启用: boolean
  配置状态?: '待配置' | '待验证' | '已验证' | '验证失败' | '已停用'
  最后测试时间?: string
  最后测试状态?: string
  最后测试提示?: string
  凭据配置完整?: boolean
  允许发送?: boolean
  建立时间?: string
  更新时间?: string
}

export interface MobilePushLogItem {
  编号: number
  通道编号: number
  通道名称: string
  通道类型: string
  消息类型: string
  业务模块: string
  业务编号?: string
  接收人编号: number
  接收人姓名: string
  接收人账号?: string
  消息标题: string
  消息摘要?: string
  卡片跳转链接?: string
  推送状态: '待发送' | '已发送' | '发送失败' | '未配置' | '通道未验证'
  推送时间: string
}

export interface MobilePushSendRequest {
  消息类型?: string
  业务模块?: string
  业务编号?: string
  接收人编号?: number
  消息标题: string
  消息摘要?: string
  卡片链接?: string
}

// 1. 获取移动协同推送配置通道列表
export function getMobilePushConfigs(onlyEnabled?: boolean) {
  return get<MobilePushConfig[]>('/Evaluation/获取移动协同推送配置', { 仅启用: onlyEnabled })
}

// 2. 保存移动协同配置通道
export function saveMobilePushConfig(data: Partial<MobilePushConfig>) {
  return post<{ 编号: number }>('/Evaluation/保存移动协同推送配置', data)
}

// 3. 测试通道连通性
export function testMobilePushChannel(通道编号: number) {
  return post<{ 成功: boolean; 提示消息: string; 真实握手已执行: boolean }>(`/Evaluation/测试移动协同推送通道?通道编号=${通道编号}`)
}

// 4. 发送移动待办推送
export function sendMobilePushNotification(data: MobilePushSendRequest) {
  return post<{ 日志编号: number; 通道名称: string; 推送状态: string; 提示消息: string; 是否已真实发送: boolean }>('/Evaluation/发送移动协同待办通知', data)
}

// 5. 获取推送流水日志
export function getMobilePushLogs(params?: {
  接收人编号?: number
  业务模块?: string
  limit?: number
}) {
  return get<MobilePushLogItem[]>('/Evaluation/获取移动待办推送日志', params)
}
