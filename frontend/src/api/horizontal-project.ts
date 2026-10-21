import request from '@/utils/request'

export interface Paging {
  pageIndex: number
  pageSize: number
}

export interface HorizontalProjectFilter {
  like合同编号?: string
  like合同名称?: string
  发起人编号?: number
}

export interface HorizontalProjectChangeFilter {
  like合同编号?: string
  like合同名称?: string
  like变更事由?: string
}

export interface HorizontalProjectClosureFilter {
  like合同编号?: string
  like合同名称?: string
}

export interface HorizontalProjectItem {
  编号: number
  合同编号: string
  合同名称: string
  项目负责人编号: number
  项目负责人姓名?: string
  合作方单位名称: string
  合同金额: number
  签订时间: string
  合同开始时间: string
  合同结束时间: string
  合作方式?: string
  技术领域?: string
  是否申请技术合同认定?: boolean
  合同技术交易额?: number
  知识产权?: string
  批准预算?: number
  已审核支出?: number
  已付款金额?: number
  状态?: number | string
  建立时间?: string
}

export interface HorizontalProjectChangeItem {
  编号: number
  横向项目编号: number
  合同编号: string
  合同名称: string
  变更事由: string
  变更时间: string
  状态?: number | string
}

export interface HorizontalProjectClosureItem {
  编号: number
  横向项目编号: number
  合同编号: string
  合同名称: string
  结题时间: string
  验收结果: string
  归档状态?: string
  状态?: number | string
}

export interface HorizontalLifecycleRequest {
  横向项目编号: number
  事件类型: '延期' | '暂停' | '终止'
  新合同结束日期?: string
  申请原因: string
  附件路径?: string
}

export interface HorizontalResearchReportRequest {
  横向项目编号: number
  报告类型: string
  报告年度: number
  报告摘要?: string
  报告正文?: string
  文件路径?: string
  提交审核?: boolean
}

/**
 * 分页获取横向项目列表
 */
export function getHorizontalProjects(paging: Paging, filter: HorizontalProjectFilter = {}) {
  return request.get('/XProject/分页获取横向项目', {
    params: {
      'paging.pageIndex': paging.pageIndex,
      'paging.pageSize': paging.pageSize,
      'filter.Like合同编号': filter.like合同编号,
      'filter.Like合同名称': filter.like合同名称,
      'filter.发起人编号': filter.发起人编号
    }
  })
}

/**
 * 获取横向项目详情
 */
export function getHorizontalProjectDetail(横向项目编号: number) {
  return request.get('/XProject/获取横向项目详情', {
    params: { 横向项目编号 }
  })
}

/**
 * 新增横向项目申请
 */
export function createHorizontalProject(data: {
  基本资料: Partial<HorizontalProjectItem>
  参与人列表: any[]
  经费预算列表: any[]
  isHold?: boolean
}) {
  return request.post('/XProject/新增横向项目申请', data)
}

/**
 * 分页获取横向项目变更
 */
export function getHorizontalProjectChanges(paging: Paging, filter: HorizontalProjectChangeFilter = {}) {
  return request.get('/XProject/分页获取横向项目变更', {
    params: {
      'paging.pageIndex': paging.pageIndex,
      'paging.pageSize': paging.pageSize,
      'filter.Like合同编号': filter.like合同编号,
      'filter.Like合同名称': filter.like合同名称,
      'filter.Like变更事由': filter.like变更事由
    }
  })
}

/**
 * 获取横向项目变更详情
 */
export function getHorizontalProjectChangeDetail(横向项目编号: number, 横向项目变更编号: number, 状态?: number) {
  return request.get('/XProject/获取横向项目变更详情', {
    params: { 横向项目编号, 横向项目变更编号, 状态 }
  })
}

/**
 * 分页获取横向项目结题
 */
export function getHorizontalProjectClosures(paging: Paging, filter: HorizontalProjectClosureFilter = {}) {
  return request.get('/XProject/分页获取横向项目结题', {
    params: {
      'paging.pageIndex': paging.pageIndex,
      'paging.pageSize': paging.pageSize,
      'filter.Like合同编号': filter.like合同编号,
      'filter.Like合同名称': filter.like合同名称
    }
  })
}

/**
 * 获取横向项目结题详情
 */
export function getHorizontalProjectClosureDetail(横向项目申请编号: number, 横向项目结题编号: number) {
  return request.get('/XProject/获取横向项目结题详情', {
    params: { 横向项目申请编号, 横向项目结题编号 }
  })
}

/** 横向合同的生命周期、研究报告、付款节点和档案留痕。 */
export function getHorizontalProjectLifecycle(横向项目编号: number) {
  return request.get('/XProject/获取横向项目生命周期档案', { params: { 横向项目编号 } })
}

export function applyHorizontalProjectLifecycle(data: HorizontalLifecycleRequest) {
  return request.post('/XProject/申请横向项目延期暂停终止', data)
}

export function submitHorizontalResearchReport(data: HorizontalResearchReportRequest) {
  return request.post('/XProject/提交横向项目研究报告', data)
}

export function archiveHorizontalProjectMaterial(data: {
  横向项目编号: number
  材料类型: string
  材料标题: string
  文件路径: string
  文件校验和?: string
  版本号?: number
  是否必需?: boolean
}) {
  return request.post('/XProject/归档横向项目材料', data)
}
