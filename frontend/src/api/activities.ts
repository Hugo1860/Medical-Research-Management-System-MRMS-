import request from '@/utils/request'

export interface Paging {
  pageIndex: number
  pageSize: number
}

export interface LectureFilter {
  编号?: number
  主办人编号?: number
  like主办人姓名?: string
  like活动名称?: string
  是否反馈过?: boolean
}

export interface LectureFeedbackFilter {
  编号?: number
  like活动名称?: string
  like主办人姓名?: string
  like专家姓名?: string
  讲座编号?: number
}

export interface ConferenceFilter {
  编号?: number
  申请人编号?: number
  like申请人姓名?: string
  like会议名称?: string
  是否反馈过?: boolean
}

export interface ConferenceFeedbackFilter {
  编号?: number
  参加会议编号?: number
  like申请人姓名?: string
  like会议名称?: string
}

export interface LectureItem {
  编号: number
  活动名称: string
  所属部门编号?: number
  所属部门名称?: string
  活动经费?: number
  会议类型?: string
  活动对象?: string
  开始时间: string
  结束时间: string
  主办人编号: number
  主办人姓名?: string
  主办人工号?: string
  主办人部门名称?: string
  主讲人姓名: string
  主讲人国别?: string
  主讲人单位?: string
  主讲人学位?: string
  主讲人职称?: string
  主讲人简历路径?: string
  讲座地点: string
  内容概述?: string
  是否反馈过?: boolean
}

export interface LectureFeedbackItem {
  编号?: number
  讲座编号: number
  活动名称?: string
  主办人姓名?: string
  主讲人姓名?: string
  专家姓名: string
  专家身份证号码?: string
  专家护照号?: string
  专家联系方式?: string
  专家讲课费金额?: number
  结算方式?: string
  活动持续分钟: number
  参会人数: number
  讲座附件路径?: string
  讲座效果?: string
}

export interface ConferenceItem {
  编号: number
  会议名称: string
  会议主办单位: string
  会议承办单位?: string
  研讨领域?: string
  学科类型?: string
  会议地址: string
  会议开始时间: string
  会议结束时间: string
  往返时间?: number
  会议网址?: string
  会议简介及申请理由?: string
  申请人编号: number
  申请人姓名?: string
  申请人工号?: string
  申请人部门名称?: string
  参会相关文件路径?: string
  是否反馈过?: boolean
}

export interface ConferenceFeedbackItem {
  编号?: number
  参加会议编号: number
  会议名称?: string
  申请人姓名?: string
  活动持续分钟?: number
  参会人数?: number
  参会总结: string
  会议反馈文件路径?: string
}

/**
 * 分页获取主办讲座
 */
export function getLectures(paging: Paging, filter: LectureFilter = {}) {
  return request.get('/AcademicActivities/分页获取主办讲座', {
    params: {
      'paging.pageIndex': paging.pageIndex,
      'paging.pageSize': paging.pageSize,
      'filter.Like活动名称': filter.like活动名称,
      'filter.Like主办人姓名': filter.like主办人姓名,
      'filter.是否反馈过': filter.是否反馈过
    }
  })
}

/**
 * 获取主办讲座详情
 */
export function getLectureDetail(主办讲座编号: number) {
  return request.get('/AcademicActivities/获取主办讲座详情', {
    params: { 主办讲座编号 }
  })
}

/**
 * 发起一个主办讲座流程
 */
export function applyLecture(data: Partial<LectureItem>) {
  return request.post('/AcademicActivities/发起一个主办讲座流程', data)
}

/**
 * 上传主讲人简历
 */
export function uploadLectureResume(file: File) {
  const formData = new FormData()
  formData.append('fileUpload', file)
  return request.post<string[]>('/AcademicActivities/上传主讲人简历', formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  })
}

/**
 * 分页获取讲座反馈
 */
export function getLectureFeedbacks(paging: Paging, filter: LectureFeedbackFilter = {}) {
  return request.get('/AcademicActivities/分页获取讲座反馈', {
    params: {
      'paging.pageIndex': paging.pageIndex,
      'paging.pageSize': paging.pageSize,
      'filter.Like活动名称': filter.like活动名称,
      'filter.Like主办人姓名': filter.like主办人姓名,
      'filter.讲座编号': filter.讲座编号
    }
  })
}

/**
 * 获取讲座反馈详情
 */
export function getLectureFeedbackDetail(讲座反馈编号: number) {
  return request.get('/AcademicActivities/获取讲座反馈详情', {
    params: { 讲座反馈编号 }
  })
}

/**
 * 发起讲座反馈
 */
export function submitLectureFeedback(data: Partial<LectureFeedbackItem>) {
  return request.post('/AcademicActivities/发起一个讲座反馈流程', data)
}

/**
 * 上传讲座附件
 */
export function uploadLectureAttachment(file: File) {
  const formData = new FormData()
  formData.append('fileUpload', file)
  return request.post<string[]>('/AcademicActivities/上传讲座附件', formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  })
}

/**
 * 分页获取参加会议
 */
export function getConferences(paging: Paging, filter: ConferenceFilter = {}) {
  return request.get('/AcademicActivities/分页获取参加会议', {
    params: {
      'paging.pageIndex': paging.pageIndex,
      'paging.pageSize': paging.pageSize,
      'filter.Like会议名称': filter.like会议名称,
      'filter.Like申请人姓名': filter.like申请人姓名,
      'filter.是否反馈过': filter.是否反馈过
    }
  })
}

/**
 * 获取参加会议详情
 */
export function getConferenceDetail(参加会议编号: number) {
  return request.get('/AcademicActivities/获取参加会议详情', {
    params: { 参加会议编号 }
  })
}

/**
 * 发起参加会议申请流程
 */
export function applyConference(data: Partial<ConferenceItem>) {
  return request.post('/AcademicActivities/发起一个参加会议流程', data)
}

/**
 * 上传参会相关文件
 */
export function uploadConferenceFile(file: File) {
  const formData = new FormData()
  formData.append('fileUpload', file)
  return request.post<string[]>('/AcademicActivities/上传参会相关文件', formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  })
}

/**
 * 分页获取参加会议反馈
 */
export function getConferenceFeedbacks(paging: Paging, filter: ConferenceFeedbackFilter = {}) {
  return request.get('/AcademicActivities/分页获取参加会议反馈', {
    params: {
      'paging.pageIndex': paging.pageIndex,
      'paging.pageSize': paging.pageSize,
      'filter.Like会议名称': filter.like会议名称,
      'filter.Like申请人姓名': filter.like申请人姓名,
      'filter.参加会议编号': filter.参加会议编号
    }
  })
}

/**
 * 获取参加会议反馈详情
 */
export function getConferenceFeedbackDetail(参加会议反馈编号: number) {
  return request.get('/AcademicActivities/获取参加会议反馈详情', {
    params: { 参加会议反馈编号 }
  })
}

/**
 * 发起参加会议反馈
 */
export function submitConferenceFeedback(data: Partial<ConferenceFeedbackItem>) {
  return request.post('/AcademicActivities/发起一个参加会议反馈流程', data)
}

/**
 * 上传会议反馈文件
 */
export function uploadConferenceFeedbackFile(file: File) {
  const formData = new FormData()
  formData.append('fileUpload', file)
  return request.post<string[]>('/AcademicActivities/上传会议反馈文件', formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  })
}
