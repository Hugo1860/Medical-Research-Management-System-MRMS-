using log4net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

using ScientificResearch.Models;
using MyLib;
using Npgsql;

namespace ScientificResearch.Infrastucture
{
    /// <summary>
    /// Business基类,用来将获取的当前请求的IServiceProvider对应的服务容器中的需要的服务
    /// </summary>
    public class BaseBusiness
    {
        /// <summary>
        /// serviceProvider
        /// </summary>
        public IServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// Session
        /// </summary>
        public ISession Session { get; private set; }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="httpContext"></param>
        public BaseBusiness(HttpContext httpContext)
        {
            ServiceProvider = httpContext.RequestServices;
            Session = httpContext.Session;
        }

        /// <summary>
        /// 配置
        /// </summary>
        protected IConfiguration config => ServiceProvider.GetService<IConfiguration>();

        /// <summary>
        /// 日志
        /// </summary>
        protected ILog Log => ServiceProvider.GetService<ILog>();

        protected IDatabaseConnectionFactory DatabaseConnectionFactory =>
            ServiceProvider.GetRequiredService<IDatabaseConnectionFactory>();

        /// <summary>
        /// 用于事务
        /// </summary>
        public string DbConnectionString
        {
            get
            {
                return DatabaseConnectionFactory.Create(currentUser.DbKey, Env.EnvironmentName);
            }
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

        //protected IDbConnection Db => ServiceProvider.GetService<IDbConnection>();

        ///// <summary>
        ///// sql数据库服务的连接对象
        ///// </summary>
        //protected IDbConnection Db => ServiceProvider.GetService<IDictionary<string, IDbConnection>>()["account"];

        ///// <summary>
        ///// 101sql数据库服务的连接对象
        ///// </summary>
        //protected IDbConnection Db101 => ServiceProvider.GetService<IDictionary<string, IDbConnection>>()["mnfTaskSystemDB"];

        ///// <summary>
        ///// redis服务器的db对象
        ///// </summary>
        ////protected IConnectionMultiplexer redis => _serviceProvider.GetService<IConnectionMultiplexer>();
        //protected IDatabase RedisDb => ServiceProvider.GetService<IConnectionMultiplexer>().GetDatabase();

        /// <summary>
        /// 网站服务器环境
        /// </summary>
        protected IWebHostEnvironment Env => ServiceProvider.GetService<IWebHostEnvironment>();

        /// <summary>
        /// 登录人的信息
        /// </summary>
        protected CurrentUser currentUser
        {
            get
            {
                var httpContext = ServiceProvider.GetService<IHttpContextAccessor>()?.HttpContext;
                object itemUser = null;
                var hasKeycloakUser = httpContext?.Items.TryGetValue(KeycloakUserMiddleware.CurrentUserItemKey, out itemUser) == true;
                var user = (hasKeycloakUser ? itemUser as CurrentUser : null)
                    ?? Session.Get<CurrentUser>("user");
                if (user == null)
                {
                    throw new UnauthorizedAccessException("请先登录");
                }

                return user;
            }
        }

    }
}
