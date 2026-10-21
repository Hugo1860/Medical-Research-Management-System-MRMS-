using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using ScientificResearch.Infrastucture;

namespace ScientificResearch.Controllers
{
    /// <summary>
    /// 期刊分区库与论文期刊自动分区匹配管理
    /// </summary>
    public sealed class JournalPartitionController : BaseController
    {
        private async Task<bool> IsManager() => await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办");

        [HttpGet]
        public async Task<object> 查询期刊分区(string 来源 = null, int? 年度 = null, string 关键词 = null)
        {
            var list = await Db.QueryAsync<dynamic>(@"
SELECT * FROM ""期刊分区库""
WHERE (@来源 IS NULL OR ""来源""=@来源)
  AND (@年度 IS NULL OR ""年度""=@年度)
  AND (@关键词 IS NULL OR ""期刊名称"" ILIKE '%' || @关键词 || '%' OR ""ISSN"" ILIKE '%' || @关键词 || '%')
ORDER BY ""年度"" DESC, ""影响因子"" DESC NULLS LAST
LIMIT 100;", new { 来源, 年度, 关键词 });

            return list;
        }

        [HttpPost]
        public async Task<object> 保存期刊分区条目([FromBody] JournalPartitionSaveModel model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权维护期刊分区库" });
            if (model == null || string.IsNullOrWhiteSpace(model.来源) || string.IsNullOrWhiteSpace(model.期刊名称) || model.年度 <= 0)
                throw new ArgumentException("来源、期刊名称和年度为必填项");

            var norm = NormalizeJournalName(model.期刊名称);
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""期刊分区库"" (""来源"",""年度"",""期刊名称"",""规范化名称"",""ISSN"",""eISSN"",""学科类别"",""分区"",""影响因子"")
VALUES (@来源,@年度,@期刊名称,@norm,@ISSN,@eISSN,@学科类别,@分区,@影响因子)
ON CONFLICT (""来源"", ""年度"", ""规范化名称"", ""学科类别"") DO UPDATE SET
    ""期刊名称""=EXCLUDED.""期刊名称"",
    ""ISSN""=COALESCE(EXCLUDED.""ISSN"", ""期刊分区库"".""ISSN""),
    ""eISSN""=COALESCE(EXCLUDED.""eISSN"", ""期刊分区库"".""eISSN""),
    ""分区""=EXCLUDED.""分区"",
    ""影响因子""=EXCLUDED.""影响因子""
RETURNING ""编号"";", new {
                来源 = model.来源.Trim(),
                model.年度,
                期刊名称 = model.期刊名称.Trim(),
                norm,
                ISSN = string.IsNullOrWhiteSpace(model.ISSN) ? null : model.ISSN.Trim(),
                eISSN = string.IsNullOrWhiteSpace(model.eISSN) ? null : model.eISSN.Trim(),
                学科类别 = string.IsNullOrWhiteSpace(model.学科类别) ? "综合" : model.学科类别.Trim(),
                分区 = string.IsNullOrWhiteSpace(model.分区) ? "Q1" : model.分区.Trim(),
                model.影响因子
            });

            return new { 编号 = id };
        }

        [HttpGet]
        public async Task<object> 智能匹配期刊分区(string issn, string 期刊名称, int? 年度)
        {
            if (string.IsNullOrWhiteSpace(issn) && string.IsNullOrWhiteSpace(期刊名称))
                throw new ArgumentException("ISSN 或期刊名称至少提供一项");

            var matches = await Db.QueryAsync<dynamic>(@"
SELECT * FROM ""fn_匹配期刊分区""(@issn, @期刊名称, @年度);", new { issn, 期刊名称, 年度 });

            return matches;
        }

        [HttpPost]
        public async Task<object> 批量匹配论文分区([FromBody] BatchPaperPartitionMatchReq model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权执行论文批量匹配" });

            // 查询待匹配论文列表
            var papers = (await Db.QueryAsync<dynamic>(@"
SELECT ""编号"", ""刊物名称"", ""ISSN"", ""出版年度""
FROM ""论文""
WHERE (@年度 IS NULL OR ""出版年度""=@年度)
LIMIT 100;", new { model.年度 })).ToList();

            int matchedCount = 0;
            foreach (var p in papers)
            {
                var matches = (await Db.QueryAsync<dynamic>(@"
SELECT * FROM ""fn_匹配期刊分区""(@issn, @期刊名称, @年度);", new {
                    issn = (string)p.ISSN,
                    期刊名称 = (string)p.刊物名称,
                    年度 = (int?)p.出版年度
                })).ToList();

                foreach (var m in matches)
                {
                    await Db.ExecuteAsync(@"
INSERT INTO ""论文期刊分区匹配"" (""论文编号"",""来源"",""年度"",""分区"",""影响因子"",""匹配方式"",""确认状态"")
VALUES (@paperId, @来源, @年度, @分区, @影响因子, @匹配方式, '待确认');", new {
                        paperId = (int)p.编号,
                        来源 = (string)m.来源,
                        年度 = (int)m.年度,
                        分区 = (string)m.分区,
                        影响因子 = (decimal?)m.影响因子,
                        匹配方式 = (string)m.匹配方式
                    });
                    matchedCount++;
                }
            }

            return new { 处理论文数 = papers.Count, 产生匹配数 = matchedCount };
        }

        private static string NormalizeJournalName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            return Regex.Replace(name.ToLowerInvariant(), @"[^\w\d\u4e00-\u9fa5]", "");
        }
    }

    public sealed class JournalPartitionSaveModel
    {
        public string 来源 { get; set; } = "中科院分区";
        public int 年度 { get; set; } = 2024;
        public string 期刊名称 { get; set; }
        public string ISSN { get; set; }
        public string eISSN { get; set; }
        public string 学科类别 { get; set; } = "综合";
        public string 分区 { get; set; } = "1区";
        public decimal? 影响因子 { get; set; }
    }

    public sealed class BatchPaperPartitionMatchReq
    {
        public int? 年度 { get; set; }
    }
}
