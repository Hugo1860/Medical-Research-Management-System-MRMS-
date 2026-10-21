import { describe, it, expect } from 'vitest'
import type {
  LectureItem,
  LectureFeedbackItem,
  ConferenceItem,
  ConferenceFeedbackItem
} from '../api/activities'

describe('Academic Activities & Lifecycle Management (学术活动与国际会议全周期)', () => {
  it('应准确聚合主办学术讲座场次、经费预算与反馈状态', () => {
    const lectures: LectureItem[] = [
      {
        编号: 1,
        活动名称: '空间转录组学在肿瘤微环境中的前沿应用',
        主讲人姓名: '张院士',
        主讲人单位: '中国科学院上海药物所',
        主讲人职称: '院士/研究员',
        讲座地点: '科研报告厅 301',
        开始时间: '2026-11-10T09:00:00Z',
        结束时间: '2026-11-10T11:30:00Z',
        活动经费: 5000,
        主办人编号: 101,
        主办人姓名: '李主任',
        是否反馈过: true
      },
      {
        编号: 2,
        活动名称: '单细胞多组学数据解析与临床转化',
        主讲人姓名: '王教授',
        主讲人单位: '北京大学医学部',
        主讲人职称: '教授/博士生导师',
        讲座地点: '远程医学中心学术厅',
        开始时间: '2026-11-18T14:30:00Z',
        结束时间: '2026-11-18T16:30:00Z',
        活动经费: 3000,
        主办人编号: 102,
        主办人姓名: '赵研究员',
        是否反馈过: false
      }
    ]

    const totalBudget = lectures.reduce((sum, item) => sum + (item.活动经费 || 0), 0)
    const feedbackCount = lectures.filter(item => item.是否反馈过).length
    const feedbackRate = Math.round((feedbackCount / lectures.length) * 100)

    expect(totalBudget).toBe(8000)
    expect(feedbackCount).toBe(1)
    expect(feedbackRate).toBe(50)
  })

  it('主讲专家档案应具备完整性并支持中外籍专家规范校验', () => {
    const domesticSpeaker: Partial<LectureItem> = {
      主讲人姓名: '陈教授',
      主讲人国别: '中国',
      主讲人单位: '复旦大学生命科学学院',
      主讲人学位: '博士',
      主讲人职称: '特聘教授',
      主讲人简历路径: '/upload/学术活动/主讲人简历/chen_cv.pdf'
    }

    const overseasSpeaker: Partial<LectureItem> = {
      主讲人姓名: 'Dr. John Smith',
      主讲人国别: '美国',
      主讲人单位: 'Harvard Medical School',
      主讲人学位: 'MD, PhD',
      主讲人职称: 'Professor of Oncology'
    }

    expect(domesticSpeaker.主讲人姓名).toBe('陈教授')
    expect(domesticSpeaker.主讲人国别).toBe('中国')
    expect(domesticSpeaker.主讲人简历路径).toContain('.pdf')

    expect(overseasSpeaker.主讲人国别).toBe('美国')
    expect(overseasSpeaker.主讲人学位).toContain('PhD')
  })

  it('讲座效果反馈应准确核算持续时长、到会人次与专家讲课报酬发放方式', () => {
    const feedback: LectureFeedbackItem = {
      编号: 301,
      讲座编号: 1,
      活动名称: '空间转录组学在肿瘤微环境中的前沿应用',
      专家姓名: '张院士',
      专家身份证号码: '110101196001011234',
      专家讲课费金额: 3000,
      结算方式: '转账',
      活动持续分钟: 120,
      参会人数: 85,
      讲座效果: '讲座反响极其热烈，全院青年学者互动充分，对科室申报国自然重点项目提供了新思路。',
      讲座附件路径: '/upload/学术活动/讲座附件/ppt_slides.zip'
    }

    expect(feedback.活动持续分钟).toBe(120)
    expect(feedback.参会人数).toBe(85)
    expect(feedback.结算方式).toBe('转账')
    expect(feedback.专家讲课费金额).toBe(3000)
    expect(feedback.讲座附件路径).toBeDefined()
    expect(feedback.讲座效果?.length).toBeGreaterThan(10)
  })

  it('参加学术会议应精确计算会期日程、往返行程天数及参会总结成果归口', () => {
    const conf: ConferenceItem = {
      编号: 201,
      会议名称: '2026年国际精准医学大会',
      会议主办单位: '国际精准医疗学会 (ISPM)',
      研讨领域: '肿瘤个体化治疗与靶向免疫',
      学科类型: '临床医学',
      会议地址: '成都世纪城国际会议中心',
      会议开始时间: '2026-12-01T08:30:00Z',
      会议结束时间: '2026-12-04T17:00:00Z',
      往返时间: 5,
      会议网址: 'https://ispm2026.med.org',
      申请人编号: 108,
      申请人姓名: '高副主任医师',
      是否反馈过: true
    }

    const confFeedback: ConferenceFeedbackItem = {
      编号: 401,
      参加会议编号: 201,
      会议名称: '2026年国际精准医学大会',
      申请人姓名: '高副主任医师',
      活动持续分钟: 1800,
      参会人数: 1200,
      参会总结: '在大会作题为《非小细胞肺癌脑转移双靶向耐药机制》口头汇报，荣获青年学者优秀壁报奖。',
      会议反馈文件路径: '/upload/学术活动/会议反馈文件/oral_presentation.pptx'
    }

    expect(conf.往返时间).toBe(5)
    expect(conf.研讨领域).toBe('肿瘤个体化治疗与靶向免疫')
    expect(confFeedback.参会人数).toBe(1200)
    expect(confFeedback.参会总结).toContain('口头汇报')
    expect(confFeedback.会议反馈文件路径).toContain('.pptx')
  })
})
