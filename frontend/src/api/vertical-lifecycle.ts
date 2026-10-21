import { get, post } from '@/utils/request'
import type { PagingResult } from '@/api/organization'

export interface VerticalProjectApprovalItem {
  编号: number
  项目名称: string
  学科门类?: string
  项目级别?: string
  批准经费: number
  负责人姓名?: string
  负责人部门名称?: string
  立项文号?: string
  计划开始日期?: string
  计划完成日期?: string
  状态?: number
  状态名称?: string
}

export interface VerticalMidtermItem {
  编号: number
  纵向项目编号: number
  项目名称: string
  负责人姓名?: string
  中检时间?: string
  中检说明?: string
  中检结论?: string
  状态?: number
  状态名称?: string
}

export interface VerticalChangeItem {
  编号: number
  纵向项目编号: number
  项目名称: string
  负责人姓名?: string
  变更类型?: string
  变更事由: string
  变更时间?: string
  状态?: number
  状态名称?: string
}

export interface VerticalCloseoutItem {
  编号: number
  纵向项目编号: number
  项目名称: string
  负责人姓名?: string
  结题时间?: string
  验收结果?: string
  结题说明?: string
  状态?: number
  状态名称?: string
}

export interface LifecyclePrecheckResult {
  通过: boolean
  阶段: string
  项目编号: number
  检查项: Array<{
    代码: string
    名称: string
    通过: boolean
    提示: string
    阻断提交?: boolean
  }>
}

export interface LifecycleArchiveData {
  基本信息?: any
  立项信息?: any
  中检记录?: any[]
  变更历史?: any[]
  结题档案?: any
  到账与执行?: any
}

// 分页获取纵向项目立项列表
export function getPagedApprovals(
  paging: { index: number; size: number },
  filter?: { Like项目名称?: string; Like负责人姓名?: string }
) {
  return get<PagingResult<VerticalProjectApprovalItem>>('/YProject/分页获取科研项目立项', {
    Index: paging.index,
    Size: paging.size,
    ...filter
  })
}

// 获取立项详情
export function getApprovalDetail(approvalId: number) {
  return get<any>('/YProject/获取纵向项目立项详情', { 纵向项目立项编号: approvalId })
}

// 分页获取在研项目中检列表
export function getPagedMidterms(
  paging: { index: number; size: number },
  filter?: { Like项目名称?: string; Like负责人姓名?: string }
) {
  return get<PagingResult<VerticalMidtermItem>>('/YProject/分页获取纵向项目中检', {
    Index: paging.index,
    Size: paging.size,
    ...filter
  })
}

// 预检查项目中检
export function precheckMidterm(projectId: number) {
  return get<LifecyclePrecheckResult>('/YProject/预检查纵向项目中检', { 纵向项目立项编号: projectId })
}

// 新增项目中检
export function createMidterm(data: { 纵向项目立项编号: number; 中检说明?: string }) {
  return post<void>('/YProject/新增纵向项目中检', data)
}

// 分页获取项目变更列表
export function getPagedChanges(
  paging: { index: number; size: number },
  filter?: { Like项目名称?: string; Like负责人姓名?: string }
) {
  return get<PagingResult<VerticalChangeItem>>('/YProject/分页获取纵向项目变更', {
    Index: paging.index,
    Size: paging.size,
    ...filter
  })
}

// 预检查项目变更
export function precheckChange(projectId: number) {
  return get<LifecyclePrecheckResult>('/YProject/预检查纵向项目变更', { 纵向项目立项编号: projectId })
}

// 申请延期、暂停、终止生命周期变更
export function applyProjectLifecycleChange(data: {
  纵向项目立项编号: number
  申请类型: '延期' | '暂停' | '终止'
  事由: string
  延期至日期?: string
}) {
  return post<any>('/YProject/申请纵向项目延期暂停终止', data)
}

// 分页获取项目结题列表
export function getPagedCloseouts(
  paging: { index: number; size: number },
  filter?: { Like项目名称?: string; Like负责人姓名?: string }
) {
  return get<PagingResult<VerticalCloseoutItem>>('/YProject/分页获取纵向项目结题', {
    Index: paging.index,
    Size: paging.size,
    ...filter
  })
}

// 预检查项目结题
export function precheckCloseout(projectId: number) {
  return get<LifecyclePrecheckResult>('/YProject/预检查纵向项目结题', { 纵向项目立项编号: projectId })
}

// 发起项目结题
export function createCloseout(data: { 纵向项目立项编号: number; 结题说明?: string }) {
  return post<void>('/YProject/新增纵向项目结题', data)
}

// 获取项目全生命周期档案全景
export function getProjectLifecycleArchive(projectId: number) {
  return get<LifecycleArchiveData>('/YProject/获取纵向项目生命周期档案', { 纵向项目立项编号: projectId })
}
