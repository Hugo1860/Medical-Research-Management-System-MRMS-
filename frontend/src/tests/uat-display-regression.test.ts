import { describe, it, expect, vi, beforeEach } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import ElementPlus from 'element-plus'
import Dashboard from '../views/dashboard/index.vue'
import Lifecycle from '../views/project/vertical-lifecycle.vue'
import * as wb from '../api/workbench'
import * as wf from '../api/workflow'
import * as db from '../api/dashboard'
import * as lifecycle from '../api/vertical-lifecycle'
import * as project from '../api/project'
import * as proposal from '../api/vertical-import'

vi.mock('vue-router', () => ({ useRouter: () => ({ push: vi.fn() }), useRoute: () => ({ query: {} }) }))
vi.mock('../stores/user', () => ({ useUserStore: () => ({ userInfo: { 姓名: '验收负责人' } }) }))
vi.mock('echarts', () => ({ init: () => ({ setOption: vi.fn() }) }))
vi.mock('../api/workbench', async (original) => ({ ...await original<typeof import('../api/workbench')>(), getWorkbenchSummary: vi.fn(), getAnalyticsOverview: vi.fn() }))
vi.mock('../api/workflow', async (original) => ({ ...await original<typeof import('../api/workflow')>(), getTodoList: vi.fn() }))
vi.mock('../api/dashboard', async (original) => ({ ...await original<typeof import('../api/dashboard')>(), getDashboardComponents: vi.fn() }))
vi.mock('../api/vertical-lifecycle', async (original) => ({ ...await original<typeof import('../api/vertical-lifecycle')>(), getPagedApprovals: vi.fn(), getApprovalDetail: vi.fn(), getProjectLifecycleArchive: vi.fn() }))
vi.mock('../api/project', async (original) => ({ ...await original<typeof import('../api/project')>(), getMilestoneAlerts: vi.fn(), getProjectTaskbook: vi.fn(), getProjectArchiveChecklist: vi.fn() }))
vi.mock('../api/vertical-import', async (original) => ({ ...await original<typeof import('../api/vertical-import')>(), downloadProposal: vi.fn() }))

beforeEach(() => {
  vi.resetAllMocks()
  vi.mocked(wb.getWorkbenchSummary).mockResolvedValue({ 角色名称: '项目负责人' })
  vi.mocked(wf.getTodoList).mockResolvedValue({ list: [], total: 0 } as any)
  vi.mocked(wb.getAnalyticsOverview).mockResolvedValue({ 指标: [] })
  vi.mocked(db.getDashboardComponents).mockResolvedValue([])
  vi.mocked(lifecycle.getPagedApprovals).mockResolvedValue({ list: [], total: 0 })
  vi.mocked(project.getMilestoneAlerts).mockResolvedValue([])
  vi.mocked(lifecycle.getApprovalDetail).mockResolvedValue({ 立项基本信息: {} })
  vi.mocked(lifecycle.getProjectLifecycleArchive).mockResolvedValue({})
  vi.mocked(project.getProjectTaskbook).mockResolvedValue({ 任务书: [], 签署记录: [] } as any)
  vi.mocked(project.getProjectArchiveChecklist).mockResolvedValue(null as any)
  vi.mocked(proposal.downloadProposal).mockResolvedValue(new Blob(['test'], { type: 'application/pdf' }))
})
async function render(component: any) {
  const wrapper = mount(component, { global: { plugins: [ElementPlus], stubs: { ElDrawer: { props: ['modelValue'], template: '<section v-if="modelValue"><slot /></section>' } } } })
  await flushPromises()
  return wrapper
}

describe('隔离 UAT 展示回归', () => {
  it('立项搜索会传递已清理的项目名称与负责人，供 API 转换查询字段', async () => {
    vi.mocked(lifecycle.getPagedApprovals).mockResolvedValue({ list: [], total: 35 })
    const wrapper = await render(Lifecycle)
    const pagination = wrapper.findComponent({ name: 'ElPagination' })
    pagination.vm.$emit('update:current-page', 2)
    await flushPromises()
    expect(pagination.props('currentPage')).toBe(2)
    await wrapper.get('input[placeholder="搜索课题名称..."]').setValue('  合成课题  ')
    await wrapper.get('input[placeholder="搜索课题负责人..."]').setValue('  张三  ')
    await wrapper.findAll('button').find((button) => button.text().trim() === '查询')!.trigger('click')
    await flushPromises()
    expect(lifecycle.getPagedApprovals).toHaveBeenLastCalledWith({ index: 1, size: 10 }, { Like项目名称: '合成课题', Like负责人姓名: '张三' })
    wrapper.unmount()
  })

  it('立项台账显示 API 的项目中文名称和独立立项文号，不能把流程项目编号误作文号', async () => {
    vi.mocked(lifecycle.getPagedApprovals).mockResolvedValue({ total: 1, list: [{ 编号: 2, 项目中文名称: '合成验收项目', 立项文号: 'UAT-123', 项目编号: 987, 批准经费: 123450, 负责人姓名: '验收负责人' } as any] })
    const wrapper = await render(Lifecycle)
    expect(wrapper.text()).toContain('合成验收项目')
    expect(wrapper.text()).toContain('UAT-123')
    expect(wrapper.text()).toContain('验收负责人')
    expect(wrapper.text()).not.toContain('987')
    wrapper.unmount()
  })
  it('统计和待办接口拒绝时显示不可用，不显示虚假的零项目或零待办', async () => {
    vi.mocked(wb.getAnalyticsOverview).mockRejectedValue(new Error('403'))
    vi.mocked(wf.getTodoList).mockRejectedValue(new Error('403'))
    const wrapper = await render(Dashboard)
    const cards = wrapper.findAll('.metric-card')
    expect(cards[0].text()).toContain('—')
    expect(cards[0].text()).toContain('流程统计暂不可用')
    expect(cards[0].text()).not.toContain('0 项')
    expect(cards[3].text()).toContain('待办总数暂不可用')
    expect(cards[3].text()).not.toContain('0 件')
    expect(wrapper.text()).toContain('待办数据暂不可用')
    wrapper.unmount()
  })
  it('有分页总数时显示真实总数，而不是最多八条列表的条数', async () => {
    vi.mocked(wf.getTodoList).mockResolvedValue({ total: 12, list: [{ 流程编号: 1, 流程名称: '科研待办', 步骤名称: '审批' }] } as any)
    const wrapper = await render(Dashboard)
    const card = wrapper.findAll('.metric-card')[3]
    expect(card.text()).toContain('12')
    expect(card.text()).toContain('列表最多显示 8 条')
    expect(wrapper.text()).toContain('科研待办')
    wrapper.unmount()
  })
  it('可用的统计说明流程视图口径，不把记录数称作不重复项目总数', async () => {
    vi.mocked(wb.getAnalyticsOverview).mockResolvedValue({ 指标: [{ 指标: '纵向项目', 数量: 0, 数值: 0 }, { 指标: '横向项目', 数量: 1, 数值: 10000 }, { 指标: '科研成果', 数量: 2, 数值: 0 }] })
    const wrapper = await render(Dashboard)
    expect(wrapper.text()).toContain('项目流程记录')
    expect(wrapper.text()).toContain('可能重复或遗漏无流程项目')
    expect(wrapper.text()).toContain('同一项目可能重复计入')
    expect(wrapper.text()).not.toContain('已登记科研项目')
    wrapper.unmount()
  })
  it('接口明确返回零待办时显示零，缺少总数字段则显示未知', async () => {
    const first = await render(Dashboard)
    expect(first.findAll('.metric-card')[3].text()).toContain('0')
    expect(first.findAll('.metric-card')[3].text()).toContain('当前没有待办事项')
    first.unmount()
    vi.mocked(wf.getTodoList).mockResolvedValue({ list: [] } as any)
    const second = await render(Dashboard)
    expect(second.findAll('.metric-card')[3].text()).toContain('—')
    expect(second.findAll('.metric-card')[3].text()).toContain('待办总数暂不可用')
    second.unmount()
  })

  it('档案无伦理或中检记录时只提示查阅实际资料，不虚构审查或结论', async () => {
    vi.mocked(lifecycle.getPagedApprovals).mockResolvedValue({ total: 1, list: [{ 编号: 2, 项目中文名称: '验收课题', 批准经费: 1 } as any] })
    vi.mocked(lifecycle.getProjectLifecycleArchive).mockResolvedValue({ 中检记录: [], 变更历史: [] })
    const wrapper = await render(Lifecycle)
    await wrapper.findAll('button').find((button) => button.text().includes('全景档案'))!.trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('生命周期资料提示')
    expect(wrapper.text()).toContain('请查阅对应的实际记录')
    expect(wrapper.text()).toContain('请查阅实际中检记录')
    expect(wrapper.text()).not.toContain('课题通过院内伦理前置预检')
    expect(wrapper.text()).not.toContain('获得正式立项批复')
    expect(wrapper.text()).not.toContain('专家给予优秀通过评定')
    expect(wrapper.text()).not.toContain('中期实验数据完整')
    wrapper.unmount()
  })

  it('授权详情包含受控计划书路径时，通过认证 Blob API 下载原件', async () => {
    vi.mocked(lifecycle.getPagedApprovals).mockResolvedValue({ total: 1, list: [{ 编号: 2, 项目中文名称: '验收课题', 立项文号: 'UAT-1', 批准经费: 1 } as any] })
    vi.mocked(lifecycle.getApprovalDetail).mockResolvedValue({ 立项基本信息: { 立项文件路径: '/VerticalProjectImport/下载计划书?明细编号=123' } })
    const originalCreate = URL.createObjectURL
    const originalRevoke = URL.revokeObjectURL
    URL.createObjectURL = vi.fn(() => 'blob:isolated-test')
    URL.revokeObjectURL = vi.fn()
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {})
    try {
      const wrapper = await render(Lifecycle)
      await wrapper.findAll('button').find((button) => button.text().includes('全景档案'))!.trigger('click')
      await flushPromises()
      expect(lifecycle.getApprovalDetail).toHaveBeenCalledWith(2)
      const button = wrapper.findAll('button').find((b) => b.text().includes('下载计划书原件'))
      expect(button).toBeTruthy()
      await button!.trigger('click')
      await flushPromises()
      expect(proposal.downloadProposal).toHaveBeenCalledWith(123)
      expect(click).toHaveBeenCalledOnce()
      await new Promise((resolve) => setTimeout(resolve, 1100))
      expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:isolated-test')
      wrapper.unmount()
    } finally {
      click.mockRestore()
      URL.createObjectURL = originalCreate
      URL.revokeObjectURL = originalRevoke
    }
  })
  it.each([
    'https://example.invalid/VerticalProjectImport/下载计划书?明细编号=123',
    '/VerticalProjectImport/下载计划书?明细编号=0',
    '/VerticalProjectImport/下载计划书?明细编号=123&redirect=1',
    '/upload/旧文件.pdf'
  ])('非严格受控路径 %s 不显示计划书下载入口', async (path) => {
    vi.mocked(lifecycle.getPagedApprovals).mockResolvedValue({ total: 1, list: [{ 编号: 2, 项目中文名称: '验收课题', 批准经费: 1 } as any] })
    vi.mocked(lifecycle.getApprovalDetail).mockResolvedValue({ 立项基本信息: { 立项文件路径: path } })
    const wrapper = await render(Lifecycle)
    await wrapper.findAll('button').find((button) => button.text().includes('全景档案'))!.trigger('click')
    await flushPromises()
    expect(wrapper.findAll('button').some((button) => button.text().includes('下载计划书原件'))).toBe(false)
    expect(proposal.downloadProposal).not.toHaveBeenCalled()
    wrapper.unmount()
  })

})
