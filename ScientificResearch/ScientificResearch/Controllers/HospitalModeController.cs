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
        public List<string> 隐藏菜单 { get; set; } = new List<string>();
        public string 说明 { get; set; }
    }

    /// <summary>
    /// 医院运营模式配置（三级综合 vs 二级/基层精简）
    /// </summary>
    public class HospitalModeController : BaseController
    {
        /// <summary>
        /// 获取当前医院运营模式配置
        /// </summary>
        [HttpGet]
        public async Task<object> 获取医院运营配置()
        {
            var config = await Db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT ""编号"", ""运营模式"", ""隐藏菜单"", ""说明"", ""更新人"", ""更新时间""
                  FROM ""医院运营配置"" WHERE ""编号"" = 1;");

            if (config == null)
            {
                return new
                {
                    运营模式 = "三级综合",
                    隐藏菜单 = new List<string> { "/office/biobank-ledger", "/office/education-statistics", "/governance/proxy-reconciliation", "/operations/external-sync", "/project/vertical-import", "/finance/fund-execution" },
                    说明 = "医院运营模式配置（默认三级综合）",
                    更新人 = "系统",
                    更新时间 = DateTime.UtcNow
                };
            }

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
                运营模式 = (string)config.运营模式,
                隐藏菜单 = hiddenMenus,
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

            var hiddenJson = JsonSerializer.Serialize(model.隐藏菜单 ?? new List<string>());
            var currentUser = CurrentUser?.姓名 ?? "管理员";

            await Db.ExecuteAsync(
                @"INSERT INTO ""医院运营配置"" (""编号"", ""运营模式"", ""隐藏菜单"", ""说明"", ""更新人"", ""更新时间"")
                  VALUES (1, @运营模式, @隐藏菜单::jsonb, @说明, @更新人, CURRENT_TIMESTAMP)
                  ON CONFLICT (""编号"") DO UPDATE
                  SET ""运营模式"" = EXCLUDED.""运营模式"",
                      ""隐藏菜单"" = EXCLUDED.""隐藏菜单"",
                      ""说明"" = EXCLUDED.""说明"",
                      ""更新人"" = EXCLUDED.""更新人"",
                      ""更新时间"" = CURRENT_TIMESTAMP;",
                new
                {
                    运营模式 = model.运营模式,
                    隐藏菜单 = hiddenJson,
                    说明 = model.说明 ?? (model.运营模式 == "二级精简" ? "二级/基层科教精简模式（聚焦临床科研与论文风控）" : "三级医院全功能综合模式"),
                    更新人 = currentUser
                });

            return new { success = true, message = "医院运营模式配置已保存" };
        }
    }
}
