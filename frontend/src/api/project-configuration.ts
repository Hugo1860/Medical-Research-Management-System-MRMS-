import { get, post } from '@/utils/request'

export function getProjectClassificationTree() {
  return get<any[]>('/ProjectConfiguration/查询分类树')
}

export function saveProjectClassification(payload: any) {
  return post('/ProjectConfiguration/保存项目分类', payload)
}

export function getBudgetTemplates(projectNature?: string) {
  return get<any[]>('/ProjectConfiguration/查询预算模板', { 项目性质: projectNature })
}

export function saveBudgetTemplate(payload: any) {
  return post('/ProjectConfiguration/保存预算模板', payload)
}

export function getNumberRules() {
  return get<any[]>('/ProjectConfiguration/查询编号规则')
}

export function saveNumberRule(payload: any) {
  return post('/ProjectConfiguration/保存编号规则', payload)
}

export function generateProjectNumber(ruleCode: string) {
  return post('/ProjectConfiguration/生成项目编号', { 规则编码: ruleCode })
}

export function calculateBudgetSplit(payload: { 模板编号: number; 总金额: number }) {
  return post('/ProjectConfiguration/按模板拆分预算', payload)
}
