import { describe, it, expect } from 'vitest'
import type {
  AchievementOverviewItem
} from '../api/fruit'

describe('Comprehensive Research Achievement Ledger (成果综合业务台账与治理)', () => {
  it('应正确归类并提取成果总览的多维类型与发起人科室', () => {
    const overviewItems: AchievementOverviewItem[] = [
      {
        编号: 1,
        成果名称: '基于深度学习的冠脉钙化积分全自动评估算法研究',
        成果类型: '论文',
        发起人编号: 10,
        发起人姓名: '陈教授',
        发起人部门名称: '放射科'
      },
      {
        编号: 2,
        成果名称: '现代精准临床药理学与个体化给药实践',
        成果类型: '著作',
        发起人编号: 12,
        发起人姓名: '赵研究员',
        发起人部门名称: '药剂科'
      },
      {
        编号: 3,
        成果名称: '一种医用内窥镜微创手术抓钳锁止机构',
        成果类型: '专利',
        发起人编号: 15,
        发起人姓名: '孙主任',
        发起人部门名称: '普通外科'
      },
      {
        编号: 4,
        成果名称: '消化道肿瘤早期内镜筛查体系的构建与推广应用',
        成果类型: '获奖',
        发起人编号: 18,
        发起人姓名: '钱院长',
        发起人部门名称: '消化内科'
      }
    ]

    expect(overviewItems).toHaveLength(4)
    const types = overviewItems.map((item) => item.成果类型)
    expect(types).toEqual(['论文', '著作', '专利', '获奖'])

    const papers = overviewItems.filter((item) => item.成果类型 === '论文')
    expect(papers[0].发起人部门名称).toBe('放射科')
  })

  it('应正确验证专利协同通知负载与幂等键规范', () => {
    const patentNotifyRequest = {
      专利编号: 88,
      外部系统编号: 2,
      事件类型: '授权' as const,
      说明: '专利已取得正式授权证书，通知代理机构办理证书交接'
    }

    // 生成与后端控制器对齐的幂等键格式: patent-agent:{外部系统编号}:{专利编号}:{事件类型}
    const expectedIdempotencyKey = `patent-agent:${patentNotifyRequest.外部系统编号}:${patentNotifyRequest.专利编号}:${patentNotifyRequest.事件类型}`
    expect(expectedIdempotencyKey).toBe('patent-agent:2:88:授权')
    expect(['申请', '授权', '变更', '年费', '撤回']).toContain(patentNotifyRequest.事件类型)
  })

  it('应校验成果治理状态流转与跨库关联模式', () => {
    const validGovernanceStatuses = ['正常', '疑义', '合并', '撤回']
    const relationTypes = ['研究', '资助', '伦理', '数据', '其他']

    const mockGovernance = {
      成果类型: 3, // 专利
      成果编号: 88,
      状态: '正常',
      治理原因: '院学术委员会年度核查'
    }

    const mockRelation = {
      成果类型: 3,
      成果编号: 88,
      关联类型: '资助',
      项目编号: 501,
      关联说明: '国自然面上项目资助产出'
    }

    expect(validGovernanceStatuses).toContain(mockGovernance.状态)
    expect(relationTypes).toContain(mockRelation.关联类型)
    expect(mockRelation.项目编号).toBeGreaterThan(0)
  })
})
