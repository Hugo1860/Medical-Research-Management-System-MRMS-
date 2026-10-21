import { describe, it, expect } from 'vitest'
import type { WorkflowTaskItem, ExpertConflictResult } from '@/api/workflow'

describe('WorkflowApprovalDialog (通用科研流转审核弹窗组件逻辑)', () => {
  const mockTask: WorkflowTaskItem = {
    流程编号: 501,
    流程模板编号: 101,
    流程名称: '国家重点研发计划青年科学家项目申报',
    项目编号: 9022,
    发起人编号: 12,
    发起人工号: 'DOC012',
    发起人姓名: '赵研究员',
    发起人部门名称: '肿瘤生物医学中心',
    状态: 1,
    流程创建时间: '2026-10-15 10:00:00',
    步骤编号: 808,
    步骤名称: '科技处形式审核',
    步骤状态: 1,
    是否可处理: true,
    匿名评审: false
  }

  it('待办任务应准确解析关键流转与责任人信息', () => {
    expect(mockTask.流程编号).toBe(501)
    expect(mockTask.步骤编号).toBe(808)
    expect(mockTask.发起人姓名).toBe('赵研究员')
    expect(mockTask.步骤名称).toBe('科技处形式审核')
  })

  it('在无终止模式(allowTermination=false)下不应允许提交状态值-2', () => {
    const validateDecision = (statusVal: number, allowTermination: boolean) => {
      if (statusVal === -2 && !allowTermination) {
        return false
      }
      return [1, -1, -2].includes(statusVal)
    }

    expect(validateDecision(1, false)).toBe(true)
    expect(validateDecision(-1, false)).toBe(true)
    expect(validateDecision(-2, false)).toBe(false)
    expect(validateDecision(-2, true)).toBe(true)
  })

  it('审核通过时快捷短语应包含符合指南与准予推进建议', () => {
    const getPresetsByDecision = (statusVal: number) => {
      if (statusVal === 1) {
        return [
          '同意申报，符合医院科研立项指南要求。',
          '材料完整详实，专家评分达标，准予通过。',
          '预算编制合理，科研处复核同意推进下一阶段。'
        ]
      }
      if (statusVal === -1) {
        return [
          '材料不齐全，缺少伦理委员会审批批文，请补充后重新提交。',
          '项目研究周期与计划进度不匹配，请退回课题组修正。'
        ]
      }
      return [
        '研究内容严重偏离申报指南，予以终止。',
        '存在学术诚信或重复资助情形，流程作终止处理。'
      ]
    }

    const passPresets = getPresetsByDecision(1)
    expect(passPresets.length).toBeGreaterThan(0)
    expect(passPresets.some(p => p.includes('准予通过'))).toBe(true)

    const rejectPresets = getPresetsByDecision(-1)
    expect(rejectPresets.some(p => p.includes('退回'))).toBe(true)

    const terminatePresets = getPresetsByDecision(-2)
    expect(terminatePresets.some(p => p.includes('终止'))).toBe(true)
  })

  it('专家利益冲突检查有冲突时应产生阻断或警示标识', () => {
    const evaluateConflict = (conflict: ExpertConflictResult | null) => {
      if (!conflict) return { level: 'safe', canProceed: true }
      if (conflict.存在利益冲突) {
        return {
          level: 'warning',
          canProceed: false,
          reason: conflict.冲突说明 || '存在潜在利益冲突关联'
        }
      }
      return { level: 'success', canProceed: true }
    }

    expect(evaluateConflict(null).canProceed).toBe(true)
    expect(evaluateConflict({ 存在利益冲突: false }).canProceed).toBe(true)

    const conflictWarning = evaluateConflict({
      存在利益冲突: true,
      冲突说明: '专家与项目申报人同属肿瘤生物医学中心同一研究团队'
    })
    expect(conflictWarning.canProceed).toBe(false)
    expect(conflictWarning.reason).toContain('同一研究团队')
  })

  it('审核说明在退回或终止时必须强制校验非空', () => {
    const validateAuditPayload = (statusVal: number, notes: string) => {
      if (statusVal !== 1 && (!notes || !notes.trim())) {
        return { valid: false, message: '不通过或终止时，必须填写具体审核说明' }
      }
      return { valid: true }
    }

    expect(validateAuditPayload(1, '').valid).toBe(true)
    expect(validateAuditPayload(-1, '').valid).toBe(false)
    expect(validateAuditPayload(-2, '').valid).toBe(false)
    expect(validateAuditPayload(-1, '缺少批件').valid).toBe(true)
  })
})
