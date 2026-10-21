using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using ScientificResearch.Infrastucture;

namespace ScientificResearch.Controllers
{
    /// <summary>角色化首页组件、业务催办和配置审计。统计值始终来自现有统计接口。</summary>
    public sealed class DashboardController : BaseController
    {
        private async Task<bool> IsManager() => await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办");
        private static string NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        [HttpGet]
        public async Task<object> 获取首页组件配置()
        {
            var roleNames = (CurrentUser.角色名称 ?? string.Empty).Split(new[] { '、', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return await Db.QueryAsync<object>(@"
SELECT ""编号"",""角色名称"",""组件编码"",""组件标题"",""组件类型"",""排序号"",""配置""
  FROM ""首页组件配置""
 WHERE ""是否启用""=TRUE AND (""角色名称""='默认' OR ""角色名称"" = ANY(@角色名称))
 ORDER BY CASE WHEN ""角色名称""='默认' THEN 1 ELSE 0 END, ""排序号"",""编号"";", new { 角色名称 = roleNames });
        }

        [HttpPost]
        public async Task<object> 保存首页组件配置([FromBody] 首页组件配置保存 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权维护首页组件配置" });
            if (model == null || string.IsNullOrWhiteSpace(model.组件编码) || string.IsNullOrWhiteSpace(model.组件标题)) throw new ArgumentException("组件编码和标题不能为空");
            if (model.组件编码.Length > 80 || model.组件标题.Length > 200) throw new ArgumentException("组件字段超出限制");
            var role = string.IsNullOrWhiteSpace(model.角色名称) ? "默认" : model.角色名称.Trim();
            var type = string.IsNullOrWhiteSpace(model.组件类型) ? "指标卡片" : model.组件类型.Trim();
            if (!new[] { "指标卡片", "待办列表", "统计图", "快捷入口", "通知列表" }.Contains(type)) throw new ArgumentException("组件类型无效");
            if (model.排序号 < 0) throw new ArgumentException("排序号不能为负数");
            if (model.编号.HasValue)
            {
                var changed = await Db.ExecuteAsync(@"
UPDATE ""首页组件配置"" SET ""角色名称""=@角色名称,""组件编码""=@组件编码,""组件标题""=@组件标题,""组件类型""=@组件类型,""排序号""=@排序号,""是否启用""=@是否启用,""配置""=@配置::jsonb,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号;", new { model.编号, 角色名称 = role, model.组件编码, model.组件标题, 组件类型 = type, model.排序号, model.是否启用, 配置 = model.配置 ?? "{}" });
                if (changed == 0) return NotFound(new { error = "首页组件不存在" });
                await 写审计("首页组件", model.编号.Value, "更新", model.配置);
                return new { 编号 = model.编号.Value };
            }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""首页组件配置"" (""角色名称"",""组件编码"",""组件标题"",""组件类型"",""排序号"",""是否启用"",""配置"",""创建人编号"")
VALUES (@角色名称,@组件编码,@组件标题,@组件类型,@排序号,@是否启用,@配置::jsonb,@创建人编号) RETURNING ""编号"";", new { 角色名称 = role, model.组件编码, model.组件标题, 组件类型 = type, model.排序号, model.是否启用, 配置 = model.配置 ?? "{}", 创建人编号 = CurrentUser.编号 });
            await 写审计("首页组件", id, "新增", model.配置);
            return new { 编号 = id };
        }

        [HttpGet]
        public async Task<object> 查询业务催办(string 状态 = null, int? 数量 = null)
        {
            var limit = Math.Clamp(数量 ?? 100, 1, 500);
            var manager = await IsManager();
            return await Db.QueryAsync<object>(@"
SELECT c.""编号"",c.""目标人员编号"",c.""目标角色名称"",c.""业务类型"",c.""业务编号"",c.""标题"",c.""催办说明"",c.""截止时间"",c.""状态"",c.""发起人编号"",c.""处理人编号"",c.""处理说明"",c.""处理时间"",c.""最后提醒时间"",c.""创建时间""
  FROM ""业务催办"" c
 WHERE (@管理者 OR c.""目标人员编号""=@人员编号 OR c.""目标角色名称"" = ANY(@角色名称))
   AND (@状态 IS NULL OR c.""状态""=@状态)
 ORDER BY CASE WHEN c.""状态""='待处理' THEN 0 ELSE 1 END,c.""截止时间"" NULLS LAST,c.""创建时间"" DESC
 LIMIT @数量;", new { 管理者 = manager, 人员编号 = CurrentUser.编号, 角色名称 = (CurrentUser.角色名称 ?? string.Empty).Split(new[] { '、', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), 状态 = NullIfEmpty(状态), 数量 = limit });
        }

        [HttpPost]
        public async Task<object> 发起业务催办([FromBody] 业务催办保存 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权发起业务催办" });
            if (model == null || string.IsNullOrWhiteSpace(model.标题) || string.IsNullOrWhiteSpace(model.业务类型)) throw new ArgumentException("业务类型和标题不能为空");
            if (model.目标人员编号 is null && string.IsNullOrWhiteSpace(model.目标角色名称)) throw new ArgumentException("必须指定目标人员或角色");
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""业务催办"" (""目标人员编号"",""目标角色名称"",""业务类型"",""业务编号"",""标题"",""催办说明"",""截止时间"",""发起人编号"")
VALUES (@目标人员编号,@目标角色名称,@业务类型,@业务编号,@标题,@催办说明,@截止时间,@发起人编号) RETURNING ""编号"";", new { model.目标人员编号, 目标角色名称 = NullIfEmpty(model.目标角色名称), model.业务类型, model.业务编号, model.标题, model.催办说明, model.截止时间, 发起人编号 = CurrentUser.编号 });
            return new { 编号 = id, 状态 = "待处理" };
        }

        [HttpPost]
        public async Task<object> 处理业务催办([FromBody] 业务催办处理 model)
        {
            if (model == null || model.编号 <= 0) throw new ArgumentException("催办编号无效");
            var manager = await IsManager();
            var allowed = await Db.QuerySingleAsync<bool>(@"SELECT @管理者 OR EXISTS (SELECT 1 FROM ""业务催办"" WHERE ""编号""=@编号 AND (""目标人员编号""=@人员编号 OR ""目标人员编号"" IS NULL AND ""目标角色名称"" = ANY(@角色名称)));", new { 管理者 = manager, model.编号, 人员编号 = CurrentUser.编号, 角色名称 = (CurrentUser.角色名称 ?? string.Empty).Split(new[] { '、', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) });
            if (!allowed) return StatusCode(403, new { error = "无权处理该催办" });
            var status = string.IsNullOrWhiteSpace(model.状态) ? "已处理" : model.状态.Trim();
            if (status is not ("已读" or "已处理" or "已取消")) throw new ArgumentException("催办状态无效");
            var changed = await Db.ExecuteAsync(@"UPDATE ""业务催办"" SET ""状态""=@状态,""处理人编号""=CASE WHEN @状态 IN ('已处理','已取消') THEN @人员编号 ELSE ""处理人编号"" END,""处理说明""=@处理说明,""处理时间""=CASE WHEN @状态 IN ('已处理','已取消') THEN CURRENT_TIMESTAMP ELSE ""处理时间"" END,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号;", new { model.编号, 状态 = status, 人员编号 = CurrentUser.编号, 处理说明 = NullIfEmpty(model.处理说明) });
            if (changed == 0) return NotFound(new { error = "催办记录不存在" });
            return new { model.编号, 状态 = status };
        }

        private Task 写审计(string type, long id, string action, string payload) => Db.ExecuteAsync(@"INSERT INTO ""首页配置审计"" (""对象类型"",""对象编号"",""操作类型"",""操作人编号"",""变更后"") VALUES (@type,@id,@action,@user,@payload::jsonb);", new { type, id, action, user = CurrentUser.编号, payload = payload ?? "{}" });
    }

    public sealed class 首页组件配置保存
    {
        public long? 编号 { get; set; }
        public string 角色名称 { get; set; }
        public string 组件编码 { get; set; }
        public string 组件标题 { get; set; }
        public string 组件类型 { get; set; }
        public int 排序号 { get; set; }
        public bool 是否启用 { get; set; } = true;
        public string 配置 { get; set; }
    }

    public sealed class 业务催办保存
    {
        public int? 目标人员编号 { get; set; }
        public string 目标角色名称 { get; set; }
        public string 业务类型 { get; set; }
        public long? 业务编号 { get; set; }
        public string 标题 { get; set; }
        public string 催办说明 { get; set; }
        public DateTime? 截止时间 { get; set; }
    }

    public sealed class 业务催办处理
    {
        public long 编号 { get; set; }
        public string 状态 { get; set; }
        public string 处理说明 { get; set; }
    }
}
