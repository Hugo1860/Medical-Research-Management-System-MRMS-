import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getMe, getOidcConfig, login as apiLogin, logout as apiLogout, type UserInfo, type PermissionNode } from '@/api/auth'

export const useUserStore = defineStore('user', () => {
  const token = ref<string>(sessionStorage.getItem('keycloakAccessToken') || localStorage.getItem('token') || '')
  const idToken = ref<string>(sessionStorage.getItem('keycloakIdToken') || '')
  function getStoredUserInfo(): UserInfo | null {
    try {
      const stored = sessionStorage.getItem('userInfo')
      if (stored) {
        const parsed = JSON.parse(stored)
        return parsed.人员 || parsed
      }
    } catch {
      // ignore
    }
    return null
  }

  function getStoredPermissions(): PermissionNode[] {
    try {
      const stored = sessionStorage.getItem('userInfo')
      if (stored) {
        const parsed = JSON.parse(stored)
        return parsed.权限 || []
      }
    } catch {
      // ignore
    }
    return []
  }

  const userInfo = ref<UserInfo | null>(getStoredUserInfo())
  const permissions = ref<PermissionNode[]>(getStoredPermissions())

  function setToken(val: string) {
    token.value = val
    sessionStorage.setItem('keycloakAccessToken', val)
    localStorage.setItem('token', val)
  }

  function setIdToken(val: string) {
    idToken.value = val
    if (val) {
      sessionStorage.setItem('keycloakIdToken', val)
    } else {
      sessionStorage.removeItem('keycloakIdToken')
    }
  }

  async function buildKeycloakLogoutUrl(): Promise<string | null> {
    const accessToken = token.value
    const currentIdToken = idToken.value || sessionStorage.getItem('keycloakIdToken') || ''
    // Local emergency sessions do not have a Keycloak browser session to terminate.
    if (!currentIdToken && (!accessToken || accessToken === 'session-auth-token')) return null

    try {
      const config = await getOidcConfig()
      if (!config?.enabled || !config.authority || !config.clientId) return null
      const authority = config.authority.replace(/\/+$/, '')
      const discoveryRes = await fetch(`${authority}/.well-known/openid-configuration`, {
        headers: { Accept: 'application/json' }
      })
      if (!discoveryRes.ok) return null
      const metadata = await discoveryRes.json() as { end_session_endpoint?: string }
      const endpoint = metadata.end_session_endpoint || `${authority}/protocol/openid-connect/logout`
      const params = new URLSearchParams()
      if (currentIdToken) params.set('id_token_hint', currentIdToken)
      params.set('client_id', config.clientId)
      params.set('post_logout_redirect_uri', config.redirectUri || `${window.location.origin}/login`)
      return `${endpoint}?${params.toString()}`
    } catch {
      return null
    }
  }

  function setUserData(data: { 人员: UserInfo; 权限: PermissionNode[] }) {
    userInfo.value = data.人员
    permissions.value = data.权限 || []
    sessionStorage.setItem('userInfo', JSON.stringify(data))
  }

  async function fetchUserInfo() {
    try {
      const data = await getMe()
      if (data && data.人员) {
        setUserData(data)
        return data
      }
    } catch (e) {
      console.error('Failed to fetch user profile:', e)
      throw e
    }
  }

  async function login(params: { 工号: string; 密码: string; DbKey?: string }) {
    const res = await apiLogin(params)
    if (res && res.人员) {
      setUserData(res)
      // 若后端设置了 Session，标记一个临时 token 保证前端路由守卫通过
      if (!token.value) {
        setToken('session-auth-token')
      }
      return res
    }
    throw new Error('登录未返回用户信息')
  }

  async function logout(): Promise<string | null> {
    const keycloakLogoutUrl = await buildKeycloakLogoutUrl()
    try {
      // Logout must never block local credential cleanup when the remote
      // identity service is unavailable. The short bound keeps the UI
      // responsive while still notifying the server during normal operation.
      await apiLogout({ timeout: 1500 })
    } catch {
      // ignore
    } finally {
      token.value = ''
      userInfo.value = null
      permissions.value = []
      sessionStorage.removeItem('keycloakAccessToken')
      sessionStorage.removeItem('keycloakIdToken')
      sessionStorage.removeItem('userInfo')
      localStorage.removeItem('token')
      idToken.value = ''
      // The next authorization request must not silently reuse the Keycloak SSO session.
      sessionStorage.setItem('keycloakForceLogin', '1')
    }
    return keycloakLogoutUrl
  }

  return {
    token,
    userInfo,
    permissions,
    setToken,
    setIdToken,
    setUserData,
    fetchUserInfo,
    login,
    logout,
    buildKeycloakLogoutUrl
  }
})
