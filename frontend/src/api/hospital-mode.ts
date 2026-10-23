import request from '@/utils/request'

export interface HospitalModeConfig {
  运营模式: '三级综合' | '二级精简'
  隐藏菜单: string[]
  说明?: string
  更新人?: string
  更新时间?: string
}

export function getHospitalModeConfig() {
  return request<HospitalModeConfig>({
    url: '/HospitalMode/获取医院运营配置',
    method: 'get'
  })
}

export function saveHospitalModeConfig(data: HospitalModeConfig) {
  return request({
    url: '/HospitalMode/保存医院运营配置',
    method: 'post',
    data
  })
}
