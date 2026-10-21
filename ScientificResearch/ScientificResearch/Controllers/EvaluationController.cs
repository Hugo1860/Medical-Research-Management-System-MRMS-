using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ScientificResearch.Infrastucture;
using ScientificResearch.Models;

namespace ScientificResearch.Controllers
{
    /// <summary>科研评价规则、可解释结果、复核申诉和奖励兑现。</summary>
    public class EvaluationController : BaseController
    {
        [HttpGet]
        async public Task<object> 获取科研评价规则(string 规则名称 = null, string 适用岗位 = null, string 适用学科 = null, string 状态 = null)
        {
            var rows = await Db.QueryAsync<object>(@"
SELECT ""编号"", ""规则名称"", ""版本号"", ""适用岗位"", ""适用学科"", ""适用研究类型"", ""评价周期"", ""生效日期"", ""失效日期"", ""状态"", ""规则说明"", ""创建人编号"", ""创建时间"", ""更新时间""
  FROM ""科研评价规则""
 WHERE (@规则名称 IS NULL OR ""规则名称"" ILIKE '%' || @规则名称 || '%')
   AND (@适用岗位 IS NULL OR ""适用岗位"" ILIKE '%' || @适用岗位 || '%')
   AND (@适用学科 IS NULL OR ""适用学科"" ILIKE '%' || @适用学科 || '%')
   AND (@状态 IS NULL OR ""状态"" = @状态)
 ORDER BY ""规则名称"", ""版本号"" DESC;", new
            {
                规则名称 = 空值(规则名称), 适用岗位 = 空值(适用岗位), 适用学科 = 空值(适用学科), 状态 = 空值(状态)
            });
            return rows;
        }

        [HttpGet]
        async public Task<object> 获取科研评价规则详情(long 规则编号)
        {
            if (规则编号 <= 0) throw new ArgumentException("规则编号无效");
            var rule = await Db.QuerySingleOrDefaultAsync<object>(@"
SELECT ""编号"", ""规则名称"", ""版本号"", ""适用岗位"", ""适用学科"", ""适用研究类型"", ""评价周期"", ""生效日期"", ""失效日期"", ""状态"", ""规则说明""
  FROM ""科研评价规则"" WHERE ""编号""=@规则编号;", new { 规则编号 });
            if (rule == null) return NotFound(new { error = "评价规则不存在" });
            var items = await Db.QueryAsync<object>(@"
SELECT ""编号"", ""规则编号"", ""项目代码"", ""项目名称"", ""项目分类"", ""计分依据"", ""权重"", ""最高分"", ""证据类型"", ""排序"", ""是否启用""
  FROM ""科研评价规则项"" WHERE ""规则编号""=@规则编号 ORDER BY ""排序"", ""编号"";", new { 规则编号 });
            return new { 规则 = rule, 规则项 = items };
        }

        [HttpPost]
        async public Task<object> 保存科研评价规则([FromBody]科研评价规则保存 model)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权维护科研评价规则" });
            if (model == null || string.IsNullOrWhiteSpace(model.规则名称)) throw new ArgumentException("规则名称不能为空");
            if (model.版本号 <= 0 || model.规则名称.Trim().Length > 200 || (model.适用岗位?.Length ?? 0) > 1000 || (model.适用学科?.Length ?? 0) > 1000 || (model.适用研究类型?.Length ?? 0) > 1000 || (model.规则说明?.Length ?? 0) > 4000)
                throw new ArgumentException("评价规则字段超出限制");
            var status = string.IsNullOrWhiteSpace(model.状态) ? "草稿" : model.状态.Trim();
            if (status is not ("草稿" or "生效" or "停用")) throw new ArgumentException("评价规则状态无效");
            if (model.生效日期.HasValue && model.失效日期.HasValue && model.失效日期.Value.Date < model.生效日期.Value.Date)
                throw new ArgumentException("失效日期不能早于生效日期");
            var name = model.规则名称.Trim();
            var cycle = string.IsNullOrWhiteSpace(model.评价周期) ? "年度" : model.评价周期.Trim();
            if (cycle.Length > 30) throw new ArgumentException("评价周期过长");
            if (model.编号.HasValue)
            {
                var updated = await Db.ExecuteAsync(@"
UPDATE ""科研评价规则"" SET ""规则名称""=@规则名称,""版本号""=@版本号,""适用岗位""=@适用岗位,""适用学科""=@适用学科,""适用研究类型""=@适用研究类型,""评价周期""=@评价周期,""生效日期""=@生效日期,""失效日期""=@失效日期,""状态""=@状态,""规则说明""=@规则说明,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@编号;", new
                {
                    编号 = model.编号.Value, 规则名称 = name, model.版本号, 适用岗位 = 空值(model.适用岗位), 适用学科 = 空值(model.适用学科), 适用研究类型 = 空值(model.适用研究类型), 评价周期 = cycle,
                    生效日期 = model.生效日期?.Date, 失效日期 = model.失效日期?.Date, 状态 = status, 规则说明 = 空值(model.规则说明)
                });
                if (updated == 0) return NotFound(new { error = "评价规则不存在" });
                return new { 编号 = model.编号.Value, 状态 = status };
            }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研评价规则"" (""规则名称"",""版本号"",""适用岗位"",""适用学科"",""适用研究类型"",""评价周期"",""生效日期"",""失效日期"",""状态"",""规则说明"",""创建人编号"")
VALUES (@规则名称,@版本号,@适用岗位,@适用学科,@适用研究类型,@评价周期,@生效日期,@失效日期,@状态,@规则说明,@创建人编号)
RETURNING ""编号"";", new
            {
                规则名称 = name, model.版本号, 适用岗位 = 空值(model.适用岗位), 适用学科 = 空值(model.适用学科), 适用研究类型 = 空值(model.适用研究类型), 评价周期 = cycle,
                生效日期 = model.生效日期?.Date, 失效日期 = model.失效日期?.Date, 状态 = status, 规则说明 = 空值(model.规则说明), 创建人编号 = CurrentUser.编号
            });
            return new { 编号 = id, 状态 = status };
        }

        [HttpPost]
        async public Task<object> 保存科研评价规则项([FromBody]科研评价规则项保存 model)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权维护科研评价规则项" });
            if (model == null || model.规则编号 <= 0 || string.IsNullOrWhiteSpace(model.项目代码) || string.IsNullOrWhiteSpace(model.项目名称))
                throw new ArgumentException("规则项代码和名称不能为空");
            if (model.项目代码.Trim().Length > 50 || model.项目名称.Trim().Length > 200 || (model.项目分类?.Length ?? 0) > 100 || (model.计分依据?.Length ?? 0) > 2000 || (model.证据类型?.Length ?? 0) > 50)
                throw new ArgumentException("评价规则项字段超出限制");
            if (model.权重 <= 0 || model.最高分 < 0) throw new ArgumentException("权重和最高分无效");
            if (!await Db.QuerySingleAsync<bool>("SELECT EXISTS (SELECT 1 FROM \"科研评价规则\" WHERE \"编号\"=@规则编号);", new { model.规则编号 }))
                throw new ArgumentException("评价规则不存在");
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研评价规则项"" (""规则编号"",""项目代码"",""项目名称"",""项目分类"",""计分依据"",""权重"",""最高分"",""证据类型"",""排序"",""是否启用"",""建立人编号"")
VALUES (@规则编号,@项目代码,@项目名称,@项目分类,@计分依据,@权重,@最高分,@证据类型,@排序,@是否启用,@建立人编号)
ON CONFLICT (""规则编号"",""项目代码"") DO UPDATE SET ""项目名称""=EXCLUDED.""项目名称"",""项目分类""=EXCLUDED.""项目分类"",""计分依据""=EXCLUDED.""计分依据"",""权重""=EXCLUDED.""权重"",""最高分""=EXCLUDED.""最高分"",""证据类型""=EXCLUDED.""证据类型"",""排序""=EXCLUDED.""排序"",""是否启用""=EXCLUDED.""是否启用"",""更新时间""=CURRENT_TIMESTAMP
RETURNING ""编号"";", new
            {
                model.规则编号, 项目代码 = model.项目代码.Trim(), 项目名称 = model.项目名称.Trim(), 项目分类 = 空值(model.项目分类), 计分依据 = 空值(model.计分依据), model.权重, model.最高分,
                证据类型 = 空值(model.证据类型), model.排序, model.是否启用, 建立人编号 = CurrentUser.编号
            });
            return new { 编号 = id };
        }

        [HttpGet]
        async public Task<object> 获取科研评价年度批次(int? 评价年度 = null)
        {
            var admin = await 是评价管理角色();
            return await Db.QueryAsync<object>(@"
SELECT b.*, COALESCE((SELECT COUNT(*) FROM ""科研评价结果"" r WHERE r.""年度批次编号""=b.""编号""),0) AS ""结果数"",
       COALESCE((SELECT COUNT(*) FROM ""科研评价奖励"" rw JOIN ""科研评价结果"" r ON r.""编号""=rw.""评价结果编号"" WHERE r.""年度批次编号""=b.""编号""),0) AS ""奖励数"",
       COALESCE((SELECT COUNT(*) FROM ""科研评价奖励"" rw JOIN ""科研评价结果"" r ON r.""编号""=rw.""评价结果编号"" WHERE r.""年度批次编号""=b.""编号"" AND rw.""支付状态"" IN ('待支付','支付失败')),0) AS ""待支付数""
  FROM ""科研评价年度批次"" b
 WHERE (@评价年度 IS NULL OR b.""评价年度""=@评价年度)
   AND (@管理角色 OR b.""状态"" IN ('公示中','已发布','已锁定','已结算'))
 ORDER BY b.""评价年度"" DESC, b.""版本号"" DESC, b.""编号"" DESC;", new { 评价年度, 管理角色 = admin });
        }

        [HttpPost]
        async public Task<object> 保存科研评价年度批次([FromBody]科研评价年度批次保存 model)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权维护年度评价批次" });
            if (model == null || model.评价年度 is < 2000 or > 2200) throw new ArgumentException("评价年度无效");
            if (model.版本号 <= 0 || string.IsNullOrWhiteSpace(model.批次名称)) throw new ArgumentException("批次名称和版本号不能为空");
            if ((model.批次名称?.Length ?? 0) > 200 || (model.公示说明?.Length ?? 0) > 4000 || (model.备注?.Length ?? 0) > 2000) throw new ArgumentException("年度批次字段超出限制");
            if (model.公示开始日期.HasValue && model.公示结束日期.HasValue && model.公示结束日期.Value.Date < model.公示开始日期.Value.Date) throw new ArgumentException("公示结束日期不能早于开始日期");
            var status = string.IsNullOrWhiteSpace(model.状态) ? "草稿" : model.状态.Trim();
            if (status is not ("草稿" or "公示中" or "已发布" or "已锁定" or "已结算" or "作废")) throw new ArgumentException("年度批次状态无效，请使用专用发布/锁定接口流转");
            if (status is "公示中" or "已发布" or "已锁定" or "已结算") throw new ArgumentException("正式公示、发布、锁定和结算必须通过专用接口，避免绕过校验");
            if (model.编号.HasValue)
            {
                var current = await Db.QuerySingleOrDefaultAsync<string>("SELECT \"状态\" FROM \"科研评价年度批次\" WHERE \"编号\"=@编号;", new { 编号 = model.编号.Value });
                if (current == null) return NotFound(new { error = "年度批次不存在" });
                if (current is "已锁定" or "已结算") throw new ArgumentException("已锁定或已结算批次不可编辑");
                var changed = await Db.ExecuteAsync(@"
UPDATE ""科研评价年度批次"" SET ""评价年度""=@评价年度,""批次名称""=@批次名称,""版本号""=@版本号,""状态""=@状态,""公示开始日期""=@公示开始日期,""公示结束日期""=@公示结束日期,""公示说明""=@公示说明,""备注""=@备注,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@编号;", new { 编号 = model.编号.Value, model.评价年度, 批次名称 = model.批次名称.Trim(), model.版本号, 状态 = status, 公示开始日期 = model.公示开始日期?.Date, 公示结束日期 = model.公示结束日期?.Date, 公示说明 = 空值(model.公示说明), 备注 = 空值(model.备注) });
                return changed == 0 ? NotFound(new { error = "年度批次不存在" }) : new { 编号 = model.编号.Value, 状态 = status };
            }
            var conflict = await Db.QuerySingleAsync<bool>("SELECT EXISTS (SELECT 1 FROM \"科研评价年度批次\" WHERE \"评价年度\"=@评价年度 AND \"状态\"<>'作废');", new { model.评价年度 });
            if (conflict) throw new ArgumentException("该年度已有有效批次，请使用已有批次或先作废旧版本");
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研评价年度批次"" (""评价年度"",""批次名称"",""版本号"",""状态"",""公示开始日期"",""公示结束日期"",""公示说明"",""备注"",""建立人编号"")
VALUES (@评价年度,@批次名称,@版本号,'草稿',@公示开始日期,@公示结束日期,@公示说明,@备注,@建立人编号) RETURNING ""编号"";", new { model.评价年度, 批次名称 = model.批次名称.Trim(), model.版本号, 公示开始日期 = model.公示开始日期?.Date, 公示结束日期 = model.公示结束日期?.Date, 公示说明 = 空值(model.公示说明), 备注 = 空值(model.备注), 建立人编号 = CurrentUser.编号 });
            return new { 编号 = id, 状态 = "草稿" };
        }

        [HttpPost]
        async public Task<object> 开始公示科研评价年度([FromBody]科研评价年度批次操作 model)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权发起年度评价公示" });
            if (model == null || model.批次编号 <= 0) throw new ArgumentException("批次编号无效");
            var batch = await Db.QuerySingleOrDefaultAsync<dynamic>("SELECT * FROM \"科研评价年度批次\" WHERE \"编号\"=@批次编号;", new { model.批次编号 });
            if (batch == null) return NotFound(new { error = "年度批次不存在" });
            if ((string)batch.状态 != "草稿") throw new ArgumentException("只有草稿批次可以发起公示");
            var pending = await Db.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"科研评价结果\" WHERE \"评价年度\"=@评价年度 AND \"状态\" NOT IN ('已发布','已修订');", new { 评价年度 = (int)batch.评价年度 });
            if (pending > 0) throw new ArgumentException($"仍有 {pending} 条评价未完成复核，不能公示");
            var count = await Db.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"科研评价结果\" WHERE \"评价年度\"=@评价年度 AND \"状态\" IN ('已发布','已修订');", new { 评价年度 = (int)batch.评价年度 });
            if (count == 0) throw new ArgumentException("该年度没有可公示的已发布评价结果");
            var start = model.公示开始日期?.Date ?? DateTime.UtcNow.Date;
            var end = model.公示结束日期?.Date;
            if (end.HasValue && end.Value < start) throw new ArgumentException("公示结束日期不能早于开始日期");
            await using var conn = new NpgsqlConnection(DbConnectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();
            await conn.ExecuteAsync(@"UPDATE ""科研评价年度批次"" SET ""状态""='公示中',""公示开始日期""=@开始,""公示结束日期""=@结束,""公示说明""=COALESCE(@说明,""公示说明""),""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@批次编号;", new { model.批次编号, 开始 = start, 结束 = end, 说明 = 空值(model.公示说明) }, tx);
            await conn.ExecuteAsync(@"UPDATE ""科研评价结果"" SET ""年度批次编号""=@批次编号,""公示状态""='公示中',""更新时间""=CURRENT_TIMESTAMP WHERE ""评价年度""=@评价年度 AND ""状态"" IN ('已发布','已修订');", new { model.批次编号, 评价年度 = (int)batch.评价年度 }, tx);
            await tx.CommitAsync();
            return new { 编号 = model.批次编号, 状态 = "公示中", 结果数 = count };
        }

        [HttpPost]
        async public Task<object> 正式发布科研评价年度([FromBody]科研评价年度批次操作 model)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权正式发布年度评价" });
            if (model == null || model.批次编号 <= 0) throw new ArgumentException("批次编号无效");
            var batch = await Db.QuerySingleOrDefaultAsync<dynamic>("SELECT \"评价年度\",\"状态\" FROM \"科研评价年度批次\" WHERE \"编号\"=@批次编号;", new { model.批次编号 });
            if (batch == null) return NotFound(new { error = "年度批次不存在" });
            if ((string)batch.状态 != "公示中") throw new ArgumentException("只有公示中的批次可以正式发布");
            await using var conn = new NpgsqlConnection(DbConnectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();
            await conn.ExecuteAsync(@"UPDATE ""科研评价年度批次"" SET ""状态""='已发布',""发布人编号""=@人员编号,""发布时间""=CURRENT_TIMESTAMP,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@批次编号;", new { model.批次编号, 人员编号 = CurrentUser.编号 }, tx);
            var count = await conn.ExecuteAsync(@"UPDATE ""科研评价结果"" SET ""公示状态""='已公示',""公示时间""=CURRENT_TIMESTAMP,""更新时间""=CURRENT_TIMESTAMP WHERE ""年度批次编号""=@批次编号 AND ""公示状态""='公示中';", new { model.批次编号 }, tx);
            await tx.CommitAsync();
            return new { 编号 = model.批次编号, 状态 = "已发布", 结果数 = count };
        }

        [HttpPost]
        async public Task<object> 锁定科研评价年度([FromBody]科研评价年度批次操作 model)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权锁定年度评价" });
            if (model == null || model.批次编号 <= 0) throw new ArgumentException("批次编号无效");
            var batch = await Db.QuerySingleOrDefaultAsync<dynamic>("SELECT \"评价年度\",\"状态\" FROM \"科研评价年度批次\" WHERE \"编号\"=@批次编号;", new { model.批次编号 });
            if (batch == null) return NotFound(new { error = "年度批次不存在" });
            if ((string)batch.状态 != "已发布") throw new ArgumentException("只有正式发布批次可以锁定");
            var pending = await Db.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"科研评价申诉\" a JOIN \"科研评价结果\" r ON r.\"编号\"=a.\"评价结果编号\" WHERE r.\"年度批次编号\"=@批次编号 AND a.\"状态\" IN ('待处理','处理中');", new { model.批次编号 });
            if (pending > 0) throw new ArgumentException($"仍有 {pending} 条申诉待处理，不能锁定");
            await using var conn = new NpgsqlConnection(DbConnectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();
            await conn.ExecuteAsync(@"UPDATE ""科研评价年度批次"" SET ""状态""='已锁定',""锁定人编号""=@人员编号,""锁定时间""=CURRENT_TIMESTAMP,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@批次编号;", new { model.批次编号, 人员编号 = CurrentUser.编号 }, tx);
            var count = await conn.ExecuteAsync(@"UPDATE ""科研评价结果"" SET ""锁定时间""=CURRENT_TIMESTAMP,""更新时间""=CURRENT_TIMESTAMP WHERE ""年度批次编号""=@批次编号 AND ""锁定时间"" IS NULL;", new { model.批次编号 }, tx);
            await tx.CommitAsync();
            return new { 编号 = model.批次编号, 状态 = "已锁定", 结果数 = count };
        }

        [HttpPost]
        async public Task<object> 结算科研评价年度([FromBody]科研评价年度批次操作 model)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权结算年度评价" });
            if (model == null || model.批次编号 <= 0) throw new ArgumentException("批次编号无效");
            var status = await Db.QuerySingleOrDefaultAsync<string>("SELECT \"状态\" FROM \"科研评价年度批次\" WHERE \"编号\"=@批次编号;", new { model.批次编号 });
            if (status == null) return NotFound(new { error = "年度批次不存在" });
            if (status != "已锁定") throw new ArgumentException("只有已锁定批次可以结算");
            var pending = await Db.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"科研评价奖励\" rw JOIN \"科研评价结果\" r ON r.\"编号\"=rw.\"评价结果编号\" WHERE r.\"年度批次编号\"=@批次编号 AND rw.\"支付状态\" IN ('待支付','支付失败');", new { model.批次编号 });
            if (pending > 0) throw new ArgumentException($"仍有 {pending} 笔奖励未支付或支付失败，不能结算");
            var changed = await Db.ExecuteAsync(@"UPDATE ""科研评价年度批次"" SET ""状态""='已结算',""结算人编号""=@人员编号,""结算时间""=CURRENT_TIMESTAMP,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@批次编号 AND ""状态""='已锁定';", new { model.批次编号, 人员编号 = CurrentUser.编号 });
            return changed == 0 ? NotFound(new { error = "年度批次不存在或状态已变化" }) : new { 编号 = model.批次编号, 状态 = "已结算" };
        }

        [HttpGet]
        async public Task<object> 获取年度奖励发放台账(int? 评价年度 = null, string 支付状态 = null)
        {
            var admin = await 是评价管理角色();
            return await Db.QueryAsync<object>(@"
SELECT rw.*, r.""评价年度"", r.""总分"", p.""姓名"", p.""工号"", b.""批次名称"", b.""状态"" AS ""批次状态""
  FROM ""科研评价奖励"" rw JOIN ""科研评价结果"" r ON r.""编号""=rw.""评价结果编号""
  JOIN ""人员"" p ON p.""编号""=rw.""人员编号"" LEFT JOIN ""科研评价年度批次"" b ON b.""编号""=r.""年度批次编号""
 WHERE (@评价年度 IS NULL OR r.""评价年度""=@评价年度)
   AND (@支付状态 IS NULL OR rw.""支付状态""=@支付状态)
   AND (@管理角色 OR rw.""人员编号""=@人员编号)
 ORDER BY r.""评价年度"" DESC, p.""姓名"", rw.""编号"" DESC;", new { 评价年度, 支付状态 = 空值(支付状态), 管理角色 = admin, 人员编号 = CurrentUser.编号 });
        }

        /// <summary>
        /// 建立工资/财务支付批次。仅锁定待支付奖励并生成待提交批次，
        /// 不调用外部财务系统，也不写入支付凭证；实际回写必须携带真实凭证完成。
        /// 一个批次只允许同一人员、同一评价年度，幂等键重复提交直接返回原批次。
        /// </summary>
        [HttpPost]
        async public Task<object> 创建科研评价奖励支付批次([FromBody]科研评价奖励支付批次创建 model)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权创建科研评价奖励支付批次" });
            if (model == null || string.IsNullOrWhiteSpace(model.批次号) || string.IsNullOrWhiteSpace(model.财务请求幂等键))
                throw new ArgumentException("批次号和财务请求幂等键不能为空");
            if (model.批次号.Trim().Length > 100 || model.财务请求幂等键.Trim().Length > 150 || (model.财务系统代码?.Length ?? 0) > 50)
                throw new ArgumentException("支付批次字段超出限制");
            if (model.奖励编号列表 == null || model.奖励编号列表.Count == 0 || model.奖励编号列表.Count > 200)
                throw new ArgumentException("必须显式选择 1-200 笔奖励，不能按条件隐式创建批次");
            var rewardIds = model.奖励编号列表.Where(x => x > 0).Distinct().ToArray();
            if (rewardIds.Length != model.奖励编号列表.Count) throw new ArgumentException("奖励编号必须为正整数且不能重复");

            await using var connection = new NpgsqlConnection(DbConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            var existing = await connection.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"",""批次号"",""状态"",""对账状态"",""奖励笔数"",""奖励总额""
  FROM ""科研评价奖励支付批次"" WHERE ""财务请求幂等键""=@财务请求幂等键;",
                new { 财务请求幂等键 = model.财务请求幂等键.Trim() }, transaction);
            if (existing != null)
            {
                await transaction.CommitAsync();
                return new { 编号 = (long)existing.编号, 批次号 = (string)existing.批次号, 状态 = (string)existing.状态, 对账状态 = (string)existing.对账状态, 幂等命中 = true };
            }

            var rewards = (await connection.QueryAsync<dynamic>(@"
SELECT rw.""编号"", rw.""人员编号"", rw.""奖励金额"", rw.""支付状态"", rw.""财务回写状态"", r.""评价年度"", rw.""支付批次编号""
  FROM ""科研评价奖励"" rw
  JOIN ""科研评价结果"" r ON r.""编号""=rw.""评价结果编号""
 WHERE rw.""编号"" = ANY(@奖励编号列表)
 FOR UPDATE;", new { 奖励编号列表 = rewardIds }, transaction)).ToList();
            if (rewards.Count != rewardIds.Length) throw new ArgumentException("存在不存在的奖励记录，批次未创建");
            if (rewards.Any(x => (string)x.支付状态 is not ("待支付" or "支付失败") || (string)x.财务回写状态 is not ("待回写" or "回写失败") || x.支付批次编号 != null))
                throw new ArgumentException("批次只能包含尚未提交、未绑定其他批次的奖励");
            var personIds = rewards.Select(x => (int)x.人员编号).Distinct().ToArray();
            var years = rewards.Select(x => (int)x.评价年度).Distinct().ToArray();
            if (personIds.Length != 1 || years.Length != 1) throw new ArgumentException("一个支付批次只能包含同一人员、同一评价年度的奖励");
            if (rewards.Any(x => x.奖励金额 == null || (decimal)x.奖励金额 <= 0)) throw new ArgumentException("奖励金额必须为正数，不能创建零金额支付批次");
            var total = rewards.Sum(x => (decimal)x.奖励金额);
            var batchId = await connection.QuerySingleAsync<long>(@"
INSERT INTO ""科研评价奖励支付批次"" (""批次号"",""评价年度"",""人员编号"",""财务系统代码"",""财务请求幂等键"",""状态"",""对账状态"",""奖励笔数"",""奖励总额"",""创建人编号"")
VALUES (@批次号,@评价年度,@人员编号,@财务系统代码,@财务请求幂等键,'待提交','未对账',@奖励笔数,@奖励总额,@创建人编号)
RETURNING ""编号"";", new
            {
                批次号 = model.批次号.Trim(), 评价年度 = years[0], 人员编号 = personIds[0], 财务系统代码 = string.IsNullOrWhiteSpace(model.财务系统代码) ? "待配置" : model.财务系统代码.Trim(),
                财务请求幂等键 = model.财务请求幂等键.Trim(), 奖励笔数 = rewards.Count, 奖励总额 = total, 创建人编号 = CurrentUser.编号
            }, transaction);
            await connection.ExecuteAsync(@"
UPDATE ""科研评价奖励"" SET ""支付批次编号""=@支付批次编号,""发放批次号""=@批次号,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号"" = ANY(@奖励编号列表);", new { 支付批次编号 = batchId, 批次号 = model.批次号.Trim(), 奖励编号列表 = rewardIds }, transaction);
            foreach (var rewardId in rewardIds)
            {
                await connection.ExecuteAsync(@"
INSERT INTO ""科研评价奖励支付流水"" (""奖励编号"",""支付批次编号"",""事件类型"",""幂等键"",""快照JSON"",""操作人编号"")
VALUES (@奖励编号,@支付批次编号,'批次创建',@幂等键,jsonb_build_object('状态','待提交','来源','科研评价奖励支付批次'),@操作人编号)
ON CONFLICT (""幂等键"") DO NOTHING;", new { 奖励编号 = rewardId, 支付批次编号 = batchId, 幂等键 = $"{model.财务请求幂等键.Trim()}:reward:{rewardId}", 操作人编号 = CurrentUser.编号 }, transaction);
            }
            await transaction.CommitAsync();
            return new { 编号 = batchId, 批次号 = model.批次号.Trim(), 状态 = "待提交", 对账状态 = "未对账", 奖励笔数 = rewards.Count, 奖励总额 = total, 幂等命中 = false };
        }

        [HttpGet]
        async public Task<object> 获取科研评价奖励支付批次(string 状态 = null, int? 评价年度 = null)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权查看科研评价奖励支付批次" });
            return await Db.QueryAsync<object>(@"
SELECT b.*, p.""姓名"", p.""工号""
  FROM ""科研评价奖励支付批次"" b JOIN ""人员"" p ON p.""编号""=b.""人员编号""
 WHERE (@状态 IS NULL OR b.""状态""=@状态) AND (@评价年度 IS NULL OR b.""评价年度""=@评价年度)
 ORDER BY b.""创建时间"" DESC, b.""编号"" DESC;", new { 状态 = 空值(状态), 评价年度 });
        }

        /// <summary>
        /// 登记财务/工资系统已受理的支付批次。接口只写入受理凭据和不可变提交流水，
        /// 不在应用内假装调用外部财务系统；实际连接器应在取得真实外部批次号后调用本接口。
        /// </summary>
        [HttpPost]
        async public Task<object> 提交科研评价奖励支付批次([FromBody]科研评价奖励支付批次提交 model)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权提交科研评价奖励支付批次" });
            if (model == null || model.批次编号 <= 0 || string.IsNullOrWhiteSpace(model.外部批次号) || string.IsNullOrWhiteSpace(model.提交幂等键))
                throw new ArgumentException("批次编号、外部批次号和提交幂等键不能为空");
            if (model.外部批次号.Trim().Length > 150 || model.提交幂等键.Trim().Length > 120 || (model.提交说明?.Length ?? 0) > 1000)
                throw new ArgumentException("财务提交字段超出限制");
            拒绝演示凭证(model.外部批次号, "外部批次号");
            拒绝演示凭证(model.提交幂等键, "提交幂等键");

            await using var connection = new NpgsqlConnection(DbConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            var key = model.提交幂等键.Trim();
            var already = await connection.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"", ""支付批次编号"", ""外部流水号""
  FROM ""科研评价奖励支付流水""
 WHERE ""支付批次编号""=@批次编号 AND ""事件类型""='提交财务'
   AND (""幂等键""=@幂等键 OR ""幂等键"" LIKE @幂等键 || ':reward:%')
 ORDER BY ""编号"" LIMIT 1;", new { model.批次编号, 幂等键 = key }, transaction);
            if (already != null)
            {
                await transaction.CommitAsync();
                return new { 批次编号 = model.批次编号, 状态 = "已提交财务", 外部批次号 = (string)already.外部流水号, 幂等命中 = true };
            }

            var batch = await connection.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"", ""状态"", ""外部批次号"", ""奖励笔数""
  FROM ""科研评价奖励支付批次"" WHERE ""编号""=@批次编号 FOR UPDATE;", new { model.批次编号 }, transaction);
            if (batch == null) return NotFound(new { error = "支付批次不存在" });
            if ((string)batch.状态 is "已取消" or "已完成") throw new ArgumentException("已取消或已完成批次不能重复提交");
            if ((string)batch.状态 is not ("待提交" or "已失败")) throw new ArgumentException("支付批次当前状态不允许提交");
            var rewardIds = (await connection.QueryAsync<long>(@"
SELECT ""编号"" FROM ""科研评价奖励"" WHERE ""支付批次编号""=@批次编号 FOR UPDATE;", new { model.批次编号 }, transaction)).ToArray();
            if (rewardIds.Length == 0) throw new ArgumentException("支付批次没有关联奖励记录");

            await connection.ExecuteAsync(@"
UPDATE ""科研评价奖励支付批次""
   SET ""外部批次号""=@外部批次号,""状态""='已提交财务',""提交时间""=CURRENT_TIMESTAMP,
       ""最后错误""=NULL,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@批次编号;", new { model.批次编号, 外部批次号 = model.外部批次号.Trim() }, transaction);
            foreach (var rewardId in rewardIds)
            {
                await connection.ExecuteAsync(@"
INSERT INTO ""科研评价奖励支付流水""
    (""奖励编号"",""支付批次编号"",""事件类型"",""幂等键"",""外部流水号"",""快照JSON"",""操作人编号"")
VALUES (@奖励编号,@批次编号,'提交财务',@幂等键,@外部批次号,
        jsonb_build_object('状态','已提交财务','外部批次号',@外部批次号,'说明',@提交说明),@操作人编号)
ON CONFLICT (""幂等键"") DO NOTHING;", new
                {
                    奖励编号 = rewardId, 批次编号 = model.批次编号, 幂等键 = $"{key}:reward:{rewardId}",
                    外部批次号 = model.外部批次号.Trim(), 提交说明 = 空值(model.提交说明), 操作人编号 = CurrentUser.编号
                }, transaction);
            }
            await transaction.CommitAsync();
            return new { 批次编号 = model.批次编号, 状态 = "已提交财务", 外部批次号 = model.外部批次号.Trim(), 奖励笔数 = rewardIds.Length, 幂等命中 = false };
        }

        /// <summary>
        /// 接收财务系统单笔回写。成功回写必须同时具备财务凭证、支付凭证和外部流水号，
        /// 失败回写必须记录原因；同一回写幂等键只允许产生一条不可变流水。
        /// </summary>
        [HttpPost]
        async public Task<object> 回写科研评价奖励支付([FromBody]科研评价奖励财务回写 model)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权回写科研评价奖励支付" });
            if (model == null || model.批次编号 <= 0 || model.奖励编号 <= 0 || string.IsNullOrWhiteSpace(model.外部流水号) || string.IsNullOrWhiteSpace(model.回写幂等键))
                throw new ArgumentException("批次编号、奖励编号、外部流水号和回写幂等键不能为空");
            var status = string.IsNullOrWhiteSpace(model.状态) ? "成功" : model.状态.Trim();
            if (status is not ("成功" or "失败")) throw new ArgumentException("回写状态只能是成功或失败");
            if (model.外部流水号.Trim().Length > 150 || model.回写幂等键.Trim().Length > 170 || (model.财务凭证号?.Length ?? 0) > 100 || (model.支付凭证号?.Length ?? 0) > 100 || (model.发放凭证号?.Length ?? 0) > 100 || (model.失败原因?.Length ?? 0) > 1000 || (model.回写说明?.Length ?? 0) > 1000)
                throw new ArgumentException("财务回写字段超出限制");
            拒绝演示凭证(model.外部流水号, "外部流水号");
            拒绝演示凭证(model.回写幂等键, "回写幂等键");
            if (status == "成功" && (string.IsNullOrWhiteSpace(model.财务凭证号) || string.IsNullOrWhiteSpace(model.支付凭证号)))
                throw new ArgumentException("成功回写必须提供财务凭证号和支付凭证号");
            if (status == "失败" && string.IsNullOrWhiteSpace(model.失败原因)) throw new ArgumentException("失败回写必须记录失败原因");
            拒绝演示凭证(model.财务凭证号, "财务凭证号");
            拒绝演示凭证(model.支付凭证号, "支付凭证号");

            await using var connection = new NpgsqlConnection(DbConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            var key = model.回写幂等键.Trim();
            var already = await connection.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"", ""奖励编号"", ""支付批次编号"", ""事件类型"", ""外部流水号""
  FROM ""科研评价奖励支付流水"" WHERE ""幂等键""=@幂等键 LIMIT 1;", new { 幂等键 = key }, transaction);
            if (already != null)
            {
                if ((long)already.奖励编号 != model.奖励编号 || (long)already.支付批次编号 != model.批次编号)
                    throw new ArgumentException("回写幂等键已被其他奖励或支付批次使用");
                await transaction.CommitAsync();
                return new { 批次编号 = model.批次编号, 奖励编号 = model.奖励编号, 状态 = (string)already.事件类型 == "回写成功" ? "已支付" : "支付失败", 外部流水号 = (string)already.外部流水号, 幂等命中 = true };
            }

            var batch = await connection.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""状态"", ""外部批次号"" FROM ""科研评价奖励支付批次"" WHERE ""编号""=@批次编号 FOR UPDATE;", new { model.批次编号 }, transaction);
            if (batch == null) return NotFound(new { error = "支付批次不存在" });
            if ((string)batch.状态 is "待提交" or "已取消") throw new ArgumentException("支付批次尚未提交财务或已取消，不能回写");
            var reward = await connection.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"", ""支付状态"", ""状态"", ""支付批次编号""
  FROM ""科研评价奖励"" WHERE ""编号""=@奖励编号 AND ""支付批次编号""=@批次编号 FOR UPDATE;", new { model.奖励编号, model.批次编号 }, transaction);
            if (reward == null) return NotFound(new { error = "奖励不存在或不属于该支付批次" });
            if ((string)reward.状态 == "不予发放") throw new ArgumentException("不予发放奖励不能接收支付回写");
            if ((string)reward.支付状态 == "已支付") throw new ArgumentException("该奖励已支付，请使用原回写幂等键重试");

            var success = status == "成功";
            var changed = await connection.ExecuteAsync(@"
UPDATE ""科研评价奖励"" SET
    ""状态""=CASE WHEN @成功 THEN '已发放' ELSE '待发放' END,
    ""发放日期""=CASE WHEN @成功 THEN COALESCE(""发放日期"",CURRENT_DATE) ELSE ""发放日期"" END,
    ""发放凭证号""=CASE WHEN @成功 THEN COALESCE(@发放凭证号,""发放凭证号"",@支付凭证号) ELSE ""发放凭证号"" END,
    ""财务回写状态""=CASE WHEN @成功 THEN '已回写' ELSE '回写失败' END,
    ""财务凭证号""=CASE WHEN @成功 THEN @财务凭证号 ELSE ""财务凭证号"" END,
    ""财务回写说明""=COALESCE(@回写说明,@失败原因,""财务回写说明""),
    ""财务回写时间""=CURRENT_TIMESTAMP,
    ""支付状态""=CASE WHEN @成功 THEN '已支付' ELSE '支付失败' END,
    ""支付凭证号""=COALESCE(@支付凭证号,@外部流水号,""支付凭证号""),
    ""支付失败原因""=CASE WHEN @成功 THEN NULL ELSE @失败原因 END,
    ""支付时间""=COALESCE(@支付时间,CURRENT_TIMESTAMP),
    ""支付尝试次数""=""支付尝试次数""+1,
    ""最后支付尝试时间""=CURRENT_TIMESTAMP,
    ""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@奖励编号 AND ""支付批次编号""=@批次编号;", new
            {
                model.奖励编号, model.批次编号, 成功 = success, 发放凭证号 = 空值(model.发放凭证号), 财务凭证号 = 空值(model.财务凭证号),
                支付凭证号 = 空值(model.支付凭证号), 外部流水号 = model.外部流水号.Trim(), 失败原因 = 空值(model.失败原因), 回写说明 = 空值(model.回写说明), 支付时间 = model.支付时间
            }, transaction);
            if (changed == 0) throw new ArgumentException("奖励状态未发生变化");
            await connection.ExecuteAsync(@"
INSERT INTO ""科研评价奖励支付流水""
    (""奖励编号"",""支付批次编号"",""事件类型"",""幂等键"",""外部流水号"",""财务凭证号"",""支付凭证号"",""错误原因"",""快照JSON"",""操作人编号"")
VALUES (@奖励编号,@批次编号,@事件类型,@幂等键,@外部流水号,@财务凭证号,@支付凭证号,@失败原因,
        jsonb_build_object('状态',@状态,'说明',COALESCE(@回写说明,@失败原因)),@操作人编号);", new
            {
                model.奖励编号, model.批次编号, 事件类型 = success ? "回写成功" : "回写失败", 幂等键 = key,
                外部流水号 = model.外部流水号.Trim(), 财务凭证号 = 空值(model.财务凭证号), 支付凭证号 = string.IsNullOrWhiteSpace(model.支付凭证号) ? model.外部流水号.Trim() : model.支付凭证号.Trim(),
                失败原因 = 空值(model.失败原因), 状态 = success ? "已支付" : "支付失败", 回写说明 = 空值(model.回写说明), 操作人编号 = CurrentUser.编号
            }, transaction);
            await transaction.CommitAsync();
            return new { 批次编号 = model.批次编号, 奖励编号 = model.奖励编号, 状态 = success ? "已支付" : "支付失败", 外部流水号 = model.外部流水号.Trim(), 幂等命中 = false };
        }

        /// <summary>登记外部财务批次对账结果。比较外部快照与库内不可变奖励状态并保留流水。</summary>
        [HttpPost]
        async public Task<object> 对账科研评价奖励支付批次([FromBody]科研评价奖励支付批次对账 model)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权对账科研评价奖励支付批次" });
            if (model == null || model.批次编号 <= 0 || string.IsNullOrWhiteSpace(model.对账幂等键)) throw new ArgumentException("批次编号和对账幂等键不能为空");
            if (model.对账幂等键.Trim().Length > 120 || (model.外部批次号?.Length ?? 0) > 150 || (model.对账说明?.Length ?? 0) > 1000 || model.外部奖励笔数 < 0 || model.外部成功笔数 < 0 || model.外部失败笔数 < 0 || model.外部总额 < 0 || model.外部成功笔数 + model.外部失败笔数 > model.外部奖励笔数)
                throw new ArgumentException("对账数量、金额或字段超出限制");
            拒绝演示凭证(model.对账幂等键, "对账幂等键");
            拒绝演示凭证(model.外部批次号, "外部批次号");

            await using var connection = new NpgsqlConnection(DbConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            var key = model.对账幂等键.Trim();
            var already = await connection.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"", ""奖励编号"", ""支付批次编号"", ""事件类型"", ""外部流水号""
  FROM ""科研评价奖励支付流水""
 WHERE ""支付批次编号""=@批次编号
   AND (""幂等键""=@幂等键 OR ""幂等键"" LIKE @幂等键 || ':reward:%')
 ORDER BY ""编号"" LIMIT 1;", new { model.批次编号, 幂等键 = key }, transaction);
            if (already != null)
            {
                if ((long)already.支付批次编号 != model.批次编号)
                    throw new ArgumentException("对账幂等键已被其他支付批次使用");
                await transaction.CommitAsync();
                return new { 批次编号 = model.批次编号, 对账状态 = (string)already.事件类型 == "对账一致" ? "一致" : "不一致", 幂等命中 = true };
            }
            var batch = await connection.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""状态"", ""外部批次号"" FROM ""科研评价奖励支付批次"" WHERE ""编号""=@批次编号 FOR UPDATE;", new { model.批次编号 }, transaction);
            if (batch == null) return NotFound(new { error = "支付批次不存在" });
            if ((string)batch.状态 is "待提交" or "已提交财务" or "回写中" or "已取消") throw new ArgumentException("支付批次仍未完成财务回写，不能最终对账");
            var actual = await connection.QuerySingleAsync<dynamic>(@"
SELECT COUNT(*)::integer AS ""奖励笔数"", COUNT(*) FILTER (WHERE ""支付状态""='已支付')::integer AS ""成功笔数"",
       COUNT(*) FILTER (WHERE ""支付状态""='支付失败')::integer AS ""失败笔数"", COALESCE(SUM(""奖励金额""),0)::numeric AS ""总额""
  FROM ""科研评价奖励"" WHERE ""支付批次编号""=@批次编号;", new { model.批次编号 }, transaction);
            var consistent = (int)actual.奖励笔数 == model.外部奖励笔数 && (int)actual.成功笔数 == model.外部成功笔数 && (int)actual.失败笔数 == model.外部失败笔数 && decimal.Round((decimal)actual.总额, 2) == decimal.Round(model.外部总额, 2);
            var reconciliation = consistent ? "一致" : "不一致";
            var explanation = consistent ? 空值(model.对账说明) : $"外部快照与库内状态不一致（库内 {actual.奖励笔数}/{actual.成功笔数}/{actual.失败笔数}/{actual.总额}，外部 {model.外部奖励笔数}/{model.外部成功笔数}/{model.外部失败笔数}/{model.外部总额}）";
            await connection.ExecuteAsync(@"
UPDATE ""科研评价奖励支付批次"" SET ""对账状态""=@对账状态,""对账时间""=CURRENT_TIMESTAMP,
       ""最后错误""=CASE WHEN @对账状态='一致' THEN NULL ELSE @说明 END,
       ""外部批次号""=COALESCE(@外部批次号,""外部批次号""),""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@批次编号;", new { model.批次编号, 对账状态 = reconciliation, 说明 = explanation, 外部批次号 = 空值(model.外部批次号) }, transaction);
            var rewardIds = (await connection.QueryAsync<long>(@"
SELECT ""编号"" FROM ""科研评价奖励"" WHERE ""支付批次编号""=@批次编号 FOR UPDATE;", new { model.批次编号 }, transaction)).ToArray();
            foreach (var rewardId in rewardIds)
            {
                await connection.ExecuteAsync(@"
INSERT INTO ""科研评价奖励支付流水""
    (""奖励编号"",""支付批次编号"",""事件类型"",""幂等键"",""外部流水号"",""错误原因"",""快照JSON"",""操作人编号"")
VALUES (@奖励编号,@批次编号,@事件类型,@幂等键,@外部批次号,@错误原因,
        jsonb_build_object('对账状态',@对账状态,'外部奖励笔数',@外部奖励笔数,'外部成功笔数',@外部成功笔数,'外部失败笔数',@外部失败笔数,'外部总额',@外部总额,'说明',@说明),@操作人编号)
ON CONFLICT (""幂等键"") DO NOTHING;", new
                {
                    奖励编号 = rewardId, 批次编号 = model.批次编号, 事件类型 = consistent ? "对账一致" : "对账不一致", 幂等键 = $"{key}:reward:{rewardId}",
                    外部批次号 = 空值(model.外部批次号), 错误原因 = consistent ? null : explanation, 对账状态 = reconciliation, model.外部奖励笔数, model.外部成功笔数, model.外部失败笔数, model.外部总额, 说明 = explanation, 操作人编号 = CurrentUser.编号
                }, transaction);
            }
            await transaction.CommitAsync();
            return new { 批次编号 = model.批次编号, 对账状态 = reconciliation, 库内奖励笔数 = (int)actual.奖励笔数, 库内成功笔数 = (int)actual.成功笔数, 库内失败笔数 = (int)actual.失败笔数, 幂等命中 = false };
        }

        [HttpPost]
        async public Task<object> 生成科研评价([FromBody]科研评价生成 model)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权生成科研评价" });
            if (model == null || model.人员编号 <= 0 || model.规则编号 <= 0 || model.周期开始日期.Date > model.周期结束日期.Date)
                throw new ArgumentException("评价人员、规则或周期无效");
            if (model.明细 == null || model.明细.Count == 0) throw new ArgumentException("评价至少需要一条证据明细");
            if (model.明细.Count > 500) throw new ArgumentException("评价明细不能超过500条");
            var evaluationYear = model.评价年度 ?? model.周期结束日期.Year;
            var yearLocked = await Db.QuerySingleAsync<bool>("SELECT EXISTS (SELECT 1 FROM \"科研评价年度批次\" WHERE \"评价年度\"=@评价年度 AND \"状态\" IN ('已锁定','已结算'));", new { 评价年度 = evaluationYear });
            if (yearLocked) throw new ArgumentException($"{evaluationYear} 年度评价已锁定，不能新增测算");
            if (!await Db.QuerySingleAsync<bool>("SELECT EXISTS (SELECT 1 FROM \"人员\" WHERE \"编号\"=@人员编号 AND \"是否启用\"=TRUE);", new { model.人员编号 }))
                throw new ArgumentException("人员不存在或已停用");
            var rule = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"", ""状态"", ""适用岗位"", ""适用学科"", ""适用研究类型""
  FROM ""科研评价规则"" WHERE ""编号""=@规则编号;", new { model.规则编号 });
            if (rule == null) return NotFound(new { error = "评价规则不存在" });
            if ((string)rule.状态 != "生效") throw new ArgumentException("只有生效状态的规则才能生成评价");
            var existing = await Db.QuerySingleOrDefaultAsync<long?>(@"
SELECT ""编号"" FROM ""科研评价结果"" WHERE ""人员编号""=@人员编号 AND ""规则编号""=@规则编号 AND ""周期开始日期""=@周期开始日期 AND ""周期结束日期""=@周期结束日期;", new
            { model.人员编号, model.规则编号, 周期开始日期 = model.周期开始日期.Date, 周期结束日期 = model.周期结束日期.Date });
            if (existing.HasValue) return Conflict(new { error = "该人员和评价周期已有结果", 评价结果编号 = existing.Value });

            var items = (await Db.QueryAsync<dynamic>(@"
SELECT ""编号"", ""项目代码"", ""项目名称"", ""权重"", ""最高分"", ""证据类型""
  FROM ""科研评价规则项"" WHERE ""规则编号""=@规则编号 AND ""是否启用""=TRUE;", new { model.规则编号 })).ToDictionary(x => (long)x.编号);
            decimal total = 0;
            var detailRows = new List<object>();
            foreach (var detail in model.明细)
            {
                if (!items.TryGetValue(detail.规则项编号, out var item)) throw new ArgumentException($"规则项不存在或未启用: {detail.规则项编号}");
                if (detail.原始值 < 0 || detail.原始值 > (decimal)item.最高分) throw new ArgumentException($"规则项原始分超出范围: {item.项目名称}");
                var evidenceType = string.IsNullOrWhiteSpace(detail.证据类型) ? (string)item.证据类型 : detail.证据类型.Trim();
                var verifyStatus = string.IsNullOrWhiteSpace(detail.核验状态) ? "待核验" : detail.核验状态.Trim();
                if (verifyStatus is not ("待核验" or "已核验" or "不采纳")) throw new ArgumentException("评价明细核验状态无效");
                var score = Math.Round(detail.原始值 * (decimal)item.权重, 2, MidpointRounding.AwayFromZero);
                total += verifyStatus == "不采纳" ? 0 : score;
                var explanation = string.IsNullOrWhiteSpace(detail.解释)
                    ? $"{item.项目名称}: 原始分 {detail.原始值:0.###} × 权重 {(decimal)item.权重:0.###} = {score:0.##}"
                    : detail.解释.Trim();
                if (explanation.Length > 4000) throw new ArgumentException("评价明细解释不能超过4000个字符");
                detailRows.Add(new { item, detail, evidenceType, verifyStatus, score, explanation });
            }
            var person = await Db.QuerySingleAsync<dynamic>(@"SELECT ""岗位名称"", ""学科门类"", ""人员类型"" FROM ""人员"" WHERE ""编号""=@人员编号;", new { model.人员编号 });
            await using var connection = new NpgsqlConnection(DbConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            var resultId = await connection.QuerySingleAsync<long>(@"
INSERT INTO ""科研评价结果"" (""人员编号"",""规则编号"",""评价年度"",""周期开始日期"",""周期结束日期"",""适用岗位"",""适用学科"",""适用研究类型"",""总分"",""状态"",""结果摘要"",""创建人编号"")
VALUES (@人员编号,@规则编号,@评价年度,@周期开始日期,@周期结束日期,@适用岗位,@适用学科,@适用研究类型,@总分,'待复核',@结果摘要,@创建人编号)
RETURNING ""编号"";", new
            {
                model.人员编号, model.规则编号, 评价年度 = evaluationYear,
                周期开始日期 = model.周期开始日期.Date, 周期结束日期 = model.周期结束日期.Date,
                适用岗位 = 空值(model.适用岗位) ?? (string)person.岗位名称, 适用学科 = 空值(model.适用学科) ?? (string)person.学科门类,
                适用研究类型 = 空值(model.适用研究类型) ?? (string)person.人员类型, 总分 = Math.Round(total, 2), 结果摘要 = 空值(model.结果摘要), 创建人编号 = CurrentUser.编号
            }, transaction);
            foreach (dynamic row in detailRows)
            {
                await connection.ExecuteAsync(@"
INSERT INTO ""科研评价明细"" (""评价结果编号"",""规则项编号"",""证据类型"",""证据编号"",""证据标题"",""原始值"",""权重"",""得分"",""解释"",""核验状态"")
VALUES (@评价结果编号,@规则项编号,@证据类型,@证据编号,@证据标题,@原始值,@权重,@得分,@解释,@核验状态);", new
                {
                    评价结果编号 = resultId, 规则项编号 = (long)row.item.编号, 证据类型 = (string)row.evidenceType, 证据编号 = 空值(row.detail.证据编号), 证据标题 = 空值(row.detail.证据标题),
                    原始值 = (decimal)row.detail.原始值, 权重 = (decimal)row.item.权重, 得分 = (decimal)row.score, 解释 = (string)row.explanation, 核验状态 = (string)row.verifyStatus
                }, transaction);
            }
            await transaction.CommitAsync();
            return new { 编号 = resultId, 总分 = Math.Round(total, 2), 状态 = "待复核" };
        }

        [HttpGet]
        async public Task<object> 查询科研评价结果(int? 人员编号 = null, int? 评价年度 = null, string 状态 = null)
        {
            var target = 人员编号 ?? CurrentUser.编号;
            if (target != CurrentUser.编号 && !await 是评价管理角色()) return StatusCode(403, new { error = "无权查看其他人员的评价结果" });
            return await Db.QueryAsync<object>(@"
SELECT r.""编号"", r.""人员编号"", p.""姓名"", r.""规则编号"", q.""规则名称"", q.""版本号"", r.""评价年度"", r.""周期开始日期"", r.""周期结束日期"", r.""总分"", r.""状态"", r.""结果摘要"", r.""复核意见"", r.""年度批次编号"", r.""公示状态"", r.""公示时间"", r.""锁定时间"", b.""批次名称"", b.""状态"" AS ""批次状态""
  FROM ""科研评价结果"" r JOIN ""人员"" p ON p.""编号""=r.""人员编号"" JOIN ""科研评价规则"" q ON q.""编号""=r.""规则编号"" LEFT JOIN ""科研评价年度批次"" b ON b.""编号""=r.""年度批次编号""
 WHERE r.""人员编号""=@人员编号 AND (@评价年度 IS NULL OR r.""评价年度""=@评价年度) AND (@状态 IS NULL OR r.""状态""=@状态)
 ORDER BY r.""周期结束日期"" DESC, r.""编号"" DESC;", new { 人员编号 = target, 评价年度, 状态 = 空值(状态) });
        }

        [HttpGet]
        async public Task<object> 获取科研评价详情(long 评价结果编号)
        {
            if (评价结果编号 <= 0) throw new ArgumentException("评价结果编号无效");
            var result = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT r.*, p.""姓名"", q.""规则名称"", q.""版本号"" FROM ""科研评价结果"" r JOIN ""人员"" p ON p.""编号""=r.""人员编号"" JOIN ""科研评价规则"" q ON q.""编号""=r.""规则编号"" WHERE r.""编号""=@评价结果编号;", new { 评价结果编号 });
            if (result == null) return NotFound(new { error = "评价结果不存在" });
            if ((int)result.人员编号 != CurrentUser.编号 && !await 是评价管理角色()) return StatusCode(403, new { error = "无权查看该评价结果" });
            var detail = await Db.QueryAsync<object>(@"
SELECT d.*, i.""项目代码"", i.""项目名称"", i.""计分依据"" FROM ""科研评价明细"" d JOIN ""科研评价规则项"" i ON i.""编号""=d.""规则项编号"" WHERE d.""评价结果编号""=@评价结果编号 ORDER BY i.""排序"", d.""编号"";", new { 评价结果编号 });
            var appeals = await Db.QueryAsync<object>("SELECT * FROM \"科研评价申诉\" WHERE \"评价结果编号\"=@评价结果编号 ORDER BY \"建立时间\" DESC;", new { 评价结果编号 });
            var rewards = await Db.QueryAsync<object>("SELECT * FROM \"科研评价奖励\" WHERE \"评价结果编号\"=@评价结果编号 ORDER BY \"建立时间\" DESC;", new { 评价结果编号 });
            return new { 评价结果 = result, 明细 = detail, 申诉 = appeals, 奖励 = rewards };
        }

        [HttpPost]
        async public Task<object> 复核科研评价([FromBody]科研评价复核 model)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权复核科研评价" });
            if (model == null || model.评价结果编号 <= 0) throw new ArgumentException("评价结果编号无效");
            var status = string.IsNullOrWhiteSpace(model.状态) ? "已发布" : model.状态.Trim();
            if (status is not ("待复核" or "已发布" or "作废")) throw new ArgumentException("复核状态无效");
            var resultLocked = await Db.QuerySingleAsync<bool>("SELECT EXISTS (SELECT 1 FROM \"科研评价结果\" r LEFT JOIN \"科研评价年度批次\" b ON b.\"编号\"=r.\"年度批次编号\" WHERE r.\"编号\"=@评价结果编号 AND (r.\"锁定时间\" IS NOT NULL OR b.\"状态\" IN ('已锁定','已结算')));", new { model.评价结果编号 });
            if (resultLocked) throw new ArgumentException("该评价结果所属年度已锁定，不能再次复核");
            var changed = await Db.ExecuteAsync(@"UPDATE ""科研评价结果"" SET ""状态""=@状态,""复核意见""=@复核意见,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@评价结果编号;", new
            { model.评价结果编号, 状态 = status, 复核意见 = 空值(model.复核意见) });
            if (changed == 0) return NotFound(new { error = "评价结果不存在" });
            return new { 编号 = model.评价结果编号, 状态 = status };
        }

        [HttpPost]
        async public Task<object> 提交科研评价申诉([FromBody]科研评价申诉保存 model)
        {
            if (model == null || model.评价结果编号 <= 0 || string.IsNullOrWhiteSpace(model.申诉理由)) throw new ArgumentException("评价结果和申诉理由不能为空");
            if (model.申诉理由.Trim().Length > 4000 || (model.期望处理?.Length ?? 0) > 2000 || (model.补充证据说明?.Length ?? 0) > 4000) throw new ArgumentException("申诉字段超出限制");
            var result = await Db.QuerySingleOrDefaultAsync<dynamic>("SELECT \"人员编号\", \"状态\" FROM \"科研评价结果\" WHERE \"编号\"=@评价结果编号;", new { model.评价结果编号 });
            if (result == null) return NotFound(new { error = "评价结果不存在" });
            if ((int)result.人员编号 != CurrentUser.编号 && !await 是评价管理角色()) return StatusCode(403, new { error = "无权申诉该评价结果" });
            if ((string)result.状态 is not ("已发布" or "已修订" or "申诉中")) throw new ArgumentException("当前评价状态不允许申诉");
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研评价申诉"" (""评价结果编号"",""发起人编号"",""申诉理由"",""期望处理"",""补充证据说明"")
VALUES (@评价结果编号,@发起人编号,@申诉理由,@期望处理,@补充证据说明) RETURNING ""编号"";", new
            { model.评价结果编号, 发起人编号 = CurrentUser.编号, 申诉理由 = model.申诉理由.Trim(), 期望处理 = 空值(model.期望处理), 补充证据说明 = 空值(model.补充证据说明) });
            await Db.ExecuteAsync("UPDATE \"科研评价结果\" SET \"状态\"='申诉中',\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@评价结果编号 AND \"状态\"<>'作废';", new { model.评价结果编号 });
            return new { 编号 = id, 状态 = "待处理" };
        }

        [HttpPost]
        async public Task<object> 处理科研评价申诉([FromBody]科研评价申诉处理 model)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权处理科研评价申诉" });
            if (model == null || model.申诉编号 <= 0) throw new ArgumentException("申诉编号无效");
            var status = model.状态?.Trim();
            if (status is not ("处理中" or "接受" or "部分接受" or "驳回")) throw new ArgumentException("申诉处理状态无效");
            if ((model.处理意见?.Length ?? 0) > 4000) throw new ArgumentException("处理意见不能超过4000个字符");
            var appeal = await Db.QuerySingleOrDefaultAsync<long?>("SELECT \"评价结果编号\" FROM \"科研评价申诉\" WHERE \"编号\"=@申诉编号;", new { model.申诉编号 });
            if (!appeal.HasValue) return NotFound(new { error = "申诉不存在" });
            await Db.ExecuteAsync(@"UPDATE ""科研评价申诉"" SET ""状态""=@状态,""处理意见""=@处理意见,""处理人编号""=@处理人编号,""处理时间""=CURRENT_TIMESTAMP WHERE ""编号""=@申诉编号;", new
            { model.申诉编号, 状态 = status, 处理意见 = 空值(model.处理意见), 处理人编号 = CurrentUser.编号 });
            var resultStatus = status switch { "接受" or "部分接受" => "已修订", "驳回" => "已发布", _ => "申诉中" };
            await Db.ExecuteAsync("UPDATE \"科研评价结果\" SET \"状态\"=@状态,\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@评价结果编号;", new { 评价结果编号 = appeal.Value, 状态 = resultStatus });
            return new { 编号 = model.申诉编号, 状态 = status, 评价结果状态 = resultStatus };
        }

        [HttpPost]
        async public Task<object> 登记科研评价奖励([FromBody]科研评价奖励保存 model)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权登记科研评价奖励" });
            if (model == null || model.评价结果编号 <= 0 || string.IsNullOrWhiteSpace(model.奖励名称)) throw new ArgumentException("评价结果和奖励名称不能为空");
            if ((model.奖励名称?.Length ?? 0) > 200 || (model.奖励级别?.Length ?? 0) > 100 || (model.备注?.Length ?? 0) > 2000 || (model.奖励金额.HasValue && model.奖励金额 < 0) || (model.发放凭证号?.Length ?? 0) > 100 || (model.财务凭证号?.Length ?? 0) > 100 || (model.财务回写说明?.Length ?? 0) > 1000 || (model.发放批次号?.Length ?? 0) > 100 || (model.支付凭证号?.Length ?? 0) > 100 || (model.支付失败原因?.Length ?? 0) > 1000) throw new ArgumentException("奖励字段无效");
            var financeStatus = 奖励财务状态(model.财务回写状态);
            var rewardStatus = 奖励状态(model.状态);
            var paymentStatus = 奖励支付状态(model.支付状态);
            if (rewardStatus == "不予发放" && paymentStatus == "待支付") paymentStatus = "不予支付";
            if (rewardStatus != "不予发放" && paymentStatus == "不予支付") throw new ArgumentException("非不予发放奖励不能设置为不予支付");
            if (rewardStatus == "已发放" && (paymentStatus != "已支付" || string.IsNullOrWhiteSpace(model.发放凭证号) || string.IsNullOrWhiteSpace(model.支付凭证号) || financeStatus != "已回写"))
                throw new ArgumentException("已发放奖励必须同时提供发放凭证、支付凭证并完成财务回写，请使用发放确认接口");
            if (financeStatus == "已回写" && string.IsNullOrWhiteSpace(model.财务凭证号)) throw new ArgumentException("已回写必须提供财务凭证号");
            if (paymentStatus == "已支付" && string.IsNullOrWhiteSpace(model.支付凭证号)) throw new ArgumentException("已支付必须提供支付凭证号");
            var result = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT r.""人员编号"",r.""状态"",b.""状态"" AS ""批次状态""
  FROM ""科研评价结果"" r LEFT JOIN ""科研评价年度批次"" b ON b.""编号""=r.""年度批次编号""
 WHERE r.""编号""=@评价结果编号;", new { model.评价结果编号 });
            if (result == null) return NotFound(new { error = "评价结果不存在" });
            if ((string)result.状态 is not ("已发布" or "已修订")) throw new ArgumentException("只有已发布或已修订的评价结果可以登记奖励");
            var resultPerson = (int)result.人员编号;
            if (model.人员编号 > 0 && model.人员编号 != resultPerson) throw new ArgumentException("奖励人员必须与评价结果人员一致");
            var personId = model.人员编号 > 0 ? model.人员编号 : resultPerson;
            var paymentTime = model.支付时间;
            if (paymentStatus is "已支付" or "支付失败") paymentTime ??= DateTime.UtcNow;
            if (model.编号.HasValue)
            {
                var existingPayment = await Db.QuerySingleOrDefaultAsync<string>("SELECT \"支付状态\" FROM \"科研评价奖励\" WHERE \"编号\"=@编号 AND \"评价结果编号\"=@评价结果编号;", new { 编号 = model.编号.Value, model.评价结果编号 });
                if (existingPayment == null) return NotFound(new { error = "奖励记录不存在" });
                if (existingPayment == "已支付" && paymentStatus != "已支付") throw new ArgumentException("已支付奖励不能回退支付状态");
                var changed = await Db.ExecuteAsync(@"
UPDATE ""科研评价奖励"" SET ""奖励名称""=@奖励名称,""奖励级别""=@奖励级别,""奖励金额""=@奖励金额,""状态""=@状态,""发放日期""=@发放日期,""发放凭证号""=@发放凭证号,""财务回写状态""=@财务回写状态,""财务凭证号""=@财务凭证号,""财务回写说明""=@财务回写说明,""财务回写时间""=CASE WHEN @财务回写状态='待回写' THEN NULL ELSE CURRENT_TIMESTAMP END,""发放批次号""=@发放批次号,""支付状态""=@支付状态,""支付凭证号""=@支付凭证号,""支付失败原因""=@支付失败原因,""支付时间""=@支付时间,""备注""=@备注,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@编号 AND ""评价结果编号""=@评价结果编号;", new
                { 编号 = model.编号.Value, model.评价结果编号, 奖励名称 = model.奖励名称.Trim(), 奖励级别 = 空值(model.奖励级别), model.奖励金额, 状态 = rewardStatus, 发放日期 = model.发放日期?.Date, 发放凭证号 = 空值(model.发放凭证号), 财务回写状态 = financeStatus, 财务凭证号 = 空值(model.财务凭证号), 财务回写说明 = 空值(model.财务回写说明), 发放批次号 = 空值(model.发放批次号), 支付状态 = paymentStatus, 支付凭证号 = 空值(model.支付凭证号), 支付失败原因 = 空值(model.支付失败原因), 支付时间 = paymentTime, 备注 = 空值(model.备注) });
                return changed == 0 ? NotFound(new { error = "奖励记录不存在" }) : new { 编号 = model.编号.Value, 状态 = rewardStatus, 支付状态 = paymentStatus };
            }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研评价奖励"" (""评价结果编号"",""人员编号"",""奖励名称"",""奖励级别"",""奖励金额"",""状态"",""发放日期"",""发放凭证号"",""财务回写状态"",""财务凭证号"",""财务回写说明"",""财务回写时间"",""发放批次号"",""支付状态"",""支付凭证号"",""支付失败原因"",""支付时间"",""备注"",""建立人编号"")
VALUES (@评价结果编号,@人员编号,@奖励名称,@奖励级别,@奖励金额,@状态,@发放日期,@发放凭证号,@财务回写状态,@财务凭证号,@财务回写说明,CASE WHEN @财务回写状态='待回写' THEN NULL ELSE CURRENT_TIMESTAMP END,@发放批次号,@支付状态,@支付凭证号,@支付失败原因,@支付时间,@备注,@建立人编号) RETURNING ""编号"";", new
            { model.评价结果编号, 人员编号 = personId, 奖励名称 = model.奖励名称.Trim(), 奖励级别 = 空值(model.奖励级别), model.奖励金额, 状态 = rewardStatus, 发放日期 = model.发放日期?.Date, 发放凭证号 = 空值(model.发放凭证号), 财务回写状态 = financeStatus, 财务凭证号 = 空值(model.财务凭证号), 财务回写说明 = 空值(model.财务回写说明), 发放批次号 = 空值(model.发放批次号), 支付状态 = paymentStatus, 支付凭证号 = 空值(model.支付凭证号), 支付失败原因 = 空值(model.支付失败原因), 支付时间 = paymentTime, 备注 = 空值(model.备注), 建立人编号 = CurrentUser.编号 });
            return new { 编号 = id, 状态 = rewardStatus, 支付状态 = paymentStatus };
        }

        /// <summary>确认奖励实际发放并写入工资/财务支付凭证。</summary>
        [HttpPost]
        async public Task<object> 确认科研评价奖励发放([FromBody]科研评价奖励发放 model)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权确认科研评价奖励发放" });
            if (model == null || model.奖励编号 <= 0 || string.IsNullOrWhiteSpace(model.发放凭证号)) throw new ArgumentException("奖励编号和发放凭证不能为空");
            if (model.发放凭证号.Trim().Length > 100 || (model.财务凭证号?.Length ?? 0) > 100 || (model.财务回写说明?.Length ?? 0) > 1000 || (model.发放批次号?.Length ?? 0) > 100 || (model.支付凭证号?.Length ?? 0) > 100 || (model.支付失败原因?.Length ?? 0) > 1000) throw new ArgumentException("发放凭证字段超出限制");
            var financeStatus = 奖励财务状态(model.财务回写状态);
            var paymentStatus = 奖励支付状态(model.支付状态);
            if (paymentStatus == "已支付" && financeStatus != "已回写")
                throw new ArgumentException("奖励标记为已支付前必须完成财务回写并取得财务凭证");
            if (financeStatus == "已回写" && string.IsNullOrWhiteSpace(model.财务凭证号)) throw new ArgumentException("已回写必须提供财务凭证号");
            if (paymentStatus == "已支付" && string.IsNullOrWhiteSpace(model.支付凭证号)) throw new ArgumentException("已支付必须提供支付凭证号");
            if (paymentStatus == "支付失败" && string.IsNullOrWhiteSpace(model.支付凭证号)) throw new ArgumentException("支付失败必须提供支付凭证或失败流水号");
            var changed = await Db.ExecuteAsync(@"
UPDATE ""科研评价奖励"" SET ""状态""=CASE WHEN @支付状态='已支付' THEN '已发放' ELSE '待发放' END,""发放日期""=COALESCE(@发放日期,CURRENT_DATE),""发放凭证号""=@发放凭证号,""财务回写状态""=@财务回写状态,""财务凭证号""=@财务凭证号,""财务回写说明""=@财务回写说明,""财务回写时间""=CASE WHEN @财务回写状态='待回写' THEN NULL ELSE CURRENT_TIMESTAMP END,""发放批次号""=@发放批次号,""支付状态""=@支付状态,""支付凭证号""=@支付凭证号,""支付失败原因""=@支付失败原因,""支付时间""=CASE WHEN @支付状态='待支付' THEN NULL ELSE COALESCE(@支付时间,CURRENT_TIMESTAMP) END,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@奖励编号 AND ""状态"" <> '不予发放';", new
            { model.奖励编号, 发放日期 = model.发放日期?.Date, 发放凭证号 = model.发放凭证号.Trim(), 财务回写状态 = financeStatus, 财务凭证号 = 空值(model.财务凭证号), 财务回写说明 = 空值(model.财务回写说明), 发放批次号 = 空值(model.发放批次号), 支付状态 = paymentStatus, 支付凭证号 = 空值(model.支付凭证号), 支付失败原因 = 空值(model.支付失败原因), 支付时间 = model.支付时间 });
            if (changed == 0) return NotFound(new { error = "奖励记录不存在或已标记不予发放" });
            return new { 编号 = model.奖励编号, 状态 = paymentStatus == "已支付" ? "已发放" : "待发放", 支付状态 = paymentStatus, 财务回写状态 = financeStatus };
        }

        [HttpGet]
        async public Task<object> 获取科研评价排名(int? 评价年度 = null, long? 规则编号 = null)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权查看科研评价排名" });
            return await Db.QueryAsync<object>(@"
SELECT row_number() OVER (ORDER BY r.""总分"" DESC, p.""姓名"") AS ""排名"", r.""编号"" AS ""评价结果编号"", r.""人员编号"", p.""姓名"", p.""岗位名称"", p.""学科门类"", r.""评价年度"", r.""总分"", r.""状态"", q.""规则名称"", q.""版本号""
  FROM ""科研评价结果"" r JOIN ""人员"" p ON p.""编号""=r.""人员编号"" JOIN ""科研评价规则"" q ON q.""编号""=r.""规则编号""
 WHERE r.""状态"" IN ('已发布','已修订') AND (@评价年度 IS NULL OR r.""评价年度""=@评价年度) AND (@规则编号 IS NULL OR r.""规则编号""=@规则编号)
 ORDER BY r.""总分"" DESC, p.""姓名"";", new { 评价年度, 规则编号 });
        }

        /// <summary>导出已发布评价结果，供管理部门留档和复核。</summary>
        [HttpGet]
        async public Task<IActionResult> 导出科研评价结果(int? 评价年度 = null, long? 规则编号 = null, bool 明细 = false)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权导出科研评价结果" });
            var rows = await Db.QueryAsync<dynamic>(明细 ? @"
SELECT p.""工号"", p.""姓名"", p.""部门名称"", p.""岗位名称"", r.""评价年度"", q.""规则名称"", q.""版本号"", r.""总分"", r.""状态"", r.""复核意见"", i.""项目代码"", i.""项目名称"", d.""证据类型"", d.""证据标题"", d.""原始值"", d.""权重"", d.""得分"", d.""解释"", d.""核验状态""
  FROM ""科研评价结果"" r
  JOIN ""人员"" p ON p.""编号""=r.""人员编号""
  JOIN ""科研评价规则"" q ON q.""编号""=r.""规则编号""
  LEFT JOIN ""科研评价明细"" d ON d.""评价结果编号""=r.""编号""
  LEFT JOIN ""科研评价规则项"" i ON i.""编号""=d.""规则项编号""
 WHERE r.""状态"" IN ('已发布','已修订')
   AND (@评价年度 IS NULL OR r.""评价年度""=@评价年度)
   AND (@规则编号 IS NULL OR r.""规则编号""=@规则编号)
 ORDER BY r.""评价年度"" DESC, r.""总分"" DESC, p.""姓名"", i.""排序"";" : @"
SELECT p.""工号"", p.""姓名"", p.""部门名称"", p.""岗位名称"", r.""评价年度"", q.""规则名称"", q.""版本号"", r.""总分"", r.""状态"", r.""复核意见""
  FROM ""科研评价结果"" r
  JOIN ""人员"" p ON p.""编号""=r.""人员编号""
  JOIN ""科研评价规则"" q ON q.""编号""=r.""规则编号""
 WHERE r.""状态"" IN ('已发布','已修订')
   AND (@评价年度 IS NULL OR r.""评价年度""=@评价年度)
   AND (@规则编号 IS NULL OR r.""规则编号""=@规则编号)
 ORDER BY r.""评价年度"" DESC, r.""总分"" DESC, p.""姓名"";", new { 评价年度, 规则编号 });
            static string Csv(object value) => "\"" + (value?.ToString() ?? string.Empty).Replace("\"", "\"\"") + "\"";
            var header = 明细 ? "工号,姓名,部门,岗位,评价年度,规则名称,版本号,总分,状态,复核意见,项目代码,项目名称,证据类型,证据标题,原始值,权重,得分,解释,核验状态" : "工号,姓名,部门,岗位,评价年度,规则名称,版本号,总分,状态,复核意见";
            var builder = new StringBuilder("\uFEFF" + header + "\r\n");
            foreach (var row in rows)
            {
                var values = new List<object> { row.工号, row.姓名, row.部门名称, row.岗位名称, row.评价年度, row.规则名称, row.版本号, row.总分, row.状态, row.复核意见 };
                if (明细) values.AddRange(new object[] { row.项目代码, row.项目名称, row.证据类型, row.证据标题, row.原始值, row.权重, row.得分, row.解释, row.核验状态 });
                builder.AppendJoin(',', values.Select(Csv));
                builder.Append("\r\n");
            }
            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv; charset=utf-8", $"科研评价报告{(明细 ? "-明细" : "")}-{DateTime.Now:yyyyMMddHHmmss}.csv");
        }

        [HttpGet]
        async public Task<object> 获取科研绩效动态公式列表(long? 规则编号 = null)
        {
            return await Db.QueryAsync<object>(@"
SELECT f.""编号"", f.""规则编号"", r.""规则名称"", f.""指标代码"", f.""指标名称"", f.""指标类别"",
       f.""基础分值"", f.""公式表达式"", f.""一作比例"", f.""通讯比例"", f.""共一比例"", f.""参研比例"",
       f.""分区加成配置JSON"", f.""是否启用"", f.""建立时间"", f.""更新时间""
  FROM ""科研绩效动态公式表"" f
  LEFT JOIN ""科研评价规则"" r ON r.""编号"" = f.""规则编号""
 WHERE (@规则编号 IS NULL OR f.""规则编号"" = @规则编号)
 ORDER BY f.""指标类别"", f.""编号"";", new { 规则编号 });
        }

        [HttpPost]
        async public Task<object> 保存科研绩效动态公式([FromBody]科研绩效动态公式保存 model)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权维护科研绩效动态公式" });
            if (model == null || string.IsNullOrWhiteSpace(model.指标代码) || string.IsNullOrWhiteSpace(model.指标名称) || string.IsNullOrWhiteSpace(model.公式表达式))
                throw new ArgumentException("指标代码、名称和公式表达式不能为空");

            if (model.编号.HasValue && model.编号.Value > 0)
            {
                var updated = await Db.ExecuteAsync(@"
UPDATE ""科研绩效动态公式表""
   SET ""指标名称"" = @指标名称, ""指标类别"" = @指标类别, ""基础分值"" = @基础分值,
       ""公式表达式"" = @公式表达式, ""一作比例"" = @一作比例, ""通讯比例"" = @通讯比例,
       ""共一比例"" = @共一比例, ""参研比例"" = @参研比例, ""分区加成配置JSON"" = @分区加成配置JSON,
       ""是否启用"" = @是否启用, ""更新时间"" = CURRENT_TIMESTAMP
 WHERE ""编号"" = @编号;", new
                {
                    model.编号, 指标名称 = model.指标名称.Trim(), 指标类别 = model.指标类别.Trim(),
                    model.基础分值, 公式表达式 = model.公式表达式.Trim(),
                    model.一作比例, model.通讯比例, model.共一比例, model.参研比例,
                    分区加成配置JSON = 空值(model.分区加成配置JSON), model.是否启用
                });
                if (updated == 0) return NotFound(new { error = "公式不存在" });
                return new { 编号 = model.编号.Value };
            }
            else
            {
                var id = await Db.QuerySingleAsync<int>(@"
INSERT INTO ""科研绩效动态公式表"" (
    ""规则编号"", ""指标代码"", ""指标名称"", ""指标类别"", ""基础分值"",
    ""公式表达式"", ""一作比例"", ""通讯比例"", ""共一比例"", ""参研比例"",
    ""分区加成配置JSON"", ""是否启用"", ""建立人编号""
) VALUES (
    @规则编号, @指标代码, @指标名称, @指标类别, @基础分值,
    @公式表达式, @一作比例, @通讯比例, @共一比例, @参研比例,
    @分区加成配置JSON, @是否启用, @建立人编号
) RETURNING ""编号"";", new
                {
                    model.规则编号, 指标代码 = model.指标代码.Trim(), 指标名称 = model.指标名称.Trim(), 指标类别 = model.指标类别.Trim(),
                    model.基础分值, 公式表达式 = model.公式表达式.Trim(),
                    model.一作比例, model.通讯比例, model.共一比例, model.参研比例,
                    分区加成配置JSON = 空值(model.分区加成配置JSON), model.是否启用, 建立人编号 = CurrentUser.编号
                });
                return new { 编号 = id };
            }
        }

        [HttpPost]
        async public Task<object> 执行年度科研绩效自动测算([FromBody]年度科研绩效测算请求 model)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权执行科研绩效全员测算" });
            var year = model?.评价年度 ?? 2026;
            var res = await Db.QuerySingleAsync<dynamic>(@"
SELECT p_测算人数, p_生成明细数, p_人均得分, p_提示消息
  FROM sp_执行年度科研绩效动态测算(@评价年度, @规则编号, @人员编号);", new
            {
                评价年度 = year,
                规则编号 = model?.规则编号,
                人员编号 = model?.人员编号
            });
            return new
            {
                评价年度 = year,
                测算人数 = (int)res.p_测算人数,
                生成明细数 = (int)res.p_生成明细数,
                人均得分 = (decimal)res.p_人均得分,
                提示消息 = (string)res.p_提示消息
            };
        }

        [HttpGet]
        async public Task<object> 获取自定义报表模板列表(string 报表分类 = null)
        {
            return await Db.QueryAsync<object>(@"
SELECT ""编号"", ""模板代码"", ""模板名称"", ""报表分类"", ""维度配置JSON"", ""度量配置JSON"", ""过滤条件JSON"", ""报表说明"", ""是否预置"", ""建立时间""
  FROM ""科研自定义报表模板""
 WHERE (@报表分类 IS NULL OR ""报表分类"" = @报表分类)
 ORDER BY ""编号"";", new { 报表分类 = 空值(报表分类) });
        }

        [HttpPost]
        async public Task<object> 执行自定义科研报表查询([FromBody]自定义报表查询请求 model)
        {
            var tplCode = string.IsNullOrWhiteSpace(model?.模板代码) ? "GK-HOSPITAL-ANNUAL" : model.模板代码.Trim();
            var year = model?.年度 ?? 2026;
            var rows = await Db.QueryAsync<object>(@"
SELECT * FROM sp_生成自定义科研报表数据(@模板代码, @年度, @部门编号);", new
            {
                模板代码 = tplCode,
                年度 = year,
                部门编号 = model?.部门编号
            });
            return rows;
        }

        [HttpGet]
        async public Task<IActionResult> 导出自定义科研报表(string 模板代码, int? 年度 = null, int? 部门编号 = null)
        {
            var tplCode = string.IsNullOrWhiteSpace(模板代码) ? "GK-HOSPITAL-ANNUAL" : 模板代码.Trim();
            var year = 年度 ?? 2026;
            var rows = await Db.QueryAsync<dynamic>(@"
SELECT * FROM sp_生成自定义科研报表数据(@模板代码, @年度, @部门编号);", new
            {
                模板代码 = tplCode,
                年度 = year,
                部门编号
            });
            static string Csv(object value) => "\"" + (value?.ToString() ?? string.Empty).Replace("\"", "\"\"") + "\"";
            var header = "行标识,科室/部门,统计年度,学术论文总数,一区Top论文数,最高影响因子,纵向立项数,纵向立项经费(元),横向合同数,横向合同金额(元),专利授权数,科研绩效总分,国考科研达标率(%)";
            var builder = new StringBuilder("\uFEFF" + header + "\r\n");
            foreach (var r in rows)
            {
                var values = new object[]
                {
                    r.行标识, r.分组维度, r.统计年度, r.学术论文总数, r.一区Top论文数,
                    r.最高影响因子, r.纵向立项数, r.纵向立项经费, r.横向合同数,
                    r.横向合同金额, r.专利授权数, r.科研绩效总分, r.国考科研达标率
                };
                builder.AppendJoin(',', values.Select(Csv));
                builder.Append("\r\n");
            }
            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv; charset=utf-8", $"{tplCode}-科研报表-{year}-{DateTime.Now:yyyyMMddHHmmss}.csv");
        }

        [HttpGet]
        async public Task<object> 获取移动协同推送配置(bool? 仅启用 = null)
        {
            var 可维护 = await 是评价管理角色();
            return await Db.QueryAsync<object>(@"
SELECT ""编号"", ""通道类型"", ""通道名称"",
       CASE WHEN @可维护 THEN ""Webhook地址"" ELSE NULL END AS ""Webhook地址"",
       CASE WHEN @可维护 THEN ""应用ID"" ELSE NULL END AS ""应用ID"",
       CASE WHEN @可维护 THEN ""基础配置JSON"" ELSE NULL END AS ""基础配置JSON"", ""是否启用"",
       ""配置状态"", ""最后测试时间"", ""最后测试状态"", ""最后测试提示"",
       (NULLIF(BTRIM(""Webhook地址""), '') IS NOT NULL
        AND NULLIF(BTRIM(""应用ID""), '') IS NOT NULL
        AND NULLIF(BTRIM(""应用密钥""), '') IS NOT NULL) AS ""凭据配置完整"",
       (""是否启用"" AND ""配置状态"" = '已验证'
        AND NULLIF(BTRIM(""Webhook地址""), '') IS NOT NULL
        AND NULLIF(BTRIM(""应用ID""), '') IS NOT NULL
        AND NULLIF(BTRIM(""应用密钥""), '') IS NOT NULL) AS ""允许发送"",
       ""建立时间"", ""更新时间""
  FROM ""移动协同消息配置""
 WHERE (@仅启用 IS NULL OR ""是否启用"" = @仅启用)
 ORDER BY ""编号"";", new { 仅启用, 可维护 });
        }

        [HttpPost]
        async public Task<object> 保存移动协同推送配置([FromBody]移动协同消息配置保存 model)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权维护移动协同推送配置" });
            if (model == null || string.IsNullOrWhiteSpace(model.通道类型) || string.IsNullOrWhiteSpace(model.通道名称))
                throw new ArgumentException("通道类型与名称不能为空");
            if (model.编号.HasValue && model.编号.Value > 0)
            {
                var updated = await Db.ExecuteAsync(@"
UPDATE ""移动协同消息配置""
   SET ""通道类型"" = @通道类型, ""通道名称"" = @通道名称, ""Webhook地址"" = @Webhook地址,
       ""应用ID"" = @应用ID, ""应用密钥"" = COALESCE(@应用密钥, ""应用密钥""),
       ""基础配置JSON"" = @基础配置JSON, ""是否启用"" = @是否启用,
       ""配置状态"" = CASE
           WHEN NOT @是否启用 THEN '已停用'
           WHEN NULLIF(BTRIM(COALESCE(@Webhook地址, ""Webhook地址"")), '') IS NOT NULL
            AND NULLIF(BTRIM(COALESCE(@应用ID, ""应用ID"")), '') IS NOT NULL
            AND NULLIF(BTRIM(COALESCE(@应用密钥, ""应用密钥"")), '') IS NOT NULL THEN '待验证'
           ELSE '待配置'
       END,
       ""最后测试状态"" = '未执行', ""最后测试提示"" = NULL,
       ""最后测试时间"" = NULL, ""更新时间"" = CURRENT_TIMESTAMP
 WHERE ""编号"" = @编号;", new
                {
                    model.编号, 通道类型 = model.通道类型.Trim(), 通道名称 = model.通道名称.Trim(),
                    Webhook地址 = 空值(model.Webhook地址), 应用ID = 空值(model.应用ID),
                    应用密钥 = 空值(model.应用密钥), 基础配置JSON = 空值(model.基础配置JSON), model.是否启用
                });
                if (updated == 0) return NotFound(new { error = "配置不存在" });
                return new { 编号 = model.编号.Value };
            }
            else
            {
                var id = await Db.QuerySingleAsync<int>(@"
INSERT INTO ""移动协同消息配置"" (
    ""通道类型"", ""通道名称"", ""Webhook地址"", ""应用ID"", ""应用密钥"", ""基础配置JSON"", ""是否启用"", ""配置状态"", ""建立人编号""
) VALUES (
    @通道类型, @通道名称, @Webhook地址, @应用ID, @应用密钥, @基础配置JSON, @是否启用,
    CASE
        WHEN NOT @是否启用 THEN '已停用'
        WHEN NULLIF(BTRIM(@Webhook地址), '') IS NOT NULL
         AND NULLIF(BTRIM(@应用ID), '') IS NOT NULL
         AND NULLIF(BTRIM(@应用密钥), '') IS NOT NULL THEN '待验证'
        ELSE '待配置'
    END,
    @建立人编号
) RETURNING ""编号"";", new
                {
                    通道类型 = model.通道类型.Trim(), 通道名称 = model.通道名称.Trim(),
                    Webhook地址 = 空值(model.Webhook地址), 应用ID = 空值(model.应用ID),
                    应用密钥 = 空值(model.应用密钥), 基础配置JSON = 空值(model.基础配置JSON), model.是否启用, 建立人编号 = CurrentUser.编号
                });
                return new { 编号 = id };
            }
        }

        [HttpPost]
        async public Task<object> 测试移动协同推送通道(int 通道编号)
        {
            if (!await 是评价管理角色()) return StatusCode(403, new { error = "无权测试移动协同推送通道" });
            var res = await Db.QuerySingleAsync<dynamic>(@"
SELECT p_成功, p_提示消息 FROM sp_测试移动推送通道(@通道编号, @测试人编号);", new
            {
                通道编号,
                测试人编号 = CurrentUser.编号
            });
            return new { 成功 = (bool)res.p_成功, 提示消息 = (string)res.p_提示消息, 真实握手已执行 = false };
        }

        [HttpPost]
        async public Task<object> 发送移动协同待办通知([FromBody]移动待办推送请求 model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.消息标题))
                throw new ArgumentException("推送消息标题不能为空");
            var targetUser = model.接收人编号 > 0 ? model.接收人编号 : CurrentUser.编号;
            if (targetUser != CurrentUser.编号 && !await 是评价管理角色())
                return StatusCode(403, new { error = "只能向自己发送移动待办；跨人员推送需要科研管理员权限" });
            var res = await Db.QuerySingleAsync<dynamic>(@"
SELECT p_日志编号, p_通道名称, p_推送状态, p_提示消息
  FROM sp_发送企业微信待办通知(@消息类型, @业务模块, @业务编号, @接收人编号, @消息标题, @消息摘要, @卡片链接);", new
            {
                消息类型 = string.IsNullOrWhiteSpace(model.消息类型) ? "待办审批" : model.消息类型.Trim(),
                业务模块 = string.IsNullOrWhiteSpace(model.业务模块) ? "工作流" : model.业务模块.Trim(),
                业务编号 = 空值(model.业务编号),
                接收人编号 = targetUser,
                消息标题 = model.消息标题.Trim(),
                消息摘要 = 空值(model.消息摘要),
                卡片链接 = 空值(model.卡片链接)
            });
            return new
            {
                日志编号 = (int)res.p_日志编号,
                通道名称 = (string)res.p_通道名称,
                推送状态 = (string)res.p_推送状态,
                提示消息 = (string)res.p_提示消息,
                是否已真实发送 = string.Equals((string)res.p_推送状态, "已发送", StringComparison.Ordinal)
            };
        }

        [HttpGet]
        async public Task<object> 获取移动待办推送日志(int? 接收人编号 = null, string 业务模块 = null, int limit = 20)
        {
            var target = 接收人编号;
            if (!target.HasValue && !await 是评价管理角色()) target = CurrentUser.编号;
            return await Db.QueryAsync<object>(@"
SELECT l.""编号"", l.""通道编号"", c.""通道名称"", c.""通道类型"", l.""消息类型"", l.""业务模块"", l.""业务编号"",
       l.""接收人编号"", l.""接收人姓名"", l.""接收人账号"", l.""消息标题"", l.""消息摘要"", l.""卡片跳转链接"",
       l.""推送状态"", l.""推送时间""
  FROM ""移动待办推送日志"" l
  LEFT JOIN ""移动协同消息配置"" c ON c.""编号"" = l.""通道编号""
 WHERE (@接收人编号 IS NULL OR l.""接收人编号"" = @接收人编号)
   AND (@业务模块 IS NULL OR l.""业务模块"" = @业务模块)
 ORDER BY l.""推送时间"" DESC, l.""编号"" DESC
 LIMIT @limit;", new { 接收人编号 = target, 业务模块 = 空值(业务模块), limit = Math.Clamp(limit, 1, 100) });
        }

        private async Task<bool> 是评价管理角色() => await HasAnyRoleAsync("系统管理员", "科研管理员");

        private static string 空值(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static void 拒绝演示凭证(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var text = value.Trim();
            var lower = text.ToLowerInvariant();
            if (text.Contains("模拟", StringComparison.Ordinal) || text.Contains("样例", StringComparison.Ordinal) || lower.Contains("demo", StringComparison.Ordinal) || lower.Contains("sample", StringComparison.Ordinal) || lower.Contains("hrp-voucher", StringComparison.Ordinal) || lower.Contains("hrp_voucher", StringComparison.Ordinal) || text.Contains("1580", StringComparison.Ordinal))
                throw new ArgumentException($"{field}不能使用演示或占位凭证");
        }

        private static string 奖励状态(string value)
        {
            var status = string.IsNullOrWhiteSpace(value) ? "待发放" : value.Trim();
            if (status is not ("待发放" or "已发放" or "不予发放")) throw new ArgumentException("奖励状态无效");
            return status;
        }

        private static string 奖励财务状态(string value)
        {
            var status = string.IsNullOrWhiteSpace(value) ? "待回写" : value.Trim();
            if (status is not ("待回写" or "已回写" or "回写失败")) throw new ArgumentException("奖励财务回写状态无效");
            return status;
        }

        private static string 奖励支付状态(string value)
        {
            var status = string.IsNullOrWhiteSpace(value) ? "待支付" : value.Trim();
            if (status is not ("待支付" or "已支付" or "支付失败" or "不予支付")) throw new ArgumentException("奖励支付状态无效");
            return status;
        }
    }
}
