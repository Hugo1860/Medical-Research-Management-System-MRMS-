import { get, post } from '@/utils/request'

export function getJournalPartitions(source?: string, year?: number, keyword?: string) {
  return get<any[]>('/JournalPartition/查询期刊分区', { 来源: source, 年度: year, 关键词: keyword })
}

export function saveJournalPartition(payload: any) {
  return post('/JournalPartition/保存期刊分区条目', payload)
}

export function matchJournalPartition(issn?: string, journalName?: string, year?: number) {
  return get<any[]>('/JournalPartition/智能匹配期刊分区', { issn, 期刊名称: journalName, 年度: year })
}

export interface PaperPartitionBatchResult {
  处理论文数: number
  产生匹配数: number
  下一起始论文编号: number
  截止论文编号: number
  是否还有论文: boolean
}

export interface PaperPartitionMatchProgress {
  processed: number
  matched: number
}

export function batchMatchPaperPartitions(year?: number, afterPaperId = 0, lastPaperId?: number) {
  return post<PaperPartitionBatchResult>('/JournalPartition/批量匹配论文分区', {
    年度: year,
    起始论文编号: afterPaperId,
    截止论文编号: lastPaperId
  })
}

export async function matchAllPaperPartitions(
  year?: number,
  onProgress?: (progress: PaperPartitionMatchProgress) => void
): Promise<PaperPartitionMatchProgress> {
  const progress = { processed: 0, matched: 0 }
  let cursor = 0
  let lastPaperId: number | undefined
  do {
    const result = await batchMatchPaperPartitions(year, cursor, lastPaperId)
    if (!result || !Number.isInteger(result.处理论文数) || !Number.isInteger(result.产生匹配数) ||
        !Number.isInteger(result.截止论文编号) || !Number.isInteger(result.下一起始论文编号) ||
        typeof result.是否还有论文 !== 'boolean' || result.处理论文数 < 0 || result.产生匹配数 < 0 ||
        result.下一起始论文编号 < cursor || result.下一起始论文编号 > result.截止论文编号 ||
        (lastPaperId !== undefined && result.截止论文编号 !== lastPaperId) ||
        (result.是否还有论文 && result.下一起始论文编号 <= cursor)) {
      throw new Error('论文批次返回异常，已停止匹配，请刷新后重试')
    }
    lastPaperId = result.截止论文编号
    cursor = result.下一起始论文编号
    progress.processed += result.处理论文数
    progress.matched += result.产生匹配数
    onProgress?.({ ...progress })
    if (!result.是否还有论文) return progress
  } while (true)
}
