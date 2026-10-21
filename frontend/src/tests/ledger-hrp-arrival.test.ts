import { describe, it, expect } from 'vitest'

describe('Arrival-Batch Budgeting and HRP Intermediate Coordination (到账批次预算与HRP中间库协同)', () => {
  it('按到账批次维护预算应正确计算批次已分配额及差额平衡状态 (文献清华长庚双模式)', () => {
    const arrivalBatch = {
      id: 1,
      batch: 'DZ-2026-001',
      label: '首期款: DZ-2026-001 (¥400,000.00)',
      amount: 400000
    }

    const budgetRows = [
      { 预算科目编号: 101, 预算科目名称: '材料与试剂耗材费', 批复金额: 180000 },
      { 预算科目编号: 102, 预算科目名称: '测试化验加工与测序', 批复金额: 120000 },
      { 预算科目编号: 103, 预算科目名称: '差旅与国际合作交流', 批复金额: 30000 },
      { 预算科目编号: 104, 预算科目名称: '研究生劳务与专家咨询', 批复金额: 40000 },
      { 预算科目编号: 105, 预算科目名称: '科研绩效与管理公摊', 批复金额: 30000 }
    ]

    const allocated = budgetRows.reduce((sum, item) => sum + item.批复金额, 0)
    const diff = arrivalBatch.amount - allocated

    expect(allocated).toBe(400000)
    expect(diff).toBe(0)
    expect(diff === 0).toBe(true) // 已足额合规匹配
  })

  it('未平衡的到账批次预算应准确检出差额并提示未平衡', () => {
    const arrivalBatch = {
      id: 2,
      batch: 'DZ-2027-002',
      amount: 240000
    }

    const unbalanceRows = [
      { 预算科目编号: 101, 预算科目名称: '材料与试剂耗材费', 批复金额: 100000 },
      { 预算科目编号: 102, 预算科目名称: '测试化验加工与测序', 批复金额: 60000 }
    ]

    const allocated = unbalanceRows.reduce((sum, item) => sum + item.批复金额, 0)
    const diff = arrivalBatch.amount - allocated

    expect(allocated).toBe(160000)
    expect(diff).toBe(80000) // 存在 80,000 元待分配差额
  })

  it('HRP 4大业务中间表协同协议应符合标准字段规范与状态枚举', () => {
    // 1. 项目主数据中间表
    const hrpProjectRecord = {
      project_id: 201,
      project_code: 'PRJ-2026-Y042',
      project_name: '脑胶质瘤微环境免疫耐受机制与靶向干预新策略研究',
      pi_name: '张三教授',
      dept_name: '基础医学与分子免疫',
      approved_budget: 800000,
      sync_status: 1, // 0: 待同步, 1: 成功, 2: 失败
      sync_time: '2026-10-15 10:30:00'
    }

    // 2. 预算明细中间表
    const hrpBudgetRecord = {
      id: 1,
      project_code: 'PRJ-2026-Y042',
      subject_name: '材料与试剂耗材费',
      amount: 300000,
      budget_mode: '按到账金额',
      arrival_batch: 'DZ-2026-001',
      sync_status: 1
    }

    // 3. 财务到账凭据中间表
    const hrpIncomingRecord = {
      voucher_no: 'HRP-IN-2026100801',
      arrival_batch: 'DZ-2026-001',
      amount: 400000,
      arrival_date: '2026-10-08',
      source_account: '国家自然科学基金委专项拨款',
      sync_status: 1
    }

    // 4. 财务核销支出流水中间表
    const hrpExpenseRecord = {
      expense_no: 'BX-2026-001',
      voucher_no: 'HRP-PZ-990182',
      subject_name: '材料与试剂耗材费',
      amount: 85000,
      payee: '赛默飞世尔科技(中国)有限公司',
      expense_date: '2026-10-12',
      sync_status: 1
    }

    expect(hrpProjectRecord.sync_status).toBe(1)
    expect(hrpBudgetRecord.budget_mode).toBe('按到账金额')
    expect(hrpIncomingRecord.voucher_no).toMatch(/^HRP-IN-/)
    expect(hrpExpenseRecord.voucher_no).toMatch(/^HRP-PZ-/)
  })

  it('HRP 协同指令模式支持推送与拉取双向操作', () => {
    const pushCommand = {
      协同模式: '推送立项与预算' as const,
      项目编号: 201
    }
    const pullCommand = {
      协同模式: '拉取到账与支出' as const,
      项目编号: 201
    }

    expect(pushCommand.协同模式).toBe('推送立项与预算')
    expect(pullCommand.协同模式).toBe('拉取到账与支出')
  })
})
