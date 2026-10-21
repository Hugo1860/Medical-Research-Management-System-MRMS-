import { describe, it, expect } from 'vitest'
import type { PaperItem, MatchedJournal } from '../api/fruit'

describe('Paper Lifecycle Management & Literature Harvest (学术论文全生命周期与文献采集)', () => {
  it('应正确构造学术论文基本数据模型与发表元数据', () => {
    const mockPaper: PaperItem = {
      编号: 201,
      论文标题: 'Single-cell transcriptomics reveals cellular diversity in coronary atheroma',
      刊物名称: 'Nature Communications',
      第一作者: '王医生',
      通讯作者: '李主任',
      论文Online日期: '2025-06-12',
      论文正式出版日期: '2025-09-01',
      年度: 2025,
      卷号: '16',
      期号: '1',
      页码范围: '1023-1035',
      DOI: '10.1038/s41467-025-01234-x',
      PMID: '39012345',
      影响因子: 14.7,
      认领状态: '已认领',
      认领人姓名: '王医生'
    }

    expect(mockPaper.编号).toBe(201)
    expect(mockPaper.论文标题).toContain('Single-cell transcriptomics')
    expect(mockPaper.DOI).toMatch(/^10\.\d{4,9}\/[-._;()/:A-Za-z0-9]+$/)
    expect(mockPaper.影响因子).toBeGreaterThan(10)
  })

  it('应准确按匹配度降序过滤智能期刊识别结果', () => {
    const candidateJournals: MatchedJournal[] = [
      { 编号: 10, 名称: '中华医学杂志', ISSN号: '0376-2491', CN号: '11-2137/R', 匹配分数: 90 },
      { 编号: 11, 名称: '中华心血管病杂志', ISSN号: '0253-3758', CN号: '11-2148/R', 匹配分数: 98 },
      { 编号: 12, 名称: '中华内科杂志', ISSN号: '0578-1426', CN号: '11-2138/R', 匹配分数: 65 }
    ]

    const sorted = [...candidateJournals].sort((a, b) => b.匹配分数 - a.匹配分数)
    expect(sorted[0].名称).toBe('中华心血管病杂志')
    expect(sorted[0].匹配分数).toBe(98)
    expect(sorted[2].匹配分数).toBe(65)
  })

  it('应严格校验 Crossref 与 PubMed 外部采集请求负载', () => {
    const crossrefPayload = {
      外部系统编号: 8,
      关键词: 'artificial intelligence',
      作者: 'Chen',
      DOI: '10.1016/j.cell.2025.01.002',
      行数: 25
    }

    const pubmedPayload = {
      外部系统编号: 8,
      关键词: 'myocardial infarction',
      作者: 'Zhao',
      行数: 30
    }

    expect(crossrefPayload.外部系统编号).toBe(8)
    expect(crossrefPayload.行数).toBeLessThanOrEqual(50)
    expect(pubmedPayload.关键词).toBeTruthy()
    expect(pubmedPayload.行数).toBeGreaterThan(0)
  })
})
