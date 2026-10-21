import { describe, it, expect, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useMenuOrderStore } from '@/stores/menuOrder'
import type { PermissionNode } from '@/api/auth'

describe('Menu Order Store (菜单自定义排序状态管理)', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
  })

  const sampleNodes: PermissionNode[] = [
    {
      菜单: { 编号: 1, 名称: '科研办公', 路径: '' },
      子级菜单: [
        { 菜单: { 编号: 101, 名称: '通知公告', 路径: '/office/notice' }, 子级菜单: [] },
        { 菜单: { 编号: 102, 名称: '下载中心', 路径: '/office/downloads' }, 子级菜单: [] }
      ]
    },
    {
      菜单: { 编号: 2, 名称: '纵向项目', 路径: '' },
      子级菜单: [
        { 菜单: { 编号: 201, 名称: '申报立项', 路径: '/project/vertical-application' }, 子级菜单: [] },
        { 菜单: { 编号: 202, 名称: '纵向导入', 路径: '/project/vertical-import' }, 子级菜单: [] }
      ]
    },
    {
      菜单: { 编号: 4, 名称: '项目经费管理', 路径: '' },
      子级菜单: [
        { 菜单: { 编号: 401, 名称: '经费报销', 路径: '/finance/expense-claims' }, 子级菜单: [] },
        { 菜单: { 编号: 402, 名称: '到款认领', 路径: '/finance/incoming' }, 子级菜单: [] }
      ]
    }
  ]

  it('初始状态下无自定义排序，保持默认节点顺序', () => {
    const store = useMenuOrderStore()
    expect(store.hasCustomOrder).toBe(false)
    const sorted = store.sortPermissionNodes(sampleNodes)
    expect(sorted.map((n) => n.菜单.名称)).toEqual(['科研办公', '纵向项目', '项目经费管理'])
  })

  it('设置一级自定义排序后，能够按指定顺序重排', () => {
    const store = useMenuOrderStore()
    store.saveRootOrder(['项目经费管理', '纵向项目', '科研办公'])

    expect(store.hasCustomOrder).toBe(true)
    const sorted = store.sortPermissionNodes(sampleNodes)
    expect(sorted.map((n) => n.菜单.名称)).toEqual(['项目经费管理', '纵向项目', '科研办公'])
  })

  it('设置二级子菜单自定义排序后，能够按指定顺序重排子级菜单', () => {
    const store = useMenuOrderStore()
    store.saveSubOrder('纵向项目', ['纵向导入', '申报立项'])

    const sorted = store.sortPermissionNodes(sampleNodes)
    const verticalNode = sorted.find((n) => n.菜单.名称 === '纵向项目')
    expect(verticalNode).toBeDefined()
    expect(verticalNode!.子级菜单.map((s) => s.菜单.名称)).toEqual(['纵向导入', '申报立项'])
  })

  it('重置自定义排序后，恢复系统默认顺序并清除 localStorage', () => {
    const store = useMenuOrderStore()
    store.saveRootOrder(['项目经费管理', '纵向项目'])
    expect(store.hasCustomOrder).toBe(true)

    store.resetAllOrders()
    expect(store.hasCustomOrder).toBe(false)
    const sorted = store.sortPermissionNodes(sampleNodes)
    expect(sorted.map((n) => n.菜单.名称)).toEqual(['科研办公', '纵向项目', '项目经费管理'])
  })
})
