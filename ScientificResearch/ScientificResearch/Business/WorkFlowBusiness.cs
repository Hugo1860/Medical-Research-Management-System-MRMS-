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
    /// 工作流静态方法
    /// 事务要是能挂载到db下就好了
    /// 事务要是能动态构成就更好了
    /// </summary>
    public class WorkFlowBusiness
    {
        #region 构造
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="dbConnectionString"></param>
        public WorkFlowBusiness(string dbConnectionString) { DbConnectionString = dbConnectionString; }
        #endregion

        private string DbConnectionString { get; set; }

        #region 流程
        /// <summary>
        /// 执行一个将返回新建项目编号的sp,同时生成一个流程,再根据这个流程返回的步骤编号完成第一个步骤;
        /// 如果不完成第一个步骤,其实就相当于暂存;
        /// </summary>
        /// <param name="流程模板编号"></param>
        /// <param name="model"></param>
        /// <param name="sql"></param>
        /// <param name="isHold"></param>
        /// <param name="发起人编号">不是有项目发起人发起的流程时,需要填写一下发起人,比如纵向立项登记时</param>
        /// <returns></returns>
        async public Task 发起流程<T>(int 流程模板编号, T model, int 操作人编号, bool isHold = false, int? 发起人编号 = null)
        {
            //string sql = PredefinedSpExtention.GetMergeSpNameByClassName<T>();
            using (var dbForTransaction = new NpgsqlConnection(DbConnectionString))
            {
                dbForTransaction.Open();
                using (var transaction = dbForTransaction.BeginTransaction())
                {
                    try
                    {
                        //这个sp,必须返回一个int类型的编号,作为流程的项目编号
                        var resultOfNewProject = await dbForTransaction.QuerySpAsync<T, int>(
                            model,
                            transaction);
                        var 项目编号 = resultOfNewProject.FirstOrDefault();
                        if (项目编号 == 0)
                        {
                            throw new Exception("发起流程出错:项目生成失败");
                        }

                        //发起流程 
                        var resultNewWorkFlow = await dbForTransaction.QuerySpAsync<sp_流程_发起, int>(new sp_流程_发起
                        {
                            流程模板编号 = 流程模板编号,
                            项目编号 = 项目编号,
                            发起人编号 = 发起人编号 ?? 操作人编号
                        }, transaction);
                        var 步骤编号 = resultNewWorkFlow.FirstOrDefault();
                        if (步骤编号 == 0)
                        {
                            throw new Exception("发起流程出错:流程生成失败");
                        }

                        if (!isHold)
                        {
                            //完成这个步骤
                            await dbForTransaction.ExecuteSpAsync(new sp_流程_完成步骤
                            {
                                步骤编号 = 步骤编号,
                                状态 = 1,
                                操作人 = 操作人编号,
                                备注 = string.Empty
                            }, transaction);
                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        #endregion

        #region 步骤
        /// <summary>
        /// 没有附加动作的完成步骤
        /// </summary>
        /// <param name="model"></param>
        /// <param name="操作人编号"></param>
        /// <returns></returns>
        async public Task 完成步骤(完成步骤 model, int 操作人编号)
        {
            await 完成步骤<object>(model, 操作人编号);
        }

        /// <summary>
        /// 完成一个步骤
        /// 针对不需要附加动作的步骤操作,T随便输个object=null,sql那里=null或者不输就行了
        /// 可以把sql,param包成一个对象,再放到list里面,也就是可以多个附加动作;
        /// </summary>
        /// <param name="step"></param>
        /// <param name="操作人编号"></param>
        /// <param name="model"></param>
        /// <param name="isHold"></param>
        /// <param name="AdditionalActionsWhenState">当前操作状态值=多少时,才执行附加动作,默认null</param>
        /// <returns></returns>
        // Optional binding is used by vertical lifecycle writes. The table is selected
        // from a server-side allowlist, never from a request-provided identifier.
        public sealed class 纵向步骤绑定
        {
            public string 阶段 { get; set; }
            public int 流程模板编号 { get; set; }
            public int 子记录编号 { get; set; }
            public int? 立项编号 { get; set; }
        }

        async public Task 完成步骤<T>(完成步骤 step, int 操作人编号, T model = null, bool isHold = false, int? AdditionalActionsWhenState = null, 纵向步骤绑定 绑定 = null) where T : class
        {
            //var sql = model == null ? string.Empty : typeof(T).Name;
            using (var dbForTransaction = new NpgsqlConnection(DbConnectionString))
            {
                dbForTransaction.Open();
                using (var transaction = dbForTransaction.BeginTransaction())
                {
                    try
                    {
                        var 下一步骤编号 = 0;

                        if (绑定 != null)
                        {
                            if (绑定.子记录编号 <= 0 || (绑定.立项编号.HasValue && 绑定.立项编号.Value <= 0))
                                throw new ForbiddenAccessException("流程步骤与纵向项目记录不匹配");
                            var table = 绑定.阶段 switch
                            {
                                "申报" => "\"纵向项目申报\"",
                                "立项" => "\"纵向项目立项\"",
                                "中检" => "\"纵向项目中检\"",
                                "变更" => "\"纵向项目变更\"",
                                "结题" => "\"纵向项目结题\"",
                                _ => throw new ArgumentException("未知纵向项目阶段")
                            };
                            // Lock all participating rows until the workflow action and
                            // accompanying write commit, preventing a concurrent rebind.
                            var root = 绑定.阶段 == "申报" || 绑定.阶段 == "立项";
                            var sql = root ? @"
SELECT entity.""编号""
  FROM ""步骤"" step
  JOIN ""流程"" flow ON flow.""编号""=step.""流程编号""
  JOIN " + table + @" entity ON entity.""编号""=flow.""项目编号""
 WHERE step.""编号""=@stepId AND step.""状态""=0
   AND flow.""流程模板编号""=@templateId AND entity.""编号""=@childId
 FOR UPDATE OF step, flow, entity;" : @"
SELECT child.""纵向项目编号""
  FROM ""步骤"" step
  JOIN ""流程"" flow ON flow.""编号""=step.""流程编号""
  JOIN " + table + @" child ON child.""编号""=flow.""项目编号""
  JOIN ""纵向项目立项"" parent ON parent.""编号""=child.""纵向项目编号""
 WHERE step.""编号""=@stepId AND step.""状态""=0
   AND flow.""流程模板编号""=@templateId AND child.""编号""=@childId
 FOR UPDATE OF step, flow, child, parent;";
                            var boundId = await dbForTransaction.QuerySingleOrDefaultAsync<int?>(sql,
                                new { stepId = step.步骤编号, templateId = 绑定.流程模板编号, childId = 绑定.子记录编号 }, transaction);
                            if (!boundId.HasValue || (!root && 绑定.立项编号.HasValue && boundId.Value != 绑定.立项编号.Value))
                                throw new ForbiddenAccessException("流程步骤与纵向项目记录不匹配");
                        }

                        // 查询函数不仅用于展示待办；流程写入前必须再次在事务内校验
                        // 当前操作人，避免直接调用完成接口绕过步骤指定人/角色限制。
                        var canOperate = await dbForTransaction.QuerySingleAsync<bool>(
                            "SELECT \"fn_某人是否可以操作某步骤\"(@步骤编号,@人员编号);",
                            new { 步骤编号 = step.步骤编号, 人员编号 = 操作人编号 },
                            transaction);
                        if (!canOperate)
                        {
                            throw new ForbiddenAccessException("当前人员无权操作该流程步骤");
                        }

                        if (!isHold)   //可以完成步骤,即不暂存;
                        {
                            var result = await dbForTransaction.QuerySpAsync<sp_流程_完成步骤, int>(new sp_流程_完成步骤
                            {
                                步骤编号 = step.步骤编号,
                                状态 = step.状态值,
                                备注 = step.备注,
                                操作人 = 操作人编号
                            }, transaction);

                            下一步骤编号 = result.FirstOrDefault();
                        }

                        //有附加动作,且操作结果的状态值等于指定的结果值;如果指定的结果值为null,则不限制
                        if (model != null && step.状态值 == (AdditionalActionsWhenState ?? step.状态值))
                        {
                            //如果有下一步骤编号这个需要的参数,且确实有下一步,则传入这个参数;比如设置下一步的负责人
                            if (model.ContainProperty("下一步骤编号") && 下一步骤编号 > 0)
                            {
                                model.SetValueByPropertyName("下一步骤编号", 下一步骤编号);
                                //model.SetValueByPropertyName<int>("下一步骤编号", 下一步骤编号);
                            }
                            
                            if (model.ContainProperty("下一步骤编号") && isHold)
                            {
                                throw new Exception("需要设置下一步骤的操作不能暂存");
                            }

                            if (model.ContainProperty("下一步骤编号") && 下一步骤编号 == 0)
                            {
                                throw new Exception("生成下一步骤失败");
                            }

                            await dbForTransaction.ExecuteSpAsync(model, transaction);

                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 附加数据携带的是流程模板编号
        /// 这个放在这里供各个不同流程模板编号的调用,不同于"分配步骤指定人"可以统一提供一个接口
        /// DOTO:这个应该放到ProjectBusiness里面去
        /// </summary>
        /// <param name="model"></param>
        /// <param name="流程模板编号"></param>
        /// <returns></returns>
        async public Task<专家利益冲突检查结果> 检查专家利益冲突(int 步骤编号, int 操作人编号)
        {
            using var db = new NpgsqlConnection(DbConnectionString);
            var result = await db.QuerySingleOrDefaultAsync<dynamic>(@"
WITH ctx AS (
    SELECT f.""流程模板编号"", f.""项目编号""
    FROM ""步骤"" s
    JOIN ""流程"" f ON f.""编号"" = s.""流程编号""
    WHERE s.""编号"" = @步骤编号 AND s.""状态"" = 0
), project_scope AS (
    SELECT 410 AS base_template, c.""项目编号"" AS base_project FROM ctx c WHERE c.""流程模板编号"" = 410
    UNION ALL
    SELECT 410, m.""纵向项目编号"" FROM ctx c JOIN ""纵向项目中检"" m ON c.""流程模板编号"" = 420 AND c.""项目编号"" = m.""编号""
    UNION ALL
    SELECT 410, m.""纵向项目编号"" FROM ctx c JOIN ""纵向项目变更"" m ON c.""流程模板编号"" = 430 AND c.""项目编号"" = m.""编号""
    UNION ALL
    SELECT 410, m.""纵向项目编号"" FROM ctx c JOIN ""纵向项目结题"" m ON c.""流程模板编号"" = 440 AND c.""项目编号"" = m.""编号""
    UNION ALL
    SELECT 450, c.""项目编号"" FROM ctx c WHERE c.""流程模板编号"" = 450
    UNION ALL
    SELECT 450, m.""横向项目编号"" FROM ctx c JOIN ""横向项目变更"" m ON c.""流程模板编号"" = 460 AND c.""项目编号"" = m.""编号""
    UNION ALL
    SELECT 450, m.""横向项目编号"" FROM ctx c JOIN ""横向项目结题"" m ON c.""流程模板编号"" = 470 AND c.""项目编号"" = m.""编号""
), involved AS (
    SELECT y.""申报人编号"" AS person_id
    FROM ctx c JOIN ""纵向项目申报"" y ON c.""流程模板编号"" = 400 AND c.""项目编号"" = y.""编号""
    UNION ALL
    SELECT p.""参与人编号""
    FROM ctx c JOIN ""项目参与人"" p ON c.""流程模板编号"" = 400 AND p.""流程模板编号"" = 400 AND p.""项目编号"" = c.""项目编号""
    UNION ALL
    SELECT y.""负责人编号"" FROM project_scope s JOIN ""纵向项目立项"" y ON s.base_template = 410 AND y.""编号"" = s.base_project
    UNION ALL
    SELECT y.""登记人编号"" FROM project_scope s JOIN ""纵向项目立项"" y ON s.base_template = 410 AND y.""编号"" = s.base_project
    UNION ALL
    SELECT x.""项目负责人编号"" FROM project_scope s JOIN ""横向项目"" x ON s.base_template = 450 AND x.""编号"" = s.base_project
    UNION ALL
    SELECT p.""参与人编号"" FROM project_scope s JOIN ""项目参与人"" p ON p.""流程模板编号"" = s.base_template AND p.""项目编号"" = s.base_project
)
SELECT COALESCE((SELECT ""流程模板编号"" FROM ctx), 0) AS template_id,
       COALESCE((SELECT ""项目编号"" FROM ctx), 0) AS project_id,
       EXISTS (SELECT 1 FROM involved WHERE person_id = @操作人编号) AS conflict;",
                new { 步骤编号, 操作人编号 });
            if (result == null)
                return new 专家利益冲突检查结果 { 通过 = false, 说明 = "评审步骤不存在、已完成或无权操作" };

            var conflict = (bool)result.conflict;
            return new 专家利益冲突检查结果
            {
                通过 = !conflict && (int)result.template_id > 0,
                流程模板编号 = (int)result.template_id,
                项目编号 = (int)result.project_id,
                说明 = conflict ? "评审人属于项目负责人、申报人或参与人，必须回避" : "未发现评审人与项目团队的直接利益冲突"
            };
        }

        async public Task 专家评审(完成步骤<IEnumerable<专家评分>> model, int 操作人编号, int 流程模板编号)
        {
            if (model == null || model.附加数据 == null)
                throw new ArgumentException("缺少专家评审数据");
            var conflictCheck = await 检查专家利益冲突(model.步骤编号, 操作人编号);
            if (!conflictCheck.通过)
                throw new ForbiddenAccessException(conflictCheck.说明);
            await 完成步骤(
                model.toSimple(), 操作人编号,
                new sp_专家评分_增改
                {
                    流程模板编号 = 流程模板编号,
                    步骤编号 = model.步骤编号,
                    tt = model.附加数据.ToDataTable()
                });
        }
        #endregion

        /// <summary>
        /// 获取某流程的步骤
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="流程模板编号"></param>
        /// <param name="项目编号"></param>
        /// <returns></returns>
        async public Task<IEnumerable<v2_步骤>> 获取某流程的步骤(int 流程模板编号, int 项目编号)
        {
            var Db = new NpgsqlConnection(DbConnectionString);
            return await Db.GetListSpAsync<v2_步骤>($"tfn_步骤({流程模板编号},{项目编号})", orderType: true);
        }
    }
}
