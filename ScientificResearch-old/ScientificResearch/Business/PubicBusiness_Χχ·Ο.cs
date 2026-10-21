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
//    /// 一些通用的sp
//    /// 2018-5-28 弃用
//    /// 转到PredefinedSpBusiness和WorkFlowBusiness
//    /// 以后的business尽量不用basebusiness
//    /// </summary>
//    public class PubicSpBusiness : BaseBusiness
//    {
//        #region 构造
//        /// <summary>
//        /// 构造
//        /// </summary>
//        /// <param name="httpContext"></param>
//        public PubicSpBusiness(HttpContext httpContext) : base(httpContext) { }
//        #endregion

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="sql"></param>
//        /// <param name="param"></param>
//        /// <param name="transaction"></param>
//        /// <param name="commandTimeout"></param>
//        /// <param name="commandType"></param>
//        /// <returns></returns>
//        //async public Task<int> ExecuteSpAsync(string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
//        //{
//        //    return await Db.MyExecuteSpAsync(sql, param, transaction, commandTimeout);
//        //}

//        /// <summary>
//        /// 获取某个"表对象"的,某个过滤条件对象的,所有数据;
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <typeparam name="TFilter"></typeparam>
//        /// <param name="tbName"></param>
//        /// <param name="filter"></param>
//        /// <param name="keyFields"></param>
//        /// <param name="orderType"></param>
//        /// <param name="orderStr"></param>
//        /// <returns></returns>
//        //async public Task<IEnumerable<T>> GetListSpAsync<T, TFilter>(
//        //    TFilter filter = null,
//        //    string tbName = null,
//        //    string keyFields = "编号",
//        //    bool orderType = false,
//        //    string orderStr = "") where TFilter : class
//        //{
//        //    return await Db.MyGetListSpAsync<T, TFilter>(tbName, filter, keyFields, orderType, orderStr);
//        //}

//        /// <summary>
//        /// 获取某个"表对象"的,某个分页的,某个过滤条件的,分页数据;
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <typeparam name="TFilter"></typeparam>
//        /// <param name="tbName"></param>
//        /// <param name="paging"></param>
//        /// <param name="filter"></param>
//        /// <param name="keyFields"></param>
//        /// <param name="orderStr"></param>
//        /// <returns></returns>
//        //async public Task<PagingResult<T>> GetPagingListSpAsync<T, TFilter>(
//        //    Paging paging = null,
//        //    TFilter filter = null,
//        //    string tbName = null,
//        //    string keyFields = "编号",
//        //    string orderStr = "") where TFilter : class
//        //{
//        //    return await Db.MyGetPagingListSpAsync<T, TFilter>(tbName, paging, filter, keyFields, orderStr);
//        //}

//        /// <summary>
//        /// 增改某个"表对象"的,一个数据;
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="model"></param>
//        /// <returns></returns>
//        //async public Task<int> MergerSpAsync<T>(T model) where T : new()
//        //{
//        //    return await Db.MyMergerSpAsync<T>(model);
//        //}
//        /// <summary>
//        /// 增改某个"表对象"的,list;
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="list"></param>
//        /// <returns></returns>
//        //async public Task<IEnumerable<int>> MergerSpAsync<T>(IEnumerable<T> list) where T : new()
//        //{
//        //    return await Db.MyMergerSpAsync<T>(list);
//        //}

//        /// <summary>
//        /// 删除的
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="idList"></param>
//        /// <returns></returns>
//        //async public Task DeleteSpAsync<T>(IEnumerable<int> idList) where T : new()
//        //{
//        //    await Db.MyDeleteSpAsync<T>(idList);
//        //}

//        /// <summary>
//        /// 启禁的
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="id"></param>
//        /// <param name="state"></param>
//        /// <returns></returns>
//        //async public Task EnableDisableSpAsync<T>(int id, bool state) where T : new()
//        //{
//        //    await Db.MyEnableDisableSpAsync<T>(id, state);
//        //}

//        #region 流程
//        /// <summary>
//        /// 生成一个流程,TODO:这个要和下面的合并,但是如何从事务外传一个function进去来合并呢?
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="流程模板编号"></param>
//        /// <param name="model"></param>
//        /// <returns></returns>
//        //async public Task 发起流程<T>(int 流程模板编号, T model) where T : new()
//        //{
//        //    //Db.Open();
//        //    using (var dbForTransaction = new NpgsqlConnection(DbConnectionString))
//        //    {
//        //        dbForTransaction.Open();
//        //        using (var transaction = dbForTransaction.BeginTransaction())
//        //        {

//        //            //IDbTransaction transaction = Db.BeginTransaction();
//        //            try
//        //            {
//        //                //添加项目,这里固定的是添加一个东西
//        //                var TName = typeof(T).Name;
//        //                var spName = $"sp_{TName}_增改";
//        //                //var 项目编号 = await dbForTransaction.QuerySpAsync(model, transaction);
//        //                var 项目编号 = await dbForTransaction.MyMergerSpAsync<T>(model, transaction);


//        //                //发起流程
//        //                var resultWorkFlow = await dbForTransaction.MyQuerySpAsync<int>("sp_流程_发起", new
//        //                {
//        //                    流程模板编号,
//        //                    项目编号,
//        //                    发起人编号 = currentUser.编号
//        //                }, transaction);

//        //                var 步骤编号 = resultWorkFlow.FirstOrDefault();

//        //                //完成这个步骤
//        //                await dbForTransaction.MyExecuteSpAsync("sp_流程_完成步骤", new
//        //                {
//        //                    步骤编号,
//        //                    状态 = 1,
//        //                    操作人 = currentUser.编号,
//        //                    备注 = string.Empty
//        //                }, transaction);

//        //                transaction.Commit();
//        //            }
//        //            catch (Exception e)
//        //            {
//        //                transaction.Rollback();
//        //                throw e;
//        //            }
//        //        }
//        //    }
//        //}

//        /// <summary>
//        /// 执行一个sp,得到一个项目编号,同时生成一个流程;
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="流程模板编号"></param>
//        /// <param name="model"></param>
//        /// <param name="sql"></param>
//        /// <param name="param"></param>
//        /// <returns></returns>
//        //async public Task 发起流程(int 流程模板编号, string sql, object param)
//        //{
//        //    //Db.Open();
//        //    using (var dbForTransaction = new NpgsqlConnection(DbConnectionString))
//        //    {
//        //        dbForTransaction.Open();
//        //        using (var transaction = dbForTransaction.BeginTransaction())
//        //        {

//        //            //IDbTransaction transaction = Db.BeginTransaction();
//        //            try
//        //            {
//        //                //添加项目,这里固定的是添加一个东西
//        //                //这个sql里面的sp,必须返回一个int类型的编号,作为流程的项目编号
//        //                var 项目编号 = (await dbForTransaction.MyQuerySpAsync<int>(sql, param, transaction)).FirstOrDefault();

//        //                //发起流程
//        //                var resultWorkFlow = await dbForTransaction.MyQuerySpAsync<int>("sp_流程_发起", new
//        //                {
//        //                    流程模板编号,
//        //                    项目编号,
//        //                    发起人编号 = currentUser.编号
//        //                }, transaction);

//        //                var 步骤编号 = resultWorkFlow.FirstOrDefault();

//        //                //完成这个步骤
//        //                await dbForTransaction.MyExecuteSpAsync("sp_流程_完成步骤", new
//        //                {
//        //                    步骤编号,
//        //                    状态 = 1,
//        //                    操作人 = currentUser.编号,
//        //                    备注 = string.Empty
//        //                }, transaction);

//        //                transaction.Commit();
//        //            }
//        //            catch (Exception e)
//        //            {
//        //                transaction.Rollback();
//        //                throw e;
//        //            }
//        //        }
//        //    }
//        //}
//        ///// <summary>
//        ///// 完成步骤,可以另写一个灵活点的,传入多个sp执行参数的,在完成步骤的同时,分别执行这几个sp的方法.
//        ///// </summary>
//        ///// <typeparam name="T"></typeparam>
//        ///// <param name="步骤编号"></param>
//        ///// <param name="状态值"></param>
//        ///// <param name="备注"></param>
//        ///// <param name="model"></param>
//        ///// <returns></returns>
//        ////async public Task 完成步骤<T>(完成步骤<T> model) where T : new()
//        //    //async public Task 完成步骤<T>(int 步骤编号, int 状态值, string 备注, T model) where T : new()
//        //{
//        //    using (var dbForTransaction = new NpgsqlConnection(DbConnectionString))
//        //    {
//        //        dbForTransaction.Open();
//        //        using (var transaction = dbForTransaction.BeginTransaction())
//        //        {
//        //            try
//        //            {
//        //                //完成附加动作,一般是一个增改;
//        //                if (model.附加数据 != null)
//        //                {
//        //                    await dbForTransaction.MyMergerSpAsync<T>(model.附加数据, transaction);
//        //                }

//        //                //完成步骤
//        //                await dbForTransaction.MyExecuteSpAsync("sp_流程_完成步骤", new
//        //                {
//        //                    步骤编号 = model.步骤编号,
//        //                    状态 = model.状态值,
//        //                    备注 = model.备注,
//        //                    操作人 = currentUser.编号
//        //                }, transaction);

//        //                transaction.Commit();
//        //            }
//        //            catch (Exception e)
//        //            {
//        //                transaction.Rollback();
//        //                throw e;
//        //            }
//        //        }
//        //    }
//        //}

//        /// <summary>
//        /// 所以TODO:上面的也要改
//        /// </summary>
//        /// <param name="model"></param>
//        /// <param name="sql"></param>
//        /// <param name="param"></param>
//        /// <returns></returns>
//        //async public Task 完成步骤(完成步骤 model, string sql, object param)
//        ////async public Task 完成步骤<T>(int 步骤编号, int 状态值, string 备注, T model) where T : new()
//        //{
//        //    using (var dbForTransaction = new NpgsqlConnection(DbConnectionString))
//        //    {
//        //        dbForTransaction.Open();
//        //        using (var transaction = dbForTransaction.BeginTransaction())
//        //        {
//        //            try
//        //            {
//        //                //完成附加动作,一般是一个增改;
//        //                await dbForTransaction.MyQuerySpAsync<int>(sql, param, transaction);

//        //                //完成步骤
//        //                await dbForTransaction.MyExecuteSpAsync("sp_流程_完成步骤", new
//        //                {
//        //                    步骤编号 = model.步骤编号,
//        //                    状态 = model.状态值,
//        //                    备注 = model.备注,
//        //                    操作人 = currentUser.编号
//        //                }, transaction);

//        //                transaction.Commit();
//        //            }
//        //            catch (Exception e)
//        //            {
//        //                transaction.Rollback();
//        //                throw e;
//        //            }
//        //        }
//        //    }
//        //}

//        ///// <summary>
//        ///// 针对不需要附加动作的步骤操作
//        ///// </summary>
//        ///// <param name="model"></param>
//        ///// <returns></returns>
//        //async public Task 完成步骤(完成步骤 model)
//        //{
//        //    await 完成步骤<object>(new Models.完成步骤<object>()
//        //    {
//        //        步骤编号 = model.步骤编号,
//        //        状态值 = model.状态值,
//        //        备注 = model.备注,
//        //        附加数据 = null
//        //    });
//        //}

//        /// <summary>
//        /// 获取某流程的步骤
//        /// </summary>
//        /// <param name="流程模板编号"></param>
//        /// <param name="项目编号"></param>
//        /// <returns></returns>
//        //async public Task<IEnumerable<v2_步骤>> 获取某流程的步骤(int 流程模板编号, int 项目编号) =>
//        //    await GetListSpAsync<v2_步骤, object>(
//        //        null,
//        //        $"tfn_步骤({流程模板编号},{项目编号})",
//        //        orderType: true);

//        #endregion

//    }
//}
