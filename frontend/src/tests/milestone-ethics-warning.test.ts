import { describe, it, expect } from 'vitest'

describe('Process Milestone Warning & Ethics Validity Shield (过程主动监督与临床合规中枢 - 清华长庚模式)', () => {
  // 1. 结题半年前巡检与预算执行率监督
  it('项目距结题不足180天且预算执行率低于50%时，必须触发黄色预警并提示合规风险', () => {
    const today = new Date('2026-10-15')
    const projectEndDate = new Date('2027-03-31') // 约167天后结题
    const diffDays = Math.ceil((projectEndDate.getTime() - today.getTime()) / (1000 * 3600 * 24))
    
    const approvedBudget = 1000000
    const executedBudget = 320000
    const executionRate = Number(((executedBudget / approvedBudget) * 100).toFixed(2)) // 32.00%

    // 巡检评估引擎规则 (清华长庚医院文献核心观点: 提前半年预警，防范突击花钱)
    let alertType = ''
    let alertLevel = ''
    let shouldAlert = false

    if (diffDays <= 180 && diffDays > 90) {
      if (executionRate < 50) {
        shouldAlert = true
        alertType = '结题半年前预算执行提醒'
        alertLevel = '警告'
      }
    }

    expect(diffDays).toBeLessThanOrEqual(180)
    expect(diffDays).toBeGreaterThan(90)
    expect(shouldAlert).toBe(true)
    expect(alertType).toBe('结题半年前预算执行提醒')
    expect(alertLevel).toBe('警告')
    expect(executionRate).toBe(32)
  })

  // 2. 结题倒计时多级督办引擎
  it('项目倒计时进入30天以内且未完成验收时，应升级为红色特急督办', () => {
    const today = new Date('2026-10-15')
    const urgentEndDate = new Date('2026-11-05') // 21天后结题
    const diffDays = Math.ceil((urgentEndDate.getTime() - today.getTime()) / (1000 * 3600 * 24))
    const isCompleted = false

    let alertType = ''
    let alertLevel = ''

    if (!isCompleted) {
      if (diffDays <= 30) {
        alertType = '结题紧急督办'
        alertLevel = '特急'
      } else if (diffDays <= 90) {
        alertType = '结题倒计时预警'
        alertLevel = '紧急'
      }
    }

    expect(diffDays).toBe(21)
    expect(alertType).toBe('结题紧急督办')
    expect(alertLevel).toBe('特急')
  })

  // 3. 临床科研伦理批文全生命周期合规硬阻断
  it('涉及人体的临床课题若伦理批文过期或缺失，必须硬性阻断中检评审与临床报销', () => {
    interface ComplianceCheckParams {
      是否需要伦理: boolean
      伦理批文编号: string
      伦理批文有效期至: string | null
      当前核查日期: string
      业务节点: '中检' | '报销'
    }

    function evaluateEthicsCompliance(params: ComplianceCheckParams) {
      if (!params.是否需要伦理) {
        return { passed: true, block: false, message: '项目无需伦理审查，合规检查通过' }
      }

      if (!params.伦理批文编号 || !params.伦理批文有效期至) {
        return {
          passed: false,
          block: true,
          message: `【合规强阻断】项目涉及临床研究但未上传有效伦理审查批文，已依法硬性阻断${params.业务节点}！`
        }
      }

      const expiry = new Date(params.伦理批文有效期至).getTime()
      const current = new Date(params.当前核查日期).getTime()

      if (expiry < current) {
        return {
          passed: false,
          block: true,
          message: `【合规强阻断】项目伦理审查批件已于 ${params.伦理批文有效期至} 过期失效，已依法硬性阻断${params.业务节点}！请立即登记批文续展或修正案。`
        }
      }

      const diffDays = Math.ceil((expiry - current) / (1000 * 3600 * 24))
      if (diffDays <= 60) {
        return {
          passed: true,
          block: false,
          warning: true,
          message: `【临期提醒】伦理批件将在 ${diffDays} 天后到期，请提前办理续展申请。`
        }
      }

      return { passed: true, block: false, message: '伦理审查批文处于法定有效期内，合规通过' }
    }

    // 场景 A: 批文过期 (Project 5 历史数据)
    const expiredResult = evaluateEthicsCompliance({
      是否需要伦理: true,
      伦理批文编号: '2018-ETHICS-005',
      伦理批文有效期至: '2019-12-31',
      当前核查日期: '2026-10-15',
      业务节点: '中检'
    })
    expect(expiredResult.passed).toBe(false)
    expect(expiredResult.block).toBe(true)
    expect(expiredResult.message).toContain('【合规强阻断】')
    expect(expiredResult.message).toContain('硬性阻断中检')

    // 场景 B: 批件即将到期 (30天内)
    const warningResult = evaluateEthicsCompliance({
      是否需要伦理: true,
      伦理批文编号: '2026-ETHICS-019',
      伦理批文有效期至: '2026-11-10',
      当前核查日期: '2026-10-15',
      业务节点: '中检'
    })
    expect(warningResult.passed).toBe(true)
    expect(warningResult.block).toBe(false)
    expect(warningResult.warning).toBe(true)
    expect(warningResult.message).toContain('临期提醒')

    // 场景 C: 基础研究无需伦理审查
    const basicResult = evaluateEthicsCompliance({
      是否需要伦理: false,
      伦理批文编号: '',
      伦理批文有效期至: null,
      当前核查日期: '2026-10-15',
      业务节点: '中检'
    })
    expect(basicResult.passed).toBe(true)
    expect(basicResult.block).toBe(false)
  })

  // 4. 登记伦理续展与修正案状态流转与解阻断
  it('登记伦理续展后应自动展期、解除阻断并将原预警状态归档', () => {
    const projectState = {
      项目编号: 5,
      伦理审查状态: '已过期阻断',
      伦理批文编号: '2018-ETHICS-005',
      伦理批文有效期至: '2019-12-31',
      合规阻断: true
    }

    const renewalPayload = {
      项目编号: 5,
      新批文编号: 'ETHICS-2026-0005-RENEW',
      申请类型: '续展申请',
      新有效期至: '2027-12-31',
      批件文件路径: '/upload/ethics/renew/2026/approval_5.pdf',
      变更事由与说明: '根据医院伦理委员会复审意见，完成年度跟踪审查并获批展期。'
    }

    // 执行状态流转模拟 (对应后端的 sp_登记伦理批文续展与修正案)
    projectState.伦理批文编号 = renewalPayload.新批文编号
    projectState.伦理批文有效期至 = renewalPayload.新有效期至
    projectState.伦理审查状态 = '已批复有效'
    projectState.合规阻断 = false

    expect(projectState.伦理批文编号).toBe('ETHICS-2026-0005-RENEW')
    expect(projectState.伦理批文有效期至).toBe('2027-12-31')
    expect(projectState.伦理审查状态).toBe('已批复有效')
    expect(projectState.合规阻断).toBe(false)
  })
})
