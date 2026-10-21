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
    /// 经费执行治理：外拨经费、跨年结转、工作量与绩效分割、结项结余处理及审计追溯
    /// </summary>
    public sealed class FundExecutionController : BaseController
    {
        private async Task<bool> IsFinanceOrManager() =>
            await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办", "财务管理员", "财务");

        private static string NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        #region 外拨经费管理

        [HttpGet]
        public async Task<object> 查询外拨经费(int 流程模板编号, int 项目编号)
        {
            if (流程模板编号 <= 0 || 项目编号 <= 0) throw new ArgumentException("项目参数无效");
            if (!await CanAccessProjectAsync(流程模板编号, 项目编号)) return ProjectAccessDenied();

            var list = await Db.QueryAsync<dynamic>(@"
SELECT e.*, p.""姓名"" as ""申请人姓名"", a.""姓名"" as ""审核人姓名""
FROM ""外拨经费"" e
LEFT JOIN ""人员"" p ON e.""申请人编号"" = p.""编号""
LEFT JOIN ""人员"" a ON e.""审核人编号"" = a.""编号""
WHERE e.""流程模板编号""=@流程模板编号 AND e.""项目编号""=@项目编号
ORDER BY e.""创建时间"" DESC;", new { 流程模板编号, 项目编号 });

            return list;
        }

        [HttpPost]
        public async Task<object> 保存外拨经费([FromBody] OutsourceFundSaveModel model)
        {
            if (model == null || model.流程模板编号 <= 0 || model.项目编号 <= 0 || model.外拨金额 <= 0)
                throw new ArgumentException("外拨申请参数无效");
            if (string.IsNullOrWhiteSpace(model.合作单位名称))
                throw new ArgumentException("合作单位名称不能为空");

            if (!await CanAccessProjectAsync(model.流程模板编号, model.项目编号)) return ProjectAccessDenied();

            using var trans = Db.BeginTransaction();
            try
            {
                long id;
                string prevStatus = null;
                if (model.编号 > 0)
                {
                    var exist = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT * FROM ""外拨经费"" WHERE ""编号""=@编号 FOR UPDATE;", new { model.编号 }, trans);
                    if (exist == null) return NotFound(new { error = "外拨记录不存在" });
                    if (exist.状态 != "草稿" && exist.状态 != "已驳回")
                        return StatusCode(409, new { error = "当前状态不允许修改" });

                    prevStatus = exist.状态;
                    id = model.编号;
                    await Db.ExecuteAsync(@"
UPDATE ""外拨经费""
SET ""经费卡编号""=@经费卡编号, ""合作单位名称""=@合作单位名称, ""统一社会信用代码""=@统一社会信用代码,
    ""外拨合同编号""=@外拨合同编号, ""外拨合同路径""=@外拨合同路径, ""外拨金额""=@外拨金额,
    ""更新时间""=CURRENT_TIMESTAMP
WHERE ""编号""=@id;", new {
                        id,
                        model.经费卡编号,
                        model.合作单位名称,
                        统一社会信用代码 = NullIfEmpty(model.统一社会信用代码),
                        外拨合同编号 = NullIfEmpty(model.外拨合同编号),
                        外拨合同路径 = NullIfEmpty(model.外拨合同路径),
                        model.外拨金额
                    }, trans);
                }
                else
                {
                    id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""外拨经费"" (""流程模板编号"",""项目编号"",""经费卡编号"",""合作单位名称"",""统一社会信用代码"",""外拨合同编号"",""外拨合同路径"",""外拨金额"",""状态"",""申请人编号"")
VALUES (@流程模板编号,@项目编号,@经费卡编号,@合作单位名称,@统一社会信用代码,@外拨合同编号,@外拨合同路径,@外拨金额,'草稿',@申请人编号)
RETURNING ""编号"";", new {
                        model.流程模板编号,
                        model.项目编号,
                        model.经费卡编号,
                        model.合作单位名称,
                        统一社会信用代码 = NullIfEmpty(model.统一社会信用代码),
                        外拨合同编号 = NullIfEmpty(model.外拨合同编号),
                        外拨合同路径 = NullIfEmpty(model.外拨合同路径),
                        model.外拨金额,
                        申请人编号 = CurrentUser.编号
                    }, trans);
                }

                // 记录审计
                await LogExecutionAudit(trans, "外拨经费", id, "保存", prevStatus, "草稿", model);
                trans.Commit();
                return new { 编号 = id, 状态 = "草稿" };
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        [HttpPost]
        public async Task<object> 审核外拨经费([FromBody] FundAuditModel model)
        {
            if (!await IsFinanceOrManager()) return StatusCode(403, new { error = "无权审核外拨经费" });
            if (model == null || model.编号 <= 0 || model.审核结果 is not ("已批准" or "已驳回"))
                throw new ArgumentException("审核参数无效");

            using var trans = Db.BeginTransaction();
            try
            {
                var record = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT * FROM ""外拨经费"" WHERE ""编号""=@编号 FOR UPDATE;", new { model.编号 }, trans);
                if (record == null) return NotFound(new { error = "外拨记录不存在" });
                if (record.状态 != "草稿" && record.状态 != "审核中")
                    return StatusCode(409, new { error = $"当前状态【{record.状态}】不可进行此审核" });

                string nextStatus = model.审核结果;
                await Db.ExecuteAsync(@"
UPDATE ""外拨经费""
SET ""状态""=@nextStatus, ""审核人编号""=@审核人编号, ""审核时间""=CURRENT_TIMESTAMP, ""审核意见""=@审核意见, ""更新时间""=CURRENT_TIMESTAMP
WHERE ""编号""=@编号;", new {
                    model.编号,
                    nextStatus,
                    审核人编号 = CurrentUser.编号,
                    审核意见 = NullIfEmpty(model.审核意见)
                }, trans);

                await LogExecutionAudit(trans, "外拨经费", model.编号, "审核", (string)record.状态, nextStatus, model);
                trans.Commit();
                return new { model.编号, 状态 = nextStatus };
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        #endregion

        #region 经费结转管理

        [HttpGet]
        public async Task<object> 查询经费结转(int 流程模板编号, int 项目编号)
        {
            if (流程模板编号 <= 0 || 项目编号 <= 0) throw new ArgumentException("项目参数无效");
            if (!await CanAccessProjectAsync(流程模板编号, 项目编号)) return ProjectAccessDenied();

            return await Db.QueryAsync<dynamic>(@"
SELECT c.*, p.""姓名"" as ""申请人姓名"", a.""姓名"" as ""审核人姓名""
FROM ""经费结转"" c
LEFT JOIN ""人员"" p ON c.""申请人编号"" = p.""编号""
LEFT JOIN ""人员"" a ON c.""审核人编号"" = a.""编号""
WHERE c.""流程模板编号""=@流程模板编号 AND c.""项目编号""=@项目编号
ORDER BY c.""创建时间"" DESC;", new { 流程模板编号, 项目编号 });
        }

        [HttpPost]
        public async Task<object> 申请经费结转([FromBody] FundCarryoverApplyModel model)
        {
            if (model == null || model.流程模板编号 <= 0 || model.项目编号 <= 0 || model.结转金额 <= 0)
                throw new ArgumentException("结转申请参数无效");
            if (model.结转目标年度 != model.结转来源年度 + 1)
                throw new ArgumentException("结转目标年度必须为来源年度的下一个自然年度");

            if (!await CanAccessProjectAsync(model.流程模板编号, model.项目编号)) return ProjectAccessDenied();

            using var trans = Db.BeginTransaction();
            try
            {
                var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""经费结转"" (""流程模板编号"",""项目编号"",""经费卡编号"",""结转来源年度"",""结转目标年度"",""可结转余额快照"",""结转金额"",""状态"",""申请人编号"",""申请说明"")
VALUES (@流程模板编号,@项目编号,@经费卡编号,@结转来源年度,@结转目标年度,@可结转余额快照,@结转金额,'草稿',@申请人编号,@申请说明)
RETURNING ""编号"";", new {
                    model.流程模板编号,
                    model.项目编号,
                    model.经费卡编号,
                    model.结转来源年度,
                    model.结转目标年度,
                    model.可结转余额快照,
                    model.结转金额,
                    申请人编号 = CurrentUser.编号,
                    申请说明 = NullIfEmpty(model.申请说明)
                }, trans);

                await LogExecutionAudit(trans, "经费结转", id, "申请", null, "草稿", model);
                trans.Commit();
                return new { 编号 = id, 状态 = "草稿" };
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        #endregion

        #region 经费分割方案

        [HttpGet]
        public async Task<object> 查询经费分割方案(int 流程模板编号, int 项目编号)
        {
            if (流程模板编号 <= 0 || 项目编号 <= 0) throw new ArgumentException("项目参数无效");
            if (!await CanAccessProjectAsync(流程模板编号, 项目编号)) return ProjectAccessDenied();

            var schemes = (await Db.QueryAsync<dynamic>(@"
SELECT s.*, p.""姓名"" as ""制定人姓名""
FROM ""经费分割方案"" s
LEFT JOIN ""人员"" p ON s.""制定人编号"" = p.""编号""
WHERE s.""流程模板编号""=@流程模板编号 AND s.""项目编号""=@项目编号
ORDER BY s.""创建时间"" DESC;", new { 流程模板编号, 项目编号 })).ToList();

            if (!schemes.Any()) return schemes;

            var schemeIds = schemes.Select(s => (long)s.编号).ToArray();
            var details = await Db.QueryAsync<dynamic>(@"
SELECT d.*, p.""姓名"" as ""人员姓名""
FROM ""经费分割明细"" d
LEFT JOIN ""人员"" p ON d.""人员编号"" = p.""编号""
WHERE d.""方案编号"" = ANY(@schemeIds)
ORDER BY d.""方案编号"", d.""分割比例"" DESC;", new { schemeIds });

            var grouped = details.GroupBy(d => (long)d.方案编号).ToDictionary(g => g.Key, g => g.ToList());
            return schemes.Select(s => new {
                s.编号,
                s.流程模板编号,
                s.项目编号,
                s.分割类型,
                s.基数金额,
                s.期间或年度,
                s.状态,
                s.制定人姓名,
                s.创建时间,
                明细 = grouped.ContainsKey((long)s.编号) ? grouped[(long)s.编号] : new List<dynamic>()
            });
        }

        [HttpPost]
        public async Task<object> 保存经费分割方案([FromBody] FundSplitSchemeSaveModel model)
        {
            if (model == null || model.流程模板编号 <= 0 || model.项目编号 <= 0 || model.基数金额 <= 0)
                throw new ArgumentException("分割方案参数无效");
            if (model.明细 == null || !model.明细.Any())
                throw new ArgumentException("必须至少指定一条人员分割明细");

            // 服务端校验比例合计严格等于 100%
            decimal totalPercent = model.明细.Sum(x => x.分割比例);
            if (Math.Abs(totalPercent - 100.00m) > 0.01m)
                throw new ArgumentException($"人员分割比例合计必须为100%，当前为 {totalPercent}%");

            if (!await CanAccessProjectAsync(model.流程模板编号, model.项目编号)) return ProjectAccessDenied();

            using var trans = Db.BeginTransaction();
            try
            {
                long schemeId;
                if (model.编号 > 0)
                {
                    schemeId = model.编号;
                    await Db.ExecuteAsync(@"
UPDATE ""经费分割方案""
SET ""分割类型""=@分割类型, ""基数金额""=@基数金额, ""期间或年度""=@期间或年度, ""备注""=@备注
WHERE ""编号""=@schemeId;", new {
                        schemeId,
                        model.分割类型,
                        model.基数金额,
                        model.期间或年度,
                        备注 = NullIfEmpty(model.备注)
                    }, trans);

                    await Db.ExecuteAsync(@"DELETE FROM ""经费分割明细"" WHERE ""方案编号""=@schemeId;", new { schemeId }, trans);
                }
                else
                {
                    schemeId = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""经费分割方案"" (""流程模板编号"",""项目编号"",""分割类型"",""基数金额"",""期间或年度"",""状态"",""制定人编号"",""备注"")
VALUES (@流程模板编号,@项目编号,@分割类型,@基数金额,@期间或年度,'草稿',@制定人编号,@备注)
RETURNING ""编号"";", new {
                        model.流程模板编号,
                        model.项目编号,
                        model.分割类型,
                        model.基数金额,
                        model.期间或年度,
                        制定人编号 = CurrentUser.编号,
                        备注 = NullIfEmpty(model.备注)
                    }, trans);
                }

                decimal allocatedAmount = 0;
                for (int i = 0; i < model.明细.Count; i++)
                {
                    var it = model.明细[i];
                    decimal itemAmount = Math.Round(model.基数金额 * (it.分割比例 / 100.0m), 2);
                    if (i == model.明细.Count - 1)
                    {
                        itemAmount = model.基数金额 - allocatedAmount;
                    }
                    else
                    {
                        allocatedAmount += itemAmount;
                    }

                    await Db.ExecuteAsync(@"
INSERT INTO ""经费分割明细"" (""方案编号"",""人员编号"",""部门名称"",""分割比例"",""分割金额"",""分配说明"")
VALUES (@schemeId, @人员编号, @部门名称, @分割比例, @itemAmount, @分配说明);", new {
                        schemeId,
                        it.人员编号,
                        部门名称 = NullIfEmpty(it.部门名称),
                        it.分割比例,
                        itemAmount,
                        分配说明 = NullIfEmpty(it.分配说明)
                    }, trans);
                }

                await LogExecutionAudit(trans, "经费分割", schemeId, "保存方案", null, "草稿", model);
                trans.Commit();
                return new { 编号 = schemeId };
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        #endregion

        #region 结项结余处理

        [HttpGet]
        public async Task<object> 查询结项结余(int 流程模板编号, int 项目编号)
        {
            if (流程模板编号 <= 0 || 项目编号 <= 0) throw new ArgumentException("项目参数无效");
            if (!await CanAccessProjectAsync(流程模板编号, 项目编号)) return ProjectAccessDenied();

            return await Db.QueryAsync<dynamic>(@"
SELECT * FROM ""结项结余处理""
WHERE ""流程模板编号""=@流程模板编号 AND ""项目编号""=@项目编号
ORDER BY ""创建时间"" DESC;", new { 流程模板编号, 项目编号 });
        }

        [HttpPost]
        public async Task<object> 登记结项结余([FromBody] ProjectBalanceDisposalModel model)
        {
            if (!await IsFinanceOrManager()) return StatusCode(403, new { error = "无权登记结项结余处理" });
            if (model == null || model.流程模板编号 <= 0 || model.项目编号 <= 0 || model.结余金额快照 < 0)
                throw new ArgumentException("结余处理参数无效");

            using var trans = Db.BeginTransaction();
            try
            {
                var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""结项结余处理"" (""流程模板编号"",""项目编号"",""结余金额快照"",""处理方式"",""后续流程模板编号"",""后续项目编号"",""处理说明"",""状态"",""经办人编号"",""财务凭证号"")
VALUES (@流程模板编号,@项目编号,@结余金额快照,@处理方式,@后续流程模板编号,@后续项目编号,@处理说明,'草稿',@经办人编号,@财务凭证号)
RETURNING ""编号"";", new {
                    model.流程模板编号,
                    model.项目编号,
                    model.结余金额快照,
                    model.处理方式,
                    model.后续流程模板编号,
                    model.后续项目编号,
                    处理说明 = NullIfEmpty(model.处理说明),
                    经办人编号 = CurrentUser.编号,
                    财务凭证号 = NullIfEmpty(model.财务凭证号)
                }, trans);

                await LogExecutionAudit(trans, "结项结余", id, "登记", null, "草稿", model);
                trans.Commit();
                return new { 编号 = id };
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        #endregion

        private async Task LogExecutionAudit(System.Data.IDbTransaction trans, string type, long id, string action, string fromState, string toState, object snapshot)
        {
            await Db.ExecuteAsync(@"
INSERT INTO ""经费执行审计"" (""业务类型"",""业务主键"",""操作动作"",""变动前状态"",""变动后状态"",""操作人编号"",""详细快照"")
VALUES (@type, @id, @action, @fromState, @toState, @userId, @snap::jsonb);", new {
                type,
                id,
                action,
                fromState,
                toState,
                userId = CurrentUser.编号,
                snap = JsonSerializer.Serialize(snapshot)
            }, trans);
        }
    }

    public sealed class OutsourceFundSaveModel
    {
        public long 编号 { get; set; }
        public int 流程模板编号 { get; set; }
        public int 项目编号 { get; set; }
        public long? 经费卡编号 { get; set; }
        public string 合作单位名称 { get; set; }
        public string 统一社会信用代码 { get; set; }
        public string 外拨合同编号 { get; set; }
        public string 外拨合同路径 { get; set; }
        public decimal 外拨金额 { get; set; }
    }

    public sealed class FundAuditModel
    {
        public long 编号 { get; set; }
        public string 审核结果 { get; set; }
        public string 审核意见 { get; set; }
    }

    public sealed class FundCarryoverApplyModel
    {
        public int 流程模板编号 { get; set; }
        public int 项目编号 { get; set; }
        public long? 经费卡编号 { get; set; }
        public int 结转来源年度 { get; set; }
        public int 结转目标年度 { get; set; }
        public decimal 可结转余额快照 { get; set; }
        public decimal 结转金额 { get; set; }
        public string 申请说明 { get; set; }
    }

    public sealed class FundSplitSchemeSaveModel
    {
        public long 编号 { get; set; }
        public int 流程模板编号 { get; set; }
        public int 项目编号 { get; set; }
        public string 分割类型 { get; set; }
        public decimal 基数金额 { get; set; }
        public string 期间或年度 { get; set; }
        public string 备注 { get; set; }
        public List<FundSplitDetailItem> 明细 { get; set; }
    }

    public sealed class FundSplitDetailItem
    {
        public int 人员编号 { get; set; }
        public string 部门名称 { get; set; }
        public decimal 分割比例 { get; set; }
        public string 分配说明 { get; set; }
    }

    public sealed class ProjectBalanceDisposalModel
    {
        public int 流程模板编号 { get; set; }
        public int 项目编号 { get; set; }
        public decimal 结余金额快照 { get; set; }
        public string 处理方式 { get; set; }
        public int? 后续流程模板编号 { get; set; }
        public int? 后续项目编号 { get; set; }
        public string 处理说明 { get; set; }
        public string 财务凭证号 { get; set; }
    }
}
