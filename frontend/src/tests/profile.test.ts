import { describe, it, expect } from 'vitest'

describe('Personal Profile & Security Settings (个人中心与账号安全)', () => {
  it('修改密码两次输入必须严格一致且满足最小长度要求', () => {
    function validatePasswordMatch(p1: string, p2: string) {
      if (p1.length < 6) return false
      return p1 === p2
    }

    expect(validatePasswordMatch('Pass1234', 'Pass1234')).toBe(true)
    expect(validatePasswordMatch('Pass1234', 'DifferentPass')).toBe(false)
    expect(validatePasswordMatch('123', '123')).toBe(false)
  })

  it('更新基本资料载荷应正确保留已修改字段', () => {
    const original = {
      编号: 10,
      手机: '13800000000',
      邮箱: 'old@hospital.org'
    }

    const updated = {
      ...original,
      手机: '13911112222',
      学术特长: '恶性肿瘤靶向治疗'
    }

    expect(updated.编号).toBe(10)
    expect(updated.手机).toBe('13911112222')
    expect(updated.学术特长).toBe('恶性肿瘤靶向治疗')
    expect(updated.邮箱).toBe('old@hospital.org')
  })
})
