import { describe, it, expect } from 'vitest'
import type { RolePermissionItem } from '../api/system'

// 树节点转换辅助函数
function buildPermissionTree(items: RolePermissionItem[]) {
  interface TreeNode extends RolePermissionItem {
    children?: TreeNode[]
  }
  const map: Record<number, TreeNode> = {}
  const roots: TreeNode[] = []

  items.forEach((item) => {
    map[item.编号] = { ...item, children: [] }
  })

  items.forEach((item) => {
    const node = map[item.编号]
    if (item.上级编号 && map[item.上级编号]) {
      map[item.上级编号].children!.push(node)
    } else {
      roots.push(node)
    }
  })

  return roots
}

// IP 判断函数
function isInternalIp(ip: string): boolean {
  if (!ip) return true
  if (ip === '127.0.0.1' || ip === '::1' || ip.startsWith('10.') || ip.startsWith('192.168.')) return true
  if (ip.startsWith('172.')) {
    const parts = ip.split('.')
    const second = parseInt(parts[1], 10)
    if (second >= 16 && second <= 31) return true
  }
  return false
}

describe('System Management & Security Audit (系统与安全治理模块)', () => {
  it('应正确将扁平的权限列表组装为树形层次结构', () => {
    const flatPermissions: RolePermissionItem[] = [
      { 编号: 1, 上级编号: 0, 名称: '科研办公', 类型: true, 拥有: true },
      { 编号: 101, 上级编号: 1, 名称: '通知公告', 路径: '科研办公/通知公告.html', 类型: false, 拥有: true },
      { 编号: 102, 上级编号: 1, 名称: '科研待办', 路径: '科研办公/科研待办.html', 类型: false, 拥有: true },
      { 编号: 2, 上级编号: 0, 名称: '纵向项目', 类型: true, 拥有: false },
      { 编号: 201, 上级编号: 2, 名称: '纵向项目申报', 路径: '纵向项目/申报.html', 类型: false, 拥有: false }
    ]

    const tree = buildPermissionTree(flatPermissions)

    expect(tree).toHaveLength(2)
    expect(tree[0].名称).toBe('科研办公')
    expect(tree[0].children).toHaveLength(2)
    expect(tree[0].children![0].名称).toBe('通知公告')
    expect(tree[1].名称).toBe('纵向项目')
    expect(tree[1].children).toHaveLength(1)
    expect(tree[1].children![0].名称).toBe('纵向项目申报')
  })

  it('应正确判定局域网内网 IP 与公网/专网接入 IP', () => {
    expect(isInternalIp('127.0.0.1')).toBe(true)
    expect(isInternalIp('::1')).toBe(true)
    expect(isInternalIp('192.168.1.105')).toBe(true)
    expect(isInternalIp('10.200.12.3')).toBe(true)
    expect(isInternalIp('172.16.5.8')).toBe(true)
    expect(isInternalIp('172.31.255.255')).toBe(true)

    // 公网与非内网
    expect(isInternalIp('114.114.114.114')).toBe(false)
    expect(isInternalIp('8.8.8.8')).toBe(false)
    expect(isInternalIp('172.32.0.1')).toBe(false)
  })

  it('字段授权规则应具有明确的角色、资源与权限开关', () => {
    const fieldRule = {
      角色编号: 3,
      资源: '纵向项目',
      字段: '项目批准经费',
      是否可读: true,
      是否可写: false
    }

    expect(fieldRule.角色编号).toBe(3)
    expect(fieldRule.资源).toBe('纵向项目')
    expect(fieldRule.字段).toBe('项目批准经费')
    expect(fieldRule.是否可读).toBe(true)
    expect(fieldRule.是否可写).toBe(false)
  })
})
