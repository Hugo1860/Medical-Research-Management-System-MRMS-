import { get, post } from '@/utils/request'

export interface InstrumentSave {
  编号?: number
  平台编号: number
  资产编号: string
  设备名称: string
  型号规格?: string
  生产厂家?: string
  购置日期?: string | null
  原值?: number
  存放地点?: string
  管理员编号?: number
  开放状态: string
  计费方式: string
  内部单价: number
  外部单价: number
  最短预约分钟: number
  最长预约分钟: number
  需培训资质: boolean
  备注?: string
}
export interface Instrument extends InstrumentSave {
  编号: number
  平台名称?: string
  管理员姓名?: string
}
export interface BookingApply {
  仪器编号: number
  开始时间: string
  结束时间: string
  样品数: number
  用途说明: string
}
export interface InstrumentBooking extends BookingApply {
  编号: number
  预约人姓名?: string
  状态: string
  审核人姓名?: string
  审核意见?: string
}
export interface UsageRegister {
  预约编号: number
  实际开始时间: string
  实际结束时间: string
  实际样品数: number
  结算凭证号?: string
  备注?: string
}
export interface AnnualReport {
  编号: number
  年度: number
  状态: string
  统计快照: string | Record<string, unknown>
  年度总结?: string
  提交人姓名?: string
}

export function getInstruments(platformId?: number) {
  return get<Instrument[]>('/PlatformInstrument/查询仪器设备', { 平台编号: platformId })
}
export function saveInstrument(payload: InstrumentSave) {
  return post<{ 编号: number }>('/PlatformInstrument/保存仪器设备', payload)
}
export function getInstrumentBookings(instrumentId: number) {
  return get<InstrumentBooking[]>('/PlatformInstrument/查询仪器预约', { 仪器编号: instrumentId })
}
export function applyInstrumentBooking(payload: BookingApply) {
  return post<{ 编号: number; 状态: string }>('/PlatformInstrument/提交仪器预约', payload)
}
export function auditInstrumentBooking(payload: { 编号: number; 审核结果: '已批准' | '已拒绝'; 审核意见: string }) {
  return post<{ 编号: number; 状态: string }>('/PlatformInstrument/审核仪器预约', payload)
}
export function registerInstrumentUsage(payload: UsageRegister) {
  return post<{ 编号: number; 计费金额: number; 机时小时: number }>('/PlatformInstrument/登记仪器使用', payload)
}
export function getPlatformAnnualReports(platformId: number) {
  return get<AnnualReport[]>('/PlatformInstrument/查询平台年报', { 平台编号: platformId })
}
export function generatePlatformAnnualReport(payload: { 平台编号: number; 年度: number; 年度总结: string }) {
  return post<{ 编号: number; 统计: Record<string, number> }>('/PlatformInstrument/生成平台年报', payload)
}
