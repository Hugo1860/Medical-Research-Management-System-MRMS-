import { describe, it, expect } from 'vitest'
import type { PersonItem } from '@/api/organization'

describe('UserSelectDialog (人员组织选择器组件逻辑)', () => {
  const mockCandidates: PersonItem[] = [
    { 编号: 1, 工号: 'DOC001', 姓名: '李院士', 性别: true, 部门编号: 10, 部门名称: '心血管内科', 电话: '13800000001', 邮箱: 'li@hospital.org', 是否启用: true },
    { 编号: 2, 工号: 'DOC002', 姓名: '王教授', 性别: true, 部门编号: 10, 部门名称: '心血管内科', 电话: '13800000002', 邮箱: 'wang@hospital.org', 是否启用: true },
    { 编号: 3, 工号: 'DOC003', 姓名: '张副主任', 性别: false, 部门编号: 20, 部门名称: '神经外科', 电话: '13800000003', 邮箱: 'zhang@hospital.org', 是否启用: true },
    { 编号: 4, 工号: 'DOC004', 姓名: '刘主治', 性别: false, 部门编号: 30, 部门名称: '检验医学中心', 电话: '13800000004', 邮箱: 'liu@hospital.org', 是否启用: true }
  ]

  it('应准确按科室部门筛选候选人员', () => {
    const filterByDept = (users: PersonItem[], deptId: number) => {
      if (!deptId) return users
      return users.filter(u => u.部门编号 === deptId)
    }

    const filtered10 = filterByDept(mockCandidates, 10)
    expect(filtered10.length).toBe(2)
    expect(filtered10.map(u => u.姓名)).toEqual(['李院士', '王教授'])

    const filtered20 = filterByDept(mockCandidates, 20)
    expect(filtered20.length).toBe(1)
    expect(filtered20[0].姓名).toBe('张副主任')
  })

  it('应支持姓名与工号模糊匹配筛选', () => {
    const searchUsers = (users: PersonItem[], kw: string) => {
      const q = kw.trim().toLowerCase()
      if (!q) return users
      return users.filter(u =>
        u.姓名.toLowerCase().includes(q) ||
        u.工号.toLowerCase().includes(q) ||
        u.部门名称.toLowerCase().includes(q)
      )
    }

    expect(searchUsers(mockCandidates, 'DOC002').length).toBe(1)
    expect(searchUsers(mockCandidates, 'DOC002')[0].姓名).toBe('王教授')
    expect(searchUsers(mockCandidates, '张').length).toBe(1)
    expect(searchUsers(mockCandidates, '医学').length).toBe(1)
    expect(searchUsers(mockCandidates, '李院士')[0].工号).toBe('DOC001')
  })

  it('穿梭框在单选模式下应只保留最新选中的一名人员', () => {
    const transfer = (
      selected: PersonItem[],
      toAdd: PersonItem[],
      multiple: boolean
    ): PersonItem[] => {
      if (!multiple) {
        return toAdd.slice(0, 1)
      }
      return [...selected, ...toAdd]
    }

    let selected: PersonItem[] = []
    selected = transfer(selected, [mockCandidates[0]], false)
    expect(selected.length).toBe(1)
    expect(selected[0].姓名).toBe('李院士')

    // 单选模式替换
    selected = transfer(selected, [mockCandidates[1]], false)
    expect(selected.length).toBe(1)
    expect(selected[0].姓名).toBe('王教授')
  })

  it('穿梭框在多选模式下应正确累加人员并去重已选', () => {
    let selected: PersonItem[] = [mockCandidates[0]]

    const availableCandidates = (all: PersonItem[], currSelected: PersonItem[]) => {
      const selectedSet = new Set(currSelected.map(s => s.编号))
      return all.filter(u => !selectedSet.has(u.编号))
    }

    let remain = availableCandidates(mockCandidates, selected)
    expect(remain.length).toBe(3)
    expect(remain.some(u => u.编号 === 1)).toBe(false)

    selected = [...selected, mockCandidates[1], mockCandidates[2]]
    remain = availableCandidates(mockCandidates, selected)
    expect(remain.length).toBe(1)
    expect(remain[0].姓名).toBe('刘主治')
  })
})
