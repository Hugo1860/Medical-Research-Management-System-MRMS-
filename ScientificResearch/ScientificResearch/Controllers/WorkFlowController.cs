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
using System.Data;

namespace ScientificResearch.Controllers
{
    public class WorkFlowController : BaseController
    {
        /// <summary>
        /// 审核等不需要附加动作的操作,可以直接调用这个接口;
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 审核步骤([FromBody]完成步骤 model) =>
           await MyWorkFlowBusiness.完成步骤(model,CurrentUser.编号);

        /// <summary>
        /// 其中状态值=-2而已
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 终止步骤([FromBody]完成步骤 model) =>
           await MyWorkFlowBusiness.完成步骤(model, CurrentUser.编号);

        /// <summary>评审人提交评分前检查项目利益冲突。</summary>
        [HttpGet]
        async public Task<专家利益冲突检查结果> 预检查专家利益冲突(int 步骤编号) =>
            await MyWorkFlowBusiness.检查专家利益冲突(步骤编号, CurrentUser.编号);

        /// <summary>
        /// 如何判断它确实是分配指定人的确认操作呢?
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 分配步骤指定人([FromBody]完成步骤<IEnumerable<int>> model) =>
            await MyWorkFlowBusiness.完成步骤(
                model.toSimple(), CurrentUser.编号,
                new sp_步骤指定人_增
                {
                    下一步骤编号 = 0,
                    tt = model.附加数据.ToPredefindedKeyFieldsList().ToDataTable()
                });



        /// <summary>
        /// 获取某步骤的可用操作
        /// </summary>
        /// <param name="步骤编号"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<object> 获取某步骤的可用操作(int 步骤编号)
        {
            return await Db.GetListSpAsync<object>($"tfn_步骤_可用操作({步骤编号})");
            //return await MyPubicBusiness.GetListSpAsync<object, object>(null, $"tfn_步骤_可用操作({步骤编号})");
        }
    }
}
