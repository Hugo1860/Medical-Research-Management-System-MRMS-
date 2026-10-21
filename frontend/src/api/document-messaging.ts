import request from '@/utils/request'

export interface ResearchDocument {
  编号?: number
  文档编码: string
  文档名称: string
  文档分类?: string
  文档说明?: string
  授权模式?: string
  是否启用?: boolean
  当前版本编号?: number
  版本号?: string
  文件名称?: string
  文件大小?: number
  SHA256?: string
  版本状态?: string
  下载次数?: number
}

export interface MessageChannel {
  编号?: number
  通道类型: string
  通道名称: string
  服务地址?: string
  凭据引用?: string
  配置JSON?: string
  通道状态?: string
  是否启用?: boolean
  最后测试状态?: string
  最后测试提示?: string
}

export interface MessageTemplate {
  编号?: number
  模板编码: string
  模板名称: string
  消息类型?: string
  标题模板: string
  正文模板: string
  变量定义?: string
  是否启用?: boolean
}

export function listResearchDocuments(params: { 文档分类?: string; 关键词?: string; 仅启用?: boolean } = {}) {
  return request.get<ResearchDocument[]>('/DocumentMessaging/查询科研文档', { params })
}

export function saveResearchDocument(data: ResearchDocument & { 授权模式?: string }) {
  return request.post<{ 编号: number }>('/DocumentMessaging/保存科研文档', data)
}

export function listDocumentVersions(文档编号: number) {
  return request.get('/DocumentMessaging/查询科研文档版本', { params: { 文档编号 } })
}

export function saveDocumentVersion(data: { 编号?: number; 文档编号: number; 版本号: string; 文件名称: string; 文件路径: string; 文件大小?: number; SHA256?: string; 版本状态?: string; 发布日期?: string }) {
  return request.post<{ 编号: number }>('/DocumentMessaging/保存科研文档版本', data)
}

export function listDocumentAuthorizations(文档编号: number) {
  return request.get('/DocumentMessaging/查询科研文档授权', { params: { 文档编号 } })
}

export function saveDocumentAuthorization(data: { 编号?: number; 文档编号: number; 授权类型: string; 授权对象编号: number; 可下载?: boolean; 有效开始?: string; 有效结束?: string; 是否撤销?: boolean }) {
  return request.post('/DocumentMessaging/保存科研文档授权', data)
}

export function getDocumentDownloadUrl(版本编号: number) {
  return `/DocumentMessaging/下载科研文档?版本编号=${encodeURIComponent(版本编号)}`
}

export function listDocumentDownloadLogs(params: { 文档编号?: number; 数量?: number } = {}) {
  return request.get('/DocumentMessaging/查询科研文档下载日志', { params })
}

export function revokeImportExportLog(data: { 编号: number; 撤销原因: string }) {
  return request.post('/DocumentMessaging/撤销科研导入导出日志', data)
}

export function listMessageChannels() {
  return request.get<MessageChannel[]>('/DocumentMessaging/查询消息通道')
}

export function saveMessageChannel(data: MessageChannel) {
  return request.post<{ 编号: number }>('/DocumentMessaging/保存消息通道', data)
}

export function listMessageTemplates(仅启用?: boolean) {
  return request.get<MessageTemplate[]>('/DocumentMessaging/查询消息模板', { params: { 仅启用 } })
}

export function saveMessageTemplate(data: MessageTemplate) {
  return request.post<{ 编号: number }>('/DocumentMessaging/保存消息模板', data)
}

export function enqueueMessage(data: { 模板编号?: number; 消息类型: string; 业务模块?: string; 业务编号?: string; 接收人编号?: number; 接收地址?: string; 消息标题: string; 消息正文?: string; 请求载荷?: string }) {
  return request.post('/DocumentMessaging/加入消息发送队列', data)
}

export function listMessageQueue(params: { 队列状态?: string; 数量?: number } = {}) {
  return request.get('/DocumentMessaging/查询消息发送队列', { params })
}

export function retryMessage(队列编号: number) {
  return request.post('/DocumentMessaging/重试消息发送', { 队列编号 })
}

export function acknowledgeMessage(data: { 队列编号: number; 外部消息编号: string }) {
  return request.post('/DocumentMessaging/登记消息真实回执', data)
}

export function remindResearchService(data: { 服务申请编号: number; 催办原因?: string }) {
  return request.post('/DocumentMessaging/催办科研服务申请', data)
}

export function listServiceReminders(服务申请编号: number) {
  return request.get('/DocumentMessaging/查询科研服务催办日志', { params: { 服务申请编号 } })
}
