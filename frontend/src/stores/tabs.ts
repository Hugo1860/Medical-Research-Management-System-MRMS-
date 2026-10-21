import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { RouteLocationNormalized } from 'vue-router'

export interface TabItem {
  title: string
  path: string
  name?: string
  query?: Record<string, any>
}

export const useTabsStore = defineStore('tabs', () => {
  const tabs = ref<TabItem[]>([
    { title: '科研工作台', path: '/dashboard', name: 'Dashboard' }
  ])
  const activeTab = ref<string>('/dashboard')

  function addTab(route: RouteLocationNormalized) {
    if (route.name === 'Login' || !route.path) return

    const title = (route.query.title as string) || (route.meta?.title as string) || '新标签'
    const path = route.fullPath

    const exists = tabs.value.find((tab) => tab.path === path)
    if (!exists) {
      tabs.value.push({
        title,
        path,
        name: route.name as string,
        query: route.query
      })
    }
    activeTab.value = path
  }

  function closeTab(targetPath: string) {
    // 保护工作台不被关闭
    if (targetPath === '/dashboard') return

    const index = tabs.value.findIndex((tab) => tab.path === targetPath)
    if (index === -1) return

    tabs.value.splice(index, 1)

    // 如果关闭的是当前激活的 tab，激活前一个
    if (activeTab.value === targetPath) {
      const nextTab = tabs.value[index] || tabs.value[index - 1]
      if (nextTab) {
        activeTab.value = nextTab.path
        return nextTab.path
      }
    }
    return activeTab.value
  }

  function closeOthers(currentPath: string) {
    tabs.value = tabs.value.filter(
      (tab) => tab.path === currentPath || tab.path === '/dashboard'
    )
    activeTab.value = currentPath
  }

  function closeAll() {
    tabs.value = [{ title: '科研工作台', path: '/dashboard', name: 'Dashboard' }]
    activeTab.value = '/dashboard'
  }

  return {
    tabs,
    activeTab,
    addTab,
    closeTab,
    closeOthers,
    closeAll
  }
})
