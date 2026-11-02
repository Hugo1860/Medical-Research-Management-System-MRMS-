import { beforeEach, describe, expect, it, vi } from 'vitest'
import { matchAllPaperPartitions } from '@/api/journal-partition'
import { post } from '@/utils/request'

vi.mock('@/utils/request', () => ({ get: vi.fn(), post: vi.fn() }))

describe('journal partition batch request orchestration', () => {
  beforeEach(() => vi.resetAllMocks())

  it('consumes all batches using the selected year and a fixed snapshot bound', async () => {
    vi.mocked(post)
      .mockResolvedValueOnce({ 处理论文数: 100, 产生匹配数: 90, 下一起始论文编号: 110, 截止论文编号: 200, 是否还有论文: true })
      .mockResolvedValueOnce({ 处理论文数: 70, 产生匹配数: 20, 下一起始论文编号: 200, 截止论文编号: 200, 是否还有论文: false })
    const progress = vi.fn()
    await expect(matchAllPaperPartitions(2026, progress)).resolves.toEqual({ processed: 170, matched: 110 })
    expect(post).toHaveBeenNthCalledWith(1, '/JournalPartition/批量匹配论文分区', { 年度: 2026, 起始论文编号: 0, 截止论文编号: undefined })
    expect(post).toHaveBeenNthCalledWith(2, '/JournalPartition/批量匹配论文分区', { 年度: 2026, 起始论文编号: 110, 截止论文编号: 200 })
    expect(progress).toHaveBeenLastCalledWith({ processed: 170, matched: 110 })
  })

  it('allows all-years matching and an empty result without issuing extra requests', async () => {
    vi.mocked(post).mockResolvedValue({ 处理论文数: 0, 产生匹配数: 0, 下一起始论文编号: 0, 截止论文编号: 0, 是否还有论文: false })
    await expect(matchAllPaperPartitions()).resolves.toEqual({ processed: 0, matched: 0 })
    expect(post).toHaveBeenCalledTimes(1)
  })

  it('stops a non-advancing cursor instead of repeatedly submitting the same batch', async () => {
    vi.mocked(post).mockResolvedValue({ 处理论文数: 0, 产生匹配数: 0, 下一起始论文编号: 0, 截止论文编号: 10, 是否还有论文: true })
    await expect(matchAllPaperPartitions(2026)).rejects.toThrow('论文批次返回异常')
    expect(post).toHaveBeenCalledTimes(1)
  })

  it('propagates a failed continuation while retaining progress from completed batches', async () => {
    vi.mocked(post)
      .mockResolvedValueOnce({ 处理论文数: 100, 产生匹配数: 90, 下一起始论文编号: 100, 截止论文编号: 200, 是否还有论文: true })
      .mockRejectedValueOnce(new Error('service unavailable'))
    const progress = vi.fn()
    await expect(matchAllPaperPartitions(2026, progress)).rejects.toThrow('service unavailable')
    expect(progress).toHaveBeenCalledTimes(1)
    expect(progress).toHaveBeenLastCalledWith({ processed: 100, matched: 90 })
    expect(post).toHaveBeenCalledTimes(2)
  })
})
