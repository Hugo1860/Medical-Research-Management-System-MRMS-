import { defineStore } from 'pinia'
import { ref } from 'vue'

export interface FavoriteMenuItem {
  name: string
  path: string
  iconName?: string
  category?: string
}

const STORAGE_KEY = 'scientific_research_favorite_menus'

const DEFAULT_PRESET_FAVORITES: FavoriteMenuItem[] = [
  { name: '科研待办流转审核', path: '/workflow/todo-approvals', iconName: 'Files', category: '办公审核' },
  { name: '纵向课题全生命周期治理', path: '/project/vertical-lifecycle', iconName: 'Files', category: '纵向课题' },
  { name: '纵向项目申报向导', path: '/project/vertical-application', iconName: 'TrendCharts', category: '项目申报' },
  { name: '经费报销与票据核销台账', path: '/finance/expense-claims', iconName: 'Money', category: '财务核算' },
  { name: '论文成果主动网推认领', path: '/fruit/paper-claim', iconName: 'Reading', category: '学术成果' },
  { name: '临床生物样本库资产台账', path: '/fruit/biobank-ledger', iconName: 'Reading', category: '临床生态' },
  { name: '科研绩效评价与奖励兑现', path: '/evaluation/performance-evaluation', iconName: 'Medal', category: '绩效考核' },
  { name: '移动科研协同工作台', path: '/office/mobile-workbench', iconName: 'House', category: '移动协同' },
  { name: '科研统计决策分析大屏', path: '/analytics/statistics', iconName: 'Histogram', category: '决策大屏' }
]

export const useFavoritesStore = defineStore('favorites', () => {
  function loadInitial(): FavoriteMenuItem[] {
    try {
      const stored = localStorage.getItem(STORAGE_KEY)
      if (stored) {
        const parsed = JSON.parse(stored)
        if (Array.isArray(parsed) && parsed.length > 0) {
          return parsed
        }
      }
    } catch {
      // ignore
    }
    return DEFAULT_PRESET_FAVORITES
  }

  const favorites = ref<FavoriteMenuItem[]>(loadInitial())

  function save() {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(favorites.value))
    } catch {
      // ignore
    }
  }

  function isFavorite(path: string): boolean {
    return favorites.value.some((f) => f.path === path)
  }

  function toggleFavorite(item: { name: string; path: string; iconName?: string; category?: string }) {
    const idx = favorites.value.findIndex((f) => f.path === item.path)
    if (idx >= 0) {
      favorites.value.splice(idx, 1)
    } else {
      favorites.value.push({
        name: item.name,
        path: item.path,
        iconName: item.iconName || 'Document',
        category: item.category || '快捷功能'
      })
    }
    save()
  }

  function removeFavorite(path: string) {
    const idx = favorites.value.findIndex((f) => f.path === path)
    if (idx >= 0) {
      favorites.value.splice(idx, 1)
      save()
    }
  }

  function resetToDefault() {
    favorites.value = [...DEFAULT_PRESET_FAVORITES]
    save()
  }

  return {
    favorites,
    isFavorite,
    toggleFavorite,
    removeFavorite,
    resetToDefault
  }
})
