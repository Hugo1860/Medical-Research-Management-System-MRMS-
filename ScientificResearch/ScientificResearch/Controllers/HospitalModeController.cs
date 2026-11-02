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
    public class HospitalModeConfigDto
    {
        public string 运营模式 { get; set; } = "三级综合";
        public string 产品层级 { get; set; }
        public List<string> 隐藏菜单 { get; set; } = new List<string>();
        /// <summary>服务端业务能力开关；隐藏菜单只影响导航，不代表业务边界。</summary>
        public Dictionary<string, bool> 启用功能 { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, object> 字段策略 { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> 流程策略 { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> 统计指标策略 { get; set; } = new Dictionary<string, object>();
        public string 说明 { get; set; }
    }

    /// <summary>
    /// 医院运营模式配置（三级综合 vs 二级/基层精简）
    /// </summary>
    public class HospitalModeController : BaseController
    {
        private static readonly string[] CoreCapabilities =
        {
            "projectLifecycle", "financeCore", "ethics", "achievements", "notifications", "basicStatistics"
        };

        private static readonly string[] ProductTiers = { "基层核心版", "医院增强版", "综合科研平台版" };

        private static string ResolveProductTier(string requested, string mode)
        {
            if (string.IsNullOrWhiteSpace(requested)) return mode == "二级精简" ? "基层核心版" : "综合科研平台版";
            return requested;
        }

        private static Dictionary<string, object> DefaultFieldStrategy(string tier) => new()
        {
            ["required"] = new[] { "项目中文名称", "负责人编号" },
            ["optional"] = tier == "基层核心版" ? new[] { "项目分类", "研究类型" } : new[] { "项目英文名称", "学科类型", "研究类型" },
            ["hidden"] = tier == "基层核心版" ? new[] { "项目英文名称", "国际合作信息" } : Array.Empty<string>()
        };

        private static Dictionary<string, object> DefaultWorkflowStrategy(string tier) => new()
        {
            ["enabled"] = tier == "基层核心版" ? new[] { "立项申请", "成果归档" } : new[] { "立项申请", "伦理审查", "经费执行", "成果归档" },
            ["approval"] = tier == "基层核心版" ? "简化审批" : "分级审批"
        };

        private static Dictionary<string, object> DefaultMetricStrategy(string tier) => new()
        {
            ["enabled"] = tier == "基层核心版" ? new[] { "纵向项目", "科研成果", "科研人员" } : new[] { "纵向项目", "横向项目", "科研成果", "科研人员" },
            ["labels"] = new Dictionary<string, string>()
        };

        private static Dictionary<string, object> ParseStrategy(object value, string tier, string kind)
        {
            var fallback = kind == "字段" ? DefaultFieldStrategy(tier) : kind == "流程" ? DefaultWorkflowStrategy(tier) : DefaultMetricStrategy(tier);
            if (value == null) return fallback;
            try
            {
                var json = value is JsonDocument document ? document.RootElement.GetRawText() : value.ToString();
                return JsonSerializer.Deserialize<Dictionary<string, object>>(json ?? "{}") ?? fallback;
            }
            catch { return fallback; }
        }

        private static readonly Dictionary<string, bool> FullCapabilities = new(StringComparer.OrdinalIgnoreCase)
        {
            ["projectLifecycle"] = true,
            ["financeCore"] = true,
            ["ethics"] = true,
            ["achievements"] = true,
            ["notifications"] = true,
            ["basicStatistics"] = true,
            ["externalIntegration"] = true,
            ["advancedModules"] = true
        };

        private static Dictionary<string, bool> DefaultCapabilities(string mode)
        {
            var result = new Dictionary<string, bool>(FullCapabilities, StringComparer.OrdinalIgnoreCase);
            if (mode == "二级精简")
            {
                result["externalIntegration"] = false;
                result["advancedModules"] = false;
            }
            return result;
        }

        private static Dictionary<string, bool> NormalizeCapabilities(string mode, IDictionary<string, bool> requested)
        {
            var result = DefaultCapabilities(mode);
            if (requested != null)
            {
                foreach (var pair in requested)
                {
                    if (result.ContainsKey(pair.Key)) result[pair.Key] = pair.Value;
                }
            }
            if (mode == "二级精简")
            {
                foreach (var key in CoreCapabilities) result[key] = true;
                // These capabilities are outside the基层/二级核心版 boundary;
                // a client cannot re-enable them by posting a feature payload.
                result["externalIntegration"] = false;
                result["advancedModules"] = false;
            }
            return result;
        }

        private static Dictionary<string, bool> ParseCapabilities(object value, string mode)
        {
            if (value == null) return DefaultCapabilities(mode);
            try
            {
                var json = value is JsonDocument document ? document.RootElement.GetRawText() : value.ToString();
                var parsed = JsonSerializer.Deserialize<Dictionary<string, bool>>(json ?? "{}");
                return NormalizeCapabilities(mode, parsed);
            }
            catch
            {
                return DefaultCapabilities(mode);
            }
        }

        /// <summary>
        /// 获取当前医院运营模式配置
        /// </summary>
        [HttpGet]
        public async Task<object> 获取医院运营配置()
        {
            var config = await Db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT ""编号"", ""运营模式"", ""产品层级"", ""隐藏菜单"", ""启用功能"", ""字段策略"", ""流程策略"", ""统计指标策略"", ""配置版本"", ""说明"", ""更新人"", ""更新时间""
                  FROM ""医院运营配置"" WHERE ""编号"" = 1;");

            if (config == null)
            {
                return new
                {
                    运营模式 = "三级综合",
                    产品层级 = "综合科研平台版",
                    隐藏菜单 = new List<string> { "/office/biobank-ledger", "/office/education-statistics", "/governance/proxy-reconciliation", "/operations/external-sync", "/project/vertical-import", "/finance/fund-execution" },
                    启用功能 = DefaultCapabilities("三级综合"),
                    字段策略 = DefaultFieldStrategy("综合科研平台版"),
                    流程策略 = DefaultWorkflowStrategy("综合科研平台版"),
                    统计指标策略 = DefaultMetricStrategy("综合科研平台版"),
                    配置版本 = 1,
                    说明 = "医院运营模式配置（默认三级综合）",
                    更新人 = "系统",
                    更新时间 = DateTime.UtcNow
                };
            }

            var mode = (string)config.运营模式;
            var productTier = ResolveProductTier((string)config.产品层级, mode);
            var capabilities = ParseCapabilities(config.启用功能, mode);
            List<string> hiddenMenus = new List<string>();
            try
            {
                if (config.隐藏菜单 != null)
                {
                    string json = config.隐藏菜单.ToString();
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        hiddenMenus = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                    }
                }
            }
            catch
            {
                hiddenMenus = new List<string>();
            }

            return new
            {
                运营模式 = mode,
                产品层级 = productTier,
                隐藏菜单 = hiddenMenus,
                启用功能 = capabilities,
                字段策略 = ParseStrategy(config.字段策略, productTier, "字段"),
                流程策略 = ParseStrategy(config.流程策略, productTier, "流程"),
                统计指标策略 = ParseStrategy(config.统计指标策略, productTier, "统计指标"),
                配置版本 = (int?)config.配置版本 ?? 1,
                说明 = (string)config.说明,
                更新人 = (string)config.更新人,
                更新时间 = (DateTime?)config.更新时间
            };
        }

        /// <summary>
        /// 保存医院运营模式配置（仅系统管理员/科研管理员/科技办）
        /// </summary>
        [HttpPost]
        public async Task<object> 保存医院运营配置([FromBody] HospitalModeConfigDto model)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办"))
            {
                return StatusCode(403, new { error = "无权修改医院运营模式配置" });
            }

            if (model == null || string.IsNullOrWhiteSpace(model.运营模式))
            {
                return BadRequest(new { error = "运营模式不能为空" });
            }

            if (model.运营模式 != "三级综合" && model.运营模式 != "二级精简")
            {
                return BadRequest(new { error = "运营模式仅支持：三级综合、二级精简" });
            }

            var productTier = ResolveProductTier(model.产品层级, model.运营模式);
            if (!ProductTiers.Contains(productTier))
            {
                return BadRequest(new { error = "产品层级仅支持：基层核心版、医院增强版、综合科研平台版" });
            }

            var capabilities = NormalizeCapabilities(model.运营模式, model.启用功能);
            var fieldStrategy = ParseStrategy(model.字段策略, productTier, "字段");
            var workflowStrategy = ParseStrategy(model.流程策略, productTier, "流程");
            var metricStrategy = ParseStrategy(model.统计指标策略, productTier, "统计指标");
            var hiddenJson = JsonSerializer.Serialize(model.隐藏菜单 ?? new List<string>());
            var capabilityJson = JsonSerializer.Serialize(capabilities);
            var fieldStrategyJson = JsonSerializer.Serialize(fieldStrategy);
            var workflowStrategyJson = JsonSerializer.Serialize(workflowStrategy);
            var metricStrategyJson = JsonSerializer.Serialize(metricStrategy);
            var currentUser = CurrentUser?.姓名 ?? "管理员";

            await Db.ExecuteAsync(
                @"INSERT INTO ""医院运营配置"" (""编号"", ""运营模式"", ""产品层级"", ""隐藏菜单"", ""启用功能"", ""字段策略"", ""流程策略"", ""统计指标策略"", ""配置版本"", ""说明"", ""更新人"", ""更新时间"")
                  VALUES (1, @运营模式, @产品层级, @隐藏菜单::jsonb, @启用功能::jsonb, @字段策略::jsonb, @流程策略::jsonb, @统计指标策略::jsonb, 3, @说明, @更新人, CURRENT_TIMESTAMP)
                  ON CONFLICT (""编号"") DO UPDATE
                  SET ""运营模式"" = EXCLUDED.""运营模式"",
                      ""产品层级"" = EXCLUDED.""产品层级"",
                      ""隐藏菜单"" = EXCLUDED.""隐藏菜单"",
                      ""启用功能"" = EXCLUDED.""启用功能"",
                      ""字段策略"" = EXCLUDED.""字段策略"",
                      ""流程策略"" = EXCLUDED.""流程策略"",
                      ""统计指标策略"" = EXCLUDED.""统计指标策略"",
                      ""配置版本"" = GREATEST(""医院运营配置"".""配置版本"", 3),
                      ""说明"" = EXCLUDED.""说明"",
                      ""更新人"" = EXCLUDED.""更新人"",
                      ""更新时间"" = CURRENT_TIMESTAMP;",
                new
                {
                    运营模式 = model.运营模式,
                    产品层级 = productTier,
                    隐藏菜单 = hiddenJson,
                    启用功能 = capabilityJson,
                    字段策略 = fieldStrategyJson,
                    流程策略 = workflowStrategyJson,
                    统计指标策略 = metricStrategyJson,
                    说明 = model.说明 ?? (model.运营模式 == "二级精简" ? "二级/基层科教精简模式（聚焦临床科研与论文风控）" : "三级医院全功能综合模式"),
                    更新人 = currentUser
                });

            return new { success = true, message = "医院运营模式配置已保存", 运营模式 = model.运营模式, 产品层级 = productTier, 启用功能 = capabilities, 字段策略 = fieldStrategy, 流程策略 = workflowStrategy, 统计指标策略 = metricStrategy, 配置版本 = 3 };
        }

        /// <summary>返回服务端实际采用的核心能力边界，供前端和验收脚本使用。</summary>
        [HttpGet]
        public async Task<object> 获取医院核心版能力()
        {
            var config = await Db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT ""运营模式"", ""产品层级"", ""启用功能"", ""配置版本"" FROM ""医院运营配置"" WHERE ""编号""=1;");
            var mode = (string)(config?.运营模式 ?? "三级综合");
            var productTier = ResolveProductTier((string)config?.产品层级, mode);
            var capabilities = ParseCapabilities(config?.启用功能, mode);
            return new
            {
                运营模式 = mode,
                产品层级 = productTier,
                核心版 = mode == "二级精简",
                启用功能 = capabilities,
                配置版本 = (int?)config?.配置版本 ?? 1,
                服务端强制 = true,
                说明 = "功能开关由服务端业务接口读取；隐藏菜单只负责导航展示"
            };
        }
    }
}
