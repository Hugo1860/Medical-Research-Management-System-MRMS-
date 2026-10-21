import { get, post } from '@/utils/request'

export interface AcademicAppointmentItem {
  编号: number
  任职人员编号?: number
  姓名?: string
  工号?: string
  部门名称?: string
  任职团体名称: string
  所任职务: string
  任职开始时间?: string
  任职结束时间?: string
  聘书路径?: string
  备注?: string
  状态?: string
  建立时间?: string
}

export interface AcademicAppointmentFilter {
  Index: number
  Size: number
  OrderType?: boolean
  Like姓名?: string
  Like任职团体名称?: string
  Like所任职务?: string
  任职人员编号?: number
}

export interface AcademicAppointmentForm {
  基本资料: {
    任职人员编号?: number
    任职团体名称: string
    所任职务: string
    任职开始时间?: string
    任职结束时间?: string
    聘书路径?: string
    备注?: string
  }
  IsHold?: boolean
  步骤?: Record<string, any>
}

export interface ExpertItem {
  编号: number
  工号: string
  姓名: string
  性别?: boolean
  部门编号?: number
  部门名称?: string
  电话?: string
  邮箱?: string
  技术职称?: string
  职务?: string
  学历?: string
  学位?: string
  是否启用?: boolean
  建立时间?: string
}

export interface ResearchPartner {
  编号: number
  工号: string
  姓名: string
  部门编号?: number
  部门名称?: string
  技术职称?: string
  研究专长?: string
  研究方向标签?: string
  技术服务能力?: string
  合作意愿?: string
  合作说明?: string
  是否专家: boolean
}

export interface PartnerSearchFilter {
  关键词?: string
  技术服务?: string
  合作意愿?: string
  数量?: number
}

export interface PagingResult<T> {
  total: number
  list: T[]
}

// 分页获取学术任职列表 (全员)
export function getPagingAcademicAppointments(
  paging: { Index: number; Size: number; OrderType?: boolean },
  filter?: { Like姓名?: string; Like任职团体名称?: string; Like所任职务?: string }
) {
  return get<PagingResult<AcademicAppointmentItem>>('/People/分页获取学术任职', {
    ...paging,
    ...filter
  })
}

// 分页获取当前登录人的学术任职列表
export function getMyPagingAcademicAppointments(
  paging: { Index: number; Size: number; OrderType?: boolean },
  filter?: { Like任职团体名称?: string; Like所任职务?: string }
) {
  return get<PagingResult<AcademicAppointmentItem>>('/People/分页获取我的学术任职', {
    ...paging,
    ...filter
  })
}

// 获取学术任职详情与审核情况
export function getAcademicAppointmentDetail(任职编号: number) {
  return get<{ 学术任职详情: AcademicAppointmentItem; 审核情况: any[] }>('/People/获取学术任职详情', {
    任职编号
  })
}

// 新增/申报学术任职
export function addAcademicAppointment(data: AcademicAppointmentForm) {
  return post('/People/新增学术任职', data)
}

// 完善学术任职资料
export function completeAcademicAppointment(data: {
  步骤编号?: number
  附加数据: AcademicAppointmentForm
}) {
  return post('/People/完善学术任职', data)
}

// 分页获取专家人才
export function getPagingExperts(
  paging: { Index: number; Size: number; OrderType?: boolean },
  filter?: { Like姓名?: string; Like工号?: string; 部门编号?: number; Like技术职称?: string }
) {
  return get<PagingResult<ExpertItem>>('/People/分页获取专家', {
    ...paging,
    ...filter
  })
}

// 分页获取候选人员 (非专家，用于遴选)
export function getPagingNonExperts(
  paging: { Index: number; Size: number; OrderType?: boolean },
  filter?: { Like姓名?: string; Like工号?: string; 部门编号?: number }
) {
  return get<PagingResult<ExpertItem>>('/People/分页获取不是专家的人员', {
    ...paging,
    ...filter
  })
}

// 从现有人员中批量添加专家
export function addExpertsFromPersonnel(编号列表: number[]) {
  return post('/People/从现有人员增加专家', 编号列表)
}

// 批量删除专家角色
export function deleteExperts(编号列表: number[]) {
  return post('/People/删除专家', 编号列表)
}

// 跨维度查找科研合作人与专家候选人
export function searchResearchPartners(filter: PartnerSearchFilter) {
  return get<ResearchPartner[]>('/People/查找科研合作人', filter)
}
