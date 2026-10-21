using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using ScientificResearch.Infrastucture;

namespace ScientificResearch.Controllers
{
    /// <summary>横向合同治理：版本签审、合作方资质、技术合同收入认定和模板库。</summary>
    public sealed class HorizontalContractGovernanceController : BaseController
    {
        private async Task<bool> IsManager() => await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办");
        private static string NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        [HttpGet]
        public async Task<object> 查询合同治理(int 横向项目编号)
        {
            if (横向项目编号 <= 0) throw new ArgumentException("横向项目编号无效");
            if (!await CanAccessProjectAsync(config.GetValue<int>("流程模板编号:横向项目申请"), 横向项目编号)) return ProjectAccessDenied();
            var versions = await Db.QueryAsync<object>(@"SELECT * FROM ""横向合同版本"" WHERE ""横向项目编号""=@横向项目编号 ORDER BY ""版本号"" DESC;", new { 横向项目编号 });
            var qualifications = await Db.QueryAsync<object>(@"SELECT * FROM ""横向合作单位资质"" WHERE ""横向项目编号""=@横向项目编号 ORDER BY ""创建时间"" DESC;", new { 横向项目编号 });
            var recognitions = await Db.QueryAsync<object>(@"SELECT * FROM ""横向合同收入认定"" WHERE ""横向项目编号""=@横向项目编号 ORDER BY ""创建时间"" DESC;", new { 横向项目编号 });
            return new { 合同版本 = versions, 合作方资质 = qualifications, 收入认定 = recognitions };
        }

        [HttpPost]
        public async Task<object> 保存合同版本([FromBody] 横向合同版本保存 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权维护合同版本" });
            if (model == null || model.横向项目编号 <= 0 || model.版本号 <= 0) throw new ArgumentException("项目和版本号不能为空");
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""横向合同版本"" (""横向项目编号"",""版本号"",""版本类型"",""文件路径"",""文件校验和"",""变更摘要"",""签审状态"",""创建人编号"")
VALUES (@横向项目编号,@版本号,@版本类型,@文件路径,@文件校验和,@变更摘要,'待签审',@创建人编号)
ON CONFLICT (""横向项目编号"",""版本号"") DO UPDATE SET ""版本类型""=EXCLUDED.""版本类型"",""文件路径""=EXCLUDED.""文件路径"",""文件校验和""=EXCLUDED.""文件校验和"",""变更摘要""=EXCLUDED.""变更摘要"",""更新时间""=CURRENT_TIMESTAMP RETURNING ""编号"";", new { model.横向项目编号, model.版本号, 版本类型 = string.IsNullOrWhiteSpace(model.版本类型) ? "草稿" : model.版本类型.Trim(), 文件路径 = NullIfEmpty(model.文件路径), 文件校验和 = NullIfEmpty(model.文件校验和), 变更摘要 = NullIfEmpty(model.变更摘要), 创建人编号 = CurrentUser.编号 });
            return new { 编号 = id };
        }

        [HttpPost]
        public async Task<object> 更新合同签审状态([FromBody] 横向合同签审 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权更新合同签审状态" });
            if (model == null || model.编号 <= 0 || model.签审状态 is not ("签审中" or "已签审" or "退回" or "已作废")) throw new ArgumentException("签审参数无效");
            var changed = await Db.ExecuteAsync(@"UPDATE ""横向合同版本"" SET ""签审状态""=@签审状态,""签审人编号""=@签审人编号,""签审时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号;", new { model.编号, model.签审状态, 签审人编号 = CurrentUser.编号 });
            if (changed == 0) return NotFound(new { error = "合同版本不存在" });
            return new { model.编号, model.签审状态 };
        }

        [HttpPost]
        public async Task<object> 保存合作方资质([FromBody] 横向合作单位资质保存 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权维护合作方资质" });
            if (model == null || model.横向项目编号 <= 0 || string.IsNullOrWhiteSpace(model.单位名称) || string.IsNullOrWhiteSpace(model.资质类型)) throw new ArgumentException("项目、单位和资质类型不能为空");
            var id = await Db.QuerySingleAsync<long>(@"INSERT INTO ""横向合作单位资质"" (""横向项目编号"",""单位名称"",""统一社会信用代码"",""资质类型"",""资质编号"",""有效期至"",""证明文件路径"",""备注"",""创建人编号"") VALUES (@横向项目编号,@单位名称,@统一社会信用代码,@资质类型,@资质编号,@有效期至,@证明文件路径,@备注,@创建人编号) RETURNING ""编号"";", new { model.横向项目编号, model.单位名称, 统一社会信用代码 = NullIfEmpty(model.统一社会信用代码), 资质类型 = model.资质类型.Trim(), 资质编号 = NullIfEmpty(model.资质编号), model.有效期至, 证明文件路径 = NullIfEmpty(model.证明文件路径), 备注 = NullIfEmpty(model.备注), 创建人编号 = CurrentUser.编号 });
            return new { 编号 = id, 核验状态 = "待核验" };
        }

        [HttpPost]
        public async Task<object> 核验合作方资质([FromBody] 横向合作单位资质核验 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权核验合作方资质" });
            if (model == null || model.编号 <= 0 || model.核验状态 is not ("已通过" or "不通过")) throw new ArgumentException("资质核验状态无效");
            var changed = await Db.ExecuteAsync(@"UPDATE ""横向合作单位资质"" SET ""核验状态""=@核验状态,""核验人编号""=@核验人编号,""核验时间""=CURRENT_TIMESTAMP,""备注""=COALESCE(@备注,""备注"") WHERE ""编号""=@编号;", new { model.编号, model.核验状态, 核验人编号 = CurrentUser.编号, 备注 = NullIfEmpty(model.备注) });
            if (changed == 0) return NotFound(new { error = "资质记录不存在" });
            return new { model.编号, model.核验状态 };
        }

        [HttpPost]
        public async Task<object> 保存收入认定([FromBody] 横向合同收入认定保存 model)
        {
            if (model == null || model.横向项目编号 <= 0 || model.申请金额 < 0) throw new ArgumentException("收入认定参数无效");
            if (!await CanAccessProjectAsync(config.GetValue<int>("流程模板编号:横向项目申请"), model.横向项目编号)) return ProjectAccessDenied();
            var id = await Db.QuerySingleAsync<long>(@"INSERT INTO ""横向合同收入认定"" (""横向项目编号"",""认定类型"",""申请金额"",""申请材料路径"",""申请人编号"",""备注"") VALUES (@横向项目编号,@认定类型,@申请金额,@申请材料路径,@申请人编号,@备注) RETURNING ""编号"";", new { model.横向项目编号, 认定类型 = string.IsNullOrWhiteSpace(model.认定类型) ? "技术合同认定" : model.认定类型.Trim(), model.申请金额, 申请材料路径 = NullIfEmpty(model.申请材料路径), 申请人编号 = CurrentUser.编号, 备注 = NullIfEmpty(model.备注) });
            return new { 编号 = id, 状态 = "待提交" };
        }

        [HttpGet]
        public async Task<object> 查询合同模板(bool? 仅启用 = true) => await Db.QueryAsync<object>(@"SELECT * FROM ""横向合同模板"" WHERE (@仅启用 IS NULL OR ""是否启用""=@仅启用) ORDER BY ""合同类型"",""模板名称"",""版本号"" DESC;", new { 仅启用 });

        [HttpPost]
        public async Task<object> 保存合同模板([FromBody] 横向合同模板保存 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权维护合同模板" });
            if (model == null || string.IsNullOrWhiteSpace(model.模板编码) || string.IsNullOrWhiteSpace(model.模板名称) || model.版本号 <= 0) throw new ArgumentException("模板编码、名称和版本号不能为空");
            var id = await Db.QuerySingleAsync<long>(@"INSERT INTO ""横向合同模板"" (""模板编码"",""模板名称"",""合同类型"",""版本号"",""文件路径"",""文件校验和"",""是否启用"",""创建人编号"") VALUES (@模板编码,@模板名称,@合同类型,@版本号,@文件路径,@文件校验和,@是否启用,@创建人编号) ON CONFLICT (""模板编码"",""版本号"") DO UPDATE SET ""模板名称""=EXCLUDED.""模板名称"",""文件路径""=EXCLUDED.""文件路径"",""文件校验和""=EXCLUDED.""文件校验和"",""是否启用""=EXCLUDED.""是否启用"",""更新时间""=CURRENT_TIMESTAMP RETURNING ""编号"";", new { model.模板编码, model.模板名称, 合同类型 = string.IsNullOrWhiteSpace(model.合同类型) ? "产学研合同" : model.合同类型.Trim(), model.版本号, 文件路径 = NullIfEmpty(model.文件路径), 文件校验和 = NullIfEmpty(model.文件校验和), model.是否启用, 创建人编号 = CurrentUser.编号 });
            return new { 编号 = id };
        }
    }

    public sealed class 横向合同版本保存 { public int 横向项目编号 { get; set; } public int 版本号 { get; set; } public string 版本类型 { get; set; } public string 文件路径 { get; set; } public string 文件校验和 { get; set; } public string 变更摘要 { get; set; } }
    public sealed class 横向合同签审 { public long 编号 { get; set; } public string 签审状态 { get; set; } }
    public sealed class 横向合作单位资质保存 { public int 横向项目编号 { get; set; } public string 单位名称 { get; set; } public string 统一社会信用代码 { get; set; } public string 资质类型 { get; set; } public string 资质编号 { get; set; } public DateTime? 有效期至 { get; set; } public string 证明文件路径 { get; set; } public string 备注 { get; set; } }
    public sealed class 横向合作单位资质核验 { public long 编号 { get; set; } public string 核验状态 { get; set; } public string 备注 { get; set; } }
    public sealed class 横向合同收入认定保存 { public int 横向项目编号 { get; set; } public string 认定类型 { get; set; } public decimal 申请金额 { get; set; } public string 申请材料路径 { get; set; } public string 备注 { get; set; } }
    public sealed class 横向合同模板保存 { public string 模板编码 { get; set; } public string 模板名称 { get; set; } public string 合同类型 { get; set; } public int 版本号 { get; set; } public string 文件路径 { get; set; } public string 文件校验和 { get; set; } public bool 是否启用 { get; set; } = true; }
}
