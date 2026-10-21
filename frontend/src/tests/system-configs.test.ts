import { describe, it, expect } from 'vitest'
import type {
  ProjectCategoryItem,
  ExpenseTemplateItem,
  ExpenseTemplateTypeItem,
  JournalItem,
  IndustryCodeItem,
  DisciplineItem,
  RegionItem
} from '../api/base-info'

describe('Research Base Info & Standard System Configs (基础信息与标准规范中心)', () => {
  it('应准确维护项目分类层级关系与排序属性', () => {
    const categories: ProjectCategoryItem[] = [
      { 编号: 1, 上级编号: 0, 名称: '国家级科研课题', 排序: 1, 说明: '国家科技重大专项、国家自然科学基金等' },
      { 编号: 2, 上级编号: 1, 名称: '国家自然科学基金面上项目', 排序: 1 },
      { 编号: 3, 上级编号: 1, 名称: '国家自然科学基金青年项目', 排序: 2 },
      { 编号: 4, 上级编号: 0, 名称: '省部级科研课题', 排序: 2, 说明: '省自然科学基金、省重点研发计划' }
    ]

    const rootNodes = categories.filter((c) => c.上级编号 === 0)
    const childNodes = categories.filter((c) => c.上级编号 === 1)

    expect(rootNodes).toHaveLength(2)
    expect(childNodes).toHaveLength(2)
    expect(childNodes[0].名称).toBe('国家自然科学基金面上项目')
    expect(childNodes[1].排序).toBe(2)
  })

  it('应准确关联经费预算模版与项目支出科目类型', () => {
    const template: ExpenseTemplateItem = {
      编号: 10,
      模版名称: '国家自然科学基金预算模版 (2026标准)',
      备注: '适用于面上项目与青年基金',
      建立时间: '2026-01-10'
    }

    const templateTypes: ExpenseTemplateTypeItem[] = [
      { 编号: 101, 所属经费模版编号: 10, 项目支出类型: '设备费' },
      { 编号: 102, 所属经费模版编号: 10, 项目支出类型: '业务费 (含材料/测试/差旅/出版)' },
      { 编号: 103, 所属经费模版编号: 10, 项目支出类型: '劳务费 (含学生助研津贴)' },
      { 编号: 104, 所属经费模版编号: 10, 项目支出类型: '间接经费 (绩效支出)' }
    ]

    expect(template.编号).toBe(10)
    expect(templateTypes).toHaveLength(4)
    expect(templateTypes.every((t) => t.所属经费模版编号 === 10)).toBe(true)
  })

  it('应准确维护学术期刊库核心期刊标记与影响因子', () => {
    const journals: JournalItem[] = [
      {
        编号: 501,
        期刊名称: 'New England Journal of Medicine',
        issn: '0028-4793',
        cn: '',
        主办单位: 'Massachusetts Medical Society',
        级别: 'SCI-1',
        是否为核心期刊: true,
        影响因子: 96.2
      },
      {
        编号: 502,
        期刊名称: '中华医学杂志',
        issn: '0376-2491',
        cn: '11-2137/R',
        主办单位: '中华医学会',
        级别: '北大核心',
        是否为核心期刊: true,
        影响因子: 2.8
      }
    ]

    const coreJournals = journals.filter((j) => j.是否为核心期刊)
    expect(coreJournals).toHaveLength(2)
    expect(journals[0].影响因子).toBeGreaterThan(90)
    expect(journals[1].cn).toBe('11-2137/R')
  })

  it('应准确验证行业代码与学科门类标准编码', () => {
    const industry: IndustryCodeItem = {
      编号: 801,
      父级编号: 0,
      代码: 'Q8411',
      名称: '综合医院',
      级别: 4,
      说明: '卫健委登记三级甲等综合医院'
    }

    const discipline: DisciplineItem = {
      编号: 901,
      上级学科编号: 10,
      代码: '1002',
      名称: '临床医学',
      级别: 1,
      备注: '医学门类下一级学科'
    }

    const region: RegionItem = {
      编号: 4401,
      父级编号: 4400,
      代码: '440100',
      名称: '广州市',
      级别: 2,
      说明: '广东省地级市'
    }

    expect(industry.代码).toBe('Q8411')
    expect(discipline.代码).toBe('1002')
    expect(region.代码).toBe('440100')
  })
})
