using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ScientificResearch.Business;
using ScientificResearch.Infrastucture;
using ScientificResearch.Models;

using MyLib;
using Microsoft.Extensions.Configuration;

namespace ScientificResearch.Controllers
{
    public class AcademicActivitiesController : BaseController
    {
        public int 主办讲座流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:主办讲座");
            }
        }

        public int 讲座反馈流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:讲座反馈");
            }
        }

        public int 参加会议流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:参加会议");
            }
        }

        public int 参加会议反馈流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:参加会议反馈");
            }
        }

        #region 主办讲座
        /// <summary>
        /// 发起一个主办讲座流程
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 发起一个主办讲座流程([FromBody]主办讲座 model)
        {
            model.主办人编号 = CurrentUser.编号;
            await MyWorkFlowBusiness.发起流程(
                主办讲座流程模板编号,
                new sp_主办讲座_增改()
                {
                    tt = model.ToDataTable()
                },
                CurrentUser.编号);
        }

        /// <summary>
        /// 完善主办讲座申请资料
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 完善主办讲座申请资料([FromBody]完成步骤<主办讲座> model) =>
            await MyWorkFlowBusiness.完成步骤(
                model.toSimple(), CurrentUser.编号,
                new sp_主办讲座_增改()
                {
                    tt = model.附加数据.ToDataTable()
                });

        /// <summary>
        /// 上传主讲人简历,返回上传成功后生成的文件相对url
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        async public Task<object> 上传主讲人简历()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/学术活动/主讲人简历",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        /// <summary>
        /// 分页获取主办讲座
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<object> 分页获取主办讲座(Paging paging, 主办讲座Filter filter) =>
            await Db.GetPagingListSpAsync<object, 主办讲座Filter>(
                paging,
                filter,
                $"tfn_主办讲座({CurrentUser.编号},{主办讲座流程模板编号})");

        [HttpGet]
        async public Task<object> 获取主办讲座详情(int 主办讲座编号)
        {
            var r1 = await Db.GetModelByIdSpAsync<v2_主办讲座>(主办讲座编号);
            var r2 = await MyWorkFlowBusiness.获取某流程的步骤(主办讲座流程模板编号, 主办讲座编号);
            return new { 学术任职详情 = r1, 审核情况 = r2 };
        }
        #endregion

        #region 主办讲座反馈
        /// <summary>
        /// 暂时没用
        /// 分页获取可反馈的主办讲座,此讲座满足条件:
        /// 1 主办人是当前登录人;
        /// 2 还没有被反馈过;
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<object> 分页获取可反馈的主办讲座(Paging paging, 主办讲座Filter filter)
        {
            filter.主办人编号 = CurrentUser.编号;
            //filter.是否反馈过 = false;
            return await Db.GetPagingListSpAsync<object, 主办讲座Filter>(
                  paging,
                  filter,
                  $"tfn_主办讲座({CurrentUser.编号},{主办讲座流程模板编号})");
        }

        /// <summary>
        /// 发起一个讲座反馈流程
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 发起一个讲座反馈流程([FromBody]讲座反馈 model)
        {
            await MyWorkFlowBusiness.发起流程(讲座反馈流程模板编号, new sp_讲座反馈_增改() { tt = model.ToDataTable() }, CurrentUser.编号);
        }


        /// <summary>
        /// 被打回来以后,完善讲座反馈申请资料
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 完善讲座反馈申请资料([FromBody]完成步骤<讲座反馈> model) =>
            await MyWorkFlowBusiness.完成步骤(model.toSimple(), CurrentUser.编号, new sp_讲座反馈_增改() { tt = model.附加数据.ToDataTable() });

        /// <summary>
        /// 上传讲座附件,注意:模型里面的讲稿文件其实就是主办讲座里面的主讲人简历,
        /// 座椅讲座反馈里面的讲稿文件完全用不上了;
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        async public Task<object> 上传讲座附件()
        {
            return await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/学术活动/讲座附件",
                config.GetValue<int>("uploadFileMaxSize"));
        }

        [HttpGet]
        async public Task<object> 分页获取讲座反馈(Paging paging, 讲座反馈Filter filter) =>
            await Db.GetPagingListSpAsync<object, 讲座反馈Filter>(
                paging,
                filter,
                $"tfn_讲座反馈({CurrentUser.编号},{讲座反馈流程模板编号})");

        [HttpGet]
        async public Task<object> 获取讲座反馈详情(int 讲座反馈编号)
        {
            var r1 = await Db.GetModelByIdSpAsync<v3_讲座反馈>(讲座反馈编号);
            var r2 = await MyWorkFlowBusiness.获取某流程的步骤(讲座反馈流程模板编号, 讲座反馈编号);
            return new { 讲座反馈详情 = r1, 审核情况 = r2 };
        }
        #endregion

        #region 参加会议
        /// <summary>
        /// 发起一个参加会议流程
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 发起一个参加会议流程([FromBody]参加会议 model)
        {
            model.申请人编号 = CurrentUser.编号;
            await MyWorkFlowBusiness.发起流程(参加会议流程模板编号, new sp_参加会议_增改() { tt = model.ToDataTable() }, CurrentUser.编号);
        }

        /// <summary>
        /// 完善参加会议申请资料
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 完善参加会议申请资料([FromBody]完成步骤<参加会议> model) =>
            await MyWorkFlowBusiness.完成步骤(model.toSimple(), CurrentUser.编号, new sp_参加会议_增改() { tt = model.附加数据.ToDataTable() });

        [HttpPost]
        async public Task<object> 上传参会相关文件()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/学术活动/参会相关文件",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpGet]
        async public Task<object> 分页获取参加会议(Paging paging, 参加会议Filter filter) =>
            await Db.GetPagingListSpAsync<object, 参加会议Filter>(
                paging,
                filter,
                $"tfn_参加会议({CurrentUser.编号},{参加会议流程模板编号})");

        [HttpGet]
        async public Task<object> 获取参加会议详情(int 参加会议编号)
        {
            var r1 = await Db.GetModelByIdSpAsync<v2_参加会议>(参加会议编号);
            var r2 = await MyWorkFlowBusiness.获取某流程的步骤(参加会议流程模板编号, 参加会议编号);
            return new { 参加会议详情 = r1, 申请情况 = r2 };
        }

        #endregion

        #region 参加会议反馈
        /// <summary>
        /// 暂时不同
        /// 分页获取可反馈的参加会议,供添加参加会议反馈时选择某个参加会议;
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<object> 分页获取可反馈的参加会议(Paging paging, 参加会议Filter filter)
        {
            filter.申请人编号 = CurrentUser.编号;
            //filter.是否反馈过 = false;
            return await Db.GetPagingListSpAsync<object, 参加会议Filter>(
                paging,
                filter,
                $"tfn_参加会议({CurrentUser.编号},{参加会议流程模板编号})");
        }

        [HttpPost]
        async public Task 发起一个参加会议反馈流程([FromBody]参加会议反馈 model)
        {
            await MyWorkFlowBusiness.发起流程(参加会议反馈流程模板编号, new sp_参加会议反馈_增改() { tt = model.ToDataTable() }, CurrentUser.编号);
        }

        /// <summary>
        /// 不幸被打回来重填.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 完善参加会议反馈申请资料([FromBody]完成步骤<参加会议反馈> model) =>
            await MyWorkFlowBusiness.完成步骤(model.toSimple(), CurrentUser.编号, new sp_参加会议反馈_增改() { tt = model.附加数据.ToDataTable() });

        [HttpPost]
        async public Task<object> 上传会议反馈文件()
        {
            return await UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/学术活动/会议反馈文件",
                config.GetValue<int>("uploadFileMaxSize"));
        }

        [HttpGet]
        async public Task<object> 分页获取参加会议反馈(Paging paging, 参加会议反馈Filter filter) =>
            await Db.GetPagingListSpAsync<object, 参加会议反馈Filter>(
                paging,
                filter,
                $"tfn_参加会议反馈({CurrentUser.编号},{参加会议反馈流程模板编号})");

        [HttpGet]
        async public Task<object> 获取参加会议反馈详情(int 参加会议反馈编号)
        {
            var r1 = await Db.GetModelByIdSpAsync<v3_参加会议反馈>(参加会议反馈编号);
            var r2 = await MyWorkFlowBusiness.获取某流程的步骤(参加会议反馈流程模板编号, 参加会议反馈编号);
            return new { 参加会议反馈详情 = r1, 审核情况 = r2 };
        }
        #endregion


    }
}