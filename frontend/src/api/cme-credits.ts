import request from '@/utils/request'

export interface CmeRecordItem {
  编号: number
  人员编号: number
  年度: number
  学分类别: 'I类' | 'II类'
  学分: number
  项目名称: string
  项目编号?: string
  授予单位: string
  获得日期: string
  来源: string
  证书附件路径?: string
  审核状态: '待审核' | '已认定' | '已驳回'
  审核人姓名?: string
  审核时间?: string
  审核意见?: string
  创建时间?: string
}

export interface MyCmeSummary {
  年度: number
  已认定总分: number
  已认定I类分: number
  已认定II类分: number
  待审核学分: number
  要求总分: number
  要求I类分: number
  是否达标: boolean
  总分差额: number
  I类差额: number
  明细列表: CmeRecordItem[]
}

export function getMyCmeCredits(年度?: number) {
  return request<MyCmeSummary>({
    url: '/CmeCredit/获取我的学分',
    method: 'get',
    params: { 年度 }
  })
}

export function applyCmeCredit(data: Partial<CmeRecordItem>) {
  return request({
    url: '/CmeCredit/申报学分',
    method: 'post',
    data
  })
}

export function withdrawCmeApply(id: number) {
  return request({
    url: `/CmeCredit/撤回申报?id=${id}`,
    method: 'post'
  })
}

export function auditCmeCredits(data: { 编号列表: number[]; 通过: boolean; 审核意见?: string }) {
  return request({
    url: '/CmeCredit/审核学分',
    method: 'post',
    data
  })
}

export function getPendingCmeAudits(params: { pageIndex: number; pageSize: number; 年度?: number; 关键词?: string }) {
  return request({
    url: '/CmeCredit/分页查询待审学分',
    method: 'get',
    params
  })
}

export function setLectureCredit(data: {
  讲座编号: number
  学分类别: string
  学分值: number
  签到开始时间: string
  签到结束时间: string
}) {
  return request({
    url: '/CmeCredit/设置讲座学分',
    method: 'post',
    data
  })
}

export function getLectureCreditSetting(讲座编号: number) {
  return request({
    url: `/CmeCredit/获取讲座学分设置?讲座编号=${讲座编号}`,
    method: 'get'
  })
}

export function signInLecture(签到令牌: string) {
  return request({
    url: '/CmeCredit/讲座签到',
    method: 'post',
    data: { 签到令牌 }
  })
}

export function grantLectureCredits(讲座编号: number) {
  return request({
    url: `/CmeCredit/讲座授分?讲座编号=${讲座编号}`,
    method: 'post'
  })
}

export function getCmeAnnualStats(params: { 年度?: number; 部门编号?: number }) {
  return request({
    url: '/CmeCredit/年度达标统计',
    method: 'get',
    params
  })
}

export function sendBatchCmeRemind(data: { 年度: number; 人员编号列表: number[]; 催办内容?: string }) {
  return request({
    url: '/CmeCredit/批量催办',
    method: 'post',
    data
  })
}
