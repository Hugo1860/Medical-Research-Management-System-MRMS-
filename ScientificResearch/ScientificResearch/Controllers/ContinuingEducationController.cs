using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ScientificResearch.Infrastucture;
using ScientificResearch.Models;

namespace ScientificResearch.Controllers;

/// <summary>P0 人员进修管理：年度计划、个人申请、预算和可配置审批。</summary>
[Route("ContinuingEducation/[action]")]
[Route("FurtherEducation/[action]")]
public sealed class FurtherEducationController : BaseController
{
    private static readonly string[] 管理角色 = { "系统管理员", "科研管理员", "科研科负责人", "科教科" };
    private static readonly string[] 科教角色 = { "系统管理员", "科研管理员", "科研科负责人", "科研秘书", "科教科" };

    private Task<bool> IsManager() => HasAnyRoleAsync(管理角色);
    private Task<bool> IsEducationStaff() => HasAnyRoleAsync(科教角色);

    [HttpGet]
    public async Task<IEnumerable<object>> 查询进修年度计划(int? 年度 = null, int? 部门编号 = null, string 状态 = null)
    {
        var manager = await IsManager();
        return await Db.QueryAsync<object>(@"
SELECT p.*, d.""名称"" AS ""部门名称"", pe.""姓名"" AS ""创建人姓名""
FROM ""进修年度计划"" p
LEFT JOIN ""部门"" d ON d.""编号""=p.""部门编号""
LEFT JOIN ""人员"" pe ON pe.""编号""=p.""创建人编号""
WHERE (@年度 IS NULL OR p.""年度""=@年度)
  AND (@部门编号 IS NULL OR p.""部门编号""=@部门编号)
  AND (@状态 IS NULL OR p.""状态""=@状态)
  AND (@管理角色 OR p.""部门编号""=@当前部门)
ORDER BY p.""年度"" DESC,p.""部门编号"",p.""编号"" DESC;", new
        {
            年度, 部门编号, 状态 = 空值(状态), 管理角色 = manager, 当前部门 = CurrentUser.部门编号
        });
    }

    // 兼容现代前端使用的简短动作名。
    [HttpGet("~/ContinuingEducation/查询年度计划")]
    [HttpGet("~/FurtherEducation/查询年度计划")]
    public Task<IEnumerable<object>> 查询年度计划别名(int? 计划年度 = null, int? 科室编号 = null, string 状态 = null) =>
        查询进修年度计划(计划年度, 科室编号, 状态);

    [HttpPost]
    public async Task<object> 保存进修年度计划([FromBody] 进修年度计划保存 model)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "仅科研管理人员可以维护进修年度计划" });
        if (model == null || model.年度 < DateTime.UtcNow.Year - 1 || model.年度 > DateTime.UtcNow.Year + 10)
            throw new ArgumentException("计划年度无效");
        if (string.IsNullOrWhiteSpace(model.专业方向)) throw new ArgumentException("专业方向不能为空");
        if (!await Db.QuerySingleAsync<bool>("SELECT EXISTS(SELECT 1 FROM \"部门\" WHERE \"编号\"=@部门编号 AND \"是否启用\"=TRUE);", new { model.部门编号 }))
            throw new ArgumentException("科室不存在或已停用");
        var amount = Math.Round(model.预算总额, 2);
        if (model.编号.HasValue && model.编号.Value > 0)
        {
            var changed = await Db.ExecuteAsync(@"
UPDATE ""进修年度计划"" SET ""年度""=@年度,""部门编号""=@部门编号,""专业方向""=@专业方向,
       ""计划人数""=@计划人数,""预算总额""=@预算总额,""备注""=@备注,""更新时间""=CURRENT_TIMESTAMP
WHERE ""编号""=@编号 AND ""状态""='草稿' AND ""已占用人数""=0 AND ""已核销金额""=0;", new
            {
                model.编号, model.年度, model.部门编号, 专业方向 = model.专业方向.Trim(), model.计划人数, 预算总额 = amount,
                备注 = 空值(model.备注)
            });
            if (changed == 0) return BadRequest(new { error = "计划不存在、已发布或已有预算占用，不能修改" });
            await 记录审计(null, "年度计划修改", null, "草稿", $"计划编号={model.编号}");
            return new { 编号 = model.编号.Value, message = "进修年度计划已保存" };
        }
        var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""进修年度计划"" (""年度"",""部门编号"",""专业方向"",""计划人数"",""预算总额"",""备注"",""创建人编号"")
VALUES (@年度,@部门编号,@专业方向,@计划人数,@预算总额,@备注,@创建人编号) RETURNING ""编号"";", new
        {
            model.年度, model.部门编号, 专业方向 = model.专业方向.Trim(), model.计划人数, 预算总额 = amount,
            备注 = 空值(model.备注), 创建人编号 = CurrentUser.编号
        });
        await 记录审计(null, "年度计划创建", null, "草稿", $"计划编号={id}");
        return new { 编号 = id, message = "进修年度计划已创建" };
    }

    [HttpPost]
    public async Task<object> 发布进修年度计划(long 计划编号)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "仅科研管理人员可以发布年度计划" });
        var changed = await Db.ExecuteAsync(@"UPDATE ""进修年度计划"" SET ""状态""='已发布',""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@计划编号 AND ""状态""='草稿';", new { 计划编号 });
        if (changed == 0) return BadRequest(new { error = "计划不存在或当前状态不可发布" });
        await 记录审计(null, "年度计划发布", "草稿", "已发布", $"计划编号={计划编号}");
        return new { 编号 = 计划编号, 状态 = "已发布" };
    }

    [HttpPost("~/ContinuingEducation/保存年度计划")]
    [HttpPost("~/FurtherEducation/保存年度计划")]
    public Task<object> 保存年度计划别名([FromBody] 进修年度计划保存 model) => 保存进修年度计划(model);

    [HttpPost("~/ContinuingEducation/发布年度计划")]
    [HttpPost("~/FurtherEducation/发布年度计划")]
    public async Task<object> 发布年度计划别名([FromBody] 进修编号请求 model) =>
        发布进修年度计划(model?.编号 ?? model?.计划编号 ?? 0);

    [HttpGet]
    public async Task<IEnumerable<object>> 查询进修申请(long? 编号 = null, int? 年度 = null, string 状态 = null)
    {
        var manager = await IsEducationStaff();
        return await Db.QueryAsync<object>(@"
SELECT a.*,p.""专业方向"",d.""名称"" AS ""部门名称"",u.""姓名"" AS ""申请人姓名"",
       CASE WHEN t.""编号"" IS NULL THEN 0 ELSE 1 END AS ""有待办""
FROM ""进修申请"" a
LEFT JOIN ""进修年度计划"" p ON p.""编号""=a.""年度计划编号""
LEFT JOIN ""部门"" d ON d.""编号""=a.""部门编号""
LEFT JOIN ""人员"" u ON u.""编号""=a.""申请人编号""
LEFT JOIN LATERAL (SELECT ""编号"" FROM ""进修审批待办"" WHERE ""进修申请编号""=a.""编号"" AND ""状态""='待处理' LIMIT 1) t ON TRUE
WHERE (@编号 IS NULL OR a.""编号""=@编号)
  AND (@年度 IS NULL OR COALESCE(p.""年度"",EXTRACT(YEAR FROM a.""进修开始日期"")::int)=@年度)
  AND (@状态 IS NULL OR a.""状态""=@状态)
  AND (@管理角色 OR a.""申请人编号""=@当前人员 OR EXISTS (SELECT 1 FROM ""进修审批待办"" x WHERE x.""进修申请编号""=a.""编号"" AND x.""状态""='待处理' AND (x.""接收人编号""=@当前人员 OR EXISTS (SELECT 1 FROM ""人员角色"" pr JOIN ""角色"" r ON r.""编号""=pr.""角色编号"" WHERE pr.""人员编号""=@当前人员 AND r.""名称""=x.""接收角色""))))
ORDER BY a.""更新时间"" DESC,a.""编号"" DESC;", new
        {
            编号, 年度, 状态 = 空值(状态), 管理角色 = manager, 当前人员 = CurrentUser.编号
        });
    }

    [HttpGet]
    public async Task<object> 获取进修申请(long 编号)
    {
        var rows = await 查询进修申请(编号, null, null);
        return rows.FirstOrDefault() ?? NotFound(new { error = "进修申请不存在或无权查看" });
    }

    [HttpPost]
    public async Task<object> 保存进修申请([FromBody] 进修申请保存 model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.目标医院) || string.IsNullOrWhiteSpace(model.进修专业)) throw new ArgumentException("目标医院和进修专业不能为空");
        if (model.进修结束日期.Date < model.进修开始日期.Date) throw new ArgumentException("进修结束日期不能早于开始日期");
        var days = (model.进修结束日期.Date - model.进修开始日期.Date).Days + 1;
        var fee = Math.Round(model.培训费 + model.交通费 + model.住宿费 + model.补助费, 2);
        var manager = await IsEducationStaff();
        var dept = manager ? model.部门编号 : CurrentUser.部门编号;
        if (dept <= 0) throw new ArgumentException("申请科室无效");
        if (model.年度计划编号.HasValue && !await Db.QuerySingleAsync<bool>("SELECT EXISTS(SELECT 1 FROM \"进修年度计划\" WHERE \"编号\"=@编号 AND \"部门编号\"=@部门编号 AND \"状态\" IN ('草稿','已发布','已锁定'));", new { 编号 = model.年度计划编号, 部门编号 = dept }))
            throw new ArgumentException("年度计划不存在、科室不匹配或当前不可使用");
        if (model.编号.HasValue && model.编号.Value > 0)
        {
            var changed = await Db.ExecuteAsync(@"
UPDATE ""进修申请"" SET ""年度计划编号""=@年度计划编号,""部门编号""=@部门编号,""目标医院""=@目标医院,""进修专业""=@进修专业,
       ""进修开始日期""=@进修开始日期,""进修结束日期""=@进修结束日期,""进修天数""=@进修天数,""培训费""=@培训费,""交通费""=@交通费,""住宿费""=@住宿费,""补助费""=@补助费,""预算金额""=@预算金额,""经费来源""=@经费来源,""申请说明""=@申请说明,""附件路径""=@附件路径,""备注""=@备注,""更新时间""=CURRENT_TIMESTAMP
WHERE ""编号""=@编号 AND ""申请人编号""=@申请人编号 AND ""状态"" IN ('草稿','已退回');", new
            {
                model.编号, model.年度计划编号, 部门编号 = dept, 目标医院 = model.目标医院.Trim(), 进修专业 = model.进修专业.Trim(),
                进修开始日期 = model.进修开始日期.Date, 进修结束日期 = model.进修结束日期.Date, 进修天数 = days,
                model.培训费, model.交通费, model.住宿费, model.补助费, 预算金额 = fee, 经费来源 = 空值(model.经费来源), 申请说明 = 空值(model.申请说明), 附件路径 = 空值(model.附件路径), 备注 = 空值(model.备注), 申请人编号 = CurrentUser.编号
            });
            if (changed == 0) return BadRequest(new { error = "申请不存在、非本人申请或当前状态不可修改" });
            await 记录审计(model.编号.Value, "申请修改", null, "草稿", null);
            return new { 编号 = model.编号.Value, 状态 = "草稿" };
        }
        var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""进修申请"" (""年度计划编号"",""申请人编号"",""部门编号"",""目标医院"",""进修专业"",""进修开始日期"",""进修结束日期"",""进修天数"",""培训费"",""交通费"",""住宿费"",""补助费"",""预算金额"",""经费来源"",""申请说明"",""附件路径"",""备注"")
VALUES (@年度计划编号,@申请人编号,@部门编号,@目标医院,@进修专业,@进修开始日期,@进修结束日期,@进修天数,@培训费,@交通费,@住宿费,@补助费,@预算金额,@经费来源,@申请说明,@附件路径,@备注) RETURNING ""编号"";", new
        {
            model.年度计划编号, 申请人编号 = CurrentUser.编号, 部门编号 = dept, 目标医院 = model.目标医院.Trim(), 进修专业 = model.进修专业.Trim(),
            进修开始日期 = model.进修开始日期.Date, 进修结束日期 = model.进修结束日期.Date, 进修天数 = days,
            model.培训费, model.交通费, model.住宿费, model.补助费, 预算金额 = fee, 经费来源 = 空值(model.经费来源), 申请说明 = 空值(model.申请说明), 附件路径 = 空值(model.附件路径), 备注 = 空值(model.备注)
        });
        await 记录审计(id, "申请创建", null, "草稿", null);
        return new { 编号 = id, 状态 = "草稿", 预算金额 = fee };
    }

    [NonAction]
    public async Task<object> 提交进修申请(long 申请编号)
    {
        await using var conn = new NpgsqlConnection(DbConnectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var app = await conn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"SELECT * FROM ""进修申请"" WHERE ""编号""=@申请编号 AND ""申请人编号""=@申请人编号 FOR UPDATE;", new { 申请编号, 申请人编号 = CurrentUser.编号 }, tx));
            if (app == null) return NotFound(new { error = "申请不存在、非本人申请或无权提交" });
            if ((string)app.状态 is not ("草稿" or "已退回")) return BadRequest(new { error = "当前状态不可提交" });
            long? planId = app.年度计划编号;
            if (planId.HasValue)
            {
                var reserved = await conn.QuerySingleOrDefaultAsync<long?>(new CommandDefinition(@"
UPDATE ""进修年度计划"" SET ""已占用人数""=""已占用人数""+1,""已占用预算""=""已占用预算""+@预算金额,""状态""=CASE WHEN ""状态""='已发布' THEN '已锁定' ELSE ""状态"" END,""更新时间""=CURRENT_TIMESTAMP
WHERE ""编号""=@计划编号 AND ""状态"" IN ('已发布','已锁定') AND ""已占用人数"" < ""计划人数"" AND ""已占用预算"" + ""已核销金额"" + @预算金额 <= ""预算总额"" RETURNING ""编号"";", new { 计划编号 = planId.Value, 预算金额 = (decimal)app.预算金额 }, tx));
                if (!reserved.HasValue) return BadRequest(new { error = "年度计划名额或预算余额不足，无法预占" });
            }
            var config = await conn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"SELECT * FROM ""进修审批流程配置"" WHERE ""业务类型""='人员进修' AND ""状态""='生效' ORDER BY ""版本号"" DESC LIMIT 1;", transaction: tx));
            if (config == null) return BadRequest(new { error = "尚未发布进修审批流程配置" });
            var nodes = (await conn.QueryAsync<dynamic>(new CommandDefinition(@"SELECT * FROM ""进修审批流程节点"" WHERE ""流程配置编号""=@编号 AND ""是否启用""=TRUE ORDER BY ""节点序号"";", new { 编号 = (long)config.编号 }, tx))).ToList();
            if (nodes.Count == 0) return BadRequest(new { error = "流程配置至少需要一个启用节点" });
            await conn.ExecuteAsync(new CommandDefinition(@"UPDATE ""进修审批待办"" SET ""状态""='已撤回' WHERE ""进修申请编号""=@申请编号 AND ""状态""='待处理';", new { 申请编号 }, tx));
            var submitCommand = new CommandDefinition(
                @"UPDATE ""进修申请"" SET ""状态""='审批中',""预算状态""=CASE WHEN ""年度计划编号"" IS NULL THEN ""预算状态"" ELSE '已占用' END,""流程配置编号""=@流程配置编号,""当前节点序号""=@当前节点序号,""提交时间""=CURRENT_TIMESTAMP,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@申请编号;",
                new { 申请编号, 流程配置编号 = (long)config.编号, 当前节点序号 = (int)nodes[0].节点序号 }, tx);
            await conn.ExecuteAsync(submitCommand);
            await 创建节点待办(conn, tx, app, nodes[0], 申请编号);
            await 记录审计(conn, tx, 申请编号, CurrentUser.编号, "申请提交", (string)app.状态, "审批中", "预算已预占");
            await tx.CommitAsync();
            return new { 编号 = 申请编号, 状态 = "审批中", 预算状态 = planId.HasValue ? "已占用" : "未占用" };
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    [HttpPost("~/ContinuingEducation/提交进修申请")]
    [HttpPost("~/FurtherEducation/提交进修申请")]
    public Task<object> 提交进修申请请求([FromBody] 进修编号请求 model) =>
        提交进修申请(model?.申请编号 ?? model?.编号 ?? 0);

    [HttpPost]
    public async Task<object> 撤回进修申请(long 申请编号)
    {
        await using var conn = new NpgsqlConnection(DbConnectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var app = await conn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"SELECT * FROM ""进修申请"" WHERE ""编号""=@申请编号 AND ""申请人编号""=@申请人编号 FOR UPDATE;", new { 申请编号, 申请人编号 = CurrentUser.编号 }, tx));
            if (app == null) return NotFound(new { error = "申请不存在或无权撤回" });
            if ((string)app.状态 is not ("草稿" or "审批中" or "已退回")) return BadRequest(new { error = "当前状态不可撤回" });
            await conn.ExecuteAsync(new CommandDefinition("UPDATE \"进修审批待办\" SET \"状态\"='已撤回',\"办理时间\"=CURRENT_TIMESTAMP WHERE \"进修申请编号\"=@申请编号 AND \"状态\"='待处理';", new { 申请编号 }, tx));
            if ((string)app.预算状态 == "已占用") await 释放预算(conn, tx, app);
            await conn.ExecuteAsync(new CommandDefinition("UPDATE \"进修申请\" SET \"状态\"='已撤回',\"预算状态\"=CASE WHEN \"预算状态\"='已占用' THEN '已释放' ELSE \"预算状态\" END,\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@申请编号;", new { 申请编号 }, tx));
            await 记录审计(conn, tx, 申请编号, CurrentUser.编号, "申请撤回", (string)app.状态, "已撤回", null);
            await tx.CommitAsync();
            return new { 编号 = 申请编号, 状态 = "已撤回", 预算状态 = (string)app.预算状态 == "已占用" ? "已释放" : (string)app.预算状态 };
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    [HttpGet]
    public async Task<IEnumerable<object>> 查询进修待办(bool 仅未处理 = true)
    {
        return await Db.QueryAsync<object>(@"
SELECT t.*,a.""申请人编号"",a.""目标医院"",a.""进修专业"",a.""预算金额"",p.""姓名"" AS ""申请人姓名""
FROM ""进修审批待办"" t JOIN ""进修申请"" a ON a.""编号""=t.""进修申请编号"" LEFT JOIN ""人员"" p ON p.""编号""=a.""申请人编号""
WHERE (@仅未处理=FALSE OR t.""状态""='待处理')
  AND (t.""接收人编号""=@人员编号 OR EXISTS (SELECT 1 FROM ""人员角色"" pr JOIN ""角色"" r ON r.""编号""=pr.""角色编号"" WHERE pr.""人员编号""=@人员编号 AND r.""名称""=t.""接收角色"") )
ORDER BY (t.""状态""='待处理') DESC,t.""创建时间"" DESC;", new { 仅未处理, 人员编号 = CurrentUser.编号 });
    }

    [HttpGet("~/ContinuingEducation/获取进修待办")]
    [HttpGet("~/FurtherEducation/获取进修待办")]
    public Task<IEnumerable<object>> 获取进修待办别名(bool 仅未处理 = true) => 查询进修待办(仅未处理);

    /// <summary>查询进修年度预算台账。金额来自年度计划及申请状态，不生成模拟付款记录。</summary>
    [HttpGet]
    public async Task<object> 查询进修预算台账(long? 计划编号 = null, int? 计划年度 = null)
    {
        var manager = await IsEducationStaff();
        var rows = await Db.QueryAsync<object>(@"
SELECT p.""编号"" AS ""计划编号"", p.""年度"" AS ""计划年度"", p.""预算总额"" AS ""批准预算"",
       p.""已占用预算"" AS ""已预占金额"", p.""已核销金额"" AS ""已核销金额"",
       COALESCE((SELECT SUM(a.""预算金额"") FROM ""进修申请"" a
                 WHERE a.""年度计划编号""=p.""编号"" AND a.""状态""='审批中'),0) AS ""审批中金额"",
       GREATEST(p.""预算总额""-p.""已占用预算""-p.""已核销金额"",0) AS ""可用余额"",
       p.""部门编号"", d.""名称"" AS ""部门名称"", p.""专业方向"", p.""状态""
FROM ""进修年度计划"" p
LEFT JOIN ""部门"" d ON d.""编号""=p.""部门编号""
WHERE (@计划编号 IS NULL OR p.""编号""=@计划编号)
  AND (@计划年度 IS NULL OR p.""年度""=@计划年度)
  AND (@管理角色 OR p.""部门编号""=@当前部门)
ORDER BY p.""年度"" DESC,p.""编号"" DESC;", new { 计划编号, 计划年度, 管理角色 = manager, 当前部门 = CurrentUser.部门编号 });
        return rows;
    }

    /// <summary>查询进修操作审计。普通人员只能查看本人申请的记录。</summary>
    [HttpGet]
    public async Task<IEnumerable<object>> 查询进修审计(long? 业务编号 = null, int Limit = 50)
    {
        var manager = await IsEducationStaff();
        var limit = Math.Clamp(Limit, 1, 200);
        return await Db.QueryAsync<object>(@"
SELECT h.""编号"", '人员进修' AS ""业务类型"", h.""进修申请编号"" AS ""业务编号"",
       h.""操作类型"", h.""原状态"", h.""新状态"", h.""说明"", h.""请求编号"",
       h.""创建时间"" AS ""操作时间"", p.""姓名"" AS ""操作人姓名""
FROM ""进修操作审计"" h
LEFT JOIN ""人员"" p ON p.""编号""=h.""操作人编号""
LEFT JOIN ""进修申请"" a ON a.""编号""=h.""进修申请编号""
WHERE (@业务编号 IS NULL OR h.""进修申请编号""=@业务编号)
  AND (@管理角色 OR a.""申请人编号""=@当前人员)
ORDER BY h.""创建时间"" DESC,h.""编号"" DESC
LIMIT @Limit;", new { 业务编号, 管理角色 = manager, 当前人员 = CurrentUser.编号, Limit = limit });
    }

    [HttpPost]
    public async Task<object> 审批进修申请([FromBody] 进修审批请求 model)
    {
        if (model == null || model.待办编号 <= 0 || model.结果 is not ("通过" or "退回" or "驳回")) throw new ArgumentException("审批结果无效");
        await using var conn = new NpgsqlConnection(DbConnectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var todo = await conn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"SELECT t.*,a.""状态"" AS ""申请状态"",a.""预算状态"",a.""年度计划编号"",a.""预算金额"" FROM ""进修审批待办"" t JOIN ""进修申请"" a ON a.""编号""=t.""进修申请编号"" WHERE t.""编号""=@待办编号 AND t.""状态""='待处理' AND (t.""接收人编号""=@人员编号 OR EXISTS (SELECT 1 FROM ""人员角色"" pr JOIN ""角色"" r ON r.""编号""=pr.""角色编号"" WHERE pr.""人员编号""=@人员编号 AND r.""名称""=t.""接收角色"")) FOR UPDATE;", new { model.待办编号, 人员编号 = CurrentUser.编号 }, tx));
            if (todo == null) return StatusCode(403, new { error = "待办不存在、已办理或当前人员无权办理" });
            var newTaskStatus = model.结果 == "通过" ? "已通过" : "已退回";
            await conn.ExecuteAsync(new CommandDefinition("UPDATE \"进修审批待办\" SET \"状态\"=@状态,\"审批意见\"=@审批意见,\"办理时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@待办编号;", new { model.待办编号, 状态 = newTaskStatus, 审批意见 = 空值(model.审批意见) }, tx));
            if (model.结果 != "通过")
            {
                await conn.ExecuteAsync(new CommandDefinition("UPDATE \"进修审批待办\" SET \"状态\"='已撤回',\"办理时间\"=CURRENT_TIMESTAMP WHERE \"进修申请编号\"=@申请编号 AND \"状态\"='待处理';", new { 申请编号 = (long)todo.进修申请编号 }, tx));
                if ((string)todo.预算状态 == "已占用") await 释放预算(conn, tx, todo);
                await conn.ExecuteAsync(new CommandDefinition("UPDATE \"进修申请\" SET \"状态\"='已退回',\"预算状态\"=CASE WHEN \"预算状态\"='已占用' THEN '已释放' ELSE \"预算状态\" END,\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@申请编号;", new { 申请编号 = (long)todo.进修申请编号 }, tx));
                await 创建通知(conn, tx, (long)todo.进修申请编号, (int)todo.申请人编号, null, "审批结果", "进修申请已退回", model.审批意见 ?? "请补充材料后重新提交");
                await 记录审计(conn, tx, (long)todo.进修申请编号, CurrentUser.编号, "申请审批", (string)todo.申请状态, "已退回", model.审批意见);
                await tx.CommitAsync();
                return new { 待办编号 = model.待办编号, 状态 = "已退回", 预算状态 = "已释放" };
            }
            var next = await conn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"SELECT n.* FROM ""进修审批流程节点"" n JOIN ""进修申请"" a ON a.""流程配置编号""=n.""流程配置编号"" WHERE a.""编号""=@申请编号 AND n.""是否启用""=TRUE AND n.""节点序号"">@当前序号 ORDER BY n.""节点序号"" LIMIT 1;", new { 申请编号 = (long)todo.进修申请编号, 当前序号 = (int)todo.节点序号 }, tx));
            if (next == null)
            {
                await conn.ExecuteAsync(new CommandDefinition("UPDATE \"进修申请\" SET \"状态\"='已通过',\"当前节点序号\"=NULL,\"完成时间\"=CURRENT_TIMESTAMP,\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@申请编号;", new { 申请编号 = (long)todo.进修申请编号 }, tx));
                await 创建通知(conn, tx, (long)todo.进修申请编号, (int)todo.申请人编号, null, "审批结果", "进修申请已通过", "审批流程已完成，请按计划办理进修手续。");
                await 记录审计(conn, tx, (long)todo.进修申请编号, CurrentUser.编号, "申请审批", (string)todo.申请状态, "已通过", model.审批意见);
                await tx.CommitAsync();
                return new { 待办编号 = model.待办编号, 状态 = "已通过", 预算状态 = "已占用" };
            }
            await conn.ExecuteAsync(new CommandDefinition("UPDATE \"进修申请\" SET \"当前节点序号\"=@节点序号,\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@申请编号;", new { 申请编号 = (long)todo.进修申请编号, 节点序号 = (int)next.节点序号 }, tx));
            await 创建节点待办(conn, tx, todo, next, (long)todo.进修申请编号);
            await 记录审计(conn, tx, (long)todo.进修申请编号, CurrentUser.编号, "申请审批", (string)todo.申请状态, "审批中", model.审批意见);
            await tx.CommitAsync();
            return new { 待办编号 = model.待办编号, 状态 = "审批中", 当前节点序号 = (int)next.节点序号 };
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    [HttpPost]
    public async Task<object> 锁定进修预算([FromBody] 进修预算操作请求 model)
    {
        if (model == null || model.申请编号 <= 0) throw new ArgumentException("申请编号无效");
        if (!await IsEducationStaff()) return StatusCode(403, new { error = "无权锁定进修预算" });
        await using var conn = new NpgsqlConnection(DbConnectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var app = await conn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition("SELECT * FROM \"进修申请\" WHERE \"编号\"=@申请编号 FOR UPDATE;", new { model.申请编号 }, tx));
            if (app == null) return NotFound(new { error = "申请不存在" });
            if ((string)app.预算状态 == "已占用") return new { 编号 = model.申请编号, 预算状态 = "已占用" };
            if ((long?)app.年度计划编号 == null) return BadRequest(new { error = "计划外申请不能锁定年度计划预算" });
            var reserved = await conn.QuerySingleOrDefaultAsync<long?>(new CommandDefinition("UPDATE \"进修年度计划\" SET \"已占用人数\"=\"已占用人数\"+1,\"已占用预算\"=\"已占用预算\"+@预算金额,\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@计划编号 AND \"状态\" IN ('已发布','已锁定') AND \"已占用人数\"<\"计划人数\" AND \"已占用预算\"+\"已核销金额\"+@预算金额<=\"预算总额\" RETURNING \"编号\";", new { 计划编号 = (long)app.年度计划编号, 预算金额 = (decimal)app.预算金额 }, tx));
            if (!reserved.HasValue) return BadRequest(new { error = "年度计划名额或预算余额不足，无法锁定" });
            await conn.ExecuteAsync(new CommandDefinition("UPDATE \"进修申请\" SET \"预算状态\"='已占用',\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@申请编号;", new { model.申请编号 }, tx));
            await 记录审计(conn, tx, model.申请编号, CurrentUser.编号, "预算锁定", (string)app.预算状态, "已占用", null);
            await tx.CommitAsync();
            return new { 编号 = model.申请编号, 预算状态 = "已占用" };
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    [HttpPost]
    public async Task<object> 释放进修预算([FromBody] 进修预算操作请求 model)
    {
        if (model == null || model.申请编号 <= 0) throw new ArgumentException("申请编号无效");
        var manager = await IsEducationStaff();
        if (!manager && !await Db.QuerySingleAsync<bool>("SELECT EXISTS(SELECT 1 FROM \"进修申请\" WHERE \"编号\"=@申请编号 AND \"申请人编号\"=@人员编号);", new { model.申请编号, 人员编号 = CurrentUser.编号 })) return StatusCode(403, new { error = "无权释放进修预算" });
        await using var conn = new NpgsqlConnection(DbConnectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var app = await conn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition("SELECT * FROM \"进修申请\" WHERE \"编号\"=@申请编号 FOR UPDATE;", new { model.申请编号 }, tx));
            if (app == null) return NotFound(new { error = "申请不存在" });
            if ((string)app.预算状态 != "已占用") return new { 编号 = model.申请编号, 预算状态 = (string)app.预算状态 };
            await 释放预算(conn, tx, app);
            await conn.ExecuteAsync(new CommandDefinition("UPDATE \"进修申请\" SET \"预算状态\"='已释放',\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@申请编号;", new { model.申请编号 }, tx));
            await 记录审计(conn, tx, model.申请编号, CurrentUser.编号, "预算释放", "已占用", "已释放", null);
            await tx.CommitAsync();
            return new { 编号 = model.申请编号, 预算状态 = "已释放" };
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    [HttpGet]
    public async Task<object> 获取进修流程配置()
    {
        var config = await Db.QuerySingleOrDefaultAsync<dynamic>("SELECT * FROM \"进修审批流程配置\" WHERE \"业务类型\"='人员进修' AND \"状态\"='生效' ORDER BY \"版本号\" DESC LIMIT 1;");
        if (config == null) return new { 配置 = (object)null, 节点 = Array.Empty<object>() };
        var nodes = await Db.QueryAsync<object>("SELECT * FROM \"进修审批流程节点\" WHERE \"流程配置编号\"=@编号 ORDER BY \"节点序号\";", new { 编号 = (long)config.编号 });
        return new { 配置 = config, 节点 = nodes };
    }

    [HttpPost]
    public async Task<object> 保存进修流程配置([FromBody] 进修流程配置保存 model)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "仅科研管理人员可以配置进修流程" });
        if (model == null || string.IsNullOrWhiteSpace(model.名称) || model.节点 == null || !model.节点.Any()) throw new ArgumentException("流程名称和至少一个节点不能为空");
        var nodes = model.节点.OrderBy(x => x.序号).ToList();
        if (nodes.Any(x => x.序号 <= 0 || string.IsNullOrWhiteSpace(x.名称) || x.类型 is not ("审批" or "备案" or "自动"))) throw new ArgumentException("流程节点定义无效");
        await using var conn = new NpgsqlConnection(DbConnectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var nextVersion = await conn.QuerySingleAsync<int>(new CommandDefinition("SELECT COALESCE(MAX(\"版本号\"),0)+1 FROM \"进修审批流程配置\" WHERE \"业务类型\"='人员进修';", transaction: tx));
            var definition = JsonSerializer.Serialize(nodes.Select(n => new { 序号 = n.序号, 名称 = n.名称.Trim(), 类型 = n.类型, 角色 = string.IsNullOrWhiteSpace(n.角色) ? null : n.角色.Trim(), 是否会签 = n.是否会签, 是否启用 = n.是否启用 }));
            var id = await conn.QuerySingleAsync<long>(new CommandDefinition(@"
INSERT INTO ""进修审批流程配置"" (""业务类型"",""版本号"",""名称"",""状态"",""流程定义"",""变更说明"",""创建人编号"")
VALUES ('人员进修',@版本号,@名称,'草稿',@流程定义::jsonb,@变更说明,@创建人编号) RETURNING ""编号"";", new { 版本号 = nextVersion, 名称 = model.名称.Trim(), 流程定义 = definition, 变更说明 = 空值(model.变更说明), 创建人编号 = CurrentUser.编号 }, tx));
            foreach (var n in nodes)
                await conn.ExecuteAsync(new CommandDefinition("INSERT INTO \"进修审批流程节点\" (\"流程配置编号\",\"节点序号\",\"节点名称\",\"节点类型\",\"处理角色\",\"是否会签\",\"是否启用\") VALUES (@配置编号,@序号,@名称,@类型,@角色,@是否会签,@是否启用);", new { 配置编号 = id, n.序号, 节点名称 = n.名称.Trim(), 节点类型 = n.类型, 处理角色 = 空值(n.角色), n.是否会签, n.是否启用 }, tx));
            await tx.CommitAsync();
            return new { 编号 = id, 版本号 = nextVersion, 状态 = "草稿" };
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    [HttpGet("~/ContinuingEducation/查询流程模板")]
    [HttpGet("~/FurtherEducation/查询流程模板")]
    public async Task<IEnumerable<object>> 查询流程模板别名()
    {
        var configs = (await Db.QueryAsync<dynamic>("SELECT * FROM \"进修审批流程配置\" WHERE \"业务类型\"='人员进修' ORDER BY \"版本号\" DESC;")).ToList();
        var result = new List<object>();
        foreach (var config in configs)
        {
            var nodes = (await Db.QueryAsync<object>("SELECT \"编号\",\"节点序号\" AS \"顺序\",\"节点名称\" AS \"节点名称\",\"节点类型\" AS \"节点类型\",\"处理角色\" AS \"审批角色\",\"是否启用\" FROM \"进修审批流程节点\" WHERE \"流程配置编号\"=@编号 ORDER BY \"节点序号\";", new { 编号 = (long)config.编号 })).ToList();
            result.Add(new { 编号 = (long)config.编号, 名称 = (string)config.名称, 版本号 = (int)config.版本号, 是否启用 = (string)config.状态 == "生效", 是否已发布 = (string)config.状态 == "生效", 节点列表 = nodes, 更新时间 = config.发布时间 ?? config.创建时间 });
        }
        return result;
    }

    [HttpPost("~/ContinuingEducation/保存流程配置")]
    [HttpPost("~/FurtherEducation/保存流程配置")]
    public Task<object> 保存流程配置别名([FromBody] 进修流程配置保存 model) => 保存进修流程配置(model);

    [HttpPost]
    public async Task<object> 发布进修流程配置(long 配置编号)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "仅科研管理人员可以发布进修流程" });
        await using var conn = new NpgsqlConnection(DbConnectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var config = await conn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition("SELECT * FROM \"进修审批流程配置\" WHERE \"编号\"=@配置编号 FOR UPDATE;", new { 配置编号 }, tx));
            if (config == null || (string)config.状态 != "草稿") return BadRequest(new { error = "配置不存在或当前状态不可发布" });
            var hasNode = await conn.QuerySingleAsync<bool>(new CommandDefinition("SELECT EXISTS(SELECT 1 FROM \"进修审批流程节点\" WHERE \"流程配置编号\"=@配置编号 AND \"是否启用\"=TRUE);", new { 配置编号 }, tx));
            if (!hasNode) return BadRequest(new { error = "流程至少需要一个启用节点" });
            await conn.ExecuteAsync(new CommandDefinition("UPDATE \"进修审批流程配置\" SET \"状态\"='停用' WHERE \"业务类型\"='人员进修' AND \"状态\"='生效'; UPDATE \"进修审批流程配置\" SET \"状态\"='生效',\"发布时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@配置编号;", new { 配置编号 }, tx));
            await tx.CommitAsync();
            return new { 编号 = 配置编号, 状态 = "生效" };
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    [HttpPost("~/ContinuingEducation/发布流程配置")]
    [HttpPost("~/FurtherEducation/发布流程配置")]
    public Task<object> 发布流程配置别名([FromBody] 进修编号请求 model) =>
        发布进修流程配置(model?.编号 ?? model?.配置编号 ?? 0);

    [HttpGet("~/ContinuingEducation/查询预算台账")]
    [HttpGet("~/FurtherEducation/查询预算台账")]
    public async Task<object> 查询预算台账别名(long? 计划编号 = null, int? 计划年度 = null)
    {
        var manager = await IsEducationStaff();
        var plan = await Db.QuerySingleOrDefaultAsync<dynamic>(@"SELECT p.*,d.""名称"" AS ""部门名称"" FROM ""进修年度计划"" p LEFT JOIN ""部门"" d ON d.""编号""=p.""部门编号"" WHERE (@计划编号 IS NULL OR p.""编号""=@计划编号) AND (@计划年度 IS NULL OR p.""年度""=@计划年度) AND (@管理角色 OR p.""部门编号""=@部门编号) ORDER BY p.""年度"" DESC,p.""编号"" DESC LIMIT 1;", new { 计划编号, 计划年度, 管理角色 = manager, 部门编号 = CurrentUser.部门编号 });
        if (plan == null) return new { 计划年度 = 计划年度 ?? 0, 批准预算 = 0m, 已预占金额 = 0m, 审批中金额 = 0m, 已核销金额 = 0m, 可用余额 = 0m, 明细 = Array.Empty<object>() };
        var detail = await Db.QueryAsync<object>(@"SELECT a.""编号"" AS ""申请编号"",p.""姓名"" AS ""申请人姓名"",a.""预算金额"" AS ""金额"",a.""状态"",a.""更新时间"" FROM ""进修申请"" a LEFT JOIN ""人员"" p ON p.""编号""=a.""申请人编号"" WHERE a.""年度计划编号""=@编号 ORDER BY a.""更新时间"" DESC;", new { 编号 = (long)plan.编号 });
        var pendingAmount = await Db.QuerySingleAsync<decimal>(@"SELECT COALESCE(SUM(""预算金额""),0) FROM ""进修申请"" WHERE ""年度计划编号""=@编号 AND ""状态""='审批中';", new { 编号 = (long)plan.编号 });
        return new { 计划编号 = (long)plan.编号, 计划年度 = (int)plan.年度, 批准预算 = (decimal)plan.预算总额, 已预占金额 = (decimal)plan.已占用预算, 审批中金额 = pendingAmount, 已核销金额 = (decimal)plan.已核销金额, 可用余额 = (decimal)plan.预算总额 - (decimal)plan.已占用预算 - (decimal)plan.已核销金额, 明细 = detail };
    }

    [HttpGet("~/ContinuingEducation/查询审计记录")]
    [HttpGet("~/FurtherEducation/查询审计记录")]
    public async Task<IEnumerable<object>> 查询审计记录别名(long? 业务编号 = null, int Limit = 100)
    {
        var manager = await IsEducationStaff();
        return await Db.QueryAsync<object>(@"SELECT a.""编号"",'人员进修' AS ""业务类型"",a.""进修申请编号"" AS ""业务编号"",a.""操作类型"",p.""姓名"" AS ""操作人姓名"",a.""创建时间"" AS ""操作时间"",a.""原状态"",a.""新状态"",a.""说明"",a.""请求编号"" FROM ""进修操作审计"" a LEFT JOIN ""人员"" p ON p.""编号""=a.""操作人编号"" WHERE (@业务编号 IS NULL OR a.""进修申请编号""=@业务编号) AND (@管理角色 OR EXISTS(SELECT 1 FROM ""进修申请"" q WHERE q.""编号""=a.""进修申请编号"" AND q.""申请人编号""=@人员编号)) ORDER BY a.""创建时间"" DESC LIMIT @Limit;", new { 业务编号, Limit = Math.Clamp(Limit, 1, 500), 管理角色 = manager, 人员编号 = CurrentUser.编号 });
    }

    [HttpGet]
    public async Task<IEnumerable<object>> 查询进修通知(bool 仅未读 = true)
    {
        return await Db.QueryAsync<object>(@"SELECT * FROM ""进修通知"" n WHERE (@仅未读=FALSE OR n.""是否已读""=FALSE) AND (n.""接收人编号""=@人员编号 OR EXISTS(SELECT 1 FROM ""人员角色"" pr JOIN ""角色"" r ON r.""编号""=pr.""角色编号"" WHERE pr.""人员编号""=@人员编号 AND r.""名称""=n.""接收角色"")) ORDER BY n.""创建时间"" DESC LIMIT 200;", new { 仅未读, 人员编号 = CurrentUser.编号 });
    }

    [HttpPost]
    public async Task<object> 标记进修通知已读(long 通知编号)
    {
        var count = await Db.ExecuteAsync(@"UPDATE ""进修通知"" SET ""是否已读""=TRUE,""已读时间""=CURRENT_TIMESTAMP WHERE ""编号""=@通知编号 AND (""接收人编号""=@人员编号 OR EXISTS(SELECT 1 FROM ""人员角色"" pr JOIN ""角色"" r ON r.""编号""=pr.""角色编号"" WHERE pr.""人员编号""=@人员编号 AND r.""名称""=""接收角色""));", new { 通知编号, 人员编号 = CurrentUser.编号 });
        return new { 通知编号, 已读 = count > 0 };
    }

    private async Task 创建节点待办(NpgsqlConnection conn, NpgsqlTransaction tx, dynamic app, dynamic node, long applicationId)
    {
        var role = string.IsNullOrWhiteSpace((string)node.处理角色) ? null : (string)node.处理角色;
        int? recipient = null;
        if (role == "科主任")
            recipient = await conn.QuerySingleOrDefaultAsync<int?>(new CommandDefinition("SELECT \"负责人编号\" FROM \"部门\" WHERE \"编号\"=@部门编号 AND \"是否启用\"=TRUE;", new { 部门编号 = (int)app.部门编号 }, tx));
        var title = $"进修申请待办理：{app.目标医院}（{app.进修专业}）";
        var todoId = await conn.QuerySingleAsync<long>(new CommandDefinition(@"INSERT INTO ""进修审批待办"" (""进修申请编号"",""流程节点编号"",""节点序号"",""接收人编号"",""接收角色"",""标题"",""到期时间"") VALUES (@进修申请编号,@流程节点编号,@节点序号,@接收人编号,@接收角色,@标题,CURRENT_TIMESTAMP+INTERVAL '7 days') RETURNING ""编号"";", new { 进修申请编号 = applicationId, 流程节点编号 = (long)node.编号, 节点序号 = (int)node.节点序号, 接收人编号 = recipient, 接收角色 = role, 标题 = title }, tx));
        if (recipient.HasValue) await 创建通知(conn, tx, applicationId, recipient.Value, null, "待办", title, "请登录科研管理系统办理进修审批。");
        else if (role != null) await 创建通知(conn, tx, applicationId, null, role, "待办", title, "请登录科研管理系统办理进修审批。");
    }

    private async Task 创建通知(NpgsqlConnection conn, NpgsqlTransaction tx, long applicationId, int? personId, string role, string type, string title, string content)
    {
        await conn.ExecuteAsync(new CommandDefinition(@"INSERT INTO ""进修通知"" (""进修申请编号"",""接收人编号"",""接收角色"",""通知类型"",""标题"",""内容"") VALUES (@进修申请编号,@接收人编号,@接收角色,@通知类型,@标题,@内容);", new { 进修申请编号 = applicationId, 接收人编号 = personId, 接收角色 = role, 通知类型 = type, 标题 = title, 内容 = content }, tx));
    }

    private async Task 释放预算(NpgsqlConnection conn, NpgsqlTransaction tx, dynamic app)
    {
        if ((long?)app.年度计划编号 == null || (string)app.预算状态 != "已占用") return;
        await conn.ExecuteAsync(new CommandDefinition("UPDATE \"进修年度计划\" SET \"已占用人数\"=GREATEST(\"已占用人数\"-1,0),\"已占用预算\"=GREATEST(\"已占用预算\"-@预算金额,0),\"状态\"=CASE WHEN \"状态\"='已锁定' AND \"已占用人数\"-1<=0 THEN '已发布' ELSE \"状态\" END,\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@计划编号;", new { 计划编号 = (long)app.年度计划编号, 预算金额 = (decimal)app.预算金额 }, tx));
    }

    private async Task 记录审计(long? applicationId, string type, string oldStatus, string newStatus, string note)
    {
        await Db.ExecuteAsync(@"INSERT INTO ""进修操作审计"" (""进修申请编号"",""操作人编号"",""操作类型"",""原状态"",""新状态"",""说明"",""请求编号"") VALUES (@进修申请编号,@操作人编号,@操作类型,@原状态,@新状态,@说明,@请求编号);", new { 进修申请编号 = applicationId, 操作人编号 = CurrentUser.编号, 操作类型 = type, 原状态 = oldStatus, 新状态 = newStatus, 说明 = note, 请求编号 = HttpContext.TraceIdentifier });
    }

    private static async Task 记录审计(NpgsqlConnection conn, NpgsqlTransaction tx, long applicationId, int actorId, string type, string oldStatus, string newStatus, string note)
    {
        await conn.ExecuteAsync(new CommandDefinition(@"INSERT INTO ""进修操作审计"" (""进修申请编号"",""操作人编号"",""操作类型"",""原状态"",""新状态"",""说明"",""请求编号"") VALUES (@进修申请编号,@操作人编号,@操作类型,@原状态,@新状态,@说明,@请求编号);", new { 进修申请编号 = applicationId, 操作人编号 = actorId, 操作类型 = type, 原状态 = oldStatus, 新状态 = newStatus, 说明 = note, 请求编号 = null as string }, tx));
    }

    private static string 空值(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
