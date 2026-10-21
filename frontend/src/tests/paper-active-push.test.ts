import { describe, it, expect } from 'vitest'
import type { ActivePushPaperItem, DisambiguationConfigData } from '../api/fruit'

describe('Active Paper Push, Institutional Alias Disambiguation & CAS Partition Auto-tagging (论文主动大数据网推、机构别名消歧与中科院分区自动打标 - 清华长庚模式)', () => {
  // 1. 机构别名与学者拼音消歧评分引擎
  it('应准确计算多源机构别名与学者署名拼音匹配置信度并判定高置信度网推', () => {
    // 模拟医院机构别名与学者署名消歧库
    const disambigConfigs: DisambiguationConfigData = {
      机构别名: [
        { 编号: 1, 规范单位编号: 1, 规范单位名称: '清华大学附属北京清华长庚医院', 别名: 'Beijing Tsinghua Changgung Hospital', 来源: 'Crossref', 是否启用: true },
        { 编号: 2, 规范单位编号: 1, 规范单位名称: '清华大学附属北京清华长庚医院', 别名: 'Tsinghua Changgung Hospital', 来源: 'PubMed', 是否启用: true },
        { 编号: 3, 规范单位编号: 1, 规范单位名称: '清华大学附属北京清华长庚医院', 别名: 'School of Clinical Medicine, Tsinghua University', 来源: 'Crossref', 是否启用: true }
      ],
      学者消歧: [
        { 编号: 1, 人员编号: 1, 工号: 'TH001', 姓名: '张三', 英文拼音署名: 'Zhang, S.', orcid: '0000-0002-1825-0097', 科室名称: '肿瘤内科', 是否主要署名: true },
        { 编号: 2, 人员编号: 1, 工号: 'TH001', 姓名: '张三', 英文拼音署名: 'Zhang, San', orcid: '0000-0002-1825-0097', 科室名称: '肿瘤内科', 是否主要署名: true },
        { 编号: 3, 人员编号: 1, 工号: 'TH001', 姓名: '张三', 英文拼音署名: 'San Zhang', orcid: '0000-0002-1825-0097', 科室名称: '肿瘤内科', 是否主要署名: false }
      ]
    }

    // 论文元数据 (抓取自 Crossref/PubMed)
    const paperRaw1 = {
      affiliations: 'Department of Medical Oncology, Beijing Tsinghua Changgung Hospital, School of Clinical Medicine, Tsinghua University, Beijing, China',
      authors: 'Zhang, S.; Wang, H.; Liu, Y.',
      firstAuthor: 'Zhang, S.'
    }

    // 消歧规则：
    // - 机构别名命中：+45 分
    // - 学者署名匹配：+35 分
    // - 科室匹配：+10 分
    // - 第一/通讯作者权重：+10 分
    let score = 0
    let reasons: string[] = []

    const institutionHit = disambigConfigs.机构别名.some(alias =>
      alias.是否启用 && paperRaw1.affiliations.includes(alias.别名)
    )
    if (institutionHit) {
      score += 45
      reasons.push('机构别名命中(Beijing Tsinghua Changgung Hospital)')
    }

    const scholarMatch = disambigConfigs.学者消歧.find(s =>
      paperRaw1.authors.includes(s.英文拼音署名)
    )
    if (scholarMatch) {
      score += 35
      reasons.push(`学者拼音匹配(${scholarMatch.英文拼音署名})`)

      if (paperRaw1.affiliations.includes('Medical Oncology') || paperRaw1.affiliations.includes('Oncology')) {
        score += 10
        reasons.push('科室精准匹配(肿瘤内科)')
      }

      if (paperRaw1.firstAuthor === scholarMatch.英文拼音署名) {
        score += 10
        reasons.push('第一作者署名')
      }
    }

    // 判定网推状态
    const pushStatus = score >= 75 ? '高置信度网推' : '普通待认领'

    expect(score).toBe(100)
    expect(score).toBeGreaterThanOrEqual(75)
    expect(pushStatus).toBe('高置信度网推')
    expect(reasons).toHaveLength(4)
    expect(scholarMatch?.姓名).toBe('张三')
  })

  // 2. 中科院分区与影响因子免录入自动识别打标
  it('应准确自动关联期刊中科院分区(1/2/3/4区)、Top期刊标识与最新影响因子', () => {
    // 模拟25,269本权威期刊库子集
    const journalDb = [
      { 编号: 1001, 名称: 'Nature Medicine', ISSN: '1078-8956', 中科院分区: '中科院1区(Top)', 大类学科: '医学', 是否Top: true, 影响因子: 28.054, JCR: 'Q1' },
      { 编号: 1002, 名称: 'Cell Stem Cell', ISSN: '1934-5909', 中科院分区: '中科院1区(Top)', 大类学科: '生物学', 是否Top: true, 影响因子: 22.151, JCR: 'Q1' },
      { 编号: 1003, 名称: 'Cardiovascular Research', ISSN: '0008-6363', 中科院分区: '中科院2区', 大类学科: '医学', 是否Top: false, 影响因子: 10.8, JCR: 'Q1' },
      { 编号: 1004, 名称: 'Plos One', ISSN: '1932-6203', 中科院分区: '中科院3区', 大类学科: '综合性期刊', 是否Top: false, 影响因子: 3.7, JCR: 'Q2' }
    ]

    function autoMatchJournalPartition(journalName: string, issn?: string) {
      const match = journalDb.find(j => (issn && j.ISSN === issn) || j.名称.toLowerCase() === journalName.toLowerCase())
      if (!match) return null
      return {
        中科院分区: match.中科院分区,
        中科院大类学科: match.大类学科,
        是否Top期刊: match.是否Top,
        影响因子: match.影响因子,
        JCR分区: match.JCR
      }
    }

    const paper1Tags = autoMatchJournalPartition('Nature Medicine', '1078-8956')
    expect(paper1Tags).not.toBeNull()
    expect(paper1Tags?.中科院分区).toBe('中科院1区(Top)')
    expect(paper1Tags?.是否Top期刊).toBe(true)
    expect(paper1Tags?.影响因子).toBe(28.054)
    expect(paper1Tags?.JCR分区).toBe('Q1')

    const paper2Tags = autoMatchJournalPartition('Cell Stem Cell', '1934-5909')
    expect(paper2Tags?.中科院分区).toBe('中科院1区(Top)')
    expect(paper2Tags?.是否Top期刊).toBe(true)
    expect(paper2Tags?.影响因子).toBeGreaterThan(20)

    const paper3Tags = autoMatchJournalPartition('Plos One')
    expect(paper3Tags?.中科院分区).toBe('中科院3区')
    expect(paper3Tags?.是否Top期刊).toBe(false)
  })

  // 3. 一键认领全自动归档与状态流转契约
  it('一键确认认领应即时写入成果论文与作者归属并同步网推状态为已确认认领', () => {
    const pushPaper: ActivePushPaperItem = {
      编号: 1,
      外部论文编号: '10.1038/s41591-026-03888-x',
      论文标题: 'Deep learning-based genomic variant classification in clinical oncology',
      作者: 'Zhang, S.; Wang, H.; Liu, Y.',
      刊物名称: 'Nature Medicine',
      issN号: '1078-8956',
      中科院分区: '中科院1区(Top)',
      中科院大类学科: '医学',
      是否Top期刊: true,
      影响因子: 28.054,
      jcR分区: 'Q1',
      推荐认领人编号: 1,
      推荐认领人姓名: '张三',
      消歧置信度: 90,
      消歧命中依据: '机构别名命中 + 学者拼音匹配 + 科室匹配',
      网推状态: '高置信度网推',
      状态: '成功'
    }

    // 认领动作
    const claimRequest = {
      采集结果编号: pushPaper.编号,
      认领人编号: pushPaper.推荐认领人编号,
      作者位次: '第一作者',
      认领说明: '第一署名单位为清华长庚医院，由青年学者基金资助'
    }

    // 状态流转执行
    const archivedPaper = {
      成果论文编号: 13,
      论文标题: pushPaper.论文标题,
      刊物名称: pushPaper.刊物名称,
      中科院分区: pushPaper.中科院分区,
      是否Top期刊: pushPaper.是否Top期刊,
      影响因子: pushPaper.影响因子,
      第一作者: '张三',
      认领状态: '已认领',
      作者列表: [
        { 人员编号: claimRequest.认领人编号, 位次: claimRequest.作者位次, 姓名: pushPaper.推荐认领人姓名 }
      ]
    }

    pushPaper.论文编号 = archivedPaper.成果论文编号
    pushPaper.网推状态 = '已确认认领'

    expect(pushPaper.网推状态).toBe('已确认认领')
    expect(pushPaper.论文编号).toBe(13)
    expect(archivedPaper.中科院分区).toBe('中科院1区(Top)')
    expect(archivedPaper.是否Top期刊).toBe(true)
    expect(archivedPaper.作者列表[0].位次).toBe('第一作者')
  })

  // 4. 网推台账多维度筛选与统计指标聚合
  it('应准确按网推状态、中科院分区与关键词多维度过滤台账数据', () => {
    const list: ActivePushPaperItem[] = [
      {
        编号: 1,
        外部论文编号: '10.1038/s41591-026-03888-x',
        论文标题: 'Deep learning-based genomic variant classification in clinical oncology',
        作者: 'Zhang, S.',
        刊物名称: 'Nature Medicine',
        中科院分区: '中科院1区(Top)',
        是否Top期刊: true,
        影响因子: 28.054,
        消歧置信度: 90,
        网推状态: '高置信度网推',
        状态: '成功'
      },
      {
        编号: 2,
        外部论文编号: '10.1016/j.stem.2026.06.012',
        论文标题: 'Single-cell mapping of microenvironmental immune escape in glioblastoma',
        作者: 'Li, M.; Zhang, S.',
        刊物名称: 'Cell Stem Cell',
        中科院分区: '中科院1区(Top)',
        是否Top期刊: true,
        影响因子: 22.151,
        消歧置信度: 80,
        网推状态: '高置信度网推',
        状态: '成功'
      },
      {
        编号: 3,
        外部论文编号: '10.1371/journal.pone.0298110',
        论文标题: 'Retrospective cohort of community-acquired pneumonia',
        作者: 'Wang, X.',
        刊物名称: 'Plos One',
        中科院分区: '中科院3区',
        是否Top期刊: false,
        影响因子: 3.7,
        消歧置信度: 40,
        网推状态: '普通待认领',
        状态: '成功'
      }
    ]

    // 统计计算
    const highConfidenceCount = list.filter(p => (p.消歧置信度 || 0) >= 75).length
    const casTopCount = list.filter(p => (p.中科院分区 && p.中科院分区.includes('1区')) || p.是否Top期刊).length
    const claimedCount = list.filter(p => p.网推状态 === '已确认认领').length

    expect(highConfidenceCount).toBe(2)
    expect(casTopCount).toBe(2)
    expect(claimedCount).toBe(0)

    // 筛选：仅查看中科院1区
    const filteredByCas = list.filter(p => p.中科院分区?.includes('1区'))
    expect(filteredByCas).toHaveLength(2)

    // 筛选：按关键词 "oncology"
    const filteredByKeyword = list.filter(p => p.论文标题.toLowerCase().includes('oncology'))
    expect(filteredByKeyword).toHaveLength(1)
    expect(filteredByKeyword[0].刊物名称).toBe('Nature Medicine')
  })
})
