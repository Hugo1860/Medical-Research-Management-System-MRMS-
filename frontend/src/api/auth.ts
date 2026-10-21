import { get, post } from '@/utils/request'
import type { AxiosRequestConfig } from 'axios'

export interface OidcConfigResponse {
  enabled: boolean
  authority: string
  clientId: string
  redirectUri: string
}

export interface UserInfo {
  编号: number
  工号: string
  姓名: string
  部门编号: number
  部门名称: string
  角色名称?: string
  DbKey?: string
}

export interface MenuItem {
  编号: number
  上级编号?: number
  名称: string
  路径: string
  类型?: boolean
  图标?: string
}

export interface PermissionNode {
  菜单: MenuItem
  子级菜单: PermissionNode[]
}

export interface LoginResult {
  人员: UserInfo
  权限: PermissionNode[]
}

// 获取 OIDC 配置
export function getOidcConfig() {
  return get<OidcConfigResponse>('/Access/OidcConfig')
}

// 本地登录 (Keycloak 未启用时)
export function login(data: { 工号: string; 密码: string; DbKey?: string }) {
  return post<LoginResult>('/Access/Login', {
    ...data,
    DbKey: data.DbKey || 'ScientificResearch_Model'
  })
}

// 获取当前登录用户及权限树 (支持 Bearer 令牌)
export function getMe() {
  return get<LoginResult>('/Access/Me')
}

// 退出登录
export function logout(config?: AxiosRequestConfig) {
  return post<void>('/Access/Logout', undefined, config)
}
