using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ScientificResearch.Infrastucture;

namespace ScientificResearch.Controllers
{
    /// <summary>
    /// 期刊分区库与论文期刊自动分区匹配管理
    /// </summary>
    public sealed class JournalPartitionController : BaseController
    {
        private async Task<bool> IsManager() => await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办");

        // The same statement is executed by the rollback regression test. Keep
        // paper metadata joined to the actual journal table, not a second model.
        internal const string BatchMatchSql = @"
WITH bounds AS (
    SELECT COALESCE(@截止论文编号, MAX(p.""编号""), 0) AS last_id
      FROM ""成果论文"" p
     WHERE (@年度 IS NULL OR COALESCE(p.""年度"", EXTRACT(YEAR FROM COALESCE(p.""论文正式出版日期"", p.""论文Online日期""))::integer)=@年度)
), papers AS MATERIALIZED (
    SELECT p.""编号"", j.""ISSN号"" AS issn,
           COALESCE(NULLIF(btrim(j.""名称""), ''), j.""英文刊名"") AS journal_name,
           COALESCE(p.""年度"", EXTRACT(YEAR FROM COALESCE(p.""论文正式出版日期"", p.""论文Online日期""))::integer) AS paper_year
      FROM ""成果论文"" p
      LEFT JOIN ""期刊"" j ON j.""编号""=p.""刊物编号""
     WHERE p.""编号"">@起始论文编号 AND p.""编号""<=(SELECT last_id FROM bounds)
       AND (@年度 IS NULL OR COALESCE(p.""年度"", EXTRACT(YEAR FROM COALESCE(p.""论文正式出版日期"", p.""论文Online日期""))::integer)=@年度)
     ORDER BY p.""编号""
     LIMIT 100
), inserted AS (
    INSERT INTO ""论文期刊分区匹配"" (""论文编号"", ""来源"", ""年度"", ""分区"", ""影响因子"", ""匹配方式"", ""确认状态"")
    SELECT p.""编号"", m.""来源"", m.""年度"", m.""分区"", m.""影响因子"", m.""匹配方式"", '待确认'
      FROM papers p
      CROSS JOIN LATERAL ""fn_匹配期刊分区""(p.issn::varchar, p.journal_name::varchar, p.paper_year) m
     WHERE NOT EXISTS (
         SELECT 1 FROM ""论文期刊分区匹配"" existing
          WHERE existing.""论文编号""=p.""编号"" AND existing.""来源""=m.""来源"" AND existing.""年度""=m.""年度""
     )
    RETURNING ""编号""
)
SELECT (SELECT count(*)::integer FROM papers) AS ""处理论文数"",
       (SELECT count(*)::integer FROM inserted) AS ""产生匹配数"",
       COALESCE((SELECT MAX(""编号"") FROM papers), @起始论文编号) AS ""下一起始论文编号"",
       (SELECT last_id FROM bounds) AS ""截止论文编号"",
       EXISTS (
           SELECT 1 FROM ""成果论文"" p
            WHERE p.""编号"">COALESCE((SELECT MAX(""编号"") FROM papers), @起始论文编号)
              AND p.""编号""<=(SELECT last_id FROM bounds)
              AND (@年度 IS NULL OR COALESCE(p.""年度"", EXTRACT(YEAR FROM COALESCE(p.""论文正式出版日期"", p.""论文Online日期""))::integer)=@年度)
       ) AS ""是否还有论文"";";

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
            if (model == null || (model.年度.HasValue && (model.年度 < 1900 || model.年度 > 9999)) ||
                model.起始论文编号 < 0 || model.截止论文编号 < 0 || model.截止论文编号 < model.起始论文编号)
                return BadRequest(new { error = "请提供有效的年度和论文批次范围" });

            await using var connection = new NpgsqlConnection(DbConnectionString);
            await connection.OpenAsync(HttpContext.RequestAborted);
            await using var transaction = await connection.BeginTransactionAsync(HttpContext.RequestAborted);
            // Serialize this writer before reading recommendations. Repeated or
            // concurrent requests must not duplicate rows or overwrite reviews.
            await connection.ExecuteAsync(new CommandDefinition("SELECT pg_advisory_xact_lock(104, 217);",
                transaction: transaction, cancellationToken: HttpContext.RequestAborted));
            var result = await connection.QuerySingleAsync<BatchPaperPartitionMatchResult>(new CommandDefinition(
                BatchMatchSql, new { model.年度, model.起始论文编号, model.截止论文编号 }, transaction,
                cancellationToken: HttpContext.RequestAborted));
            await transaction.CommitAsync(HttpContext.RequestAborted);
            return result;
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
        public int 起始论文编号 { get; set; }
        public int? 截止论文编号 { get; set; }
    }

    public sealed class BatchPaperPartitionMatchResult
    {
        public int 处理论文数 { get; set; }
        public int 产生匹配数 { get; set; }
        public int 下一起始论文编号 { get; set; }
        public int 截止论文编号 { get; set; }
        public bool 是否还有论文 { get; set; }
    }
}
