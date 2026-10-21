//using System;
//using System.Data;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//using Microsoft.AspNetCore.Http;

//using Dapper;

//using ScientificResearch.Infrastucture;
//using ScientificResearch.Models;
//using MyLib;
//using Npgsql;
//using Microsoft.Extensions.Configuration;

//namespace ScientificResearch.Business
//{
//    /// <summary>
//    /// 权限管理业务
//    /// </summary>
//    public class PermissionBusiness : BaseBusiness
//    {
//        #region 构造
//        /// <summary>
//        /// 构造
//        /// </summary>
//        /// <param name="httpContext"></param>
//        public PermissionBusiness(HttpContext httpContext) : base(httpContext) { }
//        #endregion

//        /// <summary>
//        /// 获取角色列表;
//        /// </summary>
//        /// <returns></returns>
//        async public Task<IEnumerable<v1_角色>> Get角色列表()
//        {
//            var result = await Db.QueryAsync<v1_角色>("sp_GetList", new
//            {
//                tbName = $"v1_角色",
//                OrderType = true,
//            }, commandType: CommandType.StoredProcedure);

//            return result;
//        }

//        /// <summary>
//        /// 增改角色
//        /// </summary>
//        /// <param name="model"></param>
//        /// <returns></returns>
//        async public Task 增改角色(角色 model)
//        {
//            await Db.ExecuteAsync("sp_角色_增改", new
//            {
//                tt_角色 = model.ToDataTable()
//            }, commandType: CommandType.StoredProcedure);
//        }

//        /// <summary>
//        /// 获取角色中的人员
//        /// </summary>
//        /// <param name="角色编号"></param>
//        /// <returns></returns>
//        async public Task<object> 获取角色中的人员(int 角色编号)
//        {
//            var result = await Db.QueryMultipleAsync("sp_角色中的人员_获取", new { 角色编号 }, commandType: CommandType.StoredProcedure);
//            var 角色中的人员 = result.Read<角色中的人员>();
//            var 角色中没有的人员 = result.Read<角色中的人员>();
//            return new { 角色中的人员, 角色中没有的人员 };
//        }

//        /// <summary>
//        /// 保存角色中的人员
//        /// </summary>
//        /// <param name="model"></param>
//        /// <returns></returns>
//        async public Task 保存角色中的人员(某角色中的人员 model)
//        {
//            await Db.ExecuteSpAsync(new sp_角色中的人员_增改删()
//            {
//                角色编号 = model.角色编号,
//                人员编号 = model.人员编号列表.ToPredefindedKeyFieldsList().ToDataTable()
//            });
//        }

//        /// <summary>
//        /// 往某角色里面添加人员
//        /// </summary>
//        /// <param name="model"></param>
//        /// <returns></returns>
//        async public Task 添加角色人员(某角色中的人员 model)
//        {
//            await Db.ExecuteSpAsync(new sp_角色中的人员_增
//            {
//                角色编号 = model.角色编号,
//                人员编号 = model.人员编号列表.ToPredefindedKeyFieldsList().ToDataTable()
//            });
//        }

//        /// <summary>
//        /// 从某角色里面删除人员
//        /// </summary>
//        /// <param name="model"></param>
//        /// <returns></returns>
//        async public Task 删除角色人员(某角色中的人员 model)
//        {
//            await Db.ExecuteSpAsync(new sp_角色中的人员_删
//            {
//                角色编号 = model.角色编号,
//                人员编号 = model.人员编号列表.ToPredefindedKeyFieldsList().ToDataTable()
//            });
//        }

//        /// <summary>
//        /// 增删角色人员
//        /// </summary>
//        /// <param name="model"></param>
//        /// <returns></returns>
//        async public Task 增删角色人员(增删角色中的人员 model)
//        {
//            await Db.ExecuteSpAsync(new sp_角色中的人员_增()
//            {
//                角色编号 = model.角色编号,
//                人员编号 = model.将增加的人员编号列表.ToPredefindedKeyFieldsList().ToDataTable()
//            });

//            await Db.ExecuteSpAsync(new sp_角色中的人员_删()
//            {
//                角色编号 = model.角色编号,
//                人员编号 = model.将删除的人员编号列表.ToPredefindedKeyFieldsList().ToDataTable()
//            });

//        }

//        /// <summary>
//        /// 获取角色的权限
//        /// </summary>
//        /// <param name="角色编号"></param>
//        /// <returns></returns>
//        async public Task<IEnumerable<v1_角色的权限>> 获取角色的权限(int 角色编号)
//        {
//            var result = await Db.QueryAsync<v1_角色的权限>(
//                "sp_GetList", new
//                {
//                    tbName = $"tfn_角色的权限({角色编号})",
//                    OrderType = true,
//                }, commandType: CommandType.StoredProcedure
//                );
//            return result;
//        }

//        /// <summary>
//        /// 保存角色的权限
//        /// </summary>
//        /// <param name="model"></param>
//        /// <returns></returns>
//        async public Task 保存角色的权限(某角色的权限 model)
//        {
//            await Db.ExecuteSpAsync(new sp_角色的权限_增改删()
//            {
//                角色编号 = model.角色编号,
//                权限编号 = model.权限编号列表.ToPredefindedKeyFieldsList().ToDataTable()
//            });
//        }
//    }
//}
