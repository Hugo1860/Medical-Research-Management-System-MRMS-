using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using ScientificResearch.Infrastucture;

namespace ScientificResearch.Controllers
{
    public class CmeCreditApplyDto
    {
        public int 编号 { get; set; }
        public int 年度 { get; set; }
        public string 学分类别 { get; set; } = "II类";
        public decimal 学分 { get; set; }
        public string 项目名称 { get; set; }
        public string 项目编号 { get; set; }
        public string 授予单位 { get; set; }
        public DateTime 获得日期 { get; set; }
        public string 来源 { get; set; } = "个人申报";
        public string 证书附件路径 { get; set; }
    }

    public class CmeAuditDto
    {
        public List<int> 编号列表 { get; set; }
        public bool 通过 { get; set; }
        public string 审核意见 { get; set; }
    }

    public class LectureCreditConfigDto
    {
        public int 讲座编号 { get; set; }
        public string 学分类别 { get; set; } = "II类";
        public decimal 学分值 { get; set; } = 1.0m;
        public DateTime 签到开始时间 { get; set; }
        public DateTime 签到结束时间 { get; set; }
    }

    public class LectureSignInDto
    {
        public string 签到令牌 { get; set; }
    }

    public class AdminSignInDto
    {
        public int 讲座编号 { get; set; }
        public List<int> 人员编号列表 { get; set; }
    }

    public class SendCmeRemindDto
    {
        public int 年度 { get; set; }
        public List<int> 人员编号列表 { get; set; }
        public string 催办内容 { get; set; }
    }

    public class CmeStandardDto
    {
        public int 编号 { get; set; }
        public int 年度 { get; set; }
        public string 适用人群 { get; set; } = "卫生技术人员";
        public string 职称级别 { get; set; } = "不限";
        public decimal 年度总分要求 { get; set; } = 25.0m;
        public decimal I类最低分 { get; set; } = 10.0m;
        public decimal II类最低分 { get; set; } = 0.0m;
        public string 说明 { get; set; }
    }

    /// <summary>
    /// 继续医学教育（CME）学分管理与院内讲座授分
    /// </summary>
    public class CmeCreditController : BaseController
    {
        /// <summary>
        /// 查询我的学分与达标情况
        /// </summary>
        [HttpGet]
        public async Task<object> 获取我的学分(int? 年度 = null)
        {
            var year = 年度 ?? DateTime.UtcNow.Year;
            var records = (await Db.QueryAsync<dynamic>(
                @"SELECT * FROM ""继教学分记录""
                  WHERE ""人员编号"" = @userId AND ""年度"" = @year
                  ORDER BY ""获得日期"" DESC, ""编号"" DESC;",
                new { userId = CurrentUser.编号, year })).ToList();

            decimal approvedTotal = 0;
            decimal approvedClass1 = 0;
            decimal approvedClass2 = 0;
            decimal pendingTotal = 0;

            foreach (var r in records)
            {
                var score = (decimal)(r.学分 ?? 0);
                var status = (string)r.审核状态;
                var cat = (string)r.学分类别;

                if (status == "已认定")
                {
                    approvedTotal += score;
                    if (cat == "I类") approvedClass1 += score;
                    else approvedClass2 += score;
                }
                else if (status == "待审核")
                {
                    pendingTotal += score;
                }
            }

            // 查询适用标准
            var standard = await Db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT * FROM ""继教学分标准""
                  WHERE ""年度"" = @year AND ""是否启用"" = TRUE
                  ORDER BY CASE WHEN ""职称级别"" = '不限' THEN 2 ELSE 1 END LIMIT 1;",
                new { year });

            decimal reqTotal = standard != null ? (decimal)standard.年度总分要求 : 25.0m;
            decimal reqClass1 = standard != null ? (decimal)standard.I类最低分 : 10.0m;
            bool isQualified = approvedTotal >= reqTotal && approvedClass1 >= reqClass1;

            return new
            {
                年度 = year,
                已认定总分 = approvedTotal,
                已认定I类分 = approvedClass1,
                已认定II类分 = approvedClass2,
                待审核学分 = pendingTotal,
                要求总分 = reqTotal,
                要求I类分 = reqClass1,
                是否达标 = isQualified,
                总分差额 = isQualified ? 0 : Math.Max(0, reqTotal - approvedTotal),
                I类差额 = approvedClass1 >= reqClass1 ? 0 : reqClass1 - approvedClass1,
                明细列表 = records
            };
        }

        /// <summary>
        /// 个人申报学分
        /// </summary>
        [HttpPost]
        public async Task<object> 申报学分([FromBody] CmeCreditApplyDto model)
        {
            if (model == null) return BadRequest(new { error = "数据不能为空" });
            if (string.IsNullOrWhiteSpace(model.项目名称)) return BadRequest(new { error = "项目名称不能为空" });
            if (model.学分 <= 0 || model.学分 > 50) return BadRequest(new { error = "学分值必须在 0.1 到 50 分之间" });
            if (string.IsNullOrWhiteSpace(model.授予单位)) return BadRequest(new { error = "授予单位不能为空" });

            var year = model.年度 > 0 ? model.年度 : (model.获得日期 != default ? model.获得日期.Year : DateTime.UtcNow.Year);

            if (model.编号 == 0)
            {
                var id = await Db.QuerySingleAsync<int>(
                    @"INSERT INTO ""继教学分记录"" (
                        ""人员编号"", ""年度"", ""学分类别"", ""学分"", ""项目名称"", ""项目编号"",
                        ""授予单位"", ""获得日期"", ""来源"", ""证书附件路径"", ""审核状态"", ""创建时间"", ""更新时间""
                    ) VALUES (
                        @userId, @year, @学分类别, @学分, @项目名称, @项目编号,
                        @授予单位, @获得日期, '个人申报', @证书附件路径, '待审核', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                    ) RETURNING ""编号"";",
                    new
                    {
                        userId = CurrentUser.编号,
                        year,
                        学分类别 = model.学分类别 == "I类" ? "I类" : "II类",
                        model.学分,
                        model.项目名称,
                        model.项目编号,
                        model.授予单位,
                        获得日期 = model.获得日期 != default ? model.获得日期 : DateTime.UtcNow.Date,
                        model.证书附件路径
                    });

                return new { success = true, 编号 = id, message = "学分申报已提交，等待科教科审核" };
            }
            else
            {
                var existing = await Db.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT * FROM ""继教学分记录"" WHERE ""编号"" = @编号;", new { model.编号 });
                if (existing == null) return NotFound(new { error = "学分记录不存在" });
                if ((int)existing.人员编号 != CurrentUser.编号) return StatusCode(403, new { error = "无权修改他人的记录" });
                if ((string)existing.审核状态 != "待审核") return StatusCode(409, new { error = "已审核认定的记录不可直接修改" });

                await Db.ExecuteAsync(
                    @"UPDATE ""继教学分记录"" SET
                        ""年度"" = @year,
                        ""学分类别"" = @学分类别,
                        ""学分"" = @学分,
                        ""项目名称"" = @项目名称,
                        ""项目编号"" = @项目编号,
                        ""授予单位"" = @授予单位,
                        ""获得日期"" = @获得日期,
                        ""证书附件路径"" = @证书附件路径,
                        ""更新时间"" = CURRENT_TIMESTAMP
                      WHERE ""编号"" = @编号;",
                    new
                    {
                        year,
                        学分类别 = model.学分类别 == "I类" ? "I类" : "II类",
                        model.学分,
                        model.项目名称,
                        model.项目编号,
                        model.授予单位,
                        获得日期 = model.获得日期 != default ? model.获得日期 : DateTime.UtcNow.Date,
                        model.证书附件路径,
                        model.编号
                    });

                return new { success = true, message = "学分申报已修改" };
            }
        }

        /// <summary>
        /// 撤回申报记录
        /// </summary>
        [HttpPost]
        public async Task<object> 撤回申报(int id)
        {
            var item = await Db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT * FROM ""继教学分记录"" WHERE ""编号"" = @id;", new { id });
            if (item == null) return NotFound(new { error = "记录不存在" });
            if ((int)item.人员编号 != CurrentUser.编号 && !await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办"))
            {
                return StatusCode(403, new { error = "无权删除他人记录" });
            }
            if ((string)item.审核状态 != "待审核")
            {
                return StatusCode(409, new { error = "只有待审核记录方可撤回" });
            }

            await Db.ExecuteAsync(@"DELETE FROM ""继教学分记录"" WHERE ""编号"" = @id;", new { id });
            return new { success = true, message = "申报记录已撤回删除" };
        }

        /// <summary>
        /// 科教科审核申报学分（支持批量）
        /// </summary>
        [HttpPost]
        public async Task<object> 审核学分([FromBody] CmeAuditDto model)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办"))
            {
                return StatusCode(403, new { error = "无权进行学分审核" });
            }
            if (model?.编号列表 == null || !model.编号列表.Any())
            {
                return BadRequest(new { error = "未选择待审核记录" });
            }

            var nextStatus = model.通过 ? "已认定" : "已驳回";
            var count = await Db.ExecuteAsync(
                @"UPDATE ""继教学分记录"" SET
                    ""审核状态"" = @nextStatus,
                    ""审核人编号"" = @userId,
                    ""审核人姓名"" = @userName,
                    ""审核时间"" = CURRENT_TIMESTAMP,
                    ""审核意见"" = @opinion,
                    ""更新时间"" = CURRENT_TIMESTAMP
                  WHERE ""编号"" = ANY(@ids) AND ""审核状态"" = '待审核';",
                new
                {
                    nextStatus,
                    userId = CurrentUser.编号,
                    userName = CurrentUser.姓名,
                    opinion = model.审核意见 ?? (model.通过 ? "审核通过，予以认定" : "资料不全或未达认定标准"),
                    ids = model.编号列表
                });

            return new { success = true, count, message = $"已处理 {count} 条学分记录" };
        }

        /// <summary>
        /// 分页查询待审核学分列表
        /// </summary>
        [HttpGet]
        public async Task<object> 分页查询待审学分(int pageIndex = 1, int pageSize = 20, int? 年度 = null, string 关键词 = null)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办"))
            {
                return StatusCode(403, new { error = "无权查看审核列表" });
            }

            var offset = (pageIndex - 1) * pageSize;
            var sqlWhere = @"WHERE r.""审核状态"" = '待审核'";
            if (年度.HasValue) sqlWhere += @" AND r.""年度"" = @年度";
            if (!string.IsNullOrWhiteSpace(关键词))
            {
                sqlWhere += @" AND (r.""项目名称"" ILIKE @kw OR p.""姓名"" ILIKE @kw OR p.""工号"" ILIKE @kw)";
            }

            var total = await Db.QuerySingleAsync<int>($@"
                SELECT COUNT(1)
                FROM ""继教学分记录"" r
                JOIN ""人员"" p ON r.""人员编号"" = p.""编号""
                {sqlWhere};",
                new { 年度, kw = $"%{关键词}%" });

            var list = await Db.QueryAsync<dynamic>($@"
                SELECT r.*, p.""姓名"" AS ""申请人姓名"", p.""工号"" AS ""申请人工号"", d.""名称"" AS ""部门名称""
                FROM ""继教学分记录"" r
                JOIN ""人员"" p ON r.""人员编号"" = p.""编号""
                LEFT JOIN ""部门"" d ON p.""部门编号"" = d.""编号""
                {sqlWhere}
                ORDER BY r.""创建时间"" ASC
                OFFSET @offset LIMIT @pageSize;",
                new { 年度, kw = $"%{关键词}%", offset, pageSize });

            return new { total, list, pageIndex, pageSize };
        }

        /// <summary>
        /// 配置院内主办讲座学分与扫码签到
        /// </summary>
        [HttpPost]
        public async Task<object> 设置讲座学分([FromBody] LectureCreditConfigDto model)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办"))
            {
                return StatusCode(403, new { error = "无权设置讲座学分" });
            }
            if (model == null || model.讲座编号 <= 0) return BadRequest(new { error = "讲座编号无效" });
            if (model.学分值 <= 0 || model.学分值 > 20) return BadRequest(new { error = "学分值必须在0.1到20分之间" });
            if (model.签到结束时间 <= model.签到开始时间) return BadRequest(new { error = "签到结束时间必须晚于开始时间" });

            var token = Guid.NewGuid().ToString("N");
            await Db.ExecuteAsync(
                @"INSERT INTO ""讲座学分设置"" (
                    ""讲座编号"", ""学分类别"", ""学分值"", ""签到开始时间"", ""签到结束时间"", ""签到令牌"", ""是否已授分"", ""创建人编号"", ""创建时间""
                  ) VALUES (
                    @讲座编号, @学分类别, @学分值, @签到开始时间, @签到结束时间, @token, FALSE, @userId, CURRENT_TIMESTAMP
                  ) ON CONFLICT (""讲座编号"") DO UPDATE
                  SET ""学分类别"" = EXCLUDED.""学分类别"",
                      ""学分值"" = EXCLUDED.""学分值"",
                      ""签到开始时间"" = EXCLUDED.""签到开始时间"",
                      ""签到结束时间"" = EXCLUDED.""签到结束时间"",
                      ""签到令牌"" = EXCLUDED.""签到令牌"";",
                new
                {
                    model.讲座编号,
                    model.学分类别,
                    model.学分值,
                    model.签到开始时间,
                    model.签到结束时间,
                    token,
                    userId = CurrentUser.编号
                });

            return new { success = true, 签到令牌 = token, message = "讲座学分与签到窗口已设置" };
        }

        /// <summary>
        /// 获取讲座学分设置与签到状态
        /// </summary>
        [HttpGet]
        public async Task<object> 获取讲座学分设置(int 讲座编号)
        {
            var config = await Db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT c.*, l.""活动名称"", l.""讲座地点"", l.""开始时间"", l.""结束时间""
                  FROM ""讲座学分设置"" c
                  JOIN ""主办讲座"" l ON c.""讲座编号"" = l.""编号""
                  WHERE c.""讲座编号"" = @讲座编号;",
                new { 讲座编号 });

            if (config == null)
            {
                var lecture = await Db.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT * FROM ""主办讲座"" WHERE ""编号"" = @讲座编号;", new { 讲座编号 });
                if (lecture == null) return NotFound(new { error = "讲座不存在" });
                return new { 已设置 = false, 讲座信息 = lecture };
            }

            var signCount = await Db.QuerySingleAsync<int>(
                @"SELECT COUNT(1) FROM ""院内讲座签到"" WHERE ""讲座编号"" = @讲座编号;", new { 讲座编号 });

            return new { 已设置 = true, 配置 = config, 已签到人数 = signCount };
        }

        /// <summary>
        /// 讲座现场扫码或输入令牌签到（医护人员手机端/PC端）
        /// </summary>
        [HttpPost]
        public async Task<object> 讲座签到([FromBody] LectureSignInDto model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.签到令牌))
            {
                return BadRequest(new { error = "签到令牌不能为空" });
            }

            var token = model.签到令牌.Trim();
            var setting = await Db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT s.*, l.""活动名称""
                  FROM ""讲座学分设置"" s
                  JOIN ""主办讲座"" l ON s.""讲座编号"" = l.""编号""
                  WHERE s.""签到令牌"" = @token;",
                new { token });

            if (setting == null)
            {
                return NotFound(new { error = "无效的签到令牌，请确认讲座或刷新二维码" });
            }

            var now = DateTime.UtcNow;
            if (now < (DateTime)setting.签到开始时间)
            {
                return StatusCode(409, new { error = "签到尚未开启，请在讲座规定时间内签到" });
            }
            if (now > (DateTime)setting.签到结束时间)
            {
                return StatusCode(409, new { error = "签到已截止，感谢参与" });
            }

            var existing = await Db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT * FROM ""院内讲座签到"" WHERE ""讲座编号"" = @lid AND ""人员编号"" = @uid;",
                new { lid = (int)setting.讲座编号, uid = CurrentUser.编号 });

            if (existing != null)
            {
                return new { success = true, 已签到 = true, message = $"您已于 {existing.签到时间:yyyy-MM-dd HH:mm} 完成签到，无需重复提交！" };
            }

            await Db.ExecuteAsync(
                @"INSERT INTO ""院内讲座签到"" (""讲座编号"", ""人员编号"", ""签到时间"", ""签到方式"")
                  VALUES (@lid, @uid, CURRENT_TIMESTAMP, '扫码')
                  ON CONFLICT (""讲座编号"", ""人员编号"") DO NOTHING;",
                new { lid = (int)setting.讲座编号, uid = CurrentUser.编号 });

            return new
            {
                success = true,
                讲座名称 = (string)setting.活动名称,
                学分值 = (decimal)setting.学分值,
                学分类别 = (string)setting.学分类别,
                message = $"签到成功！讲座结束后科教科统一授分（{setting.学分类别} {setting.学分值}分）"
            };
        }

        /// <summary>
        /// 讲座一键授分（将签到人员转为已认定的学分记录）
        /// </summary>
        [HttpPost]
        public async Task<object> 讲座授分(int 讲座编号)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办"))
            {
                return StatusCode(403, new { error = "仅科教科管理员有权执行讲座授分" });
            }

            var setting = await Db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT s.*, l.""活动名称"", l.""结束时间""
                  FROM ""讲座学分设置"" s
                  JOIN ""主办讲座"" l ON s.""讲座编号"" = l.""编号""
                  WHERE s.""讲座编号"" = @讲座编号;",
                new { 讲座编号 });

            if (setting == null) return NotFound(new { error = "该讲座尚未配置学分设置" });

            var attendees = (await Db.QueryAsync<int>(
                @"SELECT ""人员编号"" FROM ""院内讲座签到"" WHERE ""讲座编号"" = @讲座编号;",
                new { 讲座编号 })).ToList();

            if (!attendees.Any())
            {
                return BadRequest(new { error = "该讲座暂无任何签到人员，无法授分" });
            }

            var year = ((DateTime)setting.结束时间).Year;
            var grantDate = ((DateTime)setting.结束时间).Date;
            var creditVal = (decimal)setting.学分值;
            var creditCat = (string)setting.学分类别;
            var lectureTitle = (string)setting.活动名称;

            int grantedCount = 0;
            foreach (var uid in attendees)
            {
                try
                {
                    var inserted = await Db.ExecuteAsync(
                        @"INSERT INTO ""继教学分记录"" (
                            ""人员编号"", ""年度"", ""学分类别"", ""学分"", ""项目名称"", ""授予单位"",
                            ""获得日期"", ""来源"", ""来源业务类型"", ""来源业务编号"", ""审核状态"",
                            ""审核人编号"", ""审核人姓名"", ""审核时间"", ""审核意见"", ""创建时间"", ""更新时间""
                        ) VALUES (
                            @uid, @year, @creditCat, @creditVal, @lectureTitle, '本院科教科',
                            @grantDate, '院内讲座签到', '主办讲座', @讲座编号, '已认定',
                            @adminId, @adminName, CURRENT_TIMESTAMP, '讲座签到自动授分', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                        ) ON CONFLICT (""来源业务类型"", ""来源业务编号"", ""人员编号"") DO NOTHING;",
                        new
                        {
                            uid,
                            year,
                            creditCat,
                            creditVal,
                            lectureTitle,
                            grantDate,
                            讲座编号,
                            adminId = CurrentUser.编号,
                            adminName = CurrentUser.姓名
                        });
                    if (inserted > 0) grantedCount++;
                }
                catch {}
            }

            await Db.ExecuteAsync(
                @"UPDATE ""讲座学分设置"" SET ""是否已授分"" = TRUE WHERE ""讲座编号"" = @讲座编号;",
                new { 讲座编号 });

            return new { success = true, 签到人数 = attendees.Count, 新增授分人数 = grantedCount, message = $"讲座授分完成，共为 {grantedCount} 位参会人员计入 {creditVal} 分" };
        }

        /// <summary>
        /// 全院/科室年度达标统计与未达标名单
        /// </summary>
        [HttpGet]
        public async Task<object> 年度达标统计(int? 年度 = null, int? 部门编号 = null)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办", "科室负责人", "部门管理员"))
            {
                return StatusCode(403, new { error = "无权查看全院达标统计" });
            }

            var year = 年度 ?? DateTime.UtcNow.Year;

            // 获取默认标准要求
            var standard = await Db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT * FROM ""继教学分标准"" WHERE ""年度"" = @year AND ""是否启用"" = TRUE
                  ORDER BY CASE WHEN ""职称级别"" = '不限' THEN 2 ELSE 1 END LIMIT 1;",
                new { year });

            decimal reqTotal = standard != null ? (decimal)standard.年度总分要求 : 25.0m;
            decimal reqClass1 = standard != null ? (decimal)standard.I类最低分 : 10.0m;

            var deptFilter = 部门编号.HasValue ? @"AND p.""部门编号"" = @部门编号" : "";

            var stats = (await Db.QueryAsync<dynamic>($@"
                WITH user_credits AS (
                    SELECT 
                        r.""人员编号"",
                        COALESCE(SUM(CASE WHEN r.""审核状态"" = '已认定' THEN r.""学分"" ELSE 0 END), 0) AS ""总分"",
                        COALESCE(SUM(CASE WHEN r.""审核状态"" = '已认定' AND r.""学分类别"" = 'I类' THEN r.""学分"" ELSE 0 END), 0) AS ""I类分"",
                        COALESCE(SUM(CASE WHEN r.""审核状态"" = '已认定' AND r.""学分类别"" = 'II类' THEN r.""学分"" ELSE 0 END), 0) AS ""II类分""
                    FROM ""继教学分记录"" r
                    WHERE r.""年度"" = @year
                    GROUP BY r.""人员编号""
                )
                SELECT 
                    p.""编号"" AS ""人员编号"",
                    p.""姓名"",
                    p.""工号"",
                    COALESCE(d.""名称"", '未分配科室') AS ""部门名称"",
                    p.""部门编号"",
                    COALESCE(p.""职位"", '医护人员') AS ""职称"",
                    COALESCE(uc.""总分"", 0) AS ""总分"",
                    COALESCE(uc.""I类分"", 0) AS ""I类分"",
                    COALESCE(uc.""II类分"", 0) AS ""II类分"",
                    CASE WHEN COALESCE(uc.""总分"", 0) >= @reqTotal AND COALESCE(uc.""I类分"", 0) >= @reqClass1 THEN TRUE ELSE FALSE END AS ""是否达标""
                FROM ""人员"" p
                LEFT JOIN ""部门"" d ON p.""部门编号"" = d.""编号""
                LEFT JOIN user_credits uc ON p.""编号"" = uc.""人员编号""
                WHERE p.""是否启用"" = TRUE {deptFilter}
                ORDER BY d.""名称"", p.""姓名"";",
                new { year, reqTotal, reqClass1, 部门编号 })).ToList();

            var totalStaff = stats.Count;
            var qualifiedCount = stats.Count(s => (bool)s.是否达标);
            var unqualifiedCount = totalStaff - qualifiedCount;
            var rate = totalStaff > 0 ? Math.Round((decimal)qualifiedCount / totalStaff * 100, 1) : 0;

            // 按科室汇总
            var deptGroups = stats.GroupBy(s => (string)s.部门名称).Select(g => new
            {
                部门名称 = g.Key,
                总人数 = g.Count(),
                达标人数 = g.Count(x => (bool)x.是否达标),
                未达标人数 = g.Count(x => !(bool)x.是否达标),
                达标率 = g.Any() ? Math.Round((decimal)g.Count(x => (bool)x.是否达标) / g.Count() * 100, 1) : 0
            }).OrderByDescending(g => g.未达标人数).ToList();

            return new
            {
                年度 = year,
                要求总分 = reqTotal,
                要求I类分 = reqClass1,
                总卫技人数 = totalStaff,
                达标总人数 = qualifiedCount,
                未达标总人数 = unqualifiedCount,
                全院达标率 = rate,
                科室汇总 = deptGroups,
                未达标人员名单 = stats.Where(s => !(bool)s.是否达标).Take(200).ToList()
            };
        }

        /// <summary>
        /// 批量发送继教学分催办通知
        /// </summary>
        [HttpPost]
        public async Task<object> 批量催办([FromBody] SendCmeRemindDto model)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办", "科室负责人"))
            {
                return StatusCode(403, new { error = "无权发起催办" });
            }

            if (model?.人员编号列表 == null || !model.人员编号列表.Any())
            {
                return BadRequest(new { error = "未选择催办人员" });
            }

            var year = model.年度 > 0 ? model.年度 : DateTime.UtcNow.Year;
            var content = string.IsNullOrWhiteSpace(model.催办内容)
                ? $"【科教科提示】您{year}年度继续医学教育学分尚未达标，请尽快通过院内讲座、会议或个人申报补齐学分，以免影响年度考核与职称申报。"
                : model.催办内容.Trim();

            int sentCount = 0;
            foreach (var uid in model.人员编号列表)
            {
                try
                {
                    await Db.ExecuteAsync(
                        @"INSERT INTO ""继教催办记录"" (
                            ""年度"", ""人员编号"", ""催办方式"", ""催办内容"", ""催办人编号"", ""催办人姓名"", ""催办时间""
                          ) VALUES (
                            @year, @uid, '系统通知', @content, @adminId, @adminName, CURRENT_TIMESTAMP
                          );",
                        new
                        {
                            year,
                            uid,
                            content,
                            adminId = CurrentUser.编号,
                            adminName = CurrentUser.姓名
                        });
                    sentCount++;
                }
                catch {}
            }

            return new { success = true, sentCount, message = $"已成功向 {sentCount} 位未达标人员发送催办通知" };
        }

        /// <summary>
        /// 导出年度学分达标统计报表 (CSV)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> 导出达标统计CSV(int? 年度 = null)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办"))
            {
                return StatusCode(403, new { error = "无权导出达标报表" });
            }

            var year = 年度 ?? DateTime.UtcNow.Year;
            var statsResult = await 年度达标统计(year, null) as dynamic;
            var list = statsResult?.未达标人员名单 as IEnumerable<dynamic>;

            var sb = new StringBuilder();
            sb.AppendLine("工号,姓名,部门,职称,已获I类分,已获II类分,已获总分,年度要求总分,是否达标");

            var allStaff = await Db.QueryAsync<dynamic>(@"
                WITH user_credits AS (
                    SELECT 
                        r.""人员编号"",
                        COALESCE(SUM(CASE WHEN r.""审核状态"" = '已认定' THEN r.""学分"" ELSE 0 END), 0) AS ""总分"",
                        COALESCE(SUM(CASE WHEN r.""审核状态"" = '已认定' AND r.""学分类别"" = 'I类' THEN r.""学分"" ELSE 0 END), 0) AS ""I类分"",
                        COALESCE(SUM(CASE WHEN r.""审核状态"" = '已认定' AND r.""学分类别"" = 'II类' THEN r.""学分"" ELSE 0 END), 0) AS ""II类分""
                    FROM ""继教学分记录"" r
                    WHERE r.""年度"" = @year
                    GROUP BY r.""人员编号""
                )
                SELECT 
                    p.""工号"", p.""姓名"", COALESCE(d.""名称"", '未分配') AS ""部门"", COALESCE(p.""职位"", '卫技人员') AS ""职称"",
                    COALESCE(uc.""I类分"", 0) AS ""I类分"", COALESCE(uc.""II类分"", 0) AS ""II类分"", COALESCE(uc.""总分"", 0) AS ""总分""
                FROM ""人员"" p
                LEFT JOIN ""部门"" d ON p.""部门编号"" = d.""编号""
                LEFT JOIN user_credits uc ON p.""编号"" = uc.""人员编号""
                WHERE p.""是否启用"" = TRUE
                ORDER BY d.""名称"", p.""姓名"";", new { year });

            foreach (var s in allStaff)
            {
                decimal t = (decimal)s.总分;
                decimal c1 = (decimal)s.I类分;
                bool ok = t >= 25.0m && c1 >= 10.0m;
                var safeName = s.姓名?.ToString().Replace(",", " ");
                var safeDept = s.部门?.ToString().Replace(",", " ");
                var safeTitle = s.职称?.ToString().Replace(",", " ");
                sb.AppendLine($"'{s.工号},{safeName},{safeDept},{safeTitle},{c1},{s.II类分},{t},25.0,{(ok ? "达标" : "未达标")}");
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"{year}年度继续医学教育达标统计表.csv");
        }
    }
}
