import { describe, it, expect } from 'vitest'
import { buildApplicationPayload } from '../api/project'

describe('Project API & Payload Builder (纵向项目申报数据组装)', () => {
  it('应正确组装扁平表单为后端预期的基本资料与参与人结构', () => {
    const formData = {
      名称: '单细胞转录组免疫机制研究',
      学科门类: '医学',
      项目级别: '国家级重点',
      申请经费: 800000,
      申报年度: 2026,
      dateRange: ['2026-01-01', '2028-12-31'],
      摘要: '深入探索恶性肿瘤耐药微环境机制',
      伦理要求: true,
      是否需要伦理批文: true,
      伦理批文编号: 'KY2026-001',
      伦理批文路径: '/upload/approval.pdf',
      参与人列表: [
        { 参与人编号: 6, 工作量占比: 70, 备注: '课题主持' },
        { 参与人编号: 2, 工作量占比: 30, 备注: '生信分析' }
      ]
    }

    const payload = buildApplicationPayload(formData, false)

    expect(payload.IsHold).toBe(false)
    expect(payload.基本资料.项目名称).toBe('单细胞转录组免疫机制研究')
    expect(payload.基本资料.学科门类).toBe('医学')
    expect(payload.基本资料.申请经费).toBe(800000)
    expect(payload.基本资料.计划开始日期).toBe('2026-01-01')
    expect(payload.基本资料.计划完成日期).toBe('2028-12-31')
    expect(payload.基本资料.是否需要伦理评审).toBe(true)
    expect(payload.基本资料.是否需要伦理批文).toBe(true)
    expect(payload.基本资料.伦理批文编号).toBe('KY2026-001')
    expect(payload.参与人列表).toHaveLength(2)
    expect(payload.参与人列表[0].工作量占比).toBe(70)
  })

  it('暂存草稿时 IsHold 应设为 true', () => {
    const formData = {
      名称: '草稿项目',
      申请经费: 100000
    }
    const payload = buildApplicationPayload(formData, true)
    expect(payload.IsHold).toBe(true)
    expect(payload.基本资料.项目名称).toBe('草稿项目')
  })

  it('当输入已是标准嵌套结构时应原样透传', () => {
    const standardPayload = {
      基本资料: {
        项目名称: '透传项目',
        申请经费: 500000
      },
      参与人列表: [{ 参与人编号: 6, 工作量占比: 100 }],
      IsHold: false
    }

    const result = buildApplicationPayload(standardPayload, false)
    expect(result.基本资料.项目名称).toBe('透传项目')
    expect(result.参与人列表[0].参与人编号).toBe(6)
  })
})
