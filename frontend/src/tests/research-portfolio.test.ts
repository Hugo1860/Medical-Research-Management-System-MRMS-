import { describe, it, expect } from 'vitest'
import type { ResearchPortfolioData } from '../api/office'

describe('Personal Scientific Research Portfolio (个人科研全景与成果资产)', () => {
  it('应准确聚合科研人员个人全周期成果与课题资产', () => {
    const portfolio: ResearchPortfolioData = {
      个人资料: {
        编号: 50,
        工号: 'SCI008',
        姓名: '周教授',
        部门名称: '神经内科',
        技术职称: '主任医师',
        学历: '博士',
        学位: '博士后',
        研究方向: '脑卒中神经保护与脑血管重塑'
      },
      纵向项目总览: [
        { 项目中文名称: '脑缺血再灌注损伤微血管保护机制研究', 批准经费: 550000, 审核进度: '在研' },
        { 项目中文名称: '基于多模态MRI的超早期溶栓靶区预测', 批准经费: 200000, 审核进度: '已结题' }
      ],
      横向项目总览: [
        { 项目名称: '新型脑保护剂多中心II期临床试验开发合作', 合同经费: 800000, 状态: '履行中' }
      ],
      成果总览: [
        { 成果名称: 'Neuroprotective effects of compound X in stroke', 成果类型: '论文' },
        { 成果名称: '缺血性脑血管病微创血管重建手术导管', 成果类型: '专利' },
        { 成果名称: '实用临床神经病学新技术', 成果类型: '著作' },
        { 成果名称: '脑血管病早期综合防治体系研究', 成果类型: '获奖' }
      ]
    }

    expect(portfolio.个人资料.姓名).toBe('周教授')
    expect(portfolio.纵向项目总览).toHaveLength(2)
    expect(portfolio.横向项目总览).toHaveLength(1)
    expect(portfolio.成果总览).toHaveLength(4)

    // 课题经费汇总计算
    const verticalSum = portfolio.纵向项目总览.reduce((acc, curr) => acc + curr.批准经费, 0)
    const horizontalSum = portfolio.横向项目总览.reduce((acc, curr) => acc + curr.合同经费, 0)
    const totalFunding = verticalSum + horizontalSum

    expect(verticalSum).toBe(750000)
    expect(horizontalSum).toBe(800000)
    expect(totalFunding).toBe(1550000)
  })

  it('应准确按成果类型归类个人成果资产', () => {
    const fruits = [
      { 成果名称: '论文 A', 成果类型: '论文' },
      { 成果名称: '论文 B', 成果类型: '论文' },
      { 成果名称: '专著 C', 成果类型: '著作' },
      { 成果名称: '专利 D', 成果类型: '专利' },
      { 成果名称: '获奖 E', 成果类型: '获奖' }
    ]

    const papers = fruits.filter((f) => f.成果类型 === '论文')
    const books = fruits.filter((f) => f.成果类型 === '著作')
    const patents = fruits.filter((f) => f.成果类型 === '专利')
    const awards = fruits.filter((f) => f.成果类型 === '获奖')

    expect(papers).toHaveLength(2)
    expect(books).toHaveLength(1)
    expect(patents).toHaveLength(1)
    expect(awards).toHaveLength(1)
  })

  it('应兼容后端视图实际返回的字段属性（合同名称、合同金额、计划起止日期与复合成果类型）', () => {
    const rawData = {
      纵向项目总览: [
        {
          项目中文名称: '新型生物医用材料研发',
          批准经费: 300000,
          计划开始日期: '2026-01-01',
          计划完成日期: '2028-12-31',
          审核进度: '未完成-待分配专家'
        }
      ],
      横向项目总览: [
        {
          合同名称: '医用高分子材料技术转让合同',
          合同金额: 500000,
          签订时间: '2026-03-15',
          审核进度: '履约中'
        }
      ],
      成果总览: [
        { 成果名称: '心血管支架新工艺', 成果类型: '获奖成果' },
        { 成果名称: '现代介入放射学', 成果类型: '著作成果' }
      ]
    }

    // 经费统计兼容合同金额与合同经费
    let sum = 0
    for (const v of rawData.纵向项目总览) {
      sum += Number(v.批准经费 || 0)
    }
    for (const h of rawData.横向项目总览) {
      sum += Number((h as any).合同金额 || (h as any).合同经费 || 0)
    }
    expect(sum).toBe(800000)

    // 成果类型兼容测试
    function getTypeTagType(type?: string) {
      if (!type) return 'info'
      if (type.includes('论文')) return 'primary'
      if (type.includes('著作')) return 'success'
      if (type.includes('专利')) return 'warning'
      if (type.includes('获奖')) return 'danger'
      return 'info'
    }

    expect(getTypeTagType('获奖成果')).toBe('danger')
    expect(getTypeTagType('著作成果')).toBe('success')
    expect(getTypeTagType('期刊论文')).toBe('primary')
    expect(getTypeTagType('发明专利')).toBe('warning')
  })

  it('应准确解析五维学术能力量化指标与综合评级', () => {
    const portfolio: ResearchPortfolioData = {
      个人资料: { 编号: 101, 姓名: '李首席' },
      纵向项目总览: [],
      横向项目总览: [],
      成果总览: [],
      能力画像量化: {
        人员编号: 101,
        课题主持得分: 85,
        经费规模得分: 90,
        论文能力得分: 80,
        成果转化得分: 75,
        团队指导得分: 88,
        综合科研指数: 84.2,
        评级称号: '杰出科研骨干 (A级)'
      }
    }

    expect(portfolio.能力画像量化).toBeDefined()
    expect(portfolio.能力画像量化?.综合科研指数).toBe(84.2)
    expect(portfolio.能力画像量化?.评级称号).toContain('A级')
    expect(portfolio.能力画像量化?.课题主持得分).toBe(85)
    expect(portfolio.能力画像量化?.经费规模得分).toBe(90)
  })

  it('应准确解析历年科研学术产出走势与多维团队带教数据', () => {
    const portfolio: ResearchPortfolioData = {
      个人资料: { 编号: 102, 姓名: '王教授' },
      纵向项目总览: [],
      横向项目总览: [],
      成果总览: [],
      历年产出趋势: [
        { 年度: '2024', 纵向课题数: 1, 纵向经费: 30, 横向合同数: 1, 横向经费: 50, 论文数: 3, 专利数: 1, 著作数: 0, 获奖数: 0 },
        { 年度: '2025', 纵向课题数: 2, 纵向经费: 60, 横向合同数: 2, 横向经费: 80, 论文数: 5, 专利数: 2, 著作数: 1, 获奖数: 1 }
      ],
      团队: [{ 团队名称: '脑血管重大疾病创新团队', 成员角色: '团队带头人' }],
      导师关系: [{ 导师姓名: '王教授', 学员姓名: '张博士', 关系类型: '博导' }]
    }

    expect(portfolio.历年产出趋势).toHaveLength(2)
    expect(portfolio.历年产出趋势?.[1].年度).toBe('2025')
    expect(portfolio.历年产出趋势?.[1].论文数).toBe(5)
    expect(portfolio.团队).toHaveLength(1)
    expect(portfolio.团队?.[0].成员角色).toBe('团队带头人')
    expect(portfolio.导师关系).toHaveLength(1)
    expect(portfolio.导师关系?.[0].学员姓名).toBe('张博士')
  })
})
