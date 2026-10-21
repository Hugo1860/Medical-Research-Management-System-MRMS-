import request from '@/utils/request'
import { useUserStore } from '@/stores/user'

export interface Paging {
  pageIndex: number
  pageSize: number
}

export interface NoticeFilter {
  like通知名称?: string
  like标题?: string
  like发送人姓名?: string
  like发布人姓名?: string
  通知类型?: string
  是否启用?: boolean
}

export interface DownloadFilter {
  like文件名称?: string
  like文件类型?: string
  like上传人姓名?: string
  是否启用?: boolean
}

export interface NoticeItem {
  编号: number
  通知名称: string
  标题?: string
  通知内容?: string
  内容?: string
  通知类型: string
  通知标识?: string
  发送人编号: number
  发送人工号?: string
  发送人姓名?: string
  发布人姓名?: string
  发送人部门编号?: number
  发送人部门名称?: string
  建立时间: string
  发布时间?: string
  关闭时间?: string
  是否启用: boolean
  是否置顶?: boolean
  浏览次数?: number
  接收人数?: number
  已接收人数?: number
  相关文件路径?: string
  附件路径?: string
  附件名称?: string
  备注?: string
}

export interface DownloadItem {
  编号?: number
  文件名称: string
  文件类型: string
  文件路径: string
  是否启用: boolean
  上传人编号?: number
  上传人姓名?: string
  上传人工号?: string
  上传人部门编号?: number
  上传人部门名称?: string
  建立时间?: string
  备注?: string
}

/**
 * 分页获取通知公告
 */
export function getNotices(paging: Paging, filter: NoticeFilter = {}) {
  return request.get('/Office/分页获取通知公告', {
    params: {
      Index: paging.pageIndex,
      Size: paging.pageSize,
      Like通知名称: filter.like通知名称 || filter.like标题 || undefined,
      Like发送人姓名: filter.like发送人姓名 || filter.like发布人姓名 || undefined,
      通知类型: filter.通知类型 || undefined,
      是否启用: filter.是否启用
    }
  })
}

/**
 * 分页获取某人员可查看的通知公告
 */
export function getUserNotices(人员编号: number, paging: Paging, filter: NoticeFilter = {}) {
  return request.get('/Office/分页获取某人员可查看的通知公告', {
    params: {
      人员编号,
      Index: paging.pageIndex,
      Size: paging.pageSize,
      Like通知名称: filter.like通知名称 || filter.like标题 || undefined,
      Like发送人姓名: filter.like发送人姓名 || filter.like发布人姓名 || undefined,
      通知类型: filter.通知类型 || undefined
    }
  })
}

/**
 * 获取通知公告详情
 */
export function getNoticeDetail(编号: number, 是否已接收: boolean = true) {
  return request.get('/Office/获取通知公告详情', {
    params: { 编号, 是否已接收 }
  })
}

/**
 * 新增/发布通知公告 (构建符合后端 sp_通知公告_增改 的标准数据契约)
 */
export function createNotice(data: Partial<NoticeItem> & { 接收者列表?: Array<{ 接收者类型: number; 接收者编号: number }> }) {
  const userStore = useUserStore()
  const userId = data.发送人编号 || userStore.userInfo?.编号 || 0

  const now = new Date()
  const defaultClose = new Date(now.getFullYear() + 2, 11, 31, 23, 59, 59)
  const closeTimeStr = data.关闭时间 || `${defaultClose.getFullYear()}-12-31 23:59:59`

  const body = {
    基本资料: {
      编号: data.编号 || 0,
      通知类型: data.通知类型 || '公告',
      通知标识: data.通知标识 || '',
      发送人编号: userId,
      通知名称: data.通知名称 || data.标题 || '',
      关闭时间: closeTimeStr,
      相关文件路径: data.相关文件路径 || data.附件路径 || '',
      通知内容: data.通知内容 || data.内容 || '',
      接收人数: data.接收人数 || 0,
      已接收人数: data.已接收人数 || 0,
      是否启用: data.是否启用 !== false,
      备注: data.备注 || ''
    },
    通知公告接收条件列表: data.接收者列表 && data.接收者列表.length > 0
      ? data.接收者列表
      : [{ 接收者类型: 0, 接收者编号: 0 }]
  }

  return request.post('/Office/新增通知公告', body)
}

/**
 * 启禁通知公告
 */
export function toggleNoticeStatus(编号: number, 是否启用: boolean, 发送人编号?: number) {
  const userStore = useUserStore()
  const senderId = 发送人编号 || userStore.userInfo?.编号 || 0
  return request.post('/Office/启禁通知公告', {
    编号,
    是否启用,
    发送人编号: senderId
  })
}

/**
 * 上传通知公告相关文件
 */
export function uploadNoticeFile(file: File) {
  const formData = new FormData()
  formData.append('fileUpload', file)
  return request.post<string[]>('/Office/上传通知公告文件', formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  })
}

/**
 * 分页获取下载中心文件
 */
export function getDownloads(paging: Paging, filter: DownloadFilter = {}) {
  return request.get('/Office/分页获取下载中心', {
    params: {
      Index: paging.pageIndex,
      Size: paging.pageSize,
      Like文件名称: filter.like文件名称 || undefined,
      Like文件类型: filter.like文件类型 || undefined,
      Like上传人姓名: filter.like上传人姓名 || undefined,
      是否启用: filter.是否启用
    }
  })
}

/**
 * 分页获取可查看的启用下载中心文件
 */
export function getEnableDownloads(paging: Paging, filter: DownloadFilter = {}) {
  return request.get('/Office/分页获取可查看的下载中心', {
    params: {
      Index: paging.pageIndex,
      Size: paging.pageSize,
      Like文件名称: filter.like文件名称 || undefined,
      Like文件类型: filter.like文件类型 || undefined,
      Like上传人姓名: filter.like上传人姓名 || undefined
    }
  })
}

/**
 * 增改下载中心文件
 */
export function saveDownload(data: Partial<DownloadItem>) {
  const userStore = useUserStore()
  const userId = data.上传人编号 || userStore.userInfo?.编号 || 0
  return request.post('/Office/增改下载', {
    ...data,
    上传人编号: userId
  })
}

/**
 * 启禁下载中心文件
 */
export function toggleDownloadStatus(data: { 编号: number; 是否启用: boolean; 上传人编号?: number }) {
  const userStore = useUserStore()
  return request.post('/Office/启禁下载', {
    编号: data.编号,
    是否启用: data.是否启用,
    上传人编号: data.上传人编号 || userStore.userInfo?.编号 || 0
  })
}

/**
 * 上传下载文件
 */
export function uploadDownloadFile(file: File) {
  const formData = new FormData()
  formData.append('fileUpload', file)
  return request.post<string[]>('/Office/上传下载文件', formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  })
}

/**
 * 历年产出趋势项
 */
export interface YearTrendItem {
  年度: string
  纵向课题数: number
  纵向经费: number
  横向合同数: number
  横向经费: number
  论文数: number
  专利数: number
  著作数: number
  获奖数: number
}

/**
 * 能力画像量化得分
 */
export interface CapabilityScores {
  人员编号: number
  课题主持得分: number
  经费规模得分: number
  论文能力得分: number
  成果转化得分: number
  团队指导得分: number
  综合科研指数: number
  评级称号: string
}

/**
 * 个人科研全景详情数据
 */
export interface ResearchPortfolioData {
  个人资料: any
  纵向项目总览: any[]
  横向项目总览: any[]
  成果总览: any[]
  科研能力?: any
  能力标签?: any[]
  导师关系?: any[]
  团队?: any[]
  项目分工?: any[]
  能力画像量化?: CapabilityScores
  历年产出趋势?: YearTrendItem[]
}

/**
 * 获取个人科研全景档案与成果资产
 */
export function getResearchPortfolio(当前登录人编号?: number) {
  const userStore = useUserStore()
  const userId = 当前登录人编号 || userStore.userInfo?.编号 || 0
  return request.get<ResearchPortfolioData>('/Office/获取科研详情', {
    params: { 当前登录人编号: userId }
  })
}
