import { get, post } from '@/utils/request'

export function getImportBatches() {
  return get<any[]>('/VerticalProjectImport/查询导入批次')
}

export function getImportDetails(batchId: number) {
  return get<any[]>('/VerticalProjectImport/查询导入明细', { 批次编号: batchId })
}

export function createImportBatch(payload: { 批次名称: string; 来源类型?: string }) {
  return post('/VerticalProjectImport/创建导入批次', payload)
}

export function confirmBatchImport(batchId: number) {
  return post('/VerticalProjectImport/确认批量导入', { 批次编号: batchId })
}
