import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  getHorizontalProjects,
  getHorizontalProjectDetail,
  getHorizontalProjectLifecycle,
  createHorizontalProjectChange,
  getHorizontalProjectChangeDetail,
  getHorizontalProjectClosureDetail
} from '../api/horizontal-project'
import request from '../utils/request'

vi.mock('../utils/request', () => {
  const req = {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn()
  }
  return { default: req, ...req }
})

beforeEach(() => {
  vi.mocked(request.get).mockReset()
  vi.mocked(request.post).mockReset()
})

describe('横向项目全周期接口契约与数据流向', () => {
  it('正确传递横向合同台账分页与检索过滤条件', async () => {
    vi.mocked(request.get).mockResolvedValue({ data: { list: [], total: 0 } })
    await getHorizontalProjects(
      { pageIndex: 2, pageSize: 15 },
      { like合同编号: 'HX-2026', like合同名称: '临床试验', 发起人编号: 100 }
    )
    expect(request.get).toHaveBeenCalledWith('/XProject/分页获取横向项目', {
      params: {
        'paging.pageIndex': 2,
        'paging.pageSize': 15,
        'filter.Like合同编号': 'HX-2026',
        'filter.Like合同名称': '临床试验',
        'filter.发起人编号': 100
      }
    })
  })

  it('获取横向项目完整详情（参与人、预算与流程流转）', async () => {
    const mockDetail = {
      基本资料: { 编号: 10, 合同编号: 'HX-001', 合同名称: '新药评价' },
      参与人列表: [{ 参与人姓名: '张三', 工号: '001', 部门名称: '药学部' }],
      经费预算列表: [{ 项目支出类型: '直接经费', 项目支出内容: '试剂耗材', 批准经费: 50000 }],
      审核记录: [{ 步骤名称: '科技处审核', 状态名称: '通过' }]
    }
    vi.mocked(request.get).mockResolvedValue({ data: mockDetail })
    const res = await getHorizontalProjectDetail(10)
    expect(request.get).toHaveBeenCalledWith('/XProject/获取横向项目详情', {
      params: { 横向项目编号: 10 }
    })
    expect(res.data.基本资料.合同编号).toBe('HX-001')
    expect(res.data.参与人列表).toHaveLength(1)
    expect(res.data.经费预算列表).toHaveLength(1)
  })

  it('获取横向项目生命周期档案（延期事件、研究报告与归档材料）', async () => {
    const mockLifecycle = {
      基本资料: { 编号: 10 },
      生命周期申请: [{ 事件类型: '延期', 申请原因: '试验周期延长', 状态: '审核通过' }],
      研究报告: [{ 报告类型: '年度报告', 报告年度: 2026, 状态: '已归档' }],
      归档材料: [{ 材料标题: '总结报告盖章版', 材料类型: 'PDF' }]
    }
    vi.mocked(request.get).mockResolvedValue({ data: mockLifecycle })
    const res = await getHorizontalProjectLifecycle(10)
    expect(request.get).toHaveBeenCalledWith('/XProject/获取横向项目生命周期档案', {
      params: { 横向项目编号: 10 }
    })
    expect(res.data.生命周期申请[0].事件类型).toBe('延期')
    expect(res.data.研究报告).toHaveLength(1)
    expect(res.data.归档材料).toHaveLength(1)
  })

  it('提交横向合同变更申请请求参数符合后端模型', async () => {
    vi.mocked(request.post).mockResolvedValue({ code: 0, message: '成功' })
    const payload = {
      基本资料: {
        横向项目编号: 10,
        合同编号: 'HX-001',
        变更事由: '交付时间顺延三个月',
        变更内容简介: '调整合同第四条履约时间',
        变更时间: '2026-11-02T00:00:00.000Z'
      },
      isHold: false
    }
    await createHorizontalProjectChange(payload)
    expect(request.post).toHaveBeenCalledWith('/XProject/新增横向项目变更', payload)
  })

  it('获取变更详情与结题详情接口参数无误', async () => {
    vi.mocked(request.get).mockResolvedValue({ data: { 编号: 1 } })
    await getHorizontalProjectChangeDetail(10, 5, 1)
    expect(request.get).toHaveBeenCalledWith('/XProject/获取横向项目变更详情', {
      params: { 横向项目编号: 10, 横向项目变更编号: 5, 状态: 1 }
    })

    await getHorizontalProjectClosureDetail(10, 2)
    expect(request.get).toHaveBeenCalledWith('/XProject/获取横向项目结题详情', {
      params: { 横向项目申请编号: 10, 横向项目结题编号: 2 }
    })
  })
})
