using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using ScientificResearch.Business;
using ScientificResearch.Infrastucture;
using ScientificResearch.Models;

using MyLib;
using Microsoft.Extensions.Configuration;
using System.Data;


namespace ScientificResearch.Controllers
{
    public class OfficeController : BaseController
    {
        /// <summary>
        /// 接收者类型:
        /// 0 所有人 该情况下, 接收者编号也 = 0;
        /// 10 -人员;
        /// 20 -部门;
        /// 30 -角色;
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 新增通知公告([FromBody]通知公告增改 model)
        {
            model.基本资料.发送人编号 = CurrentUser.编号;
            if (model.基本资料.关闭时间 <= DateTime.Now)
            {
                throw new Exception("关闭时间不能是过去的时间");
            }

            if (model.基本资料.编号 != 0 && model.基本资料.发送人编号 != CurrentUser.编号)
            {
                throw new Exception("只能修改自己发布的通知公告");
            }

            await Db.ExecuteSpAsync(new sp_通知公告_增改()
            {
                tt = model.基本资料.ToDataTable(),
                tt_通知公告接收条件 = model.通知公告接收条件列表.ToDataTable()
            });
        }

        [HttpPost]
        async public Task 启禁通知公告([FromBody]通知公告启禁 model)
        {
            if (model.发送人编号 != CurrentUser.编号)
            {
                throw new Exception("只能启用或禁用自己发布的通知公告");
            }

            await Db.ExecuteSpAsync(new sp_通知公告_启禁()
            {
                编号 = model.编号,
                是否启用 = model.是否启用
            });
        }

        [HttpGet]
        async public Task<object> 分页获取通知公告(Paging paging, 通知公告Filter filter)
        {
            return await Db.GetPagingListSpAsync<v2_通知公告_列表, 通知公告Filter>(paging, filter);
        }


        [HttpGet]
        async public Task<object> 分页获取某人员可查看的通知公告(int 人员编号, Paging paging, 某人员可查看的通知公告Filter filter)
        {
            return await Db.GetPagingListSpAsync<object, 某人员可查看的通知公告Filter>(paging, filter, $"tfn_某人员可查看的通知公告({人员编号})");
        }


        [HttpGet]
        async public Task<object> 获取通知公告详情(int 编号, bool 是否已接收)
        {
            var model = await Db.GetModelByIdSpAsync<v2_通知公告>(编号);
            if (!是否已接收)
            {
                await Db.ExecuteSpAsync(new sp_通知公告_接收()
                {
                    通知公告编号 = 编号,
                    接收人编号 = CurrentUser.编号
                });
            }

            var list = await Db.GetListSpAsync<v1_通知公告接收条件, 通知公告接收条件Filter>(new 通知公告接收条件Filter() { 通知公告编号 = 编号 });

            return new
            {
                通知公告 = model,
                接收条件 = list
            };
        }

        [HttpPost]
        async public Task<object> 上传通知公告文件()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/办公/通知公告文件",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpPost]
        async public Task<object> 上传通知公告图片()
        {
            var file = Request.Form.Files.FirstOrDefault();
            if(file == null)
            {
                throw new Exception("没有上传文件");
            }

            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/办公/通知公告图片",
                config.GetValue<int>("uploadFileMaxSize"));

            var obj = new
            {
                state = "SUCCESS",
                url = filesNameList.FirstOrDefault(),
                //title = "new.png",
                //original = file.FileName,
                //type = ".png",
                //size = 1000

            };

            var objString = Newtonsoft.Json.JsonConvert.SerializeObject(obj);

            return Content(objString,"text/html");
            
            //return filesNameList.FirstOrDefault();
        }

        [HttpPost]
        async public Task 增改下载([FromBody]下载中心 model)
        {
            model.上传人编号 = CurrentUser.编号;

            //if (model.编号 != 0 && model.上传人编号 != CurrentUser.编号)
            //{
            //    throw new Exception("只能修改自己上传的下载文件");
            //}

            await Db.ExecuteSpAsync(new sp_下载中心_增改()
            {
                tt = model.ToDataTable()
            });
        }

        [HttpPost]
        async public Task<object> 上传下载文件()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/下载中心",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpPost]
        async public Task 启禁下载([FromBody]下载中心启禁 model)
        {
            //if (model.上传人编号 != CurrentUser.编号)
            //{
            //    throw new Exception("只能启用或禁用自己上传的下载文件");
            //}

            await Db.ExecuteSpAsync(new sp_下载中心_启禁()
            {
                编号 = model.编号,
                是否启用 = model.是否启用
            });
        }

        /// <summary>
        /// 首页查看有下载中心时用
        /// 分页获取某人员可查看的下载中心,由于所有信息都包含了,所以没有获取详情的接口了
        /// </summary>
        /// <param name="人员编号"></param>
        /// <param name="paging"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<object> 分页获取可查看的下载中心(Paging paging, 下载中心Filter filter)
        {
            filter.是否启用 = true;
            return await Db.GetPagingListSpAsync<v2_下载中心, 下载中心Filter>(paging, filter);
        }

        /// <summary>
        /// 管理下载中心时用
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<object> 分页获取下载中心(Paging paging, 下载中心Filter filter)
        {
            return await Db.GetPagingListSpAsync<v2_下载中心, 下载中心Filter>(paging, filter);
        }

        private enum 成果类型
        {
            论文 = 1, 著作 = 2, 专利 = 3, 获奖 = 4
        }

        [HttpGet]
        async public Task<object> 获取科研详情(int 当前登录人编号)
        {
            if (当前登录人编号 <= 0) 当前登录人编号 = CurrentUser.编号;

            var r1 = await Db.GetModelByIdSpAsync<v1_人员_带部门名>(当前登录人编号);

            var 纵向项目总览filter = new 纵向项目总览Filter() { 发起人编号 = 当前登录人编号 };
            var r2 = (await Db.GetListSpAsync<v4_纵向项目总览, 纵向项目总览Filter>(纵向项目总览filter))?.ToList() ?? new List<v4_纵向项目总览>();

            var 横向项目filter = new 横向项目Filter() { 发起人编号 = 当前登录人编号 };
            var r3 = (await Db.GetListSpAsync<v4_横向项目总览, 横向项目Filter>(横向项目filter))?.ToList() ?? new List<v4_横向项目总览>();

            var 成果总览filter = new 成果总览Filter() { 发起人编号 = 当前登录人编号 };
            var r4 = (await Db.GetListSpAsync<v4_成果总览, 成果总览Filter>(成果总览filter))?.ToList() ?? new List<v4_成果总览>();

            // 补充：人员科研能力与标签
            var capability = await Db.QuerySingleOrDefaultAsync<object>(@"
SELECT ""人员编号"", ""研究专长"", ""研究方向标签"", ""技术服务能力"", ""合作意愿"", ""合作说明"", ""可用时间"", ""每周可用小时"", ""可承担项目数"", ""当前可接项目"", ""可公开展示""
  FROM ""人员科研能力"" WHERE ""人员编号""=@当前登录人编号;", new { 当前登录人编号 });

            var tags = await Db.QueryAsync<object>(@"
SELECT ""编号"", ""标签类型"", ""标签"", ""来源""
  FROM ""人员能力标签"" WHERE ""人员编号""=@当前登录人编号
 ORDER BY ""标签类型"", ""标签"";", new { 当前登录人编号 });

            var mentor = (await Db.QueryAsync<object>(@"
SELECT r.""编号"", r.""导师编号"", m.""姓名"" AS ""导师姓名"", r.""学员编号"", s.""姓名"" AS ""学员姓名"", r.""关系类型"", r.""开始日期"", r.""结束日期"", r.""状态"", r.""备注""
  FROM ""人员导师关系"" r JOIN ""人员"" m ON m.""编号""=r.""导师编号"" JOIN ""人员"" s ON s.""编号""=r.""学员编号""
 WHERE r.""导师编号""=@当前登录人编号 OR r.""学员编号""=@当前登录人编号 ORDER BY r.""状态"", r.""开始日期"" DESC NULLS LAST;", new { 当前登录人编号 }))?.ToList() ?? new List<object>();

            var teams = (await Db.QueryAsync<object>(@"
SELECT t.""编号"", t.""团队名称"", t.""负责人编号"", t.""研究方向"", t.""技术服务范围"", t.""合作说明"", t.""状态"", m.""成员角色"", m.""加入日期"", m.""状态"" AS ""成员状态""
  FROM ""科研团队成员"" m JOIN ""科研团队"" t ON t.""编号""=m.""团队编号""
 WHERE m.""人员编号""=@当前登录人编号 ORDER BY t.""团队名称"";", new { 当前登录人编号 }))?.ToList() ?? new List<object>();

            var assignments = (await Db.QueryAsync<object>(@"
SELECT a.""流程模板编号"", a.""项目编号"", a.""分工类型"", a.""工作量占比"", a.""开始日期"", a.""结束日期"", a.""状态"", a.""备注""
  FROM ""人员项目分工"" a
 WHERE a.""人员编号""=@当前登录人编号
 ORDER BY a.""状态"", a.""开始日期"" DESC NULLS LAST;", new { 当前登录人编号 }))?.ToList() ?? new List<object>();

            // 历年成果统计
            var fruitYearStats = await Db.QueryAsync<dynamic>(@"
SELECT EXTRACT(YEAR FROM a.""创建时间"")::int AS ""年度"",
       COUNT(*) FILTER (WHERE a.""流程模板编号"" IN (300, 310))::int AS ""论文数"",
       COUNT(*) FILTER (WHERE a.""流程模板编号"" = 302)::int AS ""专利数"",
       COUNT(*) FILTER (WHERE a.""流程模板编号"" = 301)::int AS ""著作数"",
       COUNT(*) FILTER (WHERE a.""流程模板编号"" = 303)::int AS ""获奖数""
  FROM ""v3_流程"" a
 WHERE a.""发起人编号"" = @当前登录人编号 AND a.""流程模板编号"" BETWEEN 300 AND 399
 GROUP BY EXTRACT(YEAR FROM a.""创建时间"")
 ORDER BY ""年度"" ASC;", new { 当前登录人编号 });

            // 历年立项与经费趋势计算 (近 5-6 年)
            var currentYear = DateTime.Now.Year;
            var startYear = currentYear - 5;
            var yearDict = new SortedDictionary<int, YearTrendItem>();
            for (int y = startYear; y <= currentYear; y++)
            {
                yearDict[y] = new YearTrendItem
                {
                    年度 = y.ToString(),
                    纵向课题数 = 0,
                    纵向经费 = 0.0m,
                    横向合同数 = 0,
                    横向经费 = 0.0m,
                    论文数 = 0,
                    专利数 = 0,
                    著作数 = 0,
                    获奖数 = 0
                };
            }

            foreach (var v in r2)
            {
                int vy = v.计划开始日期 != default ? v.计划开始日期.Year : currentYear;
                if (yearDict.ContainsKey(vy))
                {
                    yearDict[vy].纵向课题数++;
                    yearDict[vy].纵向经费 += Math.Round((v.批准经费 ?? 0) / 10000m, 2);
                }
            }

            foreach (var h in r3)
            {
                int hy = h.签订时间 != default ? h.签订时间.Year : currentYear;
                if (yearDict.ContainsKey(hy))
                {
                    yearDict[hy].横向合同数++;
                    yearDict[hy].横向经费 += Math.Round(h.合同金额 / 10000m, 2);
                }
            }

            foreach (var f in fruitYearStats)
            {
                if (f?.年度 != null)
                {
                    int fy = (int)f.年度;
                    if (yearDict.ContainsKey(fy))
                    {
                        yearDict[fy].论文数 += (int)(f.论文数 ?? 0);
                        yearDict[fy].专利数 += (int)(f.专利数 ?? 0);
                        yearDict[fy].著作数 += (int)(f.著作数 ?? 0);
                        yearDict[fy].获奖数 += (int)(f.获奖数 ?? 0);
                    }
                }
            }

            var capabilityScores = ResearcherCapabilityEvaluator.Evaluate(
                当前登录人编号,
                r2,
                r3,
                r4,
                mentor.Count,
                teams.Count);

            return new
            {
                个人资料 = r1,
                纵向项目总览 = r2,
                横向项目总览 = r3,
                成果总览 = r4,
                科研能力 = capability,
                能力标签 = tags,
                导师关系 = mentor,
                团队 = teams,
                项目分工 = assignments,
                能力画像量化 = capabilityScores,
                历年产出趋势 = yearDict.Values.ToList()
            };
        }

        [HttpGet]
        async public Task<object> 获取待办流程(Paging paging)
        {
            return await Db.GetPagingListSpAsync<object, 流程Filter>(
                paging,
                new 流程Filter()
                {
                    是否可处理 = true
                },
                $"tfn_流程(NULL,NULL,{CurrentUser.编号})",
                "流程编号");
        }

        /// <summary>获取专家评审待办。专家看到的申请人信息在服务端脱敏，避免仅依赖前端隐藏实现匿名评审。</summary>
        [HttpGet]
        async public Task<object> 获取专家评审待办(Paging paging)
        {
            if (!await HasAnyRoleAsync("专家", "科研管理员", "系统管理员"))
                return StatusCode(403, new { error = "无权查看专家评审待办" });

            var result = await Db.GetPagingListSpAsync<object, 流程Filter>(
                paging,
                new 流程Filter { 是否可处理 = true },
                $"tfn_流程(NULL,NULL,{CurrentUser.编号})",
                "流程编号");

            var 匿名列表 = (result.List ?? Enumerable.Empty<object>()).Select(row =>
            {
                if (row is IDictionary<string, object> values)
                {
                    var masked = new Dictionary<string, object>(values, StringComparer.OrdinalIgnoreCase)
                    {
                        ["发起人姓名"] = "匿名申请人",
                        ["发起人工号"] = null,
                        ["发起人部门名称"] = null,
                        ["匿名评审"] = true
                    };
                    return (object)masked;
                }
                return row;
            }).ToList();

            return new PagingResult<object> { Total = result.Total, List = 匿名列表 };
        }

        /// <summary>
        /// 获取当前人员的统一工作台摘要：待办、未读通知、发起中的流程和下一步事项。
        /// </summary>
        [HttpGet]
        async public Task<object> 获取科研工作台摘要()
        {
            return await Db.QueryFirstAsync<object>(
                "SELECT * FROM \"tfn_科研工作台摘要_v2\"(@人员编号);",
                new { 人员编号 = CurrentUser.编号 });
        }

    }


}
