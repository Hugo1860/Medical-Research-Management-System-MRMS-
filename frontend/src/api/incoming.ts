import request from '@/utils/request'

export interface Paging {
  pageIndex: number
  pageSize: number
}

export interface IncomingFilter {
  like项目名称?: string
  like项目中文名称?: string
  like合同编号?: string
  like合同名称?: string
}

export interface VerticalIncomingSummaryItem {
  项目编号: number
  项目名称: string
  立项编号?: string
  批准经费: number
  配套经费: number
  到账总金额: number
  未到账金额?: number
  负责人姓名?: string
}

export interface HorizontalIncomingSummaryItem {
  项目编号: number
  合同编号: string
  合同名称: string
  合同金额: number
  到账总金额: number
  未到账金额?: number
  负责人姓名?: string
}

export interface IncomingRecordItem {
  编号: number
  项目编号: number
  拨款单位: string
  到账日期: string
  到账金额: number
  到账单位?: string
  凭证号?: string
  到账凭证路径?: string
  备注?: string
}

/**
 * 分页获取纵向项目经费合计到账列表
 */
export function getVerticalIncomingList(paging: Paging, filter: IncomingFilter = {}) {
  return request.get('/Incoming/分页获取纵向项目经费合计到账列表', {
    params: {
      'paging.pageIndex': paging.pageIndex,
      'paging.pageSize': paging.pageSize,
      'filter.Like项目中文名称': filter.like项目中文名称 || filter.like项目名称
    }
  })
}

/**
 * 分页获取横向项目经费合计到账列表
 */
export function getHorizontalIncomingList(paging: Paging, filter: IncomingFilter = {}) {
  return request.get('/Incoming/分页获取横向项目经费合计到账列表', {
    params: {
      'paging.pageIndex': paging.pageIndex,
      'paging.pageSize': paging.pageSize,
      'filter.Like合同编号': filter.like合同编号,
      'filter.Like合同名称': filter.like合同名称
    }
  })
}

/**
 * 获取某纵向项目历次到账明细
 */
export function getVerticalIncomingRecords(纵向项目立项编号: number) {
  return request.get('/Incoming/获取某纵向项目经费到账列表', {
    params: { 纵向项目立项编号 }
  })
}

/**
 * 获取某横向项目历次到账明细
 */
export function getHorizontalIncomingRecords(横向项目编号: number) {
  return request.get('/Incoming/获取某横向项目经费到账列表', {
    params: { 横向项目编号 }
  })
}

/**
 * 新增纵向项目经费到账登记
 */
export function addVerticalIncoming(data: {
  基本资料: Partial<IncomingRecordItem>
  isHold?: boolean
}) {
  return request.post('/Incoming/新增纵向项目经费到账', data)
}

/**
 * 新增横向项目经费到账认领
 */
export function addHorizontalIncoming(data: {
  基本资料: Partial<IncomingRecordItem>
  isHold?: boolean
}) {
  return request.post('/Incoming/新增横向项目经费到账', data)
}

export function getIncomingFeeSchemes(含停用 = false) {
  return request.get('/Incoming/获取到账费用提取方案', { params: { 含停用 } })
}

export function saveIncomingFeeScheme(data: any) {
  return request.post('/Incoming/保存到账费用提取方案', data)
}

export function previewIncomingFeeExtraction(data: { 方案编号: number; 到款金额: number }) {
  return request.post('/Incoming/预览到账费用提取', data)
}

export function registerFinanceIncoming(data: any) {
  return request.post('/Incoming/登记财务到款', data)
}

export function confirmFinanceIncoming(到款登记编号: number) {
  return request.post('/Incoming/确认财务到款', null, { params: { 到款登记编号 } })
}

export function getPendingFinanceIncoming(状态?: string) {
  return request.get('/Incoming/获取待认领财务到款', { params: { 状态 } })
}

export function claimFinanceIncoming(data: any) {
  return request.post('/Incoming/认领财务到款', data)
}

export function batchClaimFinanceIncoming(data: any) {
  return request.post('/Incoming/批量认领财务到款', data)
}

export function getProjectFinanceCards(params: { 流程模板编号?: number; 项目编号?: number } = {}) {
  return request.get('/Incoming/获取项目经费卡', { params })
}

export function splitProjectFinanceCard(data: { 父卡编号: number; 拆分金额: number; 卡片名称?: string }) {
  return request.post('/Incoming/拆分项目经费卡', data)
}
