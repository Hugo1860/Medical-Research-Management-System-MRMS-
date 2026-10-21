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
using System.Data;

namespace ScientificResearch.Controllers
{
    public class IncomingController : BaseController
    {
        public int 项目经费到账流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:项目经费到账");
            }
        }

        public int 纵向项目立项流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:纵向项目立项");
            }
        }

        public int 横向项目申请流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:横向项目申请");
            }
        }

        [HttpGet]
        async public Task<PagingResult<v3_项目经费到账_纵向>> 分页获取纵向项目经费合计到账列表(Paging paging, 纵向项目立项Filter filter)
        {
            return await Db.GetPagingListSpAsync<v3_项目经费到账_纵向, 纵向项目立项Filter>(paging, filter);
        }

        [HttpGet]
        async public Task<PagingResult<v3_项目经费到账_横向>> 分页获取横向项目经费合计到账列表(Paging paging, 横向项目Filter filter)
        {
            return await Db.GetPagingListSpAsync<v3_项目经费到账_横向, 横向项目Filter>(paging, filter);
        }

        [HttpGet]
        async public Task<object> 获取某纵向项目经费到账列表(int 纵向项目立项编号)
        {
            if (!await CanAccessProjectAsync(纵向项目立项流程模板编号, 纵向项目立项编号))
            {
                return ProjectAccessDenied();
            }
            return await Db.GetListSpAsync<object, 项目经费到账Filter>(
                new 项目经费到账Filter()
                {
                    流程模板编号 = 纵向项目立项流程模板编号,
                    项目编号 = 纵向项目立项编号
                },
            $"tfn_项目经费到账({CurrentUser.编号},{项目经费到账流程模板编号})");
        }

        [HttpGet]
        async public Task<object> 获取某横向项目经费到账列表(int 横向项目编号)
        {
            if (!await CanAccessProjectAsync(横向项目申请流程模板编号, 横向项目编号))
            {
                return ProjectAccessDenied();
            }
            return await Db.GetListSpAsync<object, 项目经费到账Filter>(
                new 项目经费到账Filter()
                {
                    流程模板编号 = 横向项目申请流程模板编号,
                    项目编号 = 横向项目编号
                },
                $"tfn_项目经费到账({CurrentUser.编号},{项目经费到账流程模板编号})");
        }

        [HttpPost]
        async public Task 新增纵向项目经费到账([FromBody]纵向项目经费到账增改 model)
        {
            if (model.基本资料.到账金额 <= 0)
            {
                throw new Exception("请填写正确的到账金额");
            }

            var entity = new 项目经费到账();
            Tool.ModelToModel(entity, model.基本资料);
            entity.流程模板编号 = 纵向项目立项流程模板编号;

            await MyWorkFlowBusiness.发起流程(
                            项目经费到账流程模板编号,
                            new sp_项目经费到账_增改()
                            {
                                tt = entity.ToDataTable()
                            },
                            CurrentUser.编号,
                            model.IsHold);
        }


        [HttpPost]
        async public Task 新增横向项目经费到账([FromBody]横向项目经费到账增改 model)
        {
            if (model.基本资料.到账金额 <= 0)
            {
                throw new Exception("请填写正确的到账金额");
            }

            var entity = new 项目经费到账();
            Tool.ModelToModel(entity, model.基本资料);
            entity.流程模板编号 = 横向项目申请流程模板编号;

            await MyWorkFlowBusiness.发起流程(
                            项目经费到账流程模板编号,
                            new sp_项目经费到账_增改()
                            {
                                tt = entity.ToDataTable()
                            },
                            CurrentUser.编号,
                            model.IsHold);
        }

        [HttpPost]
        async public Task 完善纵向项目经费到账([FromBody]完成步骤<纵向项目经费到账增改> model)
        {
            if (model.附加数据.基本资料.到账金额 <= 0)
            {
                throw new Exception("请填写正确的到账金额");
            }

            var entity = new 项目经费到账();
            Tool.ModelToModel(entity, model.附加数据.基本资料);
            entity.流程模板编号 = 纵向项目立项流程模板编号;

            await MyWorkFlowBusiness.完成步骤(
                model.toSimple(), CurrentUser.编号,
                new sp_项目经费到账_增改()
                {
                    tt = entity.ToDataTable()
                },
                model.附加数据.IsHold);
        }

        [HttpPost]
        async public Task 完善横向项目经费到账([FromBody]完成步骤<横向项目经费到账增改> model)
        {
            if (model.附加数据.基本资料.到账金额 <= 0)
            {
                throw new Exception("请填写正确的到账金额");
            }

            var entity = new 项目经费到账();
            Tool.ModelToModel(entity, model.附加数据.基本资料);
            entity.流程模板编号 = 横向项目申请流程模板编号;

            await MyWorkFlowBusiness.完成步骤(
                model.toSimple(), CurrentUser.编号,
                new sp_项目经费到账_增改()
                {
                    tt = entity.ToDataTable()
                },
                model.附加数据.IsHold);
        }


        [HttpPost]
        async public Task<object> 上传到账凭证()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/项目经费到账/到账凭证",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

    }
}
