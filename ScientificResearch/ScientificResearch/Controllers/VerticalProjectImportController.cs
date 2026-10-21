using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ScientificResearch.Infrastucture;

namespace ScientificResearch.Controllers
{
    /// <summary>
    /// 纵向项目 PDF 计划书解析、校验与批量立项导入
    /// </summary>
    public sealed class VerticalProjectImportController : BaseController
    {
        private async Task<bool> IsManager() => await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办");

        [HttpGet]
        public async Task<object> 查询导入批次()
        {
            return await Db.QueryAsync<dynamic>(@"
SELECT b.*, u.""姓名"" as ""导入人姓名""
FROM ""纵向项目导入批次"" b
JOIN ""人员"" u ON b.""导入人编号"" = u.""编号""
ORDER BY b.""创建时间"" DESC;");
        }

        [HttpGet]
        public async Task<object> 查询导入明细(long 批次编号)
        {
            return await Db.QueryAsync<dynamic>(@"
SELECT * FROM ""纵向项目导入明细""
WHERE ""批次编号""=@批次编号
ORDER BY ""编号"";", new { 批次编号 });
        }

        [HttpPost]
        public async Task<object> 创建导入批次([FromBody] CreateImportBatchReq model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权创建导入批次" });
            if (model == null || string.IsNullOrWhiteSpace(model.批次名称))
                throw new ArgumentException("批次名称不能为空");

            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""纵向项目导入批次"" (""批次名称"",""来源类型"",""状态"",""导入人编号"")
VALUES (@批次名称, @来源类型, '待校验', @导入人编号)
RETURNING ""编号"";", new {
                model.批次名称,
                来源类型 = string.IsNullOrWhiteSpace(model.来源类型) ? "国家自然科学基金" : model.来源类型.Trim(),
                导入人编号 = CurrentUser.编号
            });

            return new { 编号 = id };
        }

        [HttpPost]
        public async Task<object> 上传计划书(long 批次编号, List<IFormFile> files)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权上传导入文件" });
            if (files == null || !files.Any()) throw new ArgumentException("未检测到上传文件");

            var batch = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT * FROM ""纵向项目导入批次"" WHERE ""编号""=@批次编号;", new { 批次编号 });
            if (batch == null) return NotFound(new { error = "导入批次不存在" });

            string uploadDir = Path.Combine(Env.WebRootPath ?? "wwwroot", "upload", "纵向项目导入", 批次编号.ToString());
            if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

            int success = 0;
            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                byte[] buffer = new byte[file.Length];
                using (var stream = file.OpenReadStream())
                {
                    await stream.ReadExactlyAsync(buffer, 0, (int)file.Length);
                }

                string hash;
                using (var sha = SHA256.Create())
                {
                    hash = BitConverter.ToString(sha.ComputeHash(buffer)).Replace("-", "").ToLowerInvariant();
                }

                string savedPath = Path.Combine(uploadDir, $"{hash}.pdf");
                await System.IO.File.WriteAllBytesAsync(savedPath, buffer);

                // 提取文本与正则容错抽取
                string rawText = System.Text.Encoding.UTF8.GetString(buffer); // 基础流提取
                var extracted = ParseProposalText(rawText);

                // 校验规则：批准号、项目名称、负责人
                var validation = new Dictionary<string, string>();
                string checkStatus = "通过";

                if (string.IsNullOrWhiteSpace(extracted.项目名称))
                {
                    validation["项目名称"] = "缺失项目名称";
                    checkStatus = "失败";
                }
                if (string.IsNullOrWhiteSpace(extracted.批准号))
                {
                    validation["批准号"] = "缺失项目批准号";
                    checkStatus = "警告";
                }
                else
                {
                    // 查重：纵向项目立项表是否存在该项目编号
                    var dup = await Db.QuerySingleOrDefaultAsync<int>(@"
SELECT COUNT(1) FROM ""纵向项目立项"" WHERE ""项目编号""=@code;", new { code = extracted.批准号 });
                    if (dup > 0)
                    {
                        validation["查重结果"] = "该批准号已在立项库中登记";
                        checkStatus = "警告";
                    }
                }

                await Db.ExecuteAsync(@"
INSERT INTO ""纵向项目导入明细"" (""批次编号"",""源文件名"",""文件存储路径"",""文件哈希"",""解析状态"",""文本摘要"",""解析字段"",""校验结果"",""校验状态"",""处理状态"")
VALUES (@批次编号, @fileName, @savedPath, @hash, '已解析', @summary, @fields::jsonb, @val::jsonb, @checkStatus, '待处理')
ON CONFLICT (""批次编号"", ""文件哈希"") DO UPDATE SET
    ""解析字段""=EXCLUDED.""解析字段"",
    ""校验结果""=EXCLUDED.""校验结果"",
    ""校验状态""=EXCLUDED.""校验状态"";", new {
                    批次编号,
                    fileName = file.FileName,
                    savedPath,
                    hash,
                    summary = rawText.Length > 2000 ? rawText.Substring(0, 2000) : rawText,
                    fields = JsonSerializer.Serialize(extracted),
                    val = JsonSerializer.Serialize(validation),
                    checkStatus
                });

                success++;
            }

            await Db.ExecuteAsync(@"
UPDATE ""纵向项目导入批次""
SET ""总文件数""=""总文件数"" + @success, ""解析成功数""=""解析成功数"" + @success, ""更新时间""=CURRENT_TIMESTAMP
WHERE ""编号""=@批次编号;", new { success, 批次编号 });

            return new { 批次编号, 处理文件数 = success };
        }

        [HttpPost]
        public async Task<object> 确认批量导入([FromBody] ConfirmImportReq model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权执行立项批量导入" });

            var items = (await Db.QueryAsync<dynamic>(@"
SELECT * FROM ""纵向项目导入明细""
WHERE ""批次编号""=@批次编号 AND ""校验状态"" IN ('通过','警告') AND ""处理状态""='待处理';", new { model.批次编号 })).ToList();

            int imported = 0;
            foreach (var item in items)
            {
                var fields = JsonSerializer.Deserialize<ExtractedProposalFields>((string)item.解析字段);
                if (fields == null) continue;

                // 写入纵向项目立项库
                var projId = await Db.QuerySingleAsync<int>(@"
INSERT INTO ""纵向项目立项"" (
    ""分类"", ""项目编号"", ""项目中文名称"", ""立项年度"", ""登记日期"", ""登记人编号"", ""批准日期"",
    ""批准经费"", ""计划开始日期"", ""计划完成日期"", ""学科门类"", ""学科类型"", ""研究类型"",
    ""项目类型"", ""项目级别"", ""项目来源单位""
) VALUES (
    1, @code, @name, @year, CURRENT_DATE, @userId, CURRENT_DATE,
    @fund, @start, @end, '医学', '临床医学', '应用研究',
    '国家级项目', '国家级', '国家自然科学基金委员会'
) RETURNING ""编号"";", new {
                    code = string.IsNullOrWhiteSpace(fields.批准号) ? "NSFC-" + DateTime.Now.ToString("yyyyMMddHHmmss") : fields.批准号,
                    name = string.IsNullOrWhiteSpace(fields.项目名称) ? item.源文件名 : fields.项目名称,
                    year = fields.年度 > 0 ? fields.年度 : DateTime.Now.Year,
                    userId = CurrentUser.编号,
                    fund = fields.资助经费万元 * 10000m,
                    start = fields.开始日期 ?? DateTime.Now.Date,
                    end = fields.结束日期 ?? DateTime.Now.Date.AddYears(3)
                });

                await Db.ExecuteAsync(@"
UPDATE ""纵向项目导入明细""
SET ""处理状态""='已导入', ""导入纵向项目编号""=@projId
WHERE ""编号""=@itemId;", new { projId, itemId = (long)item.编号 });

                imported++;
            }

            await Db.ExecuteAsync(@"
UPDATE ""纵向项目导入批次""
SET ""状态""='已导入', ""导入成功数""=@imported, ""更新时间""=CURRENT_TIMESTAMP
WHERE ""编号""=@批次编号;", new { imported, model.批次编号 });

            return new { 批次编号 = model.批次编号, 成功导入项目数 = imported };
        }

        private static ExtractedProposalFields ParseProposalText(string text)
        {
            var res = new ExtractedProposalFields();
            if (string.IsNullOrWhiteSpace(text)) return res;

            var mName = Regex.Match(text, @"项目名称[：:\s]*([^\r\n]+)");
            if (mName.Success) res.项目名称 = mName.Groups[1].Value.Trim();

            var mLeader = Regex.Match(text, @"(负责人|申请人)[：:\s]*([^\r\n\s]+)");
            if (mLeader.Success) res.负责人 = mLeader.Groups[2].Value.Trim();

            var mCode = Regex.Match(text, @"(批准号|资助号|项目编号)[：:\s]*([A-Za-z0-9]+)");
            if (mCode.Success) res.批准号 = mCode.Groups[2].Value.Trim();

            var mFund = Regex.Match(text, @"(直接费用|资助经费|批准经费)[：:\s]*([0-9\.]+)\s*万?");
            if (mFund.Success && decimal.TryParse(mFund.Groups[2].Value, out decimal amt))
            {
                res.资助经费万元 = amt;
            }

            var mYear = Regex.Match(text, @"(20\d\d)年度");
            if (mYear.Success && int.TryParse(mYear.Groups[1].Value, out int yr))
            {
                res.年度 = yr;
            }

            return res;
        }
    }

    public sealed class CreateImportBatchReq
    {
        public string 批次名称 { get; set; }
        public string 来源类型 { get; set; } = "国家自然科学基金";
    }

    public sealed class ConfirmImportReq
    {
        public long 批次编号 { get; set; }
    }

    public sealed class ExtractedProposalFields
    {
        public string 项目名称 { get; set; }
        public string 负责人 { get; set; }
        public string 依托单位 { get; set; }
        public string 批准号 { get; set; }
        public int 年度 { get; set; }
        public decimal 资助经费万元 { get; set; }
        public DateTime? 开始日期 { get; set; }
        public DateTime? 结束日期 { get; set; }
    }
}
