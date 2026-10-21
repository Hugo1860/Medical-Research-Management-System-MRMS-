using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ScientificResearch.Infrastucture;
using ScientificResearch.Models;

namespace ScientificResearch.Controllers
{
    /// <summary>流程版本、失败告警和数据导出任务的运行治理接口。</summary>
    public class OperationsController : BaseController
    {
        [HttpGet]
        async public Task<object> 获取接口失败告警()
        {
            if (!await 是运行管理角色()) return StatusCode(403, new { error = "无权查看接口失败告警" });
            return await Db.QueryAsync<object>(@"
SELECT ""请求路径"", ""请求方法"", ""响应状态"", ""失败次数"", ""最近失败时间"", ""最早失败时间"", ""涉及人员数""
  FROM ""v_接口失败告警"" ORDER BY ""失败次数"" DESC, ""最近失败时间"" DESC;");
        }

        /// <summary>查询外部同步队列的终态失败任务，供运维告警看板使用。</summary>
        [HttpGet]
        async public Task<object> 获取外部同步失败告警(int? 数量 = null)
        {
            if (!await 是运行管理角色()) return StatusCode(403, new { error = "无权查看外部同步失败告警" });
            var limit = Math.Clamp(数量 ?? 100, 1, 500);
            return await Db.QueryAsync<object>(@"
SELECT q.""编号"", q.""外部系统编号"", s.""系统代码"", s.""系统名称"", s.""系统类型"",
       q.""业务类型"", q.""方向"", q.""业务主键"", q.""幂等键"", q.""状态"",
       q.""尝试次数"", q.""最大重试次数"", q.""最后错误"", q.""请求ID"",
       q.""创建时间"", q.""更新时间"", q.""完成时间""
  FROM ""外部同步任务"" q
  JOIN ""外部系统"" s ON s.""编号""=q.""外部系统编号""
 WHERE q.""状态""='失败'
 ORDER BY q.""更新时间"" DESC, q.""编号"" DESC
 LIMIT @数量;", new { 数量 = limit });
        }

        [HttpGet]
        async public Task<object> 获取流程版本(int? 流程模板编号 = null, string 状态 = null)
        {
            if (!await 是运行管理角色()) return StatusCode(403, new { error = "无权查看流程版本" });
            return await Db.QueryAsync<object>(@"
SELECT v.""编号"", v.""流程模板编号"", t.""名称"" AS ""流程名称"", v.""版本号"", v.""状态"", v.""流程定义"", v.""变更说明"", v.""生效时间"", v.""创建人编号"", v.""创建时间"", v.""更新时间""
  FROM ""流程版本"" v LEFT JOIN ""流程模板"" t ON t.""编号""=v.""流程模板编号""
 WHERE (@流程模板编号 IS NULL OR v.""流程模板编号""=@流程模板编号) AND (@状态 IS NULL OR v.""状态""=@状态)
 ORDER BY v.""流程模板编号"", v.""版本号"" DESC;", new { 流程模板编号, 状态 = 空值(状态) });
        }

        [HttpPost]
        async public Task<object> 保存流程版本([FromBody]流程版本保存 model)
        {
            if (!await 是运行管理角色()) return StatusCode(403, new { error = "无权维护流程版本" });
            if (model == null || model.流程模板编号 <= 0 || model.版本号 <= 0) throw new ArgumentException("流程模板和版本号无效");
            if ((model.流程定义?.Length ?? 0) > 200000 || (model.变更说明?.Length ?? 0) > 2000) throw new ArgumentException("流程版本字段超出限制");
            var status = string.IsNullOrWhiteSpace(model.状态) ? "草稿" : model.状态.Trim();
            if (status is not ("草稿" or "停用")) throw new ArgumentException("保存时只能使用草稿或停用状态，请通过发布接口生效");
            var definition = string.IsNullOrWhiteSpace(model.流程定义) ? "{}" : model.流程定义.Trim();
            try { using var _ = JsonDocument.Parse(definition); }
            catch (JsonException) { throw new ArgumentException("流程定义必须是有效 JSON"); }
            if (!await Db.QuerySingleAsync<bool>("SELECT EXISTS (SELECT 1 FROM \"流程模板\" WHERE \"编号\"=@流程模板编号);", new { model.流程模板编号 }))
                throw new ArgumentException("流程模板不存在");
            if (model.编号.HasValue)
            {
                var changed = await Db.ExecuteAsync(@"
UPDATE ""流程版本"" SET ""流程模板编号""=@流程模板编号,""版本号""=@版本号,""状态""=@状态,""流程定义""=CAST(@流程定义 AS jsonb),""变更说明""=@变更说明,""生效时间""=@生效时间,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号;", new
                { 编号 = model.编号.Value, model.流程模板编号, model.版本号, 状态 = status, 流程定义 = definition, 变更说明 = 空值(model.变更说明), 生效时间 = model.生效时间 });
                if (changed == 0) return NotFound(new { error = "流程版本不存在" });
                return new { 编号 = model.编号.Value, 状态 = status };
            }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""流程版本"" (""流程模板编号"",""版本号"",""状态"",""流程定义"",""变更说明"",""生效时间"",""创建人编号"")
VALUES (@流程模板编号,@版本号,@状态,CAST(@流程定义 AS jsonb),@变更说明,@生效时间,@创建人编号) RETURNING ""编号"";", new
            { model.流程模板编号, model.版本号, 状态 = status, 流程定义 = definition, 变更说明 = 空值(model.变更说明), model.生效时间, 创建人编号 = CurrentUser.编号 });
            return new { 编号 = id, 状态 = status };
        }

        [HttpPost]
        async public Task<object> 发布流程版本([FromBody]流程版本发布 model)
        {
            if (!await 是运行管理角色()) return StatusCode(403, new { error = "无权发布流程版本" });
            var 流程版本编号 = model?.流程版本编号 ?? 0;
            if (流程版本编号 <= 0) throw new ArgumentException("流程版本编号无效");
            var templateId = await Db.QuerySingleOrDefaultAsync<int?>("SELECT \"流程模板编号\" FROM \"流程版本\" WHERE \"编号\"=@流程版本编号;", new { 流程版本编号 });
            if (!templateId.HasValue) return NotFound(new { error = "流程版本不存在" });
            await Db.ExecuteAsync("UPDATE \"流程版本\" SET \"状态\"='停用',\"更新时间\"=CURRENT_TIMESTAMP WHERE \"流程模板编号\"=@流程模板编号 AND \"状态\"='生效';", new { 流程模板编号 = templateId.Value });
            await Db.ExecuteAsync("UPDATE \"流程版本\" SET \"状态\"='生效',\"生效时间\"=COALESCE(\"生效时间\",CURRENT_TIMESTAMP),\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@流程版本编号;", new { 流程版本编号 });
            return new { 编号 = 流程版本编号, 状态 = "生效" };
        }

        [HttpPost]
        async public Task<object> 创建数据导出任务([FromBody]数据导出任务保存 model)
        {
            if (!await 是运行管理角色()) return StatusCode(403, new { error = "无权创建数据导出任务" });
            var scope = string.IsNullOrWhiteSpace(model?.导出范围) ? "完整数据库" : model.导出范围.Trim();
            if (scope is not ("完整数据库" or "业务数据" or "审计数据")) throw new ArgumentException("导出范围无效");
            var id = await Db.QuerySingleAsync<long>("INSERT INTO \"数据导出任务\" (\"导出范围\",\"申请人编号\") VALUES (@导出范围,@申请人编号) RETURNING \"编号\";", new { 导出范围 = scope, 申请人编号 = CurrentUser.编号 });
            return new { 编号 = id, 状态 = "申请中", 导出范围 = scope, 说明 = "请由受控运维任务执行 deploy/export-postgres-data.sh，并回填文件校验" };
        }

        [HttpGet]
        async public Task<object> 查询数据导出任务(string 状态 = null)
        {
            if (!await 是运行管理角色()) return StatusCode(403, new { error = "无权查看数据导出任务" });
            return await Db.QueryAsync<object>("SELECT * FROM \"数据导出任务\" WHERE (@状态 IS NULL OR \"状态\"=@状态) ORDER BY \"申请时间\" DESC;", new { 状态 = 空值(状态) });
        }

        [HttpPost]
        async public Task<object> 更新数据导出任务([FromBody]数据导出任务保存 model)
        {
            if (!await 是运行管理角色()) return StatusCode(403, new { error = "无权更新数据导出任务" });
            if (model == null || !model.编号.HasValue) throw new ArgumentException("导出任务编号无效");
            var status = string.IsNullOrWhiteSpace(model.状态) ? "生成中" : model.状态.Trim();
            if (status is not ("申请中" or "生成中" or "已完成" or "失败" or "已取消")) throw new ArgumentException("导出任务状态无效");
            if ((model.文件路径?.Length ?? 0) > 1000 || (model.文件校验?.Length ?? 0) > 128 || (model.错误信息?.Length ?? 0) > 4000) throw new ArgumentException("导出任务字段超出限制");
            var changed = await Db.ExecuteAsync(@"
UPDATE ""数据导出任务"" SET ""状态""=@状态,""文件路径""=@文件路径,""文件校验""=@文件校验,""记录数""=@记录数,""错误信息""=@错误信息,""完成时间""=CASE WHEN @状态 IN ('已完成','失败','已取消') THEN CURRENT_TIMESTAMP ELSE ""完成时间"" END WHERE ""编号""=@编号;", new
            { 编号 = model.编号.Value, 状态 = status, 文件路径 = 空值(model.文件路径), 文件校验 = 空值(model.文件校验), model.记录数, 错误信息 = 空值(model.错误信息) });
            if (changed == 0) return NotFound(new { error = "导出任务不存在" });
            return new { 编号 = model.编号.Value, 状态 = status };
        }

        /// <summary>查询已登记的 HR、财务、伦理等外部系统。密钥只保存引用名，不落库。</summary>
        [HttpGet]
        async public Task<object> 获取外部系统(string 系统类型 = null, bool? 仅启用 = null)
        {
            if (!await 是运行管理角色()) return StatusCode(403, new { error = "无权查看外部系统配置" });
            return await Db.QueryAsync<object>(@"
SELECT ""编号"",""系统代码"",""系统名称"",""系统类型"",""基础地址"",""认证方式"",""认证引用"",""是否启用"",""超时秒数"",""最大重试次数"",""建立时间"",""更新时间""
  FROM ""外部系统""
 WHERE (@系统类型 IS NULL OR ""系统类型""=@系统类型)
   AND (@仅启用 IS NULL OR ""是否启用""=@仅启用)
 ORDER BY ""系统类型"",""系统名称"";", new { 系统类型 = 空值(系统类型), 仅启用 });
        }

        /// <summary>检查外部系统注册、地址和认证引用是否满足启用同步 worker 的前置条件。</summary>
        [HttpGet]
        async public Task<object> 获取外部系统配置检查()
        {
            if (!await 是运行管理角色()) return StatusCode(403, new { error = "无权查看外部系统配置检查" });
            var systems = (await Db.QueryAsync<外部系统配置行>(@"
SELECT ""系统代码"",""系统名称"",""系统类型"",""基础地址"",""认证方式"",""认证引用"",""是否启用"",""超时秒数"",""最大重试次数""
  FROM ""外部系统"" ORDER BY ""系统类型"",""系统名称"";")).ToList();
            var requiredTypes = new[] { "HR", "财务", "采购", "伦理", "合同", "人才库", "生物样本库", "论文采集" };
            var checks = new List<object>();
            foreach (var type in requiredTypes)
            {
                var candidates = systems.Where(x => x.系统类型 == type).ToList();
                var enabledCandidates = candidates.Where(x => x.是否启用).ToList();
                var enabledComplete = enabledCandidates.Where(x => 配置完整(x, requireSecret: true)).ToList();
                var configured = enabledCandidates.Count == 1 && enabledComplete.Count == 1;
                checks.Add(new
                {
                    系统类型 = type,
                    已登记 = candidates.Count > 0,
                    已配置地址 = candidates.Any(x => 地址有效(x.基础地址)),
                    已配置认证 = candidates.Any(x => 认证引用有效(x.认证方式, x.认证引用)),
                    认证密钥已注入 = candidates.Any(x => 认证密钥已注入(x.认证方式, x.认证引用)),
                    参数有效 = candidates.Any(x => 参数有效(x.超时秒数, x.最大重试次数)),
                    已启用 = enabledCandidates.Count > 0,
                    启用数量 = enabledCandidates.Count,
                    配置完整数量 = enabledComplete.Count,
                    通过 = configured,
                    提示 = configured ? "已具备唯一启用适配器、地址、认证引用、部署密钥和超时/重试参数" : $"请为 {type} 保留唯一启用适配器，并补齐基础地址、认证引用/部署密钥和超时/重试参数"
                });
            }
            var invalidEnabled = systems.Where(x => x.是否启用 && !配置完整(x, requireSecret: true)).ToList();
            var workerEnabled = config.GetValue("ExternalSync:Enabled", false);
            var allRequiredConfigured = requiredTypes.All(type =>
            {
                var enabled = systems.Where(x => x.系统类型 == type && x.是否启用).ToList();
                return enabled.Count == 1 && 配置完整(enabled[0], requireSecret: true);
            });
            return new
            {
                worker已启用 = workerEnabled,
                可启用worker = allRequiredConfigured && invalidEnabled.Count == 0,
                必需系统 = checks,
                启用但配置不完整 = invalidEnabled.Select(x => new { x.系统代码, x.系统名称, x.系统类型, 问题 = 配置问题(x, requireSecret: true) }).ToList(),
                已登记系统数 = systems.Count,
                说明 = "本检查不发起外部请求；密钥只检查部署环境引用是否存在。真实联调仍需对已确认的测试地址执行测试连接和供应商字段协议验收"
            };
        }

        /// <summary>
        /// 只读检查经费/HRP 适配器契约。它不会创建队列、发票或中间库记录，
        /// 并明确标出当前仍未具备的 OCR、采购订单关联和真实付款回写能力。
        /// </summary>
        [HttpGet]
        async public Task<object> 获取经费适配器契约检查()
        {
            if (!await 是运行管理角色()) return StatusCode(403, new { error = "无权查看经费适配器契约检查" });

            var systems = (await Db.QueryAsync<外部系统配置行>(@"
SELECT ""系统代码"",""系统名称"",""系统类型"",""基础地址"",""认证方式"",""认证引用"",
       ""是否启用"",""超时秒数"",""最大重试次数""
  FROM ""外部系统"" WHERE ""系统类型"" IN ('HR','财务','采购')
 ORDER BY ""系统类型"",""系统代码"";")).ToList();
            var requiredTypes = new[] { "HR", "财务", "采购" };
            var adapterChecks = requiredTypes.Select(type =>
            {
                var rows = systems.Where(x => x.系统类型 == type).ToList();
                var enabled = rows.Where(x => x.是否启用).ToList();
                var complete = enabled.Where(x => 配置完整(x, requireSecret: true)).ToList();
                return new
                {
                    系统类型 = type,
                    已登记数量 = rows.Count,
                    启用数量 = enabled.Count,
                    启用且完整数量 = complete.Count,
                    通过 = enabled.Count == 1 && complete.Count == 1,
                    问题 = enabled.Count == 1 && complete.Count == 1
                        ? Array.Empty<string>()
                        : new[] { $"{type} 需要且仅需要一个启用且配置完整的适配器" }
                };
            }).ToList();

            var schema = await Db.QuerySingleAsync<dynamic>(@"
SELECT
  to_regclass('public.""hrp_sync_project""') IS NOT NULL AS ""项目中间表"",
  to_regclass('public.""hrp_sync_budget""') IS NOT NULL AS ""预算中间表"",
  to_regclass('public.""hrp_sync_incoming""') IS NOT NULL AS ""到账中间表"",
  to_regclass('public.""hrp_sync_expense""') IS NOT NULL AS ""支出中间表"",
  to_regprocedure('public.""sp_hrp中间库双向协同""(integer,character varying)') IS NOT NULL AS ""HRP协同过程"",
  to_regclass('public.""科研经费发票""') IS NOT NULL AS ""发票表"",
  to_regclass('public.""项目经费预算调整申请""') IS NOT NULL AS ""预算调整表"",
  to_regclass('public.""项目经费年度结算""') IS NOT NULL AS ""年度结算表"",
  to_regclass('public.""项目经费台账""') IS NOT NULL AS ""付款台账"",
  EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public'
          AND table_name IN ('科研经费发票','报销','项目经费台账')
          AND column_name IN ('采购订单编号','采购订单号','采购订单ID')) AS ""采购订单关联字段"",
  EXISTS (SELECT 1 FROM pg_proc p WHERE p.proname='sp_hrp中间库双向协同'
          AND (pg_get_functiondef(p.oid) ~ '(模拟|HRP-VOUCHER|管理员.*1580|push_and_replay|replay)'
               OR pg_get_functiondef(p.oid) NOT ILIKE '%push_only%')) AS ""HRP过程含示例逻辑"";");

            var invoiceStates = new List<object>();
            var syntheticRows = 0L;
            if ((bool)schema.发票表)
            {
                invoiceStates = (await Db.QueryAsync<object>(@"
SELECT ""OCR状态"", count(*)::bigint AS ""数量""
  FROM ""科研经费发票"" GROUP BY ""OCR状态"" ORDER BY ""OCR状态"";")).ToList();
            }
            if ((bool)schema.支出中间表)
            {
                syntheticRows = await Db.QuerySingleAsync<long>(@"
SELECT count(*) FROM ""hrp_sync_expense""
 WHERE ""hrp_voucher_no"" LIKE 'HRP-VOUCHER-%'
    OR (""claim_id""=101 AND ""applicant_name""='管理员' AND ""amount""=1580.00);");
            }

            var tableChecks = new[]
            {
                new { 名称 = "HRP项目/预算/到账/支出中间表", 通过 = (bool)schema.项目中间表 && (bool)schema.预算中间表 && (bool)schema.到账中间表 && (bool)schema.支出中间表 },
                new { 名称 = "HRP协同过程", 通过 = (bool)schema.HRP协同过程 && !(bool)schema.HRP过程含示例逻辑 },
                new { 名称 = "发票人工查重表", 通过 = (bool)schema.发票表 },
                new { 名称 = "预算调整和版本表", 通过 = (bool)schema.预算调整表 },
                new { 名称 = "年度结算与付款台账", 通过 = (bool)schema.年度结算表 && (bool)schema.付款台账 },
                new { 名称 = "采购订单关联字段", 通过 = (bool)schema.采购订单关联字段 }
            };
            var workerEnabled = config.GetValue("ExternalSync:Enabled", false);
            var adapterReady = adapterChecks.All(x => x.通过);
            var structureReady = tableChecks.All(x => x.通过);
            var safeForRealIntegration = adapterReady && structureReady && syntheticRows == 0 && !(bool)schema.HRP过程含示例逻辑;
            return new
            {
                只读 = true,
                worker已启用 = workerEnabled,
                外部适配器 = adapterChecks,
                数据结构 = tableChecks,
                发票OCR = new
                {
                    已配置真实OCR = config.GetValue("Finance:InvoiceOcr:Enabled", false),
                    OCR状态统计 = invoiceStates,
                    说明 = "当前接口只支持人工录入和查重；未配置真实 OCR 服务时不得把人工录入标记为已识别"
                },
                采购订单关联 = new
                {
                    已有字段 = (bool)schema.采购订单关联字段,
                    说明 = "发票/报销/付款记录需关联采购订单稳定编号、验收和付款事件后才能形成采购闭环"
                },
                HRP = new
                {
                    中间库对象齐全 = (bool)schema.项目中间表 && (bool)schema.预算中间表 && (bool)schema.到账中间表 && (bool)schema.支出中间表,
                    过程含示例逻辑 = (bool)schema.HRP过程含示例逻辑,
                    疑似示例流水数 = syntheticRows,
                    真实回放可用 = !(bool)schema.HRP过程含示例逻辑 && syntheticRows == 0,
                    说明 = "迁移 085 前的旧过程可能包含示例付款写入；应用 085 后仅允许 push_only 待同步暂存。真实 HRP 回执未联调前仍必须保持阻断"
                },
                真实联调可放行 = safeForRealIntegration,
                说明 = "本检查只读访问 PostgreSQL 和部署配置，不发起 HR/财务/采购网络请求，不生成测试数据；通过后仍需供应商字段协议和脱敏账号回放"
            };
        }

        /// <summary>保存外部系统注册信息，认证密钥由部署环境管理。</summary>
        [HttpPost]
        async public Task<object> 保存外部系统([FromBody]外部系统保存 model)
        {
            if (!await 是运行管理角色()) return StatusCode(403, new { error = "无权维护外部系统配置" });
            if (model == null || string.IsNullOrWhiteSpace(model.系统代码) || string.IsNullOrWhiteSpace(model.系统名称))
                throw new ArgumentException("系统代码和系统名称不能为空");
            var code = model.系统代码.Trim();
            if (code.Length > 50 || model.系统名称.Trim().Length > 100) throw new ArgumentException("系统代码或名称超出长度限制");
            if (model.系统类型 is not ("HR" or "财务" or "采购" or "伦理" or "合同" or "人才库" or "生物样本库" or "论文采集" or "其他"))
                throw new ArgumentException("系统类型无效");
            var auth = string.IsNullOrWhiteSpace(model.认证方式) ? "无" : model.认证方式.Trim();
            if (auth is not ("无" or "Bearer" or "Basic" or "ApiKey" or "OIDC")) throw new ArgumentException("认证方式无效");
            var authRef = 空值(model.认证引用);
            if (!认证引用有效(auth, authRef)) throw new ArgumentException(auth == "无" ? "认证方式为无时不应填写认证引用" : "认证引用格式无效，只允许字母、数字、点、下划线和短横线");
            if (model.是否启用 && auth != "无" && !认证密钥已注入(auth, authRef)) throw new ArgumentException("启用外部系统前必须在部署环境注入认证密钥引用");
            var address = 空值(model.基础地址);
            if (!string.IsNullOrWhiteSpace(address) &&
                (!Uri.TryCreate(address, UriKind.Absolute, out var endpoint) || endpoint.Scheme is not ("http" or "https")))
                throw new ArgumentException("基础地址必须是 http 或 https 地址");
            // An enabled adapter is executable configuration.  Apply the same
            // HTTPS/local-only rule used by the worker and read-only preflight
            // at the write boundary so the UI cannot persist an enabled public
            // HTTP endpoint that will later be rejected by the worker.
            if (model.是否启用 && !地址有效(address))
                throw new ArgumentException("启用外部系统必须填写真实 HTTPS 地址；HTTP 仅允许 localhost/127.0.0.1 本机联调");
            if (model.超时秒数 is < 1 or > 120 || model.最大重试次数 is < 0 or > 10)
                throw new ArgumentException("超时或重试次数超出范围");
            if (model.是否启用 && await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (
  SELECT 1 FROM ""外部系统""
   WHERE ""系统类型""=@系统类型 AND ""是否启用""=TRUE
     AND (@编号 IS NULL OR ""编号""<>@编号)
);", new { model.编号, model.系统类型 }))
                return Conflict(new { error = $"{model.系统类型} 已存在其他启用适配器，请先停用后再启用" });
            var values = new
            {
                编号 = model.编号,
                系统代码 = code,
                系统名称 = model.系统名称.Trim(),
                系统类型 = model.系统类型,
                基础地址 = address,
                认证方式 = auth,
                认证引用 = authRef,
                model.是否启用,
                model.超时秒数,
                model.最大重试次数,
                建立人编号 = CurrentUser.编号
            };
            long id;
            if (model.编号.HasValue)
            {
                var changed = await Db.ExecuteAsync(@"
UPDATE ""外部系统"" SET ""系统代码""=@系统代码,""系统名称""=@系统名称,""系统类型""=@系统类型,""基础地址""=@基础地址,""认证方式""=@认证方式,""认证引用""=@认证引用,""是否启用""=@是否启用,""超时秒数""=@超时秒数,""最大重试次数""=@最大重试次数,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号;", values);
                if (changed == 0) return NotFound(new { error = "外部系统不存在" });
                id = model.编号.Value;
            }
            else
            {
                id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""外部系统"" (""系统代码"",""系统名称"",""系统类型"",""基础地址"",""认证方式"",""认证引用"",""是否启用"",""超时秒数"",""最大重试次数"",""建立人编号"")
VALUES (@系统代码,@系统名称,@系统类型,@基础地址,@认证方式,@认证引用,@是否启用,@超时秒数,@最大重试次数,@建立人编号)
RETURNING ""编号"";", values);
            }
            return new { 编号 = id, 系统代码 = code, 是否启用 = model.是否启用 };
        }

        /// <summary>使用登记的地址和部署环境密钥做一次轻量连接测试，不改变系统启用状态。</summary>
        [HttpPost]
        async public Task<object> 测试外部系统连接([FromBody]外部系统保存 model, [FromQuery] bool dryRun = false)
        {
            if (!await 是运行管理角色()) return StatusCode(403, new { error = "无权测试外部系统连接" });
            if (model == null || !model.编号.HasValue || model.编号 <= 0) throw new ArgumentException("外部系统编号无效");
            var system = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"",""系统名称"",""基础地址"",""认证方式"",""认证引用"",""超时秒数""
       ,""最大重试次数""
  FROM ""外部系统"" WHERE ""编号""=@编号;", new { 编号 = model.编号.Value });
            if (system == null) return NotFound(new { error = "外部系统不存在" });
            var address = (string)system.基础地址;
            var authMode = (string)system.认证方式;
            var authRef = (string)system.认证引用;
            var timeout = (int?)system.超时秒数 ?? 15;
            var preflightProblems = new List<string>();
            if (!地址有效(address)) preflightProblems.Add("基础地址不是有效的 HTTP/HTTPS 地址");
            if (!认证引用有效(authMode, authRef)) preflightProblems.Add("认证引用格式无效");
            if (!参数有效(timeout, (int?)system.最大重试次数 ?? 0)) preflightProblems.Add("超时或最大重试次数无效");
            if (!认证密钥已注入(authMode, authRef)) preflightProblems.Add("部署环境未注入认证密钥引用");
            if (dryRun)
            {
                return new
                {
                    编号 = model.编号.Value,
                    测试模式 = "dry-run",
                    网络请求 = false,
                    测试结果 = preflightProblems.Count == 0 ? "预检通过" : "预检失败",
                    地址已配置 = 地址有效(address),
                    认证引用已配置 = 认证引用有效(authMode, authRef),
                    认证密钥已注入 = 认证密钥已注入(authMode, authRef),
                    参数有效 = 参数有效(timeout, (int?)system.最大重试次数 ?? 0),
                    问题 = preflightProblems,
                    说明 = "dry-run 只检查本地配置，不向外部系统发起请求；预检通过不代表供应商联调成功"
                };
            }
            if (preflightProblems.Count > 0)
                return new { 编号 = model.编号.Value, 测试模式 = "连接测试", 测试结果 = "未执行", 网络请求 = false, 问题 = preflightProblems, 说明 = "配置预检未通过，未发起外部请求" };
            var endpoint = new Uri(address.Trim(), UriKind.Absolute);
            var clientFactory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
            var client = clientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(timeout, 1, 120));
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.TryAddWithoutValidation("X-Request-ID", Guid.NewGuid().ToString("N"));
            try
            {
                ApplyExternalAuthentication(request, authMode, authRef);
                var watch = Stopwatch.StartNew();
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, HttpContext.RequestAborted);
                watch.Stop();
                return new { 编号 = model.编号.Value, 测试模式 = "连接测试", 网络请求 = true, 测试结果 = response.IsSuccessStatusCode ? "通过" : "失败", 状态码 = (int)response.StatusCode, 耗时毫秒 = watch.ElapsedMilliseconds, 说明 = response.IsSuccessStatusCode ? "目标系统已响应，认证响应正常" : "目标系统已响应，但返回非 2xx" };
            }
            catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                return new { 编号 = model.编号.Value, 测试模式 = "连接测试", 网络请求 = true, 测试结果 = "失败", 状态码 = 0, 耗时毫秒 = 0L, 说明 = ex.Message.Length > 500 ? ex.Message.Substring(0, 500) : ex.Message };
            }
        }

        /// <summary>查询外部同步任务，支持按系统和状态过滤。</summary>
        [HttpGet]
        async public Task<object> 查询外部同步任务(long? 外部系统编号 = null, string 状态 = null, int? 数量 = null)
        {
            if (!await 是运行管理角色()) return StatusCode(403, new { error = "无权查看外部同步任务" });
            var limit = Math.Clamp(数量 ?? 100, 1, 500);
            return await Db.QueryAsync<object>(@"
SELECT q.""编号"",q.""外部系统编号"",s.""系统代码"",s.""系统名称"",q.""业务类型"",q.""方向"",q.""业务主键"",q.""幂等键"",q.""状态"",q.""尝试次数"",q.""最大重试次数"",q.""下次重试时间"",q.""请求ID"",q.""外部响应摘要"",q.""最后错误"",q.""创建时间"",q.""更新时间"",q.""完成时间""
  FROM ""外部同步任务"" q JOIN ""外部系统"" s ON s.""编号""=q.""外部系统编号""
 WHERE (@外部系统编号 IS NULL OR q.""外部系统编号""=@外部系统编号)
   AND (@状态 IS NULL OR q.""状态""=@状态)
 ORDER BY q.""创建时间"" DESC LIMIT @数量;", new { 外部系统编号, 状态 = 空值(状态), 数量 = limit });
        }

        /// <summary>创建幂等同步任务。相同幂等键重复提交时返回原任务。</summary>
        [HttpPost]
        async public Task<object> 创建外部同步任务([FromBody]外部同步任务创建 model)
        {
            if (!await 是运行管理角色()) return StatusCode(403, new { error = "无权创建外部同步任务" });
            if (model == null || model.外部系统编号 <= 0 || string.IsNullOrWhiteSpace(model.业务类型) || string.IsNullOrWhiteSpace(model.幂等键))
                throw new ArgumentException("外部系统、业务类型和幂等键不能为空");
            if (model.方向 is not ("推送" or "拉取")) throw new ArgumentException("同步方向无效");
            if (model.业务类型.Trim().Length > 80 || model.幂等键.Trim().Length > 200) throw new ArgumentException("业务类型或幂等键超出长度限制");
            if (!幂等键有效(model.幂等键.Trim())) throw new ArgumentException("幂等键不能包含空白或控制字符");
            var payload = string.IsNullOrWhiteSpace(model.请求负载) ? "{}" : model.请求负载.Trim();
            try { using var _ = JsonDocument.Parse(payload); }
            catch (JsonException) { throw new ArgumentException("请求负载必须是有效 JSON"); }
            var configuredMaxRetries = await Db.QuerySingleOrDefaultAsync<int?>("SELECT \"最大重试次数\" FROM \"外部系统\" WHERE \"编号\"=@外部系统编号;", new { model.外部系统编号 });
            if (!configuredMaxRetries.HasValue)
                return NotFound(new { error = "外部系统不存在" });
            var maxRetries = Math.Clamp(model.最大重试次数 ?? configuredMaxRetries.Value, 0, 10);
            var id = await Db.QuerySingleOrDefaultAsync<long?>(@"
INSERT INTO ""外部同步任务"" (""外部系统编号"",""业务类型"",""方向"",""业务主键"",""幂等键"",""请求负载"",""最大重试次数"",""创建人编号"")
VALUES (@外部系统编号,@业务类型,@方向,@业务主键,@幂等键,CAST(@请求负载 AS jsonb),@最大重试次数,@创建人编号)
ON CONFLICT (""幂等键"") DO NOTHING RETURNING ""编号"";", new
            {
                model.外部系统编号, 业务类型 = model.业务类型.Trim(), model.方向,
                业务主键 = 空值(model.业务主键), 幂等键 = model.幂等键.Trim(), 请求负载 = payload,
                最大重试次数 = maxRetries, 创建人编号 = CurrentUser.编号
            });
            if (!id.HasValue)
                id = await Db.QuerySingleAsync<long>("SELECT \"编号\" FROM \"外部同步任务\" WHERE \"幂等键\"=@幂等键;", new { 幂等键 = model.幂等键.Trim() });
            return new { 编号 = id.Value, 状态 = "待处理", 幂等键 = model.幂等键.Trim() };
        }

        /// <summary>将失败任务退回待处理队列，保留原始幂等键和审计信息。</summary>
        [HttpPost]
        async public Task<object> 重试外部同步任务([FromBody]外部同步任务操作 model)
        {
            if (!await 是运行管理角色()) return StatusCode(403, new { error = "无权重试外部同步任务" });
            if (model == null || model.编号 <= 0) throw new ArgumentException("同步任务编号无效");
            var changed = await Db.ExecuteAsync(@"
UPDATE ""外部同步任务"" SET ""状态""='待处理',""下次重试时间""=CURRENT_TIMESTAMP,""最后错误""=NULL,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@编号 AND ""状态"" IN ('失败','已取消');", new { model.编号 });
            if (changed == 0) return NotFound(new { error = "任务不存在或当前状态不可重试" });
            return new { 编号 = model.编号, 状态 = "待处理" };
        }

        /// <summary>取消尚未处理的同步任务。</summary>
        [HttpPost]
        async public Task<object> 取消外部同步任务([FromBody]外部同步任务操作 model)
        {
            if (!await 是运行管理角色()) return StatusCode(403, new { error = "无权取消外部同步任务" });
            if (model == null || model.编号 <= 0) throw new ArgumentException("同步任务编号无效");
            var changed = await Db.ExecuteAsync(@"
UPDATE ""外部同步任务"" SET ""状态""='已取消',""完成时间""=CURRENT_TIMESTAMP,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@编号 AND ""状态"" IN ('待处理','失败');", new { model.编号 });
            if (changed == 0) return NotFound(new { error = "任务不存在或当前状态不可取消" });
            return new { 编号 = model.编号, 状态 = "已取消" };
        }

        private async Task<bool> 是运行管理角色() => await HasAnyRoleAsync("系统管理员", "科研管理员");
        private static string 空值(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        private static bool 地址有效(string value) => Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var endpoint)
            && (endpoint.Scheme is "http" or "https") && !string.IsNullOrWhiteSpace(endpoint.Host)
            && (endpoint.Scheme == "https" || 本地地址(endpoint))
            && !value.Contains("example.", StringComparison.OrdinalIgnoreCase)
            && !value.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
            && !value.Contains("<host>", StringComparison.OrdinalIgnoreCase);
        private static bool 本地地址(Uri endpoint) => endpoint.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || endpoint.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || endpoint.Host.Equals("::1", StringComparison.OrdinalIgnoreCase);
        private static bool 认证引用格式有效(string reference) => !string.IsNullOrWhiteSpace(reference)
            && reference.Length <= 100 && reference.All(ch => char.IsLetterOrDigit(ch) || ch is '_' or '.' or '-');
        private static bool 认证引用有效(string mode, string reference) => mode == "无"
            ? string.IsNullOrWhiteSpace(reference)
            : 认证引用格式有效(reference);
        private bool 认证密钥已注入(string mode, string reference) => mode == "无"
            || (认证引用格式有效(reference) && !string.IsNullOrWhiteSpace(config[$"ExternalSystems:Secrets:{reference}"]));
        private static bool 参数有效(int timeout, int retries) => timeout is >= 1 and <= 120 && retries is >= 0 and <= 10;
        private bool 配置完整(外部系统配置行 row, bool requireSecret)
            => 地址有效(row.基础地址)
            && 认证引用有效(row.认证方式, row.认证引用)
            && (!requireSecret || 认证密钥已注入(row.认证方式, row.认证引用))
            && 参数有效(row.超时秒数, row.最大重试次数);
        private List<string> 配置问题(外部系统配置行 row, bool requireSecret)
        {
            var problems = new List<string>();
            if (!地址有效(row.基础地址)) problems.Add("基础地址无效");
            if (!认证引用有效(row.认证方式, row.认证引用)) problems.Add("认证引用无效");
            if (requireSecret && !认证密钥已注入(row.认证方式, row.认证引用)) problems.Add("部署环境未注入认证密钥");
            if (!参数有效(row.超时秒数, row.最大重试次数)) problems.Add("超时/重试参数无效");
            return problems;
        }
        private static bool 幂等键有效(string key) => !string.IsNullOrWhiteSpace(key)
            && key.Length <= 200 && key.All(ch => !char.IsWhiteSpace(ch) && !char.IsControl(ch));

        private sealed class 外部系统配置行
        {
            public string 系统代码 { get; set; }
            public string 系统名称 { get; set; }
            public string 系统类型 { get; set; }
            public string 基础地址 { get; set; }
            public string 认证方式 { get; set; }
            public string 认证引用 { get; set; }
            public bool 是否启用 { get; set; }
            public int 超时秒数 { get; set; }
            public int 最大重试次数 { get; set; }
        }

        private void ApplyExternalAuthentication(HttpRequestMessage request, string authMode, string authRef)
        {
            if (string.IsNullOrWhiteSpace(authMode) || authMode == "无") return;
            if (!认证引用格式有效(authRef)) throw new InvalidOperationException("外部系统认证引用格式无效");
            var secret = config[$"ExternalSystems:Secrets:{authRef}"];
            if (string.IsNullOrWhiteSpace(secret)) throw new InvalidOperationException("部署环境缺少外部系统认证密钥");
            switch (authMode)
            {
                case "Bearer":
                case "OIDC": request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret); break;
                case "ApiKey": request.Headers.TryAddWithoutValidation("X-API-Key", secret); break;
                case "Basic": request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(secret))); break;
                default: throw new InvalidOperationException("不支持的外部系统认证方式");
            }
        }
    }
}
