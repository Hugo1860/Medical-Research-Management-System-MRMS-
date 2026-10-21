/**
 * 通用 CSV / 表格数据导出工具
 * 自动添加 UTF-8 BOM 避免 Excel 打开中文乱码
 */

export interface ExportColumn {
  label: string
  prop: string
  formatter?: (val: any, row: any) => string | number
}

export function exportToCsv(
  filename: string,
  columns: ExportColumn[],
  data: Record<string, any>[]
) {
  if (!data || !data.length) {
    throw new Error('没有可导出的数据')
  }

  const headerRow = columns.map(col => `"${col.label.replace(/"/g, '""')}"`).join(',')
  
  const contentRows = data.map(row => {
    return columns.map(col => {
      let val = row[col.prop]
      if (col.formatter) {
        val = col.formatter(val, row)
      }
      if (val === null || val === undefined) {
        return '""'
      }
      const strVal = String(val).replace(/"/g, '""')
      return `"${strVal}"`
    }).join(',')
  })

  // \uFEFF 是 UTF-8 的 Byte Order Mark (BOM)，使 Excel 能正确用 UTF-8 识别中文字符
  const csvContent = '\uFEFF' + [headerRow, ...contentRows].join('\r\n')
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  
  const link = document.createElement('a')
  link.setAttribute('href', url)
  link.setAttribute('download', `${filename}_${new Date().toISOString().slice(0, 10)}.csv`)
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}
