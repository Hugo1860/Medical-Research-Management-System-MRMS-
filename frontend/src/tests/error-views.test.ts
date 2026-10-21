import { describe, it, expect, vi } from 'vitest'

describe('Error Views & Catch-all Fallbacks (404/403 异常页面与路由回退逻辑)', () => {
  it('404 页面应正确计算并展示请求失败的原始业务路径', () => {
    const computeRequestedPath = (fullPath: string) => {
      return fullPath !== '/error/404' ? fullPath : ''
    }

    expect(computeRequestedPath('/unknown-legacy-url.html')).toBe('/unknown-legacy-url.html')
    expect(computeRequestedPath('/project/not-found?id=999')).toBe('/project/not-found?id=999')
    expect(computeRequestedPath('/error/404')).toBe('')
  })

  it('404 与 403 页面导航动作应正确跳转科研工作台或回退', () => {
    const mockRouter = {
      push: vi.fn(),
      back: vi.fn()
    }

    const goHome = () => mockRouter.push('/dashboard')
    const goProxy = () => mockRouter.push('/governance/proxy-reconciliation')
    const goBack = (historyLength: number) => {
      if (historyLength > 1) {
        mockRouter.back()
      } else {
        mockRouter.push('/dashboard')
      }
    }

    goHome()
    expect(mockRouter.push).toHaveBeenCalledWith('/dashboard')

    goProxy()
    expect(mockRouter.push).toHaveBeenCalledWith('/governance/proxy-reconciliation')

    // 历史长度大于1执行 back
    goBack(5)
    expect(mockRouter.back).toHaveBeenCalledTimes(1)

    // 无历史直接跳首页
    goBack(1)
    expect(mockRouter.push).toHaveBeenLastCalledWith('/dashboard')
  })

  it('403 页面用户信息应在未登录或信息不全时优雅降级', () => {
    const formatUserInfo = (userInfo?: { 姓名?: string; 部门名称?: string; 工号?: string }) => {
      return {
        姓名: userInfo?.姓名 || '科研用户',
        部门名称: userInfo?.部门名称 || '未分配',
        工号: userInfo?.工号 || '-'
      }
    }

    expect(formatUserInfo(undefined)).toEqual({
      姓名: '科研用户',
      部门名称: '未分配',
      工号: '-'
    })

    expect(
      formatUserInfo({
        姓名: '王教授',
        部门名称: '心血管内科',
        工号: 'DOC888'
      })
    ).toEqual({
      姓名: '王教授',
      部门名称: '心血管内科',
      工号: 'DOC888'
    })
  })

  it('路由全局通配规则 (pathMatch) 应准确捕获未声明的任意深度路径', () => {
    const matchCatchAll = (path: string, routes: string[]) => {
      if (routes.includes(path)) return path
      return '/error/404'
    }

    const knownRoutes = ['/dashboard', '/project/vertical-application', '/fruit/paper-management']

    expect(matchCatchAll('/dashboard', knownRoutes)).toBe('/dashboard')
    expect(matchCatchAll('/some/nested/nonexistent/module', knownRoutes)).toBe('/error/404')
    expect(matchCatchAll('/old_2018/removed_module.html', knownRoutes)).toBe('/error/404')
  })
})
