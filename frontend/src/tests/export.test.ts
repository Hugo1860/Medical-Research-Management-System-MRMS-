import { describe, it, expect, vi } from 'vitest'
import { exportToCsv } from '../utils/export'

describe('Universal Export Utility (通用 CSV / Excel 导出工具)', () => {
  it('应准确生成带 UTF-8 BOM 的 CSV 字符串并触发下载', () => {
    let createdBlob: Blob | null = null

    // Mock URL.createObjectURL and URL.revokeObjectURL
    const originalCreateObjectURL = URL.createObjectURL
    const originalRevokeObjectURL = URL.revokeObjectURL
    URL.createObjectURL = vi.fn((blob: Blob) => {
      createdBlob = blob
      return 'blob:mock-url'
    })
    URL.revokeObjectURL = vi.fn()

    // Spy on document.body.appendChild
    const appendSpy = vi.spyOn(document.body, 'appendChild')
    const removeSpy = vi.spyOn(document.body, 'removeChild')

    const columns = [
      { label: '项目名称', prop: 'name' },
      { label: '负责人', prop: 'leader' },
      { label: '经费', prop: 'amount', formatter: (val: number) => `¥${val}` }
    ]

    const data = [
      { name: '肿瘤微环境研究', leader: '李教授', amount: 500000 },
      { name: '靶向分子筛查,含逗号', leader: '王主任', amount: 300000 }
    ]

    exportToCsv('科研课题报表', columns, data)

    expect(URL.createObjectURL).toHaveBeenCalled()
    expect(appendSpy).toHaveBeenCalled()
    expect(removeSpy).toHaveBeenCalled()
    expect(createdBlob).not.toBeNull()

    // 恢复 mock
    URL.createObjectURL = originalCreateObjectURL
    URL.revokeObjectURL = originalRevokeObjectURL
    appendSpy.mockRestore()
    removeSpy.mockRestore()
  })

  it('在无数据时应抛出友好异常', () => {
    const columns = [{ label: '测试', prop: 'test' }]
    expect(() => exportToCsv('空报表', columns, [])).toThrow('没有可导出的数据')
  })
})
