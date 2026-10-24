using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using ScientificResearch.Infrastucture;
using ScientificResearch.Models;

namespace ScientificResearch.Controllers
{
    /// <summary>
    /// 县域医共体多院区/分院科教协同、进修接收、下乡指导与联合课题管理
    /// </summary>
    public class CommunityHealthcareController : BaseController
    {
        [HttpGet]
        public async Task<object> 查询医共体单位列表(string 关键词 = null, string 单位类型 = null)
        {
            var where = "WHERE 1=1";
            if (!string.IsNullOrWhiteSpace(关键词)) where += " AND (\"单位名称\" ILIKE '%' || @关键词 || '%' OR \"行政区划\" ILIKE '%' || @关键词 || '%')";
            if (!string.IsNullOrWhiteSpace(单位类型)) where += " AND \"单位类型\" = @单位类型";

            var list = await Db.QueryAsync<object>($@"
SELECT u.*,
       COALESCE((SELECT count(*)::integer FROM ""医共体人员进修与帮扶记录"" r WHERE r.""合作单位编号"" = u.""编号""), 0) AS ""帮扶与进修人次"",
       COALESCE((SELECT count(*)::integer FROM ""医共体适宜技术推广"" t WHERE t.""受援基层单位"" = u.""单位名称""), 0) AS ""适宜技术推广场次""
FROM ""医共体合作单位"" u
{where}
ORDER BY u.""编号"" ASC;", new { 关键词, 单位类型 });

            return list;
        }

        [HttpPost]
        public async Task<object> 保存医共体单位([FromBody] 医共体合作单位增改 model)
        {
            if (model == null) throw new ArgumentException("缺少参数");
            if (string.IsNullOrWhiteSpace(model.单位名称)) throw new ArgumentException("单位名称不能为空");
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办", "院领导"))
                return StatusCode(403, new { error = "无权维护医共体合作单位" });

            if (model.编号.HasValue && model.编号.Value > 0)
            {
                await Db.ExecuteAsync(@"
UPDATE ""医共体合作单位"" SET
    ""单位名称"" = @单位名称,
    ""单位类型"" = @单位类型,
    ""行政区划"" = @行政区划,
    ""床位数"" = @床位数,
    ""卫技人员数"" = @卫技人员数,
    ""对接联系人"" = @对接联系人,
    ""联系电话"" = @联系电话,
    ""合作协议签署日期"" = @合作协议签署日期,
    ""协议有效期至"" = @协议有效期至,
    ""合作状态"" = @合作状态,
    ""备注"" = @备注
WHERE ""编号"" = @编号;", model);
                return new { 编号 = model.编号.Value, 成功 = true };
            }
            else
            {
                var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""医共体合作单位"" (
    ""单位名称"", ""单位类型"", ""行政区划"", ""床位数"", ""卫技人员数"",
    ""对接联系人"", ""联系电话"", ""合作协议签署日期"", ""协议有效期至"", ""合作状态"", ""备注""
) VALUES (
    @单位名称, @单位类型, @行政区划, @床位数, @卫技人员数,
    @对接联系人, @联系电话, @合作协议签署日期, @协议有效期至, @合作状态, @备注
) RETURNING ""编号"";", model);
                return new { 编号 = id, 成功 = true };
            }
        }

        [HttpGet]
        public async Task<object> 查询进修与帮扶记录(Paging paging, string 业务类型 = null, long? 合作单位编号 = null, string 关键词 = null)
        {
            var pIndex = Math.Max(1, paging?.Index ?? 1);
            var pSize = Math.Max(1, paging?.Size ?? 20);
            var offset = (pIndex - 1) * pSize;

            var where = "WHERE 1=1";
            if (!string.IsNullOrWhiteSpace(业务类型)) where += " AND r.\"业务类型\" = @业务类型";
            if (合作单位编号.HasValue) where += " AND r.\"合作单位编号\" = @合作单位编号";
            if (!string.IsNullOrWhiteSpace(关键词)) where += " AND (r.\"人员姓名\" ILIKE '%' || @关键词 || '%' OR u.\"单位名称\" ILIKE '%' || @关键词 || '%')";

            var total = await Db.QuerySingleAsync<int>($@"
SELECT count(*)::integer
FROM ""医共体人员进修与帮扶记录"" r
LEFT JOIN ""医共体合作单位"" u ON u.""编号"" = r.""合作单位编号""
{where};", new { 业务类型, 合作单位编号, 关键词 });

            var list = await Db.QueryAsync<object>($@"
SELECT 
    r.*,
    u.""单位名称"" AS ""合作单位名称"",
    p.""姓名"" AS ""带教导师姓名""
FROM ""医共体人员进修与帮扶记录"" r
LEFT JOIN ""医共体合作单位"" u ON u.""编号"" = r.""合作单位编号""
LEFT JOIN ""人员"" p ON p.""编号"" = r.""带教导师编号""
{where}
ORDER BY r.""开始日期"" DESC, r.""编号"" DESC
LIMIT @pSize OFFSET @offset;", new { 业务类型, 合作单位编号, 关键词, pSize, offset });

            return new { total, list };
        }

        [HttpPost]
        public async Task<object> 登记进修与帮扶记录([FromBody] 医共体人员进修帮扶登记 model)
        {
            if (model == null) throw new ArgumentException("缺少参数");
            if (string.IsNullOrWhiteSpace(model.人员姓名)) throw new ArgumentException("人员姓名不能为空");
            if (model.合作单位编号 <= 0) throw new ArgumentException("请选择合作单位");
            if (model.开始日期 > model.结束日期) throw new ArgumentException("开始日期不能晚于结束日期");

            var mentorId = model.带教导师编号 ?? CurrentUser.编号;

            if (model.编号.HasValue && model.编号.Value > 0)
            {
                await Db.ExecuteAsync(@"
UPDATE ""医共体人员进修与帮扶记录"" SET
    ""业务类型"" = @业务类型,
    ""合作单位编号"" = @合作单位编号,
    ""人员姓名"" = @人员姓名,
    ""人员职称"" = @人员职称,
    ""专业学科"" = @专业学科,
    ""开始日期"" = @开始日期,
    ""结束日期"" = @结束日期,
    ""带教导师编号"" = @带教导师编号,
    ""考核鉴定结果"" = @考核鉴定结果,
    ""主要帮扶与学习成效"" = @主要帮扶与学习成效,
    ""附件路径"" = @附件路径
WHERE ""编号"" = @编号;", new
                {
                    model.业务类型,
                    model.合作单位编号,
                    model.人员姓名,
                    model.人员职称,
                    model.专业学科,
                    model.开始日期,
                    model.结束日期,
                    带教导师编号 = mentorId,
                    model.考核鉴定结果,
                    model.主要帮扶与学习成效,
                    model.附件路径,
                    编号 = model.编号.Value
                });
                return new { 编号 = model.编号.Value, 成功 = true };
            }
            else
            {
                var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""医共体人员进修与帮扶记录"" (
    ""业务类型"", ""合作单位编号"", ""人员姓名"", ""人员职称"", ""专业学科"",
    ""开始日期"", ""结束日期"", ""带教导师编号"", ""考核鉴定结果"", ""主要帮扶与学习成效"", ""附件路径""
) VALUES (
    @业务类型, @合作单位编号, @人员姓名, @人员职称, @专业学科,
    @开始日期, @结束日期, @带教导师编号, @考核鉴定结果, @主要帮扶与学习成效, @附件路径
) RETURNING ""编号"";", new
                {
                    model.业务类型,
                    model.合作单位编号,
                    model.人员姓名,
                    model.人员职称,
                    model.专业学科,
                    model.开始日期,
                    model.结束日期,
                    带教导师编号 = mentorId,
                    model.考核鉴定结果,
                    model.主要帮扶与学习成效,
                    model.附件路径
                });
                return new { 编号 = id, 成功 = true };
            }
        }
    }
}
