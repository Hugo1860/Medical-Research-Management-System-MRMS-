import { describe, it, expect } from 'vitest'
import type { BiobankSpecimenItem, BiobankLoanItem, BiobankStockStats } from '../api/biobank'

describe('Clinical Biobank Asset Ledger & Ethics-Gated Dispense (临床生物样本库资产管理与伦理前置合规审查 - 清华长庚模式)', () => {
  // 1. 标本资产库存与冷链温区守恒
  it('应准确维护生物样本资产数据模型与冷链温区规格', () => {
    const specimen: BiobankSpecimenItem = {
      编号: 1,
      样本编码: 'BB-2026-HCC-001',
      样本类型: '冰冻组织',
      疾病诊断: '原发性肝细胞癌 (HCC)',
      解剖部位: '肝右叶肿瘤原发灶',
      储存温度: '-80℃',
      储存位置: 'Biobank-低温区-80℃冰箱#01-A架-02盒-15格',
      规格单位: '管',
      初始数量: 10.00,
      在库余量: 8.00,
      单价成本: 200.00,
      质控等级: '一级合格',
      知情同意书编号: 'ICF-2026-HCC-01',
      伦理批文号: 'ETH-2026-RENEW-005',
      状态: '部分领用'
    }

    expect(specimen.样本编码).toMatch(/^BB-\d{4}-[A-Z]+-\d{3}$/)
    expect(specimen.在库余量).toBeLessThanOrEqual(specimen.初始数量)
    expect(specimen.在库余量).toBeGreaterThanOrEqual(0)
    expect(['-80℃', '-196℃', '4℃', '常温']).toContain(specimen.储存温度)
  })

  // 2. 伦理前置合规强校验与超期批件硬阻断
  it('关联项目伦理批文过期时必须触发硬阻断，有效批件方可核准通过', () => {
    const today = new Date('2026-10-15')

    // 课题A：伦理批文已于2019年过期
    const projectA = {
      编号: 6,
      项目名称: '临床肿瘤新药早期试验',
      是否需要伦理批文: true,
      伦理批文编号: 'IRB-2018-042',
      伦理审查状态: '已过期',
      伦理批文有效期至: new Date('2019-06-06')
    }

    // 课题B：伦理批文在期有效至2028年
    const projectB = {
      编号: 5,
      项目名称: '基于临床生物样本库的原发性肝细胞癌空间多组学标志物筛选及机制研究',
      是否需要伦理批文: true,
      伦理批文编号: 'ETH-2026-RENEW-005',
      伦理审查状态: '有效',
      伦理批文有效期至: new Date('2028-12-31')
    }

    function checkEthicsForLoan(project: typeof projectA) {
      if (!project.是否需要伦理批文) {
        return { 允许出库: true, 结果: '无需伦理' }
      }
      if (project.伦理审查状态 === '已过期' || project.伦理批文有效期至 < today) {
        return {
          允许出库: false,
          结果: '批文过期硬阻断',
          错误信息: `【临床伦理硬阻断】课题《${project.项目名称}》伦理批文[${project.伦理批文编号}]已逾期失效！严禁出库领用人体生物标本！`
        }
      }
      if (project.伦理审查状态 !== '有效') {
        return {
          允许出库: false,
          结果: '批文缺失硬阻断',
          错误信息: `【临床伦理硬阻断】课题《${project.项目名称}》伦理审查状态非有效状态！`
        }
      }
      return { 允许出库: true, 结果: '伦理有效已核准' }
    }

    const checkA = checkEthicsForLoan(projectA)
    expect(checkA.允许出库).toBe(false)
    expect(checkA.结果).toBe('批文过期硬阻断')
    expect(checkA.错误信息).toContain('严禁出库领用人体生物标本')

    const checkB = checkEthicsForLoan(projectB)
    expect(checkB.允许出库).toBe(true)
    expect(checkB.结果).toBe('伦理有效已核准')
  })

  // 3. 标本出库核准与库存余量扣减
  it('出库核准应正确扣减标本在库余量并流转台账状态', () => {
    let inStock = 10.00
    const loanRequest = {
      台账编号: 1,
      申请数量: 2.00,
      单价成本: 200.00,
      状态: '申请中'
    }

    // 样本库核准出库
    const actualOut = loanRequest.申请数量
    expect(inStock).toBeGreaterThanOrEqual(actualOut)

    inStock -= actualOut
    const loanRecord: BiobankLoanItem = {
      编号: 1,
      样本编号: 1,
      样本编码: 'BB-2026-HCC-001',
      样本类型: '冰冻组织',
      疾病诊断: '原发性肝细胞癌 (HCC)',
      规格单位: '管',
      申请人编号: 3,
      申请人姓名: '张三',
      申请用途: '原发性肝癌空间转录组单细胞测序实验',
      申请数量: 2.00,
      出库数量: actualOut,
      归还数量: 0.00,
      消耗数量: 0.00,
      单价成本: 200.00,
      分摊总金额: 400.00,
      伦理核验结果: '伦理有效已核准',
      借还状态: '已出库',
      申请时间: '2026-10-15 14:12:16',
      出库时间: '2026-10-15 14:12:16',
      出库经办人: '样本库库管员'
    }

    expect(inStock).toBe(8.00)
    expect(loanRecord.出库数量).toBe(2.00)
    expect(loanRecord.借还状态).toBe('已出库')
    expect(loanRecord.分摊总金额).toBe(400.00)
  })

  // 4. 归还与实验破坏性消耗核销及课题科研成本分摊
  it('归还与消耗核销应校验总数守恒并准确分摊课题科研成本', () => {
    const loanRecord = {
      出库数量: 2.00,
      单价成本: 200.00,
      规格单位: '管'
    }

    let inStock = 8.00

    // 实验核销场景：归还 0 管，实验测序完全消耗 2 管
    const returnQty = 0.00
    const consumedQty = 2.00

    expect(returnQty + consumedQty).toBe(loanRecord.出库数量)

    // 回补归还量
    inStock += returnQty
    const allocatedCost = consumedQty * loanRecord.单价成本
    const finalStatus = consumedQty === loanRecord.出库数量 ? '已消耗核销' : '已全部归还'

    expect(inStock).toBe(8.00)
    expect(allocatedCost).toBe(400.00)
    expect(finalStatus).toBe('已消耗核销')
  })

  // 5. 专病队列与温区资产统计大屏指标聚合
  it('应准确聚合标本总数、借出批次与课题分摊总成本', () => {
    const mockStats: BiobankStockStats = {
      总样本数: 4,
      在库样本数: 3,
      正在实验中借出: 1,
      课题已分摊总成本: 400.00,
      类型统计: [
        { 样本类型: '冰冻组织', 样本总数: 1, 在库总余量: 8.00 },
        { 样本类型: '石蜡切片', 样本总数: 1, 在库总余量: 30.00 },
        { 样本类型: '血浆', 样本总数: 1, 在库总余量: 20.00 },
        { 样本类型: '脑脊液', 样本总数: 1, 在库总余量: 6.00 }
      ],
      温区统计: [
        { 储存温度: '-80℃', 标本数: 3 },
        { 储存温度: '常温', 标本数: 1 }
      ]
    }

    const totalStockQty = mockStats.类型统计.reduce((acc, cur) => acc + cur.在库总余量, 0)
    expect(mockStats.总样本数).toBe(4)
    expect(mockStats.正在实验中借出).toBe(1)
    expect(mockStats.课题已分摊总成本).toBe(400)
    expect(totalStockQty).toBe(64.00)
    expect(mockStats.温区统计.find(t => t.储存温度 === '-80℃')?.标本数).toBe(3)
  })
})
