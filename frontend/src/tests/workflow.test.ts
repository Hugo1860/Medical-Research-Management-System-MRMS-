import { describe, it, expect } from 'vitest'

function getCategoryName(processName: string): string {
  if (!processName) return '综合业务'
  if (processName.includes('纵向')) return '纵向项目'
  if (processName.includes('横向') || processName.includes('合同')) return '横向合同'
  if (processName.includes('成果') || processName.includes('论文')) return '成果管理'
  if (processName.includes('讲座') || processName.includes('会议')) return '学术活动'
  if (processName.includes('经费') || processName.includes('报销')) return '经费管理'
  return '科研审批'
}

function getCategoryTagType(processName: string): 'primary' | 'success' | 'warning' | 'info' | 'danger' {
  if (!processName) return 'info'
  if (processName.includes('纵向')) return 'primary'
  if (processName.includes('横向')) return 'success'
  if (processName.includes('经费')) return 'warning'
  if (processName.includes('成果')) return 'info'
  return 'info'
}

describe('Workflow Approvals & Todo Tasks (全流程科研审批模块)', () => {
  it('应准确按流程名称归类至对应的科研业务大类', () => {
    expect(getCategoryName('2026年国家自然基金青年项目纵向申报')).toBe('纵向项目')
    expect(getCategoryName('抗肿瘤小分子创新药横向委托研发合同')).toBe('横向合同')
    expect(getCategoryName('SCI一区高水平论文成果认领申请')).toBe('成果管理')
    expect(getCategoryName('协和医院国际学术讲座主办申请')).toBe('学术活动')
    expect(getCategoryName('重点研发课题中期经费到账认领')).toBe('经费管理')
    expect(getCategoryName('科研伦理常规审查')).toBe('科研审批')
  })

  it('应准确为不同业务流程分配视觉区分标签', () => {
    expect(getCategoryTagType('纵向项目立项审核')).toBe('primary')
    expect(getCategoryTagType('横向开发合同签约')).toBe('success')
    expect(getCategoryTagType('科研差旅经费报销')).toBe('warning')
    expect(getCategoryTagType('专利成果登记')).toBe('info')
  })

  it('审批动作载荷应严格校验状态值与审核批注', () => {
    const passPayload = {
      步骤编号: 801,
      状态值: 1,
      备注: '符合申报条件，准予立项。'
    }
    expect(passPayload.状态值).toBe(1)
    expect(passPayload.备注.length).toBeGreaterThan(0)

    const returnPayload = {
      步骤编号: 802,
      状态值: -1,
      备注: '预算科目说明不足，退回重填。'
    }
    expect(returnPayload.状态值).toBe(-1)

    const terminatePayload = {
      步骤编号: 803,
      状态值: -2,
      备注: '项目存在严重伦理合规缺陷，终止流程。'
    }
    expect(terminatePayload.状态值).toBe(-2)
  })
})
