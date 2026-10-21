//using Dapper;
//using ScientificResearch.Models;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Linq;
//using System.Threading.Tasks;
//using static Dapper.SqlMapper;

//namespace MyLib
//{
//    /// <summary>
//    /// dapper extenstions
//    /// 2018-5-28 废弃.使用PredefinedBusiness来关联预定义的各个sp
//    /// </summary>
//    public static class MyDapperExtensions
//    {

//        /// <summary>
//        /// ExecuteAsync带commandType: CommandType.StoredProcedure
//        /// </summary>
//        /// <param name="cnn"></param>
//        /// <param name="sql"></param>
//        /// <param name="param"></param>
//        /// <param name="transaction"></param>
//        /// <param name="commandTimeout"></param>
//        /// <param name="commandType"></param>
//        /// <returns></returns>
//        //public static Task<int> MyExecuteSpAsync(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
//        //{
//        //    return cnn.ExecuteAsync(sql, param, transaction, commandTimeout, commandType: CommandType.StoredProcedure);
//        //}

//        /// <summary>
//        /// QueryAsync带带commandType: CommandType.StoredProcedure
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="cnn"></param>
//        /// <param name="sql"></param>
//        /// <param name="param"></param>
//        /// <param name="transaction"></param>
//        /// <param name="commandTimeout"></param>
//        /// <param name="commandType"></param>
//        /// <returns></returns>
//        //public static Task<IEnumerable<T>> MyQuerySpAsync<T>(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
//        //{
//        //    return cnn.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType: CommandType.StoredProcedure);
//        //}

//        /// <summary>
//        /// QueryMultipleAsync带commandType: CommandType.StoredProcedure
//        /// </summary>
//        /// <param name="cnn"></param>
//        /// <param name="sql"></param>
//        /// <param name="param"></param>
//        /// <param name="transaction"></param>
//        /// <param name="commandTimeout"></param>
//        /// <param name="commandType"></param>
//        /// <returns></returns>
//        //public static Task<GridReader> MyQueryMultipleAsync(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
//        //{
//        //    return cnn.QueryMultipleAsync(sql, param, transaction, commandTimeout, commandType: CommandType.StoredProcedure);
//        //}

//        /// <summary>
//        /// sp_Getlist的,sp的参数自己写
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="cnn"></param>
//        /// <param name="param"></param>
//        /// <returns></returns>
//        //public static Task<IEnumerable<T>> MyGetListSpAsync<T>(this IDbConnection cnn, object param)
//        //{
//        //    return cnn.MyQuerySpAsync<T>("sp_GetList", param);
//        //}

//        /*====================================try to build publicBusiness==========================================*/

//        /// <summary>
//        /// 单独的传参数,和上面的不同了
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <typeparam name="TFilter"></typeparam>
//        /// <param name="cnn"></param>
//        /// <param name="tbName"></param>
//        /// <param name="filter"></param>
//        /// <param name="keyFields"></param>
//        /// <param name="orderType"></param>
//        /// <param name="orderStr"></param>
//        /// <returns></returns>
//        //async public static Task<IEnumerable<T>> MyGetListSpAsync<T, TFilter>(
//        //    this IDbConnection cnn,
//        //    string tbName = null,
//        //    TFilter filter = null,
//        //    string keyFields = "编号",
//        //    bool orderType = false,
//        //    string orderStr = "") where TFilter : class
//        //{
//        //    return await cnn.MyQuerySpAsync<T>("sp_GetList", new
//        //    {
//        //        tbName = tbName ?? typeof(T).Name,
//        //        strWhere = filter == null ? "" : SqlWhereMapper.toWhere<TFilter>(filter),
//        //        keyFields,
//        //        OrderType = orderType,
//        //        OrderStr = orderStr
//        //    });
//        //}

//        /// <summary>
//        /// 没有搜索条件的单独传参数
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="cnn"></param>
//        /// <param name="tbName"></param>
//        /// <param name="keyFields"></param>
//        /// <param name="orderType"></param>
//        /// <param name="orderStr"></param>
//        /// <returns></returns>
//        //async public static Task<IEnumerable<T>> MyGetListSpAsync<T>(
//        //    this IDbConnection cnn,
//        //    string tbName = null,
//        //    string keyFields = "编号",
//        //    bool orderType = false,
//        //    string orderStr = "")
//        //{
//        //    return await cnn.MyGetListSpAsync<T, object>(tbName, null, keyFields, orderType, orderStr);
//        //}

//        /// <summary>
//        /// 有条件的分页获取
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <typeparam name="TFilter"></typeparam>
//        /// <param name="cnn"></param>
//        /// <param name="tbName"></param>
//        /// <param name="paging"></param>
//        /// <param name="filter"></param>
//        /// <param name="keyFields"></param>
//        /// <param name="orderStr"></param>
//        /// <returns></returns>
//        //async public static Task<PagingResult<T>> MyGetPagingListSpAsync<T, TFilter>(
//        //    this IDbConnection cnn,
//        //    string tbName = null,
//        //    Paging paging = null,
//        //    TFilter filter = null,
//        //    string keyFields = "编号",
//        //    string orderStr = "") where TFilter : class
//        //{
//        //    if (paging == null) paging = new Paging();
//        //    var result = await cnn.MyQueryMultipleAsync("sp_GetPagingList", new
//        //    {
//        //        tbName = tbName ?? typeof(T).Name,
//        //        strWhere = filter == null ? "" : SqlWhereMapper.toWhere<TFilter>(filter),
//        //        keyFields,
//        //        PageSize = paging.Size,
//        //        PageIndex = paging.Index,
//        //        OrderType = paging.OrderType,
//        //        OrderStr = orderStr
//        //    });
//        //    var total = result.Read<int>().FirstOrDefault();
//        //    var list = result.Read<T>();
//        //    return new PagingResult<T> { Total = total, List = list };
//        //}

//        /// <summary>
//        /// 没有搜索条件的,返回分页数据
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="cnn"></param>
//        /// <param name="tbName"></param>
//        /// <param name="paging"></param>
//        /// <param name="where"></param>
//        /// <param name="keyFields"></param>
//        /// <param name="orderStr"></param>
//        /// <returns></returns>
//        //async public static Task<PagingResult<T>> MyGetPagingListSpAsync<T>(
//        //    this IDbConnection cnn,
//        //    string tbName = null,
//        //    Paging paging = null,
//        //    string keyFields = "编号",
//        //    string orderStr = "")
//        //{
//        //    //var result = await cnn.MyQueryMultipleAsync("sp_GetPagingList", new
//        //    //{
//        //    //    tbName,
//        //    //    keyFields,
//        //    //    PageSize = paging.Size,
//        //    //    PageIndex = paging.Index,
//        //    //    OrderType = paging.OrderType,
//        //    //    OrderStr = orderStr
//        //    //});
//        //    //var total = result.Read<int>().FirstOrDefault();
//        //    //var list = result.Read<T>();
//        //    //return new PagingResult<T> { Total = total, List = list };

//        //    return await cnn.MyGetPagingListSpAsync<T, object>(tbName, paging, null, keyFields, orderStr);
//        //}

//        /// <summary>
//        /// 增改一个表.单个对象.其中sp名称和tt的名称是按约定从表名构成的;
//        /// 2018-5-10 将增改的sp里面生成的参数名都改为tt,并不需要变化对吧!
//        /// 2018-5-16 修改为可以返回插入后值的queryasync方法
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="cnn"></param>
//        /// <param name="model"></param>
//        /// <param name="transaction"></param>
//        /// <param name="commandTimeout"></param>
//        /// <param name="commandType"></param>
//        /// <returns></returns>
//        //async public static Task<int> MyMergerSpAsync<T>(this IDbConnection cnn, T model, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null) where T : new()
//        //{
//        //    var TName = typeof(T).Name;
//        //    var spName = $"sp_{TName}_增改";
//        //    //var ttName = $"tt_{TName}";

//        //    //给obj动态添加动态属性
//        //    //dynamic obj = new System.Dynamic.ExpandoObject();
//        //    //((IDictionary<string, object>)obj).Add(ttName, model.ToDataTable());
//        //    //await cnn.MyExecuteSpAsync(spName, (object)obj);
//        //    var result = await cnn.MyQuerySpAsync<int>(spName, new { tt = model.ToDataTable() }, transaction, commandTimeout, commandType);
//        //    return result.FirstOrDefault();
//        //}
//        /// <summary>
//        /// 上面的姊妹,增改一个表.对象列表.
//        /// 2018-5-16 修改为可以返回插入后值的queryasync方法
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="cnn"></param>
//        /// <param name="list"></param>
//        /// /// <param name="transaction"></param>
//        /// <param name="commandTimeout"></param>
//        /// <param name="commandType"></param>
//        /// <returns></returns>
//        //async public static Task<IEnumerable<int>> MyMergerSpAsync<T>(this IDbConnection cnn, IEnumerable<T> list, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null) where T : new()
//        //{
//        //    var TName = typeof(T).Name;
//        //    var spName = $"sp_{TName}_增改";
//        //    //var ttName = $"tt_{TName}";
//        //    //dynamic obj = new System.Dynamic.ExpandoObject();
//        //    //((IDictionary<string, object>)obj).Add(ttName, list.ToDataTable());
//        //    //await cnn.MyExecuteSpAsync(spName, (IDictionary<string, DataTable>)obj);
//        //    return await cnn.MyQuerySpAsync<int>(spName, new { tt = list.ToDataTable() }, transaction, commandTimeout, commandType);
//        //}

//        /// <summary>
//        /// 删除sp的:sp的名称和T(表名)的对应关系,sp参数名称,编号列表这个tt的名称和类型,这些都需要有约定
//        /// 比上面好一点的地方是"tt_编号"这个sp的参数名是可以固定下来的;
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="cnn"></param>
//        /// <param name="idList"></param>
//        /// <returns></returns>
//        //async public static Task MyDeleteSpAsync<T>(this IDbConnection cnn, IEnumerable<int> idList) where T : new()
//        //{
//        //    var TName = typeof(T).Name;
//        //    var spName = $"sp_{TName}_删";

//        //    await cnn.MyExecuteSpAsync(spName, new { tt_编号 = idList.Select(i => new 编号列表() { 编号 = i }).ToDataTable() });
//        //}

//        /// <summary>
//        /// 启禁某个表的某条记录,对应的sp名有特定约定;
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="cnn"></param>
//        /// <param name="id"></param>
//        /// <param name="state"></param>
//        /// <returns></returns>
//        //async public static Task MyEnableDisableSpAsync<T>(this IDbConnection cnn, int id, bool state) where T : new()
//        //{
//        //    var TName = typeof(T).Name;
//        //    var spName = $"sp_{TName}_启禁";
//        //    await cnn.MyExecuteSpAsync(spName, new { 编号 = id, 是否启用 = state });
//        //}
//    }
//}
