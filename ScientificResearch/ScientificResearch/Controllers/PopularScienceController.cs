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
    /// 医学科普作品、临床典型案例库、护理小发明与实用新型转化管理
    /// </summary>
    public class PopularScienceController : BaseController
    {
        [HttpPost]
        public async Task<object> 保存科普案例成果([FromBody] 科普成果与案例登记 model)
        {
            if (model == null) throw new ArgumentException("缺少成果登记资料");
            if (string.IsNullOrWhiteSpace(model.成果名称)) throw new ArgumentException("成果名称不能为空");
            if (string.IsNullOrWhiteSpace(model.分类)) throw new ArgumentException("分类不能为空");

            var authorId = model.第一作者编号 ?? CurrentUser.编号;
            var authorName = string.IsNullOrWhiteSpace(model.第一作者姓名) ? CurrentUser.姓名 : model.第一作者姓名;
            var deptId = model.所属科室编号 ?? CurrentUser.部门编号;

            if (model.编号.HasValue && model.编号.Value > 0)
            {
                await Db.ExecuteAsync(@"
UPDATE ""科普成果与临床案例库"" SET
    ""分类"" = @分类,
    ""形式类型"" = @形式类型,
    ""成果名称"" = @成果名称,
    ""第一作者编号"" = @第一作者编号,
    ""第一作者姓名"" = @第一作者姓名,
    ""所属科室编号"" = @所属科室编号,
    ""合作作者"" = CAST(@合作作者 AS jsonb),
    ""完成日期"" = @完成日期,
    ""发布载体与平台"" = @发布载体与平台,
    ""受众传播量与成效"" = @受众传播量与成效,
    ""成果摘要"" = @成果摘要,
    ""附件路径"" = @附件路径
WHERE ""编号"" = @编号;", new
                {
                    model.分类,
                    model.形式类型,
                    model.成果名称,
                    第一作者编号 = authorId,
                    第一作者姓名 = authorName,
                    所属科室编号 = deptId,
                    合作作者 = string.IsNullOrWhiteSpace(model.合作作者) ? "[]" : model.合作作者,
                    model.完成日期,
                    model.发布载体与平台,
                    model.受众传播量与成效,
                    model.成果摘要,
                    model.附件路径,
                    编号 = model.编号.Value
                });
                return new { 编号 = model.编号.Value, 状态 = "已更新" };
            }
            else
            {
                var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科普成果与临床案例库"" (
    ""分类"", ""形式类型"", ""成果名称"", ""第一作者编号"", ""第一作者姓名"", ""所属科室编号"",
    ""合作作者"", ""完成日期"", ""发布载体与平台"", ""受众传播量与成效"", ""成果摘要"", ""附件路径"", ""审核状态""
) VALUES (
    @分类, @形式类型, @成果名称, @第一作者编号, @第一作者姓名, @所属科室编号,
    CAST(@合作作者 AS jsonb), @完成日期, @发布载体与平台, @受众传播量与成效, @成果摘要, @附件路径, '已入库'
) RETURNING ""编号"";", new
                {
                    model.分类,
                    model.形式类型,
                    model.成果名称,
                    第一作者编号 = authorId,
                    第一作者姓名 = authorName,
                    所属科室编号 = deptId,
                    合作作者 = string.IsNullOrWhiteSpace(model.合作作者) ? "[]" : model.合作作者,
                    model.完成日期,
                    model.发布载体与平台,
                    model.受众传播量与成效,
                    model.成果摘要,
                    model.附件路径
                });
                return new { 编号 = id, 状态 = "已入库" };
            }
        }

        [HttpGet]
        public async Task<object> 分页获取科普案例成果(Paging paging, string 分类 = null, string 形式类型 = null, int? 年度 = null, string 关键词 = null)
        {
            var pIndex = Math.Max(1, paging?.Index ?? 1);
            var pSize = Math.Max(1, paging?.Size ?? 20);
            var offset = (pIndex - 1) * pSize;

            var where = "WHERE 1=1";
            if (!string.IsNullOrWhiteSpace(分类)) where += " AND c.\"分类\" = @分类";
            if (!string.IsNullOrWhiteSpace(形式类型)) where += " AND c.\"形式类型\" = @形式类型";
            if (年度.HasValue) where += " AND EXTRACT(YEAR FROM c.\"完成日期\") = @年度";
            if (!string.IsNullOrWhiteSpace(关键词)) where += " AND (c.\"成果名称\" ILIKE '%' || @关键词 || '%' OR c.\"第一作者姓名\" ILIKE '%' || @关键词 || '%')";

            var total = await Db.QuerySingleAsync<int>($@"
SELECT count(*)::integer
FROM ""科普成果与临床案例库"" c
LEFT JOIN ""部门"" d ON d.""编号"" = c.""所属科室编号""
{where};", new { 分类, 形式类型, 年度, 关键词 });

            var list = await Db.QueryAsync<object>($@"
SELECT 
    c.*,
    COALESCE(d.""名称"", '未分配科室') AS ""科室名称""
FROM ""科普成果与临床案例库"" c
LEFT JOIN ""部门"" d ON d.""编号"" = c.""所属科室编号""
{where}
ORDER BY c.""完成日期"" DESC, c.""编号"" DESC
LIMIT @pSize OFFSET @offset;", new { 分类, 形式类型, 年度, 关键词, pSize, offset });

            return new { total, list };
        }

        [HttpPost]
        public async Task<object> 审核科普成果([FromBody] 科普成果审核 model)
        {
            if (model == null || model.编号 <= 0) throw new ArgumentException("参数缺失");
            var isManager = await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办");
            if (!isManager) return StatusCode(403, new { error = "无权审核成果" });

            var status = model.通过 ? "已入库" : "已退回";
            await Db.ExecuteAsync(@"
UPDATE ""科普成果与临床案例库"" SET
    ""审核状态"" = @status,
    ""审核意见"" = @审核意见,
    ""审核人编号"" = @userId
WHERE ""编号"" = @编号;", new { status, model.审核意见, userId = CurrentUser.编号, model.编号 });

            return new { 编号 = model.编号, 状态 = status };
        }

        [HttpGet]
        public async Task<object> 统计成果成效()
        {
            var stat = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT 
    count(*)::integer AS ""总成果数"",
    count(CASE WHEN ""分类"" = '医学科普作品' THEN 1 END)::integer AS ""科普作品数"",
    count(CASE WHEN ""分类"" = '临床典型案例' THEN 1 END)::integer AS ""临床案例数"",
    count(CASE WHEN ""分类"" = '护理小发明' THEN 1 END)::integer AS ""护理发明转化数"",
    COALESCE(sum(""受众传播量与成效""), 0)::bigint AS ""总传播与受益人次""
FROM ""科普成果与临床案例库"";");

            var categories = await Db.QueryAsync<object>(@"
SELECT ""分类"", count(*)::integer AS ""数量"", COALESCE(sum(""受众传播量与成效""), 0)::bigint AS ""受众量""
FROM ""科普成果与临床案例库""
GROUP BY ""分类"";");

            return new { 概况 = stat, 分类汇总 = categories };
        }
    }
}
