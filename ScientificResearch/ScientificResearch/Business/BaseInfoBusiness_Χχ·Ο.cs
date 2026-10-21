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
using Microsoft.Extensions.Configuration;

namespace ScientificResearch.Business
{
    /// <summary>
    /// 基础信息维护
    /// </summary>
    public class BaseInfoBusiness : BaseBusiness
    {
        #region 构造
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="httpContext"></param>
        public BaseInfoBusiness(HttpContext httpContext) : base(httpContext) { }
        #endregion

        #region 字典管理
        //async public Task<IEnumerable<字典>> 获取字典(字典Filter filter)
        //{
        //    var where = SqlWhereMapper.toWhere<字典Filter>(filter);

        //    var result = await Db.QueryAsync<字典>(
        //        "sp_GetList",
        //        new
        //        {
        //            strWhere = where,
        //            tbName = $"字典",
        //        },
        //        commandType: CommandType.StoredProcedure);

        //    return result;
        //}

        //async public Task 增改字典(字典 model)
        //{
        //    await Db.ExecuteSpAsync(new sp_字典_增改()
        //    {
        //        tt_字典 = model.ToDataTable()
        //    });
        //}

        //async public Task 删除字典(IEnumerable<int> 编号列表)
        //{
        //    await Db.ExecuteSpAsync(new sp_字典_删()
        //    {
        //        tt_编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
        //    });
        //}
        #endregion

        #region 部门管理
        //async public Task<IEnumerable<v1_部门>> 获取部门列表(部门Filter filter)
        //{
        //    return await Db.GetListSpAsync<v1_部门, 部门Filter>(filter);
        //}

        //async public Task 增改部门(部门 model)
        //{
        //    await Db.ExecuteSpAsync(new sp_部门_增改()
        //    {
        //        tt_部门 = model.ToDataTable()
        //    });
        //}

        //async public Task 启禁部门(int 编号, bool 是否启用)
        //{
        //    await Db.ExecuteSpAsync(new sp_部门_启禁()
        //    {
        //        编号 = 编号,
        //        是否启用 = 是否启用
        //    });
        //}
        #endregion

        #region 国民行业代码
        //async public Task<IEnumerable<v1_国民行业代码>> 获取国民行业代码(国民行业代码Filter filter)
        //{
        //    return await Db.GetListSpAsync<v1_国民行业代码, 国民行业代码Filter>(filter);
        //}

        //async public Task 增改国民行业代码(国民行业代码 model)
        //{
        //    await Db.ExecuteSpAsync(new sp_国民行业代码_增改() { tt_国民行业代码 = model.ToDataTable() });
        //}

        //async public Task 删除国民行业代码(IEnumerable<int> 编号列表)
        //{
        //    await Db.ExecuteSpAsync(new sp_国民行业代码_删()
        //    {
        //        tt_编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
        //    });
        //}
        #endregion

        #region 合同买方性质
        //async public Task<IEnumerable<v1_合同买方性质>> 获取合同买方性质(合同买方性质Filter filter) =>
        //    await Db.GetListSpAsync<v1_合同买方性质, 合同买方性质Filter>(filter);


        //async public Task 增改合同买方性质(合同买方性质 model)
        //{
        //    await Db.ExecuteSpAsync(new sp_合同买方性质_增改() { tt_合同买方性质 = model.ToDataTable() });
        //}

        //async public Task 删除合同买方性质(IEnumerable<int> 编号列表)
        //{
        //    await Db.ExecuteSpAsync(new sp_合同买方性质_删
        //    {
        //        tt_编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
        //    });
        //}

        #endregion

        #region 技术领域
        //async public Task<IEnumerable<v1_技术领域>> 获取技术领域(技术领域Filter filter) =>
        //    await Db.GetListSpAsync<v1_技术领域, 技术领域Filter>(filter);
        //{
        //    return await Db.MyGetListSpAsync<技术领域>(new
        //    {
        //        strWhere = SqlWhereMapper.toWhere<技术领域Filter>(filter),
        //        tbName = "技术领域"
        //    });
        //}

        //async public Task 增改技术领域(技术领域 model)
        //{
        //    await Db.ExecuteSpAsync(new sp_技术领域_增改() { tt_技术领域 = model.ToDataTable() });
        //}

        //async public Task 删除技术领域(IEnumerable<int> 编号列表)
        //{
        //    await Db.ExecuteSpAsync(new sp_技术领域_删
        //    {
        //        tt_编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
        //    });
        //}
        #endregion

        #region 学科
        //async public Task<IEnumerable<v1_学科>> 获取学科(学科Filter filter)
        //{
        //    return await Db.MyGetListSpAsync<v1_学科>(new
        //    {
        //        strWhere = SqlWhereMapper.toWhere<学科Filter>(filter),
        //        tbName = "v1_学科"
        //    });
        //}

        //async public Task 增改学科(学科 model)
        //{
        //    await Db.ExecuteSpAsync(new sp_学科_增改() { tt_学科 = model.ToDataTable() });
        //}

        //async public Task 删除学科(IEnumerable<int> 编号列表)
        //{
        //    await Db.ExecuteSpAsync(new sp_学科_删()
        //    {
        //        tt_编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
        //    });
        //}
        #endregion

        #region 区域
        //async public Task<IEnumerable<v1_区域>> 获取区域(区域Filter filter) =>
        //    await Db.GetListSpAsync<v1_区域, 区域Filter>(filter);
        //{
        //    return await Db.MyGetListSpAsync<区域>(new
        //    {
        //        strWhere = SqlWhereMapper.toWhere<区域Filter>(filter),
        //        tbName = "区域"
        //    });
        //}

        //async public Task 增改区域(区域 model)
        //{
        //    await Db.ExecuteSpAsync(new sp_区域_增改 { tt_区域 = model.ToDataTable() });
        //}

        //async public Task 删除区域(IEnumerable<int> 编号列表)
        //{
        //    await Db.ExecuteSpAsync(new sp_区域_删
        //    {
        //        tt_编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
        //    });
        //}
        #endregion

        #region 项目分类
        //async public Task<IEnumerable<项目分类>> 获取项目分类(项目分类Filter filter)
        //{
        //    return await Db.GetListSpAsync<项目分类, 项目分类Filter>(filter);
        //}

        //async public Task 增改项目分类(项目分类 model)
        //{
        //    await Db.ExecuteSpAsync(new sp_项目分类_增改() { tt_项目分类 = model.ToDataTable() });
        //}

        //async public Task 删除项目分类(IEnumerable<int> 编号列表)
        //{
        //    await Db.ExecuteSpAsync(new sp_项目分类_删()
        //    {
        //        tt_编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
        //    });
        //}
        #endregion

        #region 期刊
        //async public Task<PagingResult<期刊>> 分页获取期刊(Paging paging, 期刊Filter filter)
        //{
        //    return await Db.GetPagingListSpAsync<期刊, 期刊Filter>(paging, filter);
        //}

        //async public Task 增改期刊(期刊 model)
        //{
        //    await Db.ExecuteSpAsync(new sp_期刊_增改() { tt_期刊 = model.ToDataTable() });
        //}

        //async public Task 删除期刊(IEnumerable<int> 编号列表)
        //{
        //    await Db.ExecuteSpAsync(new sp_期刊_删()
        //    {
        //        tt_编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
        //    });
        //}
        #endregion

        #region 经费模版
        //async public Task<IEnumerable<经费模版>> 获取经费模版() =>
        //    await Db.GetListSpAsync<经费模版>();

        //async public Task 增改经费模版(经费模版 model) =>
        //    await Db.ExecuteSpAsync(new sp_经费模版_增改() { tt = model.ToDataTable() });

        //async public Task 删除经费模版(IEnumerable<int> 编号列表) =>
        //    await Db.ExecuteSpAsync(new sp_经费模版_删()
        //    {
        //        tt_编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
        //    });

        //async public Task<IEnumerable<经费模版项目支出类型>> 获取经费模版项目支出类型(经费模版项目支出类型Filter filter) =>
        //    await Db.GetListSpAsync<经费模版项目支出类型, 经费模版项目支出类型Filter>(filter);

        //async public Task 增改经费模版项目支出类型(经费模版项目支出类型 model) =>
        //    await Db.ExecuteSpAsync(new sp_经费模版项目支出类型_增改() { tt_经费模版项目支出类型 = model.ToDataTable() });

        //async public Task 删除经费模版项目支出类型(IEnumerable<int> 编号列表) =>
        //    await Db.ExecuteSpAsync(new sp_经费模版项目支出类型_删()
        //    {
        //        tt_编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
        //    });

        //async public Task<IEnumerable<经费模版项目支出内容>> 获取经费模版项目支出内容(经费模版项目支出内容Filter filter) =>
        //    await Db.GetListSpAsync<经费模版项目支出内容, 经费模版项目支出内容Filter>(filter);

        //async public Task 增改经费模版项目支出内容(经费模版项目支出内容 model) =>
        //    await Db.ExecuteSpAsync(new sp_经费模版项目支出内容_增改() { tt_经费模版项目支出内容 = model.ToDataTable() });

        //async public Task 删除经费模版项目支出内容(IEnumerable<int> 编号列表) =>
        //    await Db.ExecuteSpAsync(new sp_经费模版项目支出内容_删()
        //    {
        //        tt_编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
        //    });
        #endregion
    }
}
