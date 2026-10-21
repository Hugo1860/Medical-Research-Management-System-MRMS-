import { get, post } from '@/utils/request'

export interface ExpenseLayerItem {
  编号: number
  项目编号: number
  项目名称: string
  科目层级: string
  科目名称: string
  批复预算: number
  已支出金额: number
  冻结金额: number
  可用余额: number
}

export interface PaymentNodeItem {
  编号: number
  合同编号: number
  节点名称: string
  约定比例: number
  约定金额: number
  计划付款日期: string
  是否已付款: boolean
  实际付款金额?: number
  实际付款日期?: string
}

export interface VerticalExpenseSummaryItem {
  编号: number
  项目中文名称: string
  分类名称?: string
  项目级别?: string
  负责人姓名: string
  负责人工号?: string
  部门名称?: string
  批准经费: number
  经费: number // 累计到账
  剩余经费: number
  待审核报销: number
  已审核报销: number
}

export interface HorizontalExpenseSummaryItem {
  编号: number
  合同名称: string
  合同编号: string
  合同金额: number
  项目负责人姓名: string
  项目负责人部门名称?: string
  经费: number
  剩余经费: number
  待审核报销: number
  已审核报销: number
}

export interface ConferenceExpenseSummaryItem {
  编号: number
  会议名称: string
  申请人姓名: string
  部门名称?: string
  报销总金额: number
  报销时间?: string
  状态名称?: string
}

export interface ExpenseRecordItem {
  编号: number
  流程模板编号: number
  项目编号: number
  报销编号: string
  报销金额: number
  报销方式: string
  公务卡号?: string
  公务卡所属人姓名?: string
  报销人编号?: number
  报销人姓名?: string
  报销时间: string
  相关文件路径?: string
  备注?: string
  状态名称?: string
}

export interface ExpenseDetailItem {
  编号?: number
  报销编号?: number
  项目支出类型: string
  项目支出内容: string
  财务科目?: string
  报销金额: number
  备注?: string
}

export interface BudgetSpentDetailItem {
  编号?: number
  项目支出类型: string
  项目支出内容: string
  批复金额?: number
  已报销金额: number
  剩余金额?: number
}

export interface ExpenseSubmitForm {
  基本资料: {
    项目编号: number
    流程模板编号?: number
    报销编号?: string
    报销金额?: number
    报销方式: string
    公务卡号?: string
    公务卡所属人姓名?: string
    报销人编号?: number
    报销时间?: string
    相关文件路径?: string
    备注?: string
  }
  报销详情列表: ExpenseDetailItem[]
  IsHold?: boolean
}

// 获取项目经费分层台账
export function getExpenseLayers(params?: { 流程模板编号?: number; 项目编号?: number }) {
  return get<ExpenseLayerItem[]>('/Expense/获取项目经费分层', params)
}

// 登记项目经费台账
export function saveLedgerEntry(data: {
  项目编号: number
  科目名称: string
  金额: number
  说明: string
}) {
  return post('/Expense/登记项目经费台账', data)
}

// 获取项目合同付款节点
export function getPaymentNodes(流程模板编号: number, 项目编号: number) {
  return get<PaymentNodeItem[]>('/Expense/获取项目合同付款节点', {
    流程模板编号,
    项目编号
  })
}

// 保存合同付款节点
export function savePaymentNode(data: any) {
  return post('/Expense/保存项目合同付款节点', data)
}

// 分页获取纵向项目经费合计报销列表
export function getVerticalExpenses(
  paging: { index: number; size: number },
  filter?: { Like项目中文名称?: string; Like项目名称?: string; Like负责人姓名?: string }
) {
  return get<{ total: number; list: VerticalExpenseSummaryItem[] }>(
    '/Expense/分页获取纵向项目经费合计报销列表',
    { ...paging, ...filter }
  )
}

// 分页获取横向项目经费合计报销列表
export function getHorizontalExpenses(
  paging: { index: number; size: number },
  filter?: { Like合同名称?: string; Like合同编号?: string; Like项目负责人姓名?: string }
) {
  return get<{ total: number; list: HorizontalExpenseSummaryItem[] }>(
    '/Expense/分页获取横向项目经费合计报销列表',
    { ...paging, ...filter }
  )
}

// 分页获取参加会议合计报销列表
export function getConferenceExpenses(
  paging: { index: number; size: number },
  filter?: { Like会议名称?: string; Like申请人姓名?: string }
) {
  return get<{ total: number; list: ConferenceExpenseSummaryItem[] }>(
    '/Expense/分页获取参加会议合计报销列表',
    { ...paging, ...filter }
  )
}

// 获取某纵向项目历次报销记录
export function getProjectVerticalExpenseList(纵向项目立项编号: number) {
  return get<ExpenseRecordItem[]>('/Expense/获取某纵向项目经费报销列表', {
    纵向项目立项编号
  })
}

// 获取某横向项目历次报销记录
export function getProjectHorizontalExpenseList(横向项目编号: number) {
  return get<ExpenseRecordItem[]>('/Expense/获取某横向项目经费报销列表', {
    横向项目编号
  })
}

// 获取某参加会议历次报销记录
export function getProjectConferenceExpenseList(参加会议编号: number) {
  return get<ExpenseRecordItem[]>('/Expense/获取某参加会议报销列表', {
    参加会议编号
  })
}

// 获取某次具体报销详情（支出细项清单）
export function getExpenseDetail(报销编号: number) {
  return get<ExpenseDetailItem[]>('/Expense/获取某报销详情', {
    报销编号
  })
}

// 获取某纵向项目预算科目已报销详情
export function getProjectVerticalBudgetSpent(纵向项目立项编号: number) {
  return get<BudgetSpentDetailItem[]>('/Expense/获取某纵向项目预算已报销详情', {
    纵向项目立项编号
  })
}

// 获取某横向项目预算科目已报销详情
export function getProjectHorizontalBudgetSpent(横向项目编号: number) {
  return get<BudgetSpentDetailItem[]>('/Expense/获取某横向项目预算已报销详情', {
    横向项目编号
  })
}

// 申报纵向项目经费报销
export function addVerticalExpense(data: ExpenseSubmitForm) {
  return post('/Expense/新增纵向项目经费报销', data)
}

// 申报横向项目经费报销
export function addHorizontalExpense(data: ExpenseSubmitForm) {
  return post('/Expense/新增横向项目经费报销', data)
}

// 申报参加会议经费报销
export function addConferenceExpense(data: ExpenseSubmitForm) {
  return post('/Expense/新增参加会议报销', data)
}

// 同步外部财务流水 / 票据凭证
export function syncFinancialFlow(data: {
  外部系统编号: number
  同步批次号: string
  财务单据列表: Array<{
    外部单据号: string
    流程模板编号: number
    项目编号: number
    业务类型: string
    金额: number
    状态?: string
    交易日期?: string
    付款日期?: string
    收款方?: string
    凭证路径?: string
    备注?: string
  }>
}) {
  return post('/Expense/同步财务流水', data)
}

export interface ArrivalBudgetItem {
  预算科目编号: number
  预算科目名称: string
  批复金额: number
  备注?: string
}

export interface SaveArrivalBudgetParams {
  项目编号: number
  到账编号?: number
  到账批次?: string
  预算列表: ArrivalBudgetItem[]
}

export interface HrpSyncParams {
  协同模式: '推送立项与预算' | '拉取到账与支出' | string
  项目编号?: number
}

export interface HrpSyncStatusResult {
  项目列表: any[]
  预算列表: any[]
  到账列表: any[]
  支出列表: any[]
}

// 按到账维护项目预算 (文献清华长庚模式: 批次预算细化与闭环控制)
export function saveArrivalBudget(data: SaveArrivalBudgetParams) {
  return post('/Expense/按到账维护项目预算', data)
}

// 获取按到账项目预算列表
export function getArrivalBudgetList(projectId: number, arrivalId?: number) {
  return get<any[]>('/Expense/获取按到账项目预算列表', {
    projectId,
    arrivalId
  })
}

// HRP 中间库双向协同 (文献四大中间表协同)
export function syncHrpIntermediate(data: HrpSyncParams) {
  return post('/Expense/Hrp中间库双向协同', data)
}

// 获取 HRP 中间库同步状态
export function getHrpSyncStatus(projectId?: number) {
  return get<HrpSyncStatusResult>('/Expense/获取Hrp中间库同步状态', {
    projectId
  })
}

export interface FinanceInvoiceItem {
  编号: number
  流程模板编号: number
  项目编号: number
  报销编号?: number
  发票号码: string
  发票代码?: string
  开票日期?: string
  金额: number
  供应商名称: string
  供应商税号?: string
  发票类型?: string
  查重状态: '未查重' | '正常' | '疑似重复' | '已确认重复' | '已作废'
  重复说明?: string
  附件路径?: string
  OCR状态: '未接入' | '人工录入' | '待接入' | '已识别'
}

export interface FinanceBudgetAdjustmentItem {
  编号: number
  流程模板编号: number
  项目编号: number
  申请原因: string
  原预算快照: unknown[]
  申请预算: unknown[]
  状态: '草稿' | '待审批' | '已批准' | '已驳回' | '已撤回'
  申请人编号: number
  申请人姓名?: string
  审批人编号?: number
  审批人姓名?: string
  申请时间: string
  审批时间?: string
  审批意见?: string
}

export interface FinanceBudgetVersionItem {
  编号: number
  流程模板编号: number
  项目编号: number
  版本号: number
  来源申请编号?: number
  版本状态: '现行' | '历史' | '待审批'
  预算快照: unknown[]
  变更说明?: string
  建立时间: string
}

export interface FinanceYearSettlementItem {
  编号: number
  流程模板编号: number
  项目编号: number
  结算年度: number
  状态: '开放' | '结算中' | '已封账' | '已重开'
  批准预算: number
  到账金额: number
  支出金额: number
  付款金额: number
  结算说明?: string
  封账时间?: string
  重开时间?: string
}

export function checkFinanceInvoice(params: {
  流程模板编号: number
  项目编号: number
  发票号码: string
  供应商名称: string
  金额: number
  开票日期?: string
  排除编号?: number
}) {
  return get<FinanceInvoiceItem[]>('/Expense/获取科研经费发票查重', params)
}

export function registerFinanceInvoice(data: {
  流程模板编号: number
  项目编号: number
  报销编号?: number
  发票号码: string
  发票代码?: string
  开票日期?: string
  金额: number
  供应商名称: string
  供应商税号?: string
  发票类型?: string
  附件路径?: string
  允许重复?: boolean
}) {
  return post<{ 编号: number; 查重状态: string; OCR状态: string; 重复数量: number }>('/Expense/登记科研经费发票', data)
}

export function reviewFinanceInvoice(data: { 编号: number; 查重状态: FinanceInvoiceItem['查重状态']; 重复说明?: string }) {
  return post('/Expense/复核科研经费发票', data)
}

export function getBudgetAdjustments(params?: { 流程模板编号?: number; 项目编号?: number }) {
  return get<{ 申请列表: FinanceBudgetAdjustmentItem[]; 版本列表: FinanceBudgetVersionItem[] }>('/Expense/获取项目经费预算调整', params)
}

export function submitBudgetAdjustment(data: {
  流程模板编号: number
  项目编号: number
  申请原因: string
  申请预算: Array<{ 项目支出类型: string; 项目支出内容: string; 财务科目: string; 批准经费: number; 配套经费?: number; 编制依据?: string }>
}) {
  return post<{ 申请编号: number; 状态: string }>('/Expense/提交项目经费预算调整', data)
}

export function approveBudgetAdjustment(data: { 申请编号: number; 是否批准: boolean; 审批意见?: string }) {
  return post('/Expense/审批项目经费预算调整', data)
}

export function getYearSettlements(流程模板编号: number, 项目编号: number, 结算年度?: number) {
  return get<FinanceYearSettlementItem[]>('/Expense/获取项目经费年度结算', { 流程模板编号, 项目编号, 结算年度 })
}

export function closeYearSettlement(data: { 流程模板编号: number; 项目编号: number; 结算年度: number; 结算说明?: string }) {
  return post('/Expense/关闭项目经费年度', data)
}

export function reopenYearSettlement(data: { 流程模板编号: number; 项目编号: number; 结算年度: number; 结算说明?: string }) {
  return post('/Expense/重开项目经费年度', data)
}

export function writebackPayment(data: { 台账编号: number; 状态: string; 付款日期?: string; 凭证路径?: string; 备注?: string }) {
  return post('/Expense/回写项目付款状态', data)
}

