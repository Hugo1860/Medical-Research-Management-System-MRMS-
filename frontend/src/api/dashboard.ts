import { get, post } from '@/utils/request'

export interface DashboardComponent {
  编号: number
  角色名称: string
  组件编码: string
  组件标题: string
  组件类型: string
  排序号: number
  配置: Record<string, unknown>
}

export function getDashboardComponents() {
  return get<DashboardComponent[]>('/Dashboard/获取首页组件配置')
}

export function getChaseList(params?: { 状态?: string; 数量?: number }) {
  return get<any[]>('/Dashboard/查询业务催办', params)
}

export function handleChase(payload: { 编号: number; 状态: string; 处理说明?: string }) {
  return post('/Dashboard/处理业务催办', payload)
}
