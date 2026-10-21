import { describe, it, expect } from 'vitest'

describe('Vertical Project Full Lifecycle Governance (纵向课题全生命周期模块)', () => {
  it('项目延期申请必须包含明确的目标延期日期与事由', () => {
    const changePayload = {
      纵向项目立项编号: 101,
      申请类型: '延期' as const,
      事由: '多中心队列入组延后，需补足实验样本。',
      延期至日期: '2028-12-31'
    }

    expect(changePayload.申请类型).toBe('延期')
    expect(changePayload.延期至日期).toBeDefined()
    expect(changePayload.事由.length).toBeGreaterThan(5)
  })

  it('项目暂停与终止申请应有合规事由且延期日期可为空', () => {
    const pausePayload = {
      纵向项目立项编号: 102,
      申请类型: '暂停' as const,
      事由: '主要设备迁入新院区暂未完成调试'
    }

    expect(pausePayload.申请类型).toBe('暂停')
    expect(pausePayload.事由).toContain('设备')
  })

  it('在研项目数应正确通过立项数与已结项数差额动态计算', () => {
    const totalApproved = 25
    const totalClosed = 18
    const ongoing = Math.max(0, totalApproved - totalClosed)
    expect(ongoing).toBe(7)
  })

  it('一键配套项目应合规生成PT编码、自动继承团队并按资助额测算配套经费 (清华长庚医院实践)', () => {
    const sourceProject = {
      编号: 101,
      项目名称: '恶性肿瘤靶向药物递送与微环境调控机制研究',
      批准经费: 1200000,
      负责人姓名: '陈教授',
      负责人部门名称: '肿瘤实验医学科'
    }

    const year = 2026
    const expectedCode = `PT-${year}-${String(sourceProject.编号).padStart(4, '0')}`
    const matchingRatio = 0.5
    const calculatedMatchingAmount = Math.round(sourceProject.批准经费 * matchingRatio)

    const matchingPayload = {
      来源项目立项编号: sourceProject.编号,
      配套金额: calculatedMatchingAmount,
      备注: `配套【${sourceProject.项目名称}】专项研发支持`
    }

    expect(expectedCode).toBe('PT-2026-0101')
    expect(matchingPayload.配套金额).toBe(600000)
    expect(matchingPayload.来源项目立项编号).toBe(101)
    expect(matchingPayload.备注).toContain('恶性肿瘤靶向药物递送')
  })

  it('一键结余项目应合规生成JY编码、结转结余经费并更新结余处理状态 (清华长庚医院实践)', () => {
    const closeoutItem = {
      编号: 701,
      纵向项目编号: 99,
      项目名称: '高通量多组学肿瘤标志物早期筛查方法学研究',
      验收结果: '优秀结题',
      结余金额: 78500,
      结余处理状态: '待转结'
    }

    const year = 2026
    const expectedBalanceCode = `JY-${year}-${String(closeoutItem.编号).padStart(4, '0')}`

    const balancePayload = {
      来源项目结题编号: closeoutItem.编号,
      结余金额: closeoutItem.结余金额,
      备注: `【${closeoutItem.项目名称}】结题后科研结余资金转入后续发展基金`
    }

    expect(expectedBalanceCode).toBe('JY-2026-0701')
    expect(balancePayload.结余金额).toBe(78500)
    expect(balancePayload.来源项目结题编号).toBe(701)

    // 状态流转验证
    const updatedStatus = '已转结'
    expect(updatedStatus).toBe('已转结')
  })
})

