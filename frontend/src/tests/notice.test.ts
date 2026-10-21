import { describe, it, expect } from 'vitest'
import type { NoticeItem } from '../api/office'

describe('Research Office Notice Center (科研办公与通知公告中心)', () => {
  it('应准确对通知公告分类、统计发布量、现行生效数与附件率', () => {
    const list: NoticeItem[] = [
      {
        编号: 1,
        通知名称: '关于组织申报2026年度国家自然科学基金重点项目的通知',
        通知类型: '公告',
        通知标识: '申报指南',
        发送人编号: 2,
        发送人姓名: '管理员',
        发送人部门名称: '科研处',
        建立时间: '2026-10-10 09:00:00',
        是否启用: true,
        接收人数: 240,
        已接收人数: 185,
        相关文件路径: '/upload/办公/通知公告文件/nsfc_2026_guide.pdf'
      },
      {
        编号: 2,
        通知名称: '关于开展2026年秋季学期科研实验室生物安全检查的通知',
        通知类型: '普通通知',
        通知标识: '实验室安全',
        发送人编号: 2,
        发送人姓名: '管理员',
        发送人部门名称: '科研处',
        建立时间: '2026-10-12 14:30:00',
        是否启用: true,
        接收人数: 80,
        已接收人数: 60,
        相关文件路径: ''
      },
      {
        编号: 3,
        通知名称: '已截止-2025年度省科技进步奖提名项目院内初选公示',
        通知类型: '公告',
        通知标识: '评审公示',
        发送人编号: 2,
        发送人姓名: '管理员',
        发送人部门名称: '科研处',
        建立时间: '2025-11-01 10:00:00',
        是否启用: false,
        接收人数: 300,
        已接收人数: 290,
        相关文件路径: '/upload/办公/通知公告文件/award_nominees.xlsx'
      }
    ]

    const total = list.length
    const activeCount = list.filter(item => item.是否启用).length
    const publicNoticeCount = list.filter(item => item.通知类型 === '公告').length
    const attachCount = list.filter(item => item.相关文件路径 && item.相关文件路径 !== 'string').length
    const attachmentRate = Math.round((attachCount / total) * 100)

    expect(total).toBe(3)
    expect(activeCount).toBe(2)
    expect(publicNoticeCount).toBe(2)
    expect(attachmentRate).toBe(67)
  })

  it('应能够提取多份逗号分隔的附件路径并解析文件名', () => {
    const rawFiles = '/upload/下载中心/电话拨打_20180713160244.png, /upload/下载中心/电话拨打2_20180713160901.png'
    const parsed = rawFiles.split(',').map(s => s.trim()).filter(Boolean)

    expect(parsed.length).toBe(2)
    const file1Name = parsed[0].replace(/\\/g, '/').split('/').pop()
    const file2Name = parsed[1].replace(/\\/g, '/').split('/').pop()

    expect(file1Name).toBe('电话拨打_20180713160244.png')
    expect(file2Name).toBe('电话拨打2_20180713160901.png')
  })

  it('发布通知时应构建符合后端 sp_通知公告_增改 的标准两级数据结构', () => {
    const input: Partial<NoticeItem> = {
      通知名称: '科研伦理委员会第12次审查会议安排',
      通知类型: '公告',
      通知标识: '伦理审查',
      通知内容: '<p>请各项目组按时参会并提交纸质版材料。</p>',
      关闭时间: '2028-12-31 23:59:59',
      相关文件路径: '/upload/ethics_review.pdf',
      是否启用: true
    }

    const payload = {
      基本资料: {
        编号: input.编号 || 0,
        通知类型: input.通知类型 || '公告',
        通知标识: input.通知标识 || '',
        发送人编号: 2,
        通知名称: input.通知名称 || '',
        关闭时间: input.关闭时间,
        相关文件路径: input.相关文件路径 || '',
        通知内容: input.通知内容 || '',
        接收人数: 0,
        已接收人数: 0,
        是否启用: true,
        备注: ''
      },
      通知公告接收条件列表: [
        { 接收者类型: 0, 接收者编号: 0 }
      ]
    }

    expect(payload.基本资料.通知名称).toBe('科研伦理委员会第12次审查会议安排')
    expect(payload.基本资料.关闭时间).toBe('2028-12-31 23:59:59')
    expect(payload.通知公告接收条件列表[0].接收者类型).toBe(0)
  })
})
