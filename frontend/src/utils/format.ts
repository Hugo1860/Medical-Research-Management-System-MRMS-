import dayjs from 'dayjs'

/**
 * 格式化金额为标准中文财务记账格式 (¥ 123,456.78)
 */
export function formatMoney(val: number | string | undefined | null): string {
  if (val === undefined || val === null || val === '') return '0.00'
  const num = Number(val)
  if (isNaN(num)) return '0.00'
  return num.toLocaleString('zh-CN', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  })
}

/**
 * 格式化日期为 YYYY-MM-DD
 */
export function formatDate(val: string | number | Date | undefined | null): string {
  if (!val) return '-'
  return dayjs(val).format('YYYY-MM-DD')
}

/**
 * 格式化日期时间为 YYYY-MM-DD HH:mm:ss
 */
export function formatDateTime(val: string | number | Date | undefined | null): string {
  if (!val) return '-'
  return dayjs(val).format('YYYY-MM-DD HH:mm:ss')
}
