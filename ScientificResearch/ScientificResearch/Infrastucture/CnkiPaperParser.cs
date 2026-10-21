using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using OfficeOpenXml;
using ScientificResearch.Models;

namespace ScientificResearch.Infrastucture
{
    /// <summary>
    /// 解析中国知网 (CNKI) 导出的论文文件（包括伪装成 .xls 的 HTML 格式表格以及标准 .xlsx 格式文件）
    /// </summary>
    public static class CnkiPaperParser
    {
        private static readonly Regex TrRegex = new(@"<tr[^>]*>(.*?)</tr>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex TdRegex = new(@"<t[dh][^>]*>(.*?)</t[dh]>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex HtmlTagRegex = new(@"<[^>]+>", RegexOptions.Compiled);

        public static List<论文导入> Parse(string filePath, string worksheetName = null)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"待解析的知网论文文件不存在: {filePath}");
            }

            // 读取文件前几个字节判断格式
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var buffer = new byte[Math.Min(4096, (int)stream.Length)];
                var readBytes = stream.Read(buffer, 0, buffer.Length);
                var headerSnippet = Encoding.UTF8.GetString(buffer, 0, readBytes);

                if (headerSnippet.IndexOf("<html", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    headerSnippet.IndexOf("<table", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    headerSnippet.IndexOf("schemas-microsoft-com:office", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return ParseHtmlTable(filePath);
                }
            }

            // 尝试以标准 OpenXML Excel 格式解析
            try
            {
                return ParseExcel(filePath, worksheetName);
            }
            catch
            {
                // 若 EPPlus 打开失败，最后再尝试作为 HTML 容错解析
                return ParseHtmlTable(filePath);
            }
        }

        private static List<论文导入> ParseHtmlTable(string filePath)
        {
            string content;
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                content = reader.ReadToEnd();
            }

            // 检查编码声明
            var metaMatch = Regex.Match(content, @"charset=([a-zA-Z0-9\-]+)", RegexOptions.IgnoreCase);
            if (metaMatch.Success)
            {
                var charset = metaMatch.Groups[1].Value.Trim().ToLowerInvariant();
                if (charset is "gb2312" or "gbk" or "gb18030")
                {
                    try
                    {
                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        content = File.ReadAllText(filePath, Encoding.GetEncoding(charset));
                    }
                    catch
                    {
                        // 降级使用 UTF-8
                    }
                }
            }

            var trMatches = TrRegex.Matches(content);
            if (trMatches.Count == 0)
            {
                return new List<论文导入>();
            }

            var headerList = new List<string>();
            int dataStartIdx = -1;

            for (int i = 0; i < trMatches.Count; i++)
            {
                var tdMatches = TdRegex.Matches(trMatches[i].Groups[1].Value);
                if (tdMatches.Count == 0) continue;

                var cells = tdMatches.Select(m => CleanCellText(m.Groups[1].Value)).ToList();
                if (cells.Any(c => c.Contains("题名") || c.Contains("Title") || c.Contains("作者") || c.Contains("Author")))
                {
                    headerList = cells;
                    dataStartIdx = i + 1;
                    break;
                }
            }

            if (dataStartIdx == -1 || headerList.Count == 0)
            {
                var tdMatches = TdRegex.Matches(trMatches[0].Groups[1].Value);
                headerList = tdMatches.Select(m => CleanCellText(m.Groups[1].Value)).ToList();
                dataStartIdx = 1;
            }

            var results = new List<论文导入>();
            for (int i = dataStartIdx; i < trMatches.Count; i++)
            {
                var tdMatches = TdRegex.Matches(trMatches[i].Groups[1].Value);
                if (tdMatches.Count == 0) continue;

                var rowValues = tdMatches.Select(m => CleanCellText(m.Groups[1].Value)).ToList();
                if (rowValues.All(string.IsNullOrWhiteSpace)) continue;

                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int c = 0; c < Math.Min(headerList.Count, rowValues.Count); c++)
                {
                    dict[headerList[c]] = rowValues[c];
                }

                var item = MapRowToModel(dict);
                if (!string.IsNullOrWhiteSpace(item.论文标题))
                {
                    results.Add(item);
                }
            }

            return results;
        }

        private static List<论文导入> ParseExcel(string filePath, string worksheetName)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage(new FileInfo(filePath));
            if (package.Workbook.Worksheets.Count == 0)
            {
                return new List<论文导入>();
            }

            ExcelWorksheet sheet = null;
            if (!string.IsNullOrWhiteSpace(worksheetName))
            {
                sheet = package.Workbook.Worksheets[worksheetName];
            }
            sheet ??= package.Workbook.Worksheets[0];

            if (sheet.Dimension == null || sheet.Dimension.End.Row < 2)
            {
                return new List<论文导入>();
            }

            var headerMap = new Dictionary<int, string>();
            for (int col = sheet.Dimension.Start.Column; col <= sheet.Dimension.End.Column; col++)
            {
                var h = sheet.Cells[1, col].Text?.Trim();
                if (!string.IsNullOrEmpty(h))
                {
                    headerMap[col] = h;
                }
            }

            var results = new List<论文导入>();
            for (int row = 2; row <= sheet.Dimension.End.Row; row++)
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in headerMap)
                {
                    var val = sheet.Cells[row, kvp.Key].Text?.Trim();
                    dict[kvp.Value] = val ?? string.Empty;
                }

                if (dict.Values.All(string.IsNullOrWhiteSpace)) continue;

                var item = MapRowToModel(dict);
                if (!string.IsNullOrWhiteSpace(item.论文标题))
                {
                    results.Add(item);
                }
            }

            return results;
        }

        private static string CleanCellText(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            var stripped = HtmlTagRegex.Replace(raw, "");
            return WebUtility.HtmlDecode(stripped).Trim();
        }

        private static string FindValue(Dictionary<string, string> dict, params string[] candidateKeys)
        {
            foreach (var key in candidateKeys)
            {
                if (dict.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                {
                    return val.Trim();
                }

                var matched = dict.FirstOrDefault(kv => kv.Key.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0);
                if (!string.IsNullOrWhiteSpace(matched.Value))
                {
                    return matched.Value.Trim();
                }
            }
            return null;
        }

        private static 论文导入 MapRowToModel(Dictionary<string, string> dict)
        {
            var item = new 论文导入();

            // 1. 论文标题
            item.论文标题 = FindValue(dict, "Title-题名", "Title", "题名", "论文标题");

            // 2. 刊物名称
            item.刊物名称 = FindValue(dict, "Source-文献来源", "Source", "文献来源", "刊物名称", "Journal", "期刊");

            // 3. 作者
            var rawAuthor = FindValue(dict, "Author-作者", "Author", "作者", "第一作者");
            if (!string.IsNullOrWhiteSpace(rawAuthor))
            {
                // 去除结尾句点、逗号、分号等标点
                var cleaned = rawAuthor.TrimEnd('.', '。', ';', '；', ',', '，');
                var parts = Regex.Split(cleaned, @"[;,，；\r\n]+")
                                 .Select(p => p.Trim())
                                 .Where(p => !string.IsNullOrEmpty(p))
                                 .ToList();

                item.作者 = parts.FirstOrDefault() ?? string.Empty;
                item.作者人数 = Math.Max(1, parts.Count);
            }
            else
            {
                item.作者 = string.Empty;
                item.作者人数 = 1;
            }

            // 4. 年度
            var yearStr = FindValue(dict, "Year-年", "Year", "年", "年度", "出版年度");
            if (!string.IsNullOrWhiteSpace(yearStr) && int.TryParse(yearStr, out var yr))
            {
                item.年度 = yr;
            }

            // 5. 卷号
            var volStr = FindValue(dict, "Roll-卷", "Roll", "卷", "卷号", "Volume");
            if (!string.IsNullOrWhiteSpace(volStr) && int.TryParse(volStr, out var vol))
            {
                item.卷号 = vol;
            }

            // 6. 期号
            var periodStr = FindValue(dict, "Period-期", "Period", "期", "期号", "Issue");
            if (!string.IsNullOrWhiteSpace(periodStr) && int.TryParse(periodStr, out var per))
            {
                item.期号 = per;
            }

            // 7. 页码范围
            var pagesStr = FindValue(dict, "PageCount-页码", "PageCount", "页码", "页码范围", "Pages");
            item.页码范围 = pagesStr;
            if (!string.IsNullOrWhiteSpace(pagesStr))
            {
                var pageParts = Regex.Split(pagesStr, @"[-~～]");
                if (pageParts.Length > 1)
                {
                    if (int.TryParse(pageParts[0].Trim(), out var fromPage)) item.页码范围起 = fromPage;
                    if (int.TryParse(pageParts[1].Trim(), out var toPage)) item.页码范围止 = toPage;
                }
                else if (int.TryParse(pageParts[0].Trim(), out var singlePage))
                {
                    item.页码范围起 = singlePage;
                    item.页码范围止 = singlePage;
                }
            }

            // 8. 关键字
            var kwStr = FindValue(dict, "关键词", "Keywords", "关键字");
            if (!string.IsNullOrWhiteSpace(kwStr))
            {
                item.关键字 = kwStr.Replace(";;", "; ").Trim();
            }

            // 9. 论文摘要（做安全长度截断保护）
            var absStr = FindValue(dict, "摘要", "Abstract", "论文摘要");
            item.论文摘要 = absStr != null && absStr.Length > 1000 ? absStr.Substring(0, 1000) : absStr;

            return item;
        }
    }
}
