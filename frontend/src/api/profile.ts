import { get, post } from '@/utils/request'

export interface UserProfileDetail {
  编号: number
  工号: string
  姓名: string
  部门编号?: number
  部门名称?: string
  手机?: string
  邮箱?: string
  QQ?: string
  籍贯?: string
  住宅地址?: string
  出生日期?: string
  入职日期?: string
  学位?: string
  获得最高学历的院校?: string
  获得最高学位的院校?: string
  学术特长?: string
  研究方向?: string
  个人获得的荣誉和奖项?: string
  备注?: string
  是否启用?: boolean
}

export interface CapacityRadarData {
  人员编号: number
  论文能力得分?: number
  课题主持得分?: number
  经费规模得分?: number
  成果转化得分?: number
  团队指导得分?: number
  综合科研指数?: number
}

// 获取当前登录用户身份
export function getCurrentUser() {
  return get<any>('/Access/Me')
}

// 获取人员详细资料
export function getPersonDetail(personId: number) {
  return get<UserProfileDetail>('/People/获取某人员详情', { 人员编号: personId })
}

// 获取人员科研能力画像
export function getPersonCapacityRadar(personId: number) {
  return get<CapacityRadarData>('/People/获取人员科研能力画像', { 人员编号: personId })
}

// 更新个人资料
export function updatePersonProfile(data: Partial<UserProfileDetail>) {
  return post<void>('/People/增改人员', data)
}

// 修改密码
export function changeUserPassword(data: { 旧密码: string; 新密码: string }) {
  return post<void>('/Access/ChangePassword', data)
}
