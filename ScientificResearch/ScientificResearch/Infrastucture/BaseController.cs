using log4net;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Dapper;
using MyLib;
using ScientificResearch.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using System.Linq;
using System.Threading.Tasks;

namespace ScientificResearch.Infrastucture
{
    /// <summary>
    /// 基础控制器
    /// </summary>
    //[EnableCors("AllowAllDomain")] 没起作用
    [Produces("application/json")]
    [Consumes("application/json", "multipart/form-data")]//此处为新增
    [Route("[controller]/[action]")]
    //[Route("[area]/[controller]/[action]")] 不用area做路由,麻烦
    public class BaseController : Controller
    {
        // <summary>
        // 公共的一些业务方法
        // BaseController中好像不需要这些东西.
        // 全部都放到了BaseBusiness中去;
        // 2018-5-28 又回到了baseController. 那么以后MyPubicBusiness又没用了.
        // </summary>
        //protected Business.PubicSpBusiness MyPubicBusiness
        //{
        //    get
        //    {
        //        return new Business.PubicSpBusiness(HttpContext);
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        protected Business.WorkFlowBusiness MyWorkFlowBusiness
        {
            get
            {
                return new Business.WorkFlowBusiness(DbConnectionString);
            }
        }

        /// <summary>
        ///  配置,使用config.GetValue T来读取
        /// </summary>
        protected IConfiguration config => HttpContext.RequestServices.GetService<IConfiguration>();

        /// <summary>
        /// 日志服务
        /// </summary>
        protected ILog Log => HttpContext.RequestServices.GetService<ILog>();

        /// <summary>
        /// 原始的连接字串,具体连接哪个库还没有指定
        /// 登录时可以用这个临时生成一个连接;
        /// </summary>
        public string DbConnectionStringLack
        {
            get
            {
                var key = Env.EnvironmentName == Environments.Development
                    ? "connectionString:ScientificResearch_Development"
                    : "connectionString:ScientificResearch_Release";
                return config.GetValue<string>(key);
            }
        }

        /// <summary>
        /// 指定连接的库为当前登录人的库,字串可用于事务,也可用于下面的db
        /// </summary>
        public string DbConnectionString => BuildDbConnectionString(CurrentUser.DbKey);

        /// <summary>
        /// Builds a connection string only for a validated database key.
        /// The key is an identifier, never a free-form connection-string fragment.
        /// </summary>
        protected string BuildDbConnectionString(string dbKey)
        {
            return HttpContext.RequestServices
                .GetRequiredService<IDatabaseConnectionFactory>()
                .Create(dbKey, Env.EnvironmentName);
        }


        /// <summary>
        /// 获取当前登录者的数据库链接;可能在login方法中另做一个
        /// </summary>
        public IDbConnection Db
        {
            get
            {
                return new NpgsqlConnection(DbConnectionString);
            }
        }

        /// <summary>按项目负责人、参与人、流程发起人或科研管理角色判断项目访问权。</summary>
        protected async Task<bool> CanAccessProjectAsync(int flowTemplateId, int projectId)
        {
            var reimbursementTemplateId = config.GetValue<int>("流程模板编号:报销");
            var verticalProjectTemplateId = config.GetValue<int>("流程模板编号:纵向项目立项");
            var horizontalProjectTemplateId = config.GetValue<int>("流程模板编号:横向项目申请");
            var meetingTemplateId = config.GetValue<int>("流程模板编号:参加会议");
            return await Db.QuerySingleAsync<bool>(
                @"
WITH scope AS (
    SELECT
        @流程模板编号::integer AS original_template_id,
        @项目编号::integer AS original_project_id,
        CASE
            WHEN @流程模板编号 = @报销流程模板编号
             AND r.""编号"" IS NOT NULL
             AND EXISTS (SELECT 1 FROM ""纵向项目立项"" y WHERE y.""编号"" = r.""项目编号"")
                THEN @纵向项目立项流程模板编号
            WHEN @流程模板编号 = @报销流程模板编号
             AND r.""编号"" IS NOT NULL
             AND EXISTS (SELECT 1 FROM ""横向项目"" x WHERE x.""编号"" = r.""项目编号"")
                THEN @横向项目申请流程模板编号
            WHEN @流程模板编号 = @报销流程模板编号
             AND r.""编号"" IS NOT NULL
             AND EXISTS (SELECT 1 FROM ""参加会议"" m WHERE m.""编号"" = r.""项目编号"")
                THEN @参加会议流程模板编号
            ELSE @流程模板编号
        END AS resolved_template_id,
        CASE
            WHEN @流程模板编号 = @报销流程模板编号 AND r.""编号"" IS NOT NULL
                THEN r.""项目编号""
            ELSE @项目编号
        END AS resolved_project_id
    FROM (SELECT 1) seed
    LEFT JOIN ""报销"" r ON r.""编号"" = @项目编号
), project_access AS (
    SELECT s.*,
           ""fn_人员可访问项目""(@人员编号, s.resolved_template_id, s.resolved_project_id) AS resolved_allowed,
           ""fn_人员可访问项目""(@人员编号, s.original_template_id, s.original_project_id) AS original_allowed
      FROM scope s
)
SELECT p.resolved_allowed OR p.original_allowed
   OR EXISTS (
        SELECT 1
          FROM project_access p
          JOIN ""流程"" f ON f.""流程模板编号"" = p.resolved_template_id
                            AND f.""项目编号"" = p.resolved_project_id
          JOIN ""步骤"" s ON s.""流程编号"" = f.""编号"" AND s.""状态"" = 0
         WHERE EXISTS (
                    SELECT 1 FROM ""步骤指定人"" direct
                    WHERE direct.""步骤编号"" = s.""编号""
                      AND direct.""指定人编号"" = @人员编号
                )
                OR EXISTS (
                    SELECT 1
                      FROM ""步骤角色"" sr
                      JOIN ""人员角色"" pr ON pr.""角色编号"" = sr.""角色编号""
                     WHERE sr.""步骤模板编号"" = s.""步骤模板编号""
                       AND pr.""人员编号"" = @人员编号
                )
   )
   OR EXISTS (
        SELECT 1
          FROM project_access p
          JOIN ""流程"" f ON f.""流程模板编号"" = p.original_template_id
                            AND f.""项目编号"" = p.original_project_id
          JOIN ""步骤"" s ON s.""流程编号"" = f.""编号"" AND s.""状态"" = 0
         WHERE EXISTS (
                    SELECT 1 FROM ""步骤指定人"" direct
                    WHERE direct.""步骤编号"" = s.""编号""
                      AND direct.""指定人编号"" = @人员编号
                )
                OR EXISTS (
                    SELECT 1
                      FROM ""步骤角色"" sr
                      JOIN ""人员角色"" pr ON pr.""角色编号"" = sr.""角色编号""
                     WHERE sr.""步骤模板编号"" = s.""步骤模板编号""
                       AND pr.""人员编号"" = @人员编号
                )
   )
FROM project_access p;",
                new
                {
                    人员编号 = CurrentUser.编号,
                    流程模板编号 = flowTemplateId,
                    项目编号 = projectId,
                    报销流程模板编号 = reimbursementTemplateId,
                    纵向项目立项流程模板编号 = verticalProjectTemplateId,
                    横向项目申请流程模板编号 = horizontalProjectTemplateId,
                    参加会议流程模板编号 = meetingTemplateId
                });
        }

        protected IActionResult ProjectAccessDenied() =>
            StatusCode(StatusCodes.Status403Forbidden, new { error = "无权访问该项目" });

        protected async Task<int> ResolveOperationUserAsync(string scope)
        {
            var rawGrantor = Request.Headers["X-Proxy-Grantor-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(rawGrantor)) return CurrentUser.编号;
            if (!int.TryParse(rawGrantor, out var grantorId) || grantorId <= 0 || grantorId == CurrentUser.编号)
                throw new ForbiddenAccessException("X-Proxy-Grantor-Id 无效");
            var valid = await Db.QuerySingleAsync<bool>(
                "SELECT \"fn_代理有效\"(@授权人编号,@被代理人编号,@代理范围);",
                new { 授权人编号 = grantorId, 被代理人编号 = CurrentUser.编号, 代理范围 = scope });
            if (!valid) throw new ForbiddenAccessException("代理已撤销、已过期或范围不匹配");
            HttpContext.Items["代理授权人编号"] = grantorId;
            return grantorId;
        }

        protected async Task<bool> HasAnyRoleAsync(params string[] roleNames)
        {
            return await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (
    SELECT 1 FROM ""人员角色"" pr JOIN ""角色"" r ON r.""编号"" = pr.""角色编号""
    WHERE pr.""人员编号"" = @人员编号 AND r.""名称"" = ANY(@角色名称)
);", new { 人员编号 = CurrentUser.编号, 角色名称 = roleNames });
        }

        /// <summary>按角色和字段检查读取/写入授权；系统管理员和科研管理员拥有默认管理权限。</summary>
        protected async Task<bool> HasFieldPermissionAsync(string resource, string field, bool write = false)
        {
            if (await HasAnyRoleAsync("系统管理员", "科研管理员")) return true;
            return await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (
    SELECT 1
      FROM ""字段授权规则"" f
      JOIN ""人员角色"" pr ON pr.""角色编号"" = f.""角色编号""
     WHERE pr.""人员编号"" = @人员编号
       AND f.""资源"" = @资源
       AND f.""字段"" = @字段
       AND CASE WHEN @写入 THEN f.""是否可写"" ELSE f.""是否可读"" END
);", new { 人员编号 = CurrentUser.编号, 资源 = resource, 字段 = field, 写入 = write });
        }

        /// <summary>
        /// 安全提取日期，兼容 PostgreSQL date 暴露为 DateOnly 以及普通 DateTime / 字符串。
        /// </summary>
        protected static DateTime? SafeToDateTime(object value)
        {
            if (value == null || value is DBNull) return null;
            if (value is DateTime dt) return dt;
            if (value is DateOnly d) return d.ToDateTime(TimeOnly.MinValue);
            if (DateTime.TryParse(Convert.ToString(value), out var parsed)) return parsed;
            return null;
        }

        /// <summary>
        /// 安全转换为整型，避免 dynamic 装箱及不同整型之间的类型转换异常。
        /// </summary>
        protected static int SafeToInt32(object value, int defaultValue = 0)
        {
            if (value == null || value is DBNull) return defaultValue;
            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        ///// <summary>
        ///// redis服务的db对象 
        ///// </summary>
        ////protected IConnectionMultiplexer redis => HttpContext.RequestServices.GetService<IConnectionMultiplexer>();
        //protected IDatabase RedisDb => HttpContext.RequestServices.GetService<IConnectionMultiplexer>().GetDatabase();

        /// <summary>
        /// 网站服务器环境
        /// </summary>
        protected IWebHostEnvironment Env => HttpContext.RequestServices.GetService<IWebHostEnvironment>();

        /// <summary>
        /// 登录人的信息
        /// </summary>
        protected CurrentUser CurrentUser
        {
            get
            {
                var hasKeycloakUser = HttpContext.Items.TryGetValue(KeycloakUserMiddleware.CurrentUserItemKey, out var itemUser);
                var user = (hasKeycloakUser ? itemUser as CurrentUser : null)
                    ?? HttpContext.Session.Get<CurrentUser>("user");
                if (user == null)
                {
                    throw new UnauthorizedAccessException("请先登录");
                }

                return user;
            }
        }
    }
}

