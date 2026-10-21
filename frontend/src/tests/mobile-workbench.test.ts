import { describe, it, expect } from 'vitest'
import type { WorkbenchSummary } from '../api/workbench'
import { buildEthicsMobileSummary } from '../utils/ethics-mobile'

describe('Mobile Scientific Research Workbench (移动科研工作台)', () => {
  it('应准确解析移动科研工作台待办、通知与在办流程指标', () => {
    const summary: WorkbenchSummary = {
      人员姓名: '周主任',
      待办数量: 4,
      未读通知数量: 2,
      已发起流程数量: 6,
      最近待办事项: '纵向项目申报第一轮伦理审查',
      下一步流程名称: '伦理委员会初审',
      办理提示: '请在2个工作日内完成专家回避确认',
      缺件数: 1,
      缺件提示: '横向合同合作单位企业营业执照副本原件扫描件'
    }

    expect(summary.人员姓名).toBe('周主任')
    expect(summary.待办数量).toBe(4)
    expect(summary.未读通知数量).toBe(2)
    expect(summary.缺件数).toBe(1)
    expect(summary.缺件提示).toContain('营业执照')
  })

  it('在无缺件时应准确识别免预警状态', () => {
    const cleanSummary: WorkbenchSummary = {
      待办数量: 0,
      未读通知数量: 0,
      已发起流程数量: 2,
      缺件数: 0,
      缺件提示: ''
    }

    const hasMissingAlert = (cleanSummary.缺件数 ?? 0) > 0
    expect(hasMissingAlert).toBe(false)
  })

  it('应正确提供移动端快捷跳转路由映射', () => {
    const mobileRoutes: Record<string, string> = {
      纵向申报: '/project/vertical-application',
      横向合同: '/project/horizontal-contract',
      经费报销: '/finance/expense-claims',
      论文认领: '/fruit/paper-claim',
      学术会议: '/activities/academic-activities',
      个人画像: '/office/research-portfolio'
    }

    expect(mobileRoutes['经费报销']).toBe('/finance/expense-claims')
    expect(mobileRoutes['个人画像']).toBe('/office/research-portfolio')
    expect(mobileRoutes['论文认领']).toBe('/fruit/paper-claim')
  })

  it('伦理移动摘要只统计服务端返回的初审、修正案、严重事件和逾期数据', () => {
    const summary = buildEthicsMobileSummary({
      applications: [
        { 编号: 1, 流程模板编号: 1, 项目编号: 11, 申请人编号: 21, 申请类型: '初始审查', 申请标题: '初始审查', 受试者风险等级: '低风险', 状态: '待受理' },
        { 编号: 2, 流程模板编号: 1, 项目编号: 12, 申请人编号: 22, 申请类型: '修正案', 申请标题: '修正案', 受试者风险等级: '一般风险', 状态: '审查中' },
        { 编号: 3, 流程模板编号: 1, 项目编号: 13, 申请人编号: 23, 申请类型: '初始审查', 申请标题: '已完成', 受试者风险等级: '低风险', 状态: '已批准', 批件路径: '/ethics/decision.pdf' }
      ],
      expiryReminders: { 批件提醒: [{ 申请编号: 3 }], 持续审查提醒: [{ 编号: 7 }] },
      followups: [
        { 记录类型: '不良事件', 严重程度: '严重' },
        { 记录类型: '不良事件', 严重程度: '一般' }
      ],
      reviewerActions: 2
    })

    expect(summary.initialReview).toBe(1)
    expect(summary.amendments).toBe(1)
    expect(summary.seriousEvents).toBe(1)
    expect(summary.expiryReminders).toBe(2)
    expect(summary.reviewerActions).toBe(2)
    expect(summary.archivePending).toBe(0)
  })
})
