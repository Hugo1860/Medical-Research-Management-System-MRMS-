using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ScientificResearch.Controllers;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace ScientificResearch.Infrastucture;

/// <summary>Reads a PDF text layer; image-only documents require manual verification, not simulated OCR.</summary>
public static class ProposalPdfParser
{
    public const int MaxFileBytes = 20 * 1024 * 1024;
    public const int MaxPages = 200;
    public const int MaxTextLength = 500_000;

    public static string ExtractText(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 5 || bytes.Length > MaxFileBytes)
            throw new ArgumentException("PDF 文件不能为空且不能超过 20 MB");
        if (!bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8))
            throw new ArgumentException("文件内容不是有效的 PDF");
        using var document = PdfDocument.Open(bytes);
        if (document.NumberOfPages > MaxPages)
            throw new ArgumentException("计划书不能超过 200 页，请拆分后上传");
        var text = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            text.AppendLine(ContentOrderTextExtractor.GetText(page));
            if (text.Length > MaxTextLength)
                throw new ArgumentException("计划书文本过长，请拆分后上传");
        }
        return text.ToString().Replace("\0", "");
    }

    private static Match Match(string text, string pattern) => Regex.Match(text, pattern,
        RegexOptions.Multiline | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public static ExtractedProposalFields ParseText(string text)
    {
        var result = new ExtractedProposalFields();
        if (string.IsNullOrWhiteSpace(text)) return result;
        if (text.Length > MaxTextLength) throw new ArgumentException("计划书文本过长");
        string Field(string labels) => Match(text, @"(?:^|\n)[ \t]*(?:" + labels + @")[ \t]*[：:][ \t]*([^\r\n]+)").Groups[1].Value.Trim();
        result.项目名称 = Field("项目名称|项目中文名称");
        result.负责人 = Field("负责人|申请人|项目负责人");
        result.批准号 = Field("批准号|资助号|项目编号");
        result.依托单位 = Field("依托单位");
        result.学科门类 = Field("学科门类");
        result.学科类型 = Field("学科类型");
        result.研究类型 = Field("研究类型");
        result.项目类型 = Field("项目类型");
        result.项目级别 = Field("项目级别");
        result.项目来源单位 = Field("项目来源单位|资助单位");
        result.分类 = Field("分类") switch { "科研项目" => 1, "重点学科" => 2, "人才培养" => 3, _ => null };
        var year = Match(text, @"(?:(?:立项年度|批准年度|年度)[ \t]*[：:][ \t]*(20\d{2})|(20\d{2})[ \t]*年度)");
        if (int.TryParse(year.Groups[1].Success ? year.Groups[1].Value : year.Groups[2].Value, out var y)) result.年度 = y;
        // A missing unit must never silently multiply a yuan amount by 10,000.
        var money = Match(text, @"(?:直接费用|资助经费|批准经费)[ \t]*(?:[（(](万元|元)[）)])?[ \t]*[：:][ \t]*([0-9]+(?:,[0-9]{3})*(?:\.[0-9]+)?)[ \t]*(万元|万|元)?");
        var unit = money.Groups[3].Success ? money.Groups[3].Value : money.Groups[1].Value;
        if (!string.IsNullOrEmpty(unit) && decimal.TryParse(money.Groups[2].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            result.资助经费万元 = unit == "元" ? amount / 10000m : amount;
        DateTime? Date(string labels)
        {
            var value = Field(labels).Replace("年", "-").Replace("月", "-").Replace("日", "").Replace("/", "-").Replace(".", "-");
            return DateTime.TryParseExact(value, new[] { "yyyy-M-d", "yyyy-MM-dd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;
        }
        result.开始日期 = Date("计划开始日期|开始日期|起始日期");
        result.结束日期 = Date("计划完成日期|结束日期|终止日期");
        result.批准日期 = Date("批准日期|批准时间");
        return result;
    }

    public static Dictionary<string, string> Validate(ExtractedProposalFields fields)
    {
        var errors = new Dictionary<string, string>();
        if (fields == null) { errors["解析字段"] = "缺少计划书字段"; return errors; }
        if (string.IsNullOrWhiteSpace(fields.项目名称) || fields.项目名称.Length > 100) errors["项目名称"] = "项目名称必填，最多 100 字";
        if (string.IsNullOrWhiteSpace(fields.批准号) || fields.批准号.Length > 50) errors["批准号"] = "批准号必填，最多 50 字";
        if (string.IsNullOrWhiteSpace(fields.负责人) || fields.负责人.Length > 50) errors["负责人"] = "负责人必填，最多 50 字";
        if (fields.年度 < 1900 || fields.年度 > 2200) errors["年度"] = "请核实立项年度";
        if (!fields.资助经费万元.HasValue || fields.资助经费万元 < 0 || fields.资助经费万元 > 999999999999m)
            errors["资助经费万元"] = "请核实经费金额和单位（万元），允许明确填写 0";
        if (!fields.开始日期.HasValue || !fields.结束日期.HasValue || fields.结束日期 < fields.开始日期)
            errors["项目日期"] = "请填写真实的开始、结束日期，结束日期不能早于开始日期";
        if (!fields.批准日期.HasValue) errors["批准日期"] = "请填写原批件的批准日期";
        if (fields.分类 is not (1 or 2 or 3)) errors["分类"] = "请选择科研项目、重点学科或人才培养";
        foreach (var (name, value) in new[] {
            ("学科门类", fields.学科门类), ("学科类型", fields.学科类型), ("研究类型", fields.研究类型),
            ("项目类型", fields.项目类型), ("项目级别", fields.项目级别), ("项目来源单位", fields.项目来源单位) })
            if (string.IsNullOrWhiteSpace(value) || value.Length > 50) errors[name] = $"{name}必填，最多 50 字";
        return errors;
    }
}
