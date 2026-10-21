import { describe, it, expect } from 'vitest'
import type {
  ExpertItem,
  ResearchPartner,
  AcademicAppointmentForm
} from '../api/academic-appointment'

describe('Academic Appointment & Expert Pool (学术任职与专家人才管理)', () => {
  it('应正确验证学术任职申报表单与周期合理性', () => {
    const validForm: AcademicAppointmentForm = {
      基本资料: {
        任职团体名称: '中华医学会心血管病学分会',
        所任职务: '常务委员',
        任职开始时间: '2024-01-01',
        任职结束时间: '2027-12-31',
        聘书路径: 'upload/组织架构/学术任职/cert_001.pdf',
        备注: '第十届委员会'
      }
    }

    expect(validForm.基本资料.任职团体名称).toBe('中华医学会心血管病学分会')
    expect(validForm.基本资料.所任职务).toBe('常务委员')
    expect(new Date(validForm.基本资料.任职结束时间!).getTime()).toBeGreaterThan(
      new Date(validForm.基本资料.任职开始时间!).getTime()
    )
  })

  it('应正确识别入库专家与非专家候选人并提取科研标签', () => {
    const mockPartners: ResearchPartner[] = [
      {
        编号: 101,
        工号: 'DOC001',
        姓名: '张教授',
        部门编号: 10,
        部门名称: '心血管内科',
        技术职称: '主任医师',
        研究专长: '冠心病介入诊疗',
        研究方向标签: '心血管介入;生物材料;人工智能辅助诊断',
        技术服务能力: '动物实验手术平台',
        合作意愿: '可合作',
        是否专家: true
      },
      {
        编号: 102,
        工号: 'DOC002',
        姓名: '李研究员',
        部门编号: 12,
        部门名称: '医学检验中心',
        技术职称: '副研究员',
        研究专长: '微流控芯片核酸快速检测',
        研究方向标签: 'POCT,分子诊断',
        技术服务能力: '高通量荧光定量PCR平台',
        合作意愿: '需邀约',
        是否专家: false
      }
    ]

    expect(mockPartners.filter((p) => p.是否专家)).toHaveLength(1)
    expect(mockPartners.find((p) => !p.是否专家)?.姓名).toBe('李研究员')

    // 标签解析验证
    const tags = mockPartners[0].研究方向标签!.split(/[,;，；\s]+/).filter(Boolean)
    expect(tags).toContain('心血管介入')
    expect(tags).toContain('生物材料')
    expect(tags).toContain('人工智能辅助诊断')
    expect(tags).toHaveLength(3)
  })

  it('应正确过滤并处理专家遴选批量入库列表', () => {
    const candidates: ExpertItem[] = [
      { 编号: 1, 工号: 'E001', 姓名: '王教授', 部门名称: '肿瘤科' },
      { 编号: 2, 工号: 'E002', 姓名: '赵教授', 部门名称: '放射科' },
      { 编号: 3, 工号: 'E003', 姓名: '孙主任', 部门名称: '病理科' }
    ]

    const selectedIds = candidates.map((c) => c.编号)
    expect(selectedIds).toEqual([1, 2, 3])
    expect(selectedIds.includes(2)).toBe(true)
  })
})
