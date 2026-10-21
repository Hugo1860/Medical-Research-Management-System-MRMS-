import axios, { type AxiosRequestConfig, type AxiosResponse } from 'axios'
import { ElMessage } from 'element-plus'

// 生成请求唯一追踪 ID
function generateRequestId(): string {
  if (typeof crypto !== 'undefined' && crypto.randomUUID) {
    return crypto.randomUUID().replace(/-/g, '')
  }
  return Math.random().toString(36).substring(2) + Date.now().toString(36)
}

const service = axios.create({
  baseURL: '/',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json'
  }
})

// 请求拦截器
service.interceptors.request.use(
  (config) => {
    // 自动附加 X-Request-ID
    config.headers['X-Request-ID'] = generateRequestId()

    // 自动附加 Keycloak Bearer Token
    const token = sessionStorage.getItem('keycloakAccessToken') || localStorage.getItem('token')
    if (token) {
      config.headers['Authorization'] = `Bearer ${token}`
    }

    return config
  },
  (error) => {
    console.error('Request interceptor error:', error)
    return Promise.reject(error)
  }
)

// 响应拦截器
service.interceptors.response.use(
  (response: AxiosResponse) => {
    // 后端规范：成功响应均由 MyResultFilter 包装为 { data: ... }
    const res = response.data
    if (res && typeof res === 'object' && 'data' in res) {
      return res.data
    }
    return res
  },
  (error) => {
    const status = error.response ? error.response.status : null
    const errorMsg = error.response?.data?.error || error.message || '网络请求失败，请稍后重试'

    if (status === 401) {
      ElMessage.warning('登录已过期或未授权，请重新登录')
      sessionStorage.removeItem('keycloakAccessToken')
      sessionStorage.removeItem('keycloakIdToken')
      sessionStorage.removeItem('userInfo')
      localStorage.removeItem('token')
      sessionStorage.setItem('keycloakForceLogin', '1')
      if (window.location.pathname !== '/login') {
        window.location.href = '/login'
      }
    } else if (status === 403) {
      ElMessage.error(errorMsg || '无权限访问该资源')
    } else {
      ElMessage.error(errorMsg)
    }

    return Promise.reject(error)
  }
)

export default service

export function get<T = any>(url: string, params?: any, config?: AxiosRequestConfig): Promise<T> {
  return service.get(url, { params, ...config }) as unknown as Promise<T>
}

export function post<T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
  return service.post(url, data, config) as unknown as Promise<T>
}

export function upload<T = any>(url: string, file: File, fieldName = 'files', extraData?: Record<string, any>): Promise<T> {
  const formData = new FormData()
  formData.append(fieldName, file)
  if (extraData) {
    Object.entries(extraData).forEach(([k, v]) => formData.append(k, String(v)))
  }
  return service.post(url, formData, {
    headers: {
      'Content-Type': 'multipart/form-data'
    }
  }) as unknown as Promise<T>
}
