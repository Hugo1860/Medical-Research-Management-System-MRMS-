import { describe, it, expect } from 'vitest'
import { formatMoney, formatDate, formatDateTime } from '@/utils/format'

describe('Format Utility (通用财务与日期格式化)', () => {
  it('正确格式化金额', () => {
    expect(formatMoney(0)).toBe('0.00')
    expect(formatMoney(1000)).toBe('1,000.00')
    expect(formatMoney(1234567.89)).toBe('1,234,567.89')
    expect(formatMoney('50000')).toBe('50,000.00')
    expect(formatMoney(null)).toBe('0.00')
    expect(formatMoney(undefined)).toBe('0.00')
  })

  it('正确格式化日期', () => {
    expect(formatDate('2026-10-15T00:00:00Z')).toBe('2026-10-15')
    expect(formatDate(null)).toBe('-')
    expect(formatDate(undefined)).toBe('-')
  })

  it('正确格式化日期时间', () => {
    const date = new Date(2026, 9, 15, 8, 30, 0)
    expect(formatDateTime(date)).toBe('2026-10-15 08:30:00')
  })
})
