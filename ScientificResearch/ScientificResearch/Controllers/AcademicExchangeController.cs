using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using ScientificResearch.Infrastucture;

namespace ScientificResearch.Controllers
{
    /// <summary>
    /// 学术交流治理：主办会议申请、人员派出/接收、审核与导出
    /// </summary>
    public sealed class AcademicExchangeController : BaseController
    {
        private async Task<bool> IsManager() => await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办");
        private static string NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        #region 主办会议申请

        [HttpGet]
        public async Task<object> 查询主办会议(string 状态 = null, int? 年度 = null)
        {
            var list = await Db.QueryAsync<dynamic>(@"
SELECT m.*, u.""姓名"" as ""负责人姓名"", d.""名称"" as ""部门名称"", a.""姓名"" as ""审核人姓名""
FROM ""主办会议申请"" m
JOIN ""人员"" u ON m.""负责人编号"" = u.""编号""
LEFT JOIN ""部门"" d ON m.""归属部门编号"" = d.""编号""
LEFT JOIN ""人员"" a ON m.""审核人编号"" = a.""编号""
WHERE (@状态 IS NULL OR m.""状态""=@状态)
  AND (@年度 IS NULL OR EXTRACT(YEAR FROM m.""开始日期"")=@年度)
ORDER BY m.""开始日期"" DESC;", new { 状态, 年度 });

            return list;
        }

        [HttpPost]
        public async Task<object> 保存主办会议申请([FromBody] HostedConferenceSaveModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.会议名称) || string.IsNullOrWhiteSpace(model.主办单位))
                throw new ArgumentException("会议名称与主办单位不能为空");
            if (model.结束日期 < model.开始日期)
                throw new ArgumentException("会议结束日期不能早于开始日期");

            if (model.编号 > 0)
            {
                var exist = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT * FROM ""主办会议申请"" WHERE ""编号""=@编号;", new { model.编号 });
                if (exist == null) return NotFound(new { error = "会议申请不存在" });
                if (exist.负责人编号 != CurrentUser.编号 && !await IsManager())
                    return StatusCode(403, new { error = "无权修改他人的会议申请" });

                await Db.ExecuteAsync(@"
UPDATE ""主办会议申请""
SET ""会议名称""=@会议名称, ""会议级别""=@会议级别, ""主办单位""=@主办单位, ""承办单位""=@承办单位,
    ""协办单位""=@协办单位, ""开始日期""=@开始日期, ""结束日期""=@结束日期, ""举办地点""=@举办地点,
    ""预计参会人数""=@预计参会人数, ""境外参会人数""=@境外参会人数, ""经费预算""=@经费预算,
    ""经费来源""=@经费来源, ""是否涉外""=@是否涉外, ""议程附件路径""=@议程附件路径,
    ""更新时间""=CURRENT_TIMESTAMP
WHERE ""编号""=@编号;", new {
                    model.编号,
                    会议名称 = model.会议名称.Trim(),
                    会议级别 = string.IsNullOrWhiteSpace(model.会议级别) ? "院级" : model.会议级别.Trim(),
                    主办单位 = model.主办单位.Trim(),
                    承办单位 = NullIfEmpty(model.承办单位),
                    协办单位 = NullIfEmpty(model.协办单位),
                    model.开始日期,
                    model.结束日期,
                    举办地点 = NullIfEmpty(model.举办地点) ?? "本院学术报告厅",
                    预计参会人数 = model.预计参会人数 <= 0 ? 50 : model.预计参会人数,
                    境外参会人数 = model.境外参会人数 < 0 ? 0 : model.境外参会人数,
                    经费预算 = model.经费预算 < 0 ? 0 : model.经费预算,
                    经费来源 = NullIfEmpty(model.经费来源),
                    model.是否涉外,
                    议程附件路径 = NullIfEmpty(model.议程附件路径)
                });
                return new { model.编号 };
            }
            else
            {
                var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""主办会议申请"" (""会议名称"",""会议级别"",""主办单位"",""承办单位"",""协办单位"",""负责人编号"",""归属部门编号"",""开始日期"",""结束日期"",""举办地点"",""预计参会人数"",""境外参会人数"",""经费预算"",""经费来源"",""是否涉外"",""议程附件路径"",""状态"")
VALUES (@会议名称,@会议级别,@主办单位,@承办单位,@协办单位,@负责人编号,@归属部门编号,@开始日期,@结束日期,@举办地点,@预计参会人数,@境外参会人数,@经费预算,@经费来源,@是否涉外,@议程附件路径,'待审核')
RETURNING ""编号"";", new {
                    会议名称 = model.会议名称.Trim(),
                    会议级别 = string.IsNullOrWhiteSpace(model.会议级别) ? "院级" : model.会议级别.Trim(),
                    主办单位 = model.主办单位.Trim(),
                    承办单位 = NullIfEmpty(model.承办单位),
                    协办单位 = NullIfEmpty(model.协办单位),
                    负责人编号 = CurrentUser.编号,
                    归属部门编号 = CurrentUser.部门编号,
                    model.开始日期,
                    model.结束日期,
                    举办地点 = NullIfEmpty(model.举办地点) ?? "本院学术报告厅",
                    预计参会人数 = model.预计参会人数 <= 0 ? 50 : model.预计参会人数,
                    境外参会人数 = model.境外参会人数 < 0 ? 0 : model.境外参会人数,
                    经费预算 = model.经费预算 < 0 ? 0 : model.经费预算,
                    经费来源 = NullIfEmpty(model.经费来源),
                    model.是否涉外,
                    议程附件路径 = NullIfEmpty(model.议程附件路径)
                });
                return new { 编号 = id, 状态 = "待审核" };
            }
        }

        [HttpPost]
        public async Task<object> 审核主办会议([FromBody] HostedConferenceAuditModel model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权审核主办会议申请" });
            if (model == null || model.编号 <= 0 || model.审核结果 is not ("已批准" or "已驳回"))
                throw new ArgumentException("审核参数无效");

            var changed = await Db.ExecuteAsync(@"
UPDATE ""主办会议申请""
SET ""状态""=@审核结果, ""审核人编号""=@审核人编号, ""审核时间""=CURRENT_TIMESTAMP, ""审核意见""=@审核意见, ""更新时间""=CURRENT_TIMESTAMP
WHERE ""编号""=@编号 AND ""状态""='待审核';", new {
                model.编号,
                model.审核结果,
                审核人编号 = CurrentUser.编号,
                审核意见 = NullIfEmpty(model.审核意见)
            });

            if (changed == 0) return StatusCode(409, new { error = "会议状态不可审核或记录不存在" });
            return new { model.编号, 状态 = model.审核结果 };
        }

        #endregion

        #region 人员派遣与接收

        [HttpGet]
        public async Task<object> 查询学术交流人员(string 交流方向 = null, string 状态 = null)
        {
            return await Db.QueryAsync<dynamic>(@"
SELECT ex.*, p.""姓名"" as ""本院人员姓名"", a.""姓名"" as ""审核人姓名""
FROM ""学术交流人员"" ex
LEFT JOIN ""人员"" p ON ex.""人员编号"" = p.""编号""
LEFT JOIN ""人员"" a ON ex.""审核人编号"" = a.""编号""
WHERE (@交流方向 IS NULL OR ex.""交流方向""=@交流方向)
  AND (@状态 IS NULL OR ex.""状态""=@状态)
ORDER BY ex.""开始日期"" DESC;", new { 交流方向, 状态 });
        }

        [HttpPost]
        public async Task<object> 保存学术交流人员([FromBody] AcademicExchangePersonSaveModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.交流方向) || string.IsNullOrWhiteSpace(model.交流类型))
                throw new ArgumentException("交流方向和交流类型不能为空");

            if (model.交流方向 == "派出" && (model.人员编号 == null || model.人员编号 <= 0))
                model.人员编号 = CurrentUser.编号;

            if (model.交流方向 == "接收" && (string.IsNullOrWhiteSpace(model.外来人员姓名) || string.IsNullOrWhiteSpace(model.外来人员单位)))
                throw new ArgumentException("来访接待必须填写外来人员姓名与所在单位");

            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""学术交流人员"" (""交流方向"",""人员编号"",""外来人员姓名"",""外来人员单位"",""外来人员国籍"",""外来人员职称"",""交流类型"",""目的地或来访地"",""开始日期"",""结束日期"",""经费来源"",""关联主办会议编号"",""状态"")
VALUES (@交流方向,@人员编号,@外来人员姓名,@外来人员单位,@外来人员国籍,@外来人员职称,@交流类型,@目的地或来访地,@开始日期,@结束日期,@经费来源,@关联主办会议编号,'待审核')
RETURNING ""编号"";", new {
                model.交流方向,
                model.人员编号,
                外来人员姓名 = NullIfEmpty(model.外来人员姓名),
                外来人员单位 = NullIfEmpty(model.外来人员单位),
                外来人员国籍 = NullIfEmpty(model.外来人员国籍),
                外来人员职称 = NullIfEmpty(model.外来人员职称),
                model.交流类型,
                目的地或来访地 = NullIfEmpty(model.目的地或来访地) ?? "待定",
                model.开始日期,
                model.结束日期,
                经费来源 = NullIfEmpty(model.经费来源),
                model.关联主办会议编号
            });

            return new { 编号 = id, 状态 = "待审核" };
        }

        #endregion

        #region 统计与安全 CSV 导出

        [HttpGet]
        public async Task<object> 统计学术交流(int? 年度 = null)
        {
            var confStats = await Db.QueryAsync<dynamic>(@"
SELECT ""会议级别"", COUNT(1) as ""场次"", SUM(""预计参会人数"") as ""总参会人数"", SUM(""经费预算"") as ""总预算""
FROM ""主办会议申请""
WHERE (@年度 IS NULL OR EXTRACT(YEAR FROM ""开始日期"")=@年度)
GROUP BY ""会议级别"";", new { 年度 });

            var personStats = await Db.QueryAsync<dynamic>(@"
SELECT ""交流方向"", ""交流类型"", COUNT(1) as ""人次""
FROM ""学术交流人员""
WHERE (@年度 IS NULL OR EXTRACT(YEAR FROM ""开始日期"")=@年度)
GROUP BY ""交流方向"", ""交流类型"";", new { 年度 });

            return new { 会议统计 = confStats, 人员交流统计 = personStats };
        }

        [HttpGet]
        public async Task<IActionResult> 导出学术交流CSV(int? 年度 = null)
        {
            if (!await IsManager()) return StatusCode(403, "无权导出学术交流数据");

            var list = (await Db.QueryAsync<dynamic>(@"
SELECT m.""会议名称"", m.""会议级别"", m.""主办单位"", u.""姓名"" as ""负责人"", m.""开始日期"", m.""结束日期"", m.""举办地点"", m.""预计参会人数"", m.""经费预算"", m.""状态""
FROM ""主办会议申请"" m
JOIN ""人员"" u ON m.""负责人编号"" = u.""编号""
WHERE (@年度 IS NULL OR EXTRACT(YEAR FROM m.""开始日期"")=@年度)
ORDER BY m.""开始日期"" DESC;", new { 年度 })).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("会议名称,会议级别,主办单位,负责人,开始日期,结束日期,举办地点,预计参会人数,经费预算,状态");

            foreach (var r in list)
            {
                sb.AppendLine($"{EscapeCsvCell(r.会议名称)},{EscapeCsvCell(r.会议级别)},{EscapeCsvCell(r.主办单位)},{EscapeCsvCell(r.负责人)},{r.开始日期:yyyy-MM-dd},{r.结束日期:yyyy-MM-dd},{EscapeCsvCell(r.举办地点)},{r.预计参会人数},{r.经费预算},{EscapeCsvCell(r.状态)}");
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"学术会议统计_{年度?.ToString() ?? "全部"}.csv");
        }

        private static string EscapeCsvCell(object cell)
        {
            if (cell == null) return "";
            var str = cell.ToString();
            // 防止 CSV 注入攻击 (Excel 解释以 = + - @ 开头的公式)
            if (str.StartsWith("=") || str.StartsWith("+") || str.StartsWith("-") || str.StartsWith("@"))
            {
                str = "'" + str;
            }
            if (str.Contains(",") || str.Contains("\"") || str.Contains("\r") || str.Contains("\n"))
            {
                str = "\"" + str.Replace("\"", "\"\"") + "\"";
            }
            return str;
        }

        #endregion
    }

    public sealed class HostedConferenceSaveModel
    {
        public long 编号 { get; set; }
        public string 会议名称 { get; set; }
        public string 会议级别 { get; set; } = "院级";
        public string 主办单位 { get; set; }
        public string 承办单位 { get; set; }
        public string 协办单位 { get; set; }
        public DateTime 开始日期 { get; set; }
        public DateTime 结束日期 { get; set; }
        public string 举办地点 { get; set; }
        public int 预计参会人数 { get; set; } = 50;
        public int 境外参会人数 { get; set; } = 0;
        public decimal 经费预算 { get; set; } = 0;
        public string 经费来源 { get; set; }
        public bool 是否涉外 { get; set; }
        public string 议程附件路径 { get; set; }
    }

    public sealed class HostedConferenceAuditModel
    {
        public long 编号 { get; set; }
        public string 审核结果 { get; set; }
        public string 审核意见 { get; set; }
    }

    public sealed class AcademicExchangePersonSaveModel
    {
        public string 交流方向 { get; set; }
        public int? 人员编号 { get; set; }
        public string 外来人员姓名 { get; set; }
        public string 外来人员单位 { get; set; }
        public string 外来人员国籍 { get; set; }
        public string 外来人员职称 { get; set; }
        public string 交流类型 { get; set; }
        public string 目的地或来访地 { get; set; }
        public DateTime 开始日期 { get; set; }
        public DateTime 结束日期 { get; set; }
        public string 经费来源 { get; set; }
        public long? 关联主办会议编号 { get; set; }
    }
}
