using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using ScientificResearch.Infrastucture;

namespace ScientificResearch.Controllers
{
    public class CheckJournalRiskRequest
    {
        public string 期刊名称 { get; set; }
        public string ISSN { get; set; }
        public string CN号 { get; set; }
        public string 期刊类型 { get; set; } = "国内";
    }

    public class PaperSubmissionDto
    {
        public int 编号 { get; set; }
        public string 备案号 { get; set; }
        public string 论文题目 { get; set; }
        public string 拟投期刊名称 { get; set; }
        public string ISSN { get; set; }
        public string CN号 { get; set; }
        public string 期刊类型 { get; set; } = "国内";
        public object 作者排序 { get; set; }
        public string 关联项目类型 { get; set; }
        public int? 关联项目编号 { get; set; }
        public string 稿件附件路径 { get; set; }
        public bool 诚信承诺 { get; set; }
        public decimal 预计版面费 { get; set; }
    }

    public class SubmissionAuditRequest
    {
        public int 编号 { get; set; }
        public bool 通过 { get; set; }
        public string 审核意见 { get; set; }
    }

    public class PublishRecordRequest
    {
        public int 编号 { get; set; }
        public int? 关联论文编号 { get; set; }
        public string 见刊说明 { get; set; }
    }

    /// <summary>
    /// 论文投稿事前备案与期刊风控接口
    /// </summary>
    public class PaperSubmissionController : BaseController
    {
        private static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            var chars = name.Trim().ToLowerInvariant().Where(c => !char.IsPunctuation(c) && !char.IsWhiteSpace(c)).ToArray();
            return new string(chars);
        }

        /// <summary>
        /// 实时检查拟投期刊风险（中科院预警、院内黑名单、正规期刊库核验及分区匹配）
        /// </summary>
        [HttpPost]
        public async Task<object> 检查期刊风险([FromBody] CheckJournalRiskRequest req)
        {
            if (req == null || (string.IsNullOrWhiteSpace(req.期刊名称) && string.IsNullOrWhiteSpace(req.ISSN)))
            {
                return BadRequest(new { error = "期刊名称与ISSN不能同时为空" });
            }

            var normName = NormalizeName(req.期刊名称);
            var issn = req.ISSN?.Trim();
            var cn = req.CN号?.Trim();
            var isDomestic = req.期刊类型 == "国内";

            // 1. 查询风险名单（中科院预警 / 院内黑名单 / 院内关注）
            var riskMatches = (await Db.QueryAsync<dynamic>(
                @"SELECT ""编号"", ""来源"", ""年度"", ""期刊名称"", ""风险等级"", ""风险原因"", ""生效状态""
                  FROM ""期刊风险名单""
                  WHERE ""生效状态"" = TRUE AND (
                      (""规范化名称"" = @normName AND @normName <> '')
                      OR (""ISSN"" = @issn AND @issn <> '')
                  )
                  ORDER BY CASE ""风险等级"" WHEN '高' THEN 1 WHEN '中' THEN 2 ELSE 3 END, ""年度"" DESC;",
                new { normName, issn })).ToList();

            // 2. 国内正规期刊库校验
            bool isOfficialDomestic = false;
            dynamic domesticRecord = null;
            if (isDomestic)
            {
                domesticRecord = await Db.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT ""编号"", ""期刊名称"", ""CN号"", ""ISSN"", ""主管单位"", ""主办单位"", ""是否医学类""
                      FROM ""国内正规期刊库""
                      WHERE (""CN号"" = @cn AND @cn <> '')
                         OR (""ISSN"" = @issn AND @issn <> '')
                         OR (""规范化名称"" = @normName AND @normName <> '');",
                    new { cn, issn, normName });
                isOfficialDomestic = domesticRecord != null;
            }

            // 3. 期刊分区匹配（中科院/JCR）
            var partitions = (await Db.QueryAsync<dynamic>(
                @"SELECT * FROM fn_匹配期刊分区(@issn, @期刊名称, @年度);",
                new { issn, 期刊名称 = req.期刊名称, 年度 = DateTime.UtcNow.Year })).ToList();

            // 4. 综合风险研判
            string overallRisk = "低";
            var messages = new List<string>();

            var highRisk = riskMatches.FirstOrDefault(r => (string)r.风险等级 == "高");
            var medRisk = riskMatches.FirstOrDefault(r => (string)r.风险等级 == "中");

            if (highRisk != null)
            {
                overallRisk = "高";
                messages.Add($"【高风险警示】该期刊被列入[{highRisk.来源}]（{highRisk.年度}年），风险原因：{highRisk.风险原因}。系统严禁向高风险期刊投稿，版面费不予报销！");
            }
            else if (medRisk != null)
            {
                overallRisk = "中";
                messages.Add($"【中风险提示】该期刊被列入[{medRisk.来源}]，原因：{medRisk.风险原因}。请作者谨慎评估，科教科将重点审核。");
            }

            if (isDomestic && !isOfficialDomestic)
            {
                if (overallRisk != "高") overallRisk = "中";
                messages.Add("【合规提醒】该刊物未收录于本院国内正规期刊白名单库，请核实其国家新闻出版署合法刊号（CN号/ISSN），谨防非法假刊！");
            }

            if (partitions != null && partitions.Any())
            {
                var partitionSummary = string.Join("; ", partitions.Select(p => $"{p.来源}: {p.分区}"));
                messages.Add($"【收录分区】匹配到分区参考：{partitionSummary}");
            }

            if (messages.Count == 0)
            {
                messages.Add("期刊状态良好，未发现预警或合规风险，符合医院投稿基本要求。");
            }

            return new
            {
                风险等级 = overallRisk,
                风险名单命中 = riskMatches,
                是否正规国内期刊 = isDomestic ? isOfficialDomestic : (bool?)null,
                正规期刊详情 = domesticRecord,
                分区匹配 = partitions,
                研判提示 = messages
            };
        }

        /// <summary>
        /// 保存论文投稿备案（草稿/更新草稿）
        /// </summary>
        [HttpPost]
        public async Task<object> 保存投稿备案([FromBody] PaperSubmissionDto model)
        {
            if (model == null) return BadRequest(new { error = "数据不能为空" });
            if (string.IsNullOrWhiteSpace(model.论文题目)) return BadRequest(new { error = "论文题目不能为空" });
            if (string.IsNullOrWhiteSpace(model.拟投期刊名称)) return BadRequest(new { error = "拟投期刊名称不能为空" });

            // 自动研判风险
            var riskResultObj = await 检查期刊风险(new CheckJournalRiskRequest
            {
                期刊名称 = model.拟投期刊名称,
                ISSN = model.ISSN,
                CN号 = model.CN号,
                期刊类型 = model.期刊类型
            });
            string riskLevel = "低";
            string riskResultJson = "{}";
            try
            {
                var dyn = riskResultObj as dynamic;
                riskLevel = dyn?.风险等级 ?? "低";
                riskResultJson = JsonSerializer.Serialize(riskResultObj);
            }
            catch {}

            string authorJson = "[]";
            if (model.作者排序 != null)
            {
                authorJson = model.作者排序 is string s ? s : JsonSerializer.Serialize(model.作者排序);
            }

            if (model.编号 == 0)
            {
                var filingNo = await Db.QuerySingleAsync<string>("SELECT fn_生成论文投稿备案号();");
                var id = await Db.QuerySingleAsync<int>(
                    @"INSERT INTO ""论文投稿备案"" (
                        ""备案号"", ""申请人编号"", ""申请人姓名"", ""部门编号"", ""部门名称"",
                        ""论文题目"", ""拟投期刊名称"", ""ISSN"", ""CN号"", ""期刊类型"",
                        ""作者排序"", ""关联项目类型"", ""关联项目编号"", ""稿件附件路径"",
                        ""诚信承诺"", ""风险检查结果"", ""风险等级"", ""状态"", ""预计版面费"", ""创建时间"", ""更新时间""
                    ) VALUES (
                        @filingNo, @userId, @userName, @deptId, @deptName,
                        @论文题目, @拟投期刊名称, @ISSN, @CN号, @期刊类型,
                        @authorJson::jsonb, @关联项目类型, @关联项目编号, @稿件附件路径,
                        @诚信承诺, @riskResultJson::jsonb, @riskLevel, '草稿', @预计版面费, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                    ) RETURNING ""编号"";",
                    new
                    {
                        filingNo,
                        userId = CurrentUser.编号,
                        userName = CurrentUser.姓名,
                        deptId = CurrentUser.部门编号,
                        deptName = CurrentUser.部门名称,
                        model.论文题目,
                        model.拟投期刊名称,
                        model.ISSN,
                        model.CN号,
                        期刊类型 = string.IsNullOrWhiteSpace(model.期刊类型) ? "国内" : model.期刊类型,
                        authorJson,
                        model.关联项目类型,
                        model.关联项目编号,
                        model.稿件附件路径,
                        model.诚信承诺,
                        riskResultJson,
                        riskLevel,
                        model.预计版面费
                    });

                return new { success = true, 编号 = id, 备案号 = filingNo, 风险等级 = riskLevel, message = "投稿备案草稿已保存" };
            }
            else
            {
                var existing = await Db.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT * FROM ""论文投稿备案"" WHERE ""编号"" = @编号;", new { model.编号 });
                if (existing == null) return NotFound(new { error = "备案不存在" });
                if ((int)existing.申请人编号 != CurrentUser.编号 && !await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办"))
                {
                    return StatusCode(403, new { error = "无权修改他人的备案草稿" });
                }
                if ((string)existing.状态 != "草稿" && (string)existing.状态 != "已驳回" && (string)existing.状态 != "已撤回")
                {
                    return StatusCode(409, new { error = $"当前状态[{existing.状态}]不允许编辑草稿" });
                }

                await Db.ExecuteAsync(
                    @"UPDATE ""论文投稿备案"" SET
                        ""论文题目"" = @论文题目,
                        ""拟投期刊名称"" = @拟投期刊名称,
                        ""ISSN"" = @ISSN,
                        ""CN号"" = @CN号,
                        ""期刊类型"" = @期刊类型,
                        ""作者排序"" = @authorJson::jsonb,
                        ""关联项目类型"" = @关联项目类型,
                        ""关联项目编号"" = @关联项目编号,
                        ""稿件附件路径"" = @稿件附件路径,
                        ""诚信承诺"" = @诚信承诺,
                        ""风险检查结果"" = @riskResultJson::jsonb,
                        ""风险等级"" = @riskLevel,
                        ""预计版面费"" = @预计版面费,
                        ""更新时间"" = CURRENT_TIMESTAMP
                      WHERE ""编号"" = @编号;",
                    new
                    {
                        model.编号,
                        model.论文题目,
                        model.拟投期刊名称,
                        model.ISSN,
                        model.CN号,
                        期刊类型 = string.IsNullOrWhiteSpace(model.期刊类型) ? "国内" : model.期刊类型,
                        authorJson,
                        model.关联项目类型,
                        model.关联项目编号,
                        model.稿件附件路径,
                        model.诚信承诺,
                        riskResultJson,
                        riskLevel,
                        model.预计版面费
                    });

                return new { success = true, 编号 = model.编号, 备案号 = (string)existing.备案号, 风险等级 = riskLevel, message = "投稿备案已更新" };
            }
        }

        /// <summary>
        /// 提交投稿备案送审（强制科研诚信承诺与高风险拦截）
        /// </summary>
        [HttpPost]
        public async Task<object> 提交投稿备案(int id)
        {
            var item = await Db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT * FROM ""论文投稿备案"" WHERE ""编号"" = @id;", new { id });
            if (item == null) return NotFound(new { error = "备案记录不存在" });
            if ((int)item.申请人编号 != CurrentUser.编号 && !await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办"))
            {
                return StatusCode(403, new { error = "无权提交他人的投稿备案" });
            }

            if (!((bool)item.诚信承诺))
            {
                return BadRequest(new { error = "必须签署并勾选《科研诚信与学术道德承诺书》方可提交投稿备案！" });
            }

            if ((string)item.状态 != "草稿" && (string)item.状态 != "已驳回" && (string)item.状态 != "已撤回")
            {
                return StatusCode(409, new { error = $"当前状态[{item.状态}]不可重复提交" });
            }

            // 重新计算风控拦截
            var riskCheck = await 检查期刊风险(new CheckJournalRiskRequest
            {
                期刊名称 = (string)item.拟投期刊名称,
                ISSN = (string)item.ISSN,
                CN号 = (string)item.CN号,
                期刊类型 = (string)item.期刊类型
            });
            var dyn = riskCheck as dynamic;
            string level = dyn?.风险等级 ?? "低";
            if (level == "高" && !await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办"))
            {
                return StatusCode(409, new { error = "该期刊属于高风险预警/黑名单期刊，系统拒绝提交！如确有正当研究依据，需联系科教科特批办理。" });
            }

            await Db.ExecuteAsync(
                @"UPDATE ""论文投稿备案"" SET
                    ""状态"" = '待科室审核',
                    ""风险等级"" = @level,
                    ""更新时间"" = CURRENT_TIMESTAMP
                  WHERE ""编号"" = @id;",
                new { id, level });

            await Db.ExecuteAsync(
                @"INSERT INTO ""论文投稿备案审核记录"" (
                    ""备案编号"", ""审核节点"", ""操作人编号"", ""操作人姓名"", ""操作结果"", ""审核意见""
                  ) VALUES (
                    @id, '提交申请', @userId, @userName, '已提交', '申请人提交投稿备案送审'
                  );",
                new { id, userId = CurrentUser.编号, userName = CurrentUser.姓名 });

            return new { success = true, message = "投稿备案已成功提交，进入科室审核流程" };
        }

        /// <summary>
        /// 撤回投稿备案
        /// </summary>
        [HttpPost]
        public async Task<object> 撤回投稿备案(int id)
        {
            var item = await Db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT * FROM ""论文投稿备案"" WHERE ""编号"" = @id;", new { id });
            if (item == null) return NotFound(new { error = "备案记录不存在" });
            if ((int)item.申请人编号 != CurrentUser.编号 && !await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办"))
            {
                return StatusCode(403, new { error = "无权撤回他人的投稿备案" });
            }

            if ((string)item.状态 != "待科室审核" && (string)item.状态 != "待科教科审核")
            {
                return StatusCode(409, new { error = $"当前状态[{item.状态}]不可撤回" });
            }

            await Db.ExecuteAsync(
                @"UPDATE ""论文投稿备案"" SET ""状态"" = '已撤回', ""更新时间"" = CURRENT_TIMESTAMP WHERE ""编号"" = @id;",
                new { id });

            await Db.ExecuteAsync(
                @"INSERT INTO ""论文投稿备案审核记录"" (
                    ""备案编号"", ""审核节点"", ""操作人编号"", ""操作人姓名"", ""操作结果"", ""审核意见""
                  ) VALUES (
                    @id, '申请人撤回', @userId, @userName, '已撤回', '申请人主动撤回申请'
                  );",
                new { id, userId = CurrentUser.编号, userName = CurrentUser.姓名 });

            return new { success = true, message = "投稿备案已撤回为可编辑状态" };
        }

        /// <summary>
        /// 科室审核投稿备案
        /// </summary>
        [HttpPost]
        public async Task<object> 科室审核([FromBody] SubmissionAuditRequest req)
        {
            if (req == null) return BadRequest(new { error = "参数不能为空" });
            var item = await Db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT * FROM ""论文投稿备案"" WHERE ""编号"" = @id;", new { id = req.编号 });
            if (item == null) return NotFound(new { error = "备案记录不存在" });

            if ((string)item.状态 != "待科室审核")
            {
                return StatusCode(409, new { error = $"当前状态[{item.状态}]不是待科室审核状态" });
            }

            // 校验是否科室负责人/部门管理员或科研管理角色
            bool isDeptAdmin = (int)item.部门编号 == CurrentUser.部门编号 && await HasAnyRoleAsync("科室负责人", "部门管理员", "科研秘书");
            bool isHospitalAdmin = await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办");
            if (!isDeptAdmin && !isHospitalAdmin)
            {
                return StatusCode(403, new { error = "无权进行本科室投稿备案审核" });
            }

            string nextState = req.通过 ? "待科教科审核" : "已驳回";
            await Db.ExecuteAsync(
                @"UPDATE ""论文投稿备案"" SET
                    ""状态"" = @nextState,
                    ""科室审核人编号"" = @userId,
                    ""科室审核人姓名"" = @userName,
                    ""科室审核时间"" = CURRENT_TIMESTAMP,
                    ""科室审核意见"" = @opinion,
                    ""更新时间"" = CURRENT_TIMESTAMP
                  WHERE ""编号"" = @id;",
                new
                {
                    nextState,
                    userId = CurrentUser.编号,
                    userName = CurrentUser.姓名,
                    opinion = req.审核意见,
                    id = req.编号
                });

            await Db.ExecuteAsync(
                @"INSERT INTO ""论文投稿备案审核记录"" (
                    ""备案编号"", ""审核节点"", ""操作人编号"", ""操作人姓名"", ""操作结果"", ""审核意见""
                  ) VALUES (
                    @id, '科室审核', @userId, @userName, @res, @opinion
                  );",
                new
                {
                    id = req.编号,
                    userId = CurrentUser.编号,
                    userName = CurrentUser.姓名,
                    res = req.通过 ? "审核通过" : "已驳回",
                    opinion = req.审核意见
                });

            return new { success = true, message = req.通过 ? "科室审核通过，已呈报科教科" : "已驳回申请" };
        }

        /// <summary>
        /// 科教科终审投稿备案
        /// </summary>
        [HttpPost]
        public async Task<object> 科教科审核([FromBody] SubmissionAuditRequest req)
        {
            if (req == null) return BadRequest(new { error = "参数不能为空" });
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办"))
            {
                return StatusCode(403, new { error = "仅科教科/科研管理员有权进行终审" });
            }

            var item = await Db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT * FROM ""论文投稿备案"" WHERE ""编号"" = @id;", new { id = req.编号 });
            if (item == null) return NotFound(new { error = "备案记录不存在" });

            if ((string)item.状态 != "待科教科审核")
            {
                return StatusCode(409, new { error = $"当前状态[{item.状态}]不是待科教科审核状态" });
            }

            string nextState = req.通过 ? "已备案" : "已驳回";
            await Db.ExecuteAsync(
                @"UPDATE ""论文投稿备案"" SET
                    ""状态"" = @nextState,
                    ""科教科审核人编号"" = @userId,
                    ""科教科审核人姓名"" = @userName,
                    ""科教科审核时间"" = CURRENT_TIMESTAMP,
                    ""科教科审核意见"" = @opinion,
                    ""更新时间"" = CURRENT_TIMESTAMP
                  WHERE ""编号"" = @id;",
                new
                {
                    nextState,
                    userId = CurrentUser.编号,
                    userName = CurrentUser.姓名,
                    opinion = req.审核意见,
                    id = req.编号
                });

            await Db.ExecuteAsync(
                @"INSERT INTO ""论文投稿备案审核记录"" (
                    ""备案编号"", ""审核节点"", ""操作人编号"", ""操作人姓名"", ""操作结果"", ""审核意见""
                  ) VALUES (
                    @id, '科教科终审', @userId, @userName, @res, @opinion
                  );",
                new
                {
                    id = req.编号,
                    userId = CurrentUser.编号,
                    userName = CurrentUser.姓名,
                    res = req.通过 ? "备案成功" : "已驳回",
                    opinion = req.审核意见
                });

            return new { success = true, message = req.通过 ? "备案成功，作者可凭备案号办理投稿与版面费报销" : "已驳回备案申请" };
        }

        /// <summary>
        /// 登记论文见刊并关联论文成果
        /// </summary>
        [HttpPost]
        public async Task<object> 登记见刊([FromBody] PublishRecordRequest req)
        {
            if (req == null) return BadRequest(new { error = "参数不能为空" });
            var item = await Db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT * FROM ""论文投稿备案"" WHERE ""编号"" = @id;", new { id = req.编号 });
            if (item == null) return NotFound(new { error = "备案不存在" });

            if ((int)item.申请人编号 != CurrentUser.编号 && !await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办"))
            {
                return StatusCode(403, new { error = "无权操作该备案" });
            }

            if ((string)item.状态 != "已备案" && (string)item.状态 != "已见刊")
            {
                return StatusCode(409, new { error = $"当前状态[{item.状态}]不可登记见刊" });
            }

            await Db.ExecuteAsync(
                @"UPDATE ""论文投稿备案"" SET
                    ""状态"" = '已见刊',
                    ""关联论文编号"" = @paperId,
                    ""更新时间"" = CURRENT_TIMESTAMP
                  WHERE ""编号"" = @id;",
                new { id = req.编号, paperId = req.关联论文编号 });

            await Db.ExecuteAsync(
                @"INSERT INTO ""论文投稿备案审核记录"" (
                    ""备案编号"", ""审核节点"", ""操作人编号"", ""操作人姓名"", ""操作结果"", ""审核意见""
                  ) VALUES (
                    @id, '登记见刊', @userId, @userName, '已见刊', @opinion
                  );",
                new
                {
                    id = req.编号,
                    userId = CurrentUser.编号,
                    userName = CurrentUser.姓名,
                    opinion = req.见刊说明 ?? "已完成正式发表并登记见刊"
                });

            return new { success = true, message = "见刊登记成功" };
        }

        /// <summary>
        /// 分页查询我的投稿备案
        /// </summary>
        [HttpGet]
        public async Task<object> 分页查询我的备案(int pageIndex = 1, int pageSize = 15, string 状态 = null, string 关键词 = null)
        {
            var offset = (pageIndex - 1) * pageSize;
            var sqlWhere = @"WHERE ""申请人编号"" = @userId";
            if (!string.IsNullOrWhiteSpace(状态)) sqlWhere += @" AND ""状态"" = @状态";
            if (!string.IsNullOrWhiteSpace(关键词)) sqlWhere += @" AND (""论文题目"" ILIKE @kw OR ""拟投期刊名称"" ILIKE @kw OR ""备案号"" ILIKE @kw)";

            var total = await Db.QuerySingleAsync<int>($@"SELECT COUNT(1) FROM ""论文投稿备案"" {sqlWhere};",
                new { userId = CurrentUser.编号, 状态, kw = $"%{关键词}%" });

            var list = await Db.QueryAsync<dynamic>($@"
                SELECT * FROM ""论文投稿备案""
                {sqlWhere}
                ORDER BY ""创建时间"" DESC
                OFFSET @offset LIMIT @pageSize;",
                new { userId = CurrentUser.编号, 状态, kw = $"%{关键词}%", offset, pageSize });

            return new { total, list, pageIndex, pageSize };
        }

        /// <summary>
        /// 分页查询待审核备案（科室审核 / 科教科终审）
        /// </summary>
        [HttpGet]
        public async Task<object> 分页查询待审备案(int pageIndex = 1, int pageSize = 15, string 节点 = "全部", string 关键词 = null)
        {
            bool isManager = await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办");
            bool isDeptAdmin = await HasAnyRoleAsync("科室负责人", "部门管理员", "科研秘书");

            if (!isManager && !isDeptAdmin)
            {
                return StatusCode(403, new { error = "无权查看审核列表" });
            }

            var sqlWhere = "WHERE 1=1";
            if (!isManager && isDeptAdmin)
            {
                sqlWhere += @" AND ""部门编号"" = @deptId AND ""状态"" = '待科室审核'";
            }
            else
            {
                if (节点 == "科室审核") sqlWhere += @" AND ""状态"" = '待科室审核'";
                else if (节点 == "科教科审核") sqlWhere += @" AND ""状态"" = '待科教科审核'";
                else sqlWhere += @" AND ""状态"" IN ('待科室审核', '待科教科审核')";
            }

            if (!string.IsNullOrWhiteSpace(关键词))
            {
                sqlWhere += @" AND (""论文题目"" ILIKE @kw OR ""拟投期刊名称"" ILIKE @kw OR ""申请人姓名"" ILIKE @kw OR ""备案号"" ILIKE @kw)";
            }

            var total = await Db.QuerySingleAsync<int>($@"SELECT COUNT(1) FROM ""论文投稿备案"" {sqlWhere};",
                new { deptId = CurrentUser.部门编号, kw = $"%{关键词}%" });

            var list = await Db.QueryAsync<dynamic>($@"
                SELECT * FROM ""论文投稿备案""
                {sqlWhere}
                ORDER BY ""创建时间"" ASC
                OFFSET @offset LIMIT @pageSize;",
                new { deptId = CurrentUser.部门编号, kw = $"%{关键词}%", offset = (pageIndex - 1) * pageSize, pageSize });

            return new { total, list, pageIndex, pageSize };
        }

        /// <summary>
        /// 查询可用于报销的有效备案号列表
        /// </summary>
        [HttpGet]
        public async Task<object> 查询可用报销备案(string 关键词 = null)
        {
            var sqlWhere = @"WHERE ""状态"" IN ('已备案', '已见刊') AND (
                ""申请人编号"" = @userId OR ""作者排序""::text ILIKE @userTag
            )";
            if (!string.IsNullOrWhiteSpace(关键词))
            {
                sqlWhere += @" AND (""论文题目"" ILIKE @kw OR ""拟投期刊名称"" ILIKE @kw OR ""备案号"" ILIKE @kw)";
            }

            var list = await Db.QueryAsync<dynamic>($@"
                SELECT ""编号"", ""备案号"", ""论文题目"", ""拟投期刊名称"", ""预计版面费"", ""状态"", ""创建时间""
                FROM ""论文投稿备案""
                {sqlWhere}
                ORDER BY ""创建时间"" DESC LIMIT 30;",
                new { userId = CurrentUser.编号, userTag = $"%{CurrentUser.姓名}%", kw = $"%{关键词}%" });

            return list;
        }

        /// <summary>
        /// 获取备案详情与审核记录
        /// </summary>
        [HttpGet]
        public async Task<object> 获取备案详情(int id)
        {
            var item = await Db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT * FROM ""论文投稿备案"" WHERE ""编号"" = @id;", new { id });
            if (item == null) return NotFound(new { error = "备案不存在" });

            var logs = await Db.QueryAsync<dynamic>(
                @"SELECT * FROM ""论文投稿备案审核记录"" WHERE ""备案编号"" = @id ORDER BY ""操作时间"" ASC;", new { id });

            return new { 基本信息 = item, 审核日志 = logs };
        }

        /// <summary>
        /// 管理员批量导入中科院预警/院内黑名单（Excel/CSV）
        /// </summary>
        [HttpPost]
        public async Task<object> 导入期刊风险名单(IFormFile file, [FromForm] string 来源 = "中科院预警", [FromForm] int? 年度 = null)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办"))
            {
                return StatusCode(403, new { error = "仅管理员有权导入风险名单" });
            }

            if (file == null || file.Length == 0) return BadRequest(new { error = "上传文件为空" });

            var year = 年度 ?? DateTime.UtcNow.Year;
            var successCount = 0;
            var skipCount = 0;

            using (var stream = file.OpenReadStream())
            using (var package = new ExcelPackage(stream))
            {
                var sheet = package.Workbook.Worksheets.FirstOrDefault();
                if (sheet == null) return BadRequest(new { error = "未找到有效的工作表" });

                var rowCount = sheet.Dimension?.Rows ?? 0;
                for (int row = 2; row <= rowCount; row++)
                {
                    var journalName = sheet.Cells[row, 1].Text?.Trim();
                    if (string.IsNullOrWhiteSpace(journalName)) continue;

                    var issn = sheet.Cells[row, 2].Text?.Trim();
                    var riskLevel = sheet.Cells[row, 3].Text?.Trim();
                    if (string.IsNullOrWhiteSpace(riskLevel) || (riskLevel != "高" && riskLevel != "中" && riskLevel != "低"))
                    {
                        riskLevel = "高";
                    }
                    var reason = sheet.Cells[row, 4].Text?.Trim();
                    var normName = NormalizeName(journalName);

                    try
                    {
                        await Db.ExecuteAsync(
                            @"INSERT INTO ""期刊风险名单"" (
                                ""来源"", ""年度"", ""期刊名称"", ""规范化名称"", ""ISSN"", ""风险等级"", ""风险原因"", ""生效状态"", ""导入批次"", ""创建人""
                              ) VALUES (
                                @来源, @year, @journalName, @normName, @issn, @riskLevel, @reason, TRUE, @file, @user
                              ) ON CONFLICT (""来源"", ""年度"", ""规范化名称"") DO UPDATE
                              SET ""风险等级"" = EXCLUDED.""风险等级"",
                                  ""风险原因"" = EXCLUDED.""风险原因"",
                                  ""ISSN"" = COALESCE(EXCLUDED.""ISSN"", ""期刊风险名单"".""ISSN""),
                                  ""生效状态"" = TRUE;",
                            new
                            {
                                来源,
                                year,
                                journalName,
                                normName,
                                issn,
                                riskLevel,
                                reason = string.IsNullOrWhiteSpace(reason) ? "中科院/学术委员会预警期刊" : reason,
                                file = file.FileName,
                                user = CurrentUser.姓名
                            });
                        successCount++;
                    }
                    catch
                    {
                        skipCount++;
                    }
                }
            }

            return new { success = true, successCount, skipCount, message = $"成功导入 {successCount} 条期刊风险记录" };
        }

        /// <summary>
        /// 管理员批量导入国内正规期刊白名单（Excel/CSV）
        /// </summary>
        [HttpPost]
        public async Task<object> 导入国内正规期刊(IFormFile file)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办"))
            {
                return StatusCode(403, new { error = "仅管理员有权导入正规期刊库" });
            }

            if (file == null || file.Length == 0) return BadRequest(new { error = "上传文件为空" });

            var successCount = 0;
            var skipCount = 0;

            using (var stream = file.OpenReadStream())
            using (var package = new ExcelPackage(stream))
            {
                var sheet = package.Workbook.Worksheets.FirstOrDefault();
                if (sheet == null) return BadRequest(new { error = "未找到有效工作表" });

                var rowCount = sheet.Dimension?.Rows ?? 0;
                for (int row = 2; row <= rowCount; row++)
                {
                    var name = sheet.Cells[row, 1].Text?.Trim();
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var cn = sheet.Cells[row, 2].Text?.Trim();
                    var issn = sheet.Cells[row, 3].Text?.Trim();
                    var host = sheet.Cells[row, 4].Text?.Trim();
                    var normName = NormalizeName(name);

                    try
                    {
                        await Db.ExecuteAsync(
                            @"INSERT INTO ""国内正规期刊库"" (
                                ""期刊名称"", ""规范化名称"", ""CN号"", ""ISSN"", ""主办单位"", ""是否医学类"", ""来源说明"", ""更新时间""
                              ) VALUES (
                                @name, @normName, @cn, @issn, @host, TRUE, @file, CURRENT_TIMESTAMP
                              );",
                            new { name, normName, cn, issn, host, file = file.FileName });
                        successCount++;
                    }
                    catch
                    {
                        skipCount++;
                    }
                }
            }

            return new { success = true, successCount, skipCount, message = $"成功导入 {successCount} 本国内正规期刊" };
        }

        /// <summary>
        /// 分页查询期刊风险名单
        /// </summary>
        [HttpGet]
        public async Task<object> 分页查询期刊风险名单(int pageIndex = 1, int pageSize = 20, string 来源 = null, string 关键词 = null)
        {
            var offset = (pageIndex - 1) * pageSize;
            var sqlWhere = "WHERE 1=1";
            if (!string.IsNullOrWhiteSpace(来源)) sqlWhere += @" AND ""来源"" = @来源";
            if (!string.IsNullOrWhiteSpace(关键词)) sqlWhere += @" AND (""期刊名称"" ILIKE @kw OR ""ISSN"" ILIKE @kw)";

            var total = await Db.QuerySingleAsync<int>($@"SELECT COUNT(1) FROM ""期刊风险名单"" {sqlWhere};",
                new { 来源, kw = $"%{关键词}%" });
            var list = await Db.QueryAsync<dynamic>($@"
                SELECT * FROM ""期刊风险名单"" {sqlWhere} ORDER BY ""年度"" DESC, ""编号"" DESC OFFSET @offset LIMIT @pageSize;",
                new { 来源, kw = $"%{关键词}%", offset, pageSize });

            return new { total, list, pageIndex, pageSize };
        }

        /// <summary>
        /// 分页查询国内正规期刊库
        /// </summary>
        [HttpGet]
        public async Task<object> 分页查询国内正规期刊(int pageIndex = 1, int pageSize = 20, string 关键词 = null)
        {
            var offset = (pageIndex - 1) * pageSize;
            var sqlWhere = "WHERE 1=1";
            if (!string.IsNullOrWhiteSpace(关键词)) sqlWhere += @" AND (""期刊名称"" ILIKE @kw OR ""CN号"" ILIKE @kw OR ""ISSN"" ILIKE @kw)";

            var total = await Db.QuerySingleAsync<int>($@"SELECT COUNT(1) FROM ""国内正规期刊库"" {sqlWhere};",
                new { kw = $"%{关键词}%" });
            var list = await Db.QueryAsync<dynamic>($@"
                SELECT * FROM ""国内正规期刊库"" {sqlWhere} ORDER BY ""编号"" DESC OFFSET @offset LIMIT @pageSize;",
                new { kw = $"%{关键词}%", offset, pageSize });

            return new { total, list, pageIndex, pageSize };
        }
    }
}
