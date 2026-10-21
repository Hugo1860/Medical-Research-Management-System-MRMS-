import { get, post } from '@/utils/request'
import type { PagingResult } from '@/api/organization'

export interface RoleItem {
  编号: number
  名称: string
  备注: string
  人数?: number | null
  功能数?: number | null
}

export interface RoleMemberItem {
  编号: number
  工号: string
  姓名: string
  部门编号?: number
  部门名称?: string
  手机?: string
  邮箱?: string
  是否启用: boolean
}

export interface RolePermissionItem {
  编号: number
  上级编号: number
  名称: string
  路径?: string
  类型: boolean
  图标?: string
  备注?: string
  拥有?: boolean | null
}

export interface FieldPermissionItem {
  编号?: number
  角色编号: number
  角色名称?: string
  资源: string
  字段: string
  是否可读: boolean
  是否可写: boolean
  建立人编号?: number
  更新时间?: string
}

export interface LoginLogItem {
  编号: number
  工号: string
  姓名: string
  登录时间: string
  IP: string
}

export interface LoginLogFilter {
  工号?: string
  Like姓名?: string
  Begin登录时间?: string
  End登录时间?: string
}

// 获取角色列表
export function getRoleList() {
  return get<RoleItem[]>('/Permission/获取角色列表')
}

// 新增角色
export function createRole(data: { 编号?: number; 名称: string; 备注?: string }) {
  return post<void>('/Permission/新增角色', data)
}

// 修改角色
export function updateRole(data: { 编号: number; 名称: string; 备注?: string }) {
  return post<void>('/Permission/修改角色', data)
}

// 分页获取某角色中的人员
export function getPagedRoleMembers(
  roleId: number,
  paging: { index: number; size: number },
  filter?: { Like姓名?: string; 工号?: string }
) {
  return get<PagingResult<RoleMemberItem>>('/Permission/分页获取角色中的人员', {
    角色编号: roleId,
    Index: paging.index,
    Size: paging.size,
    ...filter
  })
}

// 分页获取未在某角色中的人员（备选）
export function getPagedAvailableUsers(
  roleId: number,
  paging: { index: number; size: number },
  filter?: { Like姓名?: string; 工号?: string }
) {
  return get<PagingResult<RoleMemberItem>>('/Permission/分页获取角色中没有的人员', {
    角色编号: roleId,
    Index: paging.index,
    Size: paging.size,
    ...filter
  })
}

// 增删角色中的人员
export function modifyRoleMembers(data: {
  角色编号: number
  将增加的人员编号列表: number[]
  将删除的人员编号列表: number[]
}) {
  return post<void>('/Permission/增删角色人员', data)
}

// 获取某角色的权限树
export function getRolePermissions(roleId: number) {
  return get<RolePermissionItem[]>('/Permission/获取角色的权限', { 角色编号: roleId })
}

// 保存角色的权限
export function saveRolePermissions(data: { 角色编号: number; 权限编号列表: number[] }) {
  return post<void>('/Permission/保存角色的权限', data)
}

// 获取字段授权规则
export function getFieldPermissions(roleId?: number, resource?: string) {
  return get<FieldPermissionItem[]>('/Permission/获取字段授权规则', {
    角色编号: roleId,
    资源: resource
  })
}

// 保存字段授权规则
export function saveFieldPermission(data: {
  角色编号: number
  资源: string
  字段: string
  是否可读: boolean
  是否可写: boolean
}) {
  return post<{ 角色编号: number; 资源: string; 字段: string; 通知人数: number }>('/Permission/保存字段授权规则', data)
}

// 分页获取系统登录日志
export function getLoginLogs(
  paging: { index: number; size: number },
  filter?: LoginLogFilter
) {
  return get<PagingResult<LoginLogItem>>('/Access/获取登录日志', {
    Index: paging.index,
    Size: paging.size,
    ...filter
  })
}
