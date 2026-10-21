using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ScientificResearch.Business;
using ScientificResearch.Infrastucture;
using ScientificResearch.Models;

using Dapper;
using MyLib;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Data;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ScientificResearch.Controllers
{
    public class FruitController : BaseController
    {
        public int 论文管理流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:论文管理");
            }
        }

        public int 认领论文流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:认领论文");
            }
        }

        public int 著作管理流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:著作管理");
            }
        }

        public int 专利管理流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:专利管理");
            }
        }

        public int 获奖管理流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:获奖管理");
            }
        }

        public enum 成果类型
        {
            论文 = 1, 著作 = 2, 专利 = 3, 获奖 = 4
        }


        #region 论文管理

        /// <summary>
        /// 单独录入论文或认领,同时开启审核流程
        /// 批量导入的不会开启流程,只有在认领时才会
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 新增论文([FromBody]成果增改<成果论文> model)
        {
            //这里要主动给加上认领人
            model.基本资料.认领人编号 = CurrentUser.编号;
            //await 发起一个成果论文增改流程<成果论文>(model, 论文管理流程模板编号);
            await MyWorkFlowBusiness.发起流程(
                           论文管理流程模板编号,
                           new sp_成果论文_增改
                           {
                               tt = model.基本资料.ToDataTable(),
                               tt_成果项目 = model.相关项目.ToDataTable(),
                               tt_成果作者 = model.作者.ToDataTable()
                           },
                            CurrentUser.编号,
                            model.IsHold);
        }

        [HttpPost]
        async public Task<object> 导入知网论文()
        {
            if (Request.Form.Files == null || Request.Form.Files.Count == 0)
            {
                return BadRequest(new { error = "请选取待导入的知网论文文件" });
            }

            var filesNameList = await MyLib.UploadFile.Upload(
               Request.Form.Files,
               Env.WebRootPath,
               "upload/成果/论文/导入知网论文",
               config.GetValue<int>("uploadFileMaxSize"));

            var relativePath = filesNameList[0].TrimStart('/', '\\');
            var localPath = Path.Combine(Env.WebRootPath, relativePath);

            List<论文导入> rows;
            try
            {
                rows = CnkiPaperParser.Parse(localPath, config.GetValue<string>("知网论文excel文件worksheet名"));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"知网导出文件解析失败: {ex.Message}" });
            }

            var validRows = rows.Where(i => !string.IsNullOrWhiteSpace(i.论文标题)).ToList();
            if (validRows.Count == 0)
            {
                return BadRequest(new { error = "未在上传文件中解析到有效的论文数据，请确认是否为中国知网导出的文件" });
            }

            int importedCount = 0;
            try
            {
                var dt = ((IEnumerable<论文导入>)validRows).ToDataTable();
                importedCount = await Db.ExecuteSpAsync(new sp_导入论文() { tt = dt });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"论文入库保存异常: {ex.Message}", details = ex.ToString() });
            }

            return new
            {
                来源 = "CNKI导出文件",
                实时接口 = false,
                解析数量 = validRows.Count,
                导入数量 = importedCount,
                说明 = $"成功解析知网文件共 {validRows.Count} 篇论文，实际成功导入入库 {importedCount} 篇（重复论文已自动跳过）；系统未调用 CNKI 实时接口。万方接口尚未接入。"
            };
        }

        [HttpPost]
        async public Task<object> 导入自定义论文()
        {
            if (Request.Form.Files == null || Request.Form.Files.Count == 0)
            {
                return BadRequest(new { error = "请选取待导入的自定义论文文件" });
            }

            var filesNameList = await MyLib.UploadFile.Upload(
               Request.Form.Files,
               Env.WebRootPath,
               "upload/成果/论文/导入自定义论文",
               config.GetValue<int>("uploadFileMaxSize"));

            var relativePath = filesNameList[0].TrimStart('/', '\\');
            var localPath = Path.Combine(Env.WebRootPath, relativePath);

            IEnumerable<论文自定义导入> result;
            try
            {
                result = MyXls.Import<论文自定义导入>(localPath);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"自定义论文表格解析失败: {ex.Message}" });
            }

            var rows = result.Where(i => !string.IsNullOrWhiteSpace(i.刊物名称) && !string.IsNullOrWhiteSpace(i.论文标题)).ToList();

            foreach (var item in rows)
            {
                if (!string.IsNullOrWhiteSpace(item.页码范围))
                {
                    var pageFromTo = item.页码范围.Split('-');
                    if (pageFromTo.Length > 1)
                    {
                        item.页码范围起 = int.TryParse(pageFromTo[0].Trim(), out var from) ? (int?)from : null;
                        item.页码范围止 = int.TryParse(pageFromTo[1].Trim(), out var to) ? (int?)to : null;
                    }
                }
            }

            int importedCount = 0;
            try
            {
                var dt = ((IEnumerable<论文自定义导入>)rows).ToDataTable();
                importedCount = await Db.ExecuteSpAsync(new sp_导入自定义论文() { tt = dt });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"自定义论文入库保存异常: {ex.Message}", details = ex.ToString() });
            }

            return new
            {
                来源 = "自定义格式文件",
                解析数量 = rows.Count,
                导入数量 = importedCount,
                说明 = $"成功解析自定义格式共 {rows.Count} 篇论文，实际成功导入入库 {importedCount} 篇（重复论文已自动跳过）。"
            };
        }

        /// <summary>
        /// 被打回,ps:认领人不能变了;
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 完善论文资料([FromBody]完成步骤<成果增改<成果论文>> model)
        {
            //await 完善成果论文资料(model);
            await MyWorkFlowBusiness.完成步骤(
                            model.toSimple(), CurrentUser.编号,
                            new sp_成果论文_增改
                            {
                                tt = model.附加数据.基本资料.ToDataTable(),
                                tt_成果项目 = model.附加数据.相关项目.ToDataTable(),
                                tt_成果作者 = model.附加数据.作者.ToDataTable()
                            }, model.附加数据.IsHold);
        }

        [HttpPost]
        async public Task<object> 上传检索信息页相关文件()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/成果/论文/检索信息页相关文件",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpPost]
        async public Task<object> 上传论文正文()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/成果/论文/论文正文",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpPost]
        async public Task<object> 上传论文其他相关文件()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/成果/论文/其他相关文件",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpGet]
        async public Task<object> 分页获取论文(Paging paging, 论文Filter filter) =>
            await Db.GetPagingListSpAsync<object, 论文Filter>(
                paging,
                filter,
                $"tfn_成果论文({CurrentUser.编号},{论文管理流程模板编号})");

        [HttpGet]
        async public Task<object> 分页获取可领取论文(Paging paging, 论文Filter filter)
        {
            filter.Null认领人编号 = true;
            return await Db.GetPagingListSpAsync<v1_成果论文_列表, 论文Filter>(
                   paging,
                   filter);
        }

        /// <summary>认领采集到的未分配论文，保留原作者和项目关联并开启认领审核流程。</summary>
        [HttpPost]
        async public Task<object> 认领论文([FromBody]论文认领请求 model)
        {
            if (model == null || model.论文编号 <= 0) throw new ArgumentException("论文编号无效");
            var paper = await Db.QuerySingleOrDefaultAsync<成果论文>(@"
SELECT * FROM ""成果论文"" WHERE ""编号""=@论文编号 AND ""认领人编号"" IS NULL;", new { model.论文编号 });
            if (paper == null) return NotFound(new { error = "论文不存在、已被认领或已进入处理流程" });
            paper.认领人编号 = CurrentUser.编号;
            var projects = await Db.GetListSpAsync<成果项目>($"tfn_成果项目({(int)成果类型.论文},{model.论文编号})");
            var authors = await Db.GetListSpAsync<成果作者>($"tfn_成果作者({(int)成果类型.论文},{model.论文编号})");
            await MyWorkFlowBusiness.发起流程(
                认领论文流程模板编号,
                new sp_成果论文_增改
                {
                    tt = paper.ToDataTable(),
                    tt_成果项目 = projects.ToDataTable(),
                    tt_成果作者 = authors.ToDataTable()
                }, CurrentUser.编号, model.IsHold);
            await Db.ExecuteAsync("UPDATE \"论文采集结果\" SET \"状态\"='已认领' WHERE \"论文编号\"=@论文编号;", new { model.论文编号 });
            return new { 编号 = model.论文编号, 状态 = model.IsHold ? "已暂存" : "已提交认领审核" };
        }

        /// <summary>接收论文采集系统结果，按外部论文编号幂等写入未认领成果并自动匹配期刊。</summary>
        [HttpPost]
        async public Task<object> 接收论文采集结果([FromBody]论文采集结果同步请求 model)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员"))
                return StatusCode(403, new { error = "无权接收论文采集结果" });
            if (model == null || model.外部系统编号 <= 0 || string.IsNullOrWhiteSpace(model.同步批次号))
                throw new ArgumentException("外部系统编号和同步批次号不能为空");
            var rows = (model.论文列表 ?? Enumerable.Empty<论文采集结果同步记录>()).ToList();
            if (rows.Count == 0 || rows.Count > 1000) throw new ArgumentException("论文采集结果数量必须在 1-1000 条之间");
            if (!await Db.QuerySingleAsync<bool>("SELECT EXISTS (SELECT 1 FROM \"外部系统\" WHERE \"编号\"=@外部系统编号 AND \"系统类型\"='论文采集');", new { model.外部系统编号 }))
                return NotFound(new { error = "外部系统不存在或不是论文采集类型" });

            await using var connection = new NpgsqlConnection(DbConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            var newCount = 0;
            var existingCount = 0;
            var paperIds = new List<int>();
            foreach (var item in rows)
            {
                if (string.IsNullOrWhiteSpace(item.外部论文编号) || string.IsNullOrWhiteSpace(item.论文标题))
                    throw new ArgumentException("外部论文编号和论文标题不能为空");
                var externalId = item.外部论文编号.Trim();
                var raw = JsonSerializer.Serialize(item.原始数据 ?? item);
                var existing = await connection.QuerySingleOrDefaultAsync<int?>(
                    "SELECT \"论文编号\" FROM \"论文采集结果\" WHERE \"外部系统编号\"=@外部系统编号 AND \"外部论文编号\"=@外部论文编号;",
                    new { model.外部系统编号, 外部论文编号 = externalId }, transaction);
                var paperId = existing.GetValueOrDefault();
                if (paperId == 0)
                {
                    var journalId = await connection.QuerySingleOrDefaultAsync<int?>(@"
	SELECT q.""编号"" FROM ""期刊"" q
 WHERE (@ISSN号 IS NOT NULL AND replace(replace(coalesce(q.""ISSN号"",''),'-',''),' ','') = @ISSN号)
    OR (@CN号 IS NOT NULL AND replace(replace(coalesce(q.""CN号"",''),'-',''),' ','') = @CN号)
    OR (@刊物名称 IS NOT NULL AND (lower(coalesce(q.""名称"",'')) = lower(@刊物名称) OR lower(coalesce(q.""英文刊名"",'')) = lower(@刊物名称)))
 ORDER BY q.""年度"" DESC NULLS LAST, q.""编号"" DESC LIMIT 1;", new
                    {
                        ISSN号 = NormalizeCode(item.ISSN号), CN号 = NormalizeCode(item.CN号),
                        刊物名称 = string.IsNullOrWhiteSpace(item.刊物名称) ? null : item.刊物名称.Trim()
                    }, transaction);
                    paperId = await connection.QuerySingleAsync<int>(@"
INSERT INTO ""成果论文"" (""认领人编号"",""论文标题"",""论文Online日期"",""论文正式出版日期"",""年度"",""作者人数"",""关键字"",""论文摘要"",""刊物编号"",""备注"")
VALUES (NULL,@论文标题,@论文Online日期,@论文正式出版日期,@年度,@作者人数,@关键字,@论文摘要,@刊物编号,@备注)
RETURNING ""编号"";", new
                    {
                        论文标题 = item.论文标题.Trim(), item.论文Online日期, item.论文正式出版日期, item.年度,
                        作者人数 = item.作者人数 ?? CountAuthors(item.作者), item.关键字, item.论文摘要,
                        刊物编号 = journalId, 备注 = $"外部论文采集：{model.同步批次号.Trim()}"
                    }, transaction);
                    var firstAuthor = FirstAuthor(item.作者);
                    if (!string.IsNullOrWhiteSpace(firstAuthor))
                    {
                        await connection.ExecuteAsync(@"
INSERT INTO ""成果作者"" (""成果类型"",""成果编号"",""作者级别"",""作者编号"")
SELECT 1,@成果编号,'第一作者',p.""编号"" FROM ""人员"" p
 WHERE lower(btrim(p.""姓名""))=lower(btrim(@作者))
   AND NOT EXISTS (SELECT 1 FROM ""成果作者"" a WHERE a.""成果类型""=1 AND a.""成果编号""=@成果编号 AND a.""作者编号""=p.""编号"");", new { 成果编号 = paperId, 作者 = firstAuthor }, transaction);
                    }
                    newCount++;
                }
                else existingCount++;
                await connection.ExecuteAsync(@"
INSERT INTO ""论文采集结果"" (""外部系统编号"",""外部论文编号"",""论文编号"",""论文标题"",""作者"",""刊物名称"",""ISSN号"",""CN号"",""状态"",""原始数据"",""同步批次号"",""最后同步时间"")
VALUES (@外部系统编号,@外部论文编号,@论文编号,@论文标题,@作者,@刊物名称,@ISSN号,@CN号,'待认领',CAST(@原始数据 AS jsonb),@同步批次号,CURRENT_TIMESTAMP)
ON CONFLICT (""外部系统编号"",""外部论文编号"") DO UPDATE SET ""论文编号""=EXCLUDED.""论文编号"",""论文标题""=EXCLUDED.""论文标题"",""作者""=EXCLUDED.""作者"",""刊物名称""=EXCLUDED.""刊物名称"",""ISSN号""=EXCLUDED.""ISSN号"",""CN号""=EXCLUDED.""CN号"",""原始数据""=EXCLUDED.""原始数据"",""同步批次号""=EXCLUDED.""同步批次号"",""最后同步时间""=CURRENT_TIMESTAMP;", new
                {
                    model.外部系统编号, 外部论文编号 = externalId, 论文编号 = paperId,
                    论文标题 = item.论文标题.Trim(), item.作者, item.刊物名称, item.ISSN号, item.CN号,
                    原始数据 = raw, 同步批次号 = model.同步批次号.Trim()
                }, transaction);
                paperIds.Add(paperId);
            }
            await transaction.CommitAsync();
            return new { model.同步批次号, 新增数量 = newCount, 已存在数量 = existingCount, 论文编号 = paperIds };
        }

        /// <summary>调用 Crossref 公共 Works API 拉取论文，转换为统一采集结果并进入待认领队列。</summary>
        [HttpPost]
        async public Task<object> 从Crossref采集论文([FromBody]Crossref论文采集请求 model)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员"))
                return StatusCode(403, new { error = "无权执行 Crossref 论文采集" });
            if (model == null || model.外部系统编号 <= 0) throw new ArgumentException("外部系统编号不能为空");
            if (string.IsNullOrWhiteSpace(model.关键词) && string.IsNullOrWhiteSpace(model.作者)) throw new ArgumentException("关键词和作者至少填写一项");
            if (model.从日期.HasValue && model.到日期.HasValue && model.到日期.Value.Date < model.从日期.Value.Date) throw new ArgumentException("到日期不能早于从日期");
            var rows = Math.Clamp(model.行数 <= 0 ? 20 : model.行数, 1, 50);
            if (!await Db.QuerySingleAsync<bool>("SELECT EXISTS (SELECT 1 FROM \"外部系统\" WHERE \"编号\"=@编号 AND \"系统类型\"='论文采集');", new { 编号 = model.外部系统编号 }))
                return NotFound(new { error = "外部系统不存在或不是论文采集类型" });

            var queryParts = new List<string> { "rows=" + rows };
            if (!string.IsNullOrWhiteSpace(model.关键词)) queryParts.Add("query.bibliographic=" + Uri.EscapeDataString(model.关键词.Trim()));
            if (!string.IsNullOrWhiteSpace(model.作者)) queryParts.Add("query.author=" + Uri.EscapeDataString(model.作者.Trim()));
            var filters = new List<string>();
            if (model.从日期.HasValue) filters.Add("from-pub-date:" + model.从日期.Value.ToString("yyyy-MM-dd"));
            if (model.到日期.HasValue) filters.Add("until-pub-date:" + model.到日期.Value.ToString("yyyy-MM-dd"));
            if (filters.Count > 0) queryParts.Add("filter=" + Uri.EscapeDataString(string.Join(",", filters)));
            var url = "https://api.crossref.org/works?" + string.Join("&", queryParts);
            var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.UserAgent.ParseAdd(config.GetValue<string>("ExternalSync:Crossref:UserAgent") ?? "ScientificResearch/1.0 (mailto:research-admin@example.org)");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, HttpContext.RequestAborted);
            var content = await response.Content.ReadAsStringAsync(HttpContext.RequestAborted);
            if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode, new { error = $"Crossref 返回 HTTP {(int)response.StatusCode}", detail = content.Length > 500 ? content.Substring(0, 500) : content });
            using var document = JsonDocument.Parse(content);
            if (!document.RootElement.TryGetProperty("message", out var message) || !message.TryGetProperty("items", out var items)) throw new InvalidOperationException("Crossref 返回格式不符合预期");
            var mapped = new List<论文采集结果同步记录>();
            foreach (var item in items.EnumerateArray())
            {
                var doi = GetJsonString(item, "DOI");
                var title = GetJsonArrayFirstString(item, "title");
                if (string.IsNullOrWhiteSpace(doi) || string.IsNullOrWhiteSpace(title)) continue;
                var authors = item.TryGetProperty("author", out var authorArray) && authorArray.ValueKind == JsonValueKind.Array
                    ? string.Join(";", authorArray.EnumerateArray().Select(x => (GetJsonString(x, "given") + " " + GetJsonString(x, "family")).Trim()).Where(x => !string.IsNullOrWhiteSpace(x))) : null;
                var published = GetCrossrefDate(item, "published-print") ?? GetCrossrefDate(item, "published-online") ?? GetCrossrefDate(item, "issued");
                mapped.Add(new 论文采集结果同步记录
                {
                    外部论文编号 = "doi:" + doi.Trim(), 论文标题 = LimitText(title.Trim(), 200), 作者 = LimitText(authors, 1000), 刊物名称 = LimitText(GetJsonArrayFirstString(item, "container-title"), 500),
                    ISSN号 = LimitText(GetJsonArrayFirstString(item, "ISSN"), 50), 论文Online日期 = GetCrossrefDate(item, "published-online"), 论文正式出版日期 = GetCrossrefDate(item, "published-print") ?? published,
                    年度 = published?.Year, 作者人数 = string.IsNullOrWhiteSpace(authors) ? null : authors.Split(';', StringSplitOptions.RemoveEmptyEntries).Length,
                    关键字 = LimitText(GetJsonArrayStrings(item, "subject"), 500), 论文摘要 = LimitText(GetJsonString(item, "abstract"), 1000), 原始数据 = item.Clone()
                });
            }
            if (mapped.Count == 0) return new { 来源 = "Crossref", 总数 = 0, 说明 = "未找到包含 DOI 和标题的结果" };
            var batch = "crossref-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var result = await 接收论文采集结果(new 论文采集结果同步请求 { 外部系统编号 = model.外部系统编号, 同步批次号 = batch, 论文列表 = mapped });
            return new { 来源 = "Crossref", 同步批次号 = batch, 采集数量 = mapped.Count, 结果 = result };
        }

        /// <summary>调用 PubMed E-utilities 公共 API，按 PMID 幂等写入待认领队列。</summary>
        [HttpPost]
        async public Task<object> 从PubMed采集论文([FromBody]PubMed论文采集请求 model)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员"))
                return StatusCode(403, new { error = "无权执行 PubMed 论文采集" });
            if (model == null || model.外部系统编号 <= 0) throw new ArgumentException("外部系统编号不能为空");
            if (string.IsNullOrWhiteSpace(model.关键词) && string.IsNullOrWhiteSpace(model.作者)) throw new ArgumentException("关键词和作者至少填写一项");
            if (model.从日期.HasValue && model.到日期.HasValue && model.到日期.Value.Date < model.从日期.Value.Date) throw new ArgumentException("到日期不能早于从日期");
            var rows = Math.Clamp(model.行数 <= 0 ? 20 : model.行数, 1, 50);
            if (!await Db.QuerySingleAsync<bool>("SELECT EXISTS (SELECT 1 FROM \"外部系统\" WHERE \"编号\"=@编号 AND \"系统类型\"='论文采集');", new { 编号 = model.外部系统编号 }))
                return NotFound(new { error = "外部系统不存在或不是论文采集类型" });

            var terms = new List<string>();
            if (!string.IsNullOrWhiteSpace(model.关键词)) terms.Add("(" + model.关键词.Trim() + ")");
            if (!string.IsNullOrWhiteSpace(model.作者)) terms.Add("" + model.作者.Trim() + "[Author]");
            if (model.从日期.HasValue || model.到日期.HasValue)
            {
                var from = (model.从日期 ?? new DateTime(1900, 1, 1)).ToString("yyyy/MM/dd");
                var to = (model.到日期 ?? DateTime.Today).ToString("yyyy/MM/dd");
                terms.Add($"(\"{from}\"[Date - Publication] : \"{to}\"[Date - Publication])");
            }
            var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            var userAgent = config.GetValue<string>("ExternalSync:PubMed:UserAgent") ?? "ScientificResearch/1.0 (research-admin@example.org)";
            var searchUrl = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esearch.fcgi?db=pubmed&retmode=json&retmax=" + rows + "&term=" + Uri.EscapeDataString(string.Join(" AND ", terms));
            using var searchRequest = new HttpRequestMessage(HttpMethod.Get, searchUrl);
            searchRequest.Headers.UserAgent.ParseAdd(userAgent);
            using var searchResponse = await client.SendAsync(searchRequest, HttpCompletionOption.ResponseHeadersRead, HttpContext.RequestAborted);
            var searchContent = await searchResponse.Content.ReadAsStringAsync(HttpContext.RequestAborted);
            if (!searchResponse.IsSuccessStatusCode) return StatusCode((int)searchResponse.StatusCode, new { error = $"PubMed 检索返回 HTTP {(int)searchResponse.StatusCode}", detail = LimitText(searchContent, 500) });
            using var searchDocument = JsonDocument.Parse(searchContent);
            if (!searchDocument.RootElement.TryGetProperty("esearchresult", out var searchResult) || !searchResult.TryGetProperty("idlist", out var idList) || idList.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("PubMed 检索返回格式不符合预期");
            var pmids = idList.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (pmids.Length == 0) return new { 来源 = "PubMed", 总数 = 0, 说明 = "未找到论文" };

            var summaryUrl = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esummary.fcgi?db=pubmed&retmode=json&id=" + string.Join(",", pmids);
            using var summaryRequest = new HttpRequestMessage(HttpMethod.Get, summaryUrl);
            summaryRequest.Headers.UserAgent.ParseAdd(userAgent);
            using var summaryResponse = await client.SendAsync(summaryRequest, HttpCompletionOption.ResponseHeadersRead, HttpContext.RequestAborted);
            var summaryContent = await summaryResponse.Content.ReadAsStringAsync(HttpContext.RequestAborted);
            if (!summaryResponse.IsSuccessStatusCode) return StatusCode((int)summaryResponse.StatusCode, new { error = $"PubMed 摘要返回 HTTP {(int)summaryResponse.StatusCode}", detail = LimitText(summaryContent, 500) });
            using var summaryDocument = JsonDocument.Parse(summaryContent);
            if (!summaryDocument.RootElement.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("PubMed 摘要返回格式不符合预期");
            var mapped = new List<论文采集结果同步记录>();
            foreach (var pmid in pmids)
            {
                if (!result.TryGetProperty(pmid, out var item) || item.ValueKind != JsonValueKind.Object) continue;
                var title = GetJsonString(item, "title");
                if (string.IsNullOrWhiteSpace(title)) continue;
                var authors = item.TryGetProperty("authors", out var authorArray) && authorArray.ValueKind == JsonValueKind.Array
                    ? string.Join(";", authorArray.EnumerateArray().Select(x => GetJsonString(x, "name")).Where(x => !string.IsNullOrWhiteSpace(x))) : null;
                var pubDate = GetJsonString(item, "pubdate");
                var date = ParsePubMedDate(pubDate);
                mapped.Add(new 论文采集结果同步记录
                {
                    外部论文编号 = "pmid:" + pmid, 论文标题 = LimitText(title.Trim(), 200), 作者 = LimitText(authors, 1000), 刊物名称 = LimitText(GetJsonString(item, "fulljournalname") ?? GetJsonString(item, "source"), 500),
                    ISSN号 = LimitText(GetJsonString(item, "issn") ?? GetJsonString(item, "issnlinking"), 50), 论文Online日期 = date, 论文正式出版日期 = date,
                    年度 = date?.Year, 作者人数 = string.IsNullOrWhiteSpace(authors) ? null : authors.Split(';', StringSplitOptions.RemoveEmptyEntries).Length,
                    原始数据 = item.Clone()
                });
            }
            if (mapped.Count == 0) return new { 来源 = "PubMed", 总数 = 0, 说明 = "未找到包含标题的结果" };
            var batch = "pubmed-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var ingest = await 接收论文采集结果(new 论文采集结果同步请求 { 外部系统编号 = model.外部系统编号, 同步批次号 = batch, 论文列表 = mapped });
            return new { 来源 = "PubMed", 同步批次号 = batch, 采集数量 = mapped.Count, 结果 = ingest };
        }

        private static string NormalizeCode(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Replace("-", string.Empty).Replace(" ", string.Empty).Trim();
        private static int CountAuthors(string value) => string.IsNullOrWhiteSpace(value) ? 0 : value.Split(new[] { ';', '；' }, StringSplitOptions.RemoveEmptyEntries).Length;
        private static string FirstAuthor(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Split(new[] { ';', '；' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).FirstOrDefault(x => x.Length > 0);
        private static string GetJsonString(JsonElement element, string property) => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        private static string GetJsonArrayFirstString(JsonElement element, string property) => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array ? value.EnumerateArray().Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : null).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) : null;
        private static string GetJsonArrayStrings(JsonElement element, string property) => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array ? string.Join(";", value.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString())) : null;
        private static DateTime? GetCrossrefDate(JsonElement element, string property)
        {
            if (!element.TryGetProperty(property, out var value) || !value.TryGetProperty("date-parts", out var parts) || parts.ValueKind != JsonValueKind.Array || parts.GetArrayLength() == 0 || parts[0].ValueKind != JsonValueKind.Array) return null;
            var values = parts[0].EnumerateArray().Select(x => x.TryGetInt32(out var number) ? number : 0).ToArray();
            if (values.Length < 1 || values[0] < 1000) return null;
            var month = values.Length > 1 && values[1] >= 1 && values[1] <= 12 ? values[1] : 1;
            var day = values.Length > 2 && values[2] >= 1 && values[2] <= DateTime.DaysInMonth(values[0], month) ? values[2] : 1;
            return new DateTime(values[0], month, day);
        }
        private static string LimitText(string value, int length) => string.IsNullOrWhiteSpace(value) ? null : (value.Length <= length ? value : value.Substring(0, length));
        private static DateTime? ParsePubMedDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(value, "(?<year>\\d{4})(?:[ /-](?<month>\\d{1,2}))?(?:[ /-](?<day>\\d{1,2}))?");
            if (!match.Success || !int.TryParse(match.Groups["year"].Value, out var year) || year < 1000) return null;
            var month = match.Groups["month"].Success && int.TryParse(match.Groups["month"].Value, out var m) && m is >= 1 and <= 12 ? m : 1;
            var day = match.Groups["day"].Success && int.TryParse(match.Groups["day"].Value, out var d) && d >= 1 && d <= DateTime.DaysInMonth(year, month) ? d : 1;
            return new DateTime(year, month, day);
        }

        /// <summary>按刊物名称、ISSN 或 CN 号匹配本地期刊库，返回分区、收录和影响因子等元数据。</summary>
        [HttpGet]
        async public Task<object> 识别论文期刊信息(string 刊物名称 = null, string ISSN号 = null, string CN号 = null)
        {
            var name = string.IsNullOrWhiteSpace(刊物名称) ? null : 刊物名称.Trim();
            var issn = string.IsNullOrWhiteSpace(ISSN号) ? null : ISSN号.Replace("-", string.Empty).Replace(" ", string.Empty).Trim();
            var cn = string.IsNullOrWhiteSpace(CN号) ? null : CN号.Replace("-", string.Empty).Replace(" ", string.Empty).Trim();
            if (name == null && issn == null && cn == null) throw new ArgumentException("至少提供刊物名称、ISSN号或CN号之一");
            return await Db.QueryAsync<object>(@"
SELECT q.""编号"", q.""名称"", q.""英文刊名"", q.""ISSN号"", q.""CN号"", q.""收录情况"", q.""JCR分区"", q.""编目名称"", q.""编目类别"", q.""年度"", q.""影响因子"", q.""特征因子分值"",
       CASE
         WHEN @ISSN号 IS NOT NULL AND replace(replace(coalesce(q.""ISSN号"",''),'-',''),' ','') = @ISSN号 THEN 100
         WHEN @CN号 IS NOT NULL AND replace(replace(coalesce(q.""CN号"",''),'-',''),' ','') = @CN号 THEN 95
         WHEN @刊物名称 IS NOT NULL AND lower(coalesce(q.""名称"",'')) = lower(@刊物名称) THEN 90
         WHEN @刊物名称 IS NOT NULL AND lower(coalesce(q.""英文刊名"",'')) = lower(@刊物名称) THEN 88
         WHEN @刊物名称 IS NOT NULL AND (q.""名称"" ILIKE '%' || @刊物名称 || '%' OR q.""英文刊名"" ILIKE '%' || @刊物名称 || '%') THEN 60
         ELSE 0
       END AS ""匹配分数""
  FROM ""期刊"" q
 WHERE (@ISSN号 IS NOT NULL AND replace(replace(coalesce(q.""ISSN号"",''),'-',''),' ','') = @ISSN号)
    OR (@CN号 IS NOT NULL AND replace(replace(coalesce(q.""CN号"",''),'-',''),' ','') = @CN号)
    OR (@刊物名称 IS NOT NULL AND (q.""名称"" ILIKE '%' || @刊物名称 || '%' OR q.""英文刊名"" ILIKE '%' || @刊物名称 || '%'))
 ORDER BY ""匹配分数"" DESC, q.""年度"" DESC NULLS LAST, q.""编号"" DESC
 LIMIT 10;", new { 刊物名称 = name, ISSN号 = issn, CN号 = cn });
        }

        [HttpGet]
        async public Task<object> 获取论文详情(int 论文编号)
        {
            var r1 = await Db.GetListSpAsync<v1_成果论文_详情, 论文Filter>(new 论文Filter() { 编号 = 论文编号 });
            var r2 = await Db.GetListSpAsync<object>($"tfn_成果项目({(int)成果类型.论文},{论文编号})");
            var r3 = await Db.GetListSpAsync<object>($"tfn_成果作者({(int)成果类型.论文},{论文编号})");
            var r4 = await MyWorkFlowBusiness.获取某流程的步骤(论文管理流程模板编号, 论文编号);
            return new { 论文详情 = r1.FirstOrDefault(), 成果项目 = r2, 成果作者 = r3, 审核情况 = r4 };
        }
        #endregion

        #region 著作管理

        [HttpPost]
        async public Task 新增著作([FromBody]成果增改<成果著作> model)
        {
            //await 发起一个成果著作增改流程<成果著作>(model, 著作管理流程模板编号);
            await MyWorkFlowBusiness.发起流程(
                           著作管理流程模板编号,
                           new sp_成果著作_增改
                           {
                               tt = model.基本资料.ToDataTable(),
                               tt_成果项目 = model.相关项目.ToDataTable(),
                               tt_成果作者 = model.作者.ToDataTable()
                           },
                           CurrentUser.编号,
                           model.IsHold);
        }

        [HttpPost]
        async public Task 完善著作资料([FromBody]完成步骤<成果增改<成果著作>> model)
        {
            //await 完善成果著作资料(model);
            await MyWorkFlowBusiness.完成步骤(
                            model.toSimple(), CurrentUser.编号,
                            new sp_成果著作_增改
                            {
                                tt = model.附加数据.基本资料.ToDataTable(),
                                tt_成果项目 = model.附加数据.相关项目.ToDataTable(),
                                tt_成果作者 = model.附加数据.作者.ToDataTable()
                            },
                            model.附加数据.IsHold);
        }

        [HttpPost]
        async public Task<object> 上传封面相关文件()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/成果/著作/封面相关文件",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpPost]
        async public Task<object> 上传版权页相关文件()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/成果/著作/版权页相关文件",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpPost]
        async public Task<object> 上传前言页相关文件()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/成果/著作/前言页相关文件",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpPost]
        async public Task<object> 上传完整目录页相关文件()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/成果/著作/完整目录页相关文件",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpPost]
        async public Task<object> 上传著作其他相关文件()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/成果/著作/其他相关文件",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpGet]
        async public Task<object> 分页获取著作(Paging paging, 论文Filter filter) =>
            await Db.GetPagingListSpAsync<object, 论文Filter>(
                paging,
                filter,
                $"tfn_成果著作({CurrentUser.编号},{著作管理流程模板编号})");

        [HttpGet]
        async public Task<object> 获取著作详情(int 著作编号)
        {
            var r1 = await Db.GetListSpAsync<v1_成果著作_详情, 著作Filter>(new 著作Filter() { 编号 = 著作编号 });
            var r2 = await Db.GetListSpAsync<object>($"tfn_成果项目({(int)成果类型.著作},{著作编号})");
            var r3 = await Db.GetListSpAsync<object>($"tfn_成果作者({(int)成果类型.著作},{著作编号})");
            var r4 = await MyWorkFlowBusiness.获取某流程的步骤(著作管理流程模板编号, 著作编号);
            return new { 著作详情 = r1.FirstOrDefault(), 成果项目 = r2, 成果作者 = r3, 审核情况 = r4 };
        }
        #endregion

        #region 专利管理

        [HttpPost]
        async public Task 新增专利([FromBody]成果增改<成果专利> model) =>
             //await 发起一个成果专利增改流程<成果专利>(model, 专利管理流程模板编号)
             await MyWorkFlowBusiness.发起流程(
                            专利管理流程模板编号,
                            new sp_成果专利_增改
                            {
                                tt = model.基本资料.ToDataTable(),
                                tt_成果项目 = model.相关项目.ToDataTable(),
                                tt_成果作者 = model.作者.ToDataTable()
                            },
                            CurrentUser.编号, 
                            model.IsHold);

        [HttpPost]
        async public Task 完善专利资料([FromBody]完成步骤<成果增改<成果专利>> model) =>
            await MyWorkFlowBusiness.完成步骤(
                            model.toSimple(), CurrentUser.编号,
                            new sp_成果专利_增改
                            {
                                tt = model.附加数据.基本资料.ToDataTable(),
                                tt_成果项目 = model.附加数据.相关项目.ToDataTable(),
                                tt_成果作者 = model.附加数据.作者.ToDataTable()
                            }, model.附加数据.IsHold);

        [HttpPost]
        async public Task<object> 上传专利相关文件()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/成果/专利/相关文件",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpGet]
        async public Task<object> 分页获取专利(Paging paging, 专利Filter filter) =>
            await Db.GetPagingListSpAsync<object, 专利Filter>(paging, filter, $"tfn_成果专利({CurrentUser.编号},{专利管理流程模板编号})");

        [HttpGet]
        async public Task<object> 获取专利详情(int 专利编号)
        {
            var r1 = await Db.GetListSpAsync<v1_成果专利_详情, 专利Filter>(new 专利Filter() { 编号 = 专利编号 });
            var r2 = await Db.GetListSpAsync<object>($"tfn_成果专利({(int)成果类型.专利},{专利编号})");
            var r3 = await Db.GetListSpAsync<object>($"tfn_成果作者({(int)成果类型.专利},{专利编号})");
            var r4 = await MyWorkFlowBusiness.获取某流程的步骤(专利管理流程模板编号, 专利编号);
            return new { 专利详情 = r1, 成果项目 = r2, 成果作者 = r3, 审核情况 = r4 };
        }

        /// <summary>将专利申请/授权事件加入专利代理机构同步队列，使用专利和事件组成幂等键。</summary>
        [HttpPost]
        async public Task<object> 通知专利代理机构([FromBody]专利代理通知请求 model)
        {
            if (model == null || model.专利编号 <= 0 || model.外部系统编号 <= 0) throw new ArgumentException("专利编号和外部系统编号不能为空");
            if (model.事件类型 is not ("申请" or "授权" or "变更" or "年费" or "撤回")) throw new ArgumentException("专利事件类型无效");
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员") && !await 可访问成果((int)成果类型.专利, model.专利编号))
                return StatusCode(403, new { error = "无权通知该专利代理机构" });
            var configuredMaxRetries = await Db.QuerySingleOrDefaultAsync<int?>("SELECT \"最大重试次数\" FROM \"外部系统\" WHERE \"编号\"=@外部系统编号 AND \"系统类型\" IN ('其他','合同');", new { model.外部系统编号 });
            if (!configuredMaxRetries.HasValue)
                return NotFound(new { error = "外部系统不存在或不是合同/其他类型" });
            var patent = await Db.QuerySingleOrDefaultAsync<object>(@"
SELECT ""编号"",""专利名称"",""专利类型"",""专利权人名称"",""申请号"",""专利号"",""授权公告日"",""代理机构"",""备注""
  FROM ""成果专利"" WHERE ""编号""=@专利编号;", new { model.专利编号 });
            if (patent == null) return NotFound(new { error = "专利不存在" });
            var payload = JsonSerializer.Serialize(new { eventType = model.事件类型, patent, note = model.说明, sentAt = DateTime.UtcNow });
            var key = $"patent-agent:{model.外部系统编号}:{model.专利编号}:{model.事件类型}";
            var id = await Db.QuerySingleOrDefaultAsync<long?>(@"
INSERT INTO ""外部同步任务"" (""外部系统编号"",""业务类型"",""方向"",""业务主键"",""幂等键"",""请求负载"",""最大重试次数"",""创建人编号"")
VALUES (@外部系统编号,'专利代理通知','推送',@业务主键,@幂等键,CAST(@请求负载 AS jsonb),@最大重试次数,@创建人编号)
ON CONFLICT (""幂等键"") DO NOTHING RETURNING ""编号"";", new { model.外部系统编号, 业务主键 = $"patent:{model.专利编号}", 幂等键 = key, 请求负载 = payload, 最大重试次数 = Math.Clamp(configuredMaxRetries.Value, 0, 10), 创建人编号 = CurrentUser.编号 });
            if (!id.HasValue) id = await Db.QuerySingleAsync<long>("SELECT \"编号\" FROM \"外部同步任务\" WHERE \"幂等键\"=@幂等键;", new { 幂等键 = key });
            return new { 编号 = id.Value, 状态 = "待处理", 幂等键 = key };
        }
        #endregion

        #region 获奖管理

        [HttpPost]
        async public Task 新增获奖([FromBody]成果增改<成果获奖> model) =>
             await MyWorkFlowBusiness.发起流程(
                            获奖管理流程模板编号,
                            new sp_成果获奖_增改
                            {
                                tt = model.基本资料.ToDataTable(),
                                tt_成果项目 = model.相关项目.ToDataTable(),
                                tt_成果作者 = model.作者.ToDataTable()
                            }, 
                            CurrentUser.编号,
                            model.IsHold);

        [HttpPost]
        async public Task 完善获奖资料([FromBody]完成步骤<成果增改<成果获奖>> model) =>
             await MyWorkFlowBusiness.完成步骤(
                            model.toSimple(), CurrentUser.编号,
                            new sp_成果获奖_增改
                            {
                                tt = model.附加数据.基本资料.ToDataTable(),
                                tt_成果项目 = model.附加数据.相关项目.ToDataTable(),
                                tt_成果作者 = model.附加数据.作者.ToDataTable()
                            }, model.附加数据.IsHold);

        [HttpPost]
        async public Task<object> 上传获奖相关文件()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/成果/获奖/相关文件",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpPost]
        async public Task<object> 上传获奖单位文件()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/成果/获奖/获奖单位文件",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpPost]
        async public Task<object> 上传获奖批文()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/成果/获奖/获奖批文",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpGet]
        async public Task<object> 分页获取获奖(Paging paging, 获奖Filter filter) =>
           await Db.GetPagingListSpAsync<object, 获奖Filter>(
               paging,
               filter,
               $"tfn_成果获奖({CurrentUser.编号},{获奖管理流程模板编号})");

        [HttpGet]
        async public Task<object> 获取获奖详情(int 获奖编号)
        {
            var r1 = await Db.GetListSpAsync<v1_成果获奖_详情, 获奖Filter>(new 获奖Filter() { 编号 = 获奖编号 });
            var r2 = await Db.GetListSpAsync<object>($"tfn_成果项目({(int)成果类型.获奖},{获奖编号})");
            var r3 = await Db.GetListSpAsync<object>($"tfn_成果作者({(int)成果类型.获奖},{获奖编号})");
            var r4 = await MyWorkFlowBusiness.获取某流程的步骤(获奖管理流程模板编号, 获奖编号);
            return new { 获奖详情 = r1.FirstOrDefault(), 成果项目 = r2, 成果作者 = r3, 审核情况 = r4 };
        }

        #endregion

        [HttpGet]
        async public Task<PagingResult<v4_成果总览>> 分页获取成果总览(Paging paging, 成果总览Filter filter) =>
            await Db.GetPagingListSpAsync<v4_成果总览, 成果总览Filter>(paging, filter);

        /// <summary>返回成果关联、作者身份核验和治理状态。</summary>
        [HttpGet]
        async public Task<object> 获取成果治理详情(int 成果类型, int 成果编号)
        {
            if (!await 可访问成果(成果类型, 成果编号)) return ProjectAccessDenied();
            var 关联 = await Db.QueryAsync<object>(@"
SELECT ""编号"", ""关联类型"", ""流程模板编号"", ""项目编号"", ""外部对象类型"", ""外部对象编号"", ""关联说明"", ""建立人编号"", ""建立时间""
  FROM ""科研成果关联"" WHERE ""成果类型""=@成果类型 AND ""成果编号""=@成果编号
 ORDER BY ""建立时间"" DESC, ""编号"" DESC;", new { 成果类型, 成果编号 });
            var 作者 = await Db.QueryAsync<object>(@"
SELECT a.""编号"", a.""作者编号"", a.""作者顺序"", a.""作者显示姓名"", a.""单位名称"", a.""规范单位编号"", a.""单位规范状态"", a.""ORCID"", a.""身份状态"", a.""核验说明"", a.""核验人编号"", a.""核验时间"", p.""姓名"" AS ""核验人姓名""
  FROM ""科研成果作者核验"" a LEFT JOIN ""人员"" p ON p.""编号""=a.""核验人编号""
 WHERE a.""成果类型""=@成果类型 AND a.""成果编号""=@成果编号
 ORDER BY a.""作者顺序"" NULLS LAST, a.""编号"";", new { 成果类型, 成果编号 });
            var 治理 = await Db.QuerySingleOrDefaultAsync<object>(@"
SELECT g.""编号"", g.""状态"", g.""规范成果类型"", g.""规范成果编号"", g.""治理原因"", g.""操作人编号"", g.""操作时间"", g.""状态凭证编号"",
       e.""事件类型"", e.""来源类型"", e.""来源凭证编号"", e.""来源凭证摘要"", e.""来源文件路径"", e.""来源文件SHA256"", e.""来源系统"", e.""操作时间"" AS ""凭证登记时间"",
       COALESCE(v.""核验结果"", '待核验') AS ""凭证核验状态"", v.""核验说明"", v.""核验人编号"", v.""核验时间""
  FROM ""科研成果治理"" g
  LEFT JOIN ""科研成果状态凭证"" e ON e.""编号""=g.""状态凭证编号""
  LEFT JOIN LATERAL (
      SELECT ""核验结果"", ""核验说明"", ""核验人编号"", ""核验时间""
        FROM ""科研成果状态凭证核验""
       WHERE ""状态凭证编号""=e.""编号""
       ORDER BY ""核验时间"" DESC, ""编号"" DESC
       LIMIT 1
  ) v ON TRUE
 WHERE g.""成果类型""=@成果类型 AND g.""成果编号""=@成果编号;", new { 成果类型, 成果编号 });
            var 治理历史 = await Db.QueryAsync<object>(@"
SELECT ""编号"", ""原状态"", ""新状态"", ""原规范成果类型"", ""原规范成果编号"",
       ""新规范成果类型"", ""新规范成果编号"", ""治理原因"", ""变更动作"", ""操作人编号"", ""操作时间""
  FROM ""科研成果治理历史""
 WHERE ""成果类型""=@成果类型 AND ""成果编号""=@成果编号
 ORDER BY ""操作时间"" DESC, ""编号"" DESC;", new { 成果类型, 成果编号 });
            var 合并审计 = await Db.QueryAsync<object>(@"
SELECT ""编号"", ""来源成果类型"", ""来源成果编号"", ""规范成果类型"", ""规范成果编号"",
       ""合并原因"", ""操作人编号"", ""操作时间""
  FROM ""科研成果合并审计""
 WHERE (""来源成果类型""=@成果类型 AND ""来源成果编号""=@成果编号)
    OR (""规范成果类型""=@成果类型 AND ""规范成果编号""=@成果编号)
 ORDER BY ""操作时间"" DESC, ""编号"" DESC;", new { 成果类型, 成果编号 });
            var 原作者 = await Db.QueryAsync<object>(@"
SELECT a.""作者级别"", a.""作者编号"", p.""姓名"", p.""工号"", p.""部门名称""
  FROM ""成果作者"" a LEFT JOIN ""v1_人员_带部门名"" p ON p.""编号""=a.""作者编号""
 WHERE a.""成果类型""=@成果类型 AND a.""成果编号""=@成果编号 ORDER BY a.""编号"";", new { 成果类型, 成果编号 });
            var 状态凭证审计 = await Db.QueryAsync<object>(@"
SELECT e.""编号"", e.""原状态"", e.""新状态"", e.""事件类型"", e.""来源类型"", e.""来源凭证编号"", e.""来源凭证摘要"", e.""来源文件路径"", e.""来源文件SHA256"", e.""来源系统"", e.""操作人编号"", e.""操作时间"",
       COALESCE(v.""核验结果"", '待核验') AS ""核验状态"", v.""核验说明"", v.""核验人编号"", v.""核验时间""
  FROM ""科研成果状态凭证"" e
  LEFT JOIN LATERAL (
      SELECT ""核验结果"", ""核验说明"", ""核验人编号"", ""核验时间""
        FROM ""科研成果状态凭证核验""
       WHERE ""状态凭证编号""=e.""编号""
       ORDER BY ""核验时间"" DESC, ""编号"" DESC
       LIMIT 1
  ) v ON TRUE
 WHERE e.""成果类型""=@成果类型 AND e.""成果编号""=@成果编号
 ORDER BY e.""操作时间"" DESC, e.""编号"" DESC;", new { 成果类型, 成果编号 });
            return new { 成果类型, 成果编号, 原作者, 作者身份核验 = 作者, 成果关联 = 关联, 成果治理 = 治理, 治理历史, 合并审计, 状态凭证审计 };
        }

        /// <summary>读取成果治理不可变历史和重复合并映射，供审计和复核使用。</summary>
        [HttpGet]
        async public Task<object> 获取成果治理审计(int 成果类型, int 成果编号)
        {
            if (!await 可访问成果(成果类型, 成果编号)) return ProjectAccessDenied();
            var history = await Db.QueryAsync<object>(@"
SELECT ""编号"", ""原状态"", ""新状态"", ""原规范成果类型"", ""原规范成果编号"",
       ""新规范成果类型"", ""新规范成果编号"", ""治理原因"", ""变更动作"", ""操作人编号"", ""操作时间""
  FROM ""科研成果治理历史""
 WHERE ""成果类型""=@成果类型 AND ""成果编号""=@成果编号
 ORDER BY ""操作时间"" DESC, ""编号"" DESC;", new { 成果类型, 成果编号 });
            var merges = await Db.QueryAsync<object>(@"
SELECT ""编号"", ""来源成果类型"", ""来源成果编号"", ""规范成果类型"", ""规范成果编号"",
       ""合并原因"", ""操作人编号"", ""操作时间""
  FROM ""科研成果合并审计""
 WHERE (""来源成果类型""=@成果类型 AND ""来源成果编号""=@成果编号)
    OR (""规范成果类型""=@成果类型 AND ""规范成果编号""=@成果编号)
 ORDER BY ""操作时间"" DESC, ""编号"" DESC;", new { 成果类型, 成果编号 });
            var evidence = await Db.QueryAsync<object>(@"
SELECT e.""编号"", e.""原状态"", e.""新状态"", e.""事件类型"", e.""来源类型"", e.""来源凭证编号"", e.""来源凭证摘要"", e.""来源文件路径"", e.""来源文件SHA256"", e.""来源系统"", e.""操作人编号"", e.""操作时间"",
       COALESCE(v.""核验结果"", '待核验') AS ""核验状态"", v.""核验说明"", v.""核验人编号"", v.""核验时间""
  FROM ""科研成果状态凭证"" e
  LEFT JOIN LATERAL (
      SELECT ""核验结果"", ""核验说明"", ""核验人编号"", ""核验时间""
        FROM ""科研成果状态凭证核验""
       WHERE ""状态凭证编号""=e.""编号""
       ORDER BY ""核验时间"" DESC, ""编号"" DESC
       LIMIT 1
  ) v ON TRUE
 WHERE e.""成果类型""=@成果类型 AND e.""成果编号""=@成果编号
 ORDER BY e.""操作时间"" DESC, e.""编号"" DESC;", new { 成果类型, 成果编号 });
            return new { 成果类型, 成果编号, 治理历史 = history, 合并审计 = merges, 状态凭证审计 = evidence };
        }

        [HttpPost]
        async public Task<object> 新增科研成果关联([FromBody]科研成果关联新增 model)
        {
            if (model == null || model.成果类型 < 1 || model.成果类型 > 4 || model.成果编号 <= 0)
                throw new ArgumentException("成果类型和成果编号无效");
            var relation = model.关联类型?.Trim();
            if (relation is not ("研究" or "资助" or "伦理" or "数据" or "其他")) throw new ArgumentException("关联类型无效");
            var hasProject = model.流程模板编号.HasValue || model.项目编号.HasValue;
            var hasExternal = !string.IsNullOrWhiteSpace(model.外部对象类型) || !string.IsNullOrWhiteSpace(model.外部对象编号);
            if (hasProject == hasExternal || (hasProject && (!model.流程模板编号.HasValue || !model.项目编号.HasValue)) || (hasExternal && (string.IsNullOrWhiteSpace(model.外部对象类型) || string.IsNullOrWhiteSpace(model.外部对象编号))))
                throw new ArgumentException("关联对象必须提供完整的项目对象或外部对象，且只能选择一种");
            if ((model.外部对象类型?.Length ?? 0) > 30 || (model.外部对象编号?.Length ?? 0) > 100 || (model.关联说明?.Length ?? 0) > 1000)
                throw new ArgumentException("成果关联字段超出长度限制");
            if (!await 可访问成果(model.成果类型, model.成果编号)) return ProjectAccessDenied();
            if (hasProject && !await 可访问科研项目(model.流程模板编号.Value, model.项目编号.Value)) return ProjectAccessDenied();
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研成果关联"" (""成果类型"",""成果编号"",""关联类型"",""流程模板编号"",""项目编号"",""外部对象类型"",""外部对象编号"",""关联说明"",""建立人编号"")
VALUES (@成果类型,@成果编号,@关联类型,@流程模板编号,@项目编号,@外部对象类型,@外部对象编号,@关联说明,@建立人编号)
RETURNING ""编号"";", new
            {
                model.成果类型, model.成果编号, 关联类型 = relation, model.流程模板编号, model.项目编号,
                外部对象类型 = string.IsNullOrWhiteSpace(model.外部对象类型) ? null : model.外部对象类型.Trim(),
                外部对象编号 = string.IsNullOrWhiteSpace(model.外部对象编号) ? null : model.外部对象编号.Trim(),
                关联说明 = string.IsNullOrWhiteSpace(model.关联说明) ? null : model.关联说明.Trim(), 建立人编号 = CurrentUser.编号
            });
            return new { 编号 = id };
        }

        [HttpPost]
        async public Task<object> 核验科研成果作者([FromBody]科研成果作者核验新增 model)
        {
            if (model == null || model.成果类型 < 1 || model.成果类型 > 4 || model.成果编号 <= 0 || model.作者编号 <= 0)
                throw new ArgumentException("成果和作者编号无效");
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员")) return StatusCode(403, new { error = "无权核验成果作者" });
            if (!await 可访问成果(model.成果类型, model.成果编号)) return ProjectAccessDenied();
            if (!await Db.QuerySingleAsync<bool>(@"SELECT EXISTS (SELECT 1 FROM ""人员"" WHERE ""编号""=@作者编号 AND ""是否启用""=TRUE);", new { model.作者编号 }))
                throw new ArgumentException("作者不存在或已停用");
            var status = string.IsNullOrWhiteSpace(model.身份状态) ? "已核验" : model.身份状态.Trim();
            if (status is not ("待核验" or "已核验" or "需更正" or "无法匹配")) throw new ArgumentException("作者身份状态无效");
            if (model.作者顺序.HasValue && model.作者顺序 <= 0) throw new ArgumentException("作者顺序必须大于0");
            if ((model.作者显示姓名?.Length ?? 0) > 100 || (model.单位名称?.Length ?? 0) > 200 || (model.ORCID?.Length ?? 0) > 50 || (model.核验说明?.Length ?? 0) > 1000)
                throw new ArgumentException("作者核验字段超出长度限制");
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研成果作者核验"" (""成果类型"",""成果编号"",""作者编号"",""作者顺序"",""作者显示姓名"",""单位名称"",""ORCID"",""身份状态"",""核验说明"",""核验人编号"",""核验时间"")
VALUES (@成果类型,@成果编号,@作者编号,@作者顺序,@作者显示姓名,@单位名称,@ORCID,@身份状态,@核验说明,@核验人编号,CURRENT_TIMESTAMP)
ON CONFLICT (""成果类型"",""成果编号"",""作者编号"") DO UPDATE SET
  ""作者顺序""=EXCLUDED.""作者顺序"", ""作者显示姓名""=EXCLUDED.""作者显示姓名"", ""单位名称""=EXCLUDED.""单位名称"", ""ORCID""=EXCLUDED.""ORCID"", ""身份状态""=EXCLUDED.""身份状态"", ""核验说明""=EXCLUDED.""核验说明"", ""核验人编号""=EXCLUDED.""核验人编号"", ""核验时间""=CURRENT_TIMESTAMP
RETURNING ""编号"";", new
            {
                model.成果类型, model.成果编号, model.作者编号, model.作者顺序,
                作者显示姓名 = string.IsNullOrWhiteSpace(model.作者显示姓名) ? null : model.作者显示姓名.Trim(),
                单位名称 = string.IsNullOrWhiteSpace(model.单位名称) ? null : model.单位名称.Trim(),
                ORCID = string.IsNullOrWhiteSpace(model.ORCID) ? null : model.ORCID.Trim(), 身份状态 = status,
                核验说明 = string.IsNullOrWhiteSpace(model.核验说明) ? null : model.核验说明.Trim(), 核验人编号 = CurrentUser.编号
            });
            return new { 编号 = id, 身份状态 = status };
        }

        [HttpPost]
        async public Task<object> 更新科研成果治理状态([FromBody]科研成果治理更新 model)
        {
            if (model == null || model.成果类型 < 1 || model.成果类型 > 4 || model.成果编号 <= 0)
                throw new ArgumentException("成果类型和成果编号无效");
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员")) return StatusCode(403, new { error = "无权更新成果治理状态" });
            if (!await 可访问成果(model.成果类型, model.成果编号)) return ProjectAccessDenied();
            var status = model.状态?.Trim();
            if (status is not ("有效" or "待更正" or "已更正" or "已撤稿" or "重复")) throw new ArgumentException("成果治理状态无效");
            if (status == "重复" && (!model.规范成果类型.HasValue || !model.规范成果编号.HasValue || (model.规范成果类型 == model.成果类型 && model.规范成果编号 == model.成果编号)))
                throw new ArgumentException("标记重复时必须提供不同的规范成果");
            if (status != "重复" && (model.规范成果类型.HasValue || model.规范成果编号.HasValue))
                throw new ArgumentException("只有重复成果状态可以提供规范成果");
            if (status == "重复" && (model.规范成果类型 < 1 || model.规范成果类型 > 4 || !await 成果存在(model.规范成果类型.Value, model.规范成果编号.Value)))
                throw new ArgumentException("规范成果不存在，不能建立重复合并映射");
            var reason = string.IsNullOrWhiteSpace(model.治理原因) ? null : model.治理原因.Trim();
            if ((status is "待更正" or "已更正" or "已撤稿" or "重复") && string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("更正、撤稿或重复合并必须填写治理原因");
            if ((reason?.Length ?? 0) > 1000) throw new ArgumentException("治理原因不能超过1000个字符");

            var sourceType = string.IsNullOrWhiteSpace(model.来源类型) ? null : model.来源类型.Trim();
            var sourceId = string.IsNullOrWhiteSpace(model.来源凭证编号) ? null : model.来源凭证编号.Trim();
            var sourceSummary = string.IsNullOrWhiteSpace(model.来源凭证摘要) ? null : model.来源凭证摘要.Trim();
            var sourcePath = string.IsNullOrWhiteSpace(model.来源文件路径) ? null : model.来源文件路径.Trim();
            var sourceHash = string.IsNullOrWhiteSpace(model.来源文件SHA256) ? null : model.来源文件SHA256.Trim();
            var sourceSystem = string.IsNullOrWhiteSpace(model.来源系统) ? null : model.来源系统.Trim();
            var sourceTypes = new[] { "期刊/出版社通知", "数据库状态", "作者/机构申请", "伦理/监管决定", "内部核查", "其他" };
            if (sourceType != null && !sourceTypes.Contains(sourceType)) throw new ArgumentException("来源类型无效");
            if ((sourceId?.Length ?? 0) > 200 || (sourceSummary?.Length ?? 0) > 1000 || (sourcePath?.Length ?? 0) > 500 || (sourceHash?.Length ?? 0) > 64 || (sourceSystem?.Length ?? 0) > 100)
                throw new ArgumentException("状态来源凭证字段超出长度限制");
            if (sourceId != null && sourceId.Contains("demo", StringComparison.OrdinalIgnoreCase) || sourceId != null && sourceId.Contains("sample", StringComparison.OrdinalIgnoreCase) || sourceId != null && sourceId.Contains("test", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("来源凭证编号不能使用示例或测试值");
            if (sourceHash != null && (!IsSha256(sourceHash) || sourcePath == null)) throw new ArgumentException("提供来源文件 SHA-256 时必须同时提供文件路径和 64 位十六进制校验值");

            await using var connection = new NpgsqlConnection(DbConnectionString);
            await connection.OpenAsync(HttpContext.RequestAborted);
            await using var transaction = await connection.BeginTransactionAsync(HttpContext.RequestAborted);
            var current = await connection.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""状态"", ""状态凭证编号"" FROM ""科研成果治理""
 WHERE ""成果类型""=@成果类型 AND ""成果编号""=@成果编号 FOR UPDATE;", new { model.成果类型, model.成果编号 }, transaction);
            var oldStatus = current == null ? null : (string)current.状态;
            var needsEvidence = status is "待更正" or "已更正" or "已撤稿"
                || (status == "有效" && (oldStatus is "待更正" or "已更正" or "已撤稿"));
            if (needsEvidence)
            {
                if (sourceType == null || sourceId == null || sourceSummary == null)
                    throw new ArgumentException("撤稿、更正和恢复有效必须登记来源类型、凭证编号和凭证摘要；记录初始为待核验");
            }
            else if (sourceType != null || sourceId != null || sourceSummary != null || sourcePath != null || sourceHash != null || sourceSystem != null)
            {
                throw new ArgumentException("当前状态不需要来源凭证；如需登记撤稿或更正，请选择对应状态");
            }

            long? evidenceId = null;
            if (needsEvidence)
            {
                var eventType = status switch
                {
                    "待更正" => "更正通知",
                    "已更正" => "更正完成",
                    "已撤稿" => "撤稿通知",
                    _ => "恢复有效"
                };
                evidenceId = await connection.QuerySingleAsync<long>(@"
INSERT INTO ""科研成果状态凭证"" (
    ""成果类型"",""成果编号"",""原状态"",""新状态"",""事件类型"",""来源类型"",
    ""来源凭证编号"",""来源凭证摘要"",""来源文件路径"",""来源文件SHA256"",""来源系统"",""操作人编号""
)
VALUES (@成果类型,@成果编号,@原状态,@新状态,@事件类型,@来源类型,
        @来源凭证编号,@来源凭证摘要,@来源文件路径,@来源文件SHA256,@来源系统,@操作人编号)
RETURNING ""编号"";", new
                {
                    model.成果类型, model.成果编号, 原状态 = oldStatus, 新状态 = status, 事件类型 = eventType,
                    来源类型 = sourceType, 来源凭证编号 = sourceId, 来源凭证摘要 = sourceSummary,
                    来源文件路径 = sourcePath, 来源文件SHA256 = sourceHash, 来源系统 = sourceSystem,
                    操作人编号 = CurrentUser.编号
                }, transaction);
            }
            var id = await connection.QuerySingleAsync<long>(@"
INSERT INTO ""科研成果治理"" (""成果类型"",""成果编号"",""状态"",""规范成果类型"",""规范成果编号"",""治理原因"",""状态凭证编号"",""操作人编号"")
VALUES (@成果类型,@成果编号,@状态,@规范成果类型,@规范成果编号,@治理原因,@状态凭证编号,@操作人编号)
ON CONFLICT (""成果类型"",""成果编号"") DO UPDATE SET
  ""状态""=EXCLUDED.""状态"", ""规范成果类型""=EXCLUDED.""规范成果类型"", ""规范成果编号""=EXCLUDED.""规范成果编号"", ""治理原因""=EXCLUDED.""治理原因"", ""状态凭证编号""=EXCLUDED.""状态凭证编号"", ""操作人编号""=EXCLUDED.""操作人编号"", ""操作时间""=CURRENT_TIMESTAMP
RETURNING ""编号"";", new
            {
                model.成果类型, model.成果编号, 状态 = status, model.规范成果类型, model.规范成果编号,
                治理原因 = reason, 状态凭证编号 = evidenceId, 操作人编号 = CurrentUser.编号
            }, transaction);
            long? mergeId = null;
            if (status == "重复")
            {
                mergeId = await connection.QuerySingleOrDefaultAsync<long?>(@"
INSERT INTO ""科研成果合并审计"" (""来源成果类型"",""来源成果编号"",""规范成果类型"",""规范成果编号"",""合并原因"",""操作人编号"")
VALUES (@来源成果类型,@来源成果编号,@规范成果类型,@规范成果编号,@合并原因,@操作人编号)
ON CONFLICT (""来源成果类型"",""来源成果编号"",""规范成果类型"",""规范成果编号"") DO UPDATE SET
  ""合并原因""=EXCLUDED.""合并原因"", ""操作人编号""=EXCLUDED.""操作人编号"", ""操作时间""=CURRENT_TIMESTAMP
RETURNING ""编号"";", new
                {
                    来源成果类型 = model.成果类型, 来源成果编号 = model.成果编号,
                    规范成果类型 = model.规范成果类型.Value, 规范成果编号 = model.规范成果编号.Value,
                    合并原因 = reason, 操作人编号 = CurrentUser.编号
                }, transaction);
            }
            await transaction.CommitAsync(HttpContext.RequestAborted);
            return new { 编号 = id, 状态 = status, 合并审计编号 = mergeId, 状态凭证编号 = evidenceId, 凭证核验状态 = evidenceId.HasValue ? "待核验" : null };
        }

        /// <summary>由系统管理员对撤稿/更正来源凭证追加核验结论；核验记录不可覆盖，默认不代表已核验。</summary>
        [HttpPost]
        async public Task<object> 核验科研成果状态凭证([FromBody]科研成果状态凭证核验 model)
        {
            if (!await HasAnyRoleAsync("系统管理员")) return StatusCode(403, new { error = "只有系统管理员可以核验成果状态来源凭证" });
            if (model == null || model.状态凭证编号 <= 0) throw new ArgumentException("状态凭证编号无效");
            var result = model.核验结果?.Trim();
            var note = model.核验说明?.Trim();
            if (result is not ("已核验" or "不通过")) throw new ArgumentException("核验结果无效");
            if (string.IsNullOrWhiteSpace(note) || note.Length > 1000) throw new ArgumentException("核验说明不能为空且不能超过1000个字符");
            var evidence = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""来源类型"", ""来源文件路径"", ""来源文件SHA256""
  FROM ""科研成果状态凭证"" WHERE ""编号""=@状态凭证编号;", new { model.状态凭证编号 });
            if (evidence == null) return NotFound(new { error = "状态来源凭证不存在" });
            if (result == "已核验" && (string)evidence.来源类型 == "历史迁移")
                throw new ArgumentException("历史迁移凭证仅用于标识待补录状态，不能直接核验通过");
            if (result == "已核验" && (string.IsNullOrWhiteSpace((string)evidence.来源文件路径) || !IsSha256((string)evidence.来源文件SHA256)))
                throw new ArgumentException("核验通过前必须登记来源文件路径和有效 SHA-256 校验值");
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研成果状态凭证核验"" (""状态凭证编号"",""核验结果"",""核验说明"",""核验人编号"")
VALUES (@状态凭证编号,@核验结果,@核验说明,@核验人编号)
RETURNING ""编号"";", new { model.状态凭证编号, 核验结果 = result, 核验说明 = note, 核验人编号 = CurrentUser.编号 });
            return new { 编号 = id, 状态凭证编号 = model.状态凭证编号, 核验结果 = result };
        }

        [HttpGet]
        async public Task<object> 获取科研单位规范(string 关键词 = null, bool? 仅启用 = null)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员")) return StatusCode(403, new { error = "无权查看科研单位规范" });
            return await Db.QueryAsync<object>(@"
SELECT u.""编号"", u.""规范名称"", u.""统一社会信用代码"", u.""状态"", u.""备注"", u.""建立时间"" AS ""创建时间"",
       coalesce(jsonb_agg(jsonb_build_object('编号',a.""编号"",'别名',a.""别名"",'来源',a.""来源"",'是否启用',a.""是否启用"")) FILTER (WHERE a.""编号"" IS NOT NULL), '[]'::jsonb) AS ""别名""
  FROM ""科研单位规范"" u LEFT JOIN ""科研单位别名"" a ON a.""规范单位编号""=u.""编号""
 WHERE (@关键词 IS NULL OR u.""规范名称"" ILIKE '%'||@关键词||'%' OR a.""别名"" ILIKE '%'||@关键词||'%')
   AND (@仅启用 IS NULL OR u.""状态""='启用')
 GROUP BY u.""编号"" ORDER BY u.""规范名称"";", new { 关键词 = string.IsNullOrWhiteSpace(关键词) ? null : 关键词.Trim(), 仅启用 });
        }

        [HttpPost]
        async public Task<object> 保存科研单位规范([FromBody]科研单位规范保存 model)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员")) return StatusCode(403, new { error = "无权维护科研单位规范" });
            if (model == null || string.IsNullOrWhiteSpace(model.规范名称)) throw new ArgumentException("规范单位名称不能为空");
            if (model.规范名称.Trim().Length > 300 || (model.统一社会信用代码?.Length ?? 0) > 50 || (model.备注?.Length ?? 0) > 1000) throw new ArgumentException("单位规范字段超出限制");
            var status = string.IsNullOrWhiteSpace(model.状态) ? "启用" : model.状态.Trim();
            if (status is not ("启用" or "停用")) throw new ArgumentException("单位规范状态无效");
            if (model.编号.HasValue)
            {
                var changed = await Db.ExecuteAsync(@"UPDATE ""科研单位规范"" SET ""规范名称""=@规范名称,""统一社会信用代码""=@统一社会信用代码,""状态""=@状态,""备注""=@备注,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号;", new
                { 编号 = model.编号.Value, 规范名称 = model.规范名称.Trim(), 统一社会信用代码 = string.IsNullOrWhiteSpace(model.统一社会信用代码) ? null : model.统一社会信用代码.Trim(), 状态 = status, 备注 = string.IsNullOrWhiteSpace(model.备注) ? null : model.备注.Trim() });
                if (changed == 0) return NotFound(new { error = "规范单位不存在" });
                return new { 编号 = model.编号.Value, 状态 = status };
            }
            var id = await Db.QuerySingleAsync<long>(@"INSERT INTO ""科研单位规范"" (""规范名称"",""统一社会信用代码"",""状态"",""备注"",""建立人编号"") VALUES (@规范名称,@统一社会信用代码,@状态,@备注,@建立人编号) RETURNING ""编号"";", new
            { 规范名称 = model.规范名称.Trim(), 统一社会信用代码 = string.IsNullOrWhiteSpace(model.统一社会信用代码) ? null : model.统一社会信用代码.Trim(), 状态 = status, 备注 = string.IsNullOrWhiteSpace(model.备注) ? null : model.备注.Trim(), 建立人编号 = CurrentUser.编号 });
            return new { 编号 = id, 状态 = status };
        }

        [HttpPost]
        async public Task<object> 保存科研单位别名([FromBody]科研单位别名保存 model)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员")) return StatusCode(403, new { error = "无权维护科研单位别名" });
            if (model == null || model.规范单位编号 <= 0 || string.IsNullOrWhiteSpace(model.别名)) throw new ArgumentException("规范单位和别名不能为空");
            if (model.别名.Trim().Length > 300 || (model.来源?.Length ?? 0) > 100) throw new ArgumentException("单位别名字段超出限制");
            if (!await Db.QuerySingleAsync<bool>("SELECT EXISTS (SELECT 1 FROM \"科研单位规范\" WHERE \"编号\"=@规范单位编号 AND \"状态\"='启用');", new { model.规范单位编号 })) throw new ArgumentException("规范单位不存在或已停用");
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研单位别名"" (""规范单位编号"",""别名"",""来源"",""是否启用"",""建立人编号"") VALUES (@规范单位编号,@别名,@来源,@是否启用,@建立人编号)
ON CONFLICT (""别名"") DO UPDATE SET ""规范单位编号""=EXCLUDED.""规范单位编号"",""来源""=EXCLUDED.""来源"",""是否启用""=EXCLUDED.""是否启用"" RETURNING ""编号"";", new
            { model.规范单位编号, 别名 = model.别名.Trim(), 来源 = string.IsNullOrWhiteSpace(model.来源) ? null : model.来源.Trim(), model.是否启用, 建立人编号 = CurrentUser.编号 });
            return new { 编号 = id };
        }

        [HttpPost]
        async public Task<object> 规范科研成果作者单位([FromBody]科研成果作者单位规范 model)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员")) return StatusCode(403, new { error = "无权规范成果作者单位" });
            if (model == null || model.成果类型 < 1 || model.成果类型 > 4 || model.成果编号 <= 0 || model.作者编号 <= 0) throw new ArgumentException("成果和作者编号无效");
            var status = string.IsNullOrWhiteSpace(model.单位规范状态) ? "已规范" : model.单位规范状态.Trim();
            if (status is not ("已规范" or "无需规范" or "待规范")) throw new ArgumentException("单位规范状态无效");
            if (status == "已规范" && !model.规范单位编号.HasValue) throw new ArgumentException("已规范状态必须提供规范单位");
            if (model.规范单位编号.HasValue && !await Db.QuerySingleAsync<bool>("SELECT EXISTS (SELECT 1 FROM \"科研单位规范\" WHERE \"编号\"=@规范单位编号 AND \"状态\"='启用');", new { model.规范单位编号 })) throw new ArgumentException("规范单位不存在或已停用");
            var changed = await Db.ExecuteAsync(@"UPDATE ""科研成果作者核验"" SET ""规范单位编号""=@规范单位编号,""单位规范状态""=@单位规范状态 WHERE ""成果类型""=@成果类型 AND ""成果编号""=@成果编号 AND ""作者编号""=@作者编号;", new
            { model.成果类型, model.成果编号, model.作者编号, model.规范单位编号, 单位规范状态 = status });
            if (changed == 0) return NotFound(new { error = "成果作者核验记录不存在，请先完成作者核验" });
            return new { 状态 = status, 规范单位编号 = model.规范单位编号 };
        }

        #region 成果转化与收益分配

        /// <summary>查询当前用户可见的成果转化台账。管理角色可查看全院，普通用户只能查看本人作者成果。</summary>
        [HttpGet]
        async public Task<object> 获取成果转化台账(int? 成果类型 = null, int? 成果编号 = null, string 状态 = null)
        {
            var isAdmin = await HasAnyRoleAsync("系统管理员", "科研管理员");
            return await Db.QueryAsync<object>(@"
SELECT c.""编号"", c.""成果类型"", c.""成果编号"", c.""转化名称"", c.""转化方式"", c.""合作单位"",
       c.""合同编号"", c.""合同签署日期"", c.""合同金额"", c.""申请说明"", c.""状态"", c.""审批意见"",
       c.""申请人编号"", ap.""姓名"" AS ""申请人姓名"", c.""审批人编号"", sp.""姓名"" AS ""审批人姓名"",
       c.""审批时间"", c.""建立时间"", c.""更新时间"",
       COALESCE((SELECT p.""论文标题"" FROM ""成果论文"" p WHERE c.""成果类型""=1 AND p.""编号""=c.""成果编号""),
                (SELECT b.""著作名称"" FROM ""成果著作"" b WHERE c.""成果类型""=2 AND b.""编号""=c.""成果编号""),
                (SELECT t.""专利名称"" FROM ""成果专利"" t WHERE c.""成果类型""=3 AND t.""编号""=c.""成果编号""),
                (SELECT a.""获奖名称"" FROM ""成果获奖"" a WHERE c.""成果类型""=4 AND a.""编号""=c.""成果编号"")) AS ""成果名称"",
       COALESCE((SELECT SUM(r.""实际金额"") FROM ""科研成果转化收益"" r WHERE r.""转化编号""=c.""编号""),0) AS ""已收款金额"",
       COALESCE((SELECT COUNT(*) FROM ""科研成果转化收益"" r WHERE r.""转化编号""=c.""编号""),0) AS ""收益记录数""
  FROM ""科研成果转化"" c
  LEFT JOIN ""人员"" ap ON ap.""编号""=c.""申请人编号""
  LEFT JOIN ""人员"" sp ON sp.""编号""=c.""审批人编号""
 WHERE (@成果类型 IS NULL OR c.""成果类型""=@成果类型)
   AND (@成果编号 IS NULL OR c.""成果编号""=@成果编号)
   AND (@状态 IS NULL OR c.""状态""=@状态)
   AND (@管理角色 OR EXISTS (SELECT 1 FROM ""成果作者"" au WHERE au.""成果类型""=c.""成果类型"" AND au.""成果编号""=c.""成果编号"" AND au.""作者编号""=@人员编号)
        OR (@人员编号 = c.""申请人编号""))
 ORDER BY c.""更新时间"" DESC, c.""编号"" DESC;", new
            { 成果类型, 成果编号, 状态 = 空值(状态), 管理角色 = isAdmin, 人员编号 = CurrentUser.编号 });
        }

        [HttpGet]
        async public Task<object> 获取成果转化详情(long 转化编号)
        {
            if (转化编号 <= 0) throw new ArgumentException("转化编号无效");
            var conversion = await Db.QuerySingleOrDefaultAsync<dynamic>("SELECT * FROM \"科研成果转化\" WHERE \"编号\"=@转化编号;", new { 转化编号 });
            if (conversion == null) return NotFound(new { error = "成果转化记录不存在" });
            if (!await 可维护成果转化((int)conversion.成果类型, (int)conversion.成果编号, CurrentUser.编号)) return ProjectAccessDenied();
            var revenues = await Db.QueryAsync<object>(@"
SELECT r.*, COALESCE((SELECT SUM(d.""分配金额"") FROM ""科研成果转化收益分配"" d WHERE d.""收益编号""=r.""编号""),0) AS ""已分配金额""
  FROM ""科研成果转化收益"" r WHERE r.""转化编号""=@转化编号 ORDER BY r.""收款日期"" NULLS LAST, r.""编号"" DESC;", new { 转化编号 });
            var distributions = await Db.QueryAsync<object>(@"
SELECT d.*, p.""姓名"", r.""转化编号"", r.""实际金额""
  FROM ""科研成果转化收益分配"" d JOIN ""人员"" p ON p.""编号""=d.""人员编号""
  JOIN ""科研成果转化收益"" r ON r.""编号""=d.""收益编号""
 WHERE r.""转化编号""=@转化编号 ORDER BY d.""编号"" DESC;", new { 转化编号 });
            return new { 转化 = conversion, 收益 = revenues, 分配 = distributions };
        }

        [HttpPost]
        async public Task<object> 登记成果转化([FromBody]科研成果转化保存 model)
        {
            if (model == null || model.成果类型 < 1 || model.成果类型 > 4 || model.成果编号 <= 0 || string.IsNullOrWhiteSpace(model.转化名称))
                throw new ArgumentException("成果和转化名称不能为空");
            if (model.转化名称.Trim().Length > 300 || (model.合作单位?.Length ?? 0) > 300 || (model.合同编号?.Length ?? 0) > 100 || (model.申请说明?.Length ?? 0) > 4000)
                throw new ArgumentException("成果转化字段超出限制");
            if (model.合同金额 < 0) throw new ArgumentException("合同金额不能为负数");
            if (!await 可维护成果转化(model.成果类型, model.成果编号, CurrentUser.编号)) return ProjectAccessDenied();
            var way = string.IsNullOrWhiteSpace(model.转化方式) ? "技术转让" : model.转化方式.Trim();
            if (way is not ("技术转让" or "许可使用" or "作价入股" or "合作开发" or "其他")) throw new ArgumentException("转化方式无效");
            if (model.编号.HasValue)
            {
                var isAdmin = await HasAnyRoleAsync("系统管理员", "科研管理员");
                var changed = await Db.ExecuteAsync(@"
UPDATE ""科研成果转化"" SET ""转化名称""=@转化名称,""转化方式""=@转化方式,""合作单位""=@合作单位,""合同编号""=@合同编号,
       ""合同签署日期""=@合同签署日期,""合同金额""=@合同金额,""申请说明""=@申请说明,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@编号 AND (@管理角色 OR ""申请人编号""=@申请人编号) AND ""状态"" IN ('草稿','待审批');", new
                { 编号 = model.编号.Value, model.成果类型, model.成果编号, 转化名称 = model.转化名称.Trim(), 转化方式 = way,
                    合作单位 = 空值(model.合作单位), 合同编号 = 空值(model.合同编号), model.合同签署日期, model.合同金额,
                    申请说明 = 空值(model.申请说明), 申请人编号 = CurrentUser.编号, 管理角色 = isAdmin });
                if (changed == 0) return NotFound(new { error = "转化记录不存在、无权修改或已进入审批" });
                return new { 编号 = model.编号.Value, 状态 = "待审批" };
            }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研成果转化"" (""成果类型"",""成果编号"",""转化名称"",""转化方式"",""合作单位"",""合同编号"",""合同签署日期"",""合同金额"",""申请说明"",""申请人编号"")
VALUES (@成果类型,@成果编号,@转化名称,@转化方式,@合作单位,@合同编号,@合同签署日期,@合同金额,@申请说明,@申请人编号)
RETURNING ""编号"";", new
            { model.成果类型, model.成果编号, 转化名称 = model.转化名称.Trim(), 转化方式 = way, 合作单位 = 空值(model.合作单位), 合同编号 = 空值(model.合同编号), model.合同签署日期, model.合同金额, 申请说明 = 空值(model.申请说明), 申请人编号 = CurrentUser.编号 });
            return new { 编号 = id, 状态 = "待审批" };
        }

        [HttpPost]
        async public Task<object> 审批成果转化([FromBody]科研成果转化审批 model)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员")) return StatusCode(403, new { error = "无权审批成果转化" });
            if (model == null || model.转化编号 <= 0) throw new ArgumentException("转化编号无效");
            var status = model.状态?.Trim();
            if (status is not ("已批准" or "已驳回" or "执行中" or "已完成" or "已终止")) throw new ArgumentException("审批状态无效");
            if ((model.审批意见?.Length ?? 0) > 4000) throw new ArgumentException("审批意见不能超过4000个字符");
            if (status == "已完成")
            {
                var openRevenue = await Db.QuerySingleAsync<int>(@"
SELECT COUNT(*) FROM ""科研成果转化收益"" r
 WHERE r.""转化编号""=@转化编号
   AND (r.""实际金额"" <= 0 OR r.""分配状态"" <> '已分配' OR EXISTS (SELECT 1 FROM ""科研成果转化收益分配"" d WHERE d.""收益编号""=r.""编号"" AND d.""财务回写状态"" <> '已回写'));", new { model.转化编号 });
                if (openRevenue > 0) throw new ArgumentException($"仍有 {openRevenue} 条收益未完成分配或财务回写，不能将成果转化标记为已完成");
            }
            var changed = await Db.ExecuteAsync(@"
UPDATE ""科研成果转化"" SET ""状态""=@状态,""审批意见""=@审批意见,""审批人编号""=@审批人编号,""审批时间""=CURRENT_TIMESTAMP,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@转化编号 AND ""状态"" IN ('待审批','已批准','执行中');", new
            { model.转化编号, 状态 = status, 审批意见 = 空值(model.审批意见), 审批人编号 = CurrentUser.编号 });
            if (changed == 0) return NotFound(new { error = "转化记录不存在或当前状态不允许审批" });
            return new { 编号 = model.转化编号, 状态 = status };
        }

        [HttpPost]
        async public Task<object> 登记成果转化收益([FromBody]科研成果转化收益保存 model)
        {
            if (model == null || model.转化编号 <= 0) throw new ArgumentException("转化编号无效");
            if (model.计划金额 < 0 || model.实际金额 < 0) throw new ArgumentException("收益金额不能为负数");
            if (model.实际金额 > 0 && (string.IsNullOrWhiteSpace(model.收款凭证号) || !model.收款日期.HasValue)) throw new ArgumentException("实际收款大于零时必须提供收款日期和收款凭证号");
            if ((model.收款凭证号?.Length ?? 0) > 100 || (model.备注?.Length ?? 0) > 2000) throw new ArgumentException("收益字段超出限制");
            var conversion = await Db.QuerySingleOrDefaultAsync<dynamic>("SELECT \"成果类型\",\"成果编号\",\"状态\" FROM \"科研成果转化\" WHERE \"编号\"=@转化编号;", new { model.转化编号 });
            if (conversion == null) return NotFound(new { error = "转化记录不存在" });
            if (!await 可维护成果转化((int)conversion.成果类型, (int)conversion.成果编号, CurrentUser.编号)) return ProjectAccessDenied();
            if (conversion.状态 is not ("已批准" or "执行中" or "已完成")) throw new ArgumentException("转化尚未批准，不能登记收益");
            var type = string.IsNullOrWhiteSpace(model.收益类型) ? "合同款" : model.收益类型.Trim();
            if (type is not ("合同款" or "许可费" or "分成" or "其他")) throw new ArgumentException("收益类型无效");
            if (model.编号.HasValue)
            {
                var financeLocked = await Db.QuerySingleAsync<bool>("SELECT EXISTS (SELECT 1 FROM \"科研成果转化收益分配\" WHERE \"收益编号\"=@编号 AND \"财务回写状态\"='已回写');", new { 编号 = model.编号.Value });
                if (financeLocked) throw new ArgumentException("该收益已有财务回写凭证，不能修改收款金额；请登记冲正或更正记录");
                var allocated = await Db.QuerySingleAsync<decimal>("SELECT COALESCE(SUM(\"分配金额\"),0) FROM \"科研成果转化收益分配\" WHERE \"收益编号\"=@编号;", new { 编号 = model.编号.Value });
                if (model.实际金额 < allocated) throw new ArgumentException("实际收款金额不能低于已登记的收益分配金额");
                var changed = await Db.ExecuteAsync(@"
UPDATE ""科研成果转化收益"" SET ""收益类型""=@收益类型,""计划金额""=@计划金额,""实际金额""=@实际金额,""收款日期""=@收款日期,""收款凭证号""=@收款凭证号,""备注""=@备注,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@编号 AND ""转化编号""=@转化编号;", new { 编号 = model.编号.Value, model.转化编号, 收益类型 = type, model.计划金额, model.实际金额, model.收款日期, 收款凭证号 = 空值(model.收款凭证号), 备注 = 空值(model.备注) });
                if (changed == 0) return NotFound(new { error = "收益记录不存在" });
                await Db.ExecuteAsync("UPDATE \"科研成果转化\" SET \"状态\"=CASE WHEN \"状态\"='已批准' THEN '执行中' ELSE \"状态\" END,\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@转化编号;", new { model.转化编号 });
                return new { 编号 = model.编号.Value };
            }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研成果转化收益"" (""转化编号"",""收益类型"",""计划金额"",""实际金额"",""收款日期"",""收款凭证号"",""备注"",""建立人编号"")
VALUES (@转化编号,@收益类型,@计划金额,@实际金额,@收款日期,@收款凭证号,@备注,@建立人编号) RETURNING ""编号"";", new { model.转化编号, 收益类型 = type, model.计划金额, model.实际金额, model.收款日期, 收款凭证号 = 空值(model.收款凭证号), 备注 = 空值(model.备注), 建立人编号 = CurrentUser.编号 });
            await Db.ExecuteAsync("UPDATE \"科研成果转化\" SET \"状态\"=CASE WHEN \"状态\"='已批准' THEN '执行中' ELSE \"状态\" END,\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@转化编号;", new { model.转化编号 });
            return new { 编号 = id };
        }

        [HttpPost]
        async public Task<object> 保存成果转化收益分配([FromBody]科研成果转化收益分配保存 model)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员")) return StatusCode(403, new { error = "无权维护收益分配" });
            if (model == null || model.收益编号 <= 0 || model.人员编号 <= 0) throw new ArgumentException("收益和人员不能为空");
            if (model.分配比例 < 0 || model.分配比例 > 1 || model.分配金额 < 0) throw new ArgumentException("分配比例或金额无效");
            if ((model.备注?.Length ?? 0) > 1000) throw new ArgumentException("分配备注不能超过1000个字符");
            await using var conn = new NpgsqlConnection(DbConnectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();
            var revenue = await conn.QuerySingleOrDefaultAsync<dynamic>("SELECT \"实际金额\",\"转化编号\" FROM \"科研成果转化收益\" WHERE \"编号\"=@收益编号 FOR UPDATE;", new { model.收益编号 }, tx);
            if (revenue == null) return NotFound(new { error = "收益记录不存在" });
            var conversion = await conn.QuerySingleOrDefaultAsync<dynamic>("SELECT \"成果类型\",\"成果编号\" FROM \"科研成果转化\" WHERE \"编号\"=@转化编号;", new { 转化编号 = (long)revenue.转化编号 }, tx);
            if (conversion == null || !await 可维护成果转化((int)conversion.成果类型, (int)conversion.成果编号, CurrentUser.编号)) return ProjectAccessDenied();
            var existingDistribution = await conn.QuerySingleOrDefaultAsync<dynamic>("SELECT \"分配金额\",\"财务回写状态\" FROM \"科研成果转化收益分配\" WHERE \"收益编号\"=@收益编号 AND \"人员编号\"=@人员编号;", new { model.收益编号, model.人员编号 }, tx);
            if (existingDistribution != null && (string)existingDistribution.财务回写状态 == "已回写") throw new ArgumentException("该人员的收益分配已完成财务回写，不能修改");
            var sum = await conn.QuerySingleAsync<decimal>("SELECT COALESCE(SUM(\"分配金额\"),0) FROM \"科研成果转化收益分配\" WHERE \"收益编号\"=@收益编号 AND \"人员编号\"<>@人员编号;", new { model.收益编号, model.人员编号 }, tx);
            if (sum + model.分配金额 > (decimal)revenue.实际金额) throw new ArgumentException("分配金额不能超过实际收款金额");
            var id = await conn.QuerySingleAsync<long>(@"
INSERT INTO ""科研成果转化收益分配"" (""收益编号"",""人员编号"",""分配比例"",""分配金额"",""备注"",""建立人编号"")
VALUES (@收益编号,@人员编号,@分配比例,@分配金额,@备注,@建立人编号)
ON CONFLICT (""收益编号"",""人员编号"") DO UPDATE SET ""分配比例""=EXCLUDED.""分配比例"",""分配金额""=EXCLUDED.""分配金额"",""备注""=EXCLUDED.""备注"",""更新时间""=CURRENT_TIMESTAMP
RETURNING ""编号"";", new { model.收益编号, model.人员编号, model.分配比例, model.分配金额, 备注 = 空值(model.备注), 建立人编号 = CurrentUser.编号 }, tx);
            var newSum = sum + model.分配金额;
            await conn.ExecuteAsync("UPDATE \"科研成果转化收益\" SET \"分配状态\"=CASE WHEN @金额=0 THEN '待分配' WHEN @金额>=\"实际金额\" THEN '已分配' ELSE '部分分配' END,\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@收益编号;", new { model.收益编号, 金额 = newSum }, tx);
            await tx.CommitAsync();
            return new { 编号 = id, 分配状态 = newSum >= (decimal)revenue.实际金额 && (decimal)revenue.实际金额 > 0 ? "已分配" : (newSum > 0 ? "部分分配" : "待分配") };
        }

        [HttpPost]
        async public Task<object> 回写成果转化财务状态([FromBody]科研成果转化财务回写 model)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员")) return StatusCode(403, new { error = "无权回写成果转化财务状态" });
            if (model == null || model.分配编号 <= 0) throw new ArgumentException("分配编号无效");
            var status = model.财务回写状态?.Trim();
            if (status is not ("待回写" or "已回写" or "回写失败")) throw new ArgumentException("财务回写状态无效");
            if (status == "已回写" && string.IsNullOrWhiteSpace(model.财务凭证号)) throw new ArgumentException("已回写必须提供财务凭证号");
            if (status == "回写失败" && string.IsNullOrWhiteSpace(model.财务回写说明)) throw new ArgumentException("回写失败必须填写失败说明");
            var existingFinanceStatus = await Db.QuerySingleOrDefaultAsync<string>("SELECT \"财务回写状态\" FROM \"科研成果转化收益分配\" WHERE \"编号\"=@分配编号;", new { model.分配编号 });
            if (existingFinanceStatus == null) return NotFound(new { error = "收益分配记录不存在" });
            if (existingFinanceStatus == "已回写" && status != "已回写") throw new ArgumentException("已回写财务记录不能回退状态");
            var changed = await Db.ExecuteAsync(@"
UPDATE ""科研成果转化收益分配"" SET ""财务回写状态""=@财务回写状态,""财务凭证号""=@财务凭证号,""财务回写说明""=@财务回写说明,""财务回写时间""=CASE WHEN @财务回写状态='待回写' THEN NULL ELSE CURRENT_TIMESTAMP END,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@分配编号;", new { model.分配编号, 财务回写状态 = status, 财务凭证号 = 空值(model.财务凭证号), 财务回写说明 = 空值(model.财务回写说明) });
            if (changed == 0) return NotFound(new { error = "收益分配记录不存在" });
            return new { 编号 = model.分配编号, 财务回写状态 = status };
        }

        #endregion

        private int 成果流程模板编号(int 成果类型编号) => 成果类型编号 switch
        {
            (int)成果类型.论文 => 论文管理流程模板编号,
            (int)成果类型.著作 => 著作管理流程模板编号,
            (int)成果类型.专利 => 专利管理流程模板编号,
            (int)成果类型.获奖 => 获奖管理流程模板编号,
            _ => 0
        };

        private async Task<bool> 可访问成果(int 成果类型, int 成果编号)
        {
            var templateId = 成果流程模板编号(成果类型);
            if (templateId <= 0 || 成果编号 <= 0) return false;
            if (!await 成果存在(成果类型, 成果编号)) return false;
            return await HasAnyRoleAsync("系统管理员", "科研管理员") || await CanAccessProjectAsync(templateId, 成果编号);
        }

        private async Task<bool> 成果存在(int 成果类型, int 成果编号)
        {
            if (成果类型 < 1 || 成果类型 > 4 || 成果编号 <= 0) return false;
            return await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (
    SELECT 1 FROM ""成果论文"" WHERE @成果类型=1 AND ""编号""=@成果编号
    UNION ALL SELECT 1 FROM ""成果著作"" WHERE @成果类型=2 AND ""编号""=@成果编号
    UNION ALL SELECT 1 FROM ""成果专利"" WHERE @成果类型=3 AND ""编号""=@成果编号
    UNION ALL SELECT 1 FROM ""成果获奖"" WHERE @成果类型=4 AND ""编号""=@成果编号
);", new { 成果类型, 成果编号 });
        }

        private async Task<bool> 可访问科研项目(int 流程模板编号, int 项目编号)
        {
            if (流程模板编号 <= 0 || 项目编号 <= 0) return false;
            return await HasAnyRoleAsync("系统管理员", "科研管理员") || await CanAccessProjectAsync(流程模板编号, 项目编号);
        }

        private async Task<bool> 可维护成果转化(int 成果类型, int 成果编号, int 申请人编号)
        {
            if (await HasAnyRoleAsync("系统管理员", "科研管理员")) return true;
            if (await 可访问成果(成果类型, 成果编号)) return true;
            return await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (
  SELECT 1 FROM ""成果作者"" WHERE ""成果类型""=@成果类型 AND ""成果编号""=@成果编号 AND ""作者编号""=@人员编号
  UNION ALL SELECT 1 FROM ""成果论文"" WHERE @成果类型=1 AND ""编号""=@成果编号 AND ""认领人编号""=@人员编号
);", new { 成果类型, 成果编号, 人员编号 = 申请人编号 });
        }

        private static string 空值(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static bool IsSha256(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != 64) return false;
            return value.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
        }

        #region 论文主动大数据网推、机构别名消歧与中科院分区自动打标 (清华长庚医院实践)

        /// <summary>
        /// 获取论文主动网推台账 (包含中科院分区、Top期刊标识、影响因子、AI消歧置信度与命中依据)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> 获取论文主动网推台账(
            [FromQuery] string status = null,
            [FromQuery] int? claimantId = null,
            [FromQuery] string casPartition = null,
            [FromQuery] string keyword = null)
        {
            var sql = @"
SELECT r.""编号"", r.""外部论文编号"", r.""论文编号"", r.""论文标题"", r.""作者"", r.""刊物名称"",
       r.""ISSN号"", r.""CN号"", r.""中科院分区"", r.""中科院大类学科"", r.""是否Top期刊"",
       r.""影响因子"", r.""JCR分区"", r.""推荐认领人编号"", r.""推荐认领人姓名"",
       r.""消歧置信度"", r.""消歧命中依据"", r.""网推状态"", r.""状态"", r.""最后同步时间""
  FROM ""论文采集结果"" r
 WHERE (@status IS NULL OR r.""网推状态"" = @status OR r.""状态"" = @status)
   AND (@claimantId IS NULL OR r.""推荐认领人编号"" = @claimantId)
   AND (@casPartition IS NULL OR r.""中科院分区"" = @casPartition)
   AND (@keyword IS NULL OR r.""论文标题"" ILIKE '%' || @keyword || '%' OR r.""作者"" ILIKE '%' || @keyword || '%' OR r.""刊物名称"" ILIKE '%' || @keyword || '%')
 ORDER BY r.""消歧置信度"" DESC, r.""编号"" DESC
 LIMIT 200;";

            var list = await Db.QueryAsync<论文网推项目项>(sql, new
            {
                status = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
                claimantId,
                casPartition = string.IsNullOrWhiteSpace(casPartition) ? null : casPartition.Trim(),
                keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim()
            });

            return Ok(list);
        }

        /// <summary>
        /// 触发全网论文主动网推巡检扫描与消歧打标
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> 触发论文全网主动网推([FromBody] 论文网推巡检请求 model)
        {
            var jsonResult = await Db.QuerySingleAsync<string>(
                "SELECT sp_触发论文全网主动网推(@外部系统编号)::text;",
                new { 外部系统编号 = model?.外部系统编号 }
            );

            return Content(jsonResult, "application/json");
        }

        /// <summary>
        /// 一键确认认领网推论文，自动归档至学术档案
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> 一键确认认领网推论文([FromBody] 一键确认认领网推请求 model)
        {
            if (model == null || model.采集结果编号 <= 0)
                return BadRequest(new { error = "采集结果编号不能为空" });

            var claimantId = model.认领人编号 ?? CurrentUser.编号;
            if (claimantId <= 0) return Unauthorized(new { error = "当前登录人员无效，不能确认论文认领" });
            if (claimantId != CurrentUser.编号 && !await HasAnyRoleAsync("系统管理员", "科研管理员"))
                return StatusCode(403, new { error = "只能为本人确认论文认领，跨人员认领需要科研管理员权限" });

            var jsonResult = await Db.QuerySingleAsync<string>(
                "SELECT sp_一键确认认领网推论文(@采集结果编号, @认领人编号, @作者位次, @认领说明)::text;",
                new
                {
                    采集结果编号 = model.采集结果编号,
                    认领人编号 = claimantId,
                    作者位次 = string.IsNullOrWhiteSpace(model.作者位次) ? "第一作者" : model.作者位次.Trim(),
                    认领说明 = model.认领说明
                }
            );

            return Content(jsonResult, "application/json");
        }

        /// <summary>
        /// 获取医院机构别名与学者消歧知识库配置
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> 获取机构与学者消歧配置()
        {
            var aliases = await Db.QueryAsync<机构别名配置项>(@"
SELECT a.""编号"", a.""规范单位编号"", u.""规范名称"" AS ""规范单位名称"", a.""别名"", a.""来源"", a.""是否启用""
  FROM ""科研单位别名"" a
  JOIN ""科研单位规范"" u ON u.""编号"" = a.""规范单位编号""
 ORDER BY a.""编号"" ASC;");

            var scholars = await Db.QueryAsync<学者消歧配置项>(@"
SELECT x.""编号"", x.""人员编号"", x.""工号"", x.""姓名"", x.""英文拼音署名"", x.""ORCID"", x.""科室名称"", x.""是否主要署名""
  FROM ""学者署名消歧库"" x
 ORDER BY x.""人员编号"" ASC, x.""编号"" ASC;");

            return Ok(new
            {
                机构别名 = aliases,
                学者消歧 = scholars
            });
        }

        /// <summary>
        /// 新增医院机构别名
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> 新增机构别名([FromBody] 添加机构别名请求 model)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员"))
                return StatusCode(403, new { error = "无权配置机构别名" });

            if (model == null || string.IsNullOrWhiteSpace(model.别名))
                return BadRequest(new { error = "机构别名不能为空" });

            await Db.ExecuteAsync(@"
INSERT INTO ""科研单位别名"" (""规范单位编号"", ""别名"", ""来源"", ""是否启用"", ""建立人编号"")
VALUES (1, @别名, @来源, true, @建立人编号)
ON CONFLICT (""别名"") DO UPDATE SET ""是否启用"" = true;", new
            {
                别名 = model.别名.Trim(),
                来源 = string.IsNullOrWhiteSpace(model.来源) ? "科管配置" : model.来源.Trim(),
                建立人编号 = CurrentUser.编号
            });

            return Ok(new { success = true, message = $"机构别名【{model.别名.Trim()}】已成功添加生效！" });
        }

        /// <summary>
        /// 新增学者消歧署名
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> 新增学者消歧署名([FromBody] 添加学者消歧请求 model)
        {
            if (model == null || model.人员编号 <= 0 || string.IsNullOrWhiteSpace(model.英文拼音署名))
                return BadRequest(new { error = "人员编号与英文拼音署名不能为空" });

            var person = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT p.""工号"", p.""姓名"", d.""名称"" AS ""科室名称""
  FROM ""人员"" p
  LEFT JOIN ""部门"" d ON d.""编号"" = p.""部门编号""
 WHERE p.""编号"" = @人员编号;", new { model.人员编号 });

            if (person == null)
                return NotFound(new { error = "指定人员不存在" });

            await Db.ExecuteAsync(@"
INSERT INTO ""学者署名消歧库"" (""人员编号"", ""工号"", ""姓名"", ""英文拼音署名"", ""ORCID"", ""科室名称"", ""是否主要署名"")
VALUES (@人员编号, @工号, @姓名, @英文拼音署名, @ORCID, @科室名称, true);", new
            {
                model.人员编号,
                工号 = (string)person.工号,
                姓名 = (string)person.姓名,
                英文拼音署名 = model.英文拼音署名.Trim(),
                ORCID = string.IsNullOrWhiteSpace(model.ORCID) ? null : model.ORCID.Trim(),
                科室名称 = string.IsNullOrWhiteSpace(model.科室名称) ? (string)person.科室名称 : model.科室名称.Trim()
            });

            return Ok(new { success = true, message = $"学者【{person.姓名}】的英文署名【{model.英文拼音署名.Trim()}】已录入消歧知识库！" });
        }

        #endregion
    }
}
