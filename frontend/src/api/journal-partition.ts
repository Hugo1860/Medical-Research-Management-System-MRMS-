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

export function batchMatchPaperPartitions(year?: number) {
  return post('/JournalPartition/批量匹配论文分区', { 年度: year })
}
