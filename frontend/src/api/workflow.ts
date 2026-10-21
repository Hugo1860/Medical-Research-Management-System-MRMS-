import { get, post } from '@/utils/request'
import type { PagingResult } from '@/api/organization'

export interface WorkflowTaskItem {
  流程编号: number
  流程模板编号: number
  流程名称: string
  流程相关项目路径?: string
  项目编号: number
  发起人编号?: number
  发起人工号?: string
  发起人姓名?: string
  发起人部门名称?: string
  状态: number
  状态名称?: string
  流程创建时间: string
  步骤编号: number
  步骤名称: string
  步骤状态: number
  步骤链接名称?: string
  步骤链接路径?: string
  步骤链接样式?: string
  步骤操作人编号?: number
  步骤操作人工号?: string
  步骤操作人姓名?: string
  步骤操作人部门名称?: string
  步骤执行时间?: string
  步骤状态说明?: string
  是否可处理: boolean
  匿名评审?: boolean
}

export interface ExpertConflictResult {
  存在利益冲突?: boolean
  冲突说明?: string
  项目编号?: number
  专家编号?: number
}

export interface WorkflowAuditPayload {
  步骤编号: number
  状态值: number // 1: 审核通过, -1: 审核退回, -2: 终止流程
  备注: string
}

// 分页获取普通科研待办流程
export function getTodoList(paging: { index: number; size: number }) {
  return get<PagingResult<WorkflowTaskItem>>('/Office/获取待办流程', {
    Index: paging.index,
    Size: paging.size
  })
}

// 分页获取专家匿名评审待办
export function getExpertTodoList(paging: { index: number; size: number }) {
  return get<PagingResult<WorkflowTaskItem>>('/Office/获取专家评审待办', {
    Index: paging.index,
    Size: paging.size
  })
}

// 提交审核动作（通过/不通过）
export function auditWorkflowStep(data: WorkflowAuditPayload) {
  return post<void>('/WorkFlow/审核步骤', data)
}

// 终止流程步骤
export function terminateWorkflowStep(data: WorkflowAuditPayload) {
  return post<void>('/WorkFlow/终止步骤', data)
}

// 专家利益冲突预检查
export function checkExpertConflict(stepId: number) {
  return get<ExpertConflictResult>('/WorkFlow/预检查专家利益冲突', {
    步骤编号: stepId
  })
}

// 获取某步骤的可用操作
export function getStepAvailableActions(stepId: number) {
  return get<any[]>('/WorkFlow/获取某步骤的可用操作', {
    步骤编号: stepId
  })
}
