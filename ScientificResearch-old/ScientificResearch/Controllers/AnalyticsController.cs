using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using ScientificResearch.Infrastucture;
using ScientificResearch.Models;

namespace ScientificResearch.Controllers
{
    /// <summary>科研瓶颈、方法学服务、入组、数据服务和资源使用分析。</summary>
    public class AnalyticsController : BaseController
    {
        [HttpGet]
        async public Task<object> 查询科研瓶颈事件(string 事件类型 = null, string 状态 = null, string 严重程度 = null, int? 人员编号 = null, int? 数量 = null)
        {
            var admin = await 是分析管理角色();
            var targetPerson = 人员编号 ?? (admin ? (int?)null : CurrentUser.编号);
            if (!admin && targetPerson.HasValue && targetPerson.Value != CurrentUser.编号) return StatusCode(403, new { error = "无权查看其他人员的瓶颈事件" });
            var limit = Math.Clamp(数量 ?? 100, 1, 500);
            return await Db.QueryAsync<object>(@"
SELECT e.""编号"", e.""流程模板编号"", e.""项目编号"", e.""人员编号"", p.""姓名"", e.""事件类型"", e.""原因代码"", e.""标题"", e.""描述"", e.""严重程度"", e.""状态"", e.""开始时间"", e.""解决时间"", e.""等待天数"", e.""责任人编号"", e.""来源类型"", e.""来源编号"", e.""建立人编号"", e.""更新时间""
  FROM ""科研瓶颈事件"" e LEFT JOIN ""人员"" p ON p.""编号""=e.""人员编号""
 WHERE (@事件类型 IS NULL OR e.""事件类型""=@事件类型)
   AND (@状态 IS NULL OR e.""状态""=@状态)
   AND (@严重程度 IS NULL OR e.""严重程度""=@严重程度)
   AND (@人员编号 IS NULL OR e.""人员编号""=@人员编号)
 ORDER BY CASE e.""严重程度"" WHEN '紧急' THEN 1 WHEN '高' THEN 2 WHEN '中' THEN 3 ELSE 4 END, e.""开始时间"" DESC
 LIMIT @数量;", new
            { 事件类型 = 空值(事件类型), 状态 = 空值(状态), 严重程度 = 空值(严重程度), 人员编号 = targetPerson, 数量 = limit });
        }

        [HttpPost]
        async public Task<object> 保存科研瓶颈事件([FromBody]科研瓶颈事件保存 model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.事件类型) || string.IsNullOrWhiteSpace(model.标题)) throw new ArgumentException("事件类型和标题不能为空");
            if (!事件类型有效(model.事件类型) || !严重程度有效(model.严重程度) || !瓶颈状态有效(model.状态)) throw new ArgumentException("瓶颈事件状态或类型无效");
            if (model.标题.Trim().Length > 200 || (model.原因代码?.Length ?? 0) > 100 || (model.描述?.Length ?? 0) > 4000 || (model.来源类型?.Length ?? 0) > 50 || (model.来源编号?.Length ?? 0) > 200) throw new ArgumentException("瓶颈事件字段超出限制");
            if (model.开始时间.HasValue && model.解决时间.HasValue && model.解决时间.Value < model.开始时间.Value) throw new ArgumentException("解决时间不能早于开始时间");
            if (!await 有范围权限(model.流程模板编号, model.项目编号, model.人员编号)) return ProjectAccessDenied();
            var start = model.开始时间 ?? DateTime.Now;
            var end = model.解决时间;
            var waitDays = model.等待天数;
            if (!waitDays.HasValue && end.HasValue) waitDays = (decimal)(end.Value - start).TotalDays;
            if (waitDays.HasValue && waitDays.Value < 0) throw new ArgumentException("等待天数不能为负数");
            var status = model.状态.Trim();
            if (model.编号.HasValue)
            {
                var changed = await Db.ExecuteAsync(@"
UPDATE ""科研瓶颈事件"" SET ""流程模板编号""=@流程模板编号,""项目编号""=@项目编号,""人员编号""=@人员编号,""事件类型""=@事件类型,""原因代码""=@原因代码,""标题""=@标题,""描述""=@描述,""严重程度""=@严重程度,""状态""=@状态,""开始时间""=@开始时间,""解决时间""=@解决时间,""等待天数""=@等待天数,""责任人编号""=@责任人编号,""来源类型""=@来源类型,""来源编号""=@来源编号,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号;", new
                {
                    编号 = model.编号.Value, model.流程模板编号, model.项目编号, model.人员编号, 事件类型 = model.事件类型.Trim(), 原因代码 = 空值(model.原因代码), 标题 = model.标题.Trim(), 描述 = 空值(model.描述), 严重程度 = model.严重程度.Trim(), 状态 = status,
                    开始时间 = start, 解决时间 = end, 等待天数 = waitDays, model.责任人编号, 来源类型 = 空值(model.来源类型), 来源编号 = 空值(model.来源编号)
                });
                if (changed == 0) return NotFound(new { error = "瓶颈事件不存在" });
                return new { 编号 = model.编号.Value, 状态 = status };
            }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研瓶颈事件"" (""流程模板编号"",""项目编号"",""人员编号"",""事件类型"",""原因代码"",""标题"",""描述"",""严重程度"",""状态"",""开始时间"",""解决时间"",""等待天数"",""责任人编号"",""来源类型"",""来源编号"",""建立人编号"")
VALUES (@流程模板编号,@项目编号,@人员编号,@事件类型,@原因代码,@标题,@描述,@严重程度,@状态,@开始时间,@解决时间,@等待天数,@责任人编号,@来源类型,@来源编号,@建立人编号) RETURNING ""编号"";", new
            {
                model.流程模板编号, model.项目编号, model.人员编号, 事件类型 = model.事件类型.Trim(), 原因代码 = 空值(model.原因代码), 标题 = model.标题.Trim(), 描述 = 空值(model.描述), 严重程度 = model.严重程度.Trim(), 状态 = status,
                开始时间 = start, 解决时间 = end, 等待天数 = waitDays, model.责任人编号, 来源类型 = 空值(model.来源类型), 来源编号 = 空值(model.来源编号), 建立人编号 = CurrentUser.编号
            });
            return new { 编号 = id, 状态 = status };
        }

        [HttpGet]
        async public Task<object> 获取科研瓶颈汇总(string 事件类型 = null, string 状态 = null, string 严重程度 = null)
        {
            if (!await 是分析管理角色()) return StatusCode(403, new { error = "无权查看科研瓶颈汇总" });
            return await Db.QueryAsync<object>(@"
SELECT ""事件类型"", ""原因代码"", ""严重程度"", ""状态"", ""事件数"", ""平均等待天数"", ""最大等待天数"", ""最早开始时间""
  FROM ""v_科研瓶颈汇总""
 WHERE (@事件类型 IS NULL OR ""事件类型""=@事件类型) AND (@状态 IS NULL OR ""状态""=@状态) AND (@严重程度 IS NULL OR ""严重程度""=@严重程度)
 ORDER BY ""事件数"" DESC, ""平均等待天数"" DESC NULLS LAST;", new { 事件类型 = 空值(事件类型), 状态 = 空值(状态), 严重程度 = 空值(严重程度) });
        }

        [HttpGet]
        async public Task<object> 获取科研资源使用汇总(string 资源类型 = null, string 状态 = null)
        {
            if (!await 是分析管理角色()) return StatusCode(403, new { error = "无权查看科研资源使用汇总" });
            return await Db.QueryAsync<object>(@"
SELECT ""资源类型"", ""资源名称"", ""状态"", ""申请数"", ""使用小时"", ""等待小时"", ""平均等待小时""
  FROM ""v_科研资源使用汇总""
 WHERE (@资源类型 IS NULL OR ""资源类型""=@资源类型) AND (@状态 IS NULL OR ""状态""=@状态)
 ORDER BY ""等待小时"" DESC, ""使用小时"" DESC;", new { 资源类型 = 空值(资源类型), 状态 = 空值(状态) });
        }

        [HttpPost]
        async public Task<object> 保存科研资源使用([FromBody]科研资源使用记录保存 model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.资源类型) || string.IsNullOrWhiteSpace(model.资源名称)) throw new ArgumentException("资源类型和名称不能为空");
            if (!资源状态有效(model.状态)) throw new ArgumentException("资源使用状态无效");
            if ((model.资源类型?.Length ?? 0) > 50 || (model.资源名称?.Length ?? 0) > 200 || (model.备注?.Length ?? 0) > 2000) throw new ArgumentException("资源使用字段超出限制");
            if (model.实际开始时间.HasValue && model.实际结束时间.HasValue && model.实际结束时间.Value < model.实际开始时间.Value) throw new ArgumentException("结束时间不能早于开始时间");
            if (!await 有范围权限(model.流程模板编号, model.项目编号, model.人员编号)) return ProjectAccessDenied();
            if ((model.数量.HasValue && model.数量 < 0) || (model.使用小时.HasValue && model.使用小时 < 0) || (model.等待小时.HasValue && model.等待小时 < 0)) throw new ArgumentException("资源使用数值不能为负数");
            if (model.编号.HasValue)
            {
                var changed = await Db.ExecuteAsync(@"
UPDATE ""科研资源使用记录"" SET ""流程模板编号""=@流程模板编号,""项目编号""=@项目编号,""人员编号""=@人员编号,""资源类型""=@资源类型,""资源名称""=@资源名称,""申请时间""=@申请时间,""计划开始时间""=@计划开始时间,""实际开始时间""=@实际开始时间,""实际结束时间""=@实际结束时间,""状态""=@状态,""数量""=@数量,""使用小时""=@使用小时,""等待小时""=@等待小时,""备注""=@备注,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号;", new
                {
                    编号 = model.编号.Value, model.流程模板编号, model.项目编号, model.人员编号, 资源类型 = model.资源类型.Trim(), 资源名称 = model.资源名称.Trim(), 申请时间 = model.申请时间 ?? DateTime.Now,
                    计划开始时间 = model.计划开始时间, 实际开始时间 = model.实际开始时间, 实际结束时间 = model.实际结束时间, 状态 = model.状态.Trim(), model.数量, model.使用小时, model.等待小时, 备注 = 空值(model.备注)
                });
                if (changed == 0) return NotFound(new { error = "资源使用记录不存在" });
                return new { 编号 = model.编号.Value };
            }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研资源使用记录"" (""流程模板编号"",""项目编号"",""人员编号"",""资源类型"",""资源名称"",""申请时间"",""计划开始时间"",""实际开始时间"",""实际结束时间"",""状态"",""数量"",""使用小时"",""等待小时"",""备注"",""建立人编号"")
VALUES (@流程模板编号,@项目编号,@人员编号,@资源类型,@资源名称,@申请时间,@计划开始时间,@实际开始时间,@实际结束时间,@状态,@数量,@使用小时,@等待小时,@备注,@建立人编号) RETURNING ""编号"";", new
            {
                model.流程模板编号, model.项目编号, model.人员编号, 资源类型 = model.资源类型.Trim(), 资源名称 = model.资源名称.Trim(), 申请时间 = model.申请时间 ?? DateTime.Now,
                model.计划开始时间, model.实际开始时间, model.实际结束时间, 状态 = model.状态.Trim(), model.数量, model.使用小时, model.等待小时, 备注 = 空值(model.备注), 建立人编号 = CurrentUser.编号
            });
            return new { 编号 = id };
        }

        [HttpGet]
        async public Task<object> 获取科研决策摘要()
        {
            if (!await 是分析管理角色()) return StatusCode(403, new { error = "无权查看科研决策摘要" });
            var bottlenecks = await Db.QueryAsync<object>(@"
SELECT ""事件类型"", ""原因代码"", ""严重程度"", ""状态"", ""事件数"", ""平均等待天数""
  FROM ""v_科研瓶颈汇总"" ORDER BY ""事件数"" DESC, ""平均等待天数"" DESC NULLS LAST LIMIT 10;");
            var resources = await Db.QueryAsync<object>(@"
SELECT ""资源类型"", ""资源名称"", ""申请数"", ""使用小时"", ""等待小时"", ""平均等待小时""
  FROM ""v_科研资源使用汇总"" WHERE ""状态"" <> '已取消' ORDER BY ""等待小时"" DESC LIMIT 10;");
            var open = await Db.QuerySingleAsync<int>("SELECT count(*)::integer FROM \"科研瓶颈事件\" WHERE \"状态\" IN ('开放','处理中');");
            var waiting = await Db.QuerySingleAsync<decimal>("SELECT coalesce(sum(\"等待小时\"),0) FROM \"科研资源使用记录\" WHERE \"状态\" IN ('申请中','已排期');");
            return new { 开放瓶颈数 = open, 待处理资源等待小时 = waiting, 主要瓶颈 = bottlenecks, 资源等待 = resources };
        }

        /// <summary>为统计菜单提供统一、可解释的指标入口。</summary>
        [HttpGet]
        async public Task<object> 获取科研统计概览(string 维度 = null)
        {
            if (!await 是分析管理角色()) return StatusCode(403, new { error = "无权查看科研统计" });
            var mode = string.IsNullOrWhiteSpace(维度) ? "overview" : 维度.Trim();
            if (mode is not ("overview" or "project" or "result" or "people")) mode = "overview";

            var 指标 = await Db.QueryAsync<object>(@"
WITH metrics AS (
SELECT '纵向项目' AS ""指标"", count(*)::integer AS ""数量"", coalesce(sum(""批准经费""),0) AS ""数值"", '立项项目批准经费' AS ""说明"" FROM ""v4_纵向项目总览""
 UNION ALL SELECT '横向项目', count(*)::integer, coalesce(sum(""合同金额""),0), '横向合同金额' FROM ""v4_横向项目总览""
 UNION ALL SELECT '科研成果', count(*)::integer, 0, '论文、著作、专利和获奖成果总数' FROM ""v4_成果总览""
 UNION ALL SELECT '科研人员', count(*)::integer, 0, '人员主数据总数' FROM ""人员""
)
SELECT * FROM metrics
 WHERE @维度='overview'
    OR (@维度='project' AND ""指标"" IN ('纵向项目','横向项目'))
    OR (@维度='result' AND ""指标""='科研成果')
    OR (@维度='people' AND ""指标""='科研人员')
 ORDER BY ""指标"";", new { 维度 = mode });
            var 口径 = await Db.QueryAsync<object>(@"
SELECT ""编号"",""指标代码"",""指标名称"",""维度"",""统计单位"",""口径说明"",""版本号"",""状态"",""更新时间""
  FROM ""科研统计指标口径""
 WHERE ""状态""='生效' AND (""维度""=@维度 OR ""维度""='overview')
 ORDER BY ""指标名称"";", new { 维度 = mode });
            return new { 维度 = mode, 指标, 口径 };
        }

        [HttpGet]
        async public Task<object> 获取科研统计指标口径(string 维度 = null, bool? 仅生效 = null)
        {
            if (!await 是分析管理角色()) return StatusCode(403, new { error = "无权查看统计指标口径" });
            var dimension = string.IsNullOrWhiteSpace(维度) ? null : 维度.Trim();
            if (dimension is not (null or "overview" or "project" or "result" or "people")) throw new ArgumentException("统计维度无效");
            return await Db.QueryAsync<object>(@"
SELECT ""编号"",""指标代码"",""指标名称"",""维度"",""统计单位"",""口径说明"",""版本号"",""状态"",""建立时间"",""更新时间""
  FROM ""科研统计指标口径""
 WHERE (@维度 IS NULL OR ""维度""=@维度 OR (@维度='project' AND ""维度""='overview'))
   AND (@仅生效 IS NULL OR (@仅生效=TRUE AND ""状态""='生效') OR (@仅生效=FALSE AND ""状态""<>'生效'))
 ORDER BY ""指标代码"",""版本号"" DESC;", new { 维度 = dimension, 仅生效 });
        }

        [HttpPost]
        async public Task<object> 保存科研统计指标口径([FromBody]科研统计指标口径保存 model)
        {
            if (!await 是分析管理角色()) return StatusCode(403, new { error = "无权维护统计指标口径" });
            if (model == null || string.IsNullOrWhiteSpace(model.指标代码) || string.IsNullOrWhiteSpace(model.指标名称) || string.IsNullOrWhiteSpace(model.口径说明))
                throw new ArgumentException("指标代码、名称和口径说明不能为空");
            var code = model.指标代码.Trim();
            if (!Regex.IsMatch(code, "^[A-Za-z0-9_-]{2,80}$")) throw new ArgumentException("指标代码只能使用字母、数字、下划线或短横线");
            if (model.指标名称.Trim().Length > 100 || model.统计单位?.Trim().Length > 50 || model.口径说明.Trim().Length > 2000) throw new ArgumentException("统计口径字段超出限制");
            if (model.维度 is not ("overview" or "project" or "result" or "people")) throw new ArgumentException("统计维度无效");
            if (model.版本号 <= 0) throw new ArgumentException("版本号必须大于0");
            var status = string.IsNullOrWhiteSpace(model.状态) ? "草稿" : model.状态.Trim();
            if (status is not ("草稿" or "生效" or "停用")) throw new ArgumentException("统计口径状态无效");
            if (status == "生效") await Db.ExecuteAsync("UPDATE \"科研统计指标口径\" SET \"状态\"='停用',\"更新时间\"=CURRENT_TIMESTAMP WHERE \"指标代码\"=@指标代码 AND \"状态\"='生效' AND (@编号 IS NULL OR \"编号\"<>@编号);", new { 指标代码 = code, model.编号 });
            if (model.编号.HasValue)
            {
                var changed = await Db.ExecuteAsync(@"
UPDATE ""科研统计指标口径"" SET ""指标代码""=@指标代码,""指标名称""=@指标名称,""维度""=@维度,""统计单位""=@统计单位,""口径说明""=@口径说明,""版本号""=@版本号,""状态""=@状态,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号;", new
                { 编号 = model.编号.Value, 指标代码 = code, 指标名称 = model.指标名称.Trim(), 维度 = model.维度, 统计单位 = 空值(model.统计单位) ?? "条", 口径说明 = model.口径说明.Trim(), model.版本号, 状态 = status });
                if (changed == 0) return NotFound(new { error = "统计指标口径不存在" });
                return new { 编号 = model.编号.Value, 状态 = status };
            }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研统计指标口径"" (""指标代码"",""指标名称"",""维度"",""统计单位"",""口径说明"",""版本号"",""状态"",""建立人编号"")
VALUES (@指标代码,@指标名称,@维度,@统计单位,@口径说明,@版本号,@状态,@建立人编号) RETURNING ""编号"";", new
            { 指标代码 = code, 指标名称 = model.指标名称.Trim(), model.维度, 统计单位 = 空值(model.统计单位) ?? "条", 口径说明 = model.口径说明.Trim(), model.版本号, 状态 = status, 建立人编号 = CurrentUser.编号 });
            return new { 编号 = id, 状态 = status };
        }

        /// <summary>按统计指标钻取明细，返回与汇总指标相同口径的可追溯记录。</summary>
        [HttpGet]
        async public Task<object> 获取科研统计钻取(string 指标, int? 数量 = null)
        {
            if (!await 是分析管理角色()) return StatusCode(403, new { error = "无权查看科研统计明细" });
            var metric = string.IsNullOrWhiteSpace(指标) ? null : 指标.Trim();
            var limit = Math.Clamp(数量 ?? 100, 1, 500);
            if (metric == "纵向项目")
            {
                var rows = await Db.QueryAsync<object>(@"
SELECT ""编号"",""项目中文名称"" AS ""名称"",""项目类型"",""批准经费"",""负责人姓名"",""负责人部门名称"",""计划开始日期"",""计划完成日期"",""审核进度""
  FROM ""v4_纵向项目总览"" ORDER BY ""批准经费"" DESC NULLS LAST,""编号"" DESC LIMIT @数量;", new { 数量 = limit });
                return new { 指标 = metric, 口径说明 = "已立项纵向项目视图，按批准经费降序，金额单位为元。", 生成时间 = DateTime.Now, 明细 = rows };
            }
            if (metric == "横向项目")
            {
                var rows = await Db.QueryAsync<object>(@"
SELECT ""编号"",""合同名称"" AS ""名称"",""合同编号"",""合同金额"",""项目负责人姓名"",""项目负责人部门名称"",""签订时间"",""审核进度""
  FROM ""v4_横向项目总览"" ORDER BY ""合同金额"" DESC NULLS LAST,""编号"" DESC LIMIT @数量;", new { 数量 = limit });
                return new { 指标 = metric, 口径说明 = "已登记横向项目合同视图，按合同金额降序，金额单位为元。", 生成时间 = DateTime.Now, 明细 = rows };
            }
            if (metric == "科研成果")
            {
                var rows = await Db.QueryAsync<object>(@"
SELECT ""编号"",""成果名称"",""成果类型"",""发起人姓名"",""发起人部门名称"",""路径""
  FROM ""v4_成果总览"" ORDER BY ""编号"" DESC LIMIT @数量;", new { 数量 = limit });
                return new { 指标 = metric, 口径说明 = "成果总览视图中的论文、著作、专利和获奖记录。", 生成时间 = DateTime.Now, 明细 = rows };
            }
            if (metric == "科研人员")
            {
                var rows = await Db.QueryAsync<object>(@"
SELECT COALESCE(NULLIF(""部门名称"",''),'未分配部门') AS ""部门名称"", count(*)::integer AS ""人数"", count(*) FILTER (WHERE ""是否启用"")::integer AS ""启用人数""
  FROM ""v1_人员_带部门名"" GROUP BY COALESCE(NULLIF(""部门名称"",''),'未分配部门') ORDER BY ""人数"" DESC,""部门名称"" LIMIT @数量;", new { 数量 = limit });
                return new { 指标 = metric, 口径说明 = "人员主数据按部门汇总，启用人数按是否启用字段统计。", 生成时间 = DateTime.Now, 明细 = rows };
            }
            throw new ArgumentException("不支持的统计指标");
        }

        /// <summary>导出当前统计维度，供管理部门留档。</summary>
        [HttpGet]
        async public Task<IActionResult> 导出科研统计概览(string 维度 = null)
        {
            if (!await 是分析管理角色()) return StatusCode(403, new { error = "无权导出科研统计" });
            var mode = string.IsNullOrWhiteSpace(维度) ? "overview" : 维度.Trim();
            if (mode is not ("overview" or "project" or "result" or "people")) mode = "overview";
            var rows = await Db.QueryAsync<dynamic>(@"
WITH metrics AS (
SELECT '纵向项目' AS ""指标"", count(*)::integer AS ""数量"", coalesce(sum(""批准经费""),0) AS ""数值"", '立项项目批准经费' AS ""说明"" FROM ""v4_纵向项目总览""
 UNION ALL SELECT '横向项目', count(*)::integer, coalesce(sum(""合同金额""),0), '横向合同金额' FROM ""v4_横向项目总览""
 UNION ALL SELECT '科研成果', count(*)::integer, 0, '论文、著作、专利和获奖成果总数' FROM ""v4_成果总览""
 UNION ALL SELECT '科研人员', count(*)::integer, 0, '人员主数据总数' FROM ""人员""
)
SELECT * FROM metrics
 WHERE @维度='overview'
    OR (@维度='project' AND ""指标"" IN ('纵向项目','横向项目'))
    OR (@维度='result' AND ""指标""='科研成果')
    OR (@维度='people' AND ""指标""='科研人员')
 ORDER BY ""指标"";", new { 维度 = mode });
            static string Csv(object value) => "\"" + (value?.ToString() ?? string.Empty).Replace("\"", "\"\"") + "\"";
            var csv = new StringBuilder("\uFEFF维度,指标,数量,数值,说明\r\n");
            foreach (var row in rows)
            {
                csv.AppendJoin(',', new object[] { mode, row.指标, row.数量, row.数值, row.说明 }.Select(Csv));
                csv.Append("\r\n");
            }
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"科研统计-{mode}-{DateTime.Now:yyyyMMddHHmmss}.csv");
        }

        private async Task<bool> 是分析管理角色() => await HasAnyRoleAsync("系统管理员", "科研管理员");

        private async Task<bool> 有范围权限(int? flowTemplateId, int? projectId, int? personId)
        {
            if (await 是分析管理角色()) return true;
            if (personId.HasValue && personId.Value != CurrentUser.编号) return false;
            if (flowTemplateId.HasValue && projectId.HasValue) return await CanAccessProjectAsync(flowTemplateId.Value, projectId.Value);
            return true;
        }

        private static string 空值(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        private static bool 事件类型有效(string value) => value?.Trim() is "延期原因" or "方法学服务缺口" or "入组困难" or "数据服务等待" or "资源使用" or "其他";
        private static bool 严重程度有效(string value) => value?.Trim() is "低" or "中" or "高" or "紧急";
        private static bool 瓶颈状态有效(string value) => value?.Trim() is "开放" or "处理中" or "已解决" or "已关闭";
        private static bool 资源状态有效(string value) => value?.Trim() is "申请中" or "已排期" or "使用中" or "已完成" or "已取消";
    }
}
