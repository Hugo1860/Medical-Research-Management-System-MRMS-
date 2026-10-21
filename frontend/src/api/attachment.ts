import { get, post, upload } from '@/utils/request'

export interface RequestUploadParams {
  businessType: string
  businessKey?: string
  fileName: string
  fileSize: number
  sha256?: string
  contentType?: string
}

export interface RequestUploadResult {
  fastUpload: boolean
  attachmentId: number
  uploadUrl?: string
  method?: string
  headers?: Record<string, string>
  objectKey: string
  originalFileName: string
}

export interface CompleteUploadParams {
  attachmentId: number
  sha256?: string
}

export interface AttachmentItem {
  编号: number
  业务类型: string
  业务主键?: string
  原始文件名: string
  扩展名: string
  MIME类型: string
  文件大小: number
  SHA256: string
  存储驱动: string
  建立时间: string
  上传人姓名?: string
}

export interface StorageStatsResult {
  driver: string
  defaultBucket: string
  summary: {
    附件总数: number
    总存储字节: number
    有效附件数: number
    未绑定草稿数: number
  }
  recentAudits: Array<{
    编号: number
    附件编号: number
    原始文件名: string
    访问动作: string
    操作人姓名?: string
    客户端IP: string
    访问时间: string
    鉴权结果: string
    摘要?: string
  }>
}

/**
 * 申请上传预签名凭证或触发秒传
 */
export function requestUpload(data: RequestUploadParams): Promise<RequestUploadResult> {
  return post<RequestUploadResult>('/api/Attachment/RequestUpload', data)
}

/**
 * 确认上传就绪
 */
export function completeUpload(data: CompleteUploadParams): Promise<{ success: boolean; attachmentId: number }> {
  return post('/api/Attachment/CompleteUpload', data)
}

/**
 * 兜底服务端流式代理上传
 */
export function uploadStream(file: File, businessType = '常规附件', businessKey = ''): Promise<any> {
  return upload('/api/Attachment/UploadStream', file, 'file', { businessType, businessKey })
}

/**
 * 获取受控安全下载预签名 URL
 */
export function getDownloadUrl(id: number): Promise<{
  attachmentId: number
  downloadUrl: string
  originalFileName: string
  fileSize: number
  mimeType: string
  driver: string
}> {
  return get(`/api/Attachment/GetDownloadUrl/${id}`)
}

/**
 * 查询指定业务实体关联的有效附件列表
 */
export function listAttachments(businessType: string, businessKey: string): Promise<AttachmentItem[]> {
  return get<AttachmentItem[]>('/api/Attachment/ListAttachments', { businessType, businessKey })
}

/**
 * 将已上传附件与业务实体绑定
 */
export function bindBusiness(attachmentIds: number[], businessType: string, businessKey: string): Promise<any> {
  return post('/api/Attachment/BindBusiness', {
    AttachmentIds: attachmentIds,
    BusinessType: businessType,
    BusinessKey: businessKey
  })
}

/**
 * 获取存储概况与审计日志
 */
export function getStorageStats(): Promise<StorageStatsResult> {
  return get<StorageStatsResult>('/api/Attachment/GetStorageStats')
}

/**
 * 一键将存量历史 upload 附件迁移至 MinIO
 */
export function migrateLegacyUploads(): Promise<{
  message: string
  totalScanned: number
  totalMigrated: number
  totalSkipped: number
  totalErrors: number
}> {
  return post('/api/Attachment/MigrateLegacyUploads')
}
