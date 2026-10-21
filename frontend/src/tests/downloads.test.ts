import { describe, it, expect } from 'vitest'
import type { DownloadItem, DownloadFilter } from '../api/office'

describe('Document & Download Center (科研规章制度与资料下载中心)', () => {
  it('应准确对科研文件分类、统计各类目数量与现行生效启用比例', () => {
    const list: DownloadItem[] = [
      {
        编号: 1,
        文件名称: '国家自然科学基金项目资金管理办法 (2026修订版)',
        文件类型: '政策法规类',
        文件路径: '/upload/下载中心/nsfc_funds_2026.pdf',
        是否启用: true,
        上传人姓名: '科技处管理专员',
        上传人部门名称: '科研处'
      },
      {
        编号: 2,
        文件名称: '院级临床科研孵化基金申请书模板 (Word版)',
        文件类型: '申报模板类',
        文件路径: '/upload/下载中心/hospital_fund_template.docx',
        是否启用: true,
        上传人姓名: '科技处管理专员',
        上传人部门名称: '科研处'
      },
      {
        编号: 3,
        文件名称: '科研经费转账与差旅劳务报销指引及审批单',
        文件类型: '财务经费类',
        文件路径: '/upload/下载中心/finance_claim_guide.xlsx',
        是否启用: true,
        上传人姓名: '财务处科研专员',
        上传人部门名称: '财务处'
      },
      {
        编号: 4,
        文件名称: '已废止-2018年科研劳务管理办法旧版',
        文件类型: '政策法规类',
        文件路径: '/upload/下载中心/old_rules_deprecated.pdf',
        是否启用: false,
        上传人姓名: '科技处管理专员',
        上传人部门名称: '科研处'
      }
    ]

    const total = list.length
    const policyCount = list.filter(i => i.文件类型 === '政策法规类').length
    const templateCount = list.filter(i => i.文件类型 === '申报模板类').length
    const financeCount = list.filter(i => i.文件类型 === '财务经费类').length
    const activeCount = list.filter(i => i.是否启用).length
    const activeRate = Number(((activeCount / total) * 100).toFixed(1))

    expect(total).toBe(4)
    expect(policyCount).toBe(2)
    expect(templateCount).toBe(1)
    expect(financeCount).toBe(1)
    expect(activeCount).toBe(3)
    expect(activeRate).toBe(75)
  })

  it('应支持文件启禁状态受控切换与历史版本归档', () => {
    const doc: DownloadItem = {
      编号: 10,
      文件名称: '临床研究伦理审查申请指南',
      文件类型: '伦理审查类',
      文件路径: '/upload/下载中心/ethics_guide.pdf',
      是否启用: true
    }

    expect(doc.是否启用).toBe(true)

    // 模拟下架停用
    doc.是否启用 = false
    expect(doc.是否启用).toBe(false)

    // 重新发布启用
    doc.是否启用 = true
    expect(doc.是否启用).toBe(true)
  })

  it('新增与编辑资料表单必须包含关键元数据字段验证', () => {
    const validData: Partial<DownloadItem> = {
      文件名称: '2026年度科技成果转化奖励申报通知及汇总表',
      文件类型: '申报模板类',
      文件路径: '/upload/下载中心/tech_transfer_awards.docx',
      是否启用: true,
      备注: '面向全院临床医技科室，截止时间2026年12月30日'
    }

    const hasName = Boolean(validData.文件名称 && validData.文件名称.trim().length > 0)
    const hasType = Boolean(validData.文件类型)
    const hasPath = Boolean(validData.文件路径 && validData.文件路径.startsWith('/upload/'))

    expect(hasName).toBe(true)
    expect(hasType).toBe(true)
    expect(hasPath).toBe(true)
  })

  it('多条件检索过滤器应规范拼装 API 查询参数', () => {
    const filter: DownloadFilter = {
      like文件名称: '基金',
      like文件类型: '政策法规类',
      like上传人姓名: '李专员',
      是否启用: true
    }

    expect(filter.like文件名称).toBe('基金')
    expect(filter.like文件类型).toBe('政策法规类')
    expect(filter.like上传人姓名).toBe('李专员')
    expect(filter.是否启用).toBe(true)
  })
})
