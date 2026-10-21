import { describe, it, expect, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useUserStore } from '@/stores/user'

describe('User Store (用户与权限状态管理)', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    sessionStorage.clear()
    localStorage.clear()
  })

  it('登录设置 Token 时应自动同步至 sessionStorage 与 localStorage', () => {
    const userStore = useUserStore()
    const testToken = 'mock-bearer-jwt-token-123456'

    userStore.setToken(testToken)
    expect(userStore.token).toBe(testToken)
    expect(sessionStorage.getItem('keycloakAccessToken')).toBe(testToken)
    expect(localStorage.getItem('token')).toBe(testToken)
  })

  it('设置用户信息与权限时应正确保存并持久化', () => {
    const userStore = useUserStore()
    const mockUser = {
      编号: 888,
      工号: 'KY2026',
      姓名: '张教授',
      部门编号: 10,
      部门名称: '人工智能医工交叉研究院'
    }

    const mockPermissions = [
      {
        菜单: {
          编号: 101,
          名称: '纵向项目申报',
          路径: '纵向项目/项目申报立项/纵向项目申报.html'
        },
        子级菜单: []
      }
    ]

    userStore.setUserData({ 人员: mockUser, 权限: mockPermissions })
    expect(userStore.userInfo?.姓名).toBe('张教授')
    expect(userStore.userInfo?.工号).toBe('KY2026')
    expect(userStore.permissions.length).toBe(1)
    expect(userStore.permissions[0].菜单.名称).toBe('纵向项目申报')

    const cached = JSON.parse(sessionStorage.getItem('userInfo') || '{}')
    expect(cached.人员?.姓名).toBe('张教授')
  })

  it('登出时应完全清除 Token、用户信息与权限缓存', async () => {
    const userStore = useUserStore()
    userStore.setToken('test-token')
    userStore.setIdToken('test-id-token')
    userStore.setUserData({
      人员: {
        编号: 1,
        工号: '001',
        姓名: '管理员',
        部门编号: 1,
        部门名称: '科研处'
      },
      权限: []
    })

    await userStore.logout()
    expect(userStore.token).toBe('')
    expect(userStore.userInfo).toBeNull()
    expect(userStore.permissions.length).toBe(0)
    expect(sessionStorage.getItem('keycloakAccessToken')).toBeNull()
    expect(sessionStorage.getItem('keycloakIdToken')).toBeNull()
    expect(sessionStorage.getItem('userInfo')).toBeNull()
    expect(localStorage.getItem('token')).toBeNull()
    expect(sessionStorage.getItem('keycloakForceLogin')).toBe('1')
  })
})
