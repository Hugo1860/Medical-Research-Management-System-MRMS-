import { get, post, upload } from '@/utils/request'

export interface Department {
  编号: number
  上级部门编号: number
  名称: string
  负责人编号?: number | null
  负责人姓名?: string
  排序值?: number
  是否启用: boolean
  备注?: string
}

export interface PersonItem {
  编号: number
  工号: string
  姓名: string
  性别: boolean | string
  部门编号: number
  部门名称: string
  电话?: string
  手机?: string
  邮箱?: string
  岗位名称?: string
  技术职称?: string
  是否启用: boolean
  建立时间?: string
}

export interface PersonDetail extends PersonItem {
  密码?: string
  英文名?: string
  身份证?: string
  出生日期?: string
  入职日期?: string
  QQ?: string
  籍贯?: string
  毕业日期?: string
  合同开始日期?: string
  合同结束日期?: string
  参加工作日期?: string
  专业技术获得日期?: string
  职务任职日期?: string
  学位?: string
  学历?: string
  政治面貌?: string
  技术职称类别?: string
  第二技术职称类别?: string
  第二技术职称?: string
  婚姻情况?: string
  学制?: string
  学历性质?: string
  学习形式?: string
  人员类别?: string
  个人身份?: string
  民族?: string
  专业技术级别?: string
  所学专业名称?: string
  岗位性质?: string
  职务名称?: string
  职务级别?: string
  岗位类型?: string
  学科门类?: string
  一级学科?: string
  二级学科?: string
  三级学科?: string
  人员类型?: string
  获得最高学历的院校?: string
  获得最高学位的院校?: string
  住宅地址?: string
  学术特长?: string
  研究方向?: string
  个人获得的荣誉和奖项?: string
  备注?: string
}

export interface PersonCapability {
  人员编号: number
  研究专长?: string | null
  研究方向标签?: string | null
  技术服务能力?: string | null
  合作意愿?: '可合作' | '需邀约' | '暂不合作' | string
  合作说明?: string | null
  可用时间?: string | null
  每周可用小时?: number
  可承担项目数?: number
  当前可接项目?: boolean
  可公开展示?: boolean
}

export interface PersonCapabilityProfile {
  人员: PersonItem
  科研能力?: PersonCapability | null
  能力标签: Array<{ 编号: number; 标签类型: string; 标签: string; 来源: string }>
  导师关系: Array<Record<string, any>>
  团队: Array<Record<string, any>>
  项目分工: Array<Record<string, any>>
}

export interface ResearchPartner {
  编号: number
  工号: string
  姓名: string
  部门名称?: string
  研究专长?: string
  研究方向标签?: string
  技术服务能力?: string
  合作意愿?: string
  合作说明?: string
  可用时间?: string
  每周可用小时?: number
  可承担项目数?: number
  当前项目数?: number
  剩余项目容量?: number
  匹配分数?: number
  是否专家?: boolean
}

export interface PagingResult<T> {
  total: number
  list: T[]
}

// 获取部门列表
export function getDepartmentList(params?: { Like名称?: string; 是否启用?: boolean }) {
  return get<Department[]>('/BaseInfo/获取部门列表', params)
}

// 增改部门
export function saveDepartment(data: Partial<Department>) {
  return post('/BaseInfo/增改部门', data)
}

// 启用部门
export function enableDepartment(id: number) {
  return post('/BaseInfo/启用部门', id)
}

// 禁用部门
export function disableDepartment(id: number) {
  return post('/BaseInfo/禁用部门', id)
}

// 分页获取人员列表
export function getPeopleList(
  paging: { index: number; size: number },
  filter?: {
    工号?: string
    Like姓名?: string
    部门编号?: number
    是否启用?: boolean
  }
) {
  return get<PagingResult<PersonItem>>('/People/分页获取人员列表', {
    ...paging,
    ...filter
  })
}

// 获取某人员详情
export function getPersonDetail(id: number) {
  return get<PersonDetail>('/People/获取某人员详情', { 人员编号: id })
}

// 获取人员详情辅助字典数据
export function getPeopleDetailAuxData() {
  return get<{
    民族: string[]
    政治面貌: string[]
    人员类别: string[]
    人员类型: string[]
    岗位名称: string[]
    岗位类型: string[]
    岗位性质: string[]
    学历: string[]
    学位: string[]
    学科门类: string[]
    学制: string[]
    所学专业: string[]
    学习形式: string[]
    学历性质: string[]
    个人身份: string[]
    职位: string[]
    职位级别: string[]
    专业技术级别: string[]
    技术职称分类: string[]
  }>('/People/获取人员详情辅助数据')
}

// 增改人员
export function savePerson(data: Partial<PersonDetail>) {
  return post('/People/增改人员', data)
}

// 启用人员
export function enablePerson(id: number) {
  return post('/People/启用人员', id)
}

// 禁用人员
export function disablePerson(id: number) {
  return post('/People/禁用人员', id)
}

// 获取人员科研能力画像
export function getPersonCapability(id: number) {
  return get<PersonCapabilityProfile>('/People/获取人员科研能力画像', { 人员编号: id })
}

export function savePersonCapability(data: PersonCapability) {
  return post('/People/保存人员科研能力画像', data)
}

export function findResearchPartners(params?: {
  关键词?: string
  技术服务?: string
  合作意愿?: string
  流程模板编号?: number
  项目编号?: number
  最低每周可用小时?: number
  数量?: number
}) {
  return get<ResearchPartner[]>('/People/匹配科研团队成员', params)
}

export function listResearchTeams(params?: { 关键词?: string; 状态?: string; 负责人编号?: number; 数量?: number }) {
  return get<any[]>('/People/查询科研团队', params)
}

export function getResearchTeamDetail(teamId: number, params?: { 流程模板编号?: number; 项目编号?: number }) {
  return get<any>('/People/获取科研团队详情', { 团队编号: teamId, ...params })
}

export function listPersonAssignments(params?: { 人员编号?: number; 流程模板编号?: number; 项目编号?: number }) {
  return get<any[]>('/People/查询人员项目分工', params)
}

export function listExpertAvailability(专家编号?: number) {
  return get<any[]>('/People/查询专家可用时间', { 专家编号 })
}

export function saveExpertAvailability(data: {
  编号?: number
  专家编号: number
  星期: number
  开始时间: string
  结束时间: string
  时区?: string
  是否启用?: boolean
  备注?: string
}) {
  return post('/People/保存专家可用时间', data)
}

export function listExternalExpertInvitations(params?: { 邀请编号?: number; 状态?: string }) {
  return get<any>('/People/查询外部专家邀请', params)
}

export function createExternalExpertInvitation(data: {
  专家编号?: number
  外部姓名: string
  外部邮箱: string
  外部机构?: string
  邀请场景: string
  流程模板编号?: number
  项目编号?: number
  外部系统编号?: number
  幂等键: string
  说明?: string
}) {
  return post('/People/创建外部专家邀请', data)
}

export function writeExternalExpertInvitationReceipt(data: {
  邀请编号: number
  状态: '已发送' | '发送失败' | '已接受' | '已拒绝' | '已取消'
  外部消息编号?: string
  响应摘要?: string
  失败原因?: string
  真实回执: boolean
}) {
  return post('/People/回写外部专家邀请回执', data)
}

// 批量导入人员
export function importPeople(file: File) {
  return upload('/People/导入人员', file, 'files')
}
