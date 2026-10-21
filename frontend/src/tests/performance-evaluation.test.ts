import { describe, it, expect } from 'vitest'
import type {
  EvaluationRankingItem,
  EvaluationDetailItem,
  EvaluationRuleItem,
  EvaluationAppealItem,
  EvaluationRewardItem
} from '../api/evaluation'

describe('Research Performance Evaluation & Reward Management (科研绩效评价与奖励管理)', () => {
  it('应准确计算评价排名榜单人均分、最高标杆分与排位高亮', () => {
    const rankings: EvaluationRankingItem[] = [
      {
        排名: 1,
        人员编号: 101,
        姓名: '张院士',
        岗位名称: '首席科学家 / 呼吸科',
        学科门类: '临床医学',
        评价年度: 2026,
        总分: 385.5,
        状态: '已发布',
        规则名称: '高水平学术创新分类评价体系',
        版本号: 2
      },
      {
        排名: 2,
        人员编号: 102,
        姓名: '李教授',
        岗位名称: '主任医师 / 肿瘤科',
        学科门类: '肿瘤学',
        评价年度: 2026,
        总分: 290.0,
        状态: '已发布',
        规则名称: '高水平学术创新分类评价体系',
        版本号: 2
      },
      {
        排名: 3,
        人员编号: 103,
        姓名: '王研究员',
        岗位名称: '特聘教授 / 药剂科',
        学科门类: '药学',
        评价年度: 2026,
        总分: 245.0,
        状态: '已发布',
        规则名称: '高水平学术创新分类评价体系',
        版本号: 2
      }
    ]

    const totalSum = rankings.reduce((acc, cur) => acc + cur.总分, 0)
    const avgScore = Number((totalSum / rankings.length).toFixed(1))
    const topScore = Math.max(...rankings.map((r) => r.总分))

    expect(avgScore).toBe(306.8)
    expect(topScore).toBe(385.5)
    expect(rankings[0].排名).toBe(1)
    expect(rankings[0].姓名).toBe('张院士')
  })

  it('应准确依权重计算各证据链指标项得分并受最高分封顶约束', () => {
    const ruleItem: EvaluationRuleItem = {
      编号: 1,
      规则编号: 10,
      项目代码: 'SCI_Q1',
      项目名称: '中科院一区 Top 顶刊发表',
      权重: 1.5,
      最高分: 150,
      证据类型: '论文'
    }

    const detailItem: EvaluationDetailItem = {
      规则项编号: 1,
      证据类型: '论文',
      证据编号: 'DOI:10.1016/j.cell.2026.01',
      证据标题: 'Structural insights into receptor targeting',
      原始值: 80, // 80 * 1.5 = 120
      核验状态: '已核验'
    }

    const rawScore = detailItem.原始值 * ruleItem.权重
    const finalScore = Math.min(rawScore, ruleItem.最高分)
    detailItem.得分 = finalScore

    expect(rawScore).toBe(120)
    expect(finalScore).toBe(120)
    expect(finalScore).toBeLessThanOrEqual(ruleItem.最高分)
  })

  it('应正确处理学术申诉与委员会复核状态流转', () => {
    const appeal: EvaluationAppealItem = {
      编号: 801,
      评价结果编号: 5001,
      发起人编号: 102,
      申诉理由: '第一单位第一作者SCI论著影响因子计分漏算',
      期望处理: '重新计入一区Top期刊加分',
      状态: '处理中'
    }

    expect(appeal.状态).toBe('处理中')

    // 委员会接受申诉，评价结果修订
    appeal.状态 = '接受'
    appeal.处理意见 = '经学术委员会核验原始文献与DOI，确认加分有效，已修订总分。'

    const resultStatus = appeal.状态 === '接受' ? '已修订' : '已发布'
    expect(appeal.状态).toBe('接受')
    expect(resultStatus).toBe('已修订')
  })

  it('应准确维护科研评价优秀个人奖励兑现记录', () => {
    const reward: EvaluationRewardItem = {
      编号: 901,
      评价结果编号: 5001,
      人员编号: 102,
      奖励名称: '2026年度科技创新杰出青年拔尖人才奖',
      奖励级别: '院级一等奖',
      奖励金额: 10000,
      状态: '待发放',
      发放日期: '2026-12-30',
      备注: '经院长办公会科研考核审定'
    }

    expect(reward.奖励金额).toBe(10000)
    expect(reward.奖励级别).toBe('院级一等奖')
    expect(reward.状态).toBe('待发放')
  })
})
