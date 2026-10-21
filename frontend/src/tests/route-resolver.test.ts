import { describe, it, expect } from 'vitest'
import { resolveModernRoute } from '../utils/route-resolver'

describe('Route Resolver (科研办公与全业务路由接管解析器)', () => {
  it('应准确将所有科研办公核心菜单无缝接管为现代 SPA 路由', () => {
    // 1. 通知公告
    expect(resolveModernRoute('科研办公/科研办公/通知公告.html')).toBe('/office/notice')
    expect(resolveModernRoute('/IMIS/views/科研办公/科研办公/通知公告.html')).toBe('/office/notice')
    expect(resolveModernRoute('科研办公/科研办公/添加通知公告.html')).toBe('/office/notice?action=add')
    expect(resolveModernRoute('科研办公/科研办公/通知公告详情.html')).toBe('/office/notice')

    // 2. 制度与下载中心
    expect(resolveModernRoute('科研办公/科研办公/下载中心.html')).toBe('/office/downloads')
    expect(resolveModernRoute('科研办公/科研办公/添加下载.html')).toBe('/office/downloads?action=add')
    expect(resolveModernRoute('科研办公/科研办公/下载详情.html')).toBe('/office/downloads')

    // 3. 个人资料与修改密码
    expect(resolveModernRoute('科研办公/科研办公/个人资料.html')).toBe('/office/profile')
    expect(resolveModernRoute('科研办公/科研办公/修改密码.html')).toBe('/office/profile?tab=security')

    // 4. 我的科研（科研详情、待办）
    expect(resolveModernRoute('科研办公/我的科研/科研详情.html')).toBe('/office/research-portfolio')
    expect(resolveModernRoute('科研办公/我的科研/科研待办.html')).toBe('/workflow/todo-approvals')

    // 5. 移动工作台与科研治理
    expect(resolveModernRoute('科研办公/科研办公/移动科研工作台.html')).toBe('/office/mobile-workbench')
    expect(resolveModernRoute('科研办公/科研办公/科研治理中心.html')).toBe('/operations/external-sync')
    expect(resolveModernRoute('科研办公/科研办公/权限代理与预算对账.html')).toBe('/governance/proxy-reconciliation')
    expect(resolveModernRoute('科研办公/科研办公/选择用户.html')).toBe('/organization/people?tab=users')
    expect(resolveModernRoute('科研办公/科研办公/科研评价管理.html')).toBe('/evaluation/performance-evaluation')
  })

  it('应能够正确自动解码后端返回的 URL 编码路径（Uri.EscapeUriString）', () => {
    // 后端 /Access/Login 和 /Access/Me 中处理中文后的实际输出格式
    const encodedNotice = '%E7%A7%91%E7%A0%94%E5%8A%9E%E5%85%AC/%E7%A7%91%E7%A0%94%E5%8A%9E%E5%85%AC/%E9%80%9A%E7%9F%A5%E5%85%AC%E5%91%8A.html'
    const encodedDownload = '%E7%A7%91%E7%A0%94%E5%8A%9E%E5%85%AC/%E7%A7%91%E7%A0%94%E5%8A%9E%E5%85%AC/%E4%B8%8B%E8%BD%BD%E4%B8%AD%E5%BF%83.html'
    const encodedProfile = '%E7%A7%91%E7%A0%94%E5%8A%9E%E5%85%AC/%E7%A7%91%E7%A0%94%E5%8A%9E%E5%85%AC/%E4%B8%AA%E4%BA%BA%E8%B5%84%E6%96%99.html'
    const encodedTodo = '%E7%A7%91%E7%A0%94%E5%8A%9E%E5%85%AC/%E6%88%91%E7%9A%84%E7%A7%91%E7%A0%94/%E7%A7%91%E7%A0%94%E5%BE%85%E5%8A%9E.html'

    expect(resolveModernRoute(encodedNotice)).toBe('/office/notice')
    expect(resolveModernRoute(encodedDownload)).toBe('/office/downloads')
    expect(resolveModernRoute(encodedProfile)).toBe('/office/profile')
    expect(resolveModernRoute(encodedTodo)).toBe('/workflow/todo-approvals')
  })

  it('应准确按参数解析科研业务台账的全部横向、纵向与财务模块', () => {
    expect(resolveModernRoute('科研办公/科研办公/科研业务台账.html?module=x-list')).toBe('/project/horizontal-contract')
    expect(resolveModernRoute('科研办公/科研办公/科研业务台账.html?module=y-income')).toBe('/finance/incoming')
    expect(resolveModernRoute('科研办公/科研办公/科研业务台账.html?module=y-approval')).toBe('/project/vertical-lifecycle?tab=approvals')
    expect(resolveModernRoute('科研办公/科研办公/科研业务台账.html?module=y-expense')).toBe('/finance/expense-claims?tab=y-expense')
    expect(resolveModernRoute('科研办公/科研办公/科研业务台账.html?module=bill-search')).toBe('/finance/expense-claims?tab=bills')
    expect(resolveModernRoute('科研办公/科研办公/科研业务台账.html?module=y-application')).toBe('/project/vertical-application')
    expect(resolveModernRoute('科研办公/科研办公/科研业务台账.html')).toBe('/project/vertical-lifecycle')
  })

  it('应动态解析参数化字典维护路径并传递字典名称参数', () => {
    const raw = '基础信息维护/字典.html?name=语种类型&type=1'
    expect(resolveModernRoute(raw)).toBe('/base-info/dictionary?name=%E8%AF%AD%E7%A7%8D%E7%B1%BB%E5%9E%8B')
    expect(resolveModernRoute('基础信息维护/字典.html')).toBe('/base-info/dictionary')
  })

  it('对未提供或未知外链应返回 null 以供 fallback', () => {
    expect(resolveModernRoute('')).toBeNull()
    expect(resolveModernRoute(null)).toBeNull()
    expect(resolveModernRoute('http://external.edu.cn/page.html')).toBeNull()
  })
})
