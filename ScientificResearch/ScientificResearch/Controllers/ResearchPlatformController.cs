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
    /// 科研平台与科研公共服务目录。平台/目录由科研管理角色维护，服务申请由申请人发起并由配置的办理角色处理。
    /// 这里不伪造外部平台预约或 HR/财务结果，所有状态均来自数据库记录。
    /// </summary>
    public sealed class ResearchPlatformController : BaseController
    {
        private async Task<bool> IsManager() => await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办");
        private static string 空值(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        [HttpGet]
        public async Task<object> 查询科研平台(string 平台类型 = null, bool? 仅启用 = null)
        {
            return await Db.QueryAsync<object>(@"
SELECT p.""编号"",p.""平台名称"",p.""平台类型"",p.""依托部门编号"",d.""名称"" AS ""依托部门名称"",
       p.""负责人编号"",owner.""姓名"" AS ""负责人姓名"",p.""平台简介"",p.""开放状态"",p.""联系方式"",
       p.""规章制度路径"",p.""年度报告年份"",p.""年度报告路径"",p.""是否启用"",p.""创建时间"",p.""更新时间"",
       (SELECT count(*) FROM ""科研平台成员"" m WHERE m.""平台编号""=p.""编号"" AND m.""退出时间"" IS NULL) AS ""成员数""
  FROM ""科研平台"" p
  LEFT JOIN ""部门"" d ON d.""编号""=p.""依托部门编号""
  LEFT JOIN ""人员"" owner ON owner.""编号""=p.""负责人编号""
 WHERE (@平台类型 IS NULL OR p.""平台类型""=@平台类型)
   AND (@仅启用 IS NULL OR p.""是否启用""=@仅启用)
 ORDER BY p.""是否启用"" DESC,p.""更新时间"" DESC,p.""编号"" DESC;", new { 平台类型 = 空值(平台类型), 仅启用 });
        }

        [HttpPost]
        public async Task<object> 保存科研平台([FromBody] 科研平台保存 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权维护科研平台" });
            if (model == null || string.IsNullOrWhiteSpace(model.平台名称)) throw new ArgumentException("平台名称不能为空");
            if (model.平台名称.Length > 200 || (model.平台简介?.Length ?? 0) > 3000) throw new ArgumentException("平台字段超出限制");
            if (model.编号.HasValue)
            {
                var changed = await Db.ExecuteAsync(@"
UPDATE ""科研平台"" SET ""平台名称""=@平台名称,""平台类型""=@平台类型,""依托部门编号""=@依托部门编号,
""负责人编号""=@负责人编号,""平台简介""=@平台简介,""开放状态""=@开放状态,""联系方式""=@联系方式,
""规章制度路径""=@规章制度路径,""年度报告年份""=@年度报告年份,""年度报告路径""=@年度报告路径,
""是否启用""=@是否启用,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号;", new
                {
                    model.编号, model.平台名称, 平台类型 = string.IsNullOrWhiteSpace(model.平台类型) ? "科研平台" : model.平台类型.Trim(),
                    model.依托部门编号, model.负责人编号, model.平台简介, 开放状态 = string.IsNullOrWhiteSpace(model.开放状态) ? "开放" : model.开放状态.Trim(),
                    model.联系方式, model.规章制度路径, model.年度报告年份, model.年度报告路径, model.是否启用
                });
                if (changed == 0) return NotFound(new { error = "科研平台不存在" });
                return new { 编号 = model.编号.Value };
            }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研平台"" (""平台名称"",""平台类型"",""依托部门编号"",""负责人编号"",""平台简介"",""开放状态"",""联系方式"",""规章制度路径"",""年度报告年份"",""年度报告路径"",""是否启用"",""创建人编号"")
VALUES (@平台名称,@平台类型,@依托部门编号,@负责人编号,@平台简介,@开放状态,@联系方式,@规章制度路径,@年度报告年份,@年度报告路径,@是否启用,@创建人编号)
RETURNING ""编号"";", new
            {
                model.平台名称, 平台类型 = string.IsNullOrWhiteSpace(model.平台类型) ? "科研平台" : model.平台类型.Trim(), model.依托部门编号,
                model.负责人编号, model.平台简介, 开放状态 = string.IsNullOrWhiteSpace(model.开放状态) ? "开放" : model.开放状态.Trim(),
                model.联系方式, model.规章制度路径, model.年度报告年份, model.年度报告路径, model.是否启用, 创建人编号 = CurrentUser.编号
            });
            return new { 编号 = id };
        }

        [HttpGet]
        public async Task<object> 查询科研平台成员(long 平台编号)
        {
            if (平台编号 <= 0) throw new ArgumentException("平台编号无效");
            return await Db.QueryAsync<object>(@"
SELECT m.""编号"",m.""平台编号"",m.""人员编号"",p.""工号"",p.""姓名"",d.""名称"" AS ""部门名称"",
       m.""成员角色"",m.""加入时间"",m.""退出时间"",m.""备注""
  FROM ""科研平台成员"" m JOIN ""人员"" p ON p.""编号""=m.""人员编号""
  LEFT JOIN ""部门"" d ON d.""编号""=p.""部门编号""
 WHERE m.""平台编号""=@平台编号 ORDER BY m.""退出时间"" NULLS FIRST,m.""加入时间"";", new { 平台编号 });
        }

        [HttpPost]
        public async Task<object> 保存科研平台成员([FromBody] 科研平台成员保存 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权维护科研平台成员" });
            if (model == null || model.平台编号 <= 0 || model.人员编号 <= 0) throw new ArgumentException("平台和人员不能为空");
            if (model.编号.HasValue)
            {
                var changed = await Db.ExecuteAsync(@"
UPDATE ""科研平台成员"" SET ""平台编号""=@平台编号,""人员编号""=@人员编号,""成员角色""=@成员角色,""加入时间""=@加入时间,""退出时间""=@退出时间,""备注""=@备注 WHERE ""编号""=@编号;", new
                { model.编号, model.平台编号, model.人员编号, 成员角色 = string.IsNullOrWhiteSpace(model.成员角色) ? "成员" : model.成员角色.Trim(), 加入时间 = model.加入时间 ?? DateTime.Today, model.退出时间, model.备注 });
                if (changed == 0) return NotFound(new { error = "平台成员不存在" });
                return new { 编号 = model.编号.Value };
            }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研平台成员"" (""平台编号"",""人员编号"",""成员角色"",""加入时间"",""退出时间"",""备注"")
VALUES (@平台编号,@人员编号,@成员角色,@加入时间,@退出时间,@备注) RETURNING ""编号"";", new
            { model.平台编号, model.人员编号, 成员角色 = string.IsNullOrWhiteSpace(model.成员角色) ? "成员" : model.成员角色.Trim(), 加入时间 = model.加入时间 ?? DateTime.Today, model.退出时间, model.备注 });
            return new { 编号 = id };
        }

        [HttpGet]
        public async Task<object> 查询科研服务事项(string 服务分类 = null, bool? 仅启用 = true)
        {
            return await Db.QueryAsync<object>(@"
SELECT s.""编号"",s.""服务编码"",s.""服务名称"",s.""服务分类"",s.""服务说明"",s.""所需材料模板路径"",
       s.""办理角色"",s.""办理人员编号"",handler.""姓名"" AS ""办理人员姓名"",s.""办理时限天数"",s.""咨询电话"",s.""是否启用"",s.""创建时间"",s.""更新时间"",
       (SELECT count(*) FROM ""科研服务申请"" a WHERE a.""服务事项编号""=s.""编号"") AS ""申请总数""
  FROM ""科研服务事项"" s
  LEFT JOIN ""人员"" handler ON handler.""编号""=s.""办理人员编号""
 WHERE (@服务分类 IS NULL OR s.""服务分类""=@服务分类) AND (@仅启用 IS NULL OR s.""是否启用""=@仅启用)
 ORDER BY s.""是否启用"" DESC,s.""服务分类"",s.""服务名称"";", new { 服务分类 = 空值(服务分类), 仅启用 });
        }

        [HttpPost]
        public async Task<object> 保存科研服务事项([FromBody] 科研服务事项保存 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权维护科研服务目录" });
            if (model == null || string.IsNullOrWhiteSpace(model.服务编码) || string.IsNullOrWhiteSpace(model.服务名称)) throw new ArgumentException("服务编码和服务名称不能为空");
            if (model.服务编码.Length > 80 || model.服务名称.Length > 200) throw new ArgumentException("服务字段超出限制");
            var args = new
            {
                model.服务编码, model.服务名称, 服务分类 = string.IsNullOrWhiteSpace(model.服务分类) ? "科研公共服务" : model.服务分类.Trim(),
                model.服务说明, model.所需材料模板路径, model.办理角色, 办理人员编号 = model.办理人员编号 is > 0 ? model.办理人员编号 : null,
                办理时限天数 = model.办理时限天数 is > 0 and <= 365 ? model.办理时限天数 : 5,
                model.咨询电话, model.是否启用, 创建人编号 = CurrentUser.编号, model.编号
            };
            if (model.编号.HasValue)
            {
                var changed = await Db.ExecuteAsync(@"
UPDATE ""科研服务事项"" SET ""服务编码""=@服务编码,""服务名称""=@服务名称,""服务分类""=@服务分类,""服务说明""=@服务说明,""所需材料模板路径""=@所需材料模板路径,
""办理角色""=@办理角色,""办理人员编号""=@办理人员编号,""办理时限天数""=@办理时限天数,""咨询电话""=@咨询电话,""是否启用""=@是否启用,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号;", args);
                if (changed == 0) return NotFound(new { error = "服务事项不存在" });
                return new { 编号 = model.编号.Value };
            }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研服务事项"" (""服务编码"",""服务名称"",""服务分类"",""服务说明"",""所需材料模板路径"",""办理角色"",""办理人员编号"",""办理时限天数"",""咨询电话"",""是否启用"",""创建人编号"")
VALUES (@服务编码,@服务名称,@服务分类,@服务说明,@所需材料模板路径,@办理角色,@办理人员编号,@办理时限天数,@咨询电话,@是否启用,@创建人编号) RETURNING ""编号"";", args);
            return new { 编号 = id };
        }

        [HttpPost]
        public async Task<object> 上传科研服务材料模板()
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权上传科研服务材料模板" });
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/科研平台/服务材料模板",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpPost]
        public async Task<object> 发起科研服务申请([FromBody] 科研服务申请保存 model)
        {
            if (model == null || model.服务事项编号 <= 0) throw new ArgumentException("服务事项不能为空");
            var id = await Db.QuerySingleOrDefaultAsync<long?>(@"
INSERT INTO ""科研服务申请"" (""服务事项编号"",""申请人编号"",""关联业务类型"",""关联业务编号"",""申请说明"",""附件路径"",""当前处理人编号"",""当前处理角色"",""办理期限"")
SELECT s.""编号"",@申请人编号,@关联业务类型,@关联业务编号,@申请说明,@附件路径,s.""办理人员编号"",s.""办理角色"",
       CURRENT_TIMESTAMP + (s.""办理时限天数"" || ' days')::interval
  FROM ""科研服务事项"" s WHERE s.""编号""=@服务事项编号 AND s.""是否启用""=TRUE
RETURNING ""编号"";", new { model.服务事项编号, 申请人编号 = CurrentUser.编号, model.关联业务类型, model.关联业务编号, model.申请说明, model.附件路径 });
            if (!id.HasValue) return BadRequest(new { error = "服务事项不存在或已停用" });
            return new { 编号 = id.Value, 状态 = "待受理" };
        }

        [HttpGet]
        public async Task<object> 查询我的科研服务申请(string 状态 = null)
        {
            return await Db.QueryAsync<object>(@"
SELECT a.""编号"",a.""服务事项编号"",s.""服务编码"",s.""服务名称"",s.""服务分类"",s.""办理角色"",s.""办理人员编号"",handler.""姓名"" AS ""办理人员姓名"",
       a.""关联业务类型"",a.""关联业务编号"",a.""申请说明"",a.""附件路径"",a.""状态"",a.""当前处理人编号"",p.""姓名"" AS ""当前处理人姓名"",
       a.""当前处理角色"",a.""受理时间"",a.""完成时间"",a.""办理期限"",a.""处理意见"",a.""创建时间"",a.""更新时间"",
       f.""评价星级"",f.""反馈内容""
  FROM ""科研服务申请"" a JOIN ""科研服务事项"" s ON s.""编号""=a.""服务事项编号""
  LEFT JOIN ""人员"" p ON p.""编号""=a.""当前处理人编号""
  LEFT JOIN ""人员"" handler ON handler.""编号""=s.""办理人员编号""
  LEFT JOIN ""科研服务反馈"" f ON f.""服务申请编号""=a.""编号"" AND f.""评价人编号""=@申请人编号
 WHERE a.""申请人编号""=@申请人编号 AND (@状态 IS NULL OR a.""状态""=@状态)
 ORDER BY a.""更新时间"" DESC,a.""编号"" DESC;", new { 申请人编号 = CurrentUser.编号, 状态 = 空值(状态) });
        }

        [HttpGet]
        public async Task<object> 查询待受理科研服务(string 状态 = "待受理")
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权查看科研服务待办" });
            return await Db.QueryAsync<object>(@"
SELECT a.""编号"",a.""服务事项编号"",s.""服务编码"",s.""服务名称"",s.""服务分类"",s.""办理角色"",s.""办理人员编号"",handler.""姓名"" AS ""办理人员姓名"",a.""申请人编号"",p.""姓名"" AS ""申请人姓名"",
       a.""状态"",a.""当前处理人编号"",current_handler.""姓名"" AS ""当前处理人姓名"",a.""当前处理角色"",a.""申请说明"",a.""附件路径"",a.""办理期限"",a.""创建时间"",a.""更新时间""
  FROM ""科研服务申请"" a JOIN ""科研服务事项"" s ON s.""编号""=a.""服务事项编号""
  JOIN ""人员"" p ON p.""编号""=a.""申请人编号""
  LEFT JOIN ""人员"" handler ON handler.""编号""=s.""办理人员编号""
  LEFT JOIN ""人员"" current_handler ON current_handler.""编号""=a.""当前处理人编号""
 WHERE (@状态 IS NULL OR a.""状态""=@状态)
 ORDER BY a.""办理期限"" NULLS LAST,a.""创建时间"";", new { 状态 = 空值(状态) });
        }

        [HttpPost]
        public async Task<object> 处理科研服务申请([FromBody] 科研服务处理 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权处理科研服务申请" });
            if (model == null || model.编号 <= 0 || !new[] { "处理中", "退回", "已完成", "已取消" }.Contains(model.状态)) throw new ArgumentException("服务处理状态无效");
            var current = await Db.QuerySingleOrDefaultAsync<string>("SELECT \"状态\" FROM \"科研服务申请\" WHERE \"编号\"=@编号;", new { model.编号 });
            if (current == null) return NotFound(new { error = "服务申请不存在" });
            if (current is "已完成" or "已取消") return BadRequest(new { error = "终态申请不可再次处理" });
            var changed = await Db.ExecuteAsync(@"
UPDATE ""科研服务申请"" SET ""状态""=@状态,""当前处理人编号""=@当前处理人编号,
""受理时间""=CASE WHEN @状态 IN ('处理中','退回','已完成') THEN COALESCE(""受理时间"",CURRENT_TIMESTAMP) ELSE ""受理时间"" END,
""完成时间""=CASE WHEN @状态='已完成' THEN CURRENT_TIMESTAMP ELSE NULL END,
""处理意见""=@处理意见,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号;", new { model.编号, model.状态, 当前处理人编号 = CurrentUser.编号, model.处理意见 });
            return new { 编号 = model.编号, 状态 = model.状态, 更新数 = changed };
        }

        [HttpPost]
        public async Task<object> 评价科研服务([FromBody] 科研服务反馈保存 model)
        {
            if (model == null || model.服务申请编号 <= 0 || model.评价星级 < 1 || model.评价星级 > 5) throw new ArgumentException("评价内容无效");
            var own = await Db.QuerySingleOrDefaultAsync<bool?>(@"
SELECT (a.""申请人编号""=@评价人编号 AND a.""状态""='已完成') FROM ""科研服务申请"" a WHERE a.""编号""=@服务申请编号;", new { model.服务申请编号, 评价人编号 = CurrentUser.编号 });
            if (own != true) return BadRequest(new { error = "仅能评价自己已完成的服务申请" });
            await Db.ExecuteAsync(@"
INSERT INTO ""科研服务反馈"" (""服务申请编号"",""评价人编号"",""评价星级"",""反馈内容"",""是否公开"")
VALUES (@服务申请编号,@评价人编号,@评价星级,@反馈内容,@是否公开)
ON CONFLICT (""服务申请编号"",""评价人编号"") DO UPDATE SET ""评价星级""=EXCLUDED.""评价星级"",""反馈内容""=EXCLUDED.""反馈内容"",""是否公开""=EXCLUDED.""是否公开"";", new { model.服务申请编号, 评价人编号 = CurrentUser.编号, model.评价星级, model.反馈内容, model.是否公开 });
            return new { 服务申请编号 = model.服务申请编号, 评价星级 = model.评价星级 };
        }

        [HttpPost]
        public async Task<object> 记录科研导入导出日志([FromBody] 科研导入导出日志保存 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权记录导入导出日志" });
            if (model == null || string.IsNullOrWhiteSpace(model.模块) || !new[] { "导入", "导出" }.Contains(model.操作类型)) throw new ArgumentException("导入导出日志字段无效");
            if (model.记录数 < 0 || model.成功数 < 0 || model.失败数 < 0 || model.成功数 + model.失败数 > model.记录数) throw new ArgumentException("导入导出记录数不一致");
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研导入导出日志"" (""模块"",""操作类型"",""文件名称"",""文件类型"",""记录数"",""成功数"",""失败数"",""状态"",""操作人编号"",""是否可撤销"",""详情"")
VALUES (@模块,@操作类型,@文件名称,@文件类型,@记录数,@成功数,@失败数,@状态,@操作人编号,@是否可撤销,CAST(@详情 AS jsonb)) RETURNING ""编号"";", new
            { model.模块, model.操作类型, model.文件名称, model.文件类型, model.记录数, model.成功数, model.失败数, 状态 = string.IsNullOrWhiteSpace(model.状态) ? "处理中" : model.状态.Trim(), 操作人编号 = CurrentUser.编号, model.是否可撤销, 详情 = string.IsNullOrWhiteSpace(model.详情) ? "{}" : model.详情 });
            return new { 编号 = id };
        }

        [HttpGet]
        public async Task<object> 查询科研导入导出日志(string 模块 = null, string 操作类型 = null, int 数量 = 100)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权查看导入导出日志" });
            var limit = Math.Clamp(数量, 1, 500);
            return await Db.QueryAsync<object>(@"
SELECT l.""编号"",l.""模块"",l.""操作类型"",l.""文件名称"",l.""文件类型"",l.""记录数"",l.""成功数"",l.""失败数"",l.""状态"",l.""操作人编号"",p.""姓名"" AS ""操作人姓名"",l.""是否可撤销"",l.""撤销时间"",l.""详情"",l.""创建时间""
  FROM ""科研导入导出日志"" l LEFT JOIN ""人员"" p ON p.""编号""=l.""操作人编号""
 WHERE (@模块 IS NULL OR l.""模块""=@模块) AND (@操作类型 IS NULL OR l.""操作类型""=@操作类型)
 ORDER BY l.""创建时间"" DESC,l.""编号"" DESC LIMIT @数量;", new { 模块 = 空值(模块), 操作类型 = 空值(操作类型), 数量 = limit });
        }
    }

    public sealed class 科研平台保存
    {
        public long? 编号 { get; set; }
        public string 平台名称 { get; set; }
        public string 平台类型 { get; set; }
        public int? 依托部门编号 { get; set; }
        public int? 负责人编号 { get; set; }
        public string 平台简介 { get; set; }
        public string 开放状态 { get; set; }
        public string 联系方式 { get; set; }
        public string 规章制度路径 { get; set; }
        public int? 年度报告年份 { get; set; }
        public string 年度报告路径 { get; set; }
        public bool 是否启用 { get; set; } = true;
    }

    public sealed class 科研平台成员保存
    {
        public long? 编号 { get; set; }
        public long 平台编号 { get; set; }
        public int 人员编号 { get; set; }
        public string 成员角色 { get; set; }
        public DateTime? 加入时间 { get; set; }
        public DateTime? 退出时间 { get; set; }
        public string 备注 { get; set; }
    }

    public sealed class 科研服务事项保存
    {
        public long? 编号 { get; set; }
        public string 服务编码 { get; set; }
        public string 服务名称 { get; set; }
        public string 服务分类 { get; set; }
        public string 服务说明 { get; set; }
        public string 所需材料模板路径 { get; set; }
        public string 办理角色 { get; set; }
        public int? 办理人员编号 { get; set; }
        public int 办理时限天数 { get; set; } = 5;
        public string 咨询电话 { get; set; }
        public bool 是否启用 { get; set; } = true;
    }

    public sealed class 科研服务申请保存
    {
        public long 服务事项编号 { get; set; }
        public string 关联业务类型 { get; set; }
        public long? 关联业务编号 { get; set; }
        public string 申请说明 { get; set; }
        public string 附件路径 { get; set; }
    }

    public sealed class 科研服务处理
    {
        public long 编号 { get; set; }
        public string 状态 { get; set; }
        public string 处理意见 { get; set; }
    }

    public sealed class 科研服务反馈保存
    {
        public long 服务申请编号 { get; set; }
        public int 评价星级 { get; set; }
        public string 反馈内容 { get; set; }
        public bool 是否公开 { get; set; }
    }

    public sealed class 科研导入导出日志保存
    {
        public string 模块 { get; set; }
        public string 操作类型 { get; set; }
        public string 文件名称 { get; set; }
        public string 文件类型 { get; set; }
        public int 记录数 { get; set; }
        public int 成功数 { get; set; }
        public int 失败数 { get; set; }
        public string 状态 { get; set; }
        public bool 是否可撤销 { get; set; }
        public string 详情 { get; set; }
    }
}
