import { get, post } from '@/utils/request'

export interface EvaluationRule {
  编号?: number
  规则名称: string
  版本号?: number
  适用岗位?: string
  适用学科?: string
  适用研究类型?: string
  评价周期?: string
  生效日期?: string
  失效日期?: string
  状态?: string
  规则说明?: string
  创建人编号?: number
  创建时间?: string
  更新时间?: string
}

export interface EvaluationRuleItem {
  编号?: number
  规则编号: number
  项目代码: string
  项目名称: string
  项目分类?: string
  计分依据?: string
  权重: number
  最高分: number
  证据类型?: string
  排序?: number
  是否启用?: boolean
}

export interface EvaluationDetailItem {
  编号?: number
  评价结果编号?: number
  规则项编号: number
  证据类型?: string
  证据编号?: string
  证据标题?: string
  原始值: number
  权重?: number
  得分?: number
  解释?: string
  核验状态?: string
  项目代码?: string
  项目名称?: string
  计分依据?: string
}

export interface EvaluationResult {
  编号: number
  人员编号: number
  姓名: string
  规则编号: number
  规则名称: string
  版本号: number
  评价年度: number
  周期开始日期: string
  周期结束日期: string
  总分: number
  状态: string
  结果摘要?: string
  复核意见?: string
}

export interface EvaluationAppealItem {
  编号: number
  评价结果编号: number
  发起人编号: number
  申诉理由: string
  期望处理?: string
  补充证据说明?: string
  状态?: string
  处理意见?: string
  建立时间?: string
}

export interface EvaluationRewardItem {
  编号?: number
  评价结果编号: number
  人员编号?: number
  奖励名称: string
  奖励级别?: string
  奖励金额?: number
  状态: string
  发放日期?: string
  发放凭证号?: string
  财务回写状态?: '待回写' | '已回写' | '回写失败'
  财务凭证号?: string
  财务回写说明?: string
  发放批次号?: string
  支付状态?: '待支付' | '已支付' | '支付失败' | '不予支付'
  支付凭证号?: string
  支付失败原因?: string
  支付时间?: string
  备注?: string
}

export interface EvaluationRankingItem {
  排名: number
  评价结果编号?: number
  人员编号: number
  姓名: string
  岗位名称?: string
  学科门类?: string
  评价年度: number
  总分: number
  状态: string
  规则名称: string
  版本号: number
}

export interface ConflictCheckResult {
  命中回避: boolean
  回避原因?: string
  专家姓名?: string
  申报人姓名?: string
  项目名称?: string
}

// 1. 获取科研评价规则列表
export function getEvaluationRules(params?: {
  规则名称?: string
  适用岗位?: string
  适用学科?: string
  状态?: string
}) {
  return get<EvaluationRule[]>('/Evaluation/获取科研评价规则', params)
}

// 2. 获取规则详情（包含规则项清单）
export function getEvaluationRuleDetail(规则编号: number) {
  return get<{
    规则: EvaluationRule
    规则项: EvaluationRuleItem[]
  }>('/Evaluation/获取科研评价规则详情', { 规则编号 })
}

// 3. 保存规则
export function saveEvaluationRule(data: Partial<EvaluationRule>) {
  return post('/Evaluation/保存科研评价规则', data)
}

// 4. 保存规则项
export function saveEvaluationRuleItem(data: Partial<EvaluationRuleItem>) {
  return post('/Evaluation/保存科研评价规则项', data)
}

// 5. 生成科研评价测算
export function generateEvaluation(data: {
  人员编号: number
  规则编号: number
  评价年度?: number
  周期开始日期: string
  周期结束日期: string
  适用岗位?: string
  适用学科?: string
  适用研究类型?: string
  结果摘要?: string
  明细: Array<{
    规则项编号: number
    证据类型?: string
    证据编号?: string
    证据标题?: string
    原始值: number
    解释?: string
    核验状态?: string
  }>
}) {
  return post('/Evaluation/生成科研评价', data)
}

// 6. 查询科研评价结果列表
export function getEvaluationResults(params?: {
  人员编号?: number
  评价年度?: number
  状态?: string
}) {
  return get<EvaluationResult[]>('/Evaluation/查询科研评价结果', params)
}

// 7. 获取单笔科研评价详情（明细得分、申诉记录、奖励记录）
export function getEvaluationDetail(评价结果编号: number) {
  return get<{
    评价结果: EvaluationResult
    明细: EvaluationDetailItem[]
    申诉: EvaluationAppealItem[]
    奖励: EvaluationRewardItem[]
  }>('/Evaluation/获取科研评价详情', { 评价结果编号 })
}

// 8. 复核评价结果
export function reviewEvaluation(data: {
  评价结果编号: number
  状态: string
  复核意见?: string
}) {
  return post('/Evaluation/复核科研评价', data)
}

// 9. 提交评价申诉
export function submitEvaluationAppeal(data: {
  评价结果编号: number
  申诉理由: string
  期望处理?: string
  补充证据说明?: string
}) {
  return post('/Evaluation/提交科研评价申诉', data)
}

// 10. 处理评价申诉
export function handleEvaluationAppeal(data: {
  申诉编号: number
  状态: string
  处理意见?: string
}) {
  return post('/Evaluation/处理科研评价申诉', data)
}

// 11. 登记科研评价奖励
export function registerEvaluationReward(data: Partial<EvaluationRewardItem>) {
  return post('/Evaluation/登记科研评价奖励', data)
}

export function confirmEvaluationRewardSettlement(data: {
  奖励编号: number
  发放凭证号: string
  发放日期?: string
  财务回写状态?: '待回写' | '已回写' | '回写失败'
  财务凭证号?: string
  财务回写说明?: string
  发放批次号?: string
  支付状态?: '待支付' | '已支付' | '支付失败' | '不予支付'
  支付凭证号?: string
  支付失败原因?: string
  支付时间?: string
}) {
  return post('/Evaluation/确认科研评价奖励发放', data)
}

export interface EvaluationAnnualBatch {
  编号: number
  评价年度: number
  批次名称: string
  版本号: number
  状态: '草稿' | '公示中' | '已发布' | '已锁定' | '已结算' | '作废'
  公示开始日期?: string
  公示结束日期?: string
  公示说明?: string
  结果数?: number
  奖励数?: number
  待支付数?: number
}

export function getEvaluationAnnualBatches(评价年度?: number) {
  return get<EvaluationAnnualBatch[]>('/Evaluation/获取科研评价年度批次', 评价年度 ? { 评价年度 } : undefined)
}
export function saveEvaluationAnnualBatch(data: Partial<EvaluationAnnualBatch>) {
  return post<{ 编号: number; 状态: string }>('/Evaluation/保存科研评价年度批次', data)
}
export function startEvaluationAnnualPublication(data: { 批次编号: number; 公示开始日期?: string; 公示结束日期?: string; 公示说明?: string }) {
  return post('/Evaluation/开始公示科研评价年度', data)
}
export function publishEvaluationAnnual(data: { 批次编号: number }) {
  return post('/Evaluation/正式发布科研评价年度', data)
}
export function lockEvaluationAnnual(data: { 批次编号: number }) {
  return post('/Evaluation/锁定科研评价年度', data)
}
export function settleEvaluationAnnual(data: { 批次编号: number }) {
  return post('/Evaluation/结算科研评价年度', data)
}
export function getEvaluationRewardLedger(params?: { 评价年度?: number; 支付状态?: string }) {
  return get<EvaluationRewardItem[]>('/Evaluation/获取年度奖励发放台账', params)
}

export interface EvaluationRewardPaymentBatch {
  编号: number
  批次号: string
  评价年度: number
  人员编号: number
  财务系统代码?: string
  财务请求幂等键: string
  外部批次号?: string
  状态: '待提交' | '已提交财务' | '回写中' | '部分成功' | '已完成' | '已失败' | '已取消'
  对账状态: '未对账' | '对账中' | '一致' | '不一致'
  奖励笔数?: number
  奖励总额?: number
  成功笔数?: number
  失败笔数?: number
  最后错误?: string
}

export function getEvaluationRewardPaymentBatches(params?: { 状态?: string; 评价年度?: number }) {
  return get<EvaluationRewardPaymentBatch[]>('/Evaluation/获取科研评价奖励支付批次', params)
}

export function createEvaluationRewardPaymentBatch(data: {
  批次号: string
  财务系统代码?: string
  财务请求幂等键: string
  奖励编号列表: number[]
}) {
  return post('/Evaluation/创建科研评价奖励支付批次', data)
}

/** 登记财务系统受理凭据；不会在浏览器内伪造外部支付调用。 */
export function submitEvaluationRewardPaymentBatch(data: {
  批次编号: number
  外部批次号: string
  提交幂等键: string
  提交说明?: string
}) {
  return post('/Evaluation/提交科研评价奖励支付批次', data)
}

export function callbackEvaluationRewardPayment(data: {
  批次编号: number
  奖励编号: number
  外部流水号: string
  回写幂等键: string
  状态: '成功' | '失败'
  财务凭证号?: string
  支付凭证号?: string
  发放凭证号?: string
  失败原因?: string
  回写说明?: string
  支付时间?: string
}) {
  return post('/Evaluation/回写科研评价奖励支付', data)
}

export function reconcileEvaluationRewardPaymentBatch(data: {
  批次编号: number
  外部批次号?: string
  对账幂等键: string
  外部奖励笔数: number
  外部成功笔数: number
  外部失败笔数: number
  外部总额: number
  对账说明?: string
}) {
  return post('/Evaluation/对账科研评价奖励支付批次', data)
}

// 12. 获取科研评价排名
export function getEvaluationRankings(params?: {
  评价年度?: number
  规则编号?: number
}) {
  return get<EvaluationRankingItem[]>('/Evaluation/获取科研评价排名', params)
}

// 13. 专家评审前利益冲突预检查
export function preCheckExpertConflict(stepId: number) {
  return get<ConflictCheckResult>('/WorkFlow/预检查专家利益冲突', { 步骤编号: stepId })
}

// 14. 提交专家评审评分与意见
export function submitExpertReview(data: any) {
  return post('/YProject/专家评审纵向项目申报', data)
}

// 15. 科研绩效考核动态公式接口
export interface PerformanceFormulaItem {
  编号?: number
  规则编号: number
  规则名称?: string
  指标代码: string
  指标名称: string
  指标类别: '学术论文' | '纵向课题' | '横向课题' | '成果专利' | '科技奖励'
  基础分值: number
  公式表达式: string
  一作比例: number
  通讯比例: number
  共一比例: number
  参研比例: number
  分区加成配置JSON?: string
  是否启用: boolean
  建立时间?: string
  更新时间?: string
}

export function getPerformanceFormulas(规则编号?: number) {
  return get<PerformanceFormulaItem[]>('/Evaluation/获取科研绩效动态公式列表', { 规则编号 })
}

export function savePerformanceFormula(data: Partial<PerformanceFormulaItem>) {
  return post<{ 编号: number }>('/Evaluation/保存科研绩效动态公式', data)
}

// 16. 执行全员年度科研绩效动态测算
export interface AutoEvaluationResult {
  评价年度: number
  测算人数: number
  生成明细数: number
  人均得分: number
  提示消息: string
}

export function executeAutoEvaluation(data: {
  评价年度: number
  规则编号?: number
  人员编号?: number
}) {
  return post<AutoEvaluationResult>('/Evaluation/执行年度科研绩效自动测算', data)
}

// 17. 自定义科研报表与卫健委国考年报接口
export interface CustomReportTemplate {
  编号: number
  模板代码: string
  模板名称: string
  报表分类: string
  维度配置JSON?: string
  度量配置JSON?: string
  过滤条件JSON?: string
  报表说明?: string
  是否预置: boolean
  建立时间: string
}

export interface CustomReportRow {
  行标识: string
  分组维度: string
  统计年度: number
  学术论文总数: number
  一区Top论文数: number
  最高影响因子: number
  纵向立项数: number
  纵向立项经费: number
  横向合同数: number
  横向合同金额: number
  专利授权数: number
  科研绩效总分: number
  国考科研达标率: number
}

export function getCustomReportTemplates(报表分类?: string) {
  return get<CustomReportTemplate[]>('/Evaluation/获取自定义报表模板列表', { 报表分类 })
}

export function queryCustomReport(data: {
  模板代码: string
  年度?: number
  部门编号?: number
}) {
  return post<CustomReportRow[]>('/Evaluation/执行自定义科研报表查询', data)
}

export function getExportCustomReportUrl(模板代码: string, 年度?: number, 部门编号?: number) {
  const params = new URLSearchParams({ 模板代码 })
  if (年度) params.append('年度', String(年度))
  if (部门编号) params.append('部门编号', String(部门编号))
  return `/Evaluation/导出自定义科研报表?${params.toString()}`
}
