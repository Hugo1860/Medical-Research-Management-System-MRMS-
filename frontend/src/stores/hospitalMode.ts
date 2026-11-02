import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import {
  getHospitalModeConfig, saveHospitalModeConfig, parseProductTier, parseFieldStrategy, parseWorkflowStrategy, parseMetricStrategy,
  type HospitalModeConfig, type ProductTier, type StrategyValue
} from '@/api/hospital-mode'

export const DEFAULT_GRASSROOTS_HIDDEN_ROUTES = ['/fruit/biobank-ledger', '/office/education-statistics', '/governance/proxy-reconciliation', '/operations/external-sync', '/project/vertical-import', '/finance/fund-execution']

export const useHospitalModeStore = defineStore('hospitalMode', () => {
  const mode = ref<'三级综合' | '二级精简'>('三级综合')
  const productTier = ref<ProductTier>('综合科研平台版')
  const hiddenRoutes = ref<string[]>([])
  const fieldStrategy = ref<StrategyValue>(parseFieldStrategy(null, productTier.value))
  const workflowStrategy = ref<StrategyValue>(parseWorkflowStrategy(null, productTier.value))
  const metricStrategy = ref<StrategyValue>(parseMetricStrategy(null, productTier.value))
  const note = ref('')
  const loaded = ref(false)
  const isGrassroots = computed(() => productTier.value === '基层核心版' || mode.value === '二级精简')

  function applyConfig(res: any) {
    mode.value = res?.运营模式 === '二级精简' ? '二级精简' : '三级综合'
    productTier.value = parseProductTier(res?.产品层级, mode.value)
    hiddenRoutes.value = Array.isArray(res?.隐藏菜单) ? res.隐藏菜单 : []
    fieldStrategy.value = parseFieldStrategy(res?.字段策略, productTier.value)
    workflowStrategy.value = parseWorkflowStrategy(res?.流程策略, productTier.value)
    metricStrategy.value = parseMetricStrategy(res?.统计指标策略, productTier.value)
    note.value = res?.说明 || ''
  }

  async function loadConfig() {
    try { applyConfig(await getHospitalModeConfig()) } catch { applyConfig({}) } finally { loaded.value = true }
  }
  async function updateConfig(newConfig: HospitalModeConfig) {
    await saveHospitalModeConfig(newConfig)
    applyConfig(newConfig)
  }
  function isRouteHidden(routePath: string): boolean {
    if (!isGrassroots.value || !routePath) return false
    const cleanPath = routePath.split('?')[0]
    return hiddenRoutes.value.some(h => cleanPath.startsWith(h.split('?')[0]))
  }
  return { mode, productTier, hiddenRoutes, fieldStrategy, workflowStrategy, metricStrategy, note, loaded, isGrassroots, loadConfig, updateConfig, isRouteHidden }
})
