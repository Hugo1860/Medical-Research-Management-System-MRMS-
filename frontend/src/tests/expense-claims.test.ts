import { describe, it, expect } from 'vitest'
import type {
  VerticalExpenseSummaryItem,
  HorizontalExpenseSummaryItem,
  ConferenceExpenseSummaryItem,
  ExpenseSubmitForm
} from '../api/finance'

describe('Expense Claims & Financial Ledger (科研经费报销与票据核销台账)', () => {
  it('应准确核算纵向项目经费报销额度、在途待审与剩余可用预算', () => {
    const verticalList: VerticalExpenseSummaryItem[] = [
      {
        编号: 101,
        项目中文名称: '肿瘤微环境靶向治疗新策略',
        分类名称: '国家自然科学基金',
        项目级别: '国家级',
        负责人姓名: '李教授',
        批准经费: 600000,
        经费: 400000, // 累计到账
        剩余经费: 280000,
        待审核报销: 20000,
        已审核报销: 100000
      },
      {
        编号: 102,
        项目中文名称: '高敏分子诊断试剂盒研发',
        分类名称: '省重点研发计划',
        项目级别: '省部级',
        负责人姓名: '王主任',
        批准经费: 300000,
        经费: 300000,
        剩余经费: 150000,
        待审核报销: 30000,
        已审核报销: 120000
      }
    ]

    const totalApproved = verticalList.reduce((acc, cur) => acc + cur.已审核报销, 0)
    const totalPending = verticalList.reduce((acc, cur) => acc + cur.待审核报销, 0)
    const totalRemaining = verticalList.reduce((acc, cur) => acc + cur.剩余经费, 0)

    expect(totalApproved).toBe(220000)
    expect(totalPending).toBe(50000)
    expect(totalRemaining).toBe(430000)

    // 单项预算执行率
    const execRatePrj1 = Math.round((verticalList[0].已审核报销 / verticalList[0].经费) * 100)
    expect(execRatePrj1).toBe(25)
  })

  it('应准确核算横向合同经费报销与收支台账', () => {
    const horizontal: HorizontalExpenseSummaryItem = {
      编号: 201,
      合同名称: '创新药物药代动力学合作研发项目',
      合同编号: 'HT-2026-MED08',
      合同金额: 1200000,
      项目负责人姓名: '陈研究员',
      项目负责人部门名称: '药物分析科',
      经费: 600000, // 已到款
      剩余经费: 450000,
      待审核报销: 50000,
      已审核报销: 100000
    }

    expect(horizontal.合同金额).toBe(1200000)
    expect(horizontal.经费 - horizontal.已审核报销 - horizontal.待审核报销).toBe(450000)
    expect(horizontal.剩余经费).toBe(450000)
  })

  it('应准确验证会议报销数据模型与状态流转', () => {
    const confItem: ConferenceExpenseSummaryItem = {
      编号: 301,
      会议名称: '2026年国际医学免疫学前沿高峰论坛',
      申请人姓名: '赵副主任',
      部门名称: '风湿免疫科',
      报销总金额: 8500,
      报销时间: '2026-05-18',
      状态名称: '已办结'
    }

    expect(confItem.报销总金额).toBe(8500)
    expect(confItem.状态名称).toBe('已办结')
  })

  it('应正确构造多科目分录报销申报表单与动态汇总', () => {
    const claimPayload: ExpenseSubmitForm = {
      基本资料: {
        项目编号: 101,
        报销方式: '公务卡',
        公务卡号: '6228481234567890',
        公务卡所属人姓名: '李教授',
        相关文件路径: '/uploads/expenses/2026/bill_batch_01.pdf',
        备注: '购买细胞培养试剂及第三方化验检测费'
      },
      报销详情列表: [
        {
          项目支出类型: '直接经费',
          项目支出内容: '材料与耗材试剂费',
          报销金额: 4500,
          备注: '发票号 26880912'
        },
        {
          项目支出类型: '直接经费',
          项目支出内容: '测试化验与加工检验费',
          报销金额: 3200,
          备注: '发票号 26880913'
        },
        {
          项目支出类型: '直接经费',
          项目支出内容: '差旅与学术会议费',
          报销金额: 1800,
          备注: '高铁票与住宿费'
        }
      ]
    }

    const totalClaimSum = claimPayload.报销详情列表.reduce((acc, cur) => acc + cur.报销金额, 0)
    expect(totalClaimSum).toBe(9500)
    expect(claimPayload.基本资料.报销方式).toBe('公务卡')
    expect(claimPayload.报销详情列表).toHaveLength(3)
  })
})
