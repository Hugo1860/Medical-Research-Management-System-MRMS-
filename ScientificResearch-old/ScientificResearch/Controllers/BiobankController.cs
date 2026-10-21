using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using Npgsql;
using ScientificResearch.Infrastucture;
using ScientificResearch.Models;

namespace ScientificResearch.Controllers
{
    public class BiobankController : BaseController
    {
        /// <summary>
        /// 获取生物样本资产列表
        /// </summary>
        [HttpGet]
        public async Task<object> 获取生物样本列表(string keyword = null, string sampleType = null, string status = null)
        {
            var sql = @"
SELECT ""编号"", ""样本编码"", ""样本类型"", ""疾病诊断"", ""解剖部位"", ""储存温度"", ""储存位置"",
       ""规格单位"", ""初始数量"", ""在库余量"", ""单价成本"", ""质控等级"", ""知情同意书编号"",
       ""伦理批文号"", ""状态"", ""采集日期"", ""入库日期"", ""备注"", ""建立时间""
  FROM ""生物样本资产""
 WHERE (@sampleType IS NULL OR ""样本类型"" = @sampleType)
   AND (@status IS NULL OR ""状态"" = @status)
   AND (@keyword IS NULL OR (
       ""样本编码"" ILIKE '%' || @keyword || '%' OR
       ""疾病诊断"" ILIKE '%' || @keyword || '%' OR
       ""储存位置"" ILIKE '%' || @keyword || '%' OR
       ""伦理批文号"" ILIKE '%' || @keyword || '%'
   ))
 ORDER BY ""编号"" DESC;";

            var list = await Db.QueryAsync<生物样本资产项>(sql, new
            {
                keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim(),
                sampleType = string.IsNullOrWhiteSpace(sampleType) ? null : sampleType.Trim(),
                status = string.IsNullOrWhiteSpace(status) ? null : status.Trim()
            });

            return list;
        }

        /// <summary>
        /// 获取生物样本借还与消耗核销台账
        /// </summary>
        [HttpGet]
        public async Task<object> 获取生物样本借还台账(string status = null, string keyword = null, int? projectId = null)
        {
            var sql = @"
SELECT l.""编号"", l.""样本编号"", s.""样本编码"", s.""样本类型"", s.""疾病诊断"", s.""规格单位"",
       l.""项目编号"", l.""项目名称"", l.""申请人编号"", l.""申请人姓名"",
       l.""申请用途"", l.""申请数量"", l.""出库数量"", l.""归还数量"", l.""消耗数量"",
       l.""单价成本"", l.""分摊总金额"", l.""伦理批文号"", l.""伦理核验结果"", l.""借还状态"",
       l.""申请时间"", l.""出库时间"", l.""出库经办人"", l.""归还时间"", l.""归还验收人"", l.""核销说明""
  FROM ""生物样本领用借还台账"" l
  JOIN ""生物样本资产"" s ON s.""编号"" = l.""样本编号""
 WHERE (@status IS NULL OR l.""借还状态"" = @status)
   AND (@projectId IS NULL OR l.""项目编号"" = @projectId)
   AND (@keyword IS NULL OR (
       s.""样本编码"" ILIKE '%' || @keyword || '%' OR
       s.""疾病诊断"" ILIKE '%' || @keyword || '%' OR
       l.""项目名称"" ILIKE '%' || @keyword || '%' OR
       l.""申请人姓名"" ILIKE '%' || @keyword || '%' OR
       l.""伦理批文号"" ILIKE '%' || @keyword || '%'
   ))
 ORDER BY l.""编号"" DESC;";

            var list = await Db.QueryAsync<生物样本借还项>(sql, new
            {
                status = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
                projectId,
                keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim()
            });

            return list;
        }

        /// <summary>
        /// 申请生物样本出库领用 (前置伦理合规审查与硬阻断)
        /// </summary>
        [HttpPost]
        public async Task<object> 申请样本领用([FromBody] 申请样本领用请求 req)
        {
            if (req == null || req.样本编号 <= 0)
            {
                return BadRequest(new { error = "请指定有效的样本编号" });
            }
            if (req.申请数量 <= 0)
            {
                return BadRequest(new { error = "申请领用数量必须大于0" });
            }
            if (string.IsNullOrWhiteSpace(req.申请用途))
            {
                return BadRequest(new { error = "必须填写申请实验用途与研究方案" });
            }

            var userId = CurrentUser?.编号 > 0 ? CurrentUser.编号 : 3;

            try
            {
                var result = await Db.QuerySingleAsync<dynamic>(@"
SELECT p_台账编号, p_伦理核验结果, p_提示消息
  FROM sp_生物样本出库领用申请(@p_样本编号, @p_项目编号, @p_申请人编号, @p_申请用途, @p_申请数量);", new
                {
                    p_样本编号 = req.样本编号,
                    p_项目编号 = req.项目编号,
                    p_申请人编号 = userId,
                    p_申请用途 = req.申请用途.Trim(),
                    p_申请数量 = req.申请数量
                });

                var recordId = (int)result.p_台账编号;
                var ethicsResult = (string)result.p_伦理核验结果;
                var message = (string)result.p_提示消息;

                return Ok(new
                {
                    success = true,
                    record_id = recordId,
                    ethics_result = ethicsResult,
                    message
                });
            }
            catch (PostgresException ex)
            {
                return BadRequest(new { error = ex.MessageText });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 样本库管理员核准出库
        /// </summary>
        [HttpPost]
        public async Task<object> 核准样本出库([FromBody] 核准样本出库请求 req)
        {
            if (req == null || req.台账编号 <= 0)
            {
                return BadRequest(new { error = "请指定有效的台账编号" });
            }

            var dispatcher = !string.IsNullOrWhiteSpace(req.出库经办人)
                ? req.出库经办人.Trim()
                : (CurrentUser?.姓名 ?? "样本库管理员");

            try
            {
                var result = await Db.QuerySingleAsync<dynamic>(@"
SELECT p_成功, p_提示消息
  FROM sp_生物样本核准出库(@p_台账编号, @p_出库经办人, @p_实际出库数量);", new
                {
                    p_台账编号 = req.台账编号,
                    p_出库经办人 = dispatcher,
                    p_实际出库数量 = req.实际出库数量
                });

                var success = (bool)result.p_成功;
                var message = (string)result.p_提示消息;

                return Ok(new { success, message });
            }
            catch (PostgresException ex)
            {
                return BadRequest(new { error = ex.MessageText });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 登记生物样本归还与消耗核销 (课题经费资产核算)
        /// </summary>
        [HttpPost]
        public async Task<object> 登记样本归还与消耗([FromBody] 登记样本归还与消耗请求 req)
        {
            if (req == null || req.台账编号 <= 0)
            {
                return BadRequest(new { error = "请指定有效的台账编号" });
            }
            if (req.归还数量 < 0 || req.消耗数量 < 0)
            {
                return BadRequest(new { error = "归还数量与消耗数量不能为负数" });
            }

            var inspector = !string.IsNullOrWhiteSpace(req.验收人)
                ? req.验收人.Trim()
                : (CurrentUser?.姓名 ?? "样本库验收员");

            try
            {
                var result = await Db.QuerySingleAsync<dynamic>(@"
SELECT p_成功, p_实际分摊金额, p_提示消息
  FROM sp_生物样本归还与消耗核销(@p_台账编号, @p_归还数量, @p_消耗数量, @p_验收人, @p_核销说明);", new
                {
                    p_台账编号 = req.台账编号,
                    p_归还数量 = req.归还数量,
                    p_消耗数量 = req.消耗数量,
                    p_验收人 = inspector,
                    p_核销说明 = req.核销说明?.Trim()
                });

                var success = (bool)result.p_成功;
                var allocatedCost = (decimal)result.p_实际分摊金额;
                var message = (string)result.p_提示消息;

                return Ok(new
                {
                    success,
                    allocated_cost = allocatedCost,
                    message
                });
            }
            catch (PostgresException ex)
            {
                return BadRequest(new { error = ex.MessageText });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 新增生物样本资产入库
        /// </summary>
        [HttpPost]
        public async Task<object> 新增样本资产([FromBody] 新增样本资产请求 req)
        {
            if (CurrentUser?.编号 <= 0) return Unauthorized(new { error = "当前登录人员无效，不能登记样本资产" });
            if (req == null || string.IsNullOrWhiteSpace(req.样本编码) || string.IsNullOrWhiteSpace(req.样本类型) || string.IsNullOrWhiteSpace(req.疾病诊断))
            {
                return BadRequest(new { error = "样本编码、类型与疾病诊断均为必填项" });
            }
            if (string.IsNullOrWhiteSpace(req.储存位置))
            {
                return BadRequest(new { error = "储存位置不能为空" });
            }

            var creatorId = CurrentUser.编号;

            var sql = @"
INSERT INTO ""生物样本资产"" (
    ""样本编码"", ""样本类型"", ""疾病诊断"", ""解剖部位"", ""储存温度"", ""储存位置"",
    ""规格单位"", ""初始数量"", ""在库余量"", ""单价成本"", ""质控等级"",
    ""知情同意书编号"", ""伦理批文号"", ""入库经办人编号"", ""备注""
)
VALUES (
    @样本编码, @样本类型, @疾病诊断, @解剖部位, @储存温度, @储存位置,
    @规格单位, @初始数量, @初始数量, @单价成本, @质控等级,
    @知情同意书编号, @伦理批文号, @creatorId, @备注
)
RETURNING ""编号"";";

            try
            {
                var id = await Db.QuerySingleAsync<int>(sql, new
                {
                    样本编码 = req.样本编码.Trim(),
                    样本类型 = req.样本类型.Trim(),
                    疾病诊断 = req.疾病诊断.Trim(),
                    解剖部位 = req.解剖部位?.Trim(),
                    储存温度 = req.储存温度?.Trim() ?? "-80℃",
                    储存位置 = req.储存位置.Trim(),
                    规格单位 = req.规格单位?.Trim() ?? "管",
                    req.初始数量,
                    req.单价成本,
                    质控等级 = req.质控等级?.Trim() ?? "一级合格",
                    知情同意书编号 = req.知情同意书编号?.Trim(),
                    伦理批文号 = req.伦理批文号?.Trim(),
                    creatorId,
                    备注 = req.备注?.Trim()
                });

                return Ok(new { success = true, id, message = "生物样本资产入库登记成功" });
            }
            catch (PostgresException ex)
            {
                return BadRequest(new { error = ex.MessageText });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 获取生物样本资产与使用统计大屏数据
        /// </summary>
        [HttpGet]
        public async Task<object> 获取样本库存统计()
        {
            var totalSpecimens = await Db.QuerySingleAsync<int>(@"SELECT count(*)::integer FROM ""生物样本资产"";");
            var inStockSpecimens = await Db.QuerySingleAsync<int>(@"SELECT count(*)::integer FROM ""生物样本资产"" WHERE ""状态"" = '在库';");
            var loaningCount = await Db.QuerySingleAsync<int>(@"SELECT count(*)::integer FROM ""生物样本领用借还台账"" WHERE ""借还状态"" = '已出库';");
            var totalCostAllocated = await Db.QuerySingleAsync<decimal>(@"SELECT coalesce(sum(""分摊总金额""), 0) FROM ""生物样本领用借还台账"";");

            var typeStats = await Db.QueryAsync<object>(@"
SELECT ""样本类型"", count(*)::integer AS ""样本总数"", sum(""在库余量"") AS ""在库总余量""
  FROM ""生物样本资产""
 GROUP BY ""样本类型""
 ORDER BY ""样本总数"" DESC;");

            var tempStats = await Db.QueryAsync<object>(@"
SELECT ""储存温度"", count(*)::integer AS ""标本数""
  FROM ""生物样本资产""
 GROUP BY ""储存温度""
 ORDER BY ""标本数"" DESC;");

            return new
            {
                总样本数 = totalSpecimens,
                在库样本数 = inStockSpecimens,
                正在实验中借出 = loaningCount,
                课题已分摊总成本 = totalCostAllocated,
                类型统计 = typeStats,
                温区统计 = tempStats
            };
        }
    }
}
