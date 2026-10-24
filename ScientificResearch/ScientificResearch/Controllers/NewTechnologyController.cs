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
    /// 医疗新技术新项目（三新）全生命周期准入、临床应用随访、期满转常规评奖及医共体适宜技术推广
    /// </summary>
    public class NewTechnologyController : BaseController
    {
        [HttpPost]
        public async Task<object> 申报新技术([FromBody] 医疗新技术申报增改 model)
        {
            if (model == null) throw new ArgumentException("缺少新技术申报资料");
            if (string.IsNullOrWhiteSpace(model.项目名称)) throw new ArgumentException("新技术项目名称不能为空");
            if (model.计划开始日期 > model.计划完成日期) throw new ArgumentException("计划开始日期不能晚于完成日期");

            var applicantId = model.负责人编号 ?? CurrentUser.编号;
            var applicantDeptId = model.科室编号 ?? CurrentUser.部门编号;
            var year = model.申报年度 ?? DateTime.Today.Year;
            var status = model.提交审批 ? "待审查" : "草稿";

            if (model.编号.HasValue && model.编号.Value > 0)
            {
                await Db.ExecuteAsync(@"
UPDATE ""医疗新技术新项目"" SET
    ""项目名称"" = @项目名称,
    ""申报类别"" = @申报类别,
    ""技术难度等级"" = @技术难度等级,
    ""风险等级"" = @风险等级,
    ""负责人编号"" = @负责人编号,
    ""科室编号"" = @科室编号,
    ""团队成员"" = CAST(@团队成员 AS jsonb),
    ""申报年度"" = @申报年度,
    ""计划开始日期"" = @计划开始日期,
    ""计划完成日期"" = @计划完成日期,
    ""预期应用例数"" = @预期应用例数,
    ""技术适应症"" = @技术适应症,
    ""禁忌症及防范预案"" = @禁忌症及防范预案,
    ""附件路径"" = @附件路径,
    ""准入审批状态"" = CASE WHEN @提交审批 THEN '待审查' ELSE ""准入审批状态"" END,
    ""更新时间"" = CURRENT_TIMESTAMP
WHERE ""编号"" = @编号;", new
                {
                    model.项目名称,
                    model.申报类别,
                    model.技术难度等级,
                    model.风险等级,
                    负责人编号 = applicantId,
                    科室编号 = applicantDeptId,
                    团队成员 = string.IsNullOrWhiteSpace(model.团队成员) ? "[]" : model.团队成员,
                    申报年度 = year,
                    model.计划开始日期,
                    model.计划完成日期,
                    model.预期应用例数,
                    model.技术适应症,
                    model.禁忌症及防范预案,
                    model.附件路径,
                    model.提交审批,
                    编号 = model.编号.Value
                });
                return new { 编号 = model.编号.Value, 状态 = status };
            }
            else
            {
                var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""医疗新技术新项目"" (
    ""项目名称"", ""申报类别"", ""技术难度等级"", ""风险等级"", ""负责人编号"", ""科室编号"",
    ""团队成员"", ""申报年度"", ""计划开始日期"", ""计划完成日期"", ""预期应用例数"",
    ""技术适应症"", ""禁忌症及防范预案"", ""附件路径"", ""准入审批状态""
) VALUES (
    @项目名称, @申报类别, @技术难度等级, @风险等级, @负责人编号, @科室编号,
    CAST(@团队成员 AS jsonb), @申报年度, @计划开始日期, @计划完成日期, @预期应用例数,
    @技术适应症, @禁忌症及防范预案, @附件路径, @准入审批状态
) RETURNING ""编号"";", new
                {
                    model.项目名称,
                    model.申报类别,
                    model.技术难度等级,
                    model.风险等级,
                    负责人编号 = applicantId,
                    科室编号 = applicantDeptId,
                    团队成员 = string.IsNullOrWhiteSpace(model.团队成员) ? "[]" : model.团队成员,
                    申报年度 = year,
                    model.计划开始日期,
                    model.计划完成日期,
                    model.预期应用例数,
                    model.技术适应症,
                    model.禁忌症及防范预案,
                    model.附件路径,
                    准入审批状态 = status
                });
                return new { 编号 = id, 状态 = status };
            }
        }

        [HttpGet]
        public async Task<object> 分页获取新技术列表(Paging paging, int? 申报年度 = null, string 申报类别 = null, string 准入审批状态 = null, string 关键词 = null)
        {
            var pIndex = Math.Max(1, paging?.Index ?? 1);
            var pSize = Math.Max(1, paging?.Size ?? 20);
            var offset = (pIndex - 1) * pSize;

            var where = "WHERE 1=1";
            if (申报年度.HasValue) where += " AND t.\"申报年度\" = @申报年度";
            if (!string.IsNullOrWhiteSpace(申报类别)) where += " AND t.\"申报类别\" = @申报类别";
            if (!string.IsNullOrWhiteSpace(准入审批状态)) where += " AND t.\"准入审批状态\" = @准入审批状态";
            if (!string.IsNullOrWhiteSpace(关键词)) where += " AND (t.\"项目名称\" ILIKE '%' || @关键词 || '%' OR p.\"姓名\" ILIKE '%' || @关键词 || '%')";

            var total = await Db.QuerySingleAsync<int>($@"
SELECT count(*)::integer
FROM ""医疗新技术新项目"" t
LEFT JOIN ""人员"" p ON p.""编号"" = t.""负责人编号""
{where};", new { 申报年度, 申报类别, 准入审批状态, 关键词 });

            var list = await Db.QueryAsync<object>($@"
SELECT 
    t.*,
    p.""姓名"" AS ""负责人姓名"",
    d.""名称"" AS ""科室名称"",
    COALESCE((SELECT count(*)::integer FROM ""医疗新技术临床应用随访"" s WHERE s.""新技术编号"" = t.""编号""), 0) AS ""实际应用例数"",
    COALESCE((SELECT count(*)::integer FROM ""医疗新技术临床应用随访"" s WHERE s.""新技术编号"" = t.""编号"" AND s.""是否发生不良反应"" = TRUE), 0) AS ""不良反应例数""
FROM ""医疗新技术新项目"" t
LEFT JOIN ""人员"" p ON p.""编号"" = t.""负责人编号""
LEFT JOIN ""部门"" d ON d.""编号"" = t.""科室编号""
{where}
ORDER BY t.""编号"" DESC
LIMIT @pSize OFFSET @offset;", new { 申报年度, 申报类别, 准入审批状态, 关键词, pSize, offset });

            return new { total, list };
        }

        [HttpGet]
        public async Task<object> 获取新技术详情(long 编号)
        {
            var tech = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT 
    t.*,
    p.""姓名"" AS ""负责人姓名"",
    d.""名称"" AS ""科室名称"",
    COALESCE(u_doc.""姓名"", '') AS ""医务处审核人姓名"",
    COALESCE(u_edu.""姓名"", '') AS ""科教处审核人姓名"",
    COALESCE((SELECT count(*)::integer FROM ""医疗新技术临床应用随访"" s WHERE s.""新技术编号"" = t.""编号""), 0) AS ""实际应用例数"",
    COALESCE((SELECT count(*)::integer FROM ""医疗新技术临床应用随访"" s WHERE s.""新技术编号"" = t.""编号"" AND s.""是否发生不良反应"" = TRUE), 0) AS ""不良反应例数""
FROM ""医疗新技术新项目"" t
LEFT JOIN ""人员"" p ON p.""编号"" = t.""负责人编号""
LEFT JOIN ""部门"" d ON d.""编号"" = t.""科室编号""
LEFT JOIN ""人员"" u_doc ON u_doc.""编号"" = t.""医务处审核人编号""
LEFT JOIN ""人员"" u_edu ON u_edu.""编号"" = t.""科教处审核人编号""
WHERE t.""编号"" = @编号;", new { 编号 });

            if (tech == null) return NotFound(new { error = "新技术项目不存在" });

            var followups = await Db.QueryAsync<object>(@"
SELECT s.*, p.""姓名"" AS ""记录人姓名""
FROM ""医疗新技术临床应用随访"" s
LEFT JOIN ""人员"" p ON p.""编号"" = s.""记录人编号""
WHERE s.""新技术编号"" = @编号
ORDER BY s.""应用日期"" DESC, s.""编号"" DESC;", new { 编号 });

            var transitions = await Db.QueryAsync<object>(@"
SELECT r.*, p.""姓名"" AS ""审批人姓名""
FROM ""医疗新技术转常规与评奖"" r
LEFT JOIN ""人员"" p ON p.""编号"" = r.""审批人编号""
WHERE r.""新技术编号"" = @编号
ORDER BY r.""编号"" DESC;", new { 编号 });

            return new
            {
                项目 = tech,
                随访记录 = followups,
                转常规及评奖记录 = transitions
            };
        }

        [HttpPost]
        public async Task<object> 准入审查([FromBody] 医疗新技术准入审查提交 model)
        {
            if (model == null || model.编号 <= 0) throw new ArgumentException("参数无效");
            var isManager = await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办", "医务处", "医务科");
            if (!isManager) return StatusCode(403, new { error = "无权执行准入审查" });

            var current = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"", ""准入审批状态"", ""医务处审核状态"", ""科教处审核状态""
FROM ""医疗新技术新项目"" WHERE ""编号"" = @编号;", new { model.编号 });

            if (current == null) return NotFound(new { error = "新技术项目不存在" });

            var dept = (model.审核部门 ?? "").Trim();
            var status = model.通过 ? "通过" : "驳回";

            if (dept == "医务处" || dept.Contains("医务"))
            {
                await Db.ExecuteAsync(@"
UPDATE ""医疗新技术新项目"" SET
    ""医务处审核状态"" = @status,
    ""医务处意见"" = @意见,
    ""医务处审核人编号"" = @userId,
    ""医务处审核时间"" = CURRENT_TIMESTAMP,
    ""更新时间"" = CURRENT_TIMESTAMP
WHERE ""编号"" = @编号;", new { status, 意见 = model.意见, userId = CurrentUser.编号, model.编号 });
            }
            else
            {
                await Db.ExecuteAsync(@"
UPDATE ""医疗新技术新项目"" SET
    ""科教处审核状态"" = @status,
    ""科教处意见"" = @意见,
    ""科教处审核人编号"" = @userId,
    ""科教处审核时间"" = CURRENT_TIMESTAMP,
    ""更新时间"" = CURRENT_TIMESTAMP
WHERE ""编号"" = @编号;", new { status, 意见 = model.意见, userId = CurrentUser.编号, model.编号 });
            }

            if (!string.IsNullOrWhiteSpace(model.伦理审查编号))
            {
                await Db.ExecuteAsync(@"
UPDATE ""医疗新技术新项目"" SET
    ""伦理审查编号"" = @伦理审查编号,
    ""伦理审查状态"" = COALESCE(@伦理审查状态, '已通过')
WHERE ""编号"" = @编号;", new { model.伦理审查编号, model.伦理审查状态, model.编号 });
            }

            // 检查医务与科教联合状态
            var updated = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""医务处审核状态"", ""科教处审核状态"" FROM ""医疗新技术新项目"" WHERE ""编号"" = @编号;", new { model.编号 });

            string finalStatus;
            if (updated.医务处审核状态 == "驳回" || updated.科教处审核状态 == "驳回")
            {
                finalStatus = "已驳回";
            }
            else if (updated.医务处审核状态 == "通过" && updated.科教处审核状态 == "通过")
            {
                finalStatus = "已准入应用";
            }
            else
            {
                finalStatus = "待审查";
            }

            await Db.ExecuteAsync(@"
UPDATE ""医疗新技术新项目"" SET ""准入审批状态"" = @finalStatus WHERE ""编号"" = @编号;", new { finalStatus, model.编号 });

            return new { 编号 = model.编号, 准入审批状态 = finalStatus };
        }

        [HttpPost]
        public async Task<object> 登记应用例数与随访([FromBody] 医疗新技术随访登记 model)
        {
            if (model == null || model.新技术编号 <= 0) throw new ArgumentException("参数缺失");
            if (string.IsNullOrWhiteSpace(model.患者姓名脱敏) || string.IsNullOrWhiteSpace(model.病历号))
                throw new ArgumentException("患者脱敏标识和病历号不能为空");

            var tech = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"", ""准入审批状态"", ""预期应用例数""
FROM ""医疗新技术新项目"" WHERE ""编号"" = @新技术编号;", new { model.新技术编号 });

            if (tech == null) return NotFound(new { error = "新技术项目不存在" });
            if (tech.准入审批状态 != "已准入应用" && tech.准入审批状态 != "已转常规")
                throw new ArgumentException("新技术尚未获得准入审批通过，暂不能登记临床应用例数");

            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""医疗新技术临床应用随访"" (
    ""新技术编号"", ""患者姓名脱敏"", ""病历号"", ""应用日期"", ""主要术者编号"", ""主要术者姓名"",
    ""临床疗效"", ""是否发生不良反应"", ""不良反应分级"", ""不良反应描述及处置"", ""随访日期"", ""记录人编号""
) VALUES (
    @新技术编号, @患者姓名脱敏, @病历号, @应用日期, @主要术者编号, @主要术者姓名,
    @临床疗效, @是否发生不良反应, @不良反应分级, @不良反应描述及处置, @随访日期, @记录人编号
) RETURNING ""编号"";", new
            {
                model.新技术编号,
                model.患者姓名脱敏,
                model.病历号,
                model.应用日期,
                主要术者编号 = model.主要术者编号 ?? CurrentUser.编号,
                主要术者姓名 = string.IsNullOrWhiteSpace(model.主要术者姓名) ? CurrentUser.姓名 : model.主要术者姓名,
                model.临床疗效,
                model.是否发生不良反应,
                不良反应分级 = model.是否发生不良反应 ? (model.不良反应分级 ?? "轻度") : "无",
                model.不良反应描述及处置,
                随访日期 = model.随访日期 ?? DateTime.Today,
                记录人编号 = CurrentUser.编号
            });

            var count = await Db.QuerySingleAsync<int>(@"
SELECT count(*)::integer FROM ""医疗新技术临床应用随访"" WHERE ""新技术编号"" = @新技术编号;", new { model.新技术编号 });

            var reachesTarget = count >= Convert.ToInt32(tech.预期应用例数);

            return new
            {
                随访记录编号 = id,
                累计完成例数 = count,
                预期例数 = Convert.ToInt32(tech.预期应用例数),
                达到预期例数 = reachesTarget,
                提示 = reachesTarget ? $"已达到或超过预期目标（{count}/{Convert.ToInt32(tech.预期应用例数)}例），符合申报转常规或院内评奖条件！" : $"登记成功（当前进度 {count}/{Convert.ToInt32(tech.预期应用例数)}例）"
            };
        }

        [HttpPost]
        public async Task<object> 申请转常规与评奖([FromBody] 医疗新技术转常规评奖申请 model)
        {
            if (model == null || model.新技术编号 <= 0) throw new ArgumentException("参数缺失");

            var tech = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"", ""准入审批状态"" FROM ""医疗新技术新项目"" WHERE ""编号"" = @新技术编号;", new { model.新技术编号 });

            if (tech == null) return NotFound(new { error = "新技术项目不存在" });

            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""医疗新技术转常规与评奖"" (
    ""新技术编号"", ""申请类型"", ""拟申报奖项"", ""总结报告路径"", ""累计开展例数"",
    ""显效率"", ""并发症发生率"", ""学术产出说明"", ""审批状态""
) VALUES (
    @新技术编号, @申请类型, @拟申报奖项, @总结报告路径, @累计开展例数,
    @显效率, @并发症发生率, @学术产出说明, '待审批'
) RETURNING ""编号"";", model);

            return new { 申请编号 = id, 状态 = "待审批" };
        }

        [HttpPost]
        public async Task<object> 审批转常规与评奖([FromBody] 医疗新技术转常规审批 model)
        {
            if (model == null || model.编号 <= 0) throw new ArgumentException("参数缺失");
            var isManager = await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办", "医务处", "学术委员会");
            if (!isManager) return StatusCode(403, new { error = "无权审批转常规或评奖" });

            var row = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"", ""新技术编号"", ""申请类型"" FROM ""医疗新技术转常规与评奖"" WHERE ""编号"" = @编号;", new { model.编号 });

            if (row == null) return NotFound(new { error = "申请记录不存在" });

            var status = model.通过 ? "已批准" : "已驳回";
            await Db.ExecuteAsync(@"
UPDATE ""医疗新技术转常规与评奖"" SET
    ""审批状态"" = @status,
    ""审批意见"" = @审批意见,
    ""审批人编号"" = @userId,
    ""审批时间"" = CURRENT_TIMESTAMP
WHERE ""编号"" = @编号;", new { status, model.审批意见, userId = CurrentUser.编号, model.编号 });

            if (model.通过)
            {
                var newStatus = (string)row.申请类型 == "期满转常规" ? "已转常规" : "已评奖";
                await Db.ExecuteAsync(@"
UPDATE ""医疗新技术新项目"" SET ""准入审批状态"" = @newStatus, ""更新时间"" = CURRENT_TIMESTAMP WHERE ""编号"" = @techId;",
                    new { newStatus, techId = (long)row.新技术编号 });
            }

            return new { 编号 = model.编号, 状态 = status };
        }

        // --- 医共体适宜技术推广 ---

        [HttpPost]
        public async Task<object> 登记适宜技术推广([FromBody] 医共体适宜技术推广登记 model)
        {
            if (model == null) throw new ArgumentException("缺少参数");
            if (string.IsNullOrWhiteSpace(model.技术名称)) throw new ArgumentException("技术名称不能为空");
            if (string.IsNullOrWhiteSpace(model.受援基层单位)) throw new ArgumentException("受援基层单位不能为空");

            var expertId = model.牵头专家编号 ?? CurrentUser.编号;
            var deptId = model.牵头科室编号 ?? CurrentUser.部门编号;

            if (model.编号.HasValue && model.编号.Value > 0)
            {
                await Db.ExecuteAsync(@"
UPDATE ""医共体适宜技术推广"" SET
    ""技术名称"" = @技术名称,
    ""牵头科室编号"" = @牵头科室编号,
    ""牵头专家编号"" = @牵头专家编号,
    ""受援基层单位"" = @受援基层单位,
    ""推广形式"" = @推广形式,
    ""推广日期"" = @推广日期,
    ""培训场次"" = @培训场次,
    ""受益医务人员数"" = @受益医务人员数,
    ""受益患者人次"" = @受益患者人次,
    ""讲义及签到材料路径"" = @讲义及签到材料路径,
    ""推广成效说明"" = @推广成效说明
WHERE ""编号"" = @编号;", new
                {
                    model.技术名称,
                    牵头科室编号 = deptId,
                    牵头专家编号 = expertId,
                    model.受援基层单位,
                    model.推广形式,
                    model.推广日期,
                    model.培训场次,
                    model.受益医务人员数,
                    model.受益患者人次,
                    model.讲义及签到材料路径,
                    model.推广成效说明,
                    编号 = model.编号.Value
                });
                return new { 编号 = model.编号.Value, 成功 = true };
            }
            else
            {
                var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""医共体适宜技术推广"" (
    ""技术名称"", ""牵头科室编号"", ""牵头专家编号"", ""受援基层单位"", ""推广形式"",
    ""推广日期"", ""培训场次"", ""受益医务人员数"", ""受益患者人次"", ""讲义及签到材料路径"",
    ""推广成效说明"", ""记录人编号""
) VALUES (
    @技术名称, @牵头科室编号, @牵头专家编号, @受援基层单位, @推广形式,
    @推广日期, @培训场次, @受益医务人员数, @受益患者人次, @讲义及签到材料路径,
    @推广成效说明, @记录人编号
) RETURNING ""编号"";", new
                {
                    model.技术名称,
                    牵头科室编号 = deptId,
                    牵头专家编号 = expertId,
                    model.受援基层单位,
                    model.推广形式,
                    model.推广日期,
                    model.培训场次,
                    model.受益医务人员数,
                    model.受益患者人次,
                    model.讲义及签到材料路径,
                    model.推广成效说明,
                    记录人编号 = CurrentUser.编号
                });
                return new { 编号 = id, 成功 = true };
            }
        }

        [HttpGet]
        public async Task<object> 分页查询适宜技术推广(Paging paging, string 关键词 = null, string 推广形式 = null, string 受援基层单位 = null)
        {
            var pIndex = Math.Max(1, paging?.Index ?? 1);
            var pSize = Math.Max(1, paging?.Size ?? 20);
            var offset = (pIndex - 1) * pSize;

            var where = "WHERE 1=1";
            if (!string.IsNullOrWhiteSpace(关键词)) where += " AND (t.\"技术名称\" ILIKE '%' || @关键词 || '%' OR p.\"姓名\" ILIKE '%' || @关键词 || '%')";
            if (!string.IsNullOrWhiteSpace(推广形式)) where += " AND t.\"推广形式\" = @推广形式";
            if (!string.IsNullOrWhiteSpace(受援基层单位)) where += " AND t.\"受援基层单位\" ILIKE '%' || @受援基层单位 || '%'";

            var total = await Db.QuerySingleAsync<int>($@"
SELECT count(*)::integer
FROM ""医共体适宜技术推广"" t
LEFT JOIN ""人员"" p ON p.""编号"" = t.""牵头专家编号""
{where};", new { 关键词, 推广形式, 受援基层单位 });

            var list = await Db.QueryAsync<object>($@"
SELECT 
    t.*,
    p.""姓名"" AS ""牵头专家姓名"",
    d.""名称"" AS ""科室名称"",
    u.""姓名"" AS ""登记人姓名""
FROM ""医共体适宜技术推广"" t
LEFT JOIN ""人员"" p ON p.""编号"" = t.""牵头专家编号""
LEFT JOIN ""部门"" d ON d.""编号"" = t.""牵头科室编号""
LEFT JOIN ""人员"" u ON u.""编号"" = t.""记录人编号""
{where}
ORDER BY t.""推广日期"" DESC, t.""编号"" DESC
LIMIT @pSize OFFSET @offset;", new { 关键词, 推广形式, 受援基层单位, pSize, offset });

            return new { total, list };
        }

        [HttpGet]
        public async Task<object> 统计适宜技术推广大盘()
        {
            var stat = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT 
    count(*)::integer AS ""总推广记录数"",
    COALESCE(sum(""培训场次""), 0)::integer AS ""总培训场次"",
    COALESCE(sum(""受益医务人员数""), 0)::integer AS ""总受益基层医务人员"",
    COALESCE(sum(""受益患者人次""), 0)::integer AS ""总受益患者人次"",
    count(DISTINCT ""受援基层单位"")::integer AS ""覆盖基层单位数""
FROM ""医共体适宜技术推广"";");

            var byUnit = await Db.QueryAsync<object>(@"
SELECT 
    ""受援基层单位"",
    count(*)::integer AS ""活动次数"",
    sum(""培训场次"")::integer AS ""场次"",
    sum(""受益医务人员数"")::integer AS ""受益医护人数""
FROM ""医共体适宜技术推广""
GROUP BY ""受援基层单位""
ORDER BY ""受益医护人数"" DESC
LIMIT 10;");

            return new
            {
                大盘概况 = stat,
                基层单位排行 = byUnit
            };
        }
    }
}
