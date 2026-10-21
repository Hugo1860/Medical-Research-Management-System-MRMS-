using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using MyLib;
using ScientificResearch.Business;
using ScientificResearch.Infrastucture;
using ScientificResearch.Models;

namespace ScientificResearch.Controllers
{
    /// <summary>
    /// 权限管理接口
    /// </summary>
    public class PermissionController : BaseController
    {
        /// <summary>
        /// 要用到的业务
        /// </summary>
        //private PermissionBusiness MyPermissionBusiness
        //{
        //    get
        //    {
        //        return new PermissionBusiness(HttpContext);
        //    }
        //}

        /// <summary>
        /// 获取角色列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        async public Task<IEnumerable<v1_角色>> 获取角色列表() =>
            await Db.GetListSpAsync<v1_角色>();

        /// <summary>
        /// 新增角色
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 新增角色([FromBody]角色 model) =>
            await Db.ExecuteSpAsync(new sp_角色_增改() { tt_角色 = model.ToDataTable() });

        /// <summary>
        /// 修改角色
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 修改角色([FromBody]角色 model) =>
            await Db.ExecuteSpAsync(new sp_角色_增改() { tt_角色 = model.ToDataTable() });

        /// <summary>
        /// 分页获取某个角色中的人员
        /// </summary>
        /// <param name="id"></param>
        /// <param name="paging"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<PagingResult<v1_人员_带部门名>> 分页获取角色中的人员(int 角色编号, Paging paging, 人员Filter filter) =>
            await Db.GetPagingListSpAsync<v1_人员_带部门名, 人员Filter>(paging, filter, $"tfn_角色中的人员({角色编号})");

        /// <summary>
        /// 分页获取角色中没有的人员
        /// </summary>
        /// <param name="id"></param>
        /// <param name="paging"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<PagingResult<v1_人员_带部门名>> 分页获取角色中没有的人员(int 角色编号, Paging paging, 人员Filter filter) =>
            await Db.GetPagingListSpAsync<v1_人员_带部门名, 人员Filter>(paging, filter, $"tfn_角色中没有的人员({角色编号})");

        /// <summary>
        /// 保存角色中的人员,传入角色编号+属于该角色的人员编号列表;不存在于该列表的原有人员将被删除;
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async private Task 保存角色中的人员([FromBody]某角色中的人员 model) =>
            await Db.ExecuteSpAsync(new sp_角色中的人员_增改删()
            {
                角色编号 = model.角色编号,
                人员编号 = model.人员编号列表.ToPredefindedKeyFieldsList().ToDataTable()
            });

        /// <summary>
        /// 向某角色添加人员
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 添加角色人员([FromBody]某角色中的人员 model) =>
            await Db.ExecuteSpAsync(new sp_角色中的人员_增
            {
                角色编号 = model.角色编号,
                人员编号 = model.人员编号列表.ToPredefindedKeyFieldsList().ToDataTable()
            });

        /// <summary>
        /// 从某角色里面删除人员
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 删除角色人员([FromBody]某角色中的人员 model) =>
            await Db.ExecuteSpAsync(new sp_角色中的人员_删
            {
                角色编号 = model.角色编号,
                人员编号 = model.人员编号列表.ToPredefindedKeyFieldsList().ToDataTable()
            });

        /// <summary>
        /// 同时增删角色人员
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 增删角色人员([FromBody]增删角色中的人员 model)
        {
            await Db.ExecuteSpAsync(new sp_角色中的人员_增()
            {
                角色编号 = model.角色编号,
                人员编号 = model.将增加的人员编号列表.ToPredefindedKeyFieldsList().ToDataTable()
            });

            await Db.ExecuteSpAsync(new sp_角色中的人员_删()
            {
                角色编号 = model.角色编号,
                人员编号 = model.将删除的人员编号列表.ToPredefindedKeyFieldsList().ToDataTable()
            });
        }


        /// <summary>
        /// 获取所有权限,指定角色拥有的权限为true
        /// </summary>
        /// <param name="角色编号"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<IEnumerable<v1_角色的权限>> 获取角色的权限(int 角色编号) =>
            await Db.GetListSpAsync<v1_角色的权限>($"tfn_角色的权限({角色编号})");

        /// <summary>
        /// 保存角色的权限,传入角色编号+该角色拥有的权限编号列表,不存在于该列表的原有权限将被删除;
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 保存角色的权限([FromBody]某角色的权限 model) =>
            await Db.ExecuteSpAsync(new sp_角色的权限_增改删()
            {
                角色编号 = model.角色编号,
                权限编号 = model.权限编号列表.ToPredefindedKeyFieldsList().ToDataTable()
            });

        /// <summary>查询字段级授权规则，限制为科研管理角色。</summary>
        [HttpGet]
        async public Task<IEnumerable<object>> 获取字段授权规则(int? 角色编号 = null, string 资源 = null)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员"))
                throw new ForbiddenAccessException("无权查看字段授权规则");
            return await Db.QueryAsync<object>(@"
SELECT f.*, r.""名称"" AS ""角色名称""
  FROM ""字段授权规则"" f
  JOIN ""角色"" r ON r.""编号"" = f.""角色编号""
 WHERE (@角色编号 IS NULL OR f.""角色编号""=@角色编号)
   AND (@资源 IS NULL OR f.""资源""=@资源)
 ORDER BY r.""编号"", f.""资源"", f.""字段"";", new { 角色编号, 资源 = string.IsNullOrWhiteSpace(资源) ? null : 资源.Trim() });
        }

        /// <summary>保存字段级授权，并向该角色成员生成权限变更通知。</summary>
        [HttpPost]
        async public Task<object> 保存字段授权规则([FromBody]字段授权规则请求 model)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员"))
                return StatusCode(403, new { error = "无权维护字段授权规则" });
            if (model == null || model.角色编号 <= 0 || string.IsNullOrWhiteSpace(model.资源) || string.IsNullOrWhiteSpace(model.字段))
                throw new ArgumentException("角色、资源和字段不能为空");
            var resource = model.资源.Trim();
            var field = model.字段.Trim();
            if (resource.Length > 100 || field.Length > 100) throw new ArgumentException("资源或字段名称过长");
            var old = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""是否可读"" AS read_access, ""是否可写"" AS write_access
  FROM ""字段授权规则""
 WHERE ""角色编号""=@角色编号 AND ""资源""=@资源 AND ""字段""=@字段;", new { model.角色编号, 资源 = resource, 字段 = field });
            var changeType = old == null ? "新增" : "修改";
            await Db.ExecuteAsync(@"
INSERT INTO ""字段授权规则"" (""角色编号"",""资源"",""字段"",""是否可读"",""是否可写"",""建立人编号"",""更新时间"")
VALUES (@角色编号,@资源,@字段,@是否可读,@是否可写,@建立人编号,CURRENT_TIMESTAMP)
ON CONFLICT (""角色编号"",""资源"",""字段"") DO UPDATE SET ""是否可读""=EXCLUDED.""是否可读"",""是否可写""=EXCLUDED.""是否可写"",""建立人编号""=EXCLUDED.""建立人编号"",""更新时间""=CURRENT_TIMESTAMP;", new
            {
                model.角色编号, 资源 = resource, 字段 = field, model.是否可读, model.是否可写, 建立人编号 = CurrentUser.编号
            });
            var notified = await Db.ExecuteAsync(@"
INSERT INTO ""权限变更通知"" (""接收人编号"",""角色编号"",""资源"",""字段"",""变更类型"",""是否可读"",""是否可写"",""操作人编号"")
SELECT pr.""人员编号"", @角色编号, @资源, @字段, @变更类型, @是否可读, @是否可写, @操作人编号
  FROM ""人员角色"" pr
 WHERE pr.""角色编号""=@角色编号 AND pr.""人员编号""<>@操作人编号;", new
            {
                model.角色编号, 资源 = resource, 字段 = field, 变更类型 = changeType,
                model.是否可读, model.是否可写, 操作人编号 = CurrentUser.编号
            });
            return new { 角色编号 = model.角色编号, 资源 = resource, 字段 = field, 通知人数 = notified };
        }

        [HttpGet]
        async public Task<IEnumerable<object>> 获取我的权限变更通知(bool 仅未读 = true)
        {
            return await Db.QueryAsync<object>(@"
SELECT n.*, p.""姓名"" AS ""操作人姓名""
  FROM ""权限变更通知"" n
  LEFT JOIN ""人员"" p ON p.""编号""=n.""操作人编号""
 WHERE n.""接收人编号""=@人员编号 AND (@仅未读=FALSE OR n.""是否已读""=FALSE)
 ORDER BY n.""建立时间"" DESC, n.""编号"" DESC LIMIT 100;", new { 人员编号 = CurrentUser.编号, 仅未读 });
        }

        [HttpPost]
        async public Task<object> 标记权限变更通知已读([FromBody]权限变更通知已读请求 model)
        {
            var ids = (model?.编号列表 ?? Enumerable.Empty<long>()).Where(x => x > 0).Distinct().ToArray();
            if (ids.Length == 0) return new { 更新数量 = 0 };
            var count = await Db.ExecuteAsync("UPDATE \"权限变更通知\" SET \"是否已读\"=TRUE WHERE \"接收人编号\"=@人员编号 AND \"编号\"=ANY(@编号列表);", new { 人员编号 = CurrentUser.编号, 编号列表 = ids });
            return new { 更新数量 = count };
        }
    }
}
