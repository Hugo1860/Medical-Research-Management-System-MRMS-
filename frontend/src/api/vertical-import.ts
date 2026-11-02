import { get, post } from '@/utils/request'
import { validateProposalUploadFiles } from '@/utils/proposal-upload'

export interface ProposalFields {
  项目名称?: string
  负责人?: string
  负责人编号?: number
  依托单位?: string
  批准号?: string
  年度?: number
  资助经费万元?: number | null
  批准日期?: string | null
  开始日期?: string | null
  结束日期?: string | null
  分类?: number
  学科门类?: string
  学科类型?: string
  研究类型?: string
  项目类型?: string
  项目级别?: string
  项目来源单位?: string
}

export function getImportBatches() {
  return get<any[]>('/VerticalProjectImport/查询导入批次')
}
export function getImportDetails(batchId: number) {
  return get<any[]>('/VerticalProjectImport/查询导入明细', { 批次编号: batchId })
}
export function createImportBatch(payload: { 批次名称: string; 来源类型?: string }) {
  return post<{ 编号: number }>('/VerticalProjectImport/创建导入批次', payload)
}
export function confirmBatchImport(batchId: number) {
  return post('/VerticalProjectImport/确认批量导入', { 批次编号: batchId })
}
export async function uploadProposalFiles(batchId: number, files: File[]) {
  const error = validateProposalUploadFiles(files)
  if (error) throw new Error(error)
  const data = new FormData()
  for (const file of files) data.append('files', file)
  return post<{ 新增文件数: number; 重复文件数: number }>(
    `/VerticalProjectImport/上传计划书?批次编号=${batchId}`, data,
    { headers: { 'Content-Type': 'multipart/form-data' }, timeout: 120000 }
  )
}
export async function restoreProposalOriginal(detailId: number, file: File) {
  const error = validateProposalUploadFiles([file], 'restore')
  if (error) throw new Error(error)
  const data = new FormData()
  data.append('file', file)
  return post(`/VerticalProjectImport/补传原件?明细编号=${detailId}`, data,
    { headers: { 'Content-Type': 'multipart/form-data' }, timeout: 120000 })
}
export function reviewProposalFields(batchId: number, detailId: number, fields: ProposalFields, checked: boolean) {
  return post<{ 通过: boolean; 校验结果: Record<string, string> }>('/VerticalProjectImport/保存人工复核', {
    批次编号: batchId, 明细编号: detailId, 解析字段: fields, 已核对原件: checked
  })
}
export function ignoreProposal(batchId: number, detailId: number) {
  return post('/VerticalProjectImport/忽略导入明细', { 批次编号: batchId, 明细编号: detailId })
}
export function searchProposalOwners(keyword: string) {
  return get<Array<{ 编号: number; 姓名: string; 工号: string; 部门编号: number }>>('/VerticalProjectImport/查询导入负责人', { 关键词: keyword })
}
export function downloadProposal(detailId: number) {
  return get<Blob>('/VerticalProjectImport/下载计划书', { 明细编号: detailId }, { responseType: 'blob' })
}
