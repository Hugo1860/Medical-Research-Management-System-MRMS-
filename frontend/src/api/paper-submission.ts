import request from '@/utils/request'

export interface JournalRiskCheckResult {
  风险等级: '高' | '中' | '低' | '无'
  风险名单命中: any[]
  是否正规国内期刊: boolean | null
  正规期刊详情: any
  分区匹配: any[]
  研判提示: string[]
}

export interface PaperSubmissionItem {
  编号: number
  备案号: string
  申请人编号?: number
  申请人姓名?: string
  部门编号?: number
  部门名称?: string
  论文题目: string
  拟投期刊名称: string
  ISSN?: string
  CN号?: string
  期刊类型: '国内' | '国外'
  作者排序: any[]
  关联项目类型?: string
  关联项目编号?: number
  稿件附件路径?: string
  诚信承诺: boolean
  风险检查结果?: any
  风险等级: '高' | '中' | '低' | '无'
  状态: '草稿' | '待科室审核' | '待科教科审核' | '已备案' | '已驳回' | '已撤回' | '已见刊' | '已作废'
  科室审核人姓名?: string
  科室审核时间?: string
  科室审核意见?: string
  科教科审核人姓名?: string
  科教科审核时间?: string
  科教科审核意见?: string
  关联论文编号?: number
  预计版面费?: number
  创建时间?: string
  更新时间?: string
}

export function checkJournalRisk(data: { 期刊名称: string; ISSN?: string; CN号?: string; 期刊类型?: string }) {
  return request<JournalRiskCheckResult>({
    url: '/PaperSubmission/检查期刊风险',
    method: 'post',
    data
  })
}

export function savePaperSubmission(data: Partial<PaperSubmissionItem>) {
  return request({
    url: '/PaperSubmission/保存投稿备案',
    method: 'post',
    data
  })
}

export function submitPaperSubmission(id: number) {
  return request({
    url: `/PaperSubmission/提交投稿备案?id=${id}`,
    method: 'post'
  })
}

export function withdrawPaperSubmission(id: number) {
  return request({
    url: `/PaperSubmission/撤回投稿备案?id=${id}`,
    method: 'post'
  })
}

export function auditDeptSubmission(data: { 编号: number; 通过: boolean; 审核意见?: string }) {
  return request({
    url: '/PaperSubmission/科室审核',
    method: 'post',
    data
  })
}

export function auditHospitalSubmission(data: { 编号: number; 通过: boolean; 审核意见?: string }) {
  return request({
    url: '/PaperSubmission/科教科审核',
    method: 'post',
    data
  })
}

export function registerPublish(data: { 编号: number; 关联论文编号?: number; 见刊说明?: string }) {
  return request({
    url: '/PaperSubmission/登记见刊',
    method: 'post',
    data
  })
}

export function getMySubmissions(params: { pageIndex: number; pageSize: number; 状态?: string; 关键词?: string }) {
  return request({
    url: '/PaperSubmission/分页查询我的备案',
    method: 'get',
    params
  })
}

export function getPendingSubmissions(params: { pageIndex: number; pageSize: number; 节点?: string; 关键词?: string }) {
  return request({
    url: '/PaperSubmission/分页查询待审备案',
    method: 'get',
    params
  })
}

export function getReimbursableSubmissions(关键词?: string) {
  return request({
    url: '/PaperSubmission/查询可用报销备案',
    method: 'get',
    params: { 关键词 }
  })
}

export function getSubmissionDetail(id: number) {
  return request({
    url: `/PaperSubmission/获取备案详情?id=${id}`,
    method: 'get'
  })
}

export function getJournalRiskList(params: { pageIndex: number; pageSize: number; 来源?: string; 关键词?: string }) {
  return request({
    url: '/PaperSubmission/分页查询期刊风险名单',
    method: 'get',
    params
  })
}

export function getDomesticJournalList(params: { pageIndex: number; pageSize: number; 关键词?: string }) {
  return request({
    url: '/PaperSubmission/分页查询国内正规期刊',
    method: 'get',
    params
  })
}
