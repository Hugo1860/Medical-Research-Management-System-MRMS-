import { describe, it, expect } from 'vitest'

describe('Achievement Claim & Performance Weight Engine (成果论文/专利认领与绩效分配核心引擎)', () => {
  // 1. 学术论文综合影响力基准分计算引擎 (CAS分区 + Top期刊 + IF影响因子乘数)
  it('论文基准分计算引擎应精准遵循中科院分区基础分、Top期刊溢价与IF影响因子乘数', () => {
    function computePaperBaseScore(paper: {
      中科院分区?: string
      是否Top期刊?: boolean
      影响因子?: number | string
    }) {
      const part = paper.中科院分区 || ''
      let base = 5.0
      if (part.includes('1区') && paper.是否Top期刊) {
        base = 120.0
      } else if (part.includes('1区')) {
        base = 100.0
      } else if (part.includes('2区')) {
        base = 60.0
      } else if (part.includes('3区')) {
        base = 30.0
      } else if (part.includes('4区')) {
        base = 10.0
      }

      if (paper.是否Top期刊 && !part.includes('1区')) {
        base += 20.0
      }

      const ifFactor = Number(paper.影响因子 || 0)
      base += Number((ifFactor * 10).toFixed(2))

      return Number(base.toFixed(2))
    }

    // 场景 A: 中科院1区 Top 期刊, IF=15.68
    const scoreA = computePaperBaseScore({
      中科院分区: '中科院1区(Top)',
      是否Top期刊: true,
      影响因子: 15.68
    })
    // 120 + 0 (已包含在120中) + 156.8 = 276.80
    expect(scoreA).toBe(276.8)

    // 场景 B: 中科院2区 Top 期刊, IF=8.5
    const scoreB = computePaperBaseScore({
      中科院分区: '中科院2区',
      是否Top期刊: true,
      影响因子: 8.5
    })
    // 60 + 20 + 85.0 = 165.00
    expect(scoreB).toBe(165.0)

    // 场景 C: 普通期刊无分区, IF=1.2
    const scoreC = computePaperBaseScore({
      中科院分区: '',
      是否Top期刊: false,
      影响因子: 1.2
    })
    // 5 + 0 + 12.0 = 17.00
    expect(scoreC).toBe(17.0)
  })

  // 2. 论文多作者位次拆分权重引擎
  it('认领时应根据作者位次动态分配绩效权重 (一作50%, 通讯30%, 共一10%, 参研10%)', () => {
    function getAuthorShareRatio(rank: string): number {
      if (rank === '第一作者') return 0.50
      if (rank === '通讯作者') return 0.30
      if (rank === '共同第一作者') return 0.10
      if (rank === '共同通讯作者') return 0.10
      return 0.10
    }

    const baseScore = 200.0

    const firstAuthorScore = Number((baseScore * getAuthorShareRatio('第一作者')).toFixed(2))
    const corrAuthorScore = Number((baseScore * getAuthorShareRatio('通讯作者')).toFixed(2))
    const coFirstScore = Number((baseScore * getAuthorShareRatio('共同第一作者')).toFixed(2))
    const coAuthorScore = Number((baseScore * getAuthorShareRatio('主要参与作者')).toFixed(2))

    expect(firstAuthorScore).toBe(100.0)
    expect(corrAuthorScore).toBe(60.0)
    expect(coFirstScore).toBe(20.0)
    expect(coAuthorScore).toBe(20.0)
  })

  // 3. 专利知识产权绩效分值与职务发明位次折算
  it('专利绩效引擎应区分发明专利与实用新型/外观，并支持第一发明人与团队权重分摊', () => {
    function calculatePatentAuthorScore(patentType: string, isFirstInventor: boolean, customRatio?: number) {
      const isInvention = patentType.includes('发明')
      const base = isInvention ? 30.00 : 10.00
      let ratio = customRatio ?? (isFirstInventor ? 0.60 : 0.20)
      return Number((base * ratio).toFixed(2))
    }

    // 发明专利第一发明人 (60% 权重)
    const invFirstScore = calculatePatentAuthorScore('发明专利', true)
    expect(invFirstScore).toBe(18.0)

    // 发明专利第二发明人 (20% 权重)
    const invSecondScore = calculatePatentAuthorScore('发明专利', false)
    expect(invSecondScore).toBe(6.0)

    // 实用新型第一发明人
    const utilFirstScore = calculatePatentAuthorScore('实用新型专利', true)
    expect(utilFirstScore).toBe(6.0)

    // 自定义工作量占比 40%
    const customScore = calculatePatentAuthorScore('发明专利', false, 0.40)
    expect(customScore).toBe(12.0)
  })

  // 4. 大数据网推成果置信度识别与认领契约校验
  it('成果消歧引擎应精准辨识高置信度待认领成果并验证认领提交参数', () => {
    const pushList = [
      { 编号: 101, 论文标题: 'Clinical Trial A', 消歧置信度: 88, 网推状态: '高置信度网推' },
      { 编号: 102, 论文标题: 'Case Study B', 消歧置信度: 65, 网推状态: '普通待认领' },
      { 编号: 103, 论文标题: 'Biomarker Review C', 消歧置信度: 92, 网推状态: '已确认认领' }
    ]

    const highConfidenceList = pushList.filter(p => (p.消歧置信度 || 0) >= 75)
    expect(highConfidenceList.length).toBe(2)

    function validateClaimPayload(payload: {
      采集结果编号: number
      认领人编号?: number
      作者位次: string
    }) {
      if (!payload.采集结果编号 || payload.采集结果编号 <= 0) return { valid: false, error: '无效采集成果编号' }
      if (!payload.作者位次?.trim()) return { valid: false, error: '必须选择作者位次' }
      const validRanks = ['第一作者', '共同第一作者', '通讯作者', '共同通讯作者', '主要参与作者']
      if (!validRanks.includes(payload.作者位次)) return { valid: false, error: '非法作者位次' }
      return { valid: true }
    }

    expect(validateClaimPayload({ 采集结果编号: 101, 作者位次: '第一作者' }).valid).toBe(true)
    expect(validateClaimPayload({ 采集结果编号: 0, 作者位次: '第一作者' }).valid).toBe(false)
    expect(validateClaimPayload({ 采集结果编号: 101, 作者位次: '客座编辑' }).valid).toBe(false)
  })

  // 5. 学术著作著者署名角色与分工占比模型
  it('学术著作分工模型应支持主编与参编的字数与工作量核定', () => {
    const bookAuthors = [
      { 作者姓名: '张教授', 作者级别: '主编', 工作量占比: 0.50, 编写字数万字: 25 },
      { 作者姓名: '李主治', 作者级别: '副主编', 工作量占比: 0.30, 编写字数万字: 15 },
      { 作者姓名: '王医师', 作者级别: '参编', 工作量占比: 0.20, 编写字数万字: 10 }
    ]

    const totalRatio = bookAuthors.reduce((sum, item) => sum + item.工作量占比, 0)
    const totalWords = bookAuthors.reduce((sum, item) => sum + item.编写字数万字, 0)

    expect(totalRatio).toBeCloseTo(1.0)
    expect(totalWords).toBe(50)
    expect(bookAuthors[0].作者级别).toBe('主编')
  })
})
