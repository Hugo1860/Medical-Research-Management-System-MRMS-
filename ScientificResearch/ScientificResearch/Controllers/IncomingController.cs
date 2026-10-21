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
using Newtonsoft.Json;

namespace ScientificResearch.Controllers
{
    public class IncomingController : BaseController
    {
        public int 项目经费到账流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:项目经费到账");
            }
        }

        public int 纵向项目立项流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:纵向项目立项");
            }
        }

        public int 横向项目申请流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:横向项目申请");
            }
        }

        [HttpGet]
        async public Task<PagingResult<v3_项目经费到账_纵向>> 分页获取纵向项目经费合计到账列表(Paging paging, 纵向项目立项Filter filter)
        {
            return await Db.GetPagingListSpAsync<v3_项目经费到账_纵向, 纵向项目立项Filter>(paging, filter);
        }

        [HttpGet]
        async public Task<PagingResult<v3_项目经费到账_横向>> 分页获取横向项目经费合计到账列表(Paging paging, 横向项目Filter filter)
        {
            return await Db.GetPagingListSpAsync<v3_项目经费到账_横向, 横向项目Filter>(paging, filter);
        }

        [HttpGet]
        async public Task<object> 获取某纵向项目经费到账列表(int 纵向项目立项编号)
        {
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, 纵向项目立项编号))
            {
                return ProjectAccessDenied();
            }
            return await Db.GetListSpAsync<object, 项目经费到账Filter>(
                new 项目经费到账Filter()
                {
                    流程模板编号 = 纵向项目立项流程模板编号,
                    项目编号 = 纵向项目立项编号
                },
            $"tfn_项目经费到账({CurrentUser.编号},{项目经费到账流程模板编号})");
        }

        [HttpGet]
        async public Task<object> 获取某横向项目经费到账列表(int 横向项目编号)
        {
            if (!await CanAccessProjectAsync(横向项目申请流程模板编号, 横向项目编号))
            {
                return ProjectAccessDenied();
            }
            return await Db.GetListSpAsync<object, 项目经费到账Filter>(
                new 项目经费到账Filter()
                {
                    流程模板编号 = 横向项目申请流程模板编号,
                    项目编号 = 横向项目编号
                },
                $"tfn_项目经费到账({CurrentUser.编号},{项目经费到账流程模板编号})");
        }

        [HttpPost]
        async public Task 新增纵向项目经费到账([FromBody]纵向项目经费到账增改 model)
        {
            if (model.基本资料.到账金额 <= 0)
            {
                throw new Exception("请填写正确的到账金额");
            }

            var entity = new 项目经费到账();
            Tool.ModelToModel(entity, model.基本资料);
            entity.流程模板编号 = 纵向项目立项流程模板编号;

            await MyWorkFlowBusiness.发起流程(
                            项目经费到账流程模板编号,
                            new sp_项目经费到账_增改()
                            {
                                tt = entity.ToDataTable()
                            },
                            CurrentUser.编号,
                            model.IsHold);
        }


        [HttpPost]
        async public Task 新增横向项目经费到账([FromBody]横向项目经费到账增改 model)
        {
            if (model.基本资料.到账金额 <= 0)
            {
                throw new Exception("请填写正确的到账金额");
            }

            var entity = new 项目经费到账();
            Tool.ModelToModel(entity, model.基本资料);
            entity.流程模板编号 = 横向项目申请流程模板编号;

            await MyWorkFlowBusiness.发起流程(
                            项目经费到账流程模板编号,
                            new sp_项目经费到账_增改()
                            {
                                tt = entity.ToDataTable()
                            },
                            CurrentUser.编号,
                            model.IsHold);
        }

        [HttpPost]
        async public Task 完善纵向项目经费到账([FromBody]完成步骤<纵向项目经费到账增改> model)
        {
            if (model.附加数据.基本资料.到账金额 <= 0)
            {
                throw new Exception("请填写正确的到账金额");
            }

            var entity = new 项目经费到账();
            Tool.ModelToModel(entity, model.附加数据.基本资料);
            entity.流程模板编号 = 纵向项目立项流程模板编号;

            await MyWorkFlowBusiness.完成步骤(
                model.toSimple(), CurrentUser.编号,
                new sp_项目经费到账_增改()
                {
                    tt = entity.ToDataTable()
                },
                model.附加数据.IsHold);
        }

        [HttpPost]
        async public Task 完善横向项目经费到账([FromBody]完成步骤<横向项目经费到账增改> model)
        {
            if (model.附加数据.基本资料.到账金额 <= 0)
            {
                throw new Exception("请填写正确的到账金额");
            }

            var entity = new 项目经费到账();
            Tool.ModelToModel(entity, model.附加数据.基本资料);
            entity.流程模板编号 = 横向项目申请流程模板编号;

            await MyWorkFlowBusiness.完成步骤(
                model.toSimple(), CurrentUser.编号,
                new sp_项目经费到账_增改()
                {
                    tt = entity.ToDataTable()
                },
                model.附加数据.IsHold);
        }


        [HttpPost]
        async public Task<object> 上传到账凭证()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/项目经费到账/到账凭证",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        private async Task<bool> 是财务管理角色()
        {
            return await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办", "财务部会计", "财务负责人");
        }

        private async Task<bool> 是可访问经费范围(int 流程模板编号, int 项目编号)
        {
            var exists = await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (
    SELECT 1 FROM ""纵向项目立项"" WHERE @流程模板编号 = 410 AND ""编号"" = @项目编号
    UNION ALL SELECT 1 FROM ""横向项目"" WHERE @流程模板编号 = 450 AND ""编号"" = @项目编号
    UNION ALL SELECT 1 FROM ""参加会议"" WHERE @流程模板编号 = 210 AND ""编号"" = @项目编号
    UNION ALL SELECT 1 FROM ""项目经费预算"" WHERE ""流程模板编号"" = @流程模板编号 AND ""项目编号"" = @项目编号
    UNION ALL SELECT 1 FROM ""项目经费到账"" WHERE ""流程模板编号"" = @流程模板编号 AND ""项目编号"" = @项目编号
    UNION ALL SELECT 1 FROM ""报销"" WHERE ""流程模板编号"" = @流程模板编号 AND ""项目编号"" = @项目编号
);", new { 流程模板编号, 项目编号 });
            if (!exists) return false;
            return await 是财务管理角色() || await CanAccessProjectAsync(流程模板编号, 项目编号);
        }

        /// <summary>查询到账费用提取方案和规则。方案生效由财务管理员明确发布。</summary>
        [HttpGet]
        async public Task<object> 获取到账费用提取方案(bool 含停用 = false)
        {
            if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权查看到账费用提取方案" });
            var schemes = await Db.QueryAsync<object>(@"
SELECT * FROM ""到账费用提取方案""
 WHERE (@含停用 OR ""状态"" <> '停用') ORDER BY ""业务类型"",""版本号"" DESC;", new { 含停用 });
            var rules = await Db.QueryAsync<object>(@"
SELECT * FROM ""到账费用提取规则"" ORDER BY ""方案编号"",""序号"";", new { });
            return new { 方案列表 = schemes, 规则列表 = rules };
        }

        /// <summary>新增或维护费用提取方案；不会代替财务系统计算或生成财务凭证。</summary>
        [HttpPost]
        async public Task<object> 保存到账费用提取方案([FromBody]到账费用提取方案请求 model)
        {
            if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权维护到账费用提取方案" });
            if (model == null || string.IsNullOrWhiteSpace(model.方案名称)) throw new ArgumentException("方案名称不能为空");
            if (model.版本号 <= 0 || model.规则列表 == null) throw new ArgumentException("版本号和规则列表不能为空");
            var rules = model.规则列表.ToList();
            if (rules.Any(r => r.序号 <= 0 || string.IsNullOrWhiteSpace(r.费用类型) || r.计算值 < 0 || (r.计算方式 != "比例" && r.计算方式 != "固定金额")))
                throw new ArgumentException("费用提取规则无效");
            if (rules.Select(r => r.序号).Distinct().Count() != rules.Count) throw new ArgumentException("费用提取规则序号不能重复");
            if (model.状态 != "草稿" && model.状态 != "生效" && model.状态 != "停用") throw new ArgumentException("方案状态无效");
            using var conn = new Npgsql.NpgsqlConnection(DbConnectionString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();
            long id;
            if (model.编号.HasValue)
            {
                var changed = await conn.ExecuteAsync(new CommandDefinition(@"
UPDATE ""到账费用提取方案"" SET ""方案名称""=@方案名称,""业务类型""=@业务类型,""状态""=@状态,""生效日期""=@生效日期,""失效日期""=@失效日期,""说明""=@说明,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@编号;", new { model.方案名称, model.业务类型, model.状态, 生效日期 = model.生效日期?.Date, 失效日期 = model.失效日期?.Date, model.说明, 编号 = model.编号.Value }, tx));
                if (changed == 0) throw new KeyNotFoundException("费用提取方案不存在");
                id = model.编号.Value;
                await conn.ExecuteAsync(new CommandDefinition(@"DELETE FROM ""到账费用提取规则"" WHERE ""方案编号""=@方案编号;", new { 方案编号 = id }, tx));
            }
            else
            {
                id = await conn.QuerySingleAsync<long>(new CommandDefinition(@"
INSERT INTO ""到账费用提取方案"" (""方案名称"",""版本号"",""业务类型"",""状态"",""生效日期"",""失效日期"",""说明"",""创建人编号"")
VALUES (@方案名称,@版本号,@业务类型,@状态,@生效日期,@失效日期,@说明,@创建人编号) RETURNING ""编号"";", new { model.方案名称, model.版本号, model.业务类型, model.状态, 生效日期 = model.生效日期?.Date, 失效日期 = model.失效日期?.Date, model.说明, 创建人编号 = CurrentUser.编号 }, tx));
            }
            foreach (var rule in rules)
            {
                await conn.ExecuteAsync(new CommandDefinition(@"
INSERT INTO ""到账费用提取规则"" (""方案编号"",""序号"",""费用类型"",""计算方式"",""计算值"",""上限金额"",""是否启用"",""说明"")
VALUES (@方案编号,@序号,@费用类型,@计算方式,@计算值,@上限金额,@是否启用,@说明);", new { 方案编号 = id, rule.序号, 费用类型 = rule.费用类型.Trim(), rule.计算方式, rule.计算值, rule.上限金额, rule.是否启用, rule.说明 }, tx));
            }
            await tx.CommitAsync();
            return new { 编号 = id, 规则数量 = rules.Count, 状态 = model.状态 };
        }

        [HttpPost]
        async public Task<object> 预览到账费用提取([FromBody]到账费用提取预览请求 model)
        {
            if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权预览到账费用提取" });
            if (model == null || model.方案编号 <= 0 || model.到款金额 <= 0) throw new ArgumentException("方案编号和到款金额必须大于 0");
            return await Db.QueryAsync<object>("SELECT * FROM \"tfn_到账费用提取预览\"(@方案编号,@到款金额);", new { model.方案编号, model.到款金额 });
        }

        /// <summary>登记财务到款事实。登记结果默认待财务核验，系统不产生外部财务回执。</summary>
        [HttpPost]
        async public Task<object> 登记财务到款([FromBody]财务到款登记请求 model)
        {
            if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权登记财务到款" });
            if (model == null || string.IsNullOrWhiteSpace(model.外部单据号) || string.IsNullOrWhiteSpace(model.到款单位) || model.到款金额 <= 0)
                throw new ArgumentException("外部单据号、到款单位和到款金额不能为空");
            if (model.来源 != "财务适配器" && model.来源 != "人工登记") throw new ArgumentException("到款来源无效");
            if (model.来源 == "财务适配器" && !model.外部系统编号.HasValue) throw new ArgumentException("财务适配器来源必须提供外部系统编号");
            if (model.外部系统编号.HasValue && !await Db.QuerySingleAsync<bool>(
                    "SELECT EXISTS (SELECT 1 FROM \"外部系统\" WHERE \"编号\"=@编号 AND \"系统类型\"='财务');",
                    new { 编号 = model.外部系统编号.Value }))
                return NotFound(new { error = "外部系统不存在或不是财务类型" });
            var payload = new
            {
                model.外部系统编号, 外部单据号 = model.外部单据号.Trim(), model.同步批次号,
                到款日期 = model.到款日期?.Date, model.到款金额, 到款单位 = model.到款单位.Trim(), model.摘要, model.凭证路径, model.来源,
                建立人编号 = CurrentUser.编号
            };
            var text = await Db.QuerySingleAsync<string>("SELECT \"sp_财务到款登记\"(@payload::jsonb)::text;", new { payload = JsonConvert.SerializeObject(payload) });
            return JsonConvert.DeserializeObject(text);
        }

        [HttpPost]
        async public Task<object> 确认财务到款(long 到款登记编号)
        {
            if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权核验财务到款" });
            if (到款登记编号 <= 0) throw new ArgumentException("到款登记编号无效");
            var text = await Db.QuerySingleAsync<string>("SELECT \"sp_财务到款确认\"(@到款登记编号,@操作人编号)::text;", new { 到款登记编号, 操作人编号 = CurrentUser.编号 });
            return JsonConvert.DeserializeObject(text);
        }

        [HttpGet]
        async public Task<object> 获取待认领财务到款(string 状态 = null)
        {
            if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权查看财务到款池" });
            return await Db.QueryAsync<object>(@"
SELECT a.*, a.""到款金额"" - COALESCE(SUM(c.""认领金额"") FILTER (WHERE c.""状态""='已认领'),0) AS ""可认领余额""
FROM ""财务到款登记"" a LEFT JOIN ""财务到款认领"" c ON c.""到款登记编号""=a.""编号""
WHERE (@状态 IS NULL OR a.""状态""=@状态) GROUP BY a.""编号"" ORDER BY a.""到款日期"" DESC,a.""编号"" DESC;", new { 状态 = string.IsNullOrWhiteSpace(状态) ? null : 状态.Trim() });
        }

        [HttpPost]
        async public Task<object> 认领财务到款([FromBody]财务到款认领请求 model)
        {
            if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权认领财务到款" });
            if (model == null || model.到款登记编号 <= 0 || model.流程模板编号 <= 0 || model.项目编号 <= 0 || model.认领金额 <= 0) throw new ArgumentException("到款、项目、流程和认领金额不能为空");
            if (!await 是可访问经费范围(model.流程模板编号, model.项目编号)) return ProjectAccessDenied();
            var payload = new { model.到款登记编号, model.流程模板编号, model.项目编号, model.认领金额, model.提取方案编号, model.认领说明, 认领人编号 = CurrentUser.编号 };
            var text = await Db.QuerySingleAsync<string>("SELECT \"sp_财务到款认领\"(@payload::jsonb)::text;", new { payload = JsonConvert.SerializeObject(payload) });
            return JsonConvert.DeserializeObject(text);
        }

        [HttpPost]
        async public Task<object> 批量认领财务到款([FromBody]财务到款批量认领请求 model)
        {
            if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权批量认领财务到款" });
            var rows = (model?.分配列表 ?? Enumerable.Empty<财务到款认领请求>()).ToList();
            if (model == null || model.到款登记编号 <= 0 || rows.Count == 0 || rows.Count > 100) throw new ArgumentException("到款登记编号和 1-100 条分配记录不能为空");
            foreach (var row in rows)
            {
                if (row.流程模板编号 <= 0 || row.项目编号 <= 0 || row.认领金额 <= 0) throw new ArgumentException("批量分配记录无效");
                if (!await 是可访问经费范围(row.流程模板编号, row.项目编号)) return ProjectAccessDenied();
            }
            var payload = new { model.到款登记编号, 分配列表 = rows.Select(row => new { row.流程模板编号, row.项目编号, row.认领金额, row.提取方案编号, row.认领说明 }), 认领人编号 = CurrentUser.编号 };
            var text = await Db.QuerySingleAsync<string>("SELECT \"sp_财务到款批量认领\"(@payload::jsonb)::text;", new { payload = JsonConvert.SerializeObject(payload) });
            return JsonConvert.DeserializeObject(text);
        }

        [HttpGet]
        async public Task<object> 获取项目经费卡(int? 流程模板编号 = null, int? 项目编号 = null)
        {
            if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权查看项目经费卡" });
            return await Db.QueryAsync<object>(@"SELECT * FROM ""项目经费卡"" WHERE (@流程模板编号 IS NULL OR ""流程模板编号""=@流程模板编号) AND (@项目编号 IS NULL OR ""项目编号""=@项目编号) ORDER BY ""编号"" DESC;", new { 流程模板编号, 项目编号 });
        }

        [HttpPost]
        async public Task<object> 拆分项目经费卡([FromBody]项目经费卡拆分请求 model)
        {
            if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权拆分项目经费卡" });
            if (model == null || model.父卡编号 <= 0 || model.拆分金额 <= 0) throw new ArgumentException("父卡编号和拆分金额必须大于 0");
            var parent = await Db.QuerySingleOrDefaultAsync<dynamic>("SELECT \"流程模板编号\",\"项目编号\" FROM \"项目经费卡\" WHERE \"编号\"=@编号;", new { 编号 = model.父卡编号 });
            if (parent == null) return NotFound(new { error = "父经费卡不存在" });
            if (!await 是可访问经费范围((int)parent.流程模板编号, (int)parent.项目编号)) return ProjectAccessDenied();
            var payload = new { model.父卡编号, model.拆分金额, 卡片名称 = model.卡片名称, 操作人编号 = CurrentUser.编号 };
            var text = await Db.QuerySingleAsync<string>("SELECT \"sp_项目经费卡拆分\"(@payload::jsonb)::text;", new { payload = JsonConvert.SerializeObject(payload) });
            return JsonConvert.DeserializeObject(text);
        }

    }
}
