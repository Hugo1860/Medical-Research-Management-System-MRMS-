import request from '@/utils/request'

export type ProductTier = '基层核心版' | '医院增强版' | '综合科研平台版'
export type StrategyValue = Record<string, unknown>

export interface HospitalModeConfig {
  运营模式: '三级综合' | '二级精简'
  产品层级?: ProductTier
  隐藏菜单: string[]
  启用功能?: Record<string, boolean>
  字段策略?: StrategyValue
  流程策略?: StrategyValue
  统计指标策略?: StrategyValue
  配置版本?: number
  说明?: string
  更新人?: string
  更新时间?: string
}

export const PRODUCT_TIERS: ProductTier[] = ['基层核心版', '医院增强版', '综合科研平台版']

export function parseProductTier(value: unknown, mode: unknown = '三级综合'): ProductTier {
  if (PRODUCT_TIERS.includes(value as ProductTier)) return value as ProductTier
  return mode === '二级精简' ? '基层核心版' : '综合科研平台版'
}

export function parseStrategy(value: unknown, fallback: StrategyValue = {}): StrategyValue {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return fallback
  return value as StrategyValue
}

export function parseFieldStrategy(value: unknown, tier: ProductTier): StrategyValue {
  return parseStrategy(value, { required: ['项目中文名称', '负责人编号'], optional: tier === '基层核心版' ? ['项目分类', '研究类型'] : ['项目英文名称', '学科类型', '研究类型'], hidden: tier === '基层核心版' ? ['项目英文名称', '国际合作信息'] : [] })
}

export function parseWorkflowStrategy(value: unknown, tier: ProductTier): StrategyValue {
  return parseStrategy(value, { enabled: tier === '基层核心版' ? ['立项申请', '成果归档'] : ['立项申请', '伦理审查', '经费执行', '成果归档'], approval: tier === '基层核心版' ? '简化审批' : '分级审批' })
}

export function parseMetricStrategy(value: unknown, tier: ProductTier): StrategyValue {
  return parseStrategy(value, { enabled: tier === '基层核心版' ? ['纵向项目', '科研成果', '科研人员'] : ['纵向项目', '横向项目', '科研成果', '科研人员'], labels: {} })
}

export function getHospitalCoreCapabilities() {
  return request<HospitalModeConfig & { 核心版: boolean; 服务端强制: boolean }>({ url: '/HospitalMode/获取医院核心版能力', method: 'get' })
}
export function getHospitalModeConfig() {
  return request<HospitalModeConfig>({ url: '/HospitalMode/获取医院运营配置', method: 'get' })
}
export function saveHospitalModeConfig(data: HospitalModeConfig) {
  return request({ url: '/HospitalMode/保存医院运营配置', method: 'post', data })
}
