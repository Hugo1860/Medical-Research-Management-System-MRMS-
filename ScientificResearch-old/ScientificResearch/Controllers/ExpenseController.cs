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
    public class ExpenseController : BaseController
    {
        public int 报销流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:报销");
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

        public int 参加会议流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:参加会议");
            }
        }

        /// <summary>
        /// 针对每个纵向项目,目前总的报销情况,一个项目一行
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<PagingResult<v3_项目经费报销_纵向>> 分页获取纵向项目经费合计报销列表(Paging paging, 纵向项目立项Filter filter) =>
            await Db.GetPagingListSpAsync<v3_项目经费报销_纵向, 纵向项目立项Filter>(paging, filter);

        [HttpGet]
        async public Task<PagingResult<v3_项目经费报销_横向>> 分页获取横向项目经费合计报销列表(Paging paging, 横向项目Filter filter) =>
            await Db.GetPagingListSpAsync<v3_项目经费报销_横向, 横向项目Filter>(paging, filter);

        [HttpGet]
        async public Task<PagingResult<v3_参加会议报销>> 分页获取参加会议合计报销列表(Paging paging, 参加会议Filter filter) =>
            await Db.GetPagingListSpAsync<v3_参加会议报销, 参加会议Filter>(paging, filter);

        /// <summary>
        /// 汇总批准预算、到账、已审核报销和待审核报销，返回异常对账项。
        /// 仅科研、财务和系统管理角色可查看全局资金异常。
        /// </summary>
        [HttpGet]
        async public Task<object> 获取预算异常对账(int? 流程模板编号 = null, int? 项目编号 = null)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办", "财务部会计", "财务负责人"))
                return StatusCode(403, new { error = "无权查看全局预算对账" });
            return await Db.QueryAsync<object>(
                "SELECT * FROM \"tfn_预算异常对账\"(@流程模板编号,@项目编号);",
                new { 流程模板编号, 项目编号 });
        }

        /// <summary>获取异常对账的处理状态、责任人和处理说明。</summary>
        [HttpGet]
        async public Task<object> 获取预算异常处理(int? 流程模板编号 = null, int? 项目编号 = null)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办", "财务部会计", "财务负责人"))
                return StatusCode(403, new { error = "无权查看预算异常处理记录" });
            return await Db.QueryAsync<object>(
                "SELECT * FROM \"tfn_预算异常对账处理\"(@流程模板编号,@项目编号);",
                new { 流程模板编号, 项目编号 });
        }

        /// <summary>登记预算异常的跟进状态，保留最近责任人和处理说明。</summary>
        [HttpPost]
        async public Task<object> 处理预算异常对账([FromBody]预算异常对账处理请求 model)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办", "财务部会计", "财务负责人"))
                return StatusCode(403, new { error = "无权处理预算异常" });
            if (model == null || model.流程模板编号 <= 0 || model.项目编号 <= 0 || string.IsNullOrWhiteSpace(model.异常类型))
                throw new ArgumentException("流程模板编号、项目编号和异常类型不能为空");

            var payload = new
            {
                model.流程模板编号,
                model.项目编号,
                model.异常类型,
                状态 = string.IsNullOrWhiteSpace(model.状态) ? "处理中" : model.状态,
                model.责任人编号,
                model.处理说明,
                处理人编号 = CurrentUser.编号
            };
            var id = await Db.QuerySingleAsync<long>(
                "SELECT \"sp_预算异常对账_处理\"(@Payload::jsonb);",
                new { Payload = JsonConvert.SerializeObject(payload) });
            return new { 编号 = id, 状态 = payload.状态 };
        }

        /// <summary>按统一口径返回批准预算、到账、在途申请、已入账支出和实际付款分层。</summary>
        [HttpGet]
        async public Task<object> 获取项目经费分层(int? 流程模板编号 = null, int? 项目编号 = null)
        {
            if ((流程模板编号.HasValue && !项目编号.HasValue) || (!流程模板编号.HasValue && 项目编号.HasValue))
                throw new ArgumentException("流程模板编号和项目编号必须同时提供");
            if (!流程模板编号.HasValue)
            {
                if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权查看全局经费分层" });
            }
            else if (!await 是可访问经费范围(流程模板编号.Value, 项目编号.Value))
            {
                return ProjectAccessDenied();
            }
            return await Db.QueryAsync<object>(
                "SELECT * FROM \"tfn_项目经费分层\"(@流程模板编号,@项目编号);",
                new { 流程模板编号, 项目编号 });
        }

        /// <summary>接收财务系统经费流水，按外部单据号幂等写入项目经费台账。</summary>
        [HttpPost]
        async public Task<object> 同步财务流水([FromBody]财务流水同步请求 model)
        {
            if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权执行财务流水同步" });
            if (model == null || model.外部系统编号 <= 0 || string.IsNullOrWhiteSpace(model.同步批次号))
                throw new ArgumentException("外部系统编号和同步批次号不能为空");
            var rows = (model.流水列表 ?? Enumerable.Empty<财务流水同步记录>()).ToList();
            if (rows.Count > 2000) throw new ArgumentException("单批流水数不能超过 2000");
            if (!await Db.QuerySingleAsync<bool>("SELECT EXISTS (SELECT 1 FROM \"外部系统\" WHERE \"编号\"=@外部系统编号 AND \"系统类型\"='财务');", new { model.外部系统编号 }))
                return NotFound(new { error = "外部系统不存在或不是财务类型" });
            var 新增 = 0; var 更新 = 0; var 跳过 = 0; var 错误 = new List<string>();
            foreach (var item in rows)
            {
                try
                {
                    var type = item.业务类型?.Trim(); var status = string.IsNullOrWhiteSpace(item.状态) ? (type == "实际付款" ? "已付款" : "待审核") : item.状态.Trim();
                    if (string.IsNullOrWhiteSpace(item.外部单据号) || item.外部单据号.Length > 100 || item.流程模板编号 <= 0 || item.项目编号 <= 0 || item.金额 <= 0)
                    { 错误.Add($"外部单据 {item.外部单据号} 基础字段无效"); 跳过++; continue; }
                    if (type is not ("付款申请" or "实际付款" or "其他收入" or "冲销")) { 错误.Add($"外部单据 {item.外部单据号} 业务类型无效"); 跳过++; continue; }
                    if (status is not ("待审核" or "已审批" or "已付款" or "已驳回" or "已冲销")) { 错误.Add($"外部单据 {item.外部单据号} 状态无效"); 跳过++; continue; }
                    if (type == "实际付款" && (status != "已付款" || !item.付款日期.HasValue)) { 错误.Add($"外部单据 {item.外部单据号} 实际付款缺少付款日期"); 跳过++; continue; }
                    if (!await 是可访问经费范围(item.流程模板编号, item.项目编号)) { 错误.Add($"外部单据 {item.外部单据号} 项目不存在或无权访问"); 跳过++; continue; }
                    var existing = await Db.QuerySingleOrDefaultAsync<long?>(@"
SELECT ""编号"" FROM ""项目经费台账"" WHERE ""流程模板编号""=@流程模板编号 AND ""项目编号""=@项目编号 AND ""业务类型""=@业务类型 AND ""业务编号""=@业务编号;", new { item.流程模板编号, item.项目编号, 业务类型 = type, 业务编号 = item.外部单据号.Trim() });
                    var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""项目经费台账"" (""流程模板编号"",""项目编号"",""业务类型"",""金额"",""状态"",""业务编号"",""交易日期"",""付款日期"",""收款方"",""凭证路径"",""备注"",""建立人编号"")
VALUES (@流程模板编号,@项目编号,@业务类型,@金额,@状态,@业务编号,@交易日期,@付款日期,@收款方,@凭证路径,@备注,@建立人编号)
ON CONFLICT (""流程模板编号"",""项目编号"",""业务类型"",""业务编号"") WHERE ""业务编号"" IS NOT NULL DO UPDATE SET
  ""金额""=EXCLUDED.""金额"",""状态""=EXCLUDED.""状态"",""交易日期""=EXCLUDED.""交易日期"",""付款日期""=EXCLUDED.""付款日期"",""收款方""=EXCLUDED.""收款方"",""凭证路径""=EXCLUDED.""凭证路径"",""备注""=EXCLUDED.""备注"",""更新时间""=CURRENT_TIMESTAMP
RETURNING ""编号"";", new
                    {
                        item.流程模板编号, item.项目编号, 业务类型 = type, item.金额, 状态 = status, 业务编号 = item.外部单据号.Trim(),
                        交易日期 = item.交易日期?.Date, 付款日期 = item.付款日期?.Date,
                        收款方 = string.IsNullOrWhiteSpace(item.收款方) ? null : item.收款方.Trim(), 凭证路径 = string.IsNullOrWhiteSpace(item.凭证路径) ? null : item.凭证路径.Trim(),
                        备注 = string.IsNullOrWhiteSpace(item.备注) ? $"财务同步批次:{model.同步批次号.Trim()}" : item.备注.Trim(), 建立人编号 = CurrentUser.编号
                    });
                    if (existing.HasValue) 更新++; else 新增++;
                }
                catch (Exception ex) { 错误.Add($"外部单据 {item.外部单据号}: {ex.Message}"); }
            }
            return new { model.同步批次号, 总数 = rows.Count, 新增, 更新, 跳过, 错误数 = 错误.Count, 错误 };
        }

        /// <summary>查询项目合同付款节点及逾期状态。</summary>
        [HttpGet]
        async public Task<object> 获取项目合同付款节点(int 流程模板编号, int 项目编号)
        {
            if (!await 是可访问经费范围(流程模板编号, 项目编号)) return ProjectAccessDenied();
            return await Db.QueryAsync<object>(@"
SELECT ""编号"", ""流程模板编号"", ""项目编号"", ""节点序号"", ""节点名称"", ""约定金额"", ""到期日期"",
       ""状态"", ""已付款金额"", ""合同编号"", ""备注"", ""建立人编号"", ""建立时间"", ""更新时间"",
       CASE WHEN ""到期日期"" < CURRENT_DATE AND ""状态"" NOT IN ('已付款','取消') THEN TRUE ELSE FALSE END AS ""是否逾期""
  FROM ""项目合同付款节点""
 WHERE ""流程模板编号""=@流程模板编号 AND ""项目编号""=@项目编号
 ORDER BY ""节点序号"";", new { 流程模板编号, 项目编号 });
        }

        /// <summary>登记付款申请或实际付款，财务和科研管理角色可操作。</summary>
        [HttpPost]
        async public Task<object> 登记项目经费台账([FromBody]项目经费台账新增 model)
        {
            if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权登记项目经费台账" });
            if (model == null || model.流程模板编号 <= 0 || model.项目编号 <= 0 || model.金额 <= 0)
                throw new ArgumentException("流程模板编号、项目编号和金额必须有效");
            var 业务类型 = model.业务类型?.Trim();
            if (业务类型 is not ("付款申请" or "实际付款" or "其他收入" or "冲销"))
                throw new ArgumentException("业务类型必须是付款申请、实际付款、其他收入或冲销");
            var 状态 = string.IsNullOrWhiteSpace(model.状态) ? (业务类型 == "实际付款" ? "已付款" : "待审核") : model.状态.Trim();
            if (状态 is not ("待审核" or "已审批" or "已付款" or "已驳回" or "已冲销"))
                throw new ArgumentException("台账状态无效");
            if (业务类型 == "实际付款" && (状态 != "已付款" || !model.付款日期.HasValue))
                throw new ArgumentException("实际付款必须使用已付款状态并填写付款日期");
            if ((model.业务编号?.Length ?? 0) > 100 || (model.收款方?.Length ?? 0) > 200 || (model.凭证路径?.Length ?? 0) > 1000 || (model.备注?.Length ?? 0) > 1000)
                throw new ArgumentException("经费台账字段超出长度限制");
            if (!await 是可访问经费范围(model.流程模板编号, model.项目编号)) return ProjectAccessDenied();
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""项目经费台账"" (""流程模板编号"",""项目编号"",""业务类型"",""金额"",""状态"",""业务编号"",""交易日期"",""付款日期"",""收款方"",""凭证路径"",""备注"",""建立人编号"")
VALUES (@流程模板编号,@项目编号,@业务类型,@金额,@状态,@业务编号,@交易日期,@付款日期,@收款方,@凭证路径,@备注,@建立人编号)
RETURNING ""编号"";", new
            {
                model.流程模板编号, model.项目编号, 业务类型, model.金额, 状态,
                业务编号 = string.IsNullOrWhiteSpace(model.业务编号) ? null : model.业务编号.Trim(),
                交易日期 = model.交易日期?.Date, 付款日期 = model.付款日期?.Date,
                收款方 = string.IsNullOrWhiteSpace(model.收款方) ? null : model.收款方.Trim(),
                凭证路径 = string.IsNullOrWhiteSpace(model.凭证路径) ? null : model.凭证路径.Trim(),
                备注 = string.IsNullOrWhiteSpace(model.备注) ? null : model.备注.Trim(), 建立人编号 = CurrentUser.编号
            });
            return new { 编号 = id, 业务类型, 状态 };
        }

        /// <summary>登记或更新合同付款节点。</summary>
        [HttpPost]
        async public Task<object> 保存项目合同付款节点([FromBody]项目合同付款节点新增 model)
        {
            if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权维护合同付款节点" });
            if (model == null || model.流程模板编号 <= 0 || model.项目编号 <= 0 || model.节点序号 <= 0 || model.约定金额 <= 0)
                throw new ArgumentException("付款节点编号和金额必须有效");
            if (string.IsNullOrWhiteSpace(model.节点名称) || model.节点名称.Trim().Length > 200)
                throw new ArgumentException("节点名称不能为空且不能超过200个字符");
            var 状态 = string.IsNullOrWhiteSpace(model.状态) ? "计划" : model.状态.Trim();
            if (状态 is not ("计划" or "待申请" or "申请中" or "部分付款" or "已付款" or "逾期" or "取消"))
                throw new ArgumentException("付款节点状态无效");
            if (model.已付款金额 < 0 || model.已付款金额 > model.约定金额) throw new ArgumentException("已付款金额必须在0至约定金额之间");
            if ((model.合同编号?.Length ?? 0) > 100 || (model.备注?.Length ?? 0) > 1000) throw new ArgumentException("付款节点字段超出长度限制");
            if (!await 是可访问经费范围(model.流程模板编号, model.项目编号)) return ProjectAccessDenied();
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""项目合同付款节点"" (""流程模板编号"",""项目编号"",""节点序号"",""节点名称"",""约定金额"",""到期日期"",""状态"",""已付款金额"",""合同编号"",""备注"",""建立人编号"")
VALUES (@流程模板编号,@项目编号,@节点序号,@节点名称,@约定金额,@到期日期,@状态,@已付款金额,@合同编号,@备注,@建立人编号)
ON CONFLICT (""流程模板编号"",""项目编号"",""节点序号"") DO UPDATE SET
  ""节点名称""=EXCLUDED.""节点名称"", ""约定金额""=EXCLUDED.""约定金额"", ""到期日期""=EXCLUDED.""到期日期"", ""状态""=EXCLUDED.""状态"", ""已付款金额""=EXCLUDED.""已付款金额"", ""合同编号""=EXCLUDED.""合同编号"", ""备注""=EXCLUDED.""备注"", ""更新时间""=CURRENT_TIMESTAMP
RETURNING ""编号"";", new
            {
                model.流程模板编号, model.项目编号, model.节点序号, 节点名称 = model.节点名称.Trim(), model.约定金额,
                到期日期 = model.到期日期?.Date, 状态, model.已付款金额,
                合同编号 = string.IsNullOrWhiteSpace(model.合同编号) ? null : model.合同编号.Trim(),
                备注 = string.IsNullOrWhiteSpace(model.备注) ? null : model.备注.Trim(), 建立人编号 = CurrentUser.编号
            });
            return new { 编号 = id, 状态 };
        }

        private async Task<bool> 是财务管理角色() => await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办", "财务部会计", "财务负责人");

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

        /// <summary>
        /// 针对某个纵向项目的多个报销;一个报销一行
        /// </summary>
        /// <param name="纵向项目立项编号"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<object> 获取某纵向项目经费报销列表(int 纵向项目立项编号)
        {
            return await 获取经费报销列表(纵向项目立项编号, 纵向项目立项流程模板编号);
        }

        [HttpGet]
        async public Task<object> 获取某横向项目经费报销列表(int 横向项目编号)
        {
            return await 获取经费报销列表(横向项目编号, 横向项目申请流程模板编号);
        }

        [HttpGet]
        async public Task<object> 获取某参加会议报销列表(int 参加会议编号)
        {
            return await 获取经费报销列表(参加会议编号, 参加会议流程模板编号);
        }

        private async Task<object> 获取经费报销列表(int 项目编号, int 流程模板编号)
        {
            if (!await CanAccessProjectAsync(流程模板编号, 项目编号))
            {
                return ProjectAccessDenied();
            }
            return await Db.GetListSpAsync<object, 报销Filter>(
                            new 报销Filter()
                            {
                                流程模板编号 = 流程模板编号,
                                项目编号 = 项目编号
                            },
                        $"tfn_报销({CurrentUser.编号},{报销流程模板编号})");
        }

        /// <summary>
        /// 查看某个报销下具体每个项目的列表,一个报销下每个报销详情一行;
        /// </summary>
        /// <param name="报销编号"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<object> 获取某报销详情(int 报销编号)
        {
            var project = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""流程模板编号"", ""项目编号""
FROM ""报销""
WHERE ""编号"" = @报销编号;", new { 报销编号 });
            if (project == null)
            {
                return NotFound(new { error = "报销记录不存在" });
            }
            if (!await CanAccessProjectAsync((int)project.流程模板编号, (int)project.项目编号))
            {
                return ProjectAccessDenied();
            }
            return await Db.GetListSpAsync<报销详情, 报销详情Filter>(new 报销详情Filter() { 报销编号 = 报销编号 });
        }

        [HttpGet]
        async public Task<object> 获取某纵向项目预算已报销详情(int 纵向项目立项编号)
        {
            return await 获取项目预算已报销详情(纵向项目立项编号, 纵向项目立项流程模板编号);
        }

        [HttpGet]
        async public Task<object> 获取某横向项目预算已报销详情(int 横向项目编号)
        {
            return await 获取项目预算已报销详情(横向项目编号, 横向项目申请流程模板编号);
        }

        private async Task<object> 获取项目预算已报销详情(int 项目编号, int 流程模板编号)
        {
            if (!await CanAccessProjectAsync(流程模板编号, 项目编号))
            {
                return ProjectAccessDenied();
            }
            return await Db.GetListSpAsync<v1_预算已报销详情, 报销Filter>(
                            new 报销Filter()
                            {
                                流程模板编号 = 流程模板编号,
                                项目编号 = 项目编号
                            });
        }

        /// <summary>
        /// 基本资料中的报销金额不用写,后台根据报销详情自己算
        /// 基本资料中的流程模板编号不用写,后台自取;
        /// 注意model.基本资料中的项目编号要提供好;
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 新增纵向项目经费报销([FromBody]报销增改 model)
        {
            await 新增有预算的经费报销(model, 纵向项目立项流程模板编号);
        }

        [HttpPost]
        async public Task 新增横向项目经费报销([FromBody]报销增改 model)
        {
            await 新增有预算的经费报销(model, 横向项目申请流程模板编号);
        }

        private async Task 新增有预算的经费报销(报销增改 model, int 流程模板编号)
        {
            var valid = await 验证报销费用没有超过预算(model.基本资料.项目编号, 流程模板编号, model.报销详情列表);
            if (!valid)
            {
                throw new Exception("报销金额超过预算");
            }

            await 新增经费报销(model, 流程模板编号);
        }

        /// <summary>
        /// 没有预算,
        /// .基本资料中的项目编号要提供,就是相应的参加会议的编号;
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 新增参加会议报销([FromBody]报销增改 model)
        {
            await 新增有预算的经费报销(model, 参加会议流程模板编号);
        }

        private async Task 新增经费报销(报销增改 model, int 流程模板编号)
        {
            model.基本资料.报销金额 = model.报销详情列表.Sum(i => i.报销金额);

            if (model.基本资料.报销金额 <= 0)
            {
                throw new Exception("报销金额错误");
            }

            model.基本资料.流程模板编号 = 流程模板编号;

            await MyWorkFlowBusiness.发起流程(
                            报销流程模板编号,
                            new sp_报销_增改()
                            {
                                tt = model.基本资料.ToDataTable(),
                                tt_报销详情 = model.报销详情列表.ToDataTable()
                            },
                            CurrentUser.编号,
                            model.IsHold);
        }

        //同一项目中的没有终止的报销,其所有细则中,按支出分类+支出内容分类统计,不能超过该项目相同支出分类+支出内容的预算费用;
        async private Task<bool> 验证报销费用没有超过预算(int 项目编号, int 流程模板编号, IEnumerable<报销详情> 报销详情列表)
        {
            //2018-7-16 根据老板反馈,不需要验证这个,可以超出;
            return true;

            //var 项目预算已报销详情 = await 获取项目预算已报销详情(项目编号, 流程模板编号);

            //if (项目预算已报销详情.Any(i =>
            //{
            //    var model = 报销详情列表.Where(j => (j.项目支出类型 == i.项目支出类型 && j.项目支出内容 == i.项目支出内容)).FirstOrDefault();

            //    //如果报销金额超过了可报销金额,则有错;返回真:有错的数据
            //    return (i.经费 - i.已报销金额 < model.报销金额);
            //}))
            //{
            //    //有错误数据
            //    return false;
            //}
            //else
            //{
            //    return true;
            //}
        }

        /// <summary>
        /// 比如横纵项目经费报销
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 完善有预算的经费报销([FromBody]完成步骤<报销增改> model)
        {
            var valid = await 验证报销费用没有超过预算(model.附加数据.基本资料.项目编号, model.附加数据.基本资料.流程模板编号, model.附加数据.报销详情列表);
            if (!valid)
            {
                throw new Exception("报销金额超过预算");
            }
            await 完善经费报销(model);
        }

        /// <summary>
        /// 比如参加会议的报销
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 完善没有预算的经费报销([FromBody]完成步骤<报销增改> model)
        {
            await 完善经费报销(model);
        }

        private async Task 完善经费报销(完成步骤<报销增改> model)
        {
            model.附加数据.基本资料.报销金额 = model.附加数据.报销详情列表.Sum(i => i.报销金额);

            await MyWorkFlowBusiness.完成步骤(
                model.toSimple(), CurrentUser.编号,
                new sp_报销_增改()
                {
                    tt = model.附加数据.基本资料.ToDataTable(),
                    tt_报销详情 = model.附加数据.报销详情列表.ToDataTable()
                },
                model.附加数据.IsHold);
        }

        [HttpPost]
        async public Task<object> 上传相关文件()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/报销/相关文件",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpPost]
        async public Task<object> 按到账维护项目预算([FromBody]按到账维护项目预算请求 model)
        {
            if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权维护按到账项目预算" });
            if (model == null || model.项目编号 <= 0) throw new ArgumentException("项目编号不能为空");
            var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(model);
            var resultStr = await Db.QuerySingleAsync<string>(
                "SELECT \"sp_按到账维护项目预算\"(@payload::jsonb)::text;",
                new { payload = jsonPayload });
            return Newtonsoft.Json.JsonConvert.DeserializeObject(resultStr);
        }

        [HttpGet]
        async public Task<object> 获取按到账项目预算列表(int 项目编号, int? 到账编号 = null)
        {
            if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权查看按到账项目预算" });
            if (项目编号 <= 0) throw new ArgumentException("项目编号不能为空");
            var sql = @"
SELECT ""编号"", ""流程模板编号"", ""项目编号"", ""项目支出类型"", ""项目支出内容"", ""财务科目"",
       ""批准经费"", ""配套经费"", ""编制依据"", ""预算管理模式"", ""到账编号"", ""到账批次""
  FROM ""项目经费预算""
 WHERE ""项目编号"" = @项目编号
   AND (@到账编号 IS NULL OR ""到账编号"" = @到账编号)
 ORDER BY ""编号"";";
            var list = await Db.QueryAsync<object>(sql, new { 项目编号, 到账编号 });
            return list;
        }

        [HttpPost]
        async public Task<object> Hrp中间库双向协同([FromBody]Hrp中间库协同请求 model)
        {
            // The historical 069 routine uses `push_and_replay` to insert a
            // hard-coded demonstration expense. Keep that path unavailable
            // until a real HRP adapter is configured.
            if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权执行 HRP 中间库协同" });
            var requestedAction = string.IsNullOrWhiteSpace(model?.动作) ? "push_only" : model.动作.Trim();
            if (requestedAction is "push_and_replay" or "replay")
                return Conflict(new { error = "HRP 回放尚未接入真实财务适配器，已阻止写入示例付款记录", action = requestedAction, realAdapterRequired = true });
            if (requestedAction is not ("push_only" or "push"))
                throw new ArgumentException("HRP 协同动作仅支持 push_only；真实回放需先完成财务适配器联调");
            if (model?.项目编号 is <= 0)
                throw new ArgumentException("项目编号必须为正整数或省略");
            if (model?.项目编号 is int projectId && !await 是可访问经费范围(纵向项目立项流程模板编号, projectId))
                return ProjectAccessDenied();
            var resultStr = await Db.QuerySingleAsync<string>(
                "SELECT \"sp_hrp中间库双向协同\"(@项目编号, @动作)::text;",
                new { 项目编号 = model?.项目编号, 动作 = "push_only" });
            return Newtonsoft.Json.JsonConvert.DeserializeObject(resultStr);
        }

        [HttpGet]
        async public Task<object> 获取Hrp中间库同步状态(int? 项目编号 = null)
        {
            var isFinanceManager = await 是财务管理角色();
            if (!isFinanceManager && !项目编号.HasValue)
                return StatusCode(403, new { error = "无权查看全局 HRP 同步状态" });
            if (项目编号 is <= 0) throw new ArgumentException("项目编号必须为正整数");
            if (项目编号.HasValue && !isFinanceManager && !await 是可访问经费范围(纵向项目立项流程模板编号, 项目编号.Value))
                return ProjectAccessDenied();
            var projects = await Db.QueryAsync<object>(@"
SELECT ""id"", ""project_id"", ""project_no"", ""project_name"", ""pi_name"", ""approved_funds"", ""sync_status"", ""sync_time""
  FROM ""hrp_sync_project""
 WHERE (@项目编号 IS NULL OR ""project_id"" = @项目编号)
 ORDER BY ""id"" DESC LIMIT 20;", new { 项目编号 });

            var budgets = await Db.QueryAsync<object>(@"
SELECT ""id"", ""project_id"", ""expense_type"", ""expense_content"", ""finance_subject"", ""amount"", ""batch_no"", ""sync_status"", ""sync_time""
  FROM ""hrp_sync_budget""
 WHERE (@项目编号 IS NULL OR ""project_id"" = @项目编号)
 ORDER BY ""id"" DESC LIMIT 20;", new { 项目编号 });

            var expenses = await Db.QueryAsync<object>(@"
SELECT ""id"", ""project_id"", ""applicant_name"", ""expense_type"", ""amount"", ""hrp_voucher_no"", ""pay_status"", ""pay_date"", ""sync_status""
  FROM ""hrp_sync_expense""
 WHERE (@项目编号 IS NULL OR ""project_id"" = @项目编号)
 ORDER BY ""id"" DESC LIMIT 20;", new { 项目编号 });

            return new
            {
                项目推送列表 = projects,
                预算推送列表 = budgets,
                支出报销回写列表 = expenses
            };
        }

        /// <summary>按发票号码，或供应商+金额+开票日期返回疑似重复发票。OCR 未接入时只接受人工录入字段。</summary>
        [HttpGet]
        async public Task<object> 获取科研经费发票查重(int 流程模板编号, int 项目编号, string 发票号码, string 供应商名称, decimal 金额, DateTime? 开票日期 = null, long? 排除编号 = null)
        {
            if (!await 是可访问经费范围(流程模板编号, 项目编号)) return ProjectAccessDenied();
            if (string.IsNullOrWhiteSpace(发票号码) || string.IsNullOrWhiteSpace(供应商名称) || 金额 <= 0)
                throw new ArgumentException("发票号码、供应商名称和金额不能为空");
            return await Db.QueryAsync<object>(@"
SELECT * FROM ""tfn_科研经费发票查重""(@流程模板编号,@项目编号,@发票号码,@供应商名称,@金额,@开票日期,@排除编号);",
                new { 流程模板编号, 项目编号, 发票号码 = 发票号码.Trim(), 供应商名称 = 供应商名称.Trim(), 金额, 开票日期 = 开票日期?.Date, 排除编号 });
        }

        /// <summary>登记一张人工录入发票。发现重复时默认阻断，只有明确允许重复才写入疑似重复状态。</summary>
        [HttpPost]
        async public Task<object> 登记科研经费发票([FromBody]科研经费发票请求 model)
        {
            if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权登记科研经费发票" });
            if (model == null || model.流程模板编号 <= 0 || model.项目编号 <= 0 || string.IsNullOrWhiteSpace(model.发票号码) || string.IsNullOrWhiteSpace(model.供应商名称) || model.金额 <= 0)
                throw new ArgumentException("项目、发票号码、供应商名称和金额不能为空");
            if (model.发票号码.Trim().Length > 100 || model.发票代码?.Trim().Length > 50 || model.供应商名称.Trim().Length > 300 || model.供应商税号?.Trim().Length > 100 || model.附件路径?.Length > 4000)
                throw new ArgumentException("发票字段超出长度限制");
            if (!await 是可访问经费范围(model.流程模板编号, model.项目编号)) return ProjectAccessDenied();
            var duplicates = (await Db.QueryAsync<object>(@"
SELECT * FROM ""tfn_科研经费发票查重""(@流程模板编号,@项目编号,@发票号码,@供应商名称,@金额,@开票日期,NULL);",
                new { model.流程模板编号, model.项目编号, 发票号码 = model.发票号码.Trim(), 供应商名称 = model.供应商名称.Trim(), model.金额, 开票日期 = model.开票日期?.Date })).ToList();
            if (duplicates.Count > 0 && !model.允许重复)
                return Conflict(new { error = "发现疑似重复发票，请人工核对后再次提交", requiresReview = true, duplicates });
            var status = duplicates.Count > 0 ? "疑似重复" : "正常";
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研经费发票"" (""流程模板编号"",""项目编号"",""报销编号"",""发票号码"",""发票代码"",""开票日期"",""金额"",""供应商名称"",""供应商税号"",""发票类型"",""查重状态"",""重复说明"",""附件路径"",""OCR状态"",""建立人编号"")
VALUES (@流程模板编号,@项目编号,@报销编号,@发票号码,@发票代码,@开票日期,@金额,@供应商名称,@供应商税号,@发票类型,@查重状态,@重复说明,@附件路径,'人工录入',@建立人编号)
RETURNING ""编号"";", new
            {
                model.流程模板编号, model.项目编号, model.报销编号,
                发票号码 = model.发票号码.Trim(), 发票代码 = 空值(model.发票代码), 开票日期 = model.开票日期?.Date, model.金额,
                供应商名称 = model.供应商名称.Trim(), 供应商税号 = 空值(model.供应商税号),
                发票类型 = string.IsNullOrWhiteSpace(model.发票类型) ? "电子普通发票" : model.发票类型.Trim(), 查重状态 = status,
                重复说明 = duplicates.Count > 0 ? "与现有票据号码或供应商金额组合重复，已允许保存，需人工复核" : null,
                附件路径 = 空值(model.附件路径), 建立人编号 = CurrentUser.编号
            });
            return new { 编号 = id, 查重状态 = status, OCR状态 = "人工录入", 重复数量 = duplicates.Count };
        }

        /// <summary>财务人员对疑似重复发票作出复核结论。</summary>
        [HttpPost]
        async public Task<object> 复核科研经费发票([FromBody]科研经费发票复核请求 model)
        {
            if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权复核科研经费发票" });
            if (model == null || model.编号 <= 0) throw new ArgumentException("发票编号无效");
            var status = model.查重状态?.Trim();
            if (status is not ("正常" or "疑似重复" or "已确认重复" or "已作废")) throw new ArgumentException("查重状态无效");
            if ((model.重复说明?.Length ?? 0) > 1000) throw new ArgumentException("重复说明不能超过1000个字符");
            var changed = await Db.ExecuteAsync(@"
UPDATE ""科研经费发票"" SET ""查重状态""=@查重状态,""重复说明""=@重复说明,""复核人编号""=@复核人编号,""复核时间""=CURRENT_TIMESTAMP,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@编号;", new { model.编号, 查重状态 = status, 重复说明 = 空值(model.重复说明), 复核人编号 = CurrentUser.编号 });
            if (changed == 0) return NotFound(new { error = "发票记录不存在" });
            return new { 编号 = model.编号, 查重状态 = status };
        }

        /// <summary>查询预算调整申请和预算版本历史，项目负责人可查看本人项目，财务角色可查看全局。</summary>
        [HttpGet]
        async public Task<object> 获取项目经费预算调整(int? 流程模板编号 = null, int? 项目编号 = null)
        {
            if ((流程模板编号.HasValue && !项目编号.HasValue) || (!流程模板编号.HasValue && 项目编号.HasValue))
                throw new ArgumentException("流程模板编号和项目编号必须同时提供");
            if (!流程模板编号.HasValue)
            {
                if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权查看全局预算调整" });
            }
            else if (!await 是可访问经费范围(流程模板编号.Value, 项目编号.Value)) return ProjectAccessDenied();
            var applications = await Db.QueryAsync<object>(@"
SELECT a.*, p.""姓名"" AS ""申请人姓名"", ap.""姓名"" AS ""审批人姓名""
  FROM ""项目经费预算调整申请"" a
  LEFT JOIN ""人员"" p ON p.""编号""=a.""申请人编号""
  LEFT JOIN ""人员"" ap ON ap.""编号""=a.""审批人编号""
 WHERE (@流程模板编号 IS NULL OR a.""流程模板编号""=@流程模板编号)
   AND (@项目编号 IS NULL OR a.""项目编号""=@项目编号)
 ORDER BY a.""申请时间"" DESC, a.""编号"" DESC;", new { 流程模板编号, 项目编号 });
            var versions = await Db.QueryAsync<object>(@"
SELECT * FROM ""项目经费预算版本""
 WHERE (@流程模板编号 IS NULL OR ""流程模板编号""=@流程模板编号)
   AND (@项目编号 IS NULL OR ""项目编号""=@项目编号)
 ORDER BY ""流程模板编号"",""项目编号"",""版本号"" DESC;", new { 流程模板编号, 项目编号 });
            return new { 申请列表 = applications, 版本列表 = versions };
        }

        /// <summary>提交完整预算快照，审批后替换项目预算并产生不可变版本记录。</summary>
        [HttpPost]
        async public Task<object> 提交项目经费预算调整([FromBody]项目经费预算调整申请请求 model)
        {
            if (model == null || model.流程模板编号 <= 0 || model.项目编号 <= 0 || string.IsNullOrWhiteSpace(model.申请原因))
                throw new ArgumentException("项目和预算调整原因不能为空");
            if ((model.申请原因?.Length ?? 0) > 2000) throw new ArgumentException("申请原因不能超过2000个字符");
            if (!await 是可访问经费范围(model.流程模板编号, model.项目编号)) return ProjectAccessDenied();
            var lines = (model.申请预算 ?? Enumerable.Empty<按到账维护项目预算项>()).ToList();
            if (lines.Count == 0 || lines.Count > 200) throw new ArgumentException("预算调整明细必须为1至200行");
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line.项目支出类型) || string.IsNullOrWhiteSpace(line.项目支出内容) || string.IsNullOrWhiteSpace(line.财务科目) || line.批准经费 < 0)
                    throw new ArgumentException("预算明细的支出类型、内容、财务科目和金额必须有效");
            }
            var pending = await Db.QuerySingleOrDefaultAsync<long?>(@"
SELECT ""编号"" FROM ""项目经费预算调整申请"" WHERE ""流程模板编号""=@流程模板编号 AND ""项目编号""=@项目编号 AND ""状态""='待审批' LIMIT 1;", new { model.流程模板编号, model.项目编号 });
            if (pending.HasValue) return Conflict(new { error = "该项目已有待审批预算调整申请", 申请编号 = pending.Value });
            var existing = await Db.QuerySingleAsync<string>(@"
SELECT COALESCE(jsonb_agg(to_jsonb(b) ORDER BY b.""编号""),'[]'::jsonb)::text
  FROM ""项目经费预算"" b WHERE b.""流程模板编号""=@流程模板编号 AND b.""项目编号""=@项目编号;", new { model.流程模板编号, model.项目编号 });
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""项目经费预算调整申请"" (""流程模板编号"",""项目编号"",""申请原因"",""原预算快照"",""申请预算"",""状态"",""申请人编号"")
VALUES (@流程模板编号,@项目编号,@申请原因,@原预算快照::jsonb,@申请预算::jsonb,'待审批',@申请人编号) RETURNING ""编号"";", new
            {
                model.流程模板编号, model.项目编号, 申请原因 = model.申请原因.Trim(), 原预算快照 = existing,
                申请预算 = JsonConvert.SerializeObject(lines), 申请人编号 = CurrentUser.编号
            });
            return new { 申请编号 = id, 状态 = "待审批" };
        }

        /// <summary>批准或驳回预算调整；批准操作由数据库事务一次性更新预算和版本历史。</summary>
        [HttpPost]
        async public Task<object> 审批项目经费预算调整([FromBody]项目经费预算调整审批请求 model)
        {
            if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权审批预算调整" });
            if (model == null || model.申请编号 <= 0) throw new ArgumentException("预算调整申请编号无效");
            if ((model.审批意见?.Length ?? 0) > 2000) throw new ArgumentException("审批意见不能超过2000个字符");
            var result = await Db.QuerySingleAsync<string>(@"
SELECT ""sp_项目经费预算调整_审批""(@申请编号,@审批人编号,@是否批准,@审批意见)::text;", new { model.申请编号, 审批人编号 = CurrentUser.编号, model.是否批准, 审批意见 = 空值(model.审批意见) });
            return JsonConvert.DeserializeObject(result);
        }

        /// <summary>查询项目年度结算、封账和重开记录。</summary>
        [HttpGet]
        async public Task<object> 获取项目经费年度结算(int 流程模板编号, int 项目编号, int? 结算年度 = null)
        {
            if (!await 是可访问经费范围(流程模板编号, 项目编号)) return ProjectAccessDenied();
            return await Db.QueryAsync<object>(@"
SELECT * FROM ""项目经费年度结算"" WHERE ""流程模板编号""=@流程模板编号 AND ""项目编号""=@项目编号
  AND (@结算年度 IS NULL OR ""结算年度""=@结算年度) ORDER BY ""结算年度"" DESC;", new { 流程模板编号, 项目编号, 结算年度 });
        }

        /// <summary>封账指定年度的经费。封账后数据库触发器阻止发票和付款流水写入。</summary>
        [HttpPost]
        async public Task<object> 关闭项目经费年度([FromBody]项目经费年度结算请求 model)
        {
            if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权执行项目经费封账" });
            if (model == null || model.流程模板编号 <= 0 || model.项目编号 <= 0) throw new ArgumentException("项目参数无效");
            if (!await 是可访问经费范围(model.流程模板编号, model.项目编号)) return ProjectAccessDenied();
            if (model.结算年度 < 2000 || model.结算年度 > 2200) throw new ArgumentException("结算年度无效");
            var result = await Db.QuerySingleAsync<string>(@"
SELECT ""sp_项目经费年度结算""(@流程模板编号,@项目编号,@结算年度,'close',@操作人编号,@结算说明)::text;", new { model.流程模板编号, model.项目编号, model.结算年度, 操作人编号 = CurrentUser.编号, 结算说明 = 空值(model.结算说明) });
            return JsonConvert.DeserializeObject(result);
        }

        /// <summary>经财务负责人授权后重开年度经费，保留原封账人和重开审计字段。</summary>
        [HttpPost]
        async public Task<object> 重开项目经费年度([FromBody]项目经费年度结算请求 model)
        {
            if (!await HasAnyRoleAsync("系统管理员", "财务负责人")) return StatusCode(403, new { error = "仅系统管理员或财务负责人可重开经费年度" });
            if (model == null || model.流程模板编号 <= 0 || model.项目编号 <= 0) throw new ArgumentException("项目参数无效");
            if (!await 是可访问经费范围(model.流程模板编号, model.项目编号)) return ProjectAccessDenied();
            if (model.结算年度 < 2000 || model.结算年度 > 2200) throw new ArgumentException("结算年度无效");
            var result = await Db.QuerySingleAsync<string>(@"
SELECT ""sp_项目经费年度结算""(@流程模板编号,@项目编号,@结算年度,'reopen',@操作人编号,@结算说明)::text;", new { model.流程模板编号, model.项目编号, model.结算年度, 操作人编号 = CurrentUser.编号, 结算说明 = 空值(model.结算说明) });
            return JsonConvert.DeserializeObject(result);
        }

        /// <summary>回写单笔实际付款状态，已封账年度由数据库触发器拒绝修改。</summary>
        [HttpPost]
        async public Task<object> 回写项目付款状态([FromBody]项目付款回写请求 model)
        {
            if (!await 是财务管理角色()) return StatusCode(403, new { error = "无权回写付款状态" });
            if (model == null || model.台账编号 <= 0) throw new ArgumentException("台账编号无效");
            var status = model.状态?.Trim();
            if (status is not ("待审核" or "已审批" or "已付款" or "已驳回" or "已冲销")) throw new ArgumentException("付款状态无效");
            if (status == "已付款" && !model.付款日期.HasValue) throw new ArgumentException("已付款必须提供付款日期");
            var item = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"",""流程模板编号"",""项目编号"",""业务类型"" FROM ""项目经费台账"" WHERE ""编号""=@台账编号;", new { model.台账编号 });
            if (item == null) return NotFound(new { error = "经费台账记录不存在" });
            if (!await 是可访问经费范围((int)item.流程模板编号, (int)item.项目编号)) return ProjectAccessDenied();
            if ((string)item.业务类型 != "实际付款" && status == "已付款") throw new ArgumentException("只有实际付款类型的台账才能回写已付款");
            var changed = await Db.ExecuteAsync(@"
UPDATE ""项目经费台账"" SET ""状态""=@状态,""付款日期""=COALESCE(@付款日期,""付款日期""),""凭证路径""=COALESCE(@凭证路径,""凭证路径""),""备注""=COALESCE(@备注,""备注""),""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@台账编号;", new { model.台账编号, 状态 = status, 付款日期 = model.付款日期?.Date, 凭证路径 = 空值(model.凭证路径), 备注 = 空值(model.备注) });
            return new { 编号 = model.台账编号, 状态 = status, 更新数 = changed };
        }

        private static string 空值(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
