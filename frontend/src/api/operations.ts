import { get, post } from '@/utils/request'

export interface ExternalSystemItem {
  编号: number
  系统代码: string
  系统名称: string
  系统类型: string
  基础地址?: string
  认证方式?: string
  认证引用?: string
  是否启用: boolean
  超时秒数: number
  最大重试次数: number
}

export interface SyncTaskItem {
  编号: number
  外部系统编号: number
  系统代码?: string
  系统名称?: string
  业务类型: string
  方向: '推送' | '拉取'
  业务主键?: string
  幂等键: string
  状态: '待处理' | '处理中' | '成功' | '失败' | '已取消'
  尝试次数: number
  最大重试次数: number
  下次重试时间?: string
  请求ID?: string
  外部响应摘要?: string
  最后错误?: string
  创建时间: string
  更新时间: string
  完成时间?: string
}

// 获取已登记外部系统列表（涵盖 HR、财务、采购、伦理、合同、人才库、生物样本库、论文采集）
export function getExternalSystems(params?: { 系统类型?: string; 仅启用?: boolean }) {
  return get<ExternalSystemItem[]>('/Operations/获取外部系统', params)
}

// 检查八类核心外部系统配置完备度
export function checkExternalConfigs() {
  return get<any>('/Operations/获取外部系统配置检查')
}

// 测试外部系统端点连通性
export function testSystemConnection(data: Partial<ExternalSystemItem>) {
  return post<any>('/Operations/测试外部系统连接', data)
}

// 查询外部同步任务队列
export function getSyncTasks(params?: { 外部系统编号?: number; 状态?: string; 数量?: number }) {
  return get<SyncTaskItem[]>('/Operations/查询外部同步任务', params)
}

// 重试失败任务
export function retrySyncTask(taskId: number) {
  return post('/Operations/重试外部同步任务', { 编号: taskId })
}

// 取消同步任务
export function cancelSyncTask(taskId: number, reason: string) {
  return post('/Operations/取消外部同步任务', { 编号: taskId, 说明: reason })
}

// 获取接口失败报警
export function getFailAlerts() {
  return get<any[]>('/Operations/获取接口失败告警')
}

// 获取外部同步队列中的终态失败任务（只读告警，不会触发重试）
export function getExternalSyncFailAlerts(params?: { 数量?: number }) {
  return get<any[]>('/Operations/获取外部同步失败告警', params)
}
