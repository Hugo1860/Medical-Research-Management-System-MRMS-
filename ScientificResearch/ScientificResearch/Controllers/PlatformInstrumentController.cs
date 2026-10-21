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
    /// 科研平台大型仪器设备、使用资质、预约防冲突、计费核算与平台年报
    /// </summary>
    public sealed class PlatformInstrumentController : BaseController
    {
        private async Task<bool> IsManager() => await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办");
        private static string NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        #region 仪器设备台账

        [HttpGet]
        public async Task<object> 查询仪器设备(int? 平台编号 = null)
        {
            var list = await Db.QueryAsync<dynamic>(@"
SELECT i.*, p.""平台名称"", u.""姓名"" as ""管理员姓名""
FROM ""平台仪器设备"" i
JOIN ""科研平台"" p ON i.""平台编号"" = p.""编号""
LEFT JOIN ""人员"" u ON i.""管理员编号"" = u.""编号""
WHERE (@平台编号 IS NULL OR i.""平台编号""=@平台编号)
ORDER BY i.""创建时间"" DESC;", new { 平台编号 });

            return list;
        }

        [HttpPost]
        public async Task<object> 保存仪器设备([FromBody] PlatformInstrumentSaveModel model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权维护平台仪器设备" });
            if (model == null || model.平台编号 <= 0 || string.IsNullOrWhiteSpace(model.资产编号) || string.IsNullOrWhiteSpace(model.设备名称))
                throw new ArgumentException("平台、资产编号和设备名称不能为空");

            if (model.编号 > 0)
            {
                await Db.ExecuteAsync(@"
UPDATE ""平台仪器设备""
SET ""平台编号""=@平台编号, ""资产编号""=@资产编号, ""设备名称""=@设备名称, ""型号规格""=@型号规格,
    ""生产厂家""=@生产厂家, ""购置日期""=@购置日期, ""原值""=@原值, ""存放地点""=@存放地点,
    ""管理员编号""=@管理员编号, ""开放状态""=@开放状态, ""计费方式""=@计费方式,
    ""内部单价""=@内部单价, ""外部单价""=@外部单价, ""最短预约分钟""=@最短预约分钟,
    ""最长预约分钟""=@最长预约分钟, ""需培训资质""=@需培训资质, ""备注""=@备注,
    ""更新时间""=CURRENT_TIMESTAMP
WHERE ""编号""=@编号;", new {
                    model.编号,
                    model.平台编号,
                    资产编号 = model.资产编号.Trim(),
                    设备名称 = model.设备名称.Trim(),
                    型号规格 = NullIfEmpty(model.型号规格),
                    生产厂家 = NullIfEmpty(model.生产厂家),
                    model.购置日期,
                    原值 = model.原值 < 0 ? 0 : model.原值,
                    存放地点 = NullIfEmpty(model.存放地点),
                    管理员编号 = model.管理员编号 <= 0 ? CurrentUser.编号 : model.管理员编号,
                    开放状态 = string.IsNullOrWhiteSpace(model.开放状态) ? "开放" : model.开放状态.Trim(),
                    计费方式 = string.IsNullOrWhiteSpace(model.计费方式) ? "按小时" : model.计费方式.Trim(),
                    内部单价 = model.内部单价 < 0 ? 0 : model.内部单价,
                    外部单价 = model.外部单价 < 0 ? 0 : model.外部单价,
                    最短预约分钟 = model.最短预约分钟 <= 0 ? 30 : model.最短预约分钟,
                    最长预约分钟 = model.最长预约分钟 <= 0 ? 480 : model.最长预约分钟,
                    model.需培训资质,
                    备注 = NullIfEmpty(model.备注)
                });
                return new { model.编号 };
            }
            else
            {
                var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""平台仪器设备"" (""平台编号"",""资产编号"",""设备名称"",""型号规格"",""生产厂家"",""购置日期"",""原值"",""存放地点"",""管理员编号"",""开放状态"",""计费方式"",""内部单价"",""外部单价"",""最短预约分钟"",""最长预约分钟"",""需培训资质"",""备注"")
VALUES (@平台编号,@资产编号,@设备名称,@型号规格,@生产厂家,@购置日期,@原值,@存放地点,@管理员编号,@开放状态,@计费方式,@内部单价,@外部单价,@最短预约分钟,@最长预约分钟,@需培训资质,@备注)
RETURNING ""编号"";", new {
                    model.平台编号,
                    资产编号 = model.资产编号.Trim(),
                    设备名称 = model.设备名称.Trim(),
                    型号规格 = NullIfEmpty(model.型号规格),
                    生产厂家 = NullIfEmpty(model.生产厂家),
                    model.购置日期,
                    原值 = model.原值 < 0 ? 0 : model.原值,
                    存放地点 = NullIfEmpty(model.存放地点),
                    管理员编号 = model.管理员编号 <= 0 ? CurrentUser.编号 : model.管理员编号,
                    开放状态 = string.IsNullOrWhiteSpace(model.开放状态) ? "开放" : model.开放状态.Trim(),
                    计费方式 = string.IsNullOrWhiteSpace(model.计费方式) ? "按小时" : model.计费方式.Trim(),
                    内部单价 = model.内部单价 < 0 ? 0 : model.内部单价,
                    外部单价 = model.外部单价 < 0 ? 0 : model.外部单价,
                    最短预约分钟 = model.最短预约分钟 <= 0 ? 30 : model.最短预约分钟,
                    最长预约分钟 = model.最长预约分钟 <= 0 ? 480 : model.最长预约分钟,
                    model.需培训资质,
                    备注 = NullIfEmpty(model.备注)
                });
                return new { 编号 = id };
            }
        }

        #endregion

        #region 仪器预约与防冲突机制

        [HttpGet]
        public async Task<object> 查询仪器预约(long 仪器编号)
        {
            return await Db.QueryAsync<dynamic>(@"
SELECT b.*, u.""姓名"" as ""预约人姓名"", a.""姓名"" as ""审核人姓名""
FROM ""仪器预约"" b
JOIN ""人员"" u ON b.""预约人编号"" = u.""编号""
LEFT JOIN ""人员"" a ON b.""审核人编号"" = a.""编号""
WHERE b.""仪器编号""=@仪器编号
ORDER BY b.""开始时间"" DESC;", new { 仪器编号 });
        }

        [HttpPost]
        public async Task<object> 提交仪器预约([FromBody] InstrumentBookingApplyModel model)
        {
            if (model == null || model.仪器编号 <= 0 || model.开始时间 >= model.结束时间)
                throw new ArgumentException("预约时段无效");

            using var trans = Db.BeginTransaction();
            try
            {
                // 1. 锁住仪器记录
                var inst = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT * FROM ""平台仪器设备"" WHERE ""编号""=@仪器编号 FOR UPDATE;", new { model.仪器编号 }, trans);
                if (inst == null) return NotFound(new { error = "仪器设备不存在" });
                if (inst.开放状态 != "开放" && inst.开放状态 != "仅内部")
                    return StatusCode(409, new { error = $"仪器当前处于【{inst.开放状态}】状态，无法预约" });

                // 2. 检查资质
                if ((bool)inst.需培训资质)
                {
                    var hasQual = await Db.QuerySingleOrDefaultAsync<int>(@"
SELECT COUNT(1) FROM ""仪器使用资质""
WHERE ""仪器编号""=@仪器编号 AND ""人员编号""=@userId AND ""状态""='有效'
  AND (""有效期至"" IS NULL OR ""有效期至"" >= CURRENT_DATE);", new { model.仪器编号, userId = CurrentUser.编号 }, trans);
                    if (hasQual == 0) return StatusCode(403, new { error = "该仪器需持培训资质预约，您当前暂无有效资质" });
                }

                // 3. 严格事务级防时间重叠冲突检测 (对使用中或已批准的预约)
                var conflict = await Db.QuerySingleOrDefaultAsync<int>(@"
SELECT COUNT(1) FROM ""仪器预约""
WHERE ""仪器编号""=@仪器编号
  AND ""状态"" IN ('待审核','已批准','使用中')
  AND (""开始时间"" < @结束时间 AND ""结束时间"" > @开始时间);", new {
                    model.仪器编号,
                    model.开始时间,
                    model.结束时间
                }, trans);

                if (conflict > 0)
                    return StatusCode(409, new { error = "该时段已被其他预约占用，请调整预约时间" });

                var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""仪器预约"" (""仪器编号"",""预约人编号"",""流程模板编号"",""项目编号"",""开始时间"",""结束时间"",""样品数"",""用途说明"",""状态"")
VALUES (@仪器编号,@预约人编号,@流程模板编号,@项目编号,@开始时间,@结束时间,@样品数,@用途说明,'待审核')
RETURNING ""编号"";", new {
                    model.仪器编号,
                    预约人编号 = CurrentUser.编号,
                    model.流程模板编号,
                    model.项目编号,
                    model.开始时间,
                    model.结束时间,
                    样品数 = model.样品数 <= 0 ? 1 : model.样品数,
                    用途说明 = NullIfEmpty(model.用途说明) ?? "科研实验"
                }, trans);

                trans.Commit();
                return new { 编号 = id, 状态 = "待审核" };
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        [HttpPost]
        public async Task<object> 审核仪器预约([FromBody] InstrumentBookingAuditModel model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权审核仪器预约" });
            if (model == null || model.编号 <= 0 || model.审核结果 is not ("已批准" or "已拒绝"))
                throw new ArgumentException("审核参数无效");

            var changed = await Db.ExecuteAsync(@"
UPDATE ""仪器预约""
SET ""状态""=@审核结果, ""审核人编号""=@审核人编号, ""审核时间""=CURRENT_TIMESTAMP, ""审核意见""=@审核意见
WHERE ""编号""=@编号 AND ""状态""='待审核';", new {
                model.编号,
                model.审核结果,
                审核人编号 = CurrentUser.编号,
                审核意见 = NullIfEmpty(model.审核意见)
            });

            if (changed == 0) return StatusCode(409, new { error = "预约状态已改变或记录不存在" });
            return new { model.编号, 状态 = model.审核结果 };
        }

        #endregion

        #region 使用记录与计费核算

        [HttpPost]
        public async Task<object> 登记仪器使用([FromBody] InstrumentUsageRegisterModel model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权登记仪器使用与核算" });
            if (model == null || model.预约编号 <= 0 || model.实际开始时间 >= model.实际结束时间)
                throw new ArgumentException("使用时段无效");

            using var trans = Db.BeginTransaction();
            try
            {
                var booking = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT b.*, i.""计费方式"", i.""内部单价"", i.""外部单价""
FROM ""仪器预约"" b
JOIN ""平台仪器设备"" i ON b.""仪器编号"" = i.""编号""
WHERE b.""编号""=@预约编号 FOR UPDATE;", new { model.预约编号 }, trans);

                if (booking == null) return NotFound(new { error = "预约记录不存在" });

                TimeSpan diff = model.实际结束时间 - model.实际开始时间;
                decimal hours = Math.Round((decimal)diff.TotalHours, 2);
                decimal unitPrice = (decimal)booking.内部单价;
                decimal fee = 0;

                string billingType = booking.计费方式;
                if (billingType == "按小时")
                {
                    fee = Math.Round(hours * unitPrice, 2);
                }
                else if (billingType == "按样品")
                {
                    fee = Math.Round(model.实际样品数 * unitPrice, 2);
                }
                else if (billingType == "按次")
                {
                    fee = unitPrice;
                }
                else
                {
                    fee = 0; // 免费
                }

                var usageId = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""仪器使用记录"" (""预约编号"",""实际开始时间"",""实际结束时间"",""实际样品数"",""实际机时小时"",""单价快照"",""计费金额"",""计费状态"",""结算凭证号"",""登记人编号"",""备注"")
VALUES (@预约编号,@实际开始时间,@实际结束时间,@实际样品数,@hours,@unitPrice,@fee,'已确认',@结算凭证号,@登记人编号,@备注)
ON CONFLICT (""预约编号"") DO UPDATE SET
    ""实际开始时间""=EXCLUDED.""实际开始时间"",
    ""实际结束时间""=EXCLUDED.""实际结束时间"",
    ""实际样品数""=EXCLUDED.""实际样品数"",
    ""实际机时小时""=EXCLUDED.""实际机时小时"",
    ""单价快照""=EXCLUDED.""单价快照"",
    ""计费金额""=EXCLUDED.""计费金额"",
    ""结算凭证号""=EXCLUDED.""结算凭证号"",
    ""备注""=EXCLUDED.""备注""
RETURNING ""编号"";", new {
                    model.预约编号,
                    model.实际开始时间,
                    model.实际结束时间,
                    实际样品数 = model.实际样品数 <= 0 ? 1 : model.实际样品数,
                    hours,
                    unitPrice,
                    fee,
                    结算凭证号 = NullIfEmpty(model.结算凭证号),
                    登记人编号 = CurrentUser.编号,
                    备注 = NullIfEmpty(model.备注)
                }, trans);

                await Db.ExecuteAsync(@"UPDATE ""仪器预约"" SET ""状态""='已完成' WHERE ""编号""=@预约编号;", new { model.预约编号 }, trans);

                trans.Commit();
                return new { 编号 = usageId, 计费金额 = fee, 机时小时 = hours };
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        #endregion

        #region 平台年报

        [HttpGet]
        public async Task<object> 查询平台年报(int 平台编号)
        {
            return await Db.QueryAsync<dynamic>(@"
SELECT r.*, u.""姓名"" as ""提交人姓名"", a.""姓名"" as ""审核人姓名""
FROM ""平台年报"" r
LEFT JOIN ""人员"" u ON r.""提交人编号"" = u.""编号""
LEFT JOIN ""人员"" a ON r.""审核人编号"" = a.""编号""
WHERE r.""平台编号""=@平台编号
ORDER BY r.""年度"" DESC;", new { 平台编号 });
        }

        [HttpPost]
        public async Task<object> 生成平台年报([FromBody] GenerateAnnualReportModel model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权生成平台年报" });
            if (model == null || model.平台编号 <= 0 || model.年度 < 2000)
                throw new ArgumentException("平台编号与年度无效");

            // 统计平台真实数据
            var instCount = await Db.QuerySingleAsync<int>(@"
SELECT COUNT(1) FROM ""平台仪器设备"" WHERE ""平台编号""=@平台编号;", new { model.平台编号 });

            var memberCount = await Db.QuerySingleAsync<int>(@"
SELECT COUNT(1) FROM ""科研平台成员"" WHERE ""平台编号""=@平台编号;", new { model.平台编号 });

            var bookingCount = await Db.QuerySingleAsync<int>(@"
SELECT COUNT(1) FROM ""仪器预约"" b
JOIN ""平台仪器设备"" i ON b.""仪器编号"" = i.""编号""
WHERE i.""平台编号""=@平台编号 AND EXTRACT(YEAR FROM b.""开始时间"") = @年度;", new { model.平台编号, model.年度 });

            var stats = new {
                平台编号 = model.平台编号,
                统计年度 = model.年度,
                仪器设备总数 = instCount,
                平台成员总数 = memberCount,
                年度预约次数 = bookingCount
            };

            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""平台年报"" (""平台编号"",""年度"",""状态"",""统计快照"",""年度总结"",""提交人编号"",""提交时间"")
VALUES (@平台编号,@年度,'草稿',@snap::jsonb,@年度总结,@提交人编号,CURRENT_TIMESTAMP)
ON CONFLICT (""平台编号"",""年度"") DO UPDATE SET
    ""统计快照""=EXCLUDED.""统计快照"",
    ""年度总结""=COALESCE(EXCLUDED.""年度总结"", ""平台年报"".""年度总结""),
    ""更新时间""=CURRENT_TIMESTAMP
RETURNING ""编号"";", new {
                model.平台编号,
                model.年度,
                snap = JsonSerializer.Serialize(stats),
                年度总结 = NullIfEmpty(model.年度总结),
                提交人编号 = CurrentUser.编号
            });

            return new { 编号 = id, 统计 = stats };
        }

        #endregion
    }

    public sealed class PlatformInstrumentSaveModel
    {
        public long 编号 { get; set; }
        public int 平台编号 { get; set; }
        public string 资产编号 { get; set; }
        public string 设备名称 { get; set; }
        public string 型号规格 { get; set; }
        public string 生产厂家 { get; set; }
        public DateTime? 购置日期 { get; set; }
        public decimal 原值 { get; set; }
        public string 存放地点 { get; set; }
        public int 管理员编号 { get; set; }
        public string 开放状态 { get; set; } = "开放";
        public string 计费方式 { get; set; } = "按小时";
        public decimal 内部单价 { get; set; }
        public decimal 外部单价 { get; set; }
        public int 最短预约分钟 { get; set; } = 30;
        public int 最长预约分钟 { get; set; } = 480;
        public bool 需培训资质 { get; set; }
        public string 备注 { get; set; }
    }

    public sealed class InstrumentBookingApplyModel
    {
        public long 仪器编号 { get; set; }
        public int? 流程模板编号 { get; set; }
        public int? 项目编号 { get; set; }
        public DateTime 开始时间 { get; set; }
        public DateTime 结束时间 { get; set; }
        public int 样品数 { get; set; } = 1;
        public string 用途说明 { get; set; }
    }

    public sealed class InstrumentBookingAuditModel
    {
        public long 编号 { get; set; }
        public string 审核结果 { get; set; }
        public string 审核意见 { get; set; }
    }

    public sealed class InstrumentUsageRegisterModel
    {
        public long 预约编号 { get; set; }
        public DateTime 实际开始时间 { get; set; }
        public DateTime 实际结束时间 { get; set; }
        public int 实际样品数 { get; set; } = 1;
        public string 结算凭证号 { get; set; }
        public string 备注 { get; set; }
    }

    public sealed class GenerateAnnualReportModel
    {
        public int 平台编号 { get; set; }
        public int 年度 { get; set; }
        public string 年度总结 { get; set; }
    }
}
