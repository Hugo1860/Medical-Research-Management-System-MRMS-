using System;
using System.Collections.Generic;
using Npgsql;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MyLib;
using ScientificResearch.Business;
using ScientificResearch.Infrastucture;
using ScientificResearch.Models;

namespace ScientificResearch.Controllers
{
    /// <summary>
    /// 登录|注销|获取权限等相关接口
    /// </summary>
    public class AccessController : BaseController
    {
        /// <summary>
        /// 要用到的业务
        /// </summary>
        //private AccessBusiness MyAccessBusiness
        //{
        //    get
        //    {
        //        return new AccessBusiness(HttpContext);
        //    }
        //}

        /// <summary>返回前端发起 OIDC/PKCE 登录所需的公开配置，不返回客户端密钥。</summary>
        [AllowAnonymous]
        [HttpGet]
        public object OidcConfig()
        {
            var enabled = config.GetValue<bool>("Keycloak:Enabled");
            var authority = (config.GetValue<string>("Keycloak:Authority") ?? "").TrimEnd('/');
            var clientId = config.GetValue<string>("Keycloak:ClientId") ?? "";
            var redirectUri = config.GetValue<string>("Keycloak:RedirectUri");
            if (string.IsNullOrWhiteSpace(redirectUri))
            {
                redirectUri = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/IMIS/views/login.html";
            }

            return new
            {
                Enabled = enabled,
                Authority = authority,
                ClientId = clientId,
                RedirectUri = redirectUri
            };
        }

        /// <summary>
        /// 登录,返回登录人所具有的权限
        /// </summary>
        /// <param name="model">其中dbKey请先填"ScientificResearch_Model"</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        async public Task<object> Login([FromBody]LoginInfo model)
        {
            if (config.GetValue<bool>("Keycloak:Enabled"))
            {
                return new BadRequestObjectResult(new
                {
                    error = "系统已启用 Keycloak，请先完成 Keycloak 登录，再调用 GET /Access/Me"
                });
            }

            // Forwarded headers are normalized by Startup only for configured trusted proxies;
            // RemoteIpAddress therefore carries the client address without trusting user input.
            var IP = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (model == null || string.IsNullOrWhiteSpace(model.DbKey))
            {
                return new BadRequestObjectResult(new { error = "数据库标识不能为空" });
            }

            using var dbWhenLogin = new NpgsqlConnection(BuildDbConnectionString(model.DbKey));
            var result = await dbWhenLogin.QueryMultipleAsync(@"
SELECT p.编号, p.工号, p.姓名, p.部门编号, d.名称 AS 部门名称,
       string_agg(DISTINCT r.名称, '、' ORDER BY r.名称) AS 角色名称
FROM 人员 p
LEFT JOIN 部门 d ON d.编号 = p.部门编号
LEFT JOIN 人员角色 pr ON pr.人员编号 = p.编号
LEFT JOIN 角色 r ON r.编号 = pr.角色编号
WHERE p.工号 = @工号 AND p.密码 = @密码 AND p.是否启用 = TRUE
GROUP BY p.编号, p.工号, p.姓名, p.部门编号, d.名称;
SELECT DISTINCT a.*
FROM 权限 a
JOIN 角色权限 b ON b.权限编号 = a.编号
JOIN 人员角色 c ON c.角色编号 = b.角色编号
JOIN 人员 p ON p.编号 = c.人员编号
WHERE p.工号 = @工号;", new { model.工号, model.密码 });

            var user = result.Read<CurrentUser>().FirstOrDefault();
            if (user == null)
            {
                return new UnauthorizedObjectResult(new { error = "工号或密码错误" });
            }

            user.DbKey = model.DbKey;
            var permission = result.Read<权限>();

            //处理一下中文
            foreach (var item in permission)
            {
                item.路径 = string.Join("?", (item.路径 ?? "").Split("?").Select(j => Uri.EscapeUriString(j)));
            }


            var rPermission = recursivePermission(permission, 0);

            HttpContext.Session.Set<CurrentUser>("user", user);

            await Db.ExecuteSpAsync(
                new sp_登录日志_增()
                {
                    tt_登录日志 = new 登录日志()
                    {
                        工号 = user.工号,
                        姓名 = user.姓名,
                        IP = IP
                    }.ToDataTable()
                });

            return new { 人员 = user, 权限 = rPermission };
        }

        /// <summary>返回当前 Keycloak 身份对应的本地人员和业务权限。</summary>
        [HttpGet]
        public async Task<object> Me()
        {
            var permission = await Db.QueryAsync<权限>(@"
SELECT DISTINCT a.*
FROM 权限 a
JOIN 角色权限 b ON b.权限编号 = a.编号
JOIN 人员角色 c ON c.角色编号 = b.角色编号
WHERE c.人员编号 = @人员编号;", new { 人员编号 = CurrentUser.编号 });

            foreach (var item in permission)
            {
                item.路径 = string.Join("?", (item.路径 ?? "").Split("?").Select(Uri.EscapeUriString));
            }

            return new { 人员 = CurrentUser, 权限 = recursivePermission(permission, 0) };
        }

        [HttpPost]
        public async Task<object> 授权代理([FromBody]权限代理申请 model)
        {
            if (model == null || model.被代理人编号 <= 0 || model.被代理人编号 == CurrentUser.编号)
                throw new ArgumentException("被代理人必须是其他有效人员");
            var scope = (model.代理范围 ?? string.Empty).Trim();
            var allowedScopes = config.GetSection("权限代理范围").Get<string[]>() ?? new[] { "项目审批" };
            if (string.IsNullOrWhiteSpace(scope) || !allowedScopes.Any(item =>
                    string.Equals(item?.Trim(), scope, StringComparison.Ordinal)))
                throw new ArgumentException("代理范围未在系统白名单中，请选择已启用的业务范围");
            var start = model.开始时间 ?? DateTime.Now;
            var end = model.结束时间 ?? start.AddDays(1);
            var id = await Db.QuerySingleAsync<long>(
                "SELECT \"sp_权限代理_增\"(@授权人编号,@被代理人编号,CAST(@代理范围 AS varchar),CAST(@开始时间 AS timestamp without time zone),CAST(@结束时间 AS timestamp without time zone),CAST(@备注 AS varchar));",
                new { 授权人编号 = CurrentUser.编号, 被代理人编号 = model.被代理人编号,
                    代理范围 = scope, 开始时间 = start, 结束时间 = end, 备注 = model.备注 });
            return new { 编号 = id, 开始时间 = start, 结束时间 = end };
        }

        /// <summary>返回当前部署实际启用的代理范围，前端不得自行拼接未登记范围。</summary>
        [HttpGet]
        public object 获取代理范围()
        {
            var scopes = config.GetSection("权限代理范围").Get<string[]>() ?? new[] { "项目审批" };
            return scopes.Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim()).Distinct(StringComparer.Ordinal).ToArray();
        }

        [HttpPost]
        public async Task<object> 撤销代理([FromBody]权限代理撤销 model)
        {
            if (model == null || model.编号 <= 0) throw new ArgumentException("代理编号无效");
            var changed = await Db.QuerySingleAsync<int>(
                "SELECT \"sp_权限代理_撤销\"(@代理编号,@处理人编号,@原因);",
                new { 代理编号 = model.编号, 处理人编号 = CurrentUser.编号, 原因 = model.原因 });
            return new { 编号 = model.编号, 已撤销 = changed == 1 };
        }

        /// <summary>系统管理角色在离岗或账号停用时批量回收某人的代理授权。</summary>
        [HttpPost]
        public async Task<object> 回收人员代理([FromBody]权限代理回收 model)
        {
            if (model == null || model.人员编号 <= 0) throw new ArgumentException("人员编号无效");
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办", "科研科负责人", "科研处处长"))
                return StatusCode(403, new { error = "无权回收其他人员的代理权限" });
            var changed = await Db.QuerySingleAsync<int>(
                "SELECT \"sp_权限代理_回收人员\"(@人员编号,@原因,@回收人编号);",
                new { model.人员编号, 原因 = string.IsNullOrWhiteSpace(model.原因) ? "管理员手工回收代理权限" : model.原因, 回收人编号 = CurrentUser.编号 });
            return new { 人员编号 = model.人员编号, 回收数量 = changed };
        }

        [HttpGet]
        public async Task<IEnumerable<object>> 获取我的代理()
        {
            return await Db.QueryAsync<object>(@"
SELECT d.*, g.姓名 AS 授权人姓名, p.姓名 AS 被代理人姓名,
       revoker.姓名 AS 撤销人姓名
FROM ""权限代理"" d
LEFT JOIN ""人员"" g ON g.""编号"" = d.""授权人编号""
LEFT JOIN ""人员"" p ON p.""编号"" = d.""被代理人编号""
LEFT JOIN ""人员"" revoker ON revoker.""编号"" = d.""撤销人编号""
WHERE d.""授权人编号"" = @人员编号 OR d.""被代理人编号"" = @人员编号
ORDER BY d.""建立时间"" DESC;", new { 人员编号 = CurrentUser.编号 });
        }

        /// <summary>
        /// 权限分层次
        /// </summary>
        /// <param name="permission"></param>
        /// <param name="topId"></param>
        /// <returns></returns>
        private object recursivePermission(IEnumerable<权限> permission, int topId)
        {
            return from item in permission
                   where item.上级编号 == topId
                   select new
                   {
                       菜单 = item,
                       子级菜单 = recursivePermission(permission, item.编号)
                   };
        }

        /// <summary>
        /// 注销当前登录
        /// </summary>
        [HttpPost]
        public void Logout() =>HttpContext.Session.Clear();

        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task ChangePassword([FromBody]ChangePassword model)
        {
            if (config.GetValue<bool>("Keycloak:Enabled"))
            {
                Response.StatusCode = 400;
                await Response.WriteAsJsonAsync(new { error = "密码由 Keycloak 统一管理，请在 Keycloak 账户中心修改" });
                return;
            }

            await Db.ExecuteSpAsync(new sp_人员_修改密码()
            {
                工号 = CurrentUser.工号,
                旧密码 = model.旧密码,
                新密码 = model.新密码
            });
        }
            //await MyAccessBusiness.ChangePassword(model);

        /// <summary>
        /// 分页获取登录日志
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<PagingResult<登录日志>> 获取登录日志(Paging paging, 登录日志Filter filter) =>
            await Db.GetPagingListSpAsync<登录日志,登录日志Filter>(paging, filter);

    }
}
