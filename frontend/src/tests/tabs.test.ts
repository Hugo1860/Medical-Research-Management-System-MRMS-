import { describe, it, expect, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useTabsStore } from '@/stores/tabs'
import type { RouteLocationNormalized } from 'vue-router'

describe('Tabs Store (多标签页状态管理)', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('初始状态应包含固定科研工作台标签', () => {
    const tabsStore = useTabsStore()
    expect(tabsStore.tabs.length).toBe(1)
    expect(tabsStore.tabs[0].path).toBe('/dashboard')
    expect(tabsStore.tabs[0].title).toBe('科研工作台')
  })

  it('新增标签页时应自动去重并加入列表', () => {
    const tabsStore = useTabsStore()
    const mockRoute = {
      path: '/project/vertical-application',
      fullPath: '/project/vertical-application',
      name: 'VerticalApplication',
      query: {},
      meta: { title: '纵向项目申报' }
    } as unknown as RouteLocationNormalized

    tabsStore.addTab(mockRoute)
    expect(tabsStore.tabs.length).toBe(2)
    expect(tabsStore.tabs[1].path).toBe('/project/vertical-application')
    expect(tabsStore.tabs[1].title).toBe('纵向项目申报')

    // 重复添加同一路由不应增加标签数量
    tabsStore.addTab(mockRoute)
    expect(tabsStore.tabs.length).toBe(2)
  })

  it('关闭标签页时应保留工作台且正确移除目标标签', () => {
    const tabsStore = useTabsStore()
    const mockRoute = {
      path: '/organization/people',
      fullPath: '/organization/people',
      name: 'OrganizationPeople',
      query: {},
      meta: { title: '人员中心' }
    } as unknown as RouteLocationNormalized

    tabsStore.addTab(mockRoute)
    expect(tabsStore.tabs.length).toBe(2)

    tabsStore.closeTab('/organization/people')
    expect(tabsStore.tabs.length).toBe(1)
    expect(tabsStore.tabs[0].path).toBe('/dashboard')

    // 不能关闭固定的工作台标签
    tabsStore.closeTab('/dashboard')
    expect(tabsStore.tabs.length).toBe(1)
  })

  it('关闭其他标签页时应只保留当前选中的标签与工作台', () => {
    const tabsStore = useTabsStore()
    tabsStore.addTab({
      path: '/page1',
      fullPath: '/page1',
      name: 'Page1',
      query: {},
      meta: { title: '页面1' }
    } as unknown as RouteLocationNormalized)

    tabsStore.addTab({
      path: '/page2',
      fullPath: '/page2',
      name: 'Page2',
      query: {},
      meta: { title: '页面2' }
    } as unknown as RouteLocationNormalized)

    expect(tabsStore.tabs.length).toBe(3)

    tabsStore.closeOthers('/page2')
    expect(tabsStore.tabs.length).toBe(2)
    expect(tabsStore.tabs.some(t => t.path === '/page2')).toBe(true)
    expect(tabsStore.tabs.some(t => t.path === '/dashboard')).toBe(true)
  })
})
