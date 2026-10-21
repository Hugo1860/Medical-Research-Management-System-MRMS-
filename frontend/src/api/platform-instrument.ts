import { get, post } from '@/utils/request'

export function getInstruments(platformId?: number) {
  return get<any[]>('/PlatformInstrument/查询仪器设备', { 平台编号: platformId })
}

export function saveInstrument(payload: any) {
  return post('/PlatformInstrument/保存仪器设备', payload)
}

export function getInstrumentBookings(instrumentId: number) {
  return get<any[]>('/PlatformInstrument/查询仪器预约', { 仪器编号: instrumentId })
}

export function applyInstrumentBooking(payload: any) {
  return post('/PlatformInstrument/提交仪器预约', payload)
}

export function auditInstrumentBooking(payload: any) {
  return post('/PlatformInstrument/审核仪器预约', payload)
}

export function registerInstrumentUsage(payload: any) {
  return post('/PlatformInstrument/登记仪器使用', payload)
}

export function getPlatformAnnualReports(platformId: number) {
  return get<any[]>('/PlatformInstrument/查询平台年报', { 平台编号: platformId })
}

export function generatePlatformAnnualReport(payload: any) {
  return post('/PlatformInstrument/生成平台年报', payload)
}
