import { get, post } from '@/utils/request'

export interface BiobankSpecimenItem {
  编号: number
  样本编码: string
  样本类型: string
  疾病诊断: string
  解剖部位?: string
  储存温度: string
  储存位置: string
  规格单位: string
  初始数量: number
  在库余量: number
  单价成本: number
  质控等级?: string
  知情同意书编号?: string
  伦理批文号?: string
  状态: '在库' | '部分领用' | '已借空' | '已报损'
  采集日期?: string
  入库日期?: string
  备注?: string
  建立时间?: string
}

export interface BiobankLoanItem {
  编号: number
  样本编号: number
  样本编码: string
  样本类型: string
  疾病诊断: string
  规格单位: string
  项目编号?: number
  项目名称?: string
  申请人编号: number
  申请人姓名: string
  申请用途: string
  申请数量: number
  出库数量: number
  归还数量: number
  消耗数量: number
  单价成本: number
  分摊总金额: number
  伦理批文号?: string
  伦理核验结果: '待核验' | '伦理有效已核准' | '批文过期硬阻断' | '批文缺失硬阻断' | '无需伦理'
  借还状态: '申请中' | '已出库' | '已部分归还' | '已全部归还' | '已消耗核销' | '已驳回'
  申请时间: string
  出库时间?: string
  出库经办人?: string
  归还时间?: string
  归还验收人?: string
  核销说明?: string
}

export interface BiobankStockStats {
  总样本数: number
  在库样本数: number
  正在实验中借出: number
  课题已分摊总成本: number
  类型统计: Array<{
    样本类型: string
    样本总数: number
    在库总余量: number
  }>
  温区统计: Array<{
    储存温度: string
    标本数: number
  }>
}

// 1. 获取生物样本资产列表
export function getBiobankSpecimens(params?: {
  keyword?: string
  sampleType?: string
  status?: string
}) {
  return get<BiobankSpecimenItem[]>('/Biobank/获取生物样本列表', params)
}

// 2. 获取生物样本借还与消耗核销台账
export function getBiobankLoanLedger(params?: {
  status?: string
  keyword?: string
  projectId?: number
}) {
  return get<BiobankLoanItem[]>('/Biobank/获取生物样本借还台账', params)
}

// 3. 申请生物样本出库领用 (前置伦理合规审查与硬阻断)
export function applyBiobankLoan(data: {
  样本编号: number
  项目编号?: number
  申请用途: string
  申请数量: number
}) {
  return post<{
    success: boolean
    record_id: number
    ethics_result: string
    message: string
  }>('/Biobank/申请样本领用', data)
}

// 4. 样本库管理员核准出库
export function approveBiobankLoan(data: {
  台账编号: number
  实际出库数量?: number
  出库经办人?: string
}) {
  return post<{
    success: boolean
    message: string
  }>('/Biobank/核准样本出库', data)
}

// 5. 登记生物样本归还与消耗核销 (课题经费资产核算)
export function returnAndReconcileBiobankLoan(data: {
  台账编号: number
  归还数量: number
  消耗数量: number
  验收人?: string
  核销说明?: string
}) {
  return post<{
    success: boolean
    allocated_cost: number
    message: string
  }>('/Biobank/登记样本归还与消耗', data)
}

// 6. 新增生物样本资产入库
export function addBiobankSpecimen(data: {
  样本编码: string
  样本类型: string
  疾病诊断: string
  解剖部位?: string
  储存温度?: string
  储存位置: string
  规格单位?: string
  初始数量: number
  单价成本?: number
  质控等级?: string
  知情同意书编号?: string
  伦理批文号?: string
  备注?: string
}) {
  return post<{
    success: boolean
    id: number
    message: string
  }>('/Biobank/新增样本资产', data)
}

// 7. 获取生物样本资产与使用统计大屏数据
export function getBiobankStockStats() {
  return get<BiobankStockStats>('/Biobank/获取样本库存统计')
}
