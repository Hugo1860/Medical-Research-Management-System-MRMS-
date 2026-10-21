using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using ScientificResearch.Infrastucture;

namespace ScientificResearch.Controllers
{
    /// <summary>
    /// 项目配置：分类树、预算科目模板、建卡模式、预算拆分与编号规则
    /// </summary>
    public sealed class ProjectConfigurationController : BaseController
    {
        private async Task<bool> IsManager() => await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办");

        [HttpGet]
        public async Task<object> 查询分类树()
        {
            var list = await Db.QueryAsync<object>(@"
SELECT ""编号"", ""上级编号"", ""名称"", ""级别"", ""排序"", ""备注"",
       ""分类编码"", ""项目性质"", ""建卡模式"", ""默认预算模板编号"", ""编号规则编号"", ""是否启用""
FROM ""项目分类""
ORDER BY ""上级编号"", ""排序"", ""编号"";");
            return list;
        }

        [HttpPost]
        public async Task<object> 保存项目分类([FromBody] ProjectClassificationSaveModel model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权维护项目分类" });
            if (model == null || string.IsNullOrWhiteSpace(model.名称)) throw new ArgumentException("分类名称不能为空");

            if (model.编号 > 0)
            {
                await Db.ExecuteAsync(@"
UPDATE ""项目分类""
SET ""上级编号""=@上级编号, ""名称""=@名称, ""级别""=@级别, ""排序""=@排序, ""备注""=@备注,
    ""分类编码""=@分类编码, ""项目性质""=@项目性质, ""建卡模式""=@建卡模式,
    ""默认预算模板编号""=@默认预算模板编号, ""编号规则编号""=@编号规则编号, ""是否启用""=@是否启用,
    ""更新时间""=CURRENT_TIMESTAMP
WHERE ""编号""=@编号;", new {
                    model.编号,
                    上级编号 = model.上级编号 ?? 0,
                    model.名称,
                    级别 = model.级别 <= 0 ? 1 : model.级别,
                    model.排序,
                    model.备注,
                    分类编码 = string.IsNullOrWhiteSpace(model.分类编码) ? null : model.分类编码.Trim(),
                    项目性质 = string.IsNullOrWhiteSpace(model.项目性质) ? "纵向" : model.项目性质.Trim(),
                    建卡模式 = string.IsNullOrWhiteSpace(model.建卡模式) ? "单卡" : model.建卡模式.Trim(),
                    model.默认预算模板编号,
                    model.编号规则编号,
                    model.是否启用
                });
                return new { model.编号 };
            }
            else
            {
                var id = await Db.QuerySingleAsync<int>(@"
INSERT INTO ""项目分类"" (""上级编号"",""名称"",""级别"",""排序"",""备注"",""分类编码"",""项目性质"",""建卡模式"",""默认预算模板编号"",""编号规则编号"",""是否启用"")
VALUES (@上级编号, @名称, @级别, @排序, @备注, @分类编码, @项目性质, @建卡模式, @默认预算模板编号, @编号规则编号, @是否启用)
RETURNING ""编号"";", new {
                    上级编号 = model.上级编号 ?? 0,
                    model.名称,
                    级别 = model.级别 <= 0 ? 1 : model.级别,
                    model.排序,
                    model.备注,
                    分类编码 = string.IsNullOrWhiteSpace(model.分类编码) ? null : model.分类编码.Trim(),
                    项目性质 = string.IsNullOrWhiteSpace(model.项目性质) ? "纵向" : model.项目性质.Trim(),
                    建卡模式 = string.IsNullOrWhiteSpace(model.建卡模式) ? "单卡" : model.建卡模式.Trim(),
                    model.默认预算模板编号,
                    model.编号规则编号,
                    model.是否启用
                });
                return new { 编号 = id };
            }
        }

        [HttpGet]
        public async Task<object> 查询预算模板(string 项目性质 = null)
        {
            var templates = (await Db.QueryAsync<dynamic>(@"
SELECT * FROM ""预算科目模板""
WHERE (@项目性质 IS NULL OR ""适用项目性质""=@项目性质)
ORDER BY ""模板编码"", ""版本号"" DESC;", new { 项目性质 })).ToList();

            if (!templates.Any()) return templates;

            var templateIds = templates.Select(t => (int)t.编号).ToArray();
            var details = await Db.QueryAsync<dynamic>(@"
SELECT * FROM ""预算科目模板明细""
WHERE ""模板编号"" = ANY(@templateIds)
ORDER BY ""模板编号"", ""排序"";", new { templateIds });

            var grouped = details.GroupBy(d => (int)d.模板编号).ToDictionary(g => g.Key, g => g.ToList());
            var result = templates.Select(t => new {
                t.编号,
                t.模板编码,
                t.模板名称,
                t.版本号,
                t.适用项目性质,
                t.间接费比例上限,
                t.状态,
                t.备注,
                t.创建时间,
                明细 = grouped.ContainsKey((int)t.编号) ? grouped[(int)t.编号] : new List<dynamic>()
            });

            return result;
        }

        [HttpPost]
        public async Task<object> 保存预算模板([FromBody] BudgetTemplateSaveModel model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权维护预算模板" });
            if (model == null || string.IsNullOrWhiteSpace(model.模板编码) || string.IsNullOrWhiteSpace(model.模板名称))
                throw new ArgumentException("模板编码和名称不能为空");

            using var trans = Db.BeginTransaction();
            try
            {
                int templateId;
                if (model.编号 > 0)
                {
                    templateId = model.编号;
                    await Db.ExecuteAsync(@"
UPDATE ""预算科目模板""
SET ""模板编码""=@模板编码, ""模板名称""=@模板名称, ""版本号""=@版本号, ""适用项目性质""=@适用项目性质,
    ""间接费比例上限""=@间接费比例上限, ""状态""=@状态, ""备注""=@备注, ""更新时间""=CURRENT_TIMESTAMP
WHERE ""编号""=@templateId;", new {
                        templateId,
                        model.模板编码,
                        model.模板名称,
                        版本号 = model.版本号 <= 0 ? 1 : model.版本号,
                        适用项目性质 = string.IsNullOrWhiteSpace(model.适用项目性质) ? "纵向" : model.适用项目性质.Trim(),
                        间接费比例上限 = model.间接费比例上限 < 0 ? 0 : model.间接费比例上限,
                        状态 = string.IsNullOrWhiteSpace(model.状态) ? "草稿" : model.状态.Trim(),
                        model.备注
                    }, trans);

                    await Db.ExecuteAsync(@"DELETE FROM ""预算科目模板明细"" WHERE ""模板编号""=@templateId;", new { templateId }, trans);
                }
                else
                {
                    templateId = await Db.QuerySingleAsync<int>(@"
INSERT INTO ""预算科目模板"" (""模板编码"",""模板名称"",""版本号"",""适用项目性质"",""间接费比例上限"",""状态"",""备注"",""创建人编号"")
VALUES (@模板编码,@模板名称,@版本号,@适用项目性质,@间接费比例上限,@状态,@备注,@创建人编号)
RETURNING ""编号"";", new {
                        model.模板编码,
                        model.模板名称,
                        版本号 = model.版本号 <= 0 ? 1 : model.版本号,
                        适用项目性质 = string.IsNullOrWhiteSpace(model.适用项目性质) ? "纵向" : model.适用项目性质.Trim(),
                        间接费比例上限 = model.间接费比例上限 < 0 ? 0 : model.间接费比例上限,
                        状态 = string.IsNullOrWhiteSpace(model.状态) ? "草稿" : model.状态.Trim(),
                        model.备注,
                        创建人编号 = CurrentUser.编号
                    }, trans);
                }

                if (model.明细 != null && model.明细.Any())
                {
                    foreach (var item in model.明细)
                    {
                        if (string.IsNullOrWhiteSpace(item.科目编码) || string.IsNullOrWhiteSpace(item.科目名称)) continue;
                        await Db.ExecuteAsync(@"
INSERT INTO ""预算科目模板明细"" (""模板编号"",""科目编码"",""科目名称"",""上级科目编码"",""比例上限"",""是否间接费"",""是否允许调剂"",""排序"")
VALUES (@templateId, @科目编码, @科目名称, @上级科目编码, @比例上限, @是否间接费, @是否允许调剂, @排序);", new {
                            templateId,
                            item.科目编码,
                            item.科目名称,
                            item.上级科目编码,
                            item.比例上限,
                            item.是否间接费,
                            item.是否允许调剂,
                            item.排序
                        }, trans);
                    }
                }

                trans.Commit();
                return new { 编号 = templateId };
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        [HttpGet]
        public async Task<object> 查询编号规则()
        {
            return await Db.QueryAsync<object>(@"SELECT * FROM ""项目编号规则"" ORDER BY ""编号"";");
        }

        [HttpPost]
        public async Task<object> 保存编号规则([FromBody] ProjectNumberRuleSaveModel model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权维护编号规则" });
            if (model == null || string.IsNullOrWhiteSpace(model.规则编码) || string.IsNullOrWhiteSpace(model.前缀))
                throw new ArgumentException("规则编码和前缀不能为空");

            if (model.编号 > 0)
            {
                await Db.ExecuteAsync(@"
UPDATE ""项目编号规则""
SET ""规则编码""=@规则编码, ""规则名称""=@规则名称, ""前缀""=@前缀, ""日期格式""=@日期格式,
    ""流水位数""=@流水位数, ""重置周期""=@重置周期, ""更新时间""=CURRENT_TIMESTAMP
WHERE ""编号""=@编号;", new {
                    model.编号,
                    model.规则编码,
                    model.规则名称,
                    model.前缀,
                    日期格式 = string.IsNullOrWhiteSpace(model.日期格式) ? "YYYY" : model.日期格式.Trim(),
                    流水位数 = model.流水位数 < 3 ? 4 : model.流水位数,
                    重置周期 = string.IsNullOrWhiteSpace(model.重置周期) ? "年" : model.重置周期.Trim()
                });
                return new { model.编号 };
            }
            else
            {
                var id = await Db.QuerySingleAsync<int>(@"
INSERT INTO ""项目编号规则"" (""规则编码"",""规则名称"",""前缀"",""日期格式"",""流水位数"",""重置周期"")
VALUES (@规则编码,@规则名称,@前缀,@日期格式,@流水位数,@重置周期)
RETURNING ""编号"";", new {
                    model.规则编码,
                    model.规则名称,
                    model.前缀,
                    日期格式 = string.IsNullOrWhiteSpace(model.日期格式) ? "YYYY" : model.日期格式.Trim(),
                    流水位数 = model.流水位数 < 3 ? 4 : model.流水位数,
                    重置周期 = string.IsNullOrWhiteSpace(model.重置周期) ? "年" : model.重置周期.Trim()
                });
                return new { 编号 = id };
            }
        }

        [HttpPost]
        public async Task<object> 生成项目编号([FromBody] GenerateNumberReq model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权生成项目编号" });
            if (model == null || string.IsNullOrWhiteSpace(model.规则编码)) throw new ArgumentException("规则编码不能为空");
            var code = await Db.QuerySingleAsync<string>(@"SELECT ""fn_生成项目编号""(@规则编码);", new { model.规则编码 });
            return new { 项目编号 = code };
        }

        [HttpPost]
        public async Task<object> 按模板拆分预算([FromBody] BudgetSplitCalculateReq model)
        {
            if (model == null || model.模板编号 <= 0 || model.总金额 <= 0)
                throw new ArgumentException("模板编号和总金额必须大于0");

            var items = (await Db.QueryAsync<dynamic>(@"
SELECT * FROM ""预算科目模板明细""
WHERE ""模板编号""=@模板编号
ORDER BY ""排序"";", new { model.模板编号 })).ToList();

            if (!items.Any()) throw new ArgumentException("该模板未配置任何科目明细");

            decimal totalSplit = 0;
            var list = new List<dynamic>();

            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                decimal cap = it.比例上限 != null ? (decimal)it.比例上限 : (100.0m / items.Count);
                decimal amount = Math.Round(model.总金额 * (cap / 100.0m), 2);

                if (i == items.Count - 1)
                {
                    // 确保尾差刚好平账
                    amount = model.总金额 - totalSplit;
                }
                else
                {
                    totalSplit += amount;
                }

                list.Add(new {
                    it.科目编码,
                    it.科目名称,
                    it.是否间接费,
                    比例 = cap,
                    拆分金额 = amount
                });
            }

            return new {
                model.总金额,
                科目拆分 = list
            };
        }
    }

    public sealed class ProjectClassificationSaveModel
    {
        public int 编号 { get; set; }
        public int? 上级编号 { get; set; }
        public string 分类编码 { get; set; }
        public string 名称 { get; set; }
        public int 级别 { get; set; }
        public int 排序 { get; set; }
        public string 项目性质 { get; set; }
        public string 建卡模式 { get; set; }
        public int? 默认预算模板编号 { get; set; }
        public int? 编号规则编号 { get; set; }
        public bool 是否启用 { get; set; } = true;
        public string 备注 { get; set; }
    }

    public sealed class BudgetTemplateSaveModel
    {
        public int 编号 { get; set; }
        public string 模板编码 { get; set; }
        public string 模板名称 { get; set; }
        public int 版本号 { get; set; } = 1;
        public string 适用项目性质 { get; set; } = "纵向";
        public decimal 间接费比例上限 { get; set; } = 30.0m;
        public string 状态 { get; set; } = "启用";
        public string 备注 { get; set; }
        public List<BudgetTemplateDetailItem> 明细 { get; set; }
    }

    public sealed class BudgetTemplateDetailItem
    {
        public string 科目编码 { get; set; }
        public string 科目名称 { get; set; }
        public string 上级科目编码 { get; set; }
        public decimal? 比例上限 { get; set; }
        public bool 是否间接费 { get; set; }
        public bool 是否允许调剂 { get; set; } = true;
        public int 排序 { get; set; }
    }

    public sealed class ProjectNumberRuleSaveModel
    {
        public int 编号 { get; set; }
        public string 规则编码 { get; set; }
        public string 规则名称 { get; set; }
        public string 前缀 { get; set; }
        public string 日期格式 { get; set; } = "YYYY";
        public int 流水位数 { get; set; } = 4;
        public string 重置周期 { get; set; } = "年";
    }

    public sealed class GenerateNumberReq
    {
        public string 规则编码 { get; set; }
    }

    public sealed class BudgetSplitCalculateReq
    {
        public int 模板编号 { get; set; }
        public decimal 总金额 { get; set; }
    }
}
