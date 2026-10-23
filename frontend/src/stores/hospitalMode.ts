import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { getHospitalModeConfig, saveHospitalModeConfig, type HospitalModeConfig } from '@/api/hospital-mode'

export const DEFAULT_GRASSROOTS_HIDDEN_ROUTES = [
  '/fruit/biobank-ledger',
  '/office/education-statistics',
  '/governance/proxy-reconciliation',
  '/operations/external-sync',
  '/project/vertical-import',
  '/finance/fund-execution'
]

export const useHospitalModeStore = defineStore('hospitalMode', () => {
  const mode = ref<'三级综合' | '二级精简'>('三级综合')
  const hiddenRoutes = ref<string[]>([])
  const note = ref('')
  const loaded = ref(false)

  const isGrassroots = computed(() => mode.value === '二级精简')

  async function loadConfig() {
    try {
      const res: any = await getHospitalModeConfig()
      if (res) {
        mode.value = res.运营模式 || '三级综合'
        hiddenRoutes.value = Array.isArray(res.隐藏菜单) ? res.隐藏菜单 : []
        note.value = res.说明 || ''
      }
    } catch {
      // 容错降级
      mode.value = '三级综合'
      hiddenRoutes.value = []
    } finally {
      loaded.value = true
    }
  }

  async function updateConfig(newConfig: HospitalModeConfig) {
    await saveHospitalModeConfig(newConfig)
    mode.value = newConfig.运营模式
    hiddenRoutes.value = newConfig.隐藏菜单 || []
    note.value = newConfig.说明 || ''
  }

  function isRouteHidden(routePath: string): boolean {
    if (!isGrassroots.value) return false
    if (!routePath) return false
    const cleanPath = routePath.split('?')[0]
    return hiddenRoutes.value.some(h => cleanPath.startsWith(h.split('?')[0]))
  }

  return {
    mode,
    hiddenRoutes,
    note,
    loaded,
    isGrassroots,
    loadConfig,
    updateConfig,
    isRouteHidden
  }
})
