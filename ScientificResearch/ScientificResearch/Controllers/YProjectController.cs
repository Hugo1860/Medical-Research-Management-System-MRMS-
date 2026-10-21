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
using Npgsql;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Data;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ScientificResearch.Controllers
{
    public class YProjectController : BaseController
    {
        public enum 纵向项目分类
        {
            科研项目 = 1, 重点学科 = 2, 人才培养 = 3
        }

        #region 纵向项目申报
        private async Task<bool> 是申报计划管理角色()
        {
            return await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办");
        }

        /// <summary>查询纵向项目申报计划。普通人员只看到已发布计划，管理人员可查看全部状态。</summary>
        [HttpGet]
        async public Task<object> 分页获取纵向项目申报计划(Paging paging)
        {
            paging = paging ?? new Paging();
            var size = Math.Max(1, Math.Min(paging.Size, 100));
            var index = Math.Max(1, paging.Index);
            var admin = await 是申报计划管理角色();
            var filter = admin ? "" : "WHERE p.\"状态\"='发布'";
            var total = await Db.QuerySingleAsync<int>($"SELECT count(*)::integer FROM \"纵向项目申报计划\" p {filter};");
            var rows = await Db.QueryAsync<object>($@"
SELECT p.""编号"",p.""计划年度"",p.""申报类别"",p.""计划名称"",p.""发布开始日期"",p.""截止日期"",
       p.""计划说明"",p.""状态"",p.""创建时间"",p.""更新时间"",
       (p.""状态""='发布' AND CURRENT_DATE BETWEEN p.""发布开始日期"" AND p.""截止日期"") AS ""是否可申报""
  FROM ""纵向项目申报计划"" p {filter}
 ORDER BY p.""计划年度"" DESC,p.""发布开始日期"" DESC,p.""编号"" DESC
 LIMIT @Size OFFSET @Offset;", new { Size = size, Offset = (index - 1) * size });
            return new { Total = total, List = rows };
        }

        /// <summary>保存申报计划草稿或关闭记录；发布动作通过独立接口完成。</summary>
        [HttpPost]
        async public Task<object> 保存纵向项目申报计划([FromBody]纵向项目申报计划保存 model)
        {
            if (!await 是申报计划管理角色()) return StatusCode(403, new { error = "无权维护纵向项目申报计划" });
            if (model == null || model.计划年度 < 2000 || model.计划年度 > 2200) throw new ArgumentException("计划年度无效");
            if (model.申报类别 < 1 || model.申报类别 > 3) throw new ArgumentException("申报类别无效");
            if (string.IsNullOrWhiteSpace(model.计划名称) || model.计划名称.Trim().Length > 200) throw new ArgumentException("计划名称不能为空且不能超过200个字符");
            if (model.截止日期.Date < model.发布开始日期.Date) throw new ArgumentException("截止日期不能早于发布开始日期");
            if ((model.计划说明?.Length ?? 0) > 2000) throw new ArgumentException("计划说明不能超过2000个字符");
            var status = string.IsNullOrWhiteSpace(model.状态) ? "草稿" : model.状态.Trim();
            if (status is not ("草稿" or "发布" or "关闭")) throw new ArgumentException("计划状态无效");
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""纵向项目申报计划"" (""编号"",""计划年度"",""申报类别"",""计划名称"",""发布开始日期"",""截止日期"",""计划说明"",""状态"",""创建人编号"")
VALUES (COALESCE(@编号, nextval(pg_get_serial_sequence('纵向项目申报计划','编号'))),@计划年度,@申报类别,@计划名称,CAST(@发布开始日期 AS date),CAST(@截止日期 AS date),@计划说明,@状态,@创建人编号)
ON CONFLICT (""编号"") DO UPDATE SET ""计划年度""=EXCLUDED.""计划年度"",""申报类别""=EXCLUDED.""申报类别"",""计划名称""=EXCLUDED.""计划名称"",""发布开始日期""=EXCLUDED.""发布开始日期"",""截止日期""=EXCLUDED.""截止日期"",""计划说明""=EXCLUDED.""计划说明"",""状态""=EXCLUDED.""状态"",""更新时间""=CURRENT_TIMESTAMP
RETURNING ""编号"";", new
            {
                model.编号, model.计划年度, model.申报类别, 计划名称 = model.计划名称.Trim(),
                发布开始日期 = model.发布开始日期.Date, 截止日期 = model.截止日期.Date,
                计划说明 = string.IsNullOrWhiteSpace(model.计划说明) ? null : model.计划说明.Trim(), 状态 = status,
                创建人编号 = CurrentUser.编号
            });
            return new { 编号 = id, 状态 = status };
        }

        /// <summary>单独发布或关闭申报计划，便于审计和避免误发布草稿。</summary>
        [HttpPost]
        async public Task<object> 发布纵向项目申报计划(long 编号, string 状态 = "发布")
        {
            if (!await 是申报计划管理角色()) return StatusCode(403, new { error = "无权发布纵向项目申报计划" });
            if (编号 <= 0 || 状态 is not ("发布" or "关闭")) throw new ArgumentException("编号或状态无效");
            var updated = await Db.ExecuteAsync("UPDATE \"纵向项目申报计划\" SET \"状态\"=@状态,\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@编号;", new { 编号, 状态 });
            if (updated == 0) return NotFound(new { error = "申报计划不存在" });
            return new { 编号, 状态 };
        }

        /// <summary>院级项目批次列表。普通人员只看到当前发布窗口，管理人员可查看全部状态。</summary>
        [HttpGet]
        async public Task<object> 分页获取院级项目申报批次(Paging paging, int? 计划年度 = null, string 状态 = null)
        {
            paging ??= new Paging();
            var size = Math.Max(1, Math.Min(paging.Size, 100));
            var index = Math.Max(1, paging.Index);
            var admin = await 是申报计划管理角色();
            var where = new List<string>();
            if (!admin) where.Add("b.\"状态\"='发布' AND CURRENT_DATE BETWEEN b.\"发布开始日期\" AND b.\"截止日期\"");
            if (计划年度.HasValue) where.Add("b.\"计划年度\"=@计划年度");
            if (admin && !string.IsNullOrWhiteSpace(状态)) where.Add("b.\"状态\"=@状态");
            var predicate = where.Count == 0 ? "" : "WHERE " + string.Join(" AND ", where);
            var total = await Db.QuerySingleAsync<int>($"SELECT count(*)::integer FROM \"院级项目申报批次\" b {predicate};", new { 计划年度, 状态 });
            var rows = await Db.QueryAsync<object>($@"
SELECT b.""编号"",b.""计划年度"",b.""批次名称"",b.""项目类别"",b.""发布开始日期"",b.""截止日期"",
       b.""个人申报限额"",b.""批次总限额"",b.""资格规则"",b.""评审方案"",b.""评审开始日期"",b.""评审截止日期"",
       b.""状态"",b.""批次说明"",b.""创建时间"",b.""更新时间"",
       (b.""状态""='发布' AND CURRENT_DATE BETWEEN b.""发布开始日期"" AND b.""截止日期"") AS ""是否可申报"",
       (SELECT count(*)::integer FROM ""纵向项目申报"" y WHERE y.""院级批次编号""=b.""编号"") AS ""已申报数""
  FROM ""院级项目申报批次"" b {predicate}
 ORDER BY b.""计划年度"" DESC,b.""发布开始日期"" DESC,b.""编号"" DESC
 LIMIT @Size OFFSET @Offset;", new { Size = size, Offset = (index - 1) * size, 计划年度, 状态 });
            return new { Total = total, List = rows };
        }

        /// <summary>保存院级项目批次，资格规则和评审方案均为可审计 JSON 配置。</summary>
        [HttpPost]
        async public Task<object> 保存院级项目申报批次([FromBody]院级项目申报批次保存 model)
        {
            if (!await 是申报计划管理角色()) return StatusCode(403, new { error = "无权维护院级项目申报批次" });
            if (model == null || model.计划年度 < 2000 || model.计划年度 > 2200) throw new ArgumentException("计划年度无效");
            if (string.IsNullOrWhiteSpace(model.批次名称) || model.批次名称.Trim().Length > 200) throw new ArgumentException("批次名称不能为空且不能超过200个字符");
            if (model.截止日期.Date < model.发布开始日期.Date) throw new ArgumentException("截止日期不能早于发布开始日期");
            if (model.评审开始日期.HasValue && model.评审截止日期.HasValue && model.评审截止日期.Value.Date < model.评审开始日期.Value.Date) throw new ArgumentException("评审截止日期不能早于评审开始日期");
            if (model.个人申报限额 <= 0 || model.个人申报限额 > 1000) throw new ArgumentException("个人申报限额必须在1至1000之间");
            if (model.批次总限额.HasValue && (model.批次总限额 <= 0 || model.批次总限额 > 100000)) throw new ArgumentException("批次总限额无效");
            var status = string.IsNullOrWhiteSpace(model.状态) ? "草稿" : model.状态.Trim();
            if (status is not ("草稿" or "发布" or "关闭")) throw new ArgumentException("批次状态无效");
            JObject qualification; JObject reviewPlan;
            try { qualification = string.IsNullOrWhiteSpace(model.资格规则) ? new JObject() : JObject.Parse(model.资格规则); }
            catch { throw new ArgumentException("资格规则必须是合法 JSON 对象"); }
            try { reviewPlan = string.IsNullOrWhiteSpace(model.评审方案) ? new JObject() : JObject.Parse(model.评审方案); }
            catch { throw new ArgumentException("评审方案必须是合法 JSON 对象"); }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""院级项目申报批次"" (""编号"",""计划年度"",""批次名称"",""项目类别"",""发布开始日期"",""截止日期"",""个人申报限额"",""批次总限额"",""资格规则"",""评审方案"",""评审开始日期"",""评审截止日期"",""状态"",""批次说明"",""创建人编号"")
VALUES (COALESCE(@编号,nextval(pg_get_serial_sequence('院级项目申报批次','编号'))),@计划年度,@批次名称,@项目类别,CAST(@发布开始日期 AS date),CAST(@截止日期 AS date),@个人申报限额,@批次总限额,CAST(@资格规则 AS jsonb),CAST(@评审方案 AS jsonb),CAST(@评审开始日期 AS date),CAST(@评审截止日期 AS date),@状态,@批次说明,@创建人编号)
ON CONFLICT (""编号"") DO UPDATE SET ""计划年度""=EXCLUDED.""计划年度"",""批次名称""=EXCLUDED.""批次名称"",""项目类别""=EXCLUDED.""项目类别"",""发布开始日期""=EXCLUDED.""发布开始日期"",""截止日期""=EXCLUDED.""截止日期"",""个人申报限额""=EXCLUDED.""个人申报限额"",""批次总限额""=EXCLUDED.""批次总限额"",""资格规则""=EXCLUDED.""资格规则"",""评审方案""=EXCLUDED.""评审方案"",""评审开始日期""=EXCLUDED.""评审开始日期"",""评审截止日期""=EXCLUDED.""评审截止日期"",""状态""=EXCLUDED.""状态"",""批次说明""=EXCLUDED.""批次说明"",""更新时间""=CURRENT_TIMESTAMP
RETURNING ""编号"";", new { model.编号, model.计划年度, 批次名称 = model.批次名称.Trim(), 项目类别 = string.IsNullOrWhiteSpace(model.项目类别) ? "院级科研项目" : model.项目类别.Trim(), model.发布开始日期, model.截止日期, model.个人申报限额, model.批次总限额, 资格规则 = qualification.ToString(Formatting.None), 评审方案 = reviewPlan.ToString(Formatting.None), 评审开始日期 = model.评审开始日期?.Date, 评审截止日期 = model.评审截止日期?.Date, 状态 = status, 批次说明 = string.IsNullOrWhiteSpace(model.批次说明) ? null : model.批次说明.Trim(), 创建人编号 = CurrentUser.编号 });
            return new { 编号 = id, 状态 = status };
        }

        [HttpPost]
        async public Task<object> 发布院级项目申报批次(long 编号, string 状态 = "发布")
        {
            if (!await 是申报计划管理角色()) return StatusCode(403, new { error = "无权发布院级项目申报批次" });
            if (编号 <= 0 || 状态 is not ("发布" or "关闭")) throw new ArgumentException("编号或状态无效");
            var updated = await Db.ExecuteAsync("UPDATE \"院级项目申报批次\" SET \"状态\"=@状态,\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@编号;", new { 编号, 状态 });
            if (updated == 0) return NotFound(new { error = "院级项目申报批次不存在" });
            return new { 编号, 状态 };
        }

        /// <summary>读取单个院级批次的资格和评审配置，供评审工作台按同一规则渲染。</summary>
        [HttpGet]
        async public Task<object> 获取院级项目评审方案(long 批次编号)
        {
            if (批次编号 <= 0) throw new ArgumentException("批次编号无效");
            var admin = await 是申报计划管理角色();
            var row = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"",""计划年度"",""批次名称"",""项目类别"",""发布开始日期"",""截止日期"",""个人申报限额"",""批次总限额"",
       ""资格规则"",""评审方案"",""评审开始日期"",""评审截止日期"",""状态"",""批次说明""
  FROM ""院级项目申报批次""
 WHERE ""编号""=@批次编号 AND (@管理角色 OR (""状态""='发布' AND CURRENT_DATE BETWEEN ""发布开始日期"" AND ""截止日期""));", new { 批次编号, 管理角色 = admin });
            if (row == null) return NotFound(new { error = "院级项目申报批次不存在或当前不可见" });
            return row;
        }

        public int 纵向项目申报流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:纵向项目申报");
            }
        }

        /// <summary>
        /// 新增科研项目申报
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 新增科研项目申报([FromBody]纵向项目申报增改 model)
        {
            if (model?.基本资料 == null) throw new ArgumentException("缺少项目申报基本资料");
            model.基本资料.分类 = (int)纵向项目分类.科研项目;
            await 发起一个纵向项目申报流程(model);
        }

        [HttpPost]
        async public Task 新增重点学科申报([FromBody]纵向项目申报增改 model)
        {
            if (model?.基本资料 == null) throw new ArgumentException("缺少项目申报基本资料");
            model.基本资料.分类 = (int)纵向项目分类.重点学科;
            await 发起一个纵向项目申报流程(model);
        }

        [HttpPost]
        async public Task 新增人才培养申报([FromBody]纵向项目申报增改 model)
        {
            if (model?.基本资料 == null) throw new ArgumentException("缺少项目申报基本资料");
            model.基本资料.分类 = (int)纵向项目分类.人才培养;
            await 发起一个纵向项目申报流程(model);
        }

        /// <summary>
        /// 申报提交前的资格和材料预检查。该接口只读，不创建项目或流程。
        /// </summary>
        [HttpPost]
        async public Task<项目申报预检查结果> 预检查纵向项目申报([FromBody]纵向项目申报增改 model)
        {
            return await 获取纵向项目申报预检查(model, model?.IsHold == true);
        }

        private async Task 发起一个纵向项目申报流程(纵向项目申报增改 model)
        {
            model.基本资料.申报计划编号 = model.申报计划编号 ?? model.基本资料.申报计划编号;
            model.基本资料.院级批次编号 = model.院级批次编号 ?? model.基本资料.院级批次编号;
            var preflight = await 获取纵向项目申报预检查(model, model?.IsHold == true);
            if (!preflight.通过)
            {
                var errors = string.Join("；", preflight.检查项.Where(x => x.阻断提交 && !x.通过).Select(x => x.提示));
                throw new ArgumentException($"项目申报预检查未通过：{errors}");
            }

            model.基本资料.申报人编号 = CurrentUser.编号;
            await MyWorkFlowBusiness.发起流程(
                            纵向项目申报流程模板编号,
                            new sp_纵向项目申报_增改()
                            {
                                流程模板编号 = 纵向项目申报流程模板编号,
                                tt = model.基本资料.ToDataTable(),
                                tt_项目参与人 = (model.参与人列表 ?? Enumerable.Empty<项目参与人>()).ToDataTable()
                            },
                            CurrentUser.编号,
                            model.IsHold);
        }

        private async Task<项目申报预检查结果> 获取纵向项目申报预检查(纵向项目申报增改 model, bool isHold)
        {
            var checks = new List<项目申报预检查项>();
            var basic = model?.基本资料;
            var participants = (model?.参与人列表 ?? Enumerable.Empty<项目参与人>()).ToList();
            var nowYear = DateTime.Today.Year;
            var planId = model?.申报计划编号 ?? basic?.申报计划编号;
            if (basic != null) basic.申报计划编号 = planId;
            var internalBatchId = model?.院级批次编号 ?? basic?.院级批次编号;
            if (basic != null) basic.院级批次编号 = internalBatchId;
            Add("BATCH_REFERENCE", "申报批次互斥", !(planId.HasValue && internalBatchId.HasValue), true,
                planId.HasValue && internalBatchId.HasValue ? "纵向申报计划和院级项目批次不能同时关联" : "申报批次关联有效");

            void Add(string code, string name, bool passed, bool blocking, string message)
            {
                checks.Add(new 项目申报预检查项
                {
                    代码 = code,
                    名称 = name,
                    通过 = passed,
                    阻断提交 = blocking,
                    提示 = message
                });
            }

            Add("BASIC", "基本资料", basic != null,
                true, basic == null ? "缺少基本资料" : "基本资料已提供");
            if (basic == null)
            {
                return new 项目申报预检查结果
                {
                    通过 = false,
                    材料完整 = false,
                    检查项 = checks
                };
            }

            var name = basic.项目名称?.Trim();
            Add("NAME", "项目名称", !string.IsNullOrWhiteSpace(name) && name.Length <= 100,
                true, string.IsNullOrWhiteSpace(name) ? "请填写项目名称" : name.Length > 100 ? "项目名称不能超过100个字符" : "项目名称格式正确");

            var duplicated = false;
            if (!string.IsNullOrWhiteSpace(name))
            {
                duplicated = await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (
    SELECT 1 FROM ""纵向项目申报""
     WHERE lower(btrim(""项目名称"")) = lower(btrim(@项目名称))
       AND ""编号"" <> COALESCE(@编号, 0)
);", new { 项目名称 = name, 编号 = basic.编号 });
            }
            Add("UNIQUE_NAME", "项目名称唯一性", !duplicated, true,
                duplicated ? "已有同名纵向项目申报，请修改项目名称或打开原申报记录" : "未发现同名申报");

            Add("YEAR", "申报年度", basic.申报年度 >= 2000 && basic.申报年度 <= nowYear + 1,
                true, basic.申报年度 < 2000 ? "申报年度无效" : basic.申报年度 > nowYear + 1 ? "申报年度不能超过下一年度" : "申报年度有效");

            var applicantId = basic.申报人编号 > 0 ? basic.申报人编号 : CurrentUser.编号;
            var applicantActive = await Db.QuerySingleAsync<bool>(
                "SELECT EXISTS (SELECT 1 FROM \"人员\" WHERE \"编号\"=@人员编号 AND \"是否启用\"=TRUE);",
                new { 人员编号 = applicantId });
            Add("APPLICANT_ACTIVE", "申报人状态", applicantActive, true,
                applicantActive ? "申报人为启用人员" : "申报人不存在或已停用");
            var applicantScope = applicantId == CurrentUser.编号 || await 是申报计划管理角色();
            Add("APPLICANT_SCOPE", "申报人操作范围", applicantScope, true,
                applicantScope ? "当前账号可以代表该申报人提交" : "当前账号不能代表指定申报人提交");

            if (planId.HasValue && planId.Value > 0)
            {
                var plan = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""申报类别"", ""状态"", ""发布开始日期"", ""截止日期""
  FROM ""纵向项目申报计划"" WHERE ""编号""=@编号;", new { 编号 = planId.Value });
                var planReady = plan != null && (string)plan.状态 == "发布"
                    && DateTime.Today >= ((DateTime)plan.发布开始日期).Date
                    && DateTime.Today <= ((DateTime)plan.截止日期).Date
                    && (int)plan.申报类别 == basic.分类;
                Add("PLAN_WINDOW", "申报计划窗口", planReady, !isHold,
                    plan == null ? "指定的申报计划不存在" : !planReady ? "申报计划未发布、已过期或申报类别不匹配" : "申报计划在有效发布窗口内");
            }
            else
            {
                Add("PLAN_OPTIONAL", "申报计划关联", true, false, "未关联具体申报计划；建议由科研管理员补充计划编号");
            }

            if (internalBatchId.HasValue && internalBatchId.Value > 0)
            {
                var batch = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""计划年度"",""发布开始日期"",""截止日期"",""个人申报限额"",""批次总限额"",""资格规则"",""状态""
  FROM ""院级项目申报批次"" WHERE ""编号""=@编号;", new { 编号 = internalBatchId.Value });
                var batchReady = batch != null && (string)batch.状态 == "发布"
                    && DateTime.Today >= ((DateTime)batch.发布开始日期).Date && DateTime.Today <= ((DateTime)batch.截止日期).Date
                    && (int)batch.计划年度 == basic.申报年度;
                Add("INTERNAL_BATCH_WINDOW", "院级批次窗口", batchReady, !isHold,
                    batch == null ? "指定的院级项目批次不存在" : !batchReady ? "院级批次未发布、已过期或年度不匹配" : "院级项目批次在有效发布窗口内");
                if (batch != null)
                {
                    var rawQualification = Convert.ToString((object)batch.资格规则);
                    var qualification = batch.资格规则 is JObject configuredRules
                        ? configuredRules
                        : JObject.Parse(string.IsNullOrWhiteSpace(rawQualification) ? "{}" : rawQualification);
                    var applicantApplications = await Db.QuerySingleAsync<int>(@"
SELECT count(*)::integer FROM ""纵向项目申报"" WHERE ""院级批次编号""=@批次编号 AND ""申报人编号""=@申报人编号
  AND ""编号""<>COALESCE(@项目编号,0) AND COALESCE(""是否立项"",FALSE)=FALSE;", new { 批次编号 = internalBatchId.Value, 申报人编号 = applicantId, 项目编号 = basic.编号 });
                    var personQuota = applicantApplications < (int)batch.个人申报限额;
                    Add("INTERNAL_BATCH_PERSON_QUOTA", "院级批次个人限额", personQuota, true,
                        personQuota ? $"本批次已有 {applicantApplications} 项未立项申报，未超过 {(int)batch.个人申报限额} 项限制" : "本批次个人申报已达到限额");
                    if (batch.批次总限额 != null)
                    {
                        var totalApplications = await Db.QuerySingleAsync<int>(@"
SELECT count(*)::integer FROM ""纵向项目申报"" WHERE ""院级批次编号""=@批次编号 AND ""编号""<>COALESCE(@项目编号,0);", new { 批次编号 = internalBatchId.Value, 项目编号 = basic.编号 });
                        var totalQuota = totalApplications < (int)batch.批次总限额;
                        Add("INTERNAL_BATCH_TOTAL_QUOTA", "院级批次总限额", totalQuota, true,
                            totalQuota ? $"批次已有 {totalApplications} 项申报，未超过 {(int)batch.批次总限额} 项限制" : "院级批次申报总数已达到限额");
                    }
                    var allowedDepartments = qualification["允许部门编号"] as JArray;
                    if (allowedDepartments != null && allowedDepartments.Count > 0)
                    {
                        var allowed = allowedDepartments.Values<int>().Contains(CurrentUser.部门编号);
                        Add("INTERNAL_BATCH_ELIGIBILITY", "院级批次资格限制", allowed, true,
                            allowed ? "当前人员所在科室符合批次资格" : "当前人员所在科室不在批次允许范围内");
                    }
                    var minAmount = qualification["最低申请经费"]?.Value<decimal?>();
                    var maxAmount = qualification["最高申请经费"]?.Value<decimal?>();
                    if (minAmount.HasValue) Add("INTERNAL_BATCH_MIN_BUDGET", "最低申请经费", basic.申请经费 >= minAmount.Value, true, basic.申请经费 >= minAmount.Value ? "申请经费达到批次下限" : $"申请经费不能低于 {minAmount.Value:0.##}");
                    if (maxAmount.HasValue) Add("INTERNAL_BATCH_MAX_BUDGET", "最高申请经费", basic.申请经费 <= maxAmount.Value, true, basic.申请经费 <= maxAmount.Value ? "申请经费未超过批次上限" : $"申请经费不能超过 {maxAmount.Value:0.##}");
                }
            }

            var existingApplications = await Db.QuerySingleAsync<int>(@"
SELECT count(*)::integer FROM ""纵向项目申报""
 WHERE ""申报人编号""=@申报人编号 AND ""申报年度""=@申报年度
   AND ""编号"" <> COALESCE(@编号,0)
   AND COALESCE(""是否立项"",FALSE)=FALSE;", new
            { 申报人编号 = applicantId, 申报年度 = basic.申报年度, 编号 = basic.编号 });
            var quotaReady = existingApplications < 3;
            Add("APPLICATION_QUOTA", "年度申报资格", quotaReady, true,
                quotaReady ? $"本年度已有 {existingApplications} 项未立项申报，未超过 3 项限制" : "本年度未立项申报已达到 3 项，请先完成或撤回既有申报");
            Add("BUDGET", "申请经费", basic.申请经费 >= 0,
                true, basic.申请经费 < 0 ? "申请经费不能为负数" : "申请经费有效");
            Add("DATE", "计划日期", basic.计划开始日期 <= basic.计划完成日期,
                true, basic.计划开始日期 > basic.计划完成日期 ? "计划开始日期不能晚于完成日期" : "计划日期有效");

            var isExternal = basic.是否委托外单位进行实验或实验分析 == true;
            Add("EXTERNAL_CONTENT", "外委实验说明", !isExternal || !string.IsNullOrWhiteSpace(basic.实验委托检测内容),
                true, isExternal && string.IsNullOrWhiteSpace(basic.实验委托检测内容) ? "已选择外委实验，请填写实验委托检测内容" : "外委实验说明完整");

            var needsEthics = basic.是否需要伦理评审 == true;
            var hasApproval = basic.是否需要伦理批文 == true;
            Add("ETHICS_CONSISTENCY", "伦理选项一致性", !hasApproval || needsEthics,
                true, hasApproval && !needsEthics ? "未选择伦理评审时不能填写需要伦理批文" : "伦理选项一致");
            var ethicsDocumentsReady = !hasApproval ||
                (!string.IsNullOrWhiteSpace(basic.伦理批文编号) && !string.IsNullOrWhiteSpace(basic.伦理批文路径));
            Add("ETHICS_DOCUMENT", "伦理批文材料", ethicsDocumentsReady,
                !isHold, hasApproval && !ethicsDocumentsReady ? "需要伦理批文时必须提供批文编号和文件" : "伦理批文材料完整");

            var applicationDocumentReady = !string.IsNullOrWhiteSpace(basic.项目申报书路径);
            Add("APPLICATION_DOCUMENT", "项目申报书", applicationDocumentReady,
                !isHold, applicationDocumentReady ? "项目申报书已上传" : "正式提交前必须上传项目申报书");
            var summaryReady = !string.IsNullOrWhiteSpace(basic.项目摘要);
            Add("SUMMARY", "项目摘要", summaryReady,
                !isHold, summaryReady ? "项目摘要已填写" : "正式提交前必须填写项目摘要");

            var participantIds = participants.Select(x => x.参与人编号).Where(x => x > 0).ToList();
            var duplicateParticipant = participantIds.Count != participantIds.Distinct().Count();
            Add("PARTICIPANTS", "参与人列表", !duplicateParticipant,
                true, duplicateParticipant ? "参与人不能重复" : "参与人列表无重复");
            var workloadValues = participants.Where(x => x.工作量占比.HasValue).Select(x => x.工作量占比.Value).ToList();
            var workloadValid = workloadValues.All(x => x >= 0 && x <= 100) && workloadValues.Sum() <= 100;
            Add("WORKLOAD", "参与人工作量", workloadValid,
                true, !workloadValid ? "工作量占比必须在0至100之间且合计不能超过100" : "参与人工作量有效");

            var participantCheckIds = participantIds.Distinct().ToArray();
            if (participantCheckIds.Length > 0)
            {
                var enabledParticipantCount = await Db.QuerySingleAsync<int>(@"
SELECT count(*)::integer FROM ""人员""
 WHERE ""编号"" = ANY(@人员编号) AND ""是否启用"" = TRUE;", new { 人员编号 = participantCheckIds });
                Add("PARTICIPANT_STATUS", "参与人状态", enabledParticipantCount == participantCheckIds.Length,
                    true, enabledParticipantCount == participantCheckIds.Length ? "参与人均为启用人员" : "参与人列表包含不存在或已停用人员");
            }

            var result = new 项目申报预检查结果
            {
                通过 = checks.All(x => !x.阻断提交 || x.通过),
                材料完整 = checks.Where(x => x.代码 is "APPLICATION_DOCUMENT" or "SUMMARY" or "ETHICS_DOCUMENT")
                    .All(x => x.通过),
                申报计划编号 = planId,
                检查项 = checks
            };
            result.记录编号 = await 记录项目申报预检查(model, result, isHold);
            return result;
        }

        /// <summary>保存预检结果快照；迁移尚未应用时仅跳过留痕，不影响只读预检。</summary>
        private async Task<long?> 记录项目申报预检查(纵向项目申报增改 model, 项目申报预检查结果 result, bool isHold)
        {
            try
            {
                return await Db.QuerySingleAsync<long>(@"
INSERT INTO ""纵向项目申报预检查记录""
 (""纵向项目申报编号"",""申报计划编号"",""申请人编号"",""是否暂存"",""是否通过"",""材料完整"",""检查结果"")
VALUES (@项目编号,@申报计划编号,@申请人编号,@是否暂存,@是否通过,@材料完整,CAST(@检查结果 AS jsonb))
RETURNING ""编号"";", new
                {
                    项目编号 = model?.基本资料?.编号 > 0 ? model.基本资料.编号 : (int?)null,
                    申报计划编号 = result.申报计划编号,
                    申请人编号 = CurrentUser.编号,
                    是否暂存 = isHold,
                    是否通过 = result.通过,
                    材料完整 = result.材料完整,
                    检查结果 = JsonConvert.SerializeObject(result.检查项 ?? Enumerable.Empty<项目申报预检查项>())
                });
            }
            catch
            {
                return null;
            }
        }

        [HttpPost]
        async public Task 完善纵向项目申报资料([FromBody]完成步骤<纵向项目申报增改> model)
        {
            //await 内部完善纵向项目申报资料(model);
            await MyWorkFlowBusiness.完成步骤(
                model.toSimple(), CurrentUser.编号,
                new sp_纵向项目申报_增改()
                {
                    流程模板编号 = 纵向项目申报流程模板编号,
                    tt = model.附加数据.基本资料.ToDataTable(),
                    tt_项目参与人 = model.附加数据.参与人列表.ToDataTable()
                },
                model.附加数据.IsHold
                );
        }

        /// <summary>
        /// 附加数据中的专家评分,只需要传其中的评分项和评分
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 专家评审纵向项目申报(完成步骤<IEnumerable<专家评分>> model)
        {
            await MyWorkFlowBusiness.专家评审(model, CurrentUser.编号, 纵向项目申报流程模板编号);
        }

        [HttpPost]
        async public Task<object> 上传伦理批文()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/纵向项目/伦理批文",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpPost]
        async public Task<object> 上传项目申报书()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/纵向项目/项目申报书",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpGet]
        async public Task<object> 分页获取科研项目申报(Paging paging, 纵向项目申报Filter filter)
        {
            filter.分类 = (int)纵向项目分类.科研项目;
            return await 分页获取纵向项目申报(paging, filter);
        }

        [HttpGet]
        async public Task<object> 分页获取人才培养申报(Paging paging, 纵向项目申报Filter filter)
        {
            filter.分类 = (int)纵向项目分类.人才培养;
            return await 分页获取纵向项目申报(paging, filter);
        }

        [HttpGet]
        async public Task<object> 分页获取重点学科申报(Paging paging, 纵向项目申报Filter filter)
        {
            filter.分类 = (int)纵向项目分类.重点学科;
            return await 分页获取纵向项目申报(paging, filter);
        }

        private async Task<object> 分页获取纵向项目申报(Paging paging, 纵向项目申报Filter filter) =>
            await Db.GetPagingListSpAsync<object, 纵向项目申报Filter>(
                paging,
                filter,
                $"tfn_纵向项目申报({CurrentUser.编号},{纵向项目申报流程模板编号})");

        [HttpGet]
        async public Task<object> 获取纵向项目申报详情(int 纵向项目申报编号)
        {
            if (!await CanAccessProjectAsync(纵向项目申报流程模板编号, 纵向项目申报编号))
            {
                return ProjectAccessDenied();
            }
            //var r1 = await Db.GetListSpAsync<v2_纵向项目申报_详情, 纵向项目申报Filter>(new 纵向项目申报Filter() { 编号 = 纵向项目申报编号 });
            var r1 = await Db.GetModelByIdSpAsync<v2_纵向项目申报_详情>(纵向项目申报编号);
            var r2 = await Db.GetListSpAsync<object>($"tfn_项目参与人({纵向项目申报流程模板编号},{纵向项目申报编号})");
            var r3 = await Db.GetListSpAsync<object>($"tfn_专家评分({纵向项目申报流程模板编号},{纵向项目申报编号})");
            var r4 = await MyWorkFlowBusiness.获取某流程的步骤(纵向项目申报流程模板编号, 纵向项目申报编号);
            return new
            {
                申报基本信息 = r1,
                参与人信息 = r2,
                专家评审记录 = r3,
                审核记录 = r4
            };
        }

        /// <summary>汇总申报版本、评审意见、评分和资助结果。</summary>
        [HttpGet]
        async public Task<object> 获取纵向项目申报评审汇总(int 纵向项目申报编号)
        {
            if (!await CanAccessProjectAsync(纵向项目申报流程模板编号, 纵向项目申报编号))
                return ProjectAccessDenied();
            var 基本信息 = await Db.QuerySingleOrDefaultAsync<object>(@"
SELECT y.""编号"", y.""项目名称"", y.""申报人编号"", y.""申报年度"", y.""是否立项"",
       f.""状态"" AS ""流程状态"", f.""编号"" AS ""流程编号""
  FROM ""纵向项目申报"" y
  LEFT JOIN ""流程"" f ON f.""流程模板编号"" = @模板编号 AND f.""项目编号"" = y.""编号""
 WHERE y.""编号"" = @项目编号;",
                new { 模板编号 = 纵向项目申报流程模板编号, 项目编号 = 纵向项目申报编号 });
            if (基本信息 == null) return NotFound(new { error = "项目申报不存在" });
            var 可查看内部意见 = await HasAnyRoleAsync("专家", "科研管理员", "系统管理员");
            var 版本 = await Db.QueryAsync<object>(@"
SELECT ""编号"", ""版本号"", ""变更类型"", ""变更人编号"", ""建立时间"", ""快照""::text AS ""快照""
  FROM ""纵向项目申报版本""
 WHERE ""纵向项目申报编号"" = @项目编号
 ORDER BY ""版本号"" DESC;", new { 项目编号 = 纵向项目申报编号 });
            var 意见 = await Db.QueryAsync<object>(@"
SELECT i.""编号"", i.""步骤编号"", i.""评审人编号"", p.""姓名"" AS ""评审人姓名"",
       i.""意见类型"", i.""意见内容"", i.""是否公开给申请人"", i.""建立时间""
  FROM ""项目评审意见"" i
  LEFT JOIN ""人员"" p ON p.""编号"" = i.""评审人编号""
 WHERE i.""流程模板编号"" = @模板编号 AND i.""项目编号"" = @项目编号
   AND (@可查看内部意见 OR i.""是否公开给申请人"")
 ORDER BY i.""建立时间"" DESC, i.""编号"" DESC;",
                new { 模板编号 = 纵向项目申报流程模板编号, 项目编号 = 纵向项目申报编号, 可查看内部意见 });
            var 复盘 = await Db.QueryAsync<object>(@"
SELECT r.""编号"", r.""原因分类"", r.""复盘结论"", r.""改进措施"", r.""负责人编号"",
       r.""建立人编号"", r.""建立时间"", p.""姓名"" AS ""负责人姓名""
  FROM ""未获资助复盘"" r
  LEFT JOIN ""人员"" p ON p.""编号"" = r.""负责人编号""
 WHERE r.""纵向项目申报编号"" = @项目编号
 ORDER BY r.""建立时间"" DESC, r.""编号"" DESC;", new { 项目编号 = 纵向项目申报编号 });
            var 评分 = await Db.GetListSpAsync<object>($"tfn_专家评分({纵向项目申报流程模板编号},{纵向项目申报编号})");
            return new { 基本信息, 版本, 评审意见 = 意见, 专家评分 = 评分, 未获资助复盘 = 复盘, 是否可发起复盘 = await 是否可发起未获资助复盘(纵向项目申报编号) };
        }

        /// <summary>接收伦理系统审批结果，回写项目伦理字段并保留外部批件审计。</summary>
        [HttpPost]
        async public Task<object> 同步伦理审批结果([FromBody]伦理审批同步请求 model)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员")) return StatusCode(403, new { error = "无权执行伦理审批同步" });
            if (model == null || model.外部系统编号 <= 0 || string.IsNullOrWhiteSpace(model.同步批次号)) throw new ArgumentException("外部系统编号和同步批次号不能为空");
            var rows = (model.审批列表 ?? Enumerable.Empty<伦理审批同步记录>()).ToList();
            if (rows.Count > 1000) throw new ArgumentException("单批伦理审批结果不能超过 1000 条");
            if (!await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (SELECT 1 FROM ""外部系统""
 WHERE ""编号""=@外部系统编号 AND ""系统类型""='伦理' AND ""是否启用""=TRUE
   AND NULLIF(BTRIM(""基础地址""),'') IS NOT NULL);", new { model.外部系统编号 }))
                return StatusCode(409, new { error = "伦理外部系统不存在、未启用或未登记基础地址，拒绝执行项目伦理回写" });
            var verticalApplicationTemplate = 纵向项目申报流程模板编号;
            var verticalProjectTemplate = config.GetValue<int>("流程模板编号:纵向项目立项");
            var horizontalTemplate = config.GetValue<int>("流程模板编号:横向项目申请");
            var updated = 0; var skipped = 0; var errors = new List<string>();
            foreach (var item in rows)
            {
                try
                {
                    var status = item.状态?.Trim();
                    if (string.IsNullOrWhiteSpace(item.外部审批编号) || item.外部审批编号.Length > 150 || item.项目编号 <= 0 || status is not ("待审" or "已批准" or "有效" or "已拒绝" or "已过期" or "已撤回")) { errors.Add($"外部审批 {item.外部审批编号} 字段无效"); skipped++; continue; }
                    if ((status is "已批准" or "有效") && (string.IsNullOrWhiteSpace(item.批文编号) || !item.有效期至.HasValue || item.有效期至.Value.Date < DateTime.Today || string.IsNullOrWhiteSpace(item.批文路径)))
                    { errors.Add($"外部审批 {item.外部审批编号} 批准/有效状态必须带批文编号、文件路径和未过期有效期"); skipped++; continue; }
                    var exists = await Db.QuerySingleAsync<bool>(@"
SELECT CASE WHEN @模板=@纵向申报 AND EXISTS (SELECT 1 FROM ""纵向项目申报"" WHERE ""编号""=@项目编号)
         THEN TRUE WHEN @模板=@纵向立项 AND EXISTS (SELECT 1 FROM ""纵向项目立项"" WHERE ""编号""=@项目编号)
         THEN TRUE WHEN @模板=@横向申请 AND EXISTS (SELECT 1 FROM ""横向项目"" WHERE ""编号""=@项目编号)
         THEN TRUE ELSE FALSE END;", new { 模板 = item.流程模板编号, 项目编号 = item.项目编号, 纵向申报 = verticalApplicationTemplate, 纵向立项 = verticalProjectTemplate, 横向申请 = horizontalTemplate });
                    if (!exists) { errors.Add($"外部审批 {item.外部审批编号} 项目不存在"); skipped++; continue; }
                    var hasApproval = status is "已批准" or "有效";
                    if (item.流程模板编号 == verticalApplicationTemplate)
                        await Db.ExecuteAsync("UPDATE \"纵向项目申报\" SET \"是否需要伦理评审\"=TRUE,\"是否需要伦理批文\"=@是否需要伦理批文,\"伦理批文编号\"=@批文编号,\"伦理批文路径\"=@批文路径 WHERE \"编号\"=@项目编号;", new { 是否需要伦理批文 = hasApproval, 批文编号 = string.IsNullOrWhiteSpace(item.批文编号) ? null : item.批文编号.Trim(), 批文路径 = string.IsNullOrWhiteSpace(item.批文路径) ? null : item.批文路径.Trim(), item.项目编号 });
                    else if (item.流程模板编号 == verticalProjectTemplate)
                        await Db.ExecuteAsync("UPDATE \"纵向项目立项\" SET \"是否需要伦理评审\"=TRUE,\"是否需要伦理批文\"=@是否需要伦理批文,\"伦理批文编号\"=@批文编号,\"伦理批文时间\"=@批准时间,\"伦理批文路径\"=@批文路径 WHERE \"编号\"=@项目编号;", new { 是否需要伦理批文 = hasApproval, 批文编号 = string.IsNullOrWhiteSpace(item.批文编号) ? null : item.批文编号.Trim(), item.批准时间, 批文路径 = string.IsNullOrWhiteSpace(item.批文路径) ? null : item.批文路径.Trim(), item.项目编号 });
                    else if (item.流程模板编号 == horizontalTemplate)
                        await Db.ExecuteAsync("UPDATE \"横向项目\" SET \"是否需要伦理评审\"=TRUE WHERE \"编号\"=@项目编号;", new { item.项目编号 });
                    await Db.ExecuteAsync(@"
INSERT INTO ""项目伦理同步记录"" (""外部系统编号"",""流程模板编号"",""项目编号"",""外部审批编号"",""状态"",""批文编号"",""批准时间"",""有效期至"",""批文路径"",""外部更新时间"",""同步批次号"",""备注"")
VALUES (@外部系统编号,@流程模板编号,@项目编号,@外部审批编号,@状态,@批文编号,@批准时间,@有效期至,@批文路径,@外部更新时间,@同步批次号,@备注)
ON CONFLICT (""外部系统编号"",""外部审批编号"") DO UPDATE SET ""流程模板编号""=EXCLUDED.""流程模板编号"",""项目编号""=EXCLUDED.""项目编号"",""状态""=EXCLUDED.""状态"",""批文编号""=EXCLUDED.""批文编号"",""批准时间""=EXCLUDED.""批准时间"",""有效期至""=EXCLUDED.""有效期至"",""批文路径""=EXCLUDED.""批文路径"",""外部更新时间""=EXCLUDED.""外部更新时间"",""最后同步时间""=CURRENT_TIMESTAMP,""同步批次号""=EXCLUDED.""同步批次号"",""备注""=EXCLUDED.""备注"";", new
                    {
                        model.外部系统编号, item.流程模板编号, item.项目编号, 外部审批编号 = item.外部审批编号.Trim(), 状态 = status,
                        批文编号 = string.IsNullOrWhiteSpace(item.批文编号) ? null : item.批文编号.Trim(), item.批准时间, 有效期至 = item.有效期至?.Date,
                        批文路径 = string.IsNullOrWhiteSpace(item.批文路径) ? null : item.批文路径.Trim(), item.外部更新时间, model.同步批次号, 备注 = string.IsNullOrWhiteSpace(item.备注) ? null : item.备注.Trim()
                    });
                    updated++;
                }
                catch (Exception ex) { errors.Add($"外部审批 {item.外部审批编号}: {ex.Message}"); }
            }
            return new { model.同步批次号, 总数 = rows.Count, 已处理 = updated, 跳过 = skipped, 错误数 = errors.Count, 错误 = errors };
        }

        /// <summary>对比两个申报版本的字段值，不修改任何申报数据。</summary>
        [HttpGet]
        async public Task<object> 比较纵向项目申报版本(int 纵向项目申报编号, int 左版本号, int 右版本号)
        {
            if (!await CanAccessProjectAsync(纵向项目申报流程模板编号, 纵向项目申报编号))
                return ProjectAccessDenied();
            if (左版本号 <= 0 || 右版本号 <= 0 || 左版本号 == 右版本号)
                throw new ArgumentException("请选择两个不同的版本");
            var rows = (await Db.QueryAsync<dynamic>(@"
SELECT ""版本号"", ""快照""::text AS ""快照""
  FROM ""纵向项目申报版本""
 WHERE ""纵向项目申报编号"" = @项目编号 AND ""版本号"" = ANY(@版本号);",
                new { 项目编号 = 纵向项目申报编号, 版本号 = new[] { 左版本号, 右版本号 } })).ToList();
            var leftRow = rows.SingleOrDefault(x => (int)x.版本号 == 左版本号);
            var rightRow = rows.SingleOrDefault(x => (int)x.版本号 == 右版本号);
            if (leftRow == null || rightRow == null) throw new ArgumentException("申报版本不存在");
            var left = JObject.Parse((string)leftRow.快照);
            var right = JObject.Parse((string)rightRow.快照);
            var keys = left.Properties().Select(x => x.Name).Union(right.Properties().Select(x => x.Name)).OrderBy(x => x);
            var changes = keys.Where(key => !JToken.DeepEquals(left[key], right[key]))
                .Select(key => new { 字段 = key, 左值 = left[key]?.ToString(Formatting.None), 右值 = right[key]?.ToString(Formatting.None) })
                .ToList();
            return new { 项目编号 = 纵向项目申报编号, 左版本号, 右版本号, 变化数量 = changes.Count, 变化项 = changes };
        }

        [HttpPost]
        async public Task<object> 新增纵向项目评审意见([FromBody]项目评审意见新增 model)
        {
            if (model == null || model.流程模板编号 != 纵向项目申报流程模板编号 || model.项目编号 <= 0)
                throw new ArgumentException("流程模板编号和项目编号无效");
            if (string.IsNullOrWhiteSpace(model.意见类型) || string.IsNullOrWhiteSpace(model.意见内容))
                throw new ArgumentException("意见类型和意见内容不能为空");
            if (model.意见类型.Trim().Length > 30 || model.意见内容.Trim().Length > 4000)
                throw new ArgumentException("评审意见长度超出限制");
            if (!await HasAnyRoleAsync("专家", "科研管理员", "系统管理员"))
                return StatusCode(403, new { error = "无权登记评审意见" });
            if (!await CanAccessProjectAsync(model.流程模板编号, model.项目编号))
                return ProjectAccessDenied();
            if (model.步骤编号.HasValue)
            {
                var conflictCheck = await MyWorkFlowBusiness.检查专家利益冲突(model.步骤编号.Value, CurrentUser.编号);
                if (!conflictCheck.通过) throw new ForbiddenAccessException(conflictCheck.说明);
            }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""项目评审意见"" (""流程模板编号"", ""项目编号"", ""步骤编号"", ""评审人编号"", ""意见类型"", ""意见内容"", ""是否公开给申请人""
) VALUES (@流程模板编号,@项目编号,@步骤编号,@评审人编号,@意见类型,@意见内容,@是否公开给申请人)
RETURNING ""编号"";", new
            {
                model.流程模板编号, model.项目编号, model.步骤编号,
                评审人编号 = CurrentUser.编号,
                意见类型 = model.意见类型.Trim(), 意见内容 = model.意见内容.Trim(), model.是否公开给申请人
            });
            return new { 编号 = id };
        }

        [HttpPost]
        async public Task<object> 新增未获资助复盘([FromBody]未获资助复盘新增 model)
        {
            if (model == null || model.纵向项目申报编号 <= 0)
                throw new ArgumentException("缺少纵向项目申报编号");
            if (string.IsNullOrWhiteSpace(model.原因分类) || string.IsNullOrWhiteSpace(model.复盘结论))
                throw new ArgumentException("原因分类和复盘结论不能为空");
            if (model.原因分类.Trim().Length > 50 || model.复盘结论.Trim().Length > 4000 || (model.改进措施?.Length ?? 0) > 4000)
                throw new ArgumentException("复盘内容长度超出限制");
            if (!await CanAccessProjectAsync(纵向项目申报流程模板编号, model.纵向项目申报编号))
                return ProjectAccessDenied();
            if (!await 是否可发起未获资助复盘(model.纵向项目申报编号))
                throw new ArgumentException("该申报尚未完成且未获资助，暂不能登记复盘");
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""未获资助复盘"" (""纵向项目申报编号"", ""原因分类"", ""复盘结论"", ""改进措施"", ""负责人编号"", ""建立人编号""
) VALUES (@纵向项目申报编号,@原因分类,@复盘结论,@改进措施,@负责人编号,@建立人编号)
RETURNING ""编号"";", new
            {
                model.纵向项目申报编号,
                原因分类 = model.原因分类.Trim(), 复盘结论 = model.复盘结论.Trim(),
                改进措施 = string.IsNullOrWhiteSpace(model.改进措施) ? null : model.改进措施.Trim(),
                model.负责人编号, 建立人编号 = CurrentUser.编号
            });
            return new { 编号 = id };
        }

        private async Task<bool> 是否可发起未获资助复盘(int 申报编号)
        {
            return await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (
    SELECT 1
      FROM ""纵向项目申报"" y
      JOIN ""流程"" f ON f.""流程模板编号"" = @模板编号 AND f.""项目编号"" = y.""编号"" AND f.""状态"" = 1
     WHERE y.""编号"" = @申报编号 AND COALESCE(y.""是否立项"", FALSE) = FALSE
);", new { 模板编号 = 纵向项目申报流程模板编号, 申报编号 });
        }
        #endregion

        #region 纵向项目申报立项
        /// <summary>
        /// 分页获取可立项的科研项目申报
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<object> 分页获取可立项的科研项目申报(Paging paging, 纵向项目申报Filter filter)
        {
            filter.分类 = (int)纵向项目分类.科研项目;
            return await 分页获取可立项的纵向项目申报(paging, filter);
        }

        [HttpGet]
        async public Task<object> 分页获取可立项的重点学科申报(Paging paging, 纵向项目申报Filter filter)
        {
            filter.分类 = (int)纵向项目分类.重点学科;
            return await 分页获取可立项的纵向项目申报(paging, filter);
        }

        [HttpGet]
        async public Task<object> 分页获取可立项的人才培养申报(Paging paging, 纵向项目申报Filter filter)
        {
            filter.分类 = (int)纵向项目分类.人才培养;
            return await 分页获取可立项的纵向项目申报(paging, filter);
        }

        private async Task<object> 分页获取可立项的纵向项目申报(Paging paging, 纵向项目申报Filter filter)
        {
            filter.是否立项 = false;
            return await Db.GetPagingListSpAsync<object, 纵向项目申报Filter>(
                 paging,
                 filter,
                 $"tfn_纵向项目申报({CurrentUser.编号},{纵向项目申报流程模板编号})");
        }

        [HttpGet]
        async public Task<object> 通过申报详情获取可用的纵向项目立项详情(int 纵向项目申报编号)
        {
            if (!await CanAccessProjectAsync(纵向项目申报流程模板编号, 纵向项目申报编号))
            {
                return ProjectAccessDenied();
            }
            //var r1 = await Db.GetListSpAsync<v2_纵向项目申报_详情, 纵向项目申报Filter>(new 纵向项目申报Filter() { 编号 = 纵向项目申报编号 });
            //var r1Result = r1.FirstOrDefault();

            var r1Result = await Db.GetModelByIdSpAsync<v2_纵向项目申报_详情>(纵向项目申报编号);
            var r1Change = new 纵向项目立项()
            {
                编号 = r1Result.编号,       //will used by 新增纵向项目申报立项 and reset to 0
                分类 = r1Result.分类,
                项目中文名称 = r1Result.项目名称,
                负责人编号 = r1Result.申报人编号,
                计划开始日期 = r1Result.计划开始日期,
                计划完成日期 = r1Result.计划完成日期,
                登记日期 = DateTime.Now,
                批准日期 = DateTime.Now,
                批准经费 = r1Result.申请经费,
                项目等级 = r1Result.项目等级,
                学科门类 = r1Result.学科门类,
                学科类型 = r1Result.学科类型,
                项目类型 = r1Result.项目类型,
                项目级别 = r1Result.项目级别,
                项目来源单位 = r1Result.项目来源单位,
                项目类别 = r1Result.项目类别,
                是否需要伦理评审 = r1Result.是否需要伦理评审,
                是否需要伦理批文 = r1Result.是否需要伦理批文,
                伦理批文编号 = r1Result.伦理批文编号,
                伦理批文路径 = r1Result.伦理批文路径
            };

            var r2 = await Db.GetListSpAsync<object>($"tfn_项目参与人({纵向项目申报流程模板编号},{纵向项目申报编号})");

            return new { 立项基本信息 = r1Change, 参与人信息 = r2 };
        }

        [HttpPost]
        async public Task 新增纵向项目申报立项([FromBody]纵向项目立项增改 model)
        {
            var 来源纵向项目申报编号 = model.基本资料.编号;
            model.基本资料.编号 = 0;
            await 发起一个纵向项目立项流程(model);
            await Db.ExecuteSpAsync(
                new sp_纵向项目申报_设为已立项()
                {
                    项目申报编号 = 来源纵向项目申报编号
                });

        }
        #endregion

        #region 纵向项目立项
        public int 纵向项目立项流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:纵向项目立项");
            }
        }

        [HttpPost]
        async public Task 新增科研项目立项([FromBody]纵向项目立项增改 model)
        {
            model.基本资料.分类 = (int)纵向项目分类.科研项目;
            await 发起一个纵向项目立项流程(model);
        }

        [HttpPost]
        async public Task 新增重点学科立项([FromBody]纵向项目立项增改 model)
        {
            model.基本资料.分类 = (int)纵向项目分类.重点学科;
            await 发起一个纵向项目立项流程(model);
        }

        [HttpPost]
        async public Task 新增人才培养立项([FromBody]纵向项目立项增改 model)
        {
            model.基本资料.分类 = (int)纵向项目分类.人才培养;
            await 发起一个纵向项目立项流程(model);
        }

        private async Task 发起一个纵向项目立项流程(纵向项目立项增改 model)
        {
            if (!验证纵向项目立项增改数据(model))
            {
                throw new Exception("批准经费或配套经费与预算金额不符");
            }

            model.基本资料.登记人编号 = CurrentUser.编号;
            await MyWorkFlowBusiness.发起流程(
                纵向项目立项流程模板编号,
                new sp_纵向项目立项_增改()
                {
                    流程模板编号 = 纵向项目立项流程模板编号,
                    tt = model.基本资料.ToDataTable(),
                    tt_项目参与人 = model.参与人列表.ToDataTable(),
                    tt_项目经费预算 = model.经费预算列表.ToDataTable(),
                    tt_项目计划 = model.计划列表.ToDataTable(),
                    tt_项目预期成果 = model.预期成果列表.ToDataTable()
                },
                CurrentUser.编号,
                model.IsHold
                //,model.基本资料.负责人编号 //18-6-13 注释这行,项目的负责人是实际负责人,却不一定来使用这个系统.使用这个系统登记项目的登记人才是流程的发起人;也就是当前登录人;所以流程的发起人不能是项目负责人;
                );
        }

        [HttpPost]
        async public Task 完善纵向项目立项资料([FromBody]完成步骤<纵向项目立项增改> model)
        {
            if (!验证纵向项目立项增改数据(model.附加数据))
            {
                throw new Exception("批准经费或配套经费与预算金额不符");
            }

            await MyWorkFlowBusiness.完成步骤(
                  model.toSimple(), CurrentUser.编号,
                  new sp_纵向项目立项_增改()
                  {
                      流程模板编号 = 纵向项目立项流程模板编号,
                      tt = model.附加数据.基本资料.ToDataTable(),
                      tt_项目参与人 = model.附加数据.参与人列表.ToDataTable(),
                      tt_项目经费预算 = model.附加数据.经费预算列表.ToDataTable(),
                      tt_项目计划 = model.附加数据.计划列表.ToDataTable(),
                      tt_项目预期成果 = model.附加数据.预期成果列表.ToDataTable()
                  }, model.附加数据.IsHold);
        }

        private bool 验证纵向项目立项增改数据(纵向项目立项增改 model)
        {
            return model.基本资料.批准经费 == model.经费预算列表.Sum(i => i.批准经费) &&
                   model.基本资料.配套经费 == model.经费预算列表.Sum(i => i.配套经费)
                   ? true : false;
        }

        [HttpPost]
        async public Task<object> 上传立项文件()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/纵向项目/立项文件",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpGet]
        async public Task<object> 分页获取科研项目立项(Paging paging, 纵向项目立项Filter filter)
        {
            filter.分类 = (int)纵向项目分类.科研项目;
            return await 分页获取纵向项目立项列表(paging, filter);
        }

        [HttpGet]
        async public Task<object> 分页获取人才培养立项(Paging paging, 纵向项目立项Filter filter)
        {
            filter.分类 = (int)纵向项目分类.人才培养;
            return await 分页获取纵向项目立项列表(paging, filter);
        }

        [HttpGet]
        async public Task<object> 分页获取重点学科立项(Paging paging, 纵向项目立项Filter filter)
        {
            filter.分类 = (int)纵向项目分类.重点学科;
            return await 分页获取纵向项目立项列表(paging, filter);
        }

        private async Task<object> 分页获取纵向项目立项列表(Paging paging, 纵向项目立项Filter filter) =>
            await Db.GetPagingListSpAsync<object, 纵向项目立项Filter>(
                paging,
                filter,
                $"tfn_纵向项目立项({CurrentUser.编号},{纵向项目立项流程模板编号})");

        [HttpGet]
        async public Task<object> 获取纵向项目立项详情(int 纵向项目立项编号)
        {
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, 纵向项目立项编号))
            {
                return ProjectAccessDenied();
            }
            //var r1 = await Db.GetListSpAsync<v2_纵向项目立项_详情, 纵向项目立项Filter>(new 纵向项目立项Filter() { 编号 = 纵向项目立项编号 });
            var r1 = await Db.GetModelByIdSpAsync<v2_纵向项目立项_详情>(纵向项目立项编号);
            var r2 = await Db.GetListSpAsync<object>($"tfn_项目参与人({纵向项目立项流程模板编号},{纵向项目立项编号})");
            var r3 = await Db.GetListSpAsync<object>($"tfn_项目经费预算({纵向项目立项流程模板编号},{纵向项目立项编号})");
            var r4 = await Db.GetListSpAsync<object>($"tfn_项目计划({纵向项目立项流程模板编号},{纵向项目立项编号})");
            var r5 = await Db.GetListSpAsync<object>($"tfn_项目预期成果({纵向项目立项流程模板编号},{纵向项目立项编号})");
            var r6 = await MyWorkFlowBusiness.获取某流程的步骤(纵向项目立项流程模板编号, 纵向项目立项编号);
            return new { 立项基本信息 = r1, 参与人信息 = r2, 财务信息 = r3, 项目计划与目标 = r4, 项目预期成果 = r5, 审核记录 = r6 };
        }
        #endregion

        #region 纵向项目中检
        public int 纵向项目中检流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:纵向项目中检");
            }
        }

        [HttpGet]
        async public Task<项目生命周期预检查结果> 预检查纵向项目中检(int 纵向项目立项编号)
        {
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, 纵向项目立项编号))
                return new 项目生命周期预检查结果 { 通过 = false, 阶段 = "中检", 项目编号 = 纵向项目立项编号, 检查项 = new[] { new 项目生命周期预检查项 { 代码 = "ACCESS", 名称 = "项目访问权", 提示 = "无权访问该项目", 阻断提交 = true } } };
            return await 获取纵向项目生命周期预检查("中检", 纵向项目立项编号, 纵向项目中检流程模板编号);
        }

        private async Task<项目生命周期预检查结果> 获取纵向项目生命周期预检查(string stage, int projectId, int stageTemplateId)
        {
            var checks = new List<项目生命周期预检查项>();
            void Add(string code, string name, bool passed, string message) => checks.Add(new 项目生命周期预检查项
            {
                代码 = code, 名称 = name, 通过 = passed, 阻断提交 = true, 提示 = message
            });

            Add("PROJECT_ID", "项目编号", projectId > 0, projectId > 0 ? "项目编号有效" : "项目编号必须大于0");
            if (projectId <= 0)
                return new 项目生命周期预检查结果 { 通过 = false, 阶段 = stage, 项目编号 = projectId, 检查项 = checks };

            var state = await Db.QuerySingleAsync<dynamic>(@"
SELECT EXISTS (SELECT 1 FROM ""纵向项目立项"" WHERE ""编号"" = @项目编号) AS project_exists,
       EXISTS (SELECT 1 FROM ""流程"" WHERE ""流程模板编号"" = @立项模板编号 AND ""项目编号"" = @项目编号 AND ""状态"" = 1) AS approved,
       EXISTS (SELECT 1 FROM ""纵向项目结题"" WHERE ""纵向项目编号"" = @项目编号) AS closed;",
                new { 项目编号 = projectId, 立项模板编号 = 纵向项目立项流程模板编号 });
            Add("PROJECT_EXISTS", "立项记录", (bool)state.project_exists, (bool)state.project_exists ? "已找到立项记录" : "未找到立项记录");
            Add("APPROVED", "立项审批", (bool)state.approved, (bool)state.approved ? "立项审批已通过" : "项目尚未完成立项审批");

            var closed = (bool)state.closed;
            Add("CLOSED", "项目状态", !closed, closed ? "项目已结题，不能再次办理" : "项目尚未结题");

            var pendingSql = stage switch
            {
                "中检" => @"SELECT EXISTS (SELECT 1 FROM ""流程"" f JOIN ""纵向项目中检"" m ON m.""编号"" = f.""项目编号"" WHERE f.""流程模板编号"" = @模板编号 AND m.""纵向项目编号"" = @项目编号 AND f.""状态"" <> 1);",
                "变更" => @"SELECT EXISTS (SELECT 1 FROM ""流程"" f JOIN ""纵向项目变更"" m ON m.""编号"" = f.""项目编号"" WHERE f.""流程模板编号"" = @模板编号 AND m.""纵向项目编号"" = @项目编号 AND f.""状态"" <> 1);",
                _ => @"SELECT EXISTS (SELECT 1 FROM ""流程"" WHERE ""流程模板编号"" = @模板编号 AND ""项目编号"" IN (SELECT ""编号"" FROM ""纵向项目结题"" WHERE ""纵向项目编号"" = @项目编号) AND ""状态"" <> 1);"
            };
            var pending = await Db.QuerySingleAsync<bool>(pendingSql, new { 模板编号 = stageTemplateId, 项目编号 = projectId });
            Add("PENDING", "流程状态", !pending, pending ? $"已有未完成的{stage}流程" : $"没有未完成的{stage}流程");

            if (stage == "中检")
            {
                var exists = await Db.QuerySingleAsync<bool>(@"SELECT EXISTS (SELECT 1 FROM ""纵向项目中检"" WHERE ""纵向项目编号"" = @项目编号);", new { 项目编号 = projectId });
                Add("DUPLICATE", "中检记录", !exists, exists ? "该项目已有中检记录" : "尚未建立中检记录");
            }
            else if (stage == "结题")
            {
                var midtermPending = await Db.QuerySingleAsync<bool>(@"SELECT EXISTS (SELECT 1 FROM ""流程"" f JOIN ""纵向项目中检"" m ON m.""编号"" = f.""项目编号"" WHERE f.""流程模板编号"" = @模板编号 AND m.""纵向项目编号"" = @项目编号 AND f.""状态"" <> 1);", new { 模板编号 = 纵向项目中检流程模板编号, 项目编号 = projectId });
                var changePending = await Db.QuerySingleAsync<bool>(@"SELECT EXISTS (SELECT 1 FROM ""流程"" f JOIN ""纵向项目变更"" m ON m.""编号"" = f.""项目编号"" WHERE f.""流程模板编号"" = @模板编号 AND m.""纵向项目编号"" = @项目编号 AND f.""状态"" <> 1);", new { 模板编号 = 纵向项目变更流程模板编号, 项目编号 = projectId });
                Add("MIDTERM_DONE", "中检流程", !midtermPending, midtermPending ? "中检流程尚未完成" : "中检流程已完成或未发起");
                Add("CHANGE_DONE", "变更流程", !changePending, changePending ? "变更流程尚未完成" : "变更流程已完成或未发起");
                var exists = await Db.QuerySingleAsync<bool>(@"SELECT EXISTS (SELECT 1 FROM ""纵向项目结题"" WHERE ""纵向项目编号"" = @项目编号);", new { 项目编号 = projectId });
                Add("DUPLICATE", "结题记录", !exists, exists ? "该项目已有结题记录" : "尚未建立结题记录");
            }

            return new 项目生命周期预检查结果 { 通过 = checks.All(x => x.通过), 阶段 = stage, 项目编号 = projectId, 检查项 = checks };
        }

        /// <summary>
        /// 发起一个纵向项目中检
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 新增纵向项目中检([FromBody]发起纵向项目中检 model)
        {
            if (model == null) throw new ArgumentException("缺少中检项目编号");
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, model.纵向项目立项编号)) throw new ForbiddenAccessException("无权访问该项目");
            var preflight = await 获取纵向项目生命周期预检查("中检", model.纵向项目立项编号, 纵向项目中检流程模板编号);
            if (!preflight.通过) throw new ArgumentException("项目中检预检查未通过：" + string.Join("；", preflight.检查项.Where(x => !x.通过).Select(x => x.提示)));
            var newModel = new 纵向项目中检() { 纵向项目编号 = model.纵向项目立项编号 };
            await MyWorkFlowBusiness.发起流程(
                纵向项目中检流程模板编号,
                new sp_纵向项目中检_增改()
                {
                    tt = newModel.ToDataTable()
                },
                CurrentUser.编号,
                true,   //有权限的人来发起,但不提交这第一步,补全的事情交给负责人去做;
                model.立项登记人编号 //负责人只有从立项信息里面传来
                );
        }

        [HttpPost]
        async public Task 完善纵向项目中检资料([FromBody]完成步骤<纵向项目中检完善> model) =>
            await MyWorkFlowBusiness.完成步骤(
                model.toSimple(), CurrentUser.编号,
                new sp_纵向项目中检_增改()
                {
                    tt = model.附加数据.基本资料.ToDataTable()
                }, model.附加数据.IsHold);

        [HttpPost]
        async public Task 专家评审纵向项目中检(完成步骤<IEnumerable<专家评分>> model)
        {
            await MyWorkFlowBusiness.专家评审(model, CurrentUser.编号, 纵向项目中检流程模板编号);
        }

        [HttpPost]
        async public Task<object> 上传中检资料()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/纵向项目/中检资料",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpGet]
        async public Task<object> 分页获取纵向项目中检(Paging paging, 纵向项目中检Filter filter) =>
            await Db.GetPagingListSpAsync<object, 纵向项目中检Filter>(
                paging,
                filter,
                $"tfn_纵向项目中检({CurrentUser.编号},{纵向项目立项流程模板编号},{纵向项目中检流程模板编号})");

        [HttpGet]
        async public Task<object> 获取纵向项目中检详情(int 纵向项目立项编号, int 纵向项目中检编号)
        {
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, 纵向项目立项编号))
            {
                return ProjectAccessDenied();
            }
            //var r1 = await Db.GetListSpAsync<v2_纵向项目立项_详情, 纵向项目立项Filter>(new 纵向项目立项Filter() { 编号 = 纵向项目立项编号 });
            var r1 = await Db.GetModelByIdSpAsync<v2_纵向项目立项_详情>(纵向项目立项编号);
            var r2 = await Db.GetListSpAsync<object>($"tfn_项目参与人({纵向项目立项流程模板编号},{纵向项目立项编号})");
            var r3 = await Db.GetListSpAsync<object>($"tfn_项目经费预算({纵向项目立项流程模板编号},{纵向项目立项编号})");
            var r4 = await Db.GetListSpAsync<object>($"tfn_项目计划({纵向项目立项流程模板编号},{纵向项目立项编号})");

            //var r5 = await Db.GetListSpAsync<纵向项目中检, 纵向项目中检Filter>(new 纵向项目中检Filter() { 编号 = 纵向项目中检编号 });
            var r5 = await Db.GetModelByIdSpAsync<纵向项目中检>(纵向项目中检编号);
            var r6 = await Db.GetListSpAsync<object>($"tfn_专家评分({纵向项目中检流程模板编号},{纵向项目中检编号})");

            var r7 = await MyWorkFlowBusiness.获取某流程的步骤(纵向项目中检流程模板编号, 纵向项目中检编号);
            return new
            {
                立项基本信息 = r1,
                参与人信息 = r2,
                财务信息 = r3,
                项目计划与目标 = r4,
                项目中检信息 = r5,
                中检专家评审 = r6,
                中检审核记录 = r7
            };

        }
        #endregion

        #region 纵向项目变更
        public int 纵向项目变更流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:纵向项目变更");
            }
        }

        [HttpGet]
        async public Task<项目生命周期预检查结果> 预检查纵向项目变更(int 纵向项目立项编号)
        {
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, 纵向项目立项编号))
                return new 项目生命周期预检查结果 { 通过 = false, 阶段 = "变更", 项目编号 = 纵向项目立项编号, 检查项 = new[] { new 项目生命周期预检查项 { 代码 = "ACCESS", 名称 = "项目访问权", 提示 = "无权访问该项目", 阻断提交 = true } } };
            return await 获取纵向项目生命周期预检查("变更", 纵向项目立项编号, 纵向项目变更流程模板编号);
        }

        /// <summary>
        /// 注意:前台在调用这个接口之前,要注意"纵向项目编号"和"编号"的赋值
        /// "纵向项目编号"是打开新增页面之前就已知的;
        /// "编号"这里=0;
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 新增纵向项目变更([FromBody]纵向项目变更增改 model)
        {
            if (model?.基本资料 == null) throw new ArgumentException("缺少变更基本资料");
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, model.基本资料.纵向项目编号)) throw new ForbiddenAccessException("无权访问该项目");
            var preflight = await 获取纵向项目生命周期预检查("变更", model.基本资料.纵向项目编号, 纵向项目变更流程模板编号);
            if (!preflight.通过) throw new ArgumentException("项目变更预检查未通过：" + string.Join("；", preflight.检查项.Where(x => !x.通过).Select(x => x.提示)));
            await MyWorkFlowBusiness.发起流程(
                纵向项目变更流程模板编号,
                new sp_纵向项目变更_增改 { tt = model.基本资料.ToDataTable() }, CurrentUser.编号, model.IsHold);
        }

        [HttpPost]
        async public Task 完善纵向项目变更资料([FromBody] 完成步骤<纵向项目变更增改> model)
        {
            await MyWorkFlowBusiness.完成步骤(
                model.toSimple(), CurrentUser.编号,
                new sp_纵向项目变更_增改()
                {
                    tt = model.附加数据.基本资料.ToDataTable()
                }, model.附加数据.IsHold);
        }

        /// <summary>
        /// 变更最后一步的审核,调这个接口而不是通用的审核接口;
        /// 此步骤仅当操作结果为通过时,交换被变更的立项资料和将变更的立项资料;
        /// </summary>
        /// <param name="纵向项目变更编号"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 审核并应用纵向项目变更([FromBody]完成步骤<int> model)
        {
            var 操作人编号 = await ResolveOperationUserAsync("项目审批");
            await MyWorkFlowBusiness.完成步骤(
                model.toSimple(), 操作人编号,
                new sp_纵向项目变更_应用变更()
                {
                    纵向项目变更编号 = model.附加数据
                },
                AdditionalActionsWhenState: 1);
        }

        [HttpGet]
        async public Task<object> 分页获取纵向项目变更(Paging paging, 纵向项目变更Filter filter) =>
           await Db.GetPagingListSpAsync<object, 纵向项目变更Filter>(
               paging,
               filter,
               $"tfn_纵向项目变更({CurrentUser.编号},{纵向项目立项流程模板编号},{纵向项目变更流程模板编号})");

        /// <summary>
        /// 获取纵向项目变更详情,包括变更前后的详情;
        /// </summary>
        /// <param name="纵向项目立项编号"></param>
        /// <param name="纵向项目变更编号"></param>
        /// <param name="状态">当前流程的状态</param>
        /// <returns></returns>
        [HttpGet]
        async public Task<object> 获取纵向项目变更详情(int 纵向项目立项编号, int 纵向项目变更编号, int? 状态)
        {
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, 纵向项目立项编号))
            {
                return ProjectAccessDenied();
            }
            var r1 = await Db.GetModelByIdSpAsync<v2_纵向项目立项_详情>(纵向项目立项编号);
            var r2 = await Db.GetModelByIdSpAsync<v2_纵向项目变更_详情>(纵向项目变更编号);

            var r3 = new v2_纵向项目立项_详情();
            Tool.ModelToModel(r3, r2);
            r3.编号 = r2.纵向项目编号;

            var r4 = await MyWorkFlowBusiness.获取某流程的步骤(纵向项目变更流程模板编号, 纵向项目变更编号);
            return new
            {
                r2.编号,
                r2.变更内容简介,
                r2.变更事由,
                r2.变更备注,
                r2.变更时间,
                变更前 = 状态 == 1 ? r3 : r1,
                变更后 = 状态 == 1 ? r1 : r3,
                审核记录 = r4
            };

        }
        #endregion

        #region 纵向项目结题
        public int 纵向项目结题流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:纵向项目结题");
            }
        }

        [HttpGet]
        async public Task<项目生命周期预检查结果> 预检查纵向项目结题(int 纵向项目立项编号)
        {
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, 纵向项目立项编号))
                return new 项目生命周期预检查结果 { 通过 = false, 阶段 = "结题", 项目编号 = 纵向项目立项编号, 检查项 = new[] { new 项目生命周期预检查项 { 代码 = "ACCESS", 名称 = "项目访问权", 提示 = "无权访问该项目", 阻断提交 = true } } };
            return await 获取纵向项目生命周期预检查("结题", 纵向项目立项编号, 纵向项目结题流程模板编号);
        }

        /// <summary>
        /// 发起一个纵向项目结题;
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 新增纵向项目结题([FromBody]发起纵向项目结题 model)
        {
            if (model == null) throw new ArgumentException("缺少结题项目编号");
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, model.纵向项目立项编号)) throw new ForbiddenAccessException("无权访问该项目");
            var preflight = await 获取纵向项目生命周期预检查("结题", model.纵向项目立项编号, 纵向项目结题流程模板编号);
            if (!preflight.通过) throw new ArgumentException("项目结题预检查未通过：" + string.Join("；", preflight.检查项.Where(x => !x.通过).Select(x => x.提示)));
            var newModel = new 纵向项目结题() { 纵向项目编号 = model.纵向项目立项编号 };
            await MyWorkFlowBusiness.发起流程(
                纵向项目结题流程模板编号,
                new sp_纵向项目结题_增改()
                {
                    tt = newModel.ToDataTable()
                },
                CurrentUser.编号,
                true,
                model.立项发起人编号);
        }

        [HttpPost]
        async public Task 完善纵向项目结题资料([FromBody] 完成步骤<纵向项目结题完善> model) =>
            await MyWorkFlowBusiness.完成步骤(
                model.toSimple(), CurrentUser.编号,
                new sp_纵向项目结题_增改()
                {
                    tt = model.附加数据.基本资料.ToDataTable()
                }, model.附加数据.IsHold);

        [HttpPost]
        async public Task<object> 上传结题资料()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/纵向项目/结题资料",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpGet]
        async public Task<object> 分页获取纵向项目结题(Paging paging, 纵向项目结题Filter filter) =>
           await Db.GetPagingListSpAsync<object, 纵向项目结题Filter>(
               paging,
               filter,
               $"tfn_纵向项目结题({CurrentUser.编号},{纵向项目立项流程模板编号},{纵向项目中检流程模板编号},{纵向项目变更流程模板编号},{纵向项目结题流程模板编号})");

        [HttpGet]
        async public Task<object> 获取纵向项目结题详情(int 纵向项目立项编号, int 纵向项目中检编号, int 纵向项目结题编号)
        {
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, 纵向项目立项编号))
            {
                return ProjectAccessDenied();
            }
            var r1 = await Db.GetModelByIdSpAsync<v2_纵向项目立项_详情>(纵向项目立项编号);
            var r2 = await Db.GetListSpAsync<object>($"tfn_项目参与人({纵向项目立项流程模板编号},{纵向项目立项编号})");
            var r3 = await Db.GetListSpAsync<object>($"tfn_项目经费预算({纵向项目立项流程模板编号},{纵向项目立项编号})");
            var r4 = await Db.GetListSpAsync<object>($"tfn_项目计划({纵向项目立项流程模板编号},{纵向项目立项编号})");

            var r5 = await Db.GetModelByIdSpAsync<纵向项目中检>(纵向项目中检编号);
            var r6 = await Db.GetListSpAsync<object>($"tfn_专家评分({纵向项目中检流程模板编号},{纵向项目中检编号})");

            var r7 = await Db.GetModelByIdSpAsync<纵向项目结题>(纵向项目结题编号);
            var r8 = await Db.GetListSpAsync<object>($"tfn_专家评分({纵向项目结题流程模板编号},{纵向项目结题编号})");
            var r9 = await MyWorkFlowBusiness.获取某流程的步骤(纵向项目结题流程模板编号, 纵向项目结题编号);
            return new
            {
                立项基本信息 = r1,
                参与人信息 = r2,
                财务信息 = r3,
                项目计划与目标 = r4,

                项目中检信息 = r5,
                中检专家评审 = r6,

                项目结题信息 = r7,
                结题专家评审 = r8,
                结题审核记录 = r9
            };

        }
        #endregion

        #region 项目生命周期治理
        /// <summary>返回延期、暂停、终止申请及研究报告、档案材料的统一视图。</summary>
        [HttpGet]
        async public Task<object> 获取纵向项目生命周期档案(int 纵向项目立项编号)
        {
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, 纵向项目立项编号))
                return ProjectAccessDenied();
            var 项目 = await Db.QuerySingleOrDefaultAsync<object>(@"
SELECT p.""编号"", p.""项目中文名称"", p.""负责人编号"", p.""计划开始日期"", p.""计划完成日期"",
       COALESCE((SELECT e.""事件类型"" FROM ""纵向项目生命周期申请"" e
                  WHERE e.""纵向项目立项编号"" = p.""编号"" AND e.""状态"" = '已批准'
                  ORDER BY e.""审核时间"" DESC NULLS LAST, e.""编号"" DESC LIMIT 1), '正常') AS ""当前状态""
  FROM ""纵向项目立项"" p WHERE p.""编号"" = @项目编号;", new { 项目编号 = 纵向项目立项编号 });
            if (项目 == null) return NotFound(new { error = "纵向项目不存在" });
            var 申请 = await Db.QueryAsync<object>(@"
SELECT e.""编号"", e.""事件类型"", e.""原计划完成日期"", e.""新计划完成日期"", e.""申请原因"",
       e.""附件路径"", e.""状态"", e.""申请人编号"", e.""审核人编号"", e.""审核意见"", e.""申请时间"", e.""审核时间""
  FROM ""纵向项目生命周期申请"" e
 WHERE e.""纵向项目立项编号"" = @项目编号 ORDER BY e.""申请时间"" DESC, e.""编号"" DESC;", new { 项目编号 = 纵向项目立项编号 });
            var 报告 = await Db.QueryAsync<object>(@"
SELECT ""编号"", ""报告类型"", ""报告年度"", ""报告摘要"", ""文件路径"", ""状态"",
       ""提交人编号"", ""提交时间"", ""审核人编号"", ""审核意见"", ""审核时间"", ""建立时间""
  FROM ""纵向项目研究报告"" WHERE ""纵向项目立项编号"" = @项目编号
 ORDER BY ""报告年度"" DESC, ""建立时间"" DESC;", new { 项目编号 = 纵向项目立项编号 });
            var 材料 = await Db.QueryAsync<object>(@"
SELECT ""编号"", ""材料类型"", ""材料标题"", ""文件路径"", ""文件校验和"", ""版本号"", ""是否必需"",
       ""状态"", ""上传人编号"", ""上传时间""
  FROM ""纵向项目档案材料"" WHERE ""纵向项目立项编号"" = @项目编号
 ORDER BY ""材料类型"", ""版本号"" DESC, ""上传时间"" DESC;", new { 项目编号 = 纵向项目立项编号 });
            return new { 项目, 生命周期申请 = 申请, 研究报告 = 报告, 档案材料 = 材料 };
        }

        [HttpPost]
        async public Task<object> 申请纵向项目延期暂停终止([FromBody]纵向项目生命周期申请新增 model)
        {
            if (model == null || model.纵向项目立项编号 <= 0) throw new ArgumentException("缺少纵向项目编号");
            var eventType = model.事件类型?.Trim();
            if (eventType is not ("延期" or "暂停" or "终止")) throw new ArgumentException("事件类型必须是延期、暂停或终止");
            if (string.IsNullOrWhiteSpace(model.申请原因) || model.申请原因.Trim().Length > 2000)
                throw new ArgumentException("申请原因不能为空且不能超过2000个字符");
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, model.纵向项目立项编号)) return ProjectAccessDenied();
            var state = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT p.""计划完成日期"", EXISTS (SELECT 1 FROM ""纵向项目结题"" c WHERE c.""纵向项目编号"" = p.""编号"") AS closed,
       COALESCE((SELECT e.""事件类型"" FROM ""纵向项目生命周期申请"" e
                  WHERE e.""纵向项目立项编号"" = p.""编号"" AND e.""状态"" = '已批准'
                  ORDER BY e.""审核时间"" DESC NULLS LAST, e.""编号"" DESC LIMIT 1), '正常') AS current_status
  FROM ""纵向项目立项"" p WHERE p.""编号"" = @项目编号;", new { 项目编号 = model.纵向项目立项编号 });
            if (state == null) return NotFound(new { error = "纵向项目不存在" });
            if ((bool)state.closed || (string)state.current_status == "终止") throw new ArgumentException("项目已结题或终止，不能再申请生命周期变更");
            if (eventType == "延期" && (!model.新计划完成日期.HasValue || model.新计划完成日期.Value.Date <= ((DateTime)state.计划完成日期).Date))
                throw new ArgumentException("延期后的完成日期必须晚于当前计划完成日期");
            var pending = await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (SELECT 1 FROM ""纵向项目生命周期申请"" WHERE ""纵向项目立项编号""=@项目编号 AND ""事件类型""=@事件类型 AND ""状态""='待审核');",
                new { 项目编号 = model.纵向项目立项编号, 事件类型 = eventType });
            if (pending) throw new ArgumentException("该类生命周期申请已有待审核记录");
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""纵向项目生命周期申请"" (""纵向项目立项编号"",""事件类型"",""原计划完成日期"",""新计划完成日期"",""申请原因"",""附件路径"",""申请人编号"")
SELECT @项目编号,@事件类型,""计划完成日期"",@新计划完成日期,@申请原因,@附件路径,@申请人编号
  FROM ""纵向项目立项"" WHERE ""编号""=@项目编号 RETURNING ""编号"";", new
            {
                项目编号 = model.纵向项目立项编号, 事件类型 = eventType, 新计划完成日期 = model.新计划完成日期?.Date,
                申请原因 = model.申请原因.Trim(), 附件路径 = string.IsNullOrWhiteSpace(model.附件路径) ? null : model.附件路径.Trim(),
                申请人编号 = CurrentUser.编号
            });
            return new { 编号 = id, 状态 = "待审核" };
        }

        [HttpPost]
        async public Task<object> 审核纵向项目生命周期申请([FromBody]纵向项目生命周期审核 model)
        {
            if (model == null || model.编号 <= 0) throw new ArgumentException("缺少申请编号");
            if ((model.审核意见?.Length ?? 0) > 2000) throw new ArgumentException("审核意见不能超过2000个字符");
            if (!await HasAnyRoleAsync("科研管理员", "系统管理员")) return StatusCode(403, new { error = "无权审核项目生命周期申请" });
            var 审核人编号 = await ResolveOperationUserAsync("项目审批");
            using var connection = Db;
            connection.Open();
            using var transaction = connection.BeginTransaction();
            var request = await connection.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"", ""纵向项目立项编号"", ""事件类型"", ""新计划完成日期""
  FROM ""纵向项目生命周期申请"" WHERE ""编号""=@编号 AND ""状态""='待审核';", new { model.编号 }, transaction);
            if (request == null) throw new ArgumentException("申请不存在或已审核");
            var status = model.通过 ? "已批准" : "已驳回";
            await connection.ExecuteAsync(@"
UPDATE ""纵向项目生命周期申请""
   SET ""状态""=@状态, ""审核人编号""=@审核人编号, ""审核意见""=@审核意见, ""审核时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@编号 AND ""状态""='待审核';", new
            {
                状态 = status, 审核人编号, 审核意见 = string.IsNullOrWhiteSpace(model.审核意见) ? null : model.审核意见.Trim(), model.编号
            }, transaction);
            if (model.通过 && (string)request.事件类型 == "延期")
            {
                await connection.ExecuteAsync(@"
UPDATE ""纵向项目立项"" SET ""计划完成日期""=@新计划完成日期 WHERE ""编号""=@项目编号;",
                    new { 新计划完成日期 = (DateTime)request.新计划完成日期, 项目编号 = (int)request.纵向项目立项编号 }, transaction);
            }
            transaction.Commit();
            return new { 编号 = model.编号, 状态 = status };
        }

        [HttpPost]
        async public Task<object> 提交纵向项目研究报告([FromBody]纵向项目研究报告新增 model)
        {
            if (model == null || model.纵向项目立项编号 <= 0) throw new ArgumentException("缺少纵向项目编号");
            if (string.IsNullOrWhiteSpace(model.报告类型) || model.报告类型.Trim().Length > 30) throw new ArgumentException("报告类型不能为空");
            if (model.报告年度 < 2000 || model.报告年度 > DateTime.Today.Year + 1) throw new ArgumentException("报告年度无效");
            if ((model.报告摘要?.Length ?? 0) > 2000 || (model.报告正文?.Length ?? 0) > 200000) throw new ArgumentException("研究报告内容超出限制");
            if (string.IsNullOrWhiteSpace(model.报告正文) && string.IsNullOrWhiteSpace(model.文件路径)) throw new ArgumentException("请填写报告正文或上传报告文件");
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, model.纵向项目立项编号)) return ProjectAccessDenied();
            var currentStatus = await Db.QuerySingleOrDefaultAsync<string>(@"
SELECT COALESCE((SELECT e.""事件类型"" FROM ""纵向项目生命周期申请"" e WHERE e.""纵向项目立项编号""=@项目编号 AND e.""状态""='已批准' ORDER BY e.""审核时间"" DESC NULLS LAST, e.""编号"" DESC LIMIT 1),'正常');", new { 项目编号 = model.纵向项目立项编号 });
            if (currentStatus == "终止") throw new ArgumentException("项目已终止，不能提交新的研究报告");
            var status = model.提交审核 ? "待审核" : "草稿";
            var id = await Db.QuerySingleOrDefaultAsync<long?>(@"
INSERT INTO ""纵向项目研究报告"" (""纵向项目立项编号"",""报告类型"",""报告年度"",""报告摘要"",""报告正文"",""文件路径"",""状态"",""提交人编号"",""提交时间"")
VALUES (@项目编号,@报告类型,@报告年度,@报告摘要,@报告正文,@文件路径,@状态,@提交人编号,CASE WHEN @提交审核 THEN CURRENT_TIMESTAMP ELSE NULL END)
ON CONFLICT (""纵向项目立项编号"",""报告类型"",""报告年度"") DO UPDATE SET
  ""报告摘要""=EXCLUDED.""报告摘要"", ""报告正文""=EXCLUDED.""报告正文"", ""文件路径""=EXCLUDED.""文件路径"", ""状态""=EXCLUDED.""状态"", ""提交时间""=EXCLUDED.""提交时间""
 WHERE ""纵向项目研究报告"".""提交人编号""=@提交人编号 AND ""纵向项目研究报告"".""状态"" IN ('草稿','已退回')
RETURNING ""编号"";", new
            {
                项目编号 = model.纵向项目立项编号, 报告类型 = model.报告类型.Trim(), model.报告年度,
                报告摘要 = string.IsNullOrWhiteSpace(model.报告摘要) ? null : model.报告摘要.Trim(),
                报告正文 = string.IsNullOrWhiteSpace(model.报告正文) ? null : model.报告正文.Trim(),
                文件路径 = string.IsNullOrWhiteSpace(model.文件路径) ? null : model.文件路径.Trim(), 状态 = status,
                提交人编号 = CurrentUser.编号, model.提交审核
            });
            if (!id.HasValue) throw new ArgumentException("同年度同类型报告已由其他人员提交审核，不能覆盖");
            return new { 编号 = id.Value, 状态 = status };
        }

        [HttpPost]
        async public Task<object> 审核纵向项目研究报告([FromBody]纵向项目研究报告审核 model)
        {
            if (model == null || model.编号 <= 0) throw new ArgumentException("缺少报告编号");
            if ((model.审核意见?.Length ?? 0) > 2000) throw new ArgumentException("审核意见不能超过2000个字符");
            if (!await HasAnyRoleAsync("科研管理员", "系统管理员")) return StatusCode(403, new { error = "无权审核研究报告" });
            var 审核人编号 = await ResolveOperationUserAsync("项目审批");
            var updated = await Db.ExecuteAsync(@"
UPDATE ""纵向项目研究报告"" SET ""状态""=@状态,""审核人编号""=@审核人编号,""审核意见""=@审核意见,""审核时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@编号 AND ""状态""='待审核';", new
            {
                状态 = model.通过 ? "已通过" : "已退回", 审核人编号,
                审核意见 = string.IsNullOrWhiteSpace(model.审核意见) ? null : model.审核意见.Trim(), model.编号
            });
            if (updated == 0) throw new ArgumentException("报告不存在或当前不在待审核状态");
            return new { 编号 = model.编号, 状态 = model.通过 ? "已通过" : "已退回" };
        }

        [HttpPost]
        async public Task<object> 归档纵向项目材料([FromBody]纵向项目档案材料新增 model)
        {
            if (model == null || model.纵向项目立项编号 <= 0) throw new ArgumentException("缺少纵向项目编号");
            if (string.IsNullOrWhiteSpace(model.材料类型) || string.IsNullOrWhiteSpace(model.材料标题) || string.IsNullOrWhiteSpace(model.文件路径))
                throw new ArgumentException("材料类型、材料标题和文件路径不能为空");
            if (model.材料类型.Trim().Length > 50 || model.材料标题.Trim().Length > 200 || model.文件路径.Trim().Length > 1000)
                throw new ArgumentException("档案材料字段超出限制");
            if (model.版本号 <= 0) throw new ArgumentException("档案材料版本号必须大于0");
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, model.纵向项目立项编号)) return ProjectAccessDenied();
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""纵向项目档案材料"" (""纵向项目立项编号"",""材料类型"",""材料标题"",""文件路径"",""文件校验和"",""版本号"",""是否必需"",""上传人编号"")
VALUES (@项目编号,@材料类型,@材料标题,@文件路径,@文件校验和,@版本号,@是否必需,@上传人编号) RETURNING ""编号"";", new
            {
                项目编号 = model.纵向项目立项编号, 材料类型 = model.材料类型.Trim(), 材料标题 = model.材料标题.Trim(), 文件路径 = model.文件路径.Trim(),
                文件校验和 = string.IsNullOrWhiteSpace(model.文件校验和) ? null : model.文件校验和.Trim(), model.版本号, model.是否必需, 上传人编号 = CurrentUser.编号
            });
            return new { 编号 = id };
        }

        [HttpPost]
        async public Task<object> 上传项目档案材料()
        {
            var filesNameList = await MyLib.UploadFile.Upload(Request.Form.Files, Env.WebRootPath, "upload/纵向项目/档案材料", config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        #region 任务书与电子档案
        /// <summary>获取项目任务书的所有版本及内部确认留痕。</summary>
        [HttpGet]
        async public Task<object> 获取纵向项目任务书(int 纵向项目立项编号)
        {
            if (纵向项目立项编号 <= 0 || !await CanAccessProjectAsync(纵向项目立项流程模板编号, 纵向项目立项编号))
                return ProjectAccessDenied();
            try
            {
                var 任务书 = await Db.QueryAsync<object>(@"
SELECT ""编号"",""纵向项目立项编号"",""版本号"",""任务书标题"",""任务书内容""::text AS ""任务书内容"",
       ""文件路径"",""文件校验和"",""生成方式"",""生成说明"",""状态"",""生成人人员编号"",""生效时间"",""建立时间"",""更新时间""
  FROM ""纵向项目任务书"" WHERE ""纵向项目立项编号""=@项目编号 ORDER BY ""版本号"" DESC;", new { 项目编号 = 纵向项目立项编号 });
                var 签署记录 = await Db.QueryAsync<object>(@"
SELECT s.""编号"",s.""任务书编号"",s.""签署人编号"",p.""姓名"" AS ""签署人姓名"",s.""签署角色"",
       s.""签署方式"",s.""签署声明"",s.""状态"",s.""签署说明"",s.""请求时间"",s.""确认时间""
  FROM ""纵向项目任务书签署"" s JOIN ""纵向项目任务书"" t ON t.""编号""=s.""任务书编号""
  LEFT JOIN ""人员"" p ON p.""编号""=s.""签署人编号""
 WHERE t.""纵向项目立项编号""=@项目编号 ORDER BY s.""请求时间"" DESC,s.""编号"" DESC;", new { 项目编号 = 纵向项目立项编号 });
                return new { 任务书, 签署记录, 内部签署说明 = "签署记录仅表示系统内确认，不构成法律电子签章。" };
            }
            catch (PostgresException)
            {
                return StatusCode(503, new { error = "任务书迁移尚未应用，请先执行 077 号迁移" });
            }
        }

        /// <summary>生成任务书新版本。未传内容时由立项、计划、预算和预期成果生成 JSON 快照。</summary>
        [HttpPost]
        async public Task<object> 生成纵向项目任务书([FromBody]纵向项目任务书生成请求 model)
        {
            if (model == null || model.纵向项目立项编号 <= 0) throw new ArgumentException("缺少纵向项目编号");
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, model.纵向项目立项编号)) return ProjectAccessDenied();
            if ((model.任务书标题?.Trim().Length ?? 0) > 300 || (model.生成说明?.Length ?? 0) > 2000)
                throw new ArgumentException("任务书标题或生成说明超出限制");
            var taskbookPath = string.IsNullOrWhiteSpace(model.文件路径) ? null : model.文件路径.Trim();
            var taskbookChecksum = string.IsNullOrWhiteSpace(model.文件校验和) ? null : model.文件校验和.Trim();
            if (taskbookPath != null && System.Text.RegularExpressions.Regex.IsMatch(taskbookPath, @"(^|[/\\])(demo|sample|模拟|样例)([/\\]|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                throw new ArgumentException("任务书文件路径不能使用示例或演示目录");
            if (taskbookChecksum != null && !System.Text.RegularExpressions.Regex.IsMatch(taskbookChecksum, "^[0-9a-fA-F]{64}$"))
                throw new ArgumentException("任务书文件校验和必须是64位SHA-256十六进制摘要");
            if (model.上传修订 && (taskbookPath == null || taskbookChecksum == null))
                throw new ArgumentException("上传修订任务书必须同时提供文件路径和SHA-256校验和");

            var project = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"",""项目中文名称"",""负责人编号"",""计划开始日期"",""计划完成日期"",""批准经费"",""项目级别"",""项目来源单位""
  FROM ""纵向项目立项"" WHERE ""编号""=@项目编号;", new { 项目编号 = model.纵向项目立项编号 });
            if (project == null) return NotFound(new { error = "纵向项目不存在" });

            var content = model.任务书内容;
            if (string.IsNullOrWhiteSpace(content))
            {
                var plans = await Db.GetListSpAsync<object>($"tfn_项目计划({纵向项目立项流程模板编号},{model.纵向项目立项编号})");
                var budgets = await Db.GetListSpAsync<object>($"tfn_项目经费预算({纵向项目立项流程模板编号},{model.纵向项目立项编号})");
                var outputs = await Db.GetListSpAsync<object>($"tfn_项目预期成果({纵向项目立项流程模板编号},{model.纵向项目立项编号})");
                content = JsonConvert.SerializeObject(new
                {
                    项目 = project,
                    计划 = plans,
                    预算 = budgets,
                    预期成果 = outputs,
                    生成时间 = DateTime.UtcNow,
                    生成说明 = "由立项数据自动生成，提交前应由项目负责人核对并补充。"
                });
            }
            try
            {
                JToken.Parse(content);
            }
            catch (Exception)
            {
                throw new ArgumentException("任务书内容必须是合法 JSON");
            }
            var title = string.IsNullOrWhiteSpace(model.任务书标题) ? $"{project.项目中文名称}任务书" : model.任务书标题.Trim();
            var id = await Db.QuerySingleAsync<long>(@"
WITH next_version AS (
    SELECT COALESCE(MAX(""版本号""),0)+1 AS version FROM ""纵向项目任务书"" WHERE ""纵向项目立项编号""=@项目编号
)
INSERT INTO ""纵向项目任务书""
 (""纵向项目立项编号"",""版本号"",""任务书标题"",""任务书内容"",""文件路径"",""文件校验和"",""生成方式"",""生成说明"",""状态"",""生成人人员编号"")
SELECT @项目编号,version,@任务书标题,CAST(@任务书内容 AS jsonb),@文件路径,@文件校验和,
       CASE WHEN @上传修订 THEN '上传修订' ELSE '系统生成' END,@生成说明,'草稿',@生成人人员编号
  FROM next_version RETURNING ""编号"";", new
            {
                项目编号 = model.纵向项目立项编号, 任务书标题 = title, 任务书内容 = content,
                文件路径 = taskbookPath,
                文件校验和 = taskbookChecksum,
                上传修订 = model.上传修订, 生成说明 = string.IsNullOrWhiteSpace(model.生成说明) ? null : model.生成说明.Trim(),
                生成人人员编号 = CurrentUser.编号
            });
            var version = await Db.QuerySingleAsync<int>("SELECT \"版本号\" FROM \"纵向项目任务书\" WHERE \"编号\"=@编号;", new { 编号 = id });
            return new { 编号 = id, 版本号 = version, 状态 = "草稿", 内部签署说明 = "任务书内部确认不构成法律电子签章" };
        }

        /// <summary>向项目负责人、科研秘书或管理人员发起内部确认，不调用外部电子签章服务。</summary>
        [HttpPost]
        async public Task<object> 发起纵向项目任务书内部签署([FromBody]纵向项目任务书签署请求 model)
        {
            if (model == null || model.任务书编号 <= 0 || model.签署人编号 <= 0 || string.IsNullOrWhiteSpace(model.签署角色))
                throw new ArgumentException("任务书编号、签署人和签署角色不能为空");
            if (model.签署角色.Trim().Length > 50) throw new ArgumentException("签署角色不能超过50个字符");
            var signerRole = model.签署角色.Trim();
            if (signerRole is not ("项目负责人" or "科研秘书" or "科室负责人" or "科研管理员" or "科技办"))
                throw new ArgumentException("签署角色必须是项目负责人、科研秘书、科室负责人、科研管理员或科技办");
            var taskbook = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT t.""编号"",t.""纵向项目立项编号"",t.""状态""
  FROM ""纵向项目任务书"" t WHERE t.""编号""=@编号;", new { 编号 = model.任务书编号 });
            if (taskbook == null) return NotFound(new { error = "任务书不存在" });
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, (int)taskbook.纵向项目立项编号)) return ProjectAccessDenied();
            var personExists = await Db.QuerySingleAsync<bool>("SELECT EXISTS (SELECT 1 FROM \"人员\" WHERE \"编号\"=@编号 AND \"是否启用\"=TRUE);", new { 编号 = model.签署人编号 });
            if (!personExists) throw new ArgumentException("签署人不存在或已停用");
            var managementRole = await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办");
            var signerInProject = await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (
    SELECT 1 FROM ""纵向项目立项"" p
     WHERE p.""编号""=@项目编号 AND p.""负责人编号""=@签署人编号
) OR EXISTS (
    SELECT 1 FROM ""项目参与人"" pp
     WHERE pp.""流程模板编号""=@模板编号
       AND pp.""项目编号""=@项目编号
       AND pp.""参与人编号""=@签署人编号
);", new { 项目编号 = (int)taskbook.纵向项目立项编号, 模板编号 = 纵向项目立项流程模板编号, 签署人编号 = model.签署人编号 });
            if (!managementRole && !signerInProject)
                return StatusCode(403, new { error = "内部签署人必须是项目负责人、项目参与人或受授权管理人员" });
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""纵向项目任务书签署"" (""任务书编号"",""签署人编号"",""签署角色"")
VALUES (@任务书编号,@签署人编号,@签署角色)
ON CONFLICT (""任务书编号"",""签署人编号"",""签署角色"") DO UPDATE SET ""状态""='待签署',""请求时间""=CURRENT_TIMESTAMP,""确认时间""=NULL,""签署说明""=NULL
RETURNING ""编号"";", new { model.任务书编号, model.签署人编号, 签署角色 = signerRole });
            await Db.ExecuteAsync("UPDATE \"纵向项目任务书\" SET \"状态\"='签署中',\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@编号 AND \"状态\" IN ('草稿','待签署','签署中');", new { 编号 = model.任务书编号 });
            return new { 编号 = id, 状态 = "待签署", 签署声明 = "本人确认已阅读任务书内容。本记录仅作为科研管理系统内部确认留痕，不构成法律电子签章。" };
        }

        [HttpPost]
        async public Task<object> 确认纵向项目任务书内部签署([FromBody]纵向项目任务书确认请求 model)
        {
            if (model == null || model.签署记录编号 <= 0) throw new ArgumentException("缺少签署记录编号");
            if ((model.签署说明?.Length ?? 0) > 2000) throw new ArgumentException("签署说明不能超过2000个字符");
            var row = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT s.""编号"",s.""签署人编号"",s.""任务书编号"",t.""状态""
  FROM ""纵向项目任务书签署"" s JOIN ""纵向项目任务书"" t ON t.""编号""=s.""任务书编号""
 WHERE s.""编号""=@编号;", new { 编号 = model.签署记录编号 });
            if (row == null) return NotFound(new { error = "签署记录不存在" });
            if ((int)row.签署人编号 != CurrentUser.编号) return StatusCode(403, new { error = "只能确认分配给当前账号的内部签署记录" });
            var updated = await Db.ExecuteAsync(@"
UPDATE ""纵向项目任务书签署"" SET ""状态""='已确认',""签署说明""=@签署说明,""确认时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@编号 AND ""状态""='待签署';", new { 编号 = model.签署记录编号, 签署说明 = string.IsNullOrWhiteSpace(model.签署说明) ? null : model.签署说明.Trim() });
            if (updated == 0) throw new ArgumentException("签署记录不在待签署状态");
            return new { 编号 = model.签署记录编号, 状态 = "已确认", 说明 = "内部确认已留痕，不构成法律电子签章" };
        }

        [HttpPost]
        async public Task<object> 生效纵向项目任务书([FromBody]纵向项目任务书确认请求 model)
        {
            if (model == null || (model.任务书编号 <= 0 && model.签署记录编号 <= 0)) throw new ArgumentException("请传入任务书编号");
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办")) return StatusCode(403, new { error = "无权生效任务书" });
            var taskbookId = model.任务书编号 > 0 ? model.任务书编号 : model.签署记录编号;
            var taskbook = await Db.QuerySingleOrDefaultAsync<dynamic>("SELECT \"编号\",\"纵向项目立项编号\" FROM \"纵向项目任务书\" WHERE \"编号\"=@编号;", new { 编号 = taskbookId });
            if (taskbook == null) return NotFound(new { error = "任务书不存在" });
            var pending = await Db.QuerySingleAsync<bool>("SELECT EXISTS (SELECT 1 FROM \"纵向项目任务书签署\" WHERE \"任务书编号\"=@编号 AND \"状态\"<>'已确认');", new { 编号 = taskbookId });
            if (pending) throw new ArgumentException("仍有未完成的内部确认，不能生效任务书");
            await Db.ExecuteAsync("UPDATE \"纵向项目任务书\" SET \"状态\"='已作废',\"更新时间\"=CURRENT_TIMESTAMP WHERE \"纵向项目立项编号\"=@项目编号 AND \"状态\"='已生效' AND \"编号\"<>@编号;", new { 项目编号 = (int)taskbook.纵向项目立项编号, 编号 = taskbookId });
            await Db.ExecuteAsync("UPDATE \"纵向项目任务书\" SET \"状态\"='已生效',\"生效时间\"=CURRENT_TIMESTAMP,\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@编号;", new { 编号 = taskbookId });
            return new { 编号 = taskbookId, 状态 = "已生效" };
        }

        private static readonly (string Code, string Name, string Description, bool Required)[] 默认档案清单 = new[]
        {
            ("APPROVAL", "立项批复", "项目立项批复或批准文件", true),
            ("TASKBOOK", "项目任务书", "已确认的项目任务书版本", true),
            ("ETHICS", "伦理批件", "涉及伦理的项目必须归档有效批件；不涉及时可标记不适用", false),
            ("MIDTERM", "中期检查材料", "中期检查报告、专家意见或结论", true),
            ("CLOSEOUT", "结题验收材料", "结题报告、验收意见和成果清单", true),
            ("FINANCE", "经费决算材料", "经费决算或财务结算凭证", true)
        };

        private async Task 确保项目档案清单(int projectId)
        {
            foreach (var item in 默认档案清单)
            {
                await Db.ExecuteAsync(@"
INSERT INTO ""纵向项目电子档案清单"" (""纵向项目立项编号"",""材料代码"",""材料名称"",""是否必需"",""要求说明"")
VALUES (@项目编号,@材料代码,@材料名称,@是否必需,@要求说明)
ON CONFLICT (""纵向项目立项编号"",""材料代码"") DO NOTHING;", new
                {
                    项目编号 = projectId, 材料代码 = item.Code, 材料名称 = item.Name,
                    是否必需 = item.Required, 要求说明 = item.Description
                });
            }
        }

        [HttpGet]
        async public Task<object> 获取纵向项目电子档案清单(int 纵向项目立项编号)
        {
            if (纵向项目立项编号 <= 0 || !await CanAccessProjectAsync(纵向项目立项流程模板编号, 纵向项目立项编号)) return ProjectAccessDenied();
            await 确保项目档案清单(纵向项目立项编号);
            return await 查询项目档案清单(纵向项目立项编号);
        }

        [HttpPost]
        async public Task<object> 初始化纵向项目电子档案清单([FromBody]纵向项目档案清单项保存 model)
        {
            if (model == null || model.纵向项目立项编号 <= 0) throw new ArgumentException("缺少纵向项目编号");
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, model.纵向项目立项编号)) return ProjectAccessDenied();
            await 确保项目档案清单(model.纵向项目立项编号);
            return await 查询项目档案清单(model.纵向项目立项编号);
        }

        [HttpPost]
        async public Task<object> 保存纵向项目电子档案清单项([FromBody]纵向项目档案清单项保存 model)
        {
            if (model == null || model.纵向项目立项编号 <= 0 || string.IsNullOrWhiteSpace(model.材料代码) || string.IsNullOrWhiteSpace(model.材料名称))
                throw new ArgumentException("项目编号、材料代码和材料名称不能为空");
            if (model.材料代码.Trim().Length > 50 || model.材料名称.Trim().Length > 200 || (model.要求说明?.Length ?? 0) > 1000 || (model.核验说明?.Length ?? 0) > 2000)
                throw new ArgumentException("档案清单字段超出限制");
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, model.纵向项目立项编号)) return ProjectAccessDenied();
            var status = string.IsNullOrWhiteSpace(model.状态) ? "缺失" : model.状态.Trim();
            if (status is not ("缺失" or "已上传" or "已核验" or "不适用" or "作废")) throw new ArgumentException("档案清单状态无效");
            await Db.ExecuteAsync(@"
INSERT INTO ""纵向项目电子档案清单"" (""纵向项目立项编号"",""材料代码"",""材料名称"",""是否必需"",""要求说明"",""状态"",""核验说明"",""核验人编号"",""核验时间"")
VALUES (@项目编号,@材料代码,@材料名称,@是否必需,@要求说明,@状态,@核验说明,CASE WHEN @状态='已核验' THEN @核验人编号 ELSE NULL END,CASE WHEN @状态='已核验' THEN CURRENT_TIMESTAMP ELSE NULL END)
ON CONFLICT (""纵向项目立项编号"",""材料代码"") DO UPDATE SET ""材料名称""=EXCLUDED.""材料名称"",""是否必需""=EXCLUDED.""是否必需"",""要求说明""=EXCLUDED.""要求说明"",""状态""=EXCLUDED.""状态"",""核验说明""=EXCLUDED.""核验说明"",""核验人编号""=EXCLUDED.""核验人编号"",""核验时间""=EXCLUDED.""核验时间"",""更新时间""=CURRENT_TIMESTAMP;", new
            {
                项目编号 = model.纵向项目立项编号, 材料代码 = model.材料代码.Trim(), 材料名称 = model.材料名称.Trim(),
                model.是否必需, 要求说明 = string.IsNullOrWhiteSpace(model.要求说明) ? null : model.要求说明.Trim(),
                状态 = status, 核验说明 = string.IsNullOrWhiteSpace(model.核验说明) ? null : model.核验说明.Trim(), 核验人编号 = CurrentUser.编号
            });
            return await 查询项目档案清单(model.纵向项目立项编号);
        }

        [HttpPost]
        async public Task<object> 核验纵向项目电子档案清单([FromBody]纵向项目档案清单项保存 model)
        {
            if (model == null || model.纵向项目立项编号 <= 0) throw new ArgumentException("缺少纵向项目编号");
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办") && !await CanAccessProjectAsync(纵向项目立项流程模板编号, model.纵向项目立项编号)) return ProjectAccessDenied();
            await 确保项目档案清单(model.纵向项目立项编号);
            var missing = await Db.QuerySingleAsync<int>(@"
SELECT count(*)::integer FROM ""纵向项目电子档案清单"" WHERE ""纵向项目立项编号""=@项目编号 AND ""是否必需"" AND ""状态""='缺失';", new { 项目编号 = model.纵向项目立项编号 });
            if (missing > 0) throw new ArgumentException($"仍缺少 {missing} 项必需档案材料，请补齐后再核验");
            await Db.ExecuteAsync(@"UPDATE ""纵向项目电子档案清单"" SET ""状态""='已核验',""核验人编号""=@人员编号,""核验时间""=CURRENT_TIMESTAMP,""更新时间""=CURRENT_TIMESTAMP WHERE ""纵向项目立项编号""=@项目编号 AND ""状态""='已上传';", new { 项目编号 = model.纵向项目立项编号, 人员编号 = CurrentUser.编号 });
            return await 查询项目档案清单(model.纵向项目立项编号);
        }

        private async Task<object> 查询项目档案清单(int projectId)
        {
            var rows = (await Db.QueryAsync<dynamic>(@"
SELECT c.""编号"",c.""纵向项目立项编号"",c.""材料代码"",c.""材料名称"",c.""是否必需"",c.""要求说明"",c.""状态"",
       c.""关联档案材料编号"",c.""核验说明"",c.""核验人编号"",c.""核验时间""
  FROM ""纵向项目电子档案清单"" c WHERE c.""纵向项目立项编号""=@项目编号 ORDER BY c.""是否必需"" DESC,c.""材料代码"";", new { 项目编号 = projectId })).ToList();
            var missing = rows.Count(x => (bool)x.是否必需 && (string)x.状态 == "缺失");
            return new { 项目编号 = projectId, 完整 = missing == 0, 缺失必需材料数 = missing, 清单 = rows };
        }

        #region 正式电子签章与电子档案服务契约
        /// <summary>
        /// 外部文档服务的配置门禁。Enabled、Provider、Endpoint 和 AdapterReady
        /// 必须同时满足，才允许进入真实供应商联调；本地内部确认永远不等同于法律电子签章。
        /// </summary>
        private sealed class 外部文档服务契约状态
        {
            public string 功能 { get; init; }
            public bool 已启用 { get; init; }
            public bool 配置完整 { get; init; }
            public bool 适配器已验收 { get; init; }
            public bool 可用 { get; init; }
            public string 供应商 { get; init; }
            public string 协议版本 { get; init; }
            public string 原因 { get; init; }
            public string 法律效力 { get; init; }
        }

        private 外部文档服务契约状态 读取外部文档服务契约(string section, string feature)
        {
            var enabled = config.GetValue($"{section}:Enabled", false);
            var provider = config.GetValue<string>($"{section}:Provider")?.Trim() ?? string.Empty;
            var endpoint = config.GetValue<string>($"{section}:Endpoint")?.Trim() ?? string.Empty;
            var protocolVersion = config.GetValue<string>($"{section}:ProtocolVersion")?.Trim() ?? string.Empty;
            var adapterReady = config.GetValue($"{section}:AdapterReady", false);
            var requireHttps = config.GetValue($"{section}:RequireHttps", true);
            var endpointValid = Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri)
                && !string.IsNullOrWhiteSpace(endpointUri.Host)
                && (endpointUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
                    || (!requireHttps && endpointUri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)));
            var configured = enabled && !string.IsNullOrWhiteSpace(provider)
                && !string.IsNullOrWhiteSpace(protocolVersion) && endpointValid;
            var ready = configured && adapterReady;
            var reason = !enabled
                ? "服务未启用，仍处于待接入状态"
                : string.IsNullOrWhiteSpace(provider)
                    ? "未配置供应商标识"
                    : string.IsNullOrWhiteSpace(protocolVersion)
                        ? "未配置协议版本"
                        : !endpointValid
                            ? (requireHttps ? "端点必须是 HTTPS 绝对地址" : "端点必须是 HTTP/HTTPS 绝对地址")
                            : !adapterReady
                                ? "适配器尚未完成真实联调验收"
                                : "配置和适配器验收已完成";
            return new 外部文档服务契约状态
            {
                功能 = feature,
                已启用 = enabled,
                配置完整 = configured,
                适配器已验收 = adapterReady,
                可用 = ready,
                供应商 = string.IsNullOrWhiteSpace(provider) ? null : provider,
                协议版本 = string.IsNullOrWhiteSpace(protocolVersion) ? null : protocolVersion,
                原因 = reason,
                法律效力 = feature == "电子签章"
                    ? "只有外部合格电子签章服务返回并验收后，才可声明电子签章效力；内部确认不构成法律电子签章。"
                    : "只有外部正式电子档案服务完成接入和验收后，才可声明正式电子档案托管；本地上传仅是系统内留存。"
            };
        }

        private object 正式文档服务未就绪(外部文档服务契约状态 status)
        {
            return StatusCode(503, new
            {
                code = "DOCUMENT_PROVIDER_NOT_READY",
                error = $"{status.功能}服务尚未完成真实接入：{status.原因}",
                状态 = status,
                下一步 = "配置供应商、协议版本和 HTTPS 端点，完成真实适配器联调及多角色验收后再启用。"
            });
        }

        /// <summary>查询电子签章供应商接入门禁；未配置时明确返回 503，不把内部确认冒充为电子签章。</summary>
        [HttpGet]
        async public Task<object> 获取纵向项目电子签章服务状态()
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办"))
                return StatusCode(403, new { error = "无权查看电子签章服务状态" });
            var status = 读取外部文档服务契约("ElectronicSignature", "电子签章");
            return status.可用 ? status : 正式文档服务未就绪(status);
        }

        /// <summary>查询正式电子档案供应商接入门禁；本地归档材料不会被标记为正式电子档案。</summary>
        [HttpGet]
        async public Task<object> 获取纵向项目正式档案服务状态()
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办"))
                return StatusCode(403, new { error = "无权查看正式电子档案服务状态" });
            var status = 读取外部文档服务契约("ElectronicArchive", "正式电子档案");
            return status.可用 ? status : 正式文档服务未就绪(status);
        }

        /// <summary>
        /// 正式电子签章提交门禁。目前不调用供应商，也不写入“已签章”状态；
        /// 未完成适配器验收时始终返回 503，避免系统产生虚假的法律签章记录。
        /// </summary>
        [HttpPost]
        async public Task<object> 提交纵向项目任务书电子签章([FromBody]纵向项目任务书电子签章请求 model)
        {
            if (model == null || model.任务书编号 <= 0 || string.IsNullOrWhiteSpace(model.幂等键))
                throw new ArgumentException("任务书编号和幂等键不能为空");
            if (model.幂等键.Trim().Length > 120) throw new ArgumentException("幂等键不能超过120个字符");
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办"))
                return StatusCode(403, new { error = "无权提交正式电子签章" });
            var taskbook = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"",""纵向项目立项编号"",""状态"" FROM ""纵向项目任务书"" WHERE ""编号""=@编号;", new { 编号 = model.任务书编号 });
            if (taskbook == null) return NotFound(new { error = "任务书不存在" });
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, (int)taskbook.纵向项目立项编号)) return ProjectAccessDenied();
            var status = 读取外部文档服务契约("ElectronicSignature", "电子签章");
            if (!status.可用) return 正式文档服务未就绪(status);
            return StatusCode(501, new
            {
                code = "DOCUMENT_PROVIDER_ADAPTER_NOT_IMPLEMENTED",
                error = "电子签章适配器尚未在本版本实现；未调用外部服务，也未改变任务书状态。",
                任务书编号 = model.任务书编号,
                幂等键 = model.幂等键.Trim()
            });
        }

        /// <summary>
        /// 正式电子档案提交门禁。目前不写入已托管状态；本地档案清单仍可用于内部留存和核验。
        /// </summary>
        [HttpPost]
        async public Task<object> 提交纵向项目正式电子档案([FromBody]纵向项目正式电子档案提交请求 model)
        {
            if (model == null || model.纵向项目立项编号 <= 0 || string.IsNullOrWhiteSpace(model.幂等键))
                throw new ArgumentException("项目编号和幂等键不能为空");
            if (model.幂等键.Trim().Length > 120) throw new ArgumentException("幂等键不能超过120个字符");
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办"))
                return StatusCode(403, new { error = "无权提交正式电子档案" });
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, model.纵向项目立项编号)) return ProjectAccessDenied();
            var status = 读取外部文档服务契约("ElectronicArchive", "正式电子档案");
            if (!status.可用) return 正式文档服务未就绪(status);
            return StatusCode(501, new
            {
                code = "DOCUMENT_PROVIDER_ADAPTER_NOT_IMPLEMENTED",
                error = "正式电子档案适配器尚未在本版本实现；未调用外部服务，也未改变本地档案状态。",
                纵向项目立项编号 = model.纵向项目立项编号,
                幂等键 = model.幂等键.Trim()
            });
        }
        #endregion

        [HttpPost]
        async public Task<object> 一键建立配套项目([FromBody]一键建立配套项目请求 model)
        {
            if (model == null || model.来源项目编号 <= 0) throw new ArgumentException("来源项目编号不能为空");
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, model.来源项目编号)) return ProjectAccessDenied();

            var jsonStr = await Db.QuerySingleAsync<string>(
                "SELECT \"sp_一键建立配套项目\"(@来源项目编号, @配套经费, @操作人编号)::text;",
                new { 来源项目编号 = model.来源项目编号, 配套经费 = model.配套经费, 操作人编号 = CurrentUser.编号 });
            return Newtonsoft.Json.JsonConvert.DeserializeObject(jsonStr);
        }

        [HttpPost]
        async public Task<object> 一键建立结余项目([FromBody]一键建立结余项目请求 model)
        {
            if (model == null || model.来源项目编号 <= 0) throw new ArgumentException("来源项目编号不能为空");
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, model.来源项目编号)) return ProjectAccessDenied();

            var jsonStr = await Db.QuerySingleAsync<string>(
                "SELECT \"sp_一键建立结余项目\"(@来源项目编号, @结余金额, @操作人编号)::text;",
                new { 来源项目编号 = model.来源项目编号, 结余金额 = model.结余金额, 操作人编号 = CurrentUser.编号 });
            return Newtonsoft.Json.JsonConvert.DeserializeObject(jsonStr);
        }

        [HttpGet]
        async public Task<object> 获取过程预警巡检台账([FromQuery] int? projectId, [FromQuery] string alertType, [FromQuery] string status)
        {
            var sql = @"
SELECT ""编号"", ""项目类型"", ""项目编号"", ""项目名称"", ""负责人编号"", ""负责人姓名"",
       ""预警类型"", ""预警级别"", ""节点截止日期"", ""剩余天数"", ""当前预算执行率"",
       ""预警提示内容"", ""处理状态"", ""通知渠道"", ""最近巡检时间"", ""建立时间""
  FROM ""科研项目过程预警记录""
 WHERE (@projectId IS NULL OR ""项目编号"" = @projectId)
   AND (@alertType IS NULL OR ""预警类型"" = @alertType)
   AND (@status IS NULL OR ""处理状态"" = @status)
 ORDER BY CASE ""预警级别"" WHEN '红色督办/阻断' THEN 1 WHEN '黄色预警' THEN 2 ELSE 3 END, ""剩余天数"" ASC;";

            var list = await Db.QueryAsync<过程预警记录项>(sql, new { projectId, alertType, status });
            return list;
        }

        [HttpPost]
        async public Task<object> 触发过程主动巡检()
        {
            var jsonStr = await Db.QuerySingleAsync<string>("SELECT \"sp_科研过程主动巡检与预警生成\"()::text;");
            return Newtonsoft.Json.JsonConvert.DeserializeObject(jsonStr);
        }

        [HttpPost]
        async public Task<object> 确认过程预警([FromBody] 确认过程预警请求 model)
        {
            if (model == null || model.预警编号 <= 0) throw new ArgumentException("预警编号不能为空");

            var remarkSuffix = string.IsNullOrWhiteSpace(model.处理说明) ? "" : $" [处理说明: {model.处理说明.Trim()}]";
            var count = await Db.ExecuteAsync(@"
UPDATE ""科研项目过程预警记录""
   SET ""处理状态"" = '已确认',
       ""预警提示内容"" = ""预警提示内容"" || @remarkSuffix,
       ""最近巡检时间"" = CURRENT_TIMESTAMP
 WHERE ""编号"" = @预警编号;", new { 预警编号 = model.预警编号, remarkSuffix });

            return new { success = count > 0, message = count > 0 ? "过程预警已确认" : "未找到指定预警记录" };
        }

        [HttpGet]
        async public Task<object> 项目过程前置合规检查([FromQuery] int projectId, [FromQuery] string checkType = "中检")
        {
            if (projectId <= 0) throw new ArgumentException("项目编号不能为空");

            var jsonStr = await Db.QuerySingleAsync<string>(
                "SELECT \"sp_项目过程前置合规检查\"(@projectId, @checkType)::text;",
                new { projectId, checkType });
            return Newtonsoft.Json.JsonConvert.DeserializeObject(jsonStr);
        }

        [HttpPost]
        async public Task<object> 登记伦理批文续展([FromBody] 登记伦理批文续展请求 model)
        {
            if (model == null || model.项目编号 <= 0) throw new ArgumentException("项目编号不能为空");
            if (string.IsNullOrWhiteSpace(model.新批文编号)) throw new ArgumentException("新批文编号不能为空");

            model.登记人编号 = CurrentUser.编号;
            var payloadJson = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                项目编号 = model.项目编号,
                新批文编号 = model.新批文编号.Trim(),
                申请类型 = string.IsNullOrWhiteSpace(model.申请类型) ? "延期续展" : model.申请类型.Trim(),
                新有效期至 = model.新有效期至.ToString("yyyy-MM-dd"),
                批件文件路径 = model.批件文件路径,
                变更事由与说明 = model.变更事由与说明,
                登记人编号 = model.登记人编号
            });

            var jsonStr = await Db.QuerySingleAsync<string>(
                "SELECT \"sp_登记伦理批文续展与修正案\"(@payload::jsonb)::text;",
                new { payload = payloadJson });
            return Newtonsoft.Json.JsonConvert.DeserializeObject(jsonStr);
        }
        #endregion
        #endregion
    }
}
