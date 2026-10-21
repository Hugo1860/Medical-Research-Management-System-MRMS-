using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using Dapper;

using ScientificResearch.Infrastucture;
using ScientificResearch.Models;
using MyLib;
using Npgsql;

namespace ScientificResearch.Business
{
    /// <summary>
    /// 登录|注销|获取个人权限|个人信息等
    /// </summary>
    public class AccessBusiness : BaseBusiness
    {
        #region 构造
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="httpContext"></param>
        public AccessBusiness(HttpContext httpContext) : base(httpContext) { }
        #endregion

        /// <summary>
        /// login
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        async public Task<object> Login(LoginInfo model, string IP)
        {
            //var where = SqlWhereMapper.toWhere(new LoginInfoFilter() { 工号 = model.工号 });
            //TODO:先写死.
            //model.DbKey = "ScientificResearch_Model";

            using var dbWhenLogin = new NpgsqlConnection(
                DatabaseConnectionFactory.Create(model.DbKey, Env.EnvironmentName));
            var result = await dbWhenLogin.QueryMultipleAsync(@"
SELECT p.编号, p.工号, p.姓名, p.部门编号, d.名称 AS 部门名称
FROM 人员 p
LEFT JOIN 部门 d ON d.编号 = p.部门编号
WHERE p.工号 = @工号 AND p.密码 = @密码 AND p.是否启用 = TRUE;
SELECT DISTINCT a.*
FROM 权限 a
JOIN 角色权限 b ON b.权限编号 = a.编号
JOIN 人员角色 c ON c.角色编号 = b.角色编号
JOIN 人员 p ON p.编号 = c.人员编号
WHERE p.工号 = @工号;", new { model.工号, model.密码 });

            var user = result.Read<CurrentUser>().FirstOrDefault();
            if (user == null)
            {
                throw new UnauthorizedAccessException("工号或密码错误");
            }

            user.DbKey = model.DbKey;
            var permission = result.Read<权限>();

            foreach (var item in permission)
            {
                item.路径 = string.Join("?", (item.路径 ?? "").Split("?").Select(j => Uri.EscapeUriString(j)));
            }


            var rPermission = recursivePermission(permission, 0);

            Session.Set<CurrentUser>("user", user);

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
        /// logout
        /// </summary>
        /// <returns></returns>
        public void Logout()
        {
            Session.Clear();
        }

    }
}
