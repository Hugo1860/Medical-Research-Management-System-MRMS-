import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { PermissionNode } from '@/api/auth'

const STORAGE_KEY = 'scientific_research_custom_menu_order'

export interface MenuOrderConfig {
  [parentKey: string]: string[] // parentKey -> list of menu names in custom order
}

export const useMenuOrderStore = defineStore('menuOrder', () => {
  function loadStoredOrder(): MenuOrderConfig {
    try {
      const stored = localStorage.getItem(STORAGE_KEY)
      if (stored) {
        const parsed = JSON.parse(stored)
        if (parsed && typeof parsed === 'object') {
          return parsed
        }
      }
    } catch {
      // ignore
    }
    return {}
  }

  const customOrderMap = ref<MenuOrderConfig>(loadStoredOrder())

  const hasCustomOrder = computed(() => {
    return Object.keys(customOrderMap.value).some(
      (k) => Array.isArray(customOrderMap.value[k]) && customOrderMap.value[k].length > 0
    )
  })

  function saveAllOrders(orders: MenuOrderConfig) {
    customOrderMap.value = { ...orders }
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(customOrderMap.value))
    } catch {
      // ignore
    }
  }

  function saveRootOrder(names: string[]) {
    customOrderMap.value = {
      ...customOrderMap.value,
      _root: names
    }
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(customOrderMap.value))
    } catch {
      // ignore
    }
  }

  function saveSubOrder(parentName: string, names: string[]) {
    customOrderMap.value = {
      ...customOrderMap.value,
      [parentName]: names
    }
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(customOrderMap.value))
    } catch {
      // ignore
    }
  }

  function resetAllOrders() {
    customOrderMap.value = {}
    try {
      localStorage.removeItem(STORAGE_KEY)
    } catch {
      // ignore
    }
  }

  /**
   * 递归对 PermissionNode[] 树按照用户设定的自定义顺序重排
   */
  function sortPermissionNodes(nodes: PermissionNode[], parentKey = '_root'): PermissionNode[] {
    if (!Array.isArray(nodes) || nodes.length <= 1) return nodes

    const orderList = customOrderMap.value[parentKey]
    let sortedNodes = [...nodes]

    if (orderList && Array.isArray(orderList) && orderList.length > 0) {
      const rankMap = new Map<string, number>()
      orderList.forEach((name, idx) => rankMap.set(name, idx))

      sortedNodes.sort((a, b) => {
        const nameA = a.菜单?.名称 || ''
        const nameB = b.菜单?.名称 || ''
        const rankA = rankMap.has(nameA) ? rankMap.get(nameA)! : 9999
        const rankB = rankMap.has(nameB) ? rankMap.get(nameB)! : 9999
        if (rankA !== rankB) return rankA - rankB
        return (a.菜单?.编号 || 0) - (b.菜单?.编号 || 0)
      })
    }

    // 递归对子级菜单按父级名称或上级编号排序
    return sortedNodes.map((node) => {
      if (node.子级菜单 && node.子级菜单.length > 1) {
        const childParentKey = node.菜单?.名称 || String(node.菜单?.编号)
        return {
          ...node,
          子级菜单: sortPermissionNodes(node.子级菜单, childParentKey)
        }
      }
      return node
    })
  }

  return {
    customOrderMap,
    hasCustomOrder,
    saveAllOrders,
    saveRootOrder,
    saveSubOrder,
    resetAllOrders,
    sortPermissionNodes
  }
})
