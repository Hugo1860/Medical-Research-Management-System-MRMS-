using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using ScientificResearch.Infrastucture;
using ScientificResearch.Models;

namespace ScientificResearch.Controllers;

/// <summary>
/// 独立科研伦理审查流程。该控制器只登记真实申请、委员会意见和决定，
/// 不使用示例数据；外部伦理系统同步仍由 YProjectController 负责。
/// </summary>
public sealed class EthicsController : BaseController
{
    private int 纵向申报模板 => config.GetValue<int>("流程模板编号:纵向项目申报");
    private int 纵向立项模板 => config.GetValue<int>("流程模板编号:纵向项目立项");
    private int 横向申请模板 => config.GetValue<int>("流程模板编号:横向项目申请");

    /// <summary>查询当前人员可见的伦理申请。伦理委员会角色可查看全院申请。</summary>
    [HttpGet]
    public async Task<object> 查询伦理申请(string 状态 = null, string 申请类型 = null, string 关键词 = null, int? 项目编号 = null, int? 数量 = null)
    {
        var committee = await 是伦理管理角色();
        var limit = Math.Clamp(数量 ?? 100, 1, 500);
        return await Db.QueryAsync<object>(@"
SELECT a.""编号"",a.""流程模板编号"",a.""项目编号"",a.""申请人编号"",p.""姓名"" AS ""申请人姓名"",
       COALESCE(y.""项目中文名称"",ya.""项目名称"",x.""合同名称"",'未命名项目') AS ""项目名称"",
       a.""申请类型"",a.""申请标题"",a.""受试者风险等级"",a.""状态"",a.""提交时间"",a.""受理时间"",a.""决定时间"",a.""批件编号"",a.""有效期至"",a.""建立时间"",a.""更新时间""
  FROM ""伦理审查申请"" a
  LEFT JOIN ""人员"" p ON p.""编号""=a.""申请人编号""
  LEFT JOIN ""纵向项目立项"" y ON a.""流程模板编号""=@纵向立项模板 AND y.""编号""=a.""项目编号""
  LEFT JOIN ""纵向项目申报"" ya ON a.""流程模板编号""=@纵向申报模板 AND ya.""编号""=a.""项目编号""
  LEFT JOIN ""横向项目"" x ON a.""流程模板编号""=@横向申请模板 AND x.""编号""=a.""项目编号""
 WHERE (@状态 IS NULL OR a.""状态""=@状态)
   AND (@申请类型 IS NULL OR a.""申请类型""=@申请类型)
   AND (@项目编号 IS NULL OR a.""项目编号""=@项目编号)
   AND (@关键词 IS NULL OR a.""申请标题"" ILIKE '%'||@关键词||'%' OR a.""批件编号"" ILIKE '%'||@关键词||'%' OR p.""姓名"" ILIKE '%'||@关键词||'%')
   AND (@管理角色 OR a.""申请人编号""=@人员编号)
 ORDER BY CASE a.""状态"" WHEN '审查中' THEN 1 WHEN '待受理' THEN 2 WHEN '需补正' THEN 3 ELSE 4 END,a.""更新时间"" DESC,a.""编号"" DESC
 LIMIT @数量;", new
        {
            状态 = 空值(状态), 申请类型 = 空值(申请类型), 关键词 = 空值(关键词), 项目编号,
            纵向立项模板, 纵向申报模板, 横向申请模板, 管理角色 = committee,
            人员编号 = CurrentUser.编号, 数量 = limit
        });
    }

    /// <summary>提交初始审查、修正案、年度跟踪或延期续展申请。</summary>
    [HttpPost]
    public async Task<object> 提交伦理申请([FromBody] 伦理申请保存 model)
    {
        校验申请(model);
        if (!await CanAccessProjectAsync(model.流程模板编号, model.项目编号)) return ProjectAccessDenied();
        var title = model.申请标题.Trim();
        var summary = model.研究方案摘要.Trim();
        if (title.Length > 200 || summary.Length > 20000) throw new ArgumentException("伦理申请标题或方案摘要超出长度限制");
        var risk = string.IsNullOrWhiteSpace(model.受试者风险等级) ? "一般风险" : model.受试者风险等级.Trim();
        if (risk is not ("低风险" or "一般风险" or "高风险")) throw new ArgumentException("受试者风险等级无效");
        if (model.编号.HasValue)
        {
            var changed = await Db.ExecuteAsync(@"
UPDATE ""伦理审查申请"" SET ""申请类型""=@申请类型,""申请标题""=@申请标题,""研究方案摘要""=@研究方案摘要,
       ""受试者风险等级""=@受试者风险等级,""知情同意说明""=@知情同意说明,""附件路径""=@附件路径,""备注""=@备注,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@编号 AND ""申请人编号""=@申请人编号 AND ""状态"" IN ('草稿','需补正');", new
            {
                model.编号, model.申请类型, 申请标题 = title, 研究方案摘要 = summary, 受试者风险等级 = risk,
                知情同意说明 = 空值(model.知情同意说明), 附件路径 = 空值(model.附件路径), 备注 = 空值(model.备注), 申请人编号 = CurrentUser.编号
            });
            if (changed == 0) return NotFound(new { error = "伦理申请不存在、无权修改或当前状态不可编辑" });
            return new { 编号 = model.编号.Value, 状态 = "草稿", message = "伦理申请草稿已保存" };
        }
        var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""伦理审查申请"" (""流程模板编号"",""项目编号"",""申请人编号"",""申请类型"",""申请标题"",""研究方案摘要"",""受试者风险等级"",""知情同意说明"",""附件路径"",""备注"",""建立人编号"")
VALUES (@流程模板编号,@项目编号,@申请人编号,@申请类型,@申请标题,@研究方案摘要,@受试者风险等级,@知情同意说明,@附件路径,@备注,@建立人编号)
RETURNING ""编号"";", new
        {
            model.流程模板编号, model.项目编号, 申请人编号 = CurrentUser.编号, model.申请类型,
            申请标题 = title, 研究方案摘要 = summary, 受试者风险等级 = risk,
            知情同意说明 = 空值(model.知情同意说明), 附件路径 = 空值(model.附件路径), 备注 = 空值(model.备注), 建立人编号 = CurrentUser.编号
        });
        return new { 编号 = id, 状态 = "草稿", message = "伦理申请草稿已创建，请提交后进入委员会受理" };
    }

    /// <summary>将草稿提交到委员会受理队列，提交后申请人不可直接修改。</summary>
    [HttpPost]
    public async Task<object> 提交伦理审查([FromBody] long 申请编号)
    {
        if (申请编号 <= 0) throw new ArgumentException("申请编号无效");
        var application = await 获取申请(申请编号);
        if (application == null) return NotFound(new { error = "伦理申请不存在" });
        if ((int)application.申请人编号 != CurrentUser.编号 && !await 是伦理管理角色()) return StatusCode(403, new { error = "无权提交该伦理申请" });
        var changed = await Db.ExecuteAsync(@"
UPDATE ""伦理审查申请"" SET ""状态""='待受理',""提交时间""=CURRENT_TIMESTAMP,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@申请编号 AND ""状态"" IN ('草稿','需补正');", new { 申请编号 });
        if (changed == 0) return BadRequest(new { error = "申请当前状态不可提交" });
        return new { 编号 = 申请编号, 状态 = "待受理", message = "伦理申请已提交，等待伦理委员会秘书受理" };
    }

    /// <summary>委员会秘书受理或退回补正。</summary>
    [HttpPost]
    public async Task<object> 受理伦理申请([FromBody] 伦理申请受理 model)
    {
        if (!await 是伦理管理角色()) return StatusCode(403, new { error = "仅科技办、科研管理员或科研处负责人可以受理申请" });
        if (model == null || model.申请编号 <= 0 || model.结果 is not ("受理" or "退回补正")) throw new ArgumentException("受理结果无效");
        if (model.结果 == "退回补正" && string.IsNullOrWhiteSpace(model.退回原因)) throw new ArgumentException("退回补正必须填写原因");
        var changed = await Db.ExecuteAsync(@"
UPDATE ""伦理审查申请"" SET ""状态""=@状态,""受理时间""=CASE WHEN @状态='审查中' THEN CURRENT_TIMESTAMP ELSE ""受理时间"" END,
       ""退回原因""=@退回原因,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@申请编号 AND ""状态""='待受理';", new { model.申请编号, 状态 = model.结果 == "受理" ? "审查中" : "需补正", 退回原因 = model.结果 == "退回补正" ? model.退回原因.Trim() : null });
        if (changed == 0) return BadRequest(new { error = "申请不存在或当前不是待受理状态" });
        return new { 编号 = model.申请编号, 状态 = model.结果 == "受理" ? "审查中" : "需补正" };
    }

    /// <summary>伦理委员提交意见；利益冲突时只能选择回避。</summary>
    [HttpPost]
    public async Task<object> 登记伦理委员意见([FromBody] 伦理委员意见保存 model)
    {
        if (!await HasAnyRoleAsync("专家", "伦理委员", "伦理委员会委员", "科技办", "科研科负责人", "科研处处长", "科研管理员", "系统管理员")) return StatusCode(403, new { error = "无权登记伦理委员意见" });
        if (model == null || model.申请编号 <= 0 || model.意见结论 is not ("同意" or "修改后同意" or "不同意" or "回避")) throw new ArgumentException("委员意见结论无效");
        if (model.是否利益冲突 && model.意见结论 != "回避") throw new ArgumentException("存在利益冲突时必须回避，不能提交实质性意见");
        if (!model.会议编号.HasValue || model.会议编号.Value <= 0) throw new ArgumentException("委员意见必须关联已排期的伦理委员会会议");
        var scheduled = await Db.QuerySingleAsync<bool>(@"SELECT EXISTS (SELECT 1 FROM ""伦理审查会议议程"" g JOIN ""伦理审查会议"" m ON m.""编号""=g.""会议编号"" WHERE g.""会议编号""=@会议编号 AND g.""申请编号""=@申请编号 AND g.""状态"" IN ('已排期','已审查') AND m.""状态"" IN ('计划中','进行中'));", new { model.会议编号, model.申请编号 });
        if (!scheduled) throw new ArgumentException("该申请尚未排入有效的伦理委员会会议议程");
        var application = await 获取申请(model.申请编号);
        if (application == null) return NotFound(new { error = "伦理申请不存在" });
        if ((string)application.状态 != "审查中") return BadRequest(new { error = "仅审查中的伦理申请可以登记委员意见" });
        var changed = await Db.ExecuteAsync(@"
INSERT INTO ""伦理审查委员意见"" (""申请编号"",""会议编号"",""委员编号"",""是否利益冲突"",""意见结论"",""意见内容"",""评分"")
VALUES (@申请编号,@会议编号,@委员编号,@是否利益冲突,@意见结论,@意见内容,@评分)
ON CONFLICT (""申请编号"",""委员编号"") DO UPDATE SET ""会议编号""=EXCLUDED.""会议编号"",""是否利益冲突""=EXCLUDED.""是否利益冲突"",""意见结论""=EXCLUDED.""意见结论"",""意见内容""=EXCLUDED.""意见内容"",""评分""=EXCLUDED.""评分"",""提交时间""=CURRENT_TIMESTAMP;", new
        {
            model.申请编号, model.会议编号, 委员编号 = CurrentUser.编号, model.是否利益冲突, model.意见结论,
            意见内容 = 空值(model.意见内容), model.评分
        });
        return new { success = changed > 0, 申请编号 = model.申请编号, message = "伦理委员意见已保存并纳入审查记录" };
    }

    /// <summary>登记委员会正式决定，并在批准时回写纵向项目伦理状态与有效期。</summary>
    [HttpPost]
    public async Task<object> 登记伦理决定([FromBody] 伦理决定保存 model)
    {
        if (!await 是伦理管理角色()) return StatusCode(403, new { error = "无权登记伦理委员会决定" });
        if (model == null || model.申请编号 <= 0 || model.决定 is not ("批准" or "修改后批准" or "补正后再审" or "拒绝" or "终止")) throw new ArgumentException("伦理决定无效");
        if (string.IsNullOrWhiteSpace(model.决定说明)) throw new ArgumentException("决定说明不能为空");
        if (model.决定 is "批准" or "修改后批准")
        {
            if (string.IsNullOrWhiteSpace(model.批件编号) || !model.有效期至.HasValue) throw new ArgumentException("批准决定必须填写批件编号和有效期");
            if (model.有效期至.Value.Date < DateTime.Today) throw new ArgumentException("伦理批件有效期不能早于当前日期");
        }
        var application = await 获取申请(model.申请编号);
        if (application == null) return NotFound(new { error = "伦理申请不存在" });
        if ((string)application.状态 != "审查中") return BadRequest(new { error = "仅审查中的伦理申请可以登记正式决定" });
        var hasAgenda = await Db.QuerySingleAsync<bool>(@"SELECT EXISTS (SELECT 1 FROM ""伦理审查会议议程"" WHERE ""申请编号""=@申请编号 AND ""状态"" IN ('已排期','已审查'));", new { model.申请编号 });
        if (!hasAgenda) return BadRequest(new { error = "伦理决定必须基于已排期的委员会会议议程" });
        await using var connection = new Npgsql.NpgsqlConnection(DbConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var result = model.决定 switch
            {
                "批准" or "修改后批准" => "已批准",
                "补正后再审" => "需补正",
                "拒绝" => "已拒绝",
                _ => "已撤回"
            };
            await connection.ExecuteAsync(@"
INSERT INTO ""伦理审查决定"" (""申请编号"",""决定"",""决定说明"",""批件编号"",""批准时间"",""有效期至"",""决定书路径"",""决定人编号"")
VALUES (@申请编号,@决定,@决定说明,@批件编号,@批准时间,@有效期至,@决定书路径,@决定人编号);", new
            {
                model.申请编号, model.决定, 决定说明 = model.决定说明.Trim(), 批件编号 = 空值(model.批件编号),
                批准时间 = model.决定 is "批准" or "修改后批准" ? DateTime.Now : (DateTime?)null,
                有效期至 = model.有效期至?.Date, 决定书路径 = 空值(model.决定书路径), 决定人编号 = CurrentUser.编号
            }, transaction);
            await connection.ExecuteAsync(@"
UPDATE ""伦理审查申请"" SET ""状态""=@状态,""决定时间""=CURRENT_TIMESTAMP,""批件编号""=@批件编号,""有效期至""=@有效期至,""批件路径""=@决定书路径,""退回原因""=NULL,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@申请编号;", new
            {
                model.申请编号, 状态 = result, 批件编号 = 空值(model.批件编号), 有效期至 = model.有效期至?.Date,
                决定书路径 = 空值(model.决定书路径)
            }, transaction);
            if ((int)application.流程模板编号 == 纵向立项模板 && result == "已批准")
            {
                await connection.ExecuteAsync(@"
UPDATE ""纵向项目立项"" SET ""是否需要伦理评审""=TRUE,""是否需要伦理批文""=TRUE,""伦理批文编号""=@批件编号,""伦理批文时间""=CURRENT_TIMESTAMP,""伦理批文有效期至""=@有效期至,""伦理批文路径""=@决定书路径,""伦理审查状态""='有效'
 WHERE ""编号""=@项目编号;", new { application.项目编号, 批件编号 = model.批件编号.Trim(), 有效期至 = model.有效期至.Value.Date, 决定书路径 = 空值(model.决定书路径) }, transaction);
            }
            else if ((int)application.流程模板编号 == 横向申请模板 && result == "已批准")
            {
                await connection.ExecuteAsync("UPDATE \"横向项目\" SET \"是否需要伦理评审\"=TRUE WHERE \"编号\"=@项目编号;", new { application.项目编号 }, transaction);
            }
            await transaction.CommitAsync();
            return new { success = true, 编号 = model.申请编号, 状态 = result, message = result == "已批准" ? "伦理决定已登记，项目合规状态已更新" : "伦理决定已登记" };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>提交年度跟踪审查、方案修正案、知情同意修订、不良事件或偏离方案记录。</summary>
    [HttpPost]
    public async Task<object> 提交伦理持续记录([FromBody] 伦理持续记录保存 model)
    {
        校验持续记录(model);
        var application = await 获取申请(model.申请编号);
        if (application == null) return NotFound(new { error = "伦理申请不存在" });
        if ((string)application.状态 != "已批准") return BadRequest(new { error = "仅已批准的伦理申请可以提交持续审查或不良事件记录" });
        if ((int)application.申请人编号 != CurrentUser.编号 && !await 是伦理管理角色()) return StatusCode(403, new { error = "无权提交该项目的持续伦理记录" });
        var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""伦理持续审查记录"" (""申请编号"",""记录类型"",""发生日期"",""下次审查日期"",""严重程度"",""事件说明"",""处理结论"",""附件路径"",""提交人编号"")
VALUES (@申请编号,@记录类型,@发生日期,@下次审查日期,@严重程度,@事件说明,@处理结论,@附件路径,@提交人编号)
RETURNING ""编号"";", new
        {
            model.申请编号, model.记录类型, 发生日期 = model.发生日期?.Date, 下次审查日期 = model.下次审查日期?.Date,
            严重程度 = 空值(model.严重程度), 事件说明 = model.事件说明.Trim(), 处理结论 = 空值(model.处理结论), 附件路径 = 空值(model.附件路径), 提交人编号 = CurrentUser.编号
        });
        return new { 编号 = id, 状态 = "待审查", message = "持续伦理记录已提交" };
    }

    /// <summary>委员会审核持续记录；不良事件和方案偏离必须填写处理结论。</summary>
    [HttpPost]
    public async Task<object> 审核伦理持续记录([FromBody] 伦理持续记录审核 model)
    {
        if (!await 是伦理管理角色()) return StatusCode(403, new { error = "无权审核持续伦理记录" });
        if (model == null || model.记录编号 <= 0 || model.结果 is not ("审查中" or "已批准" or "需补正" or "已关闭")) throw new ArgumentException("持续记录审核结果无效");
        if (string.IsNullOrWhiteSpace(model.处理结论)) throw new ArgumentException("持续记录审核必须填写处理结论");
        var changed = await Db.ExecuteAsync(@"
UPDATE ""伦理持续审查记录"" SET ""状态""=@状态,""处理结论""=COALESCE(@处理结论,""处理结论""),""审核人编号""=@审核人编号,""审核时间""=CURRENT_TIMESTAMP,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@记录编号 AND ""状态"" IN ('待审查','审查中');", new { model.记录编号, 状态 = model.结果, 处理结论 = 空值(model.处理结论), 审核人编号 = CurrentUser.编号 });
        if (changed == 0) return NotFound(new { error = "持续记录不存在或当前状态不可审核" });
        return new { success = true, 编号 = model.记录编号, 状态 = model.结果 };
    }

    /// <summary>获取申请详情、委员意见、决定和持续审查记录。</summary>
    [HttpGet]
    public async Task<object> 获取伦理申请详情(long 申请编号)
    {
        if (申请编号 <= 0) throw new ArgumentException("申请编号无效");
        var application = await 获取申请(申请编号);
        if (application == null) return NotFound(new { error = "伦理申请不存在" });
        if (!await 有权访问申请(application)) return StatusCode(403, new { error = "无权查看该伦理申请" });
        var opinions = await Db.QueryAsync<object>(@"
SELECT o.""编号"",o.""会议编号"",o.""委员编号"",p.""姓名"" AS ""委员姓名"",o.""是否利益冲突"",o.""意见结论"",o.""意见内容"",o.""评分"",o.""提交时间""
  FROM ""伦理审查委员意见"" o LEFT JOIN ""人员"" p ON p.""编号""=o.""委员编号"" WHERE o.""申请编号""=@申请编号 ORDER BY o.""提交时间"" DESC;", new { 申请编号 });
        var decisions = await Db.QueryAsync<object>(@"SELECT * FROM ""伦理审查决定"" WHERE ""申请编号""=@申请编号 ORDER BY ""建立时间"" DESC;", new { 申请编号 });
        var followups = await Db.QueryAsync<object>(@"SELECT * FROM ""伦理持续审查记录"" WHERE ""申请编号""=@申请编号 ORDER BY ""建立时间"" DESC;", new { 申请编号 });
        return new { 申请 = application, 委员意见 = opinions, 决定 = decisions, 持续记录 = followups };
    }

    /// <summary>查询有效期临近或已过期的伦理批件及待跟踪审查记录。</summary>
    [HttpGet]
    public async Task<object> 查询伦理到期提醒(int? 提前天数 = null)
    {
        var days = Math.Clamp(提前天数 ?? 60, 0, 365);
        var committee = await 是伦理管理角色();
        var approvals = await Db.QueryAsync<object>(@"
SELECT a.""编号"" AS ""申请编号"",a.""项目编号"",a.""申请标题"",a.""批件编号"",a.""有效期至"",a.""申请人编号"",p.""姓名"" AS ""申请人姓名"",
       CASE WHEN a.""有效期至"" < CURRENT_DATE THEN '已过期' ELSE '即将到期' END AS ""提醒状态"",
       (a.""有效期至"" - CURRENT_DATE) AS ""剩余天数""
  FROM ""伦理审查申请"" a LEFT JOIN ""人员"" p ON p.""编号""=a.""申请人编号""
 WHERE a.""状态""='已批准' AND a.""有效期至"" IS NOT NULL AND a.""有效期至"" <= CURRENT_DATE + @天数
   AND (@管理角色 OR a.""申请人编号""=@人员编号) ORDER BY a.""有效期至"" ASC;", new { 天数 = days, 管理角色 = committee, 人员编号 = CurrentUser.编号 });
        var followups = await Db.QueryAsync<object>(@"
SELECT r.""编号"",r.""申请编号"",r.""记录类型"",r.""下次审查日期"",r.""状态"",a.""申请标题""
  FROM ""伦理持续审查记录"" r JOIN ""伦理审查申请"" a ON a.""编号""=r.""申请编号""
 WHERE r.""下次审查日期"" IS NOT NULL AND r.""下次审查日期"" <= CURRENT_DATE + @天数 AND r.""状态"" IN ('待审查','审查中')
   AND (@管理角色 OR a.""申请人编号""=@人员编号) ORDER BY r.""下次审查日期"" ASC;", new { 天数 = days, 管理角色 = committee, 人员编号 = CurrentUser.编号 });
        return new { 提前天数 = days, 批件提醒 = approvals, 持续审查提醒 = followups };
    }

    /// <summary>查询伦理委员会会议排期、议程数量和纪要状态。</summary>
    [HttpGet]
    public async Task<object> 查询伦理会议(string 状态 = null, DateTime? 开始时间 = null, DateTime? 结束时间 = null)
    {
        if (!await 是伦理管理角色()) return StatusCode(403, new { error = "仅伦理委员会管理角色可以查询会议排期" });
        if (!string.IsNullOrWhiteSpace(状态) && 状态.Trim() is not ("计划中" or "进行中" or "已完成" or "已取消")) throw new ArgumentException("会议状态无效");
        return await Db.QueryAsync<object>(@"
SELECT m.""编号"",m.""会议序号"",m.""会议时间"",m.""会议地点"",m.""主持人编号"",p.""姓名"" AS ""主持人姓名"",
       m.""状态"",m.""议程说明"",m.""议程路径"",m.""会议纪要路径"",m.""纪要状态"",m.""纪要提交时间"",m.""纪要确认时间"",m.""备注"",
       COUNT(g.""编号"")::integer AS ""议程数量""
  FROM ""伦理审查会议"" m
  LEFT JOIN ""人员"" p ON p.""编号""=m.""主持人编号""
  LEFT JOIN ""伦理审查会议议程"" g ON g.""会议编号""=m.""编号"" AND g.""状态""='已排期'
 WHERE (@状态 IS NULL OR m.""状态""=@状态)
   AND (@开始时间 IS NULL OR m.""会议时间"">=@开始时间)
   AND (@结束时间 IS NULL OR m.""会议时间""<@结束时间 + INTERVAL '1 day')
 GROUP BY m.""编号"",p.""姓名""
 ORDER BY m.""会议时间"" DESC,m.""编号"" DESC;", new { 状态 = 空值(状态), 开始时间, 结束时间 });
    }

    /// <summary>新建或调整伦理委员会会议排期。会议序号是可审计的业务编号。</summary>
    [HttpPost]
    public async Task<object> 保存伦理会议([FromBody] 伦理会议保存 model)
    {
        if (!await 是伦理管理角色()) return StatusCode(403, new { error = "仅伦理委员会管理角色可以维护会议排期" });
        if (model == null || string.IsNullOrWhiteSpace(model.会议序号) || model.会议时间 == default) throw new ArgumentException("会议序号和会议时间均为必填项");
        var status = string.IsNullOrWhiteSpace(model.状态) ? "计划中" : model.状态.Trim();
        if (status is not ("计划中" or "进行中" or "已完成" or "已取消")) throw new ArgumentException("会议状态无效");
        if (model.编号.HasValue)
        {
            var changed = await Db.ExecuteAsync(@"
UPDATE ""伦理审查会议"" SET ""会议序号""=@会议序号,""会议时间""=@会议时间,""会议地点""=@会议地点,
       ""主持人编号""=@主持人编号,""状态""=@状态,""议程说明""=@议程说明,""议程路径""=@议程路径,""备注""=@备注
 WHERE ""编号""=@编号;", new
            {
                编号 = model.编号.Value, 会议序号 = model.会议序号.Trim(), model.会议时间,
                会议地点 = 空值(model.会议地点), model.主持人编号, 状态 = status,
                议程说明 = 空值(model.议程说明), 议程路径 = 空值(model.议程路径), 备注 = 空值(model.备注)
            });
            if (changed == 0) return NotFound(new { error = "伦理会议不存在" });
            return new { 编号 = model.编号.Value, 状态 = status, message = "伦理会议排期已更新" };
        }
        var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""伦理审查会议"" (""会议序号"",""会议时间"",""会议地点"",""主持人编号"",""状态"",""议程说明"",""议程路径"",""备注"",""建立人编号"")
VALUES (@会议序号,@会议时间,@会议地点,@主持人编号,@状态,@议程说明,@议程路径,@备注,@建立人编号)
RETURNING ""编号"";", new
        {
            会议序号 = model.会议序号.Trim(), model.会议时间, 会议地点 = 空值(model.会议地点), model.主持人编号,
            状态 = status, 议程说明 = 空值(model.议程说明), 议程路径 = 空值(model.议程路径), 备注 = 空值(model.备注), 建立人编号 = CurrentUser.编号
        });
        return new { 编号 = id, 状态 = status, message = "伦理委员会会议已排期" };
    }

    /// <summary>将已受理的伦理申请加入会议议程，或将议程标记为撤回。</summary>
    [HttpPost]
    public async Task<object> 维护伦理会议议程([FromBody] 伦理会议议程保存 model)
    {
        if (!await 是伦理管理角色()) return StatusCode(403, new { error = "仅伦理委员会管理角色可以维护会议议程" });
        if (model == null || model.会议编号 <= 0 || model.申请编号 <= 0) throw new ArgumentException("会议编号和申请编号均为必填项");
        var agendaState = string.IsNullOrWhiteSpace(model.状态) ? "已排期" : model.状态.Trim();
        if (agendaState is not ("已排期" or "已审查" or "已撤回")) throw new ArgumentException("议程状态无效");
        if (model.排序号 <= 0) throw new ArgumentException("议程排序号必须大于零");
        var meeting = await Db.QuerySingleOrDefaultAsync<dynamic>("SELECT \"编号\",\"状态\" FROM \"伦理审查会议\" WHERE \"编号\"=@编号;", new { 编号 = model.会议编号 });
        if (meeting == null) return NotFound(new { error = "伦理会议不存在" });
        if ((string)meeting.状态 is "已完成" or "已取消") return BadRequest(new { error = "已完成或已取消的会议不能调整议程" });
        var application = await 获取申请(model.申请编号);
        if (application == null) return NotFound(new { error = "伦理申请不存在" });
        if ((string)application.状态 != "审查中") return BadRequest(new { error = "只有审查中的伦理申请可以排入委员会议程" });
        await Db.ExecuteAsync(@"
INSERT INTO ""伦理审查会议议程"" (""会议编号"",""申请编号"",""排序号"",""议题说明"",""状态"",""建立人编号"")
VALUES (@会议编号,@申请编号,@排序号,@议题说明,@状态,@建立人编号)
ON CONFLICT (""会议编号"",""申请编号"") DO UPDATE SET ""排序号""=EXCLUDED.""排序号"",""议题说明""=EXCLUDED.""议题说明"",""状态""=EXCLUDED.""状态"";", new
        {
            model.会议编号, model.申请编号, model.排序号, 议题说明 = 空值(model.议题说明), 状态 = agendaState, 建立人编号 = CurrentUser.编号
        });
        return new { success = true, 会议编号 = model.会议编号, 申请编号 = model.申请编号, 状态 = agendaState };
    }

    /// <summary>查询会议的申请议程、委员意见、会议纪要和归档记录。</summary>
    [HttpGet]
    public async Task<object> 获取伦理会议详情(long 会议编号)
    {
        if (会议编号 <= 0) throw new ArgumentException("会议编号无效");
        if (!await 是伦理管理角色()) return StatusCode(403, new { error = "仅伦理委员会管理角色可以查看会议详情" });
        var meeting = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT m.*,p.""姓名"" AS ""主持人姓名"" FROM ""伦理审查会议"" m LEFT JOIN ""人员"" p ON p.""编号""=m.""主持人编号"" WHERE m.""编号""=@会议编号;", new { 会议编号 });
        if (meeting == null) return NotFound(new { error = "伦理会议不存在" });
        var agenda = await Db.QueryAsync<object>(@"
SELECT g.*,a.""申请标题"",a.""申请类型"",a.""状态"" AS ""申请状态"",p.""姓名"" AS ""申请人姓名""
  FROM ""伦理审查会议议程"" g JOIN ""伦理审查申请"" a ON a.""编号""=g.""申请编号""
  LEFT JOIN ""人员"" p ON p.""编号""=a.""申请人编号"" WHERE g.""会议编号""=@会议编号 ORDER BY g.""排序号"",g.""编号"";", new { 会议编号 });
        var minutes = await Db.QuerySingleOrDefaultAsync<object>("SELECT * FROM \"伦理审查会议纪要\" WHERE \"会议编号\"=@会议编号;", new { 会议编号 });
        var archives = await Db.QueryAsync<object>("SELECT * FROM \"伦理电子档案\" WHERE \"会议编号\"=@会议编号 ORDER BY \"建立时间\" DESC;", new { 会议编号 });
        return new { 会议 = meeting, 议程 = agenda, 纪要 = minutes, 电子档案 = archives };
    }

    /// <summary>保存会议纪要草稿或提交待确认；确认必须使用单独接口。</summary>
    [HttpPost]
    public async Task<object> 保存伦理会议纪要([FromBody] 伦理会议纪要保存 model)
    {
        if (!await 是伦理管理角色()) return StatusCode(403, new { error = "仅伦理委员会管理角色可以维护会议纪要" });
        if (model == null || model.会议编号 <= 0 || string.IsNullOrWhiteSpace(model.纪要正文)) throw new ArgumentException("会议编号和纪要正文均为必填项");
        var status = string.IsNullOrWhiteSpace(model.状态) ? "草稿" : model.状态.Trim();
        if (status is not ("草稿" or "待确认")) throw new ArgumentException("会议纪要只能保存为草稿或待确认");
        var meeting = await Db.QuerySingleOrDefaultAsync<dynamic>("SELECT \"编号\",\"状态\" FROM \"伦理审查会议\" WHERE \"编号\"=@编号;", new { 编号 = model.会议编号 });
        if (meeting == null) return NotFound(new { error = "伦理会议不存在" });
        if ((string)meeting.状态 == "已取消") return BadRequest(new { error = "已取消的会议不能提交纪要" });
        var existing = await Db.QuerySingleOrDefaultAsync<dynamic>("SELECT \"状态\" FROM \"伦理审查会议纪要\" WHERE \"会议编号\"=@会议编号;", new { model.会议编号 });
        if (existing != null && (string)existing.状态 is "已确认" or "已归档") return BadRequest(new { error = "已确认或已归档的会议纪要不可覆盖" });
        await Db.ExecuteAsync(@"
INSERT INTO ""伦理审查会议纪要"" (""会议编号"",""纪要正文"",""出席委员摘要"",""决议摘要"",""附件路径"",""状态"",""记录人编号"")
VALUES (@会议编号,@纪要正文,@出席委员摘要,@决议摘要,@附件路径,@状态,@记录人编号)
ON CONFLICT (""会议编号"") DO UPDATE SET ""纪要正文""=EXCLUDED.""纪要正文"",""出席委员摘要""=EXCLUDED.""出席委员摘要"",""决议摘要""=EXCLUDED.""决议摘要"",""附件路径""=EXCLUDED.""附件路径"",""状态""=EXCLUDED.""状态"",""更新时间""=CURRENT_TIMESTAMP,""确认人编号""=NULL,""确认时间""=NULL; 
UPDATE ""伦理审查会议"" SET ""纪要状态""=@状态,""纪要提交时间""=CURRENT_TIMESTAMP,""纪要确认时间""=NULL,""纪要确认人编号""=NULL WHERE ""编号""=@会议编号;", new
        {
            model.会议编号, 纪要正文 = model.纪要正文.Trim(), 出席委员摘要 = 空值(model.出席委员摘要), 决议摘要 = 空值(model.决议摘要),
            附件路径 = 空值(model.附件路径), 状态 = status, 记录人编号 = CurrentUser.编号
        });
        return new { success = true, 会议编号 = model.会议编号, 状态 = status, message = status == "待确认" ? "会议纪要已提交确认" : "会议纪要草稿已保存" };
    }

    /// <summary>确认会议纪要，确认后只允许通过正式档案更正流程留痕。</summary>
    [HttpPost]
    public async Task<object> 确认伦理会议纪要([FromBody] long 会议编号)
    {
        if (!await 是伦理管理角色()) return StatusCode(403, new { error = "仅伦理委员会管理角色可以确认会议纪要" });
        if (会议编号 <= 0) throw new ArgumentException("会议编号无效");
        var changed = await Db.ExecuteAsync(@"
UPDATE ""伦理审查会议纪要"" SET ""状态""='已确认',""确认人编号""=@确认人编号,""确认时间""=CURRENT_TIMESTAMP,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""会议编号""=@会议编号 AND ""状态""='待确认';
UPDATE ""伦理审查会议"" SET ""纪要状态""='已确认',""纪要确认人编号""=@确认人编号,""纪要确认时间""=CURRENT_TIMESTAMP WHERE ""编号""=@会议编号 AND ""纪要状态""='待确认';", new { 会议编号, 确认人编号 = CurrentUser.编号 });
        if (changed < 1) return BadRequest(new { error = "会议不存在或纪要当前不是待确认状态" });
        return new { success = true, 会议编号, 状态 = "已确认", message = "会议纪要已确认" };
    }

    /// <summary>查询正式伦理电子档案。普通人员只能查看自己项目的档案。</summary>
    [HttpGet]
    public async Task<object> 查询伦理电子档案(long? 申请编号 = null, long? 会议编号 = null, string 档案类型 = null, string 状态 = null)
    {
        var manager = await 是伦理管理角色();
        return await Db.QueryAsync<object>(@"
SELECT e.*,a.""申请标题"",a.""申请人编号"",m.""会议序号""
  FROM ""伦理电子档案"" e
  LEFT JOIN ""伦理审查申请"" a ON a.""编号""=e.""申请编号""
  LEFT JOIN ""伦理审查会议"" m ON m.""编号""=e.""会议编号""
 WHERE (@申请编号 IS NULL OR e.""申请编号""=@申请编号)
   AND (@会议编号 IS NULL OR e.""会议编号""=@会议编号)
   AND (@档案类型 IS NULL OR e.""档案类型""=@档案类型)
   AND (@状态 IS NULL OR e.""状态""=@状态)
   AND (@管理角色 OR a.""申请人编号""=@人员编号)
 ORDER BY e.""建立时间"" DESC,e.""编号"" DESC;", new { 申请编号, 会议编号, 档案类型 = 空值(档案类型), 状态 = 空值(状态), 管理角色 = manager, 人员编号 = CurrentUser.编号 });
    }

    /// <summary>登记并立即归档受控文件路径，记录申请、会议或决定关联。</summary>
    [HttpPost]
    public async Task<object> 归档伦理电子材料([FromBody] 伦理电子档案保存 model)
    {
        if (!await 是伦理管理角色()) return StatusCode(403, new { error = "仅伦理委员会管理角色可以归档电子材料" });
        if (model == null || string.IsNullOrWhiteSpace(model.档案类型) || string.IsNullOrWhiteSpace(model.文件路径)) throw new ArgumentException("档案类型和文件路径均为必填项");
        if (!model.申请编号.HasValue && !model.会议编号.HasValue && !model.决定编号.HasValue) throw new ArgumentException("电子档案必须关联申请、会议或决定");
        var archiveType = model.档案类型.Trim();
        if (archiveType is not ("申请材料" or "会议议程" or "会议纪要" or "决定书" or "持续审查" or "不良事件" or "修正案" or "其他")) throw new ArgumentException("档案类型无效");
        var archivePath = model.文件路径.Trim();
        if (archivePath.Length > 2000) throw new ArgumentException("电子档案路径超出限制");
        if (System.Text.RegularExpressions.Regex.IsMatch(archivePath, @"(^|[/\\])(demo|sample|模拟|样例)([/\\]|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            throw new ArgumentException("电子档案不能使用示例或演示文件路径");
        var checksum = 空值(model.文件校验值);
        if (checksum != null && !System.Text.RegularExpressions.Regex.IsMatch(checksum, "^[0-9a-fA-F]{64}$"))
            throw new ArgumentException("文件校验值必须是64位SHA-256十六进制摘要");
        if (archiveType is "会议议程" or "会议纪要" or "决定书" && checksum == null)
            throw new ArgumentException("正式伦理档案必须填写64位SHA-256文件校验值");
        if (archiveType == "决定书" && !model.决定编号.HasValue) throw new ArgumentException("决定书档案必须关联伦理委员会决定");
        if ((archiveType is "会议议程" or "会议纪要") && !model.会议编号.HasValue) throw new ArgumentException("会议档案必须关联伦理委员会会议");
        if ((archiveType is "申请材料" or "持续审查" or "不良事件" or "修正案") && !model.申请编号.HasValue) throw new ArgumentException("申请类档案必须关联伦理审查申请");
        if (model.会议编号.HasValue && archiveType == "会议纪要")
        {
            var confirmed = await Db.QuerySingleAsync<bool>("SELECT EXISTS (SELECT 1 FROM \"伦理审查会议纪要\" WHERE \"会议编号\"=@会议编号 AND \"状态\" IN ('已确认','已归档'));", new { model.会议编号 });
            if (!confirmed) throw new ArgumentException("会议纪要必须先确认后才能归档");
        }
        var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""伦理电子档案"" (""申请编号"",""会议编号"",""决定编号"",""档案类型"",""文件名称"",""文件路径"",""文件校验值"",""状态"",""备注"",""归档人编号"",""归档时间"",""建立人编号"")
VALUES (@申请编号,@会议编号,@决定编号,@档案类型,@文件名称,@文件路径,@文件校验值,'已归档',@备注,@归档人编号,CURRENT_TIMESTAMP,@建立人编号)
RETURNING ""编号"";", new
        {
            model.申请编号, model.会议编号, model.决定编号, 档案类型 = archiveType, 文件名称 = 空值(model.文件名称),
            文件路径 = archivePath, 文件校验值 = checksum, 备注 = 空值(model.备注), 归档人编号 = CurrentUser.编号, 建立人编号 = CurrentUser.编号
        });
        if (model.会议编号.HasValue && archiveType == "会议纪要")
        {
            await Db.ExecuteAsync(@"
UPDATE ""伦理审查会议纪要"" SET ""状态""='已归档',""更新时间""=CURRENT_TIMESTAMP
 WHERE ""会议编号""=@会议编号 AND ""状态""='已确认';
UPDATE ""伦理审查会议"" SET ""纪要状态""='已归档' WHERE ""编号""=@会议编号 AND ""纪要状态""='已确认';", new { model.会议编号 });
        }
        return new { success = true, 编号 = id, 状态 = "已归档", message = "伦理电子材料已归档" };
    }

    private async Task<dynamic> 获取申请(long id) => await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"",""流程模板编号"",""项目编号"",""申请人编号"",""申请类型"",""申请标题"",""研究方案摘要"",""受试者风险等级"",""知情同意说明"",""附件路径"",""状态"",""提交时间"",""受理时间"",""决定时间"",""批件编号"",""有效期至"",""批件路径"",""退回原因"",""备注""
  FROM ""伦理审查申请"" WHERE ""编号""=@id;", new { id });

    private async Task<bool> 有权访问申请(dynamic application)
    {
        if (await 是伦理管理角色()) return true;
        if ((int)application.申请人编号 == CurrentUser.编号) return true;
        return await CanAccessProjectAsync((int)application.流程模板编号, (int)application.项目编号);
    }

    // 兼容既有科研治理角色，同时支持部署方建立独立的伦理秘书/委员角色；正式环境由 Keycloak 角色映射决定实际授权。
    private async Task<bool> 是伦理管理角色() => await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办", "科研科负责人", "科研处处长", "伦理秘书", "伦理委员会秘书", "伦理委员", "伦理委员会委员");

    private static void 校验申请(伦理申请保存 model)
    {
        if (model == null || model.流程模板编号 <= 0 || model.项目编号 <= 0 || string.IsNullOrWhiteSpace(model.申请类型) || string.IsNullOrWhiteSpace(model.申请标题) || string.IsNullOrWhiteSpace(model.研究方案摘要)) throw new ArgumentException("流程模板、项目编号、申请类型、标题和研究方案摘要均为必填项");
        if (model.申请类型.Trim() is not ("初始审查" or "修正案" or "年度跟踪审查" or "延期续展")) throw new ArgumentException("伦理申请类型无效");
    }

    private static void 校验持续记录(伦理持续记录保存 model)
    {
        if (model == null || model.申请编号 <= 0 || string.IsNullOrWhiteSpace(model.记录类型) || string.IsNullOrWhiteSpace(model.事件说明)) throw new ArgumentException("申请编号、记录类型和事件说明均为必填项");
        if (model.记录类型.Trim() is not ("年度跟踪审查" or "方案修正案" or "知情同意修订" or "不良事件" or "偏离方案")) throw new ArgumentException("持续记录类型无效");
        if (!string.IsNullOrWhiteSpace(model.严重程度) && model.严重程度.Trim() is not ("一般" or "严重" or "危及生命")) throw new ArgumentException("不良事件严重程度无效");
        if (model.记录类型.Trim() == "不良事件")
        {
            if (string.IsNullOrWhiteSpace(model.严重程度)) throw new ArgumentException("不良事件必须填写严重程度");
            if (!model.发生日期.HasValue) throw new ArgumentException("不良事件必须填写发生日期");
        }
    }

    private static string 空值(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
