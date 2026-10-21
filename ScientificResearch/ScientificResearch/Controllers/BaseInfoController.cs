using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ScientificResearch.Business;
using ScientificResearch.Infrastucture;
using ScientificResearch.Models;
using Dapper;
using MyLib;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ScientificResearch.Controllers
{
    /// <summary>
    /// 基础信息维护的接口
    /// </summary>
    public class BaseInfoController : BaseController
    {
        /// <summary>查看迁移后历史重复键，供系统管理员和科研管理员清理主数据。</summary>
        [HttpGet]
        async public Task<object> 获取数据完整性异常()
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办"))
                return StatusCode(403, new { error = "无权查看数据完整性异常" });
            return await Db.QueryAsync<object>("SELECT * FROM \"v1_数据完整性异常\" ORDER BY \"对象\", \"重复键\";");
        }

        /// <summary>查看迁移后的项目、经费和参与人绑定异常；只读，不自动清理历史数据。</summary>
        [HttpGet]
        async public Task<object> 获取迁移绑定异常()
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办"))
                return StatusCode(403, new { error = "无权查看迁移绑定异常" });
            return await Db.QueryAsync<object>(@"
SELECT '项目参与人' AS ""异常类型"", p.""编号""::text AS ""记录编号"", p.""流程模板编号"", p.""项目编号"", p.""参与人编号""::text AS ""关联编号"", '流程模板、项目和参与人均为0或无对应项目' AS ""说明""
  FROM ""项目参与人"" p
 WHERE NOT (
       (p.""流程模板编号""=400 AND EXISTS (SELECT 1 FROM ""纵向项目申报"" x WHERE x.""编号""=p.""项目编号""))
    OR (p.""流程模板编号""=410 AND EXISTS (SELECT 1 FROM ""纵向项目立项"" x WHERE x.""编号""=p.""项目编号""))
    OR (p.""流程模板编号""=450 AND EXISTS (SELECT 1 FROM ""横向项目"" x WHERE x.""编号""=p.""项目编号""))
    OR EXISTS (SELECT 1 FROM ""流程"" f WHERE f.""流程模板编号""=p.""流程模板编号"" AND f.""项目编号""=p.""项目编号""))
 ORDER BY p.""编号"";");
        }

        #region 字典
        /// <summary>
        /// 根据分类名称,获取某类字典,包括一个层次的和两个层次的字典数据,具体有哪些见基本信息整理文档.xlsx
        /// </summary>
        /// <param name="分类"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<IEnumerable<字典>> 获取某类字典(string 分类)
        {
            var filter = new 字典Filter() { 分类 = 分类 };
            return await Db.GetListSpAsync<字典, 字典Filter>(filter);
        }

        /// <summary>
        /// 分页获取某类字典
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<PagingResult<字典>> 分页获取某类字典(Paging paging, 字典Filter filter)
        {
            return await Db.GetPagingListSpAsync<字典, 字典Filter>(paging, filter);
        }

        [HttpGet]
        async public Task<IEnumerable<字典>> 获取某上级编号下字典(int 编号)
        {
            var filter = new 字典Filter() { 上级编号 = 编号 };
            return await Db.GetListSpAsync<字典, 字典Filter>(filter);
        }
        
        [HttpGet]
        async public Task<PagingResult<字典>> 分页获取某上级编号下字典(Paging paging, int 编号)
        {
            var filter = new 字典Filter() { 上级编号 = 编号 };
            return await Db.GetPagingListSpAsync<字典, 字典Filter>(paging, filter);
        }

        /// <summary>
        /// 新增一个字典数据
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 新增字典([FromBody]字典 model) =>
            await Db.ExecuteSpAsync(new sp_字典_增改()
            {
                tt_字典 = model.ToDataTable()
            });

        /// <summary>
        /// 修改一个字典数据
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 修改字典([FromBody]字典 model) =>
            await Db.ExecuteSpAsync(new sp_字典_增改()
            {
                tt_字典 = model.ToDataTable()
            });

        /// <summary>
        /// 删除一个字典数据
        /// </summary>
        /// <param name="编号列表"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 删除字典([FromBody]IEnumerable<int> 编号列表) =>
            await Db.ExecuteSpAsync(new sp_字典_删()
            {
                tt_编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
            });
        #endregion

        #region 部门
        /// <summary>
        /// 获取全部部门,维护部门时用
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<IEnumerable<v1_部门>> 获取部门列表(部门Filter filter) =>
            await Db.GetListSpAsync<v1_部门, 部门Filter>(filter);

        /// <summary>
        /// 获取启用的部门列表,使用部门时用
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<IEnumerable<v1_部门>> 获取启用的部门列表(部门Filter filter)
        {
            filter.是否启用 = true;
            return await Db.GetListSpAsync<v1_部门, 部门Filter>(filter);
        }

        /// <summary>
        /// 新增部门
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 增改部门([FromBody]部门 model) =>
            await Db.ExecuteSpAsync(new sp_部门_增改()
            {
                tt_部门 = model.ToDataTable()
            });

        /// <summary>
        /// 启用某部门
        /// </summary>
        /// <param name="编号"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 启用部门([FromBody]int 编号) =>
            await Db.ExecuteSpAsync(new sp_部门_启禁()
            {
                编号 = 编号,
                是否启用 = true
            });

        /// <summary>
        /// 禁用某部门
        /// </summary>
        /// <param name="编号"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 禁用部门([FromBody]int 编号) =>
            await Db.ExecuteSpAsync(new sp_部门_启禁()
            {
                编号 = 编号,
                是否启用 = false
            });
        #endregion

        #region 国民行业代码
        /// <summary>
        /// 获取国民行业代码,父级编号必须输入,以便一层一层的获取
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<IEnumerable<v1_国民行业代码>> 获取国民行业代码(国民行业代码Filter filter) =>
            await Db.GetListSpAsync<v1_国民行业代码, 国民行业代码Filter>(filter);

        /// <summary>
        /// 新增国民行业代码
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 新增国民行业代码([FromBody]国民行业代码 model) =>
            await Db.ExecuteSpAsync(new sp_国民行业代码_增改() { tt_国民行业代码 = model.ToDataTable() });

        /// <summary>
        /// 修改国民行业代码
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 修改国民行业代码([FromBody]国民行业代码 model) =>
            await Db.ExecuteSpAsync(new sp_国民行业代码_增改() { tt_国民行业代码 = model.ToDataTable() });

        /// <summary>
        /// 删除国民行业代码,传入的是要删除的编号列表
        /// </summary>
        /// <param name="编号列表"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 删除国民行业代码([FromBody]IEnumerable<int> 编号列表) =>
             await Db.ExecuteSpAsync(new sp_国民行业代码_删()
             {
                 tt_编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
             });

        #endregion

        #region 合同买方性质
        /// <summary>
        /// 获取合同买方性质,父级编号必须输入,以便一层一层的获取
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<IEnumerable<v1_合同买方性质>> 获取合同买方性质(合同买方性质Filter filter) =>
            await Db.GetListSpAsync<v1_合同买方性质, 合同买方性质Filter>(filter);

        /// <summary>
        /// 新增合同买方性质
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 新增合同买方性质([FromBody]合同买方性质 model) =>
            await Db.ExecuteSpAsync(new sp_合同买方性质_增改() { tt_合同买方性质 = model.ToDataTable() });

        /// <summary>
        /// 修改合同买方性质
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 修改合同买方性质([FromBody]合同买方性质 model) =>
            await Db.ExecuteSpAsync(new sp_合同买方性质_增改() { tt_合同买方性质 = model.ToDataTable() });

        /// <summary>
        /// 删除合同买方性质,传入的是要删除的编号列表
        /// </summary>
        /// <param name="编号列表"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 删除合同买方性质([FromBody]IEnumerable<int> 编号列表) =>
            await Db.ExecuteSpAsync(new sp_合同买方性质_删
            {
                tt_编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
            });

        #endregion

        #region 技术领域
        /// <summary>
        /// 获取技术领域,父级编号必须输入,以便一层一层的获取
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<IEnumerable<v1_技术领域>> 获取技术领域(技术领域Filter filter) =>
            await Db.GetListSpAsync<v1_技术领域, 技术领域Filter>(filter);

        /// <summary>
        /// 新增技术领域
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 新增技术领域([FromBody]技术领域 model) =>
             await Db.ExecuteSpAsync(new sp_技术领域_增改() { tt_技术领域 = model.ToDataTable() });

        /// <summary>
        /// 修改技术领域
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 修改技术领域([FromBody]技术领域 model) =>
             await Db.ExecuteSpAsync(new sp_技术领域_增改() { tt_技术领域 = model.ToDataTable() });

        /// <summary>
        /// 删除技术领域,传入的是要删除的编号列表
        /// </summary>
        /// <param name="编号列表"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 删除技术领域([FromBody]IEnumerable<int> 编号列表) =>
            await Db.ExecuteSpAsync(new sp_技术领域_删
            {
                tt_编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
            });

        #endregion

        #region 学科
        /// <summary>
        /// 获取学科,父级编号必须输入,以便一层一层的获取
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<IEnumerable<v1_学科>> 获取学科(学科Filter filter) =>
            await Db.GetListSpAsync<v1_学科, 学科Filter>(filter);
        //await MyBaseInfoBusiness.获取学科(filter);

        /// <summary>
        /// 新增学科
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 新增学科([FromBody]学科 model) =>
            await Db.ExecuteSpAsync(new sp_学科_增改() { tt_学科 = model.ToDataTable() });

        /// <summary>
        /// 修改学科
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 修改学科([FromBody]学科 model) =>
            await Db.ExecuteSpAsync(new sp_学科_增改() { tt_学科 = model.ToDataTable() });

        /// <summary>
        /// 删除学科,传入的是要删除的编号列表
        /// </summary>
        /// <param name="编号列表"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 删除学科([FromBody]IEnumerable<int> 编号列表) =>
            await Db.ExecuteSpAsync(new sp_学科_删()
            {
                tt_编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
            });

        #endregion

        #region 区域
        /// <summary>
        /// 获取区域,父级编号必须输入,以便一层一层的获取
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<IEnumerable<v1_区域>> 获取区域(区域Filter filter) =>
            await Db.GetListSpAsync<v1_区域, 区域Filter>(filter);

        /// <summary>
        /// 新增区域
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 新增区域([FromBody]区域 model) =>
            await Db.ExecuteSpAsync(new sp_区域_增改 { tt_区域 = model.ToDataTable() });

        /// <summary>
        /// 修改区域
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 修改区域([FromBody]区域 model) =>
            await Db.ExecuteSpAsync(new sp_区域_增改 { tt_区域 = model.ToDataTable() });

        /// <summary>
        /// 删除区域,传入的是要删除的编号列表
        /// </summary>
        /// <param name="编号列表"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 删除区域([FromBody]IEnumerable<int> 编号列表) =>
            await Db.ExecuteSpAsync(new sp_区域_删
            {
                tt_编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
            });

        #endregion

        #region 项目分类
        /// <summary>
        /// 获取项目分类,父级编号必须输入,以便一层一层的获取
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<IEnumerable<v1_项目分类>> 获取项目分类(项目分类Filter filter) =>
            await Db.GetListSpAsync<v1_项目分类, 项目分类Filter>(filter);

        /// <summary>
        /// 新增项目分类
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 增改项目分类([FromBody]项目分类 model) =>
            await Db.ExecuteSpAsync(new sp_项目分类_增改() { tt_项目分类 = model.ToDataTable() });


        /// <summary>
        /// 删除项目分类,传入的是要删除的编号列表
        /// </summary>
        /// <param name="编号列表"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 删除项目分类([FromBody]IEnumerable<int> 编号列表) =>
            await Db.ExecuteSpAsync(new sp_项目分类_删()
            {
                tt_编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
            });

        #endregion

        #region 期刊
        /// <summary>
        /// 分页获取期刊
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<PagingResult<v1_期刊>> 分页获取期刊(Paging paging, 期刊Filter filter) =>
            await Db.GetPagingListSpAsync<v1_期刊, 期刊Filter>(paging, filter);

        /// <summary>
        /// 新增期刊
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 增改期刊([FromBody]期刊 model) =>
            await Db.ExecuteSpAsync(new sp_期刊_增改() { tt_期刊 = model.ToDataTable() });

        [HttpPost]
        async public Task 导入期刊()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
               Request.Form.Files,
               Env.WebRootPath,
               "upload/基础信息/导入期刊",
               config.GetValue<int>("uploadFileMaxSize"));

            var relativePath = filesNameList[0].TrimStart('/', '\\');
            var result = MyXls.Import<期刊>(Path.Combine(Env.WebRootPath, relativePath));

            //var f = "C:\\Users\\Administrator\\Desktop\\导入期刊模板.xlsx";
            //var result = MyXls.Import<期刊>(f);

            //名称或英文刊名都空的删掉
            result = result.Where(i => !string.IsNullOrWhiteSpace(i.名称) || !string.IsNullOrWhiteSpace(i.英文刊名));

            TryValidateModel(result);
            await Db.ExecuteSpAsync(new sp_期刊_增改() { tt_期刊 = result.ToDataTable() });
        }

        /// <summary>
        /// 删除期刊
        /// </summary>
        /// <param name="编号列表"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 删除期刊([FromBody]IEnumerable<int> 编号列表) =>
            await Db.ExecuteSpAsync(new sp_期刊_删()
            {
                tt_编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
            });
        #endregion

        #region 经费模版三套件
        /// <summary>
        /// 获取所有经费模版
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        async public Task<PagingResult<经费模版>> 获取经费模版(Paging paging, 经费模版Filter filter) =>
            //await MyBaseInfoBusiness.获取经费模版();
            await Db.GetPagingListSpAsync<经费模版, 经费模版Filter>(paging, filter);

        /// <summary>
        /// 增改经费模版
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 增改经费模版([FromBody]经费模版 model) =>
             await Db.ExecuteSpAsync(new sp_经费模版_增改() { tt = model.ToDataTable() });

        /// <summary>
        /// 删除经费模版,其下的类型和内容没有管
        /// </summary>
        /// <param name="编号列表"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 删除经费模版([FromBody]IEnumerable<int> 编号列表) =>
            await Db.ExecuteSpAsync(new sp_经费模版_删()
            {
                tt_编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
            });

        /// <summary>
        /// 获取经费模版项目支出类型
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<PagingResult<经费模版项目支出类型>> 获取经费模版项目支出类型列表(Paging paging, 经费模版项目支出类型Filter filter) =>
            await Db.GetPagingListSpAsync<经费模版项目支出类型, 经费模版项目支出类型Filter>(paging, filter);
        //await MyBaseInfoBusiness.获取经费模版项目支出类型(filter);

        [HttpGet]
        async public Task<经费模版项目支出类型详情> 获取经费模版项目支出类型详情(int 经费模版项目支出类型编号)
        {
            var r1 = await Db.GetListSpAsync<经费模版项目支出类型, 经费模版项目支出类型Filter>(
                new 经费模版项目支出类型Filter()
                {
                    编号 = 经费模版项目支出类型编号
                });
            var r1Result = r1.FirstOrDefault();

            var r2 = await Db.GetListSpAsync<经费模版项目支出内容, 经费模版项目支出内容Filter>(
                new 经费模版项目支出内容Filter()
                {
                    所属经费模版项目支出类型编号 = 经费模版项目支出类型编号
                });
            if (r1Result == null)
            {
                return new 经费模版项目支出类型详情
                {
                    基本信息 = null,
                    经费模版项目支出内容列表 = r2,
                    可选支出内容列表 = Array.Empty<string>()
                };
            }

            var typeName = "项目支出类型";
            var escapedProjectType = (r1Result.项目支出类型 ?? string.Empty)
                .Replace("'", "''", StringComparison.Ordinal);
            var r3 = await Db.GetListSpAsync<字典>($"tfn_获取下级字典_通过上级分类和名称('{typeName}','{escapedProjectType}')");

            var r3Result = r3.Select(i => i.值);

            return new 经费模版项目支出类型详情() { 基本信息 = r1Result, 经费模版项目支出内容列表 = r2, 可选支出内容列表 = r3Result };
        }


        /// <summary>
        /// 增改经费模版项目支出类型
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 增改经费模版项目支出类型([FromBody]经费模版项目支出类型增改详情 某经费模版项目支出类型详情)
        {
            await Db.ExecuteSpAsync(
                new sp_经费模版项目支出类型_增改
                {
                    tt_经费模版项目支出类型 = 某经费模版项目支出类型详情.基本信息.ToDataTable(),
                    tt_经费模版项目支出内容 = 某经费模版项目支出类型详情.经费模版项目支出内容列表.ToDataTable()
                });
        }

        /// <summary>
        /// 删除经费模版项目支出类型
        /// </summary>
        /// <param name="编号列表"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 删除经费模版项目支出类型([FromBody]IEnumerable<int> 编号列表) =>
             await Db.ExecuteSpAsync(new sp_经费模版项目支出类型_删()
             {
                 tt_编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
             });

        #endregion
    }
}
