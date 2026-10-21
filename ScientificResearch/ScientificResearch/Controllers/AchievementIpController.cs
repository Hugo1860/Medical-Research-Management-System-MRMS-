using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using ScientificResearch.Infrastucture;

namespace ScientificResearch.Controllers
{
    /// <summary>
    /// 成果类型扩展、知识产权生命周期和获奖申报评审。
    /// 这些台账不覆盖历史成果表；所有状态变化均保留事件或认定记录。
    /// </summary>
    public sealed class AchievementIpController : BaseController
    {
        private async Task<bool> IsManager() => await HasAnyRoleAsync("系统管理员", "科研管理员", "科研秘书", "科技办");
        private static string TrimOrNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        private static readonly string[] ExtendedTypes = { "研究报告", "鉴定成果", "艺术作品", "标准", "药品证书", "新品种", "集成电路布图", "其他" };
        private static readonly string[] IpTypes = { "专利", "著作权", "标准", "药品证书", "新品种", "集成电路布图" };
        private static readonly string[] IpStates = { "筹备", "申请中", "受理", "审查中", "已授权", "已登记", "已失效", "已撤回" };

        [HttpGet]
        public async Task<object> 查询成果扩展记录(string 成果类别 = null, string 审核状态 = null, int? 负责人编号 = null)
        {
            if (!await IsManager()) 负责人编号 = CurrentUser.编号;
            return await Db.QueryAsync<object>(@"
SELECT e.""编号"",e.""成果类别"",e.""成果名称"",e.""年度"",e.""完成日期"",e.""负责人编号"",p.""姓名"" AS ""负责人姓名"",
       e.""关联成果类型"",e.""关联成果编号"",e.""关联项目编号"",e.""成果摘要"",e.""扩展字段"",e.""附件路径"",e.""审核状态"",e.""审核意见"",e.""审核人编号"",e.""审核时间"",e.""创建人编号"",e.""创建时间"",e.""更新时间""
  FROM ""科研成果扩展记录"" e LEFT JOIN ""人员"" p ON p.""编号""=e.""负责人编号""
 WHERE (@成果类别 IS NULL OR e.""成果类别""=@成果类别)
   AND (@审核状态 IS NULL OR e.""审核状态""=@审核状态)
   AND (@负责人编号 IS NULL OR e.""负责人编号""=@负责人编号 OR e.""创建人编号""=@负责人编号)
 ORDER BY e.""更新时间"" DESC,e.""编号"" DESC;", new { 成果类别 = TrimOrNull(成果类别), 审核状态 = TrimOrNull(审核状态), 负责人编号 });
        }

        [HttpPost]
        public async Task<object> 保存成果扩展记录([FromBody] 成果扩展记录保存 model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.成果类别) || !ExtendedTypes.Contains(model.成果类别.Trim())) throw new ArgumentException("成果类别无效");
            if (string.IsNullOrWhiteSpace(model.成果名称) || model.成果名称.Trim().Length > 500) throw new ArgumentException("成果名称不能为空且不能超过500字");
            if (model.年度.HasValue && (model.年度 < 1900 || model.年度 > 2200)) throw new ArgumentException("成果年度无效");
            if (model.关联成果类型.HasValue && (model.关联成果类型 < 1 || model.关联成果类型 > 4)) throw new ArgumentException("关联成果类型无效");
            var payload = JsonSerializer.Serialize(model.扩展字段 ?? new Dictionary<string, object>());
            if (model.编号.HasValue)
            {
                var allowed = await Db.QuerySingleAsync<bool>("SELECT @管理 OR EXISTS(SELECT 1 FROM \"科研成果扩展记录\" WHERE \"编号\"=@编号 AND (\"负责人编号\"=@人员编号 OR \"创建人编号\"=@人员编号));", new { 管理 = await IsManager(), model.编号, 人员编号 = CurrentUser.编号 });
                if (!allowed) return StatusCode(403, new { error = "无权修改该成果记录" });
                var changed = await Db.ExecuteAsync(@"
UPDATE ""科研成果扩展记录"" SET ""成果类别""=@成果类别,""成果名称""=@成果名称,""年度""=@年度,""完成日期""=@完成日期,""负责人编号""=@负责人编号,
 ""关联成果类型""=@关联成果类型,""关联成果编号""=@关联成果编号,""关联项目编号""=@关联项目编号,""成果摘要""=@成果摘要,""扩展字段""=CAST(@扩展字段 AS jsonb),""附件路径""=@附件路径,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@编号;", new { 编号 = model.编号.Value, 成果类别 = model.成果类别.Trim(), 成果名称 = model.成果名称.Trim(), model.年度, model.完成日期, model.负责人编号, model.关联成果类型, model.关联成果编号, model.关联项目编号, model.成果摘要, 扩展字段 = payload, model.附件路径 });
                if (changed == 0) return NotFound(new { error = "成果扩展记录不存在" });
                return new { 编号 = model.编号.Value, 审核状态 = "待审核" };
            }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研成果扩展记录"" (""成果类别"",""成果名称"",""年度"",""完成日期"",""负责人编号"",""关联成果类型"",""关联成果编号"",""关联项目编号"",""成果摘要"",""扩展字段"",""附件路径"",""创建人编号"")
VALUES (@成果类别,@成果名称,@年度,@完成日期,@负责人编号,@关联成果类型,@关联成果编号,@关联项目编号,@成果摘要,CAST(@扩展字段 AS jsonb),@附件路径,@创建人编号) RETURNING ""编号"";", new { 成果类别 = model.成果类别.Trim(), 成果名称 = model.成果名称.Trim(), model.年度, model.完成日期, 负责人编号 = model.负责人编号 ?? CurrentUser.编号, model.关联成果类型, model.关联成果编号, model.关联项目编号, model.成果摘要, 扩展字段 = payload, model.附件路径, 创建人编号 = CurrentUser.编号 });
            return new { 编号 = id, 审核状态 = "待审核" };
        }

        [HttpPost]
        public async Task<object> 审核成果扩展记录([FromBody] 成果扩展审核 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权审核成果扩展记录" });
            if (model == null || model.编号 <= 0 || model.审核状态 is not ("已通过" or "已退回")) throw new ArgumentException("审核状态无效");
            var changed = await Db.ExecuteAsync(@"UPDATE ""科研成果扩展记录"" SET ""审核状态""=@审核状态,""审核意见""=@审核意见,""审核人编号""=@审核人编号,""审核时间""=CURRENT_TIMESTAMP,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号;", new { model.编号, model.审核状态, model.审核意见, 审核人编号 = CurrentUser.编号 });
            return changed == 0 ? NotFound(new { error = "成果扩展记录不存在" }) : new { model.编号, model.审核状态 };
        }

        [HttpGet]
        public async Task<object> 查询知识产权(string 知识产权类型 = null, string 状态 = null, bool? 仅待缴费 = null)
        {
            var manager = await IsManager();
            var owner = manager ? (int?)null : CurrentUser.编号;
            return await Db.QueryAsync<object>(@"
SELECT i.""编号"",i.""知识产权类型"",i.""名称"",i.""状态"",i.""申请号"",i.""申请日期"",i.""公开编号"",i.""公开日期"",i.""证书号"",i.""授权日期"",i.""法律状态"",i.""权利人"",i.""负责人编号"",p.""姓名"" AS ""负责人姓名"",i.""代理机构"",i.""代理联系人"",i.""关联项目编号"",i.""总费用"",i.""已缴费用"",i.""下次缴费日期"",i.""摘要"",i.""扩展字段"",i.""附件路径"",i.""创建人编号"",i.""创建时间"",i.""更新时间"",
       COALESCE((SELECT count(*) FROM ""知识产权事件"" e WHERE e.""知识产权编号""=i.""编号""),0) AS ""事件数"",
       COALESCE((SELECT count(*) FROM ""知识产权缴费提醒"" r WHERE r.""知识产权编号""=i.""编号"" AND r.""状态"" IN ('待提醒','已提醒')),0) AS ""待缴费提醒数""
  FROM ""知识产权记录"" i LEFT JOIN ""人员"" p ON p.""编号""=i.""负责人编号""
 WHERE (@知识产权类型 IS NULL OR i.""知识产权类型""=@知识产权类型) AND (@状态 IS NULL OR i.""状态""=@状态)
   AND (@负责人编号 IS NULL OR i.""负责人编号""=@负责人编号 OR i.""创建人编号""=@负责人编号)
   AND (@仅待缴费 IS NULL OR (@仅待缴费=TRUE AND EXISTS(SELECT 1 FROM ""知识产权缴费提醒"" r WHERE r.""知识产权编号""=i.""编号"" AND r.""状态"" IN ('待提醒','已提醒') AND r.""应缴日期"" <= CURRENT_DATE + 60) OR @仅待缴费=FALSE)
 ORDER BY i.""下次缴费日期"" NULLS LAST,i.""更新时间"" DESC,i.""编号"" DESC;", new { 知识产权类型 = TrimOrNull(知识产权类型), 状态 = TrimOrNull(状态), 负责人编号 = owner, 仅待缴费 });
        }

        [HttpPost]
        public async Task<object> 保存知识产权([FromBody] 知识产权记录保存 model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.知识产权类型) || !IpTypes.Contains(model.知识产权类型.Trim())) throw new ArgumentException("知识产权类型无效");
            if (string.IsNullOrWhiteSpace(model.名称) || model.名称.Trim().Length > 500) throw new ArgumentException("知识产权名称不能为空且不能超过500字");
            var status = string.IsNullOrWhiteSpace(model.状态) ? "筹备" : model.状态.Trim();
            if (!IpStates.Contains(status)) throw new ArgumentException("知识产权状态无效");
            if (model.总费用 < 0 || model.已缴费用 < 0 || model.已缴费用 > model.总费用) throw new ArgumentException("知识产权费用无效");
            var payload = JsonSerializer.Serialize(model.扩展字段 ?? new Dictionary<string, object>());
            if (model.编号.HasValue)
            {
                var allowed = await Db.QuerySingleAsync<bool>("SELECT @管理 OR EXISTS(SELECT 1 FROM \"知识产权记录\" WHERE \"编号\"=@编号 AND (\"负责人编号\"=@人员编号 OR \"创建人编号\"=@人员编号));", new { 管理 = await IsManager(), model.编号, 人员编号 = CurrentUser.编号 });
                if (!allowed) return StatusCode(403, new { error = "无权修改该知识产权" });
                var changed = await Db.ExecuteAsync(@"
UPDATE ""知识产权记录"" SET ""知识产权类型""=@知识产权类型,""名称""=@名称,""状态""=@状态,""申请号""=@申请号,""申请日期""=@申请日期,""公开编号""=@公开编号,""公开日期""=@公开日期,""证书号""=@证书号,""授权日期""=@授权日期,""法律状态""=@法律状态,""权利人""=@权利人,""负责人编号""=@负责人编号,""代理机构""=@代理机构,""代理联系人""=@代理联系人,""关联项目编号""=@关联项目编号,""总费用""=@总费用,""已缴费用""=@已缴费用,""下次缴费日期""=@下次缴费日期,""摘要""=@摘要,""扩展字段""=CAST(@扩展字段 AS jsonb),""附件路径""=@附件路径,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号;", new { 编号 = model.编号.Value, 知识产权类型 = model.知识产权类型.Trim(), model.名称, 状态 = status, model.申请号, model.申请日期, model.公开编号, model.公开日期, model.证书号, model.授权日期, model.法律状态, model.权利人, 负责人编号 = model.负责人编号 ?? CurrentUser.编号, model.代理机构, model.代理联系人, model.关联项目编号, model.总费用, model.已缴费用, model.下次缴费日期, model.摘要, 扩展字段 = payload, model.附件路径 });
                if (changed == 0) return NotFound(new { error = "知识产权记录不存在" });
                return new { 编号 = model.编号.Value, 状态 = status };
            }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""知识产权记录"" (""知识产权类型"",""名称"",""状态"",""申请号"",""申请日期"",""公开编号"",""公开日期"",""证书号"",""授权日期"",""法律状态"",""权利人"",""负责人编号"",""代理机构"",""代理联系人"",""关联项目编号"",""总费用"",""已缴费用"",""下次缴费日期"",""摘要"",""扩展字段"",""附件路径"",""创建人编号"")
VALUES (@知识产权类型,@名称,@状态,@申请号,@申请日期,@公开编号,@公开日期,@证书号,@授权日期,@法律状态,@权利人,@负责人编号,@代理机构,@代理联系人,@关联项目编号,@总费用,@已缴费用,@下次缴费日期,@摘要,CAST(@扩展字段 AS jsonb),@附件路径,@创建人编号) RETURNING ""编号"";", new { 知识产权类型 = model.知识产权类型.Trim(), model.名称, 状态 = status, model.申请号, model.申请日期, model.公开编号, model.公开日期, model.证书号, model.授权日期, model.法律状态, model.权利人, 负责人编号 = model.负责人编号 ?? CurrentUser.编号, model.代理机构, model.代理联系人, model.关联项目编号, model.总费用, model.已缴费用, model.下次缴费日期, model.摘要, 扩展字段 = payload, model.附件路径, 创建人编号 = CurrentUser.编号 });
            return new { 编号 = id, 状态 = status };
        }

        [HttpGet]
        public async Task<object> 查询知识产权事件(long 知识产权编号)
        {
            if (知识产权编号 <= 0) throw new ArgumentException("知识产权编号无效");
            if (!await HasIpAccess(知识产权编号)) return ProjectAccessDenied();
            return await Db.QueryAsync<object>(@"SELECT e.""编号"",e.""知识产权编号"",e.""事件类型"",e.""事件日期"",e.""状态"",e.""事件说明"",e.""凭证编号"",e.""凭证路径"",e.""金额"",e.""下次缴费日期"",e.""操作人编号"",p.""姓名"" AS ""操作人姓名"",e.""操作时间"" FROM ""知识产权事件"" e LEFT JOIN ""人员"" p ON p.""编号""=e.""操作人编号"" WHERE e.""知识产权编号""=@知识产权编号 ORDER BY e.""事件日期"" DESC,e.""编号"" DESC;", new { 知识产权编号 });
        }

        [HttpPost]
        public async Task<object> 记录知识产权事件([FromBody] 知识产权事件保存 model)
        {
            if (model == null || model.知识产权编号 <= 0 || string.IsNullOrWhiteSpace(model.事件类型)) throw new ArgumentException("知识产权事件不能为空");
            if (!new[] { "申请", "受理", "审查", "授权", "登记", "变更", "缴费", "年费提醒", "失效", "撤回" }.Contains(model.事件类型.Trim())) throw new ArgumentException("知识产权事件类型无效");
            if (!await HasIpAccess(model.知识产权编号)) return ProjectAccessDenied();
            await using var connection = (Npgsql.NpgsqlConnection)Db;
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            var args = new { model.知识产权编号, 事件类型 = model.事件类型.Trim(), 事件日期 = model.事件日期 ?? DateTime.Today, 状态 = TrimOrNull(model.状态), 事件说明 = TrimOrNull(model.事件说明), model.凭证编号, model.凭证路径, model.金额, model.下次缴费日期, 操作人编号 = CurrentUser.编号 };
            var id = await connection.QuerySingleAsync<long>(@"INSERT INTO ""知识产权事件"" (""知识产权编号"",""事件类型"",""事件日期"",""状态"",""事件说明"",""凭证编号"",""凭证路径"",""金额"",""下次缴费日期"",""操作人编号"") VALUES (@知识产权编号,@事件类型,@事件日期,@状态,@事件说明,@凭证编号,@凭证路径,@金额,@下次缴费日期,@操作人编号) RETURNING ""编号"";", args, transaction);
            var state = string.IsNullOrWhiteSpace(model.状态) ? model.事件类型.Trim() switch { "申请" => "申请中", "受理" => "受理", "审查" => "审查中", "授权" => "已授权", "登记" => "已登记", "失效" => "已失效", "撤回" => "已撤回", _ => null } : model.状态.Trim();
            await connection.ExecuteAsync(@"UPDATE ""知识产权记录"" SET ""状态""=COALESCE(@状态,""状态""),""下次缴费日期""=COALESCE(@下次缴费日期,""下次缴费日期""),""已缴费用""=CASE WHEN @事件类型='缴费' THEN LEAST(""总费用"",""已缴费用""+@金额) ELSE ""已缴费用"" END,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@知识产权编号;", new { model.知识产权编号, 状态 = state, model.下次缴费日期, 事件类型 = model.事件类型.Trim(), model.金额 }, transaction);
            if (model.事件类型 is "缴费" or "年费提醒")
                await connection.ExecuteAsync(@"INSERT INTO ""知识产权缴费提醒"" (""知识产权编号"",""费用类型"",""应缴日期"",""应缴金额"",""状态"",""创建人编号"") VALUES (@知识产权编号,@费用类型,COALESCE(@应缴日期,CURRENT_DATE),@应缴金额,@状态,@创建人编号) ON CONFLICT (""知识产权编号"",""费用类型"",""应缴日期"") DO UPDATE SET ""应缴金额""=EXCLUDED.""应缴金额"";", new { model.知识产权编号, 费用类型 = model.事件类型 == "缴费" ? "缴费" : "年费", 应缴日期 = model.下次缴费日期, 应缴金额 = model.金额, 状态 = model.事件类型 == "缴费" ? "已缴费" : "待提醒", 创建人编号 = CurrentUser.编号 }, transaction);
            await transaction.CommitAsync();
            return new { 编号 = id, 状态 = state ?? model.事件类型.Trim() };
        }

        [HttpGet]
        public async Task<object> 查询知识产权缴费提醒(bool? 仅待处理 = true)
        {
            var manager = await IsManager();
            var owner = manager ? (int?)null : CurrentUser.编号;
            return await Db.QueryAsync<object>(@"SELECT r.""编号"",r.""知识产权编号"",i.""知识产权类型"",i.""名称"",r.""费用类型"",r.""应缴日期"",r.""应缴金额"",r.""状态"",r.""提醒提前天数"",r.""提醒时间"",r.""处理时间"",r.""处理人编号"",r.""处理说明"" FROM ""知识产权缴费提醒"" r JOIN ""知识产权记录"" i ON i.""编号""=r.""知识产权编号"" WHERE (@负责人编号 IS NULL OR i.""负责人编号""=@负责人编号 OR i.""创建人编号""=@负责人编号) AND (@仅待处理 IS NULL OR (@仅待处理=TRUE AND r.""状态"" IN ('待提醒','已提醒')) OR (@仅待处理=FALSE)) ORDER BY r.""应缴日期"" ASC,r.""编号"" DESC;", new { 负责人编号 = owner, 仅待处理 });
        }

        [HttpPost]
        public async Task<object> 更新知识产权缴费提醒([FromBody] 知识产权缴费提醒更新 model)
        {
            if (model == null || model.编号 <= 0 || !new[] { "已提醒", "已缴费", "已取消" }.Contains(model.状态?.Trim())) throw new ArgumentException("缴费提醒状态无效");
            var row = await Db.QuerySingleOrDefaultAsync<long?>("SELECT i.\"编号\" FROM \"知识产权缴费提醒\" r JOIN \"知识产权记录\" i ON i.\"编号\"=r.\"知识产权编号\" WHERE r.\"编号\"=@编号 AND (@管理 OR i.\"负责人编号\"=@人员编号 OR i.\"创建人编号\"=@人员编号);", new { model.编号, 管理 = await IsManager(), 人员编号 = CurrentUser.编号 });
            if (!row.HasValue) return NotFound(new { error = "缴费提醒不存在或无权修改" });
            await Db.ExecuteAsync("UPDATE \"知识产权缴费提醒\" SET \"状态\"=@状态,\"处理时间\"=CURRENT_TIMESTAMP,\"处理人编号\"=@处理人编号,\"处理说明\"=@处理说明,\"提醒时间\"=CASE WHEN @状态='已提醒' THEN CURRENT_TIMESTAMP ELSE \"提醒时间\" END WHERE \"编号\"=@编号;", new { model.编号, 状态 = model.状态.Trim(), 处理人编号 = CurrentUser.编号, model.处理说明 });
            return new { model.编号, 状态 = model.状态.Trim() };
        }

        [HttpGet]
        public async Task<object> 查询获奖申报批次(string 状态 = null)
        {
            var manager = await IsManager();
            return await Db.QueryAsync<object>(@"SELECT b.""编号"",b.""计划年度"",b.""批次名称"",b.""获奖类别"",b.""申报开始日期"",b.""申报截止日期"",b.""评审开始日期"",b.""评审截止日期"",b.""申报限额"",b.""申报资格规则"",b.""评审方案"",b.""状态"",b.""批次说明"",b.""创建人编号"",b.""创建时间"",b.""更新时间"",(SELECT count(*) FROM ""获奖申报"" d WHERE d.""批次编号""=b.""编号"") AS ""申报数"" FROM ""获奖申报批次"" b WHERE (@状态 IS NULL OR b.""状态""=@状态) AND (@管理 OR b.""状态""='发布') ORDER BY b.""计划年度"" DESC,b.""申报开始日期"" DESC,b.""编号"" DESC;", new { 状态 = TrimOrNull(状态), 管理 = manager });
        }

        [HttpPost]
        public async Task<object> 保存获奖申报批次([FromBody] 获奖申报批次保存 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权维护获奖申报批次" });
            if (model == null || string.IsNullOrWhiteSpace(model.批次名称) || string.IsNullOrWhiteSpace(model.获奖类别)) throw new ArgumentException("批次名称和获奖类别不能为空");
            if (model.申报截止日期 < model.申报开始日期) throw new ArgumentException("申报截止日期不能早于开始日期");
            var status = string.IsNullOrWhiteSpace(model.状态) ? "草稿" : model.状态.Trim();
            if (!new[] { "草稿", "发布", "关闭" }.Contains(status)) throw new ArgumentException("批次状态无效");
            var payload = JsonSerializer.Serialize(model.申报资格规则 ?? new Dictionary<string, object>()); var review = JsonSerializer.Serialize(model.评审方案 ?? new Dictionary<string, object>());
            if (model.编号.HasValue)
            {
                var changed = await Db.ExecuteAsync(@"UPDATE ""获奖申报批次"" SET ""计划年度""=@计划年度,""批次名称""=@批次名称,""获奖类别""=@获奖类别,""申报开始日期""=@申报开始日期,""申报截止日期""=@申报截止日期,""评审开始日期""=@评审开始日期,""评审截止日期""=@评审截止日期,""申报限额""=@申报限额,""申报资格规则""=CAST(@申报资格规则 AS jsonb),""评审方案""=CAST(@评审方案 AS jsonb),""状态""=@状态,""批次说明""=@批次说明,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号;", new { model.编号, model.计划年度, model.批次名称, model.获奖类别, model.申报开始日期, model.申报截止日期, model.评审开始日期, model.评审截止日期, model.申报限额, 申报资格规则 = payload, 评审方案 = review, 状态 = status, model.批次说明 });
                if (changed == 0) return NotFound(new { error = "获奖申报批次不存在" });
                return new { 编号 = model.编号.Value, 状态 = status };
            }
            var id = await Db.QuerySingleAsync<long>(@"INSERT INTO ""获奖申报批次"" (""计划年度"",""批次名称"",""获奖类别"",""申报开始日期"",""申报截止日期"",""评审开始日期"",""评审截止日期"",""申报限额"",""申报资格规则"",""评审方案"",""状态"",""批次说明"",""创建人编号"") VALUES (@计划年度,@批次名称,@获奖类别,@申报开始日期,@申报截止日期,@评审开始日期,@评审截止日期,@申报限额,CAST(@申报资格规则 AS jsonb),CAST(@评审方案 AS jsonb),@状态,@批次说明,@创建人编号) RETURNING ""编号"";", new { model.计划年度, model.批次名称, model.获奖类别, model.申报开始日期, model.申报截止日期, model.评审开始日期, model.评审截止日期, model.申报限额, 申报资格规则 = payload, 评审方案 = review, 状态 = status, model.批次说明, 创建人编号 = CurrentUser.编号 });
            return new { 编号 = id, 状态 = status };
        }

        [HttpGet]
        public async Task<object> 查询获奖申报(string 状态 = null, long? 批次编号 = null)
        {
            var manager = await IsManager();
            return await Db.QueryAsync<object>(@"SELECT d.""编号"",d.""批次编号"",b.""批次名称"",b.""获奖类别"" AS ""批次获奖类别"",d.""申报名称"",d.""申报人编号"",p.""姓名"" AS ""申报人姓名"",d.""获奖类别"",d.""拟申报级别"",d.""成果摘要"",d.""关联成果类型"",d.""关联成果编号"",d.""关联项目编号"",d.""申报材料路径"",d.""状态"",d.""提交时间"",d.""创建时间"",d.""更新时间"",COALESCE((SELECT avg(r.""评分"") FROM ""获奖申报评审"" r WHERE r.""申报编号""=d.""编号"" AND r.""是否回避""=FALSE),0) AS ""平均评分"" FROM ""获奖申报"" d JOIN ""获奖申报批次"" b ON b.""编号""=d.""批次编号"" LEFT JOIN ""人员"" p ON p.""编号""=d.""申报人编号"" WHERE (@状态 IS NULL OR d.""状态""=@状态) AND (@批次编号 IS NULL OR d.""批次编号""=@批次编号) AND (@管理 OR d.""申报人编号""=@人员编号) ORDER BY d.""更新时间"" DESC,d.""编号"" DESC;", new { 状态 = TrimOrNull(状态), 批次编号, 管理 = manager, 人员编号 = CurrentUser.编号 });
        }

        [HttpPost]
        public async Task<object> 保存获奖申报([FromBody] 获奖申报保存 model)
        {
            if (model == null || model.批次编号 <= 0 || string.IsNullOrWhiteSpace(model.申报名称)) throw new ArgumentException("申报批次和名称不能为空");
            var manager = await IsManager();
            if (model.编号.HasValue)
            {
                var allowed = await Db.QuerySingleAsync<bool>("SELECT @管理 OR EXISTS(SELECT 1 FROM \"获奖申报\" WHERE \"编号\"=@编号 AND \"申报人编号\"=@人员编号);", new { 管理 = manager, model.编号, 人员编号 = CurrentUser.编号 });
                if (!allowed) return StatusCode(403, new { error = "无权修改该获奖申报" });
                var changed = await Db.ExecuteAsync(@"UPDATE ""获奖申报"" SET ""批次编号""=@批次编号,""申报名称""=@申报名称,""获奖类别""=@获奖类别,""拟申报级别""=@拟申报级别,""成果摘要""=@成果摘要,""关联成果类型""=@关联成果类型,""关联成果编号""=@关联成果编号,""关联项目编号""=@关联项目编号,""申报材料路径""=@申报材料路径,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号;", new { model.编号, model.批次编号, model.申报名称, model.获奖类别, model.拟申报级别, model.成果摘要, model.关联成果类型, model.关联成果编号, model.关联项目编号, model.申报材料路径 });
                if (changed == 0) return NotFound(new { error = "获奖申报不存在" });
                return new { 编号 = model.编号.Value };
            }
            var valid = await Db.QuerySingleAsync<bool>("SELECT EXISTS(SELECT 1 FROM \"获奖申报批次\" WHERE \"编号\"=@批次编号 AND \"状态\"='发布' AND CURRENT_DATE BETWEEN \"申报开始日期\" AND \"申报截止日期\");", new { model.批次编号 });
            if (!valid && !manager) return BadRequest(new { error = "申报批次未发布或已截止" });
            var id = await Db.QuerySingleAsync<long>(@"INSERT INTO ""获奖申报"" (""批次编号"",""申报名称"",""申报人编号"",""获奖类别"",""拟申报级别"",""成果摘要"",""关联成果类型"",""关联成果编号"",""关联项目编号"",""申报材料路径"") VALUES (@批次编号,@申报名称,@申报人编号,@获奖类别,@拟申报级别,@成果摘要,@关联成果类型,@关联成果编号,@关联项目编号,@申报材料路径) RETURNING ""编号"";", new { model.批次编号, model.申报名称, 申报人编号 = CurrentUser.编号, model.获奖类别, model.拟申报级别, model.成果摘要, model.关联成果类型, model.关联成果编号, model.关联项目编号, model.申报材料路径 });
            return new { 编号 = id, 状态 = "草稿" };
        }

        [HttpPost]
        public async Task<object> 提交获奖申报(long 申报编号)
        {
            if (申报编号 <= 0) throw new ArgumentException("申报编号无效");
            var changed = await Db.ExecuteAsync(@"UPDATE ""获奖申报"" SET ""状态""='已提交',""提交时间""=CURRENT_TIMESTAMP,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@申报编号 AND (""申报人编号""=@人员编号 OR @管理);", new { 申报编号, 人员编号 = CurrentUser.编号, 管理 = await IsManager() });
            return changed == 0 ? NotFound(new { error = "申报不存在或无权提交" }) : new { 申报编号, 状态 = "已提交" };
        }

        [HttpGet]
        public async Task<object> 查询获奖评审(long 申报编号)
        {
            if (申报编号 <= 0) throw new ArgumentException("申报编号无效");
            var manager = await IsManager();
            return await Db.QueryAsync<object>(@"SELECT r.""编号"",r.""申报编号"",r.""评审专家编号"",p.""姓名"" AS ""评审专家姓名"",r.""是否回避"",r.""回避原因"",r.""评分"",r.""评审意见"",r.""推荐结果"",r.""提交时间"",r.""创建时间"",r.""更新时间"" FROM ""获奖申报评审"" r LEFT JOIN ""人员"" p ON p.""编号""=r.""评审专家编号"" WHERE r.""申报编号""=@申报编号 AND (@管理 OR r.""评审专家编号""=@人员编号) ORDER BY r.""更新时间"" DESC,r.""编号"" DESC;", new { 申报编号, 管理 = manager, 人员编号 = CurrentUser.编号 });
        }

        [HttpPost]
        public async Task<object> 指派获奖评审([FromBody] 获奖评审指派 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权指派获奖评审" });
            if (model == null || model.申报编号 <= 0 || model.评审专家编号 <= 0) throw new ArgumentException("申报和评审专家不能为空");
            var id = await Db.QuerySingleAsync<long>(@"INSERT INTO ""获奖申报评审"" (""申报编号"",""评审专家编号"") VALUES (@申报编号,@评审专家编号) ON CONFLICT (""申报编号"",""评审专家编号"") DO UPDATE SET ""更新时间""=CURRENT_TIMESTAMP RETURNING ""编号"";", model);
            await Db.ExecuteAsync("UPDATE \"获奖申报\" SET \"状态\"='评审中',\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@申报编号 AND \"状态\" IN ('已提交','草稿');", model);
            return new { 编号 = id, 状态 = "已指派" };
        }

        [HttpPost]
        public async Task<object> 保存获奖评审([FromBody] 获奖评审保存 model)
        {
            if (model == null || model.申报编号 <= 0) throw new ArgumentException("申报编号不能为空");
            var manager = await IsManager();
            var allowed = await Db.QuerySingleAsync<bool>("SELECT @管理 OR EXISTS(SELECT 1 FROM \"获奖申报评审\" WHERE \"申报编号\"=@申报编号 AND \"评审专家编号\"=@人员编号);", new { 管理 = manager, model.申报编号, 人员编号 = CurrentUser.编号 });
            if (!allowed) return StatusCode(403, new { error = "无权填写该获奖评审" });
            if (model.评分.HasValue && (model.评分 < 0 || model.评分 > 100)) throw new ArgumentException("评分范围为0-100");
            if (model.是否回避 && string.IsNullOrWhiteSpace(model.回避原因)) throw new ArgumentException("回避时必须填写原因");
            await Db.ExecuteAsync(@"UPDATE ""获奖申报评审"" SET ""是否回避""=@是否回避,""回避原因""=@回避原因,""评分""=@评分,""评审意见""=@评审意见,""推荐结果""=@推荐结果,""提交时间""=CURRENT_TIMESTAMP,""更新时间""=CURRENT_TIMESTAMP WHERE ""申报编号""=@申报编号 AND ""评审专家编号""=@评审专家编号;", new { model.申报编号, 评审专家编号 = model.评审专家编号 > 0 ? model.评审专家编号 : CurrentUser.编号, model.是否回避, model.回避原因, model.评分, model.评审意见, model.推荐结果 });
            return new { model.申报编号, 状态 = "已保存" };
        }

        [HttpPost]
        public async Task<object> 认定获奖申报([FromBody] 获奖认定保存 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权认定获奖申报" });
            if (model == null || model.申报编号 <= 0 || !new[] { "待认定", "已认定", "不通过" }.Contains(model.认定结果?.Trim())) throw new ArgumentException("认定结果无效");
            var id = await Db.QuerySingleAsync<long>(@"INSERT INTO ""获奖认定"" (""申报编号"",""认定结果"",""成果获奖编号"",""获奖名称"",""获奖级别"",""颁奖单位"",""证书编号"",""获奖日期"",""认定意见"",""认定人编号"",""认定时间"",""证书路径"") VALUES (@申报编号,@认定结果,@成果获奖编号,@获奖名称,@获奖级别,@颁奖单位,@证书编号,@获奖日期,@认定意见,@认定人编号,CURRENT_TIMESTAMP,@证书路径) ON CONFLICT (""申报编号"") DO UPDATE SET ""认定结果""=EXCLUDED.""认定结果"",""成果获奖编号""=EXCLUDED.""成果获奖编号"",""获奖名称""=EXCLUDED.""获奖名称"",""获奖级别""=EXCLUDED.""获奖级别"",""颁奖单位""=EXCLUDED.""颁奖单位"",""证书编号""=EXCLUDED.""证书编号"",""获奖日期""=EXCLUDED.""获奖日期"",""认定意见""=EXCLUDED.""认定意见"",""认定人编号""=EXCLUDED.""认定人编号"",""认定时间""=CURRENT_TIMESTAMP,""证书路径""=EXCLUDED.""证书路径"" RETURNING ""编号"";", new { model.申报编号, 认定结果 = model.认定结果.Trim(), model.成果获奖编号, model.获奖名称, model.获奖级别, model.颁奖单位, model.证书编号, model.获奖日期, model.认定意见, 认定人编号 = CurrentUser.编号, model.证书路径 });
            await Db.ExecuteAsync("UPDATE \"获奖申报\" SET \"状态\"=CASE WHEN @结果='已认定' THEN '已认定' WHEN @结果='不通过' THEN '未通过' ELSE \"状态\" END,\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@申报编号;", new { model.申报编号, 结果 = model.认定结果.Trim() });
            return new { 编号 = id, model.申报编号, 认定结果 = model.认定结果.Trim() };
        }

        [HttpGet]
        public async Task<object> 查询获奖认定(long? 申报编号 = null)
        {
            var manager = await IsManager();
            return await Db.QueryAsync<object>(@"SELECT a.""编号"",a.""申报编号"",d.""申报名称"",d.""申报人编号"",p.""姓名"" AS ""申报人姓名"",a.""认定结果"",a.""成果获奖编号"",a.""获奖名称"",a.""获奖级别"",a.""颁奖单位"",a.""证书编号"",a.""获奖日期"",a.""认定意见"",a.""认定人编号"",a.""认定时间"",a.""证书路径"" FROM ""获奖认定"" a JOIN ""获奖申报"" d ON d.""编号""=a.""申报编号"" LEFT JOIN ""人员"" p ON p.""编号""=d.""申报人编号"" WHERE (@申报编号 IS NULL OR a.""申报编号""=@申报编号) AND (@管理 OR d.""申报人编号""=@人员编号) ORDER BY a.""认定时间"" DESC,a.""编号"" DESC;", new { 申报编号, 管理 = manager, 人员编号 = CurrentUser.编号 });
        }

        private async Task<bool> HasIpAccess(long id) => await Db.QuerySingleAsync<bool>("SELECT @管理 OR EXISTS(SELECT 1 FROM \"知识产权记录\" WHERE \"编号\"=@编号 AND (\"负责人编号\"=@人员编号 OR \"创建人编号\"=@人员编号));", new { 编号 = id, 管理 = await IsManager(), 人员编号 = CurrentUser.编号 });
    }

    public sealed class 成果扩展记录保存 { public long? 编号 { get; set; } public string 成果类别 { get; set; } public string 成果名称 { get; set; } public int? 年度 { get; set; } public DateTime? 完成日期 { get; set; } public int? 负责人编号 { get; set; } public int? 关联成果类型 { get; set; } public int? 关联成果编号 { get; set; } public int? 关联项目编号 { get; set; } public string 成果摘要 { get; set; } public Dictionary<string, object> 扩展字段 { get; set; } public string 附件路径 { get; set; } }
    public sealed class 成果扩展审核 { public long 编号 { get; set; } public string 审核状态 { get; set; } public string 审核意见 { get; set; } }
    public sealed class 知识产权记录保存 { public long? 编号 { get; set; } public string 知识产权类型 { get; set; } public string 名称 { get; set; } public string 状态 { get; set; } public string 申请号 { get; set; } public DateTime? 申请日期 { get; set; } public string 公开编号 { get; set; } public DateTime? 公开日期 { get; set; } public string 证书号 { get; set; } public DateTime? 授权日期 { get; set; } public string 法律状态 { get; set; } public string 权利人 { get; set; } public int? 负责人编号 { get; set; } public string 代理机构 { get; set; } public string 代理联系人 { get; set; } public int? 关联项目编号 { get; set; } public decimal 总费用 { get; set; } public decimal 已缴费用 { get; set; } public DateTime? 下次缴费日期 { get; set; } public string 摘要 { get; set; } public Dictionary<string, object> 扩展字段 { get; set; } public string 附件路径 { get; set; } }
    public sealed class 知识产权事件保存 { public long 知识产权编号 { get; set; } public string 事件类型 { get; set; } public DateTime? 事件日期 { get; set; } public string 状态 { get; set; } public string 事件说明 { get; set; } public string 凭证编号 { get; set; } public string 凭证路径 { get; set; } public decimal 金额 { get; set; } public DateTime? 下次缴费日期 { get; set; } }
    public sealed class 知识产权缴费提醒更新 { public long 编号 { get; set; } public string 状态 { get; set; } public string 处理说明 { get; set; } }
    public sealed class 获奖申报批次保存 { public long? 编号 { get; set; } public int 计划年度 { get; set; } public string 批次名称 { get; set; } public string 获奖类别 { get; set; } public DateTime 申报开始日期 { get; set; } public DateTime 申报截止日期 { get; set; } public DateTime? 评审开始日期 { get; set; } public DateTime? 评审截止日期 { get; set; } public int? 申报限额 { get; set; } public Dictionary<string, object> 申报资格规则 { get; set; } public Dictionary<string, object> 评审方案 { get; set; } public string 状态 { get; set; } public string 批次说明 { get; set; } }
    public sealed class 获奖申报保存 { public long? 编号 { get; set; } public long 批次编号 { get; set; } public string 申报名称 { get; set; } public string 获奖类别 { get; set; } public string 拟申报级别 { get; set; } public string 成果摘要 { get; set; } public int? 关联成果类型 { get; set; } public int? 关联成果编号 { get; set; } public int? 关联项目编号 { get; set; } public string 申报材料路径 { get; set; } }
    public sealed class 获奖评审指派 { public long 申报编号 { get; set; } public int 评审专家编号 { get; set; } }
    public sealed class 获奖评审保存 { public long 申报编号 { get; set; } public int 评审专家编号 { get; set; } public bool 是否回避 { get; set; } public string 回避原因 { get; set; } public decimal? 评分 { get; set; } public string 评审意见 { get; set; } public string 推荐结果 { get; set; } }
    public sealed class 获奖认定保存 { public long 申报编号 { get; set; } public string 认定结果 { get; set; } public int? 成果获奖编号 { get; set; } public string 获奖名称 { get; set; } public string 获奖级别 { get; set; } public string 颁奖单位 { get; set; } public string 证书编号 { get; set; } public DateTime? 获奖日期 { get; set; } public string 认定意见 { get; set; } public string 证书路径 { get; set; } }
}
