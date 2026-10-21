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
    public class XProjectController : BaseController
    {
        #region 横向项目申请
        public int 横向项目申请流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:横向项目申请");
            }
        }

        /// <summary>
        /// 注:横向项目经费预算中的配套经费设为0,没用
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 新增横向项目申请([FromBody]横向项目申请增改 model)
        {
            if (!验证横向项目申请增改资料(model))
            {
                throw new Exception("经费预算总额与合同金额不符");
            }

            await MyWorkFlowBusiness.发起流程(
                            横向项目申请流程模板编号,
                            new sp_横向项目_增改()
                            {
                                流程模板编号 = 横向项目申请流程模板编号,
                                tt = model.基本资料.ToDataTable(),
                                tt_项目参与人 = model.参与人列表.ToDataTable(),
                                tt_项目经费预算 = model.经费预算列表.ToDataTable()
                            },
                            CurrentUser.编号,
                            model.IsHold);
        }

        /// <summary>
        /// 注:横向项目经费预算中的配套经费设为0,没用
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 完善横向项目申请资料([FromBody]完成步骤<横向项目申请增改> model)
        {
            if(!验证横向项目申请增改资料(model.附加数据))
            {
                throw new Exception("经费预算总额与合同金额不符");
            }

            await MyWorkFlowBusiness.完成步骤(
                model.toSimple(), CurrentUser.编号,
                new sp_横向项目_增改()
                {
                    流程模板编号 = 横向项目申请流程模板编号,
                    tt = model.附加数据.基本资料.ToDataTable(),
                    tt_项目参与人 = model.附加数据.参与人列表.ToDataTable(),
                    tt_项目经费预算 = model.附加数据.经费预算列表.ToDataTable()
                }, model.附加数据.IsHold);
        }

        private bool 验证横向项目申请增改资料(横向项目申请增改 model)
        {
            return model.基本资料.合同金额 == model.经费预算列表.Sum(i => i.批准经费) ? true : false;
        }

        [HttpPost]
        async public Task<object> 上传合同认定登记承诺书()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/横向项目/合同认定登记承诺书",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpPost]
        async public Task<object> 上传合同文件()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/横向项目/合同文件",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpGet]
        async public Task<object> 分页获取横向项目(Paging paging, 横向项目Filter filter)
        {
            return await Db.GetPagingListSpAsync<object, 横向项目Filter>(paging, filter, $"tfn_横向项目({CurrentUser.编号},{横向项目申请流程模板编号})");
        }

        [HttpGet]
        async public Task<object> 获取横向项目详情(int 横向项目编号)
        {
            if (!await CanAccessProjectAsync(横向项目申请流程模板编号, 横向项目编号))
            {
                return ProjectAccessDenied();
            }
            var r1 = await Db.GetModelByIdSpAsync<v2_横向项目_详情>(横向项目编号);
            var r2 = await Db.GetListSpAsync<object>($"tfn_项目参与人({横向项目申请流程模板编号},{横向项目编号})");
            var r3 = await Db.GetListSpAsync<object>($"tfn_项目经费预算({横向项目申请流程模板编号},{横向项目编号})");
            var r6 = await MyWorkFlowBusiness.获取某流程的步骤(横向项目申请流程模板编号, 横向项目编号);
            return new { 基本信息 = r1, 参与人信息 = r2, 财务信息 = r3, 审核记录 = r6 };
        }
        #endregion

        #region 横向项目变更
        public int 横向项目变更流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:横向项目变更");
            }
        }

        [HttpPost]
        async public Task<object> 新增横向项目变更([FromBody]横向项目变更增改 model)
        {
            if (model?.基本资料 == null || model.基本资料.横向项目编号 <= 0) throw new ArgumentException("缺少变更基本资料");
            if (!await CanAccessProjectAsync(横向项目申请流程模板编号, model.基本资料.横向项目编号))
                return ProjectAccessDenied();
            var state = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT EXISTS (SELECT 1 FROM ""横向项目结题"" WHERE ""横向项目编号""=@项目编号) AS closed,
       COALESCE((SELECT e.""事件类型"" FROM ""横向项目生命周期申请"" e WHERE e.""横向项目编号""=@项目编号 AND e.""状态""='已批准' ORDER BY e.""审核时间"" DESC NULLS LAST, e.""编号"" DESC LIMIT 1),'正常') AS current_status
  FROM ""横向项目"" WHERE ""编号""=@项目编号;", new { 项目编号 = model.基本资料.横向项目编号 });
            if (state == null) return NotFound(new { error = "横向项目不存在" });
            if ((bool)state.closed || (string)state.current_status is "终止" or "暂停")
                throw new ArgumentException("项目已结题、终止或暂停，不能新增合同变更");
            await MyWorkFlowBusiness.发起流程(
                横向项目变更流程模板编号,
                new sp_横向项目变更_增改 { tt = model.基本资料.ToDataTable() },
                CurrentUser.编号,
                model.IsHold);
            return new { success = true };
        }

        [HttpPost]
        async public Task<object> 完善横向项目变更资料([FromBody]完成步骤<横向项目变更增改> model)
        {
            if (model?.附加数据?.基本资料 == null || model.附加数据.基本资料.横向项目编号 <= 0)
                throw new ArgumentException("缺少变更基本资料");
            if (!await CanAccessProjectAsync(横向项目申请流程模板编号, model.附加数据.基本资料.横向项目编号))
                return ProjectAccessDenied();
            await MyWorkFlowBusiness.完成步骤(
                model.toSimple(), CurrentUser.编号,
                new sp_横向项目变更_增改 { tt = model.附加数据.基本资料.ToDataTable() }, model.附加数据.IsHold);
            return new { success = true };
        }

        [HttpPost]
        async public Task 审核并应用横向项目变更([FromBody]完成步骤<int> model)
        {
            var 操作人编号 = await ResolveOperationUserAsync("项目审批");
            await MyWorkFlowBusiness.完成步骤(
                model.toSimple(), 操作人编号,
                new sp_横向项目变更_应用变更()
                {
                    横向项目变更编号 = model.附加数据
                },
                AdditionalActionsWhenState: 1);
        }

        [HttpGet]
        async public Task<object> 分页获取横向项目变更(Paging paging, 横向项目变更Filter filter) =>
            await Db.GetPagingListSpAsync<object, 横向项目变更Filter>(
                paging,
                filter,
                $"tfn_横向项目变更({CurrentUser.编号},{横向项目申请流程模板编号},{横向项目变更流程模板编号})");

        [HttpGet]
        async public Task<object> 获取横向项目变更详情(int 横向项目编号, int 横向项目变更编号, int? 状态)
        {
            if (!await CanAccessProjectAsync(横向项目申请流程模板编号, 横向项目编号))
            {
                return ProjectAccessDenied();
            }
            var r1 = await Db.GetModelByIdSpAsync<v2_横向项目_详情>(横向项目编号);
            var r2 = await Db.GetModelByIdSpAsync<v2_横向项目变更_详情>(横向项目变更编号);

            var r3 = new v2_横向项目_详情();
            Tool.ModelToModel(r3, r2);
            r3.编号 = r2.横向项目编号;

            var r4 = await MyWorkFlowBusiness.获取某流程的步骤(横向项目变更流程模板编号, 横向项目变更编号);

            return new
            {
                r2.编号,
                r2.变更内容简介,
                r2.变更事由,
                r2.变更备注,
                r2.变更时间,
                变更前 = 状态 == 1 ? r3 : r1,
                变更后 = 状态 == 1 ? r1 : r3,
                审核记录 = r4
            };
        }
        #endregion

        #region 横向项目结题
        public int 横向项目结题流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:横向项目结题");
            }
        }

        /// <summary>
        /// 发起一个横向项目结题
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 新增横向项目结题([FromBody]横向项目结题增改 model)
        {
            var newModel = new 横向项目结题() { 横向项目编号 = model.横向项目申请编号 };
            await MyWorkFlowBusiness.发起流程(
                横向项目结题流程模板编号,
                new sp_横向项目结题_增改()
                {
                    tt = newModel.ToDataTable()
                },
                CurrentUser.编号,
                true,
                model.申请发起人编号);
        }

        [HttpPost]
        async public Task 完善横向项目结题资料([FromBody]完成步骤<横向项目结题完善> model) =>
            await MyWorkFlowBusiness.完成步骤(
                model.toSimple(), CurrentUser.编号,
                new sp_横向项目结题_增改()
                {
                    tt = model.附加数据.基本资料.ToDataTable()
                }, model.附加数据.IsHold);

        [HttpPost]
        async public Task<object> 上传结题资料()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/横向项目/结题资料",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpGet]
        async public Task<object> 分页获取横向项目结题(Paging paging, 横向项目结题Filter filter) =>
            await Db.GetPagingListSpAsync<object, 横向项目结题Filter>(
                paging,
                filter,
                $"tfn_横向项目结题({CurrentUser.编号},{横向项目申请流程模板编号},{横向项目变更流程模板编号},{横向项目结题流程模板编号})");

        [HttpGet]
        async public Task<object> 获取横向项目结题详情(int 横向项目申请编号, int 横向项目结题编号)
        {
            if (!await CanAccessProjectAsync(横向项目申请流程模板编号, 横向项目申请编号))
            {
                return ProjectAccessDenied();
            }
            var r1 = await Db.GetModelByIdSpAsync<v2_横向项目_详情>(横向项目申请编号);
            var r2 = await Db.GetListSpAsync<object>($"tfn_项目参与人({横向项目申请流程模板编号},{横向项目申请编号})");
            var r3 = await Db.GetListSpAsync<object>($"tfn_项目经费预算({横向项目申请流程模板编号},{横向项目申请编号})");
            var r4 = await Db.GetModelByIdSpAsync<横向项目结题>(横向项目结题编号);
            var r5 = await MyWorkFlowBusiness.获取某流程的步骤(横向项目结题流程模板编号, 横向项目结题编号);
            return new
            {
                项目信息 = r1,
                参与人信息 = r2,
                财务信息 = r3,
                项目结题信息 = r4,
                结题审核记录 = r5
            };

        }
        #endregion

        #region 横向项目生命周期与合同档案
        /// <summary>返回横向合同的延期、暂停、终止申请、研究报告、付款节点和档案材料。</summary>
        [HttpGet]
        async public Task<object> 获取横向项目生命周期档案(int 横向项目编号)
        {
            if (!await CanAccessProjectAsync(横向项目申请流程模板编号, 横向项目编号))
                return ProjectAccessDenied();
            var 项目 = await Db.QuerySingleOrDefaultAsync<object>(@"
SELECT p.""编号"", p.""合同编号"", p.""合同名称"", p.""项目负责人编号"", p.""合同开始时间"", p.""合同结束时间"",
       COALESCE((SELECT e.""事件类型"" FROM ""横向项目生命周期申请"" e
                  WHERE e.""横向项目编号"" = p.""编号"" AND e.""状态"" = '已批准'
                  ORDER BY e.""审核时间"" DESC NULLS LAST, e.""编号"" DESC LIMIT 1), '正常') AS ""当前状态""
  FROM ""横向项目"" p WHERE p.""编号"" = @项目编号;", new { 项目编号 = 横向项目编号 });
            if (项目 == null) return NotFound(new { error = "横向项目不存在" });
            var 申请 = await Db.QueryAsync<object>(@"
SELECT e.""编号"", e.""事件类型"", e.""原合同结束日期"", e.""新合同结束日期"", e.""申请原因"",
       e.""附件路径"", e.""状态"", e.""申请人编号"", e.""审核人编号"", e.""审核意见"", e.""申请时间"", e.""审核时间""
  FROM ""横向项目生命周期申请"" e
 WHERE e.""横向项目编号"" = @项目编号 ORDER BY e.""申请时间"" DESC, e.""编号"" DESC;", new { 项目编号 = 横向项目编号 });
            var 报告 = await Db.QueryAsync<object>(@"
SELECT ""编号"", ""报告类型"", ""报告年度"", ""报告摘要"", ""文件路径"", ""状态"",
       ""提交人编号"", ""提交时间"", ""审核人编号"", ""审核意见"", ""审核时间"", ""建立时间""
  FROM ""横向项目研究报告"" WHERE ""横向项目编号"" = @项目编号
 ORDER BY ""报告年度"" DESC, ""建立时间"" DESC;", new { 项目编号 = 横向项目编号 });
            var 付款节点 = await Db.QueryAsync<object>(@"
SELECT ""编号"", ""节点序号"", ""节点名称"", ""约定金额"", ""到期日期"", ""状态"", ""已付款金额"",
       ""合同编号"", ""备注"", CASE WHEN ""到期日期"" < CURRENT_DATE AND ""状态"" NOT IN ('已付款','取消') THEN TRUE ELSE FALSE END AS ""是否逾期""
  FROM ""项目合同付款节点"" WHERE ""流程模板编号""=@流程模板编号 AND ""项目编号""=@项目编号 ORDER BY ""节点序号"";",
                new { 流程模板编号 = 横向项目申请流程模板编号, 项目编号 = 横向项目编号 });
            var 材料 = await Db.QueryAsync<object>(@"
SELECT ""编号"", ""材料类型"", ""材料标题"", ""文件路径"", ""文件校验和"", ""版本号"", ""是否必需"",
       ""状态"", ""上传人编号"", ""上传时间""
  FROM ""横向项目档案材料"" WHERE ""横向项目编号"" = @项目编号
 ORDER BY ""材料类型"", ""版本号"" DESC, ""上传时间"" DESC;", new { 项目编号 = 横向项目编号 });
            return new { 项目, 生命周期申请 = 申请, 研究报告 = 报告, 合同付款节点 = 付款节点, 档案材料 = 材料 };
        }

        [HttpPost]
        async public Task<object> 申请横向项目延期暂停终止([FromBody]横向项目生命周期申请新增 model)
        {
            if (model == null || model.横向项目编号 <= 0) throw new ArgumentException("缺少横向项目编号");
            var eventType = model.事件类型?.Trim();
            if (eventType is not ("延期" or "暂停" or "终止")) throw new ArgumentException("事件类型必须是延期、暂停或终止");
            if (string.IsNullOrWhiteSpace(model.申请原因) || model.申请原因.Trim().Length > 2000)
                throw new ArgumentException("申请原因不能为空且不能超过2000个字符");
            if (!await CanAccessProjectAsync(横向项目申请流程模板编号, model.横向项目编号)) return ProjectAccessDenied();
            var state = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT p.""合同结束时间"", EXISTS (SELECT 1 FROM ""横向项目结题"" c WHERE c.""横向项目编号"" = p.""编号"") AS closed,
       COALESCE((SELECT e.""事件类型"" FROM ""横向项目生命周期申请"" e
                  WHERE e.""横向项目编号"" = p.""编号"" AND e.""状态"" = '已批准'
                  ORDER BY e.""审核时间"" DESC NULLS LAST, e.""编号"" DESC LIMIT 1), '正常') AS current_status
  FROM ""横向项目"" p WHERE p.""编号"" = @项目编号;", new { 项目编号 = model.横向项目编号 });
            if (state == null) return NotFound(new { error = "横向项目不存在" });
            if ((bool)state.closed || (string)state.current_status == "终止") throw new ArgumentException("项目已结题或终止，不能再申请生命周期变更");
            if (eventType == "延期" && (!model.新合同结束日期.HasValue || model.新合同结束日期.Value.Date <= ((DateTime)state.合同结束时间).Date))
                throw new ArgumentException("延期后的合同结束日期必须晚于当前合同结束日期");
            var pending = await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (SELECT 1 FROM ""横向项目生命周期申请"" WHERE ""横向项目编号""=@项目编号 AND ""事件类型""=@事件类型 AND ""状态""='待审核');",
                new { 项目编号 = model.横向项目编号, 事件类型 = eventType });
            if (pending) throw new ArgumentException("该类生命周期申请已有待审核记录");
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""横向项目生命周期申请"" (""横向项目编号"",""事件类型"",""原合同结束日期"",""新合同结束日期"",""申请原因"",""附件路径"",""申请人编号"")
SELECT @项目编号,@事件类型,""合同结束时间""::date,@新合同结束日期,@申请原因,@附件路径,@申请人编号
  FROM ""横向项目"" WHERE ""编号""=@项目编号 RETURNING ""编号"";", new
            {
                项目编号 = model.横向项目编号, 事件类型 = eventType, 新合同结束日期 = model.新合同结束日期?.Date,
                申请原因 = model.申请原因.Trim(), 附件路径 = string.IsNullOrWhiteSpace(model.附件路径) ? null : model.附件路径.Trim(),
                申请人编号 = CurrentUser.编号
            });
            return new { 编号 = id, 状态 = "待审核" };
        }

        [HttpPost]
        async public Task<object> 审核横向项目生命周期申请([FromBody]横向项目生命周期审核 model)
        {
            if (model == null || model.编号 <= 0) throw new ArgumentException("缺少申请编号");
            if ((model.审核意见?.Length ?? 0) > 2000) throw new ArgumentException("审核意见不能超过2000个字符");
            if (!await HasAnyRoleAsync("科研管理员", "系统管理员")) return StatusCode(403, new { error = "无权审核横向项目生命周期申请" });
            var 审核人编号 = await ResolveOperationUserAsync("项目审批");
            using var connection = Db;
            connection.Open();
            using var transaction = connection.BeginTransaction();
            var request = await connection.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"", ""横向项目编号"", ""事件类型"", ""新合同结束日期""
  FROM ""横向项目生命周期申请"" WHERE ""编号""=@编号 AND ""状态""='待审核';", new { model.编号 }, transaction);
            if (request == null) throw new ArgumentException("申请不存在或已审核");
            var status = model.通过 ? "已批准" : "已驳回";
            await connection.ExecuteAsync(@"
UPDATE ""横向项目生命周期申请""
   SET ""状态""=@状态, ""审核人编号""=@审核人编号, ""审核意见""=@审核意见, ""审核时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@编号 AND ""状态""='待审核';", new
            {
                状态 = status, 审核人编号, 审核意见 = string.IsNullOrWhiteSpace(model.审核意见) ? null : model.审核意见.Trim(), model.编号
            }, transaction);
            if (model.通过 && (string)request.事件类型 == "延期")
            {
                await connection.ExecuteAsync(@"
UPDATE ""横向项目"" SET ""合同结束时间""=@新合同结束日期 WHERE ""编号""=@项目编号;",
                    new { 新合同结束日期 = (DateTime)request.新合同结束日期, 项目编号 = (int)request.横向项目编号 }, transaction);
            }
            transaction.Commit();
            return new { 编号 = model.编号, 状态 = status };
        }

        [HttpPost]
        async public Task<object> 提交横向项目研究报告([FromBody]横向项目研究报告新增 model)
        {
            if (model == null || model.横向项目编号 <= 0) throw new ArgumentException("缺少横向项目编号");
            if (string.IsNullOrWhiteSpace(model.报告类型) || model.报告类型.Trim().Length > 30) throw new ArgumentException("报告类型不能为空");
            if (model.报告年度 < 2000 || model.报告年度 > DateTime.Today.Year + 1) throw new ArgumentException("报告年度无效");
            if ((model.报告摘要?.Length ?? 0) > 2000 || (model.报告正文?.Length ?? 0) > 200000) throw new ArgumentException("研究报告内容超出限制");
            if (string.IsNullOrWhiteSpace(model.报告正文) && string.IsNullOrWhiteSpace(model.文件路径)) throw new ArgumentException("请填写报告正文或上传报告文件");
            if (!await CanAccessProjectAsync(横向项目申请流程模板编号, model.横向项目编号)) return ProjectAccessDenied();
            var currentStatus = await Db.QuerySingleOrDefaultAsync<string>(@"
SELECT COALESCE((SELECT e.""事件类型"" FROM ""横向项目生命周期申请"" e WHERE e.""横向项目编号""=@项目编号 AND e.""状态""='已批准' ORDER BY e.""审核时间"" DESC NULLS LAST, e.""编号"" DESC LIMIT 1),'正常');", new { 项目编号 = model.横向项目编号 });
            if (currentStatus == "终止") throw new ArgumentException("项目已终止，不能提交新的研究报告");
            var status = model.提交审核 ? "待审核" : "草稿";
            var id = await Db.QuerySingleOrDefaultAsync<long?>(@"
INSERT INTO ""横向项目研究报告"" (""横向项目编号"",""报告类型"",""报告年度"",""报告摘要"",""报告正文"",""文件路径"",""状态"",""提交人编号"",""提交时间"")
VALUES (@项目编号,@报告类型,@报告年度,@报告摘要,@报告正文,@文件路径,@状态,@提交人编号,CASE WHEN @提交审核 THEN CURRENT_TIMESTAMP ELSE NULL END)
ON CONFLICT (""横向项目编号"",""报告类型"",""报告年度"") DO UPDATE SET
  ""报告摘要""=EXCLUDED.""报告摘要"", ""报告正文""=EXCLUDED.""报告正文"", ""文件路径""=EXCLUDED.""文件路径"", ""状态""=EXCLUDED.""状态"", ""提交时间""=EXCLUDED.""提交时间""
 WHERE ""横向项目研究报告"".""提交人编号""=@提交人编号 AND ""横向项目研究报告"".""状态"" IN ('草稿','已退回')
RETURNING ""编号"";", new
            {
                项目编号 = model.横向项目编号, 报告类型 = model.报告类型.Trim(), model.报告年度,
                报告摘要 = string.IsNullOrWhiteSpace(model.报告摘要) ? null : model.报告摘要.Trim(),
                报告正文 = string.IsNullOrWhiteSpace(model.报告正文) ? null : model.报告正文.Trim(),
                文件路径 = string.IsNullOrWhiteSpace(model.文件路径) ? null : model.文件路径.Trim(), 状态 = status,
                提交人编号 = CurrentUser.编号, model.提交审核
            });
            if (!id.HasValue) throw new ArgumentException("同年度同类型报告已由其他人员提交审核，不能覆盖");
            return new { 编号 = id.Value, 状态 = status };
        }

        [HttpPost]
        async public Task<object> 审核横向项目研究报告([FromBody]横向项目研究报告审核 model)
        {
            if (model == null || model.编号 <= 0) throw new ArgumentException("缺少报告编号");
            if ((model.审核意见?.Length ?? 0) > 2000) throw new ArgumentException("审核意见不能超过2000个字符");
            if (!await HasAnyRoleAsync("科研管理员", "系统管理员")) return StatusCode(403, new { error = "无权审核横向项目研究报告" });
            var 审核人编号 = await ResolveOperationUserAsync("项目审批");
            var updated = await Db.ExecuteAsync(@"
UPDATE ""横向项目研究报告"" SET ""状态""=@状态,""审核人编号""=@审核人编号,""审核意见""=@审核意见,""审核时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@编号 AND ""状态""='待审核';", new
            {
                状态 = model.通过 ? "已通过" : "已退回", 审核人编号,
                审核意见 = string.IsNullOrWhiteSpace(model.审核意见) ? null : model.审核意见.Trim(), model.编号
            });
            if (updated == 0) throw new ArgumentException("报告不存在或当前不在待审核状态");
            return new { 编号 = model.编号, 状态 = model.通过 ? "已通过" : "已退回" };
        }

        [HttpPost]
        async public Task<object> 归档横向项目材料([FromBody]横向项目档案材料新增 model)
        {
            if (model == null || model.横向项目编号 <= 0) throw new ArgumentException("缺少横向项目编号");
            if (string.IsNullOrWhiteSpace(model.材料类型) || string.IsNullOrWhiteSpace(model.材料标题) || string.IsNullOrWhiteSpace(model.文件路径))
                throw new ArgumentException("材料类型、材料标题和文件路径不能为空");
            if (model.材料类型.Trim().Length > 50 || model.材料标题.Trim().Length > 200 || model.文件路径.Trim().Length > 1000)
                throw new ArgumentException("档案材料字段超出限制");
            if (model.版本号 <= 0) throw new ArgumentException("档案材料版本号必须大于0");
            if (!string.IsNullOrWhiteSpace(model.文件校验和) && !System.Text.RegularExpressions.Regex.IsMatch(model.文件校验和.Trim(), "^[0-9a-fA-F]{64}$"))
                throw new ArgumentException("文件校验和必须是64位SHA-256");
            if (!await CanAccessProjectAsync(横向项目申请流程模板编号, model.横向项目编号)) return ProjectAccessDenied();
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""横向项目档案材料"" (""横向项目编号"",""材料类型"",""材料标题"",""文件路径"",""文件校验和"",""版本号"",""是否必需"",""上传人编号"")
VALUES (@项目编号,@材料类型,@材料标题,@文件路径,@文件校验和,@版本号,@是否必需,@上传人编号) RETURNING ""编号"";", new
            {
                项目编号 = model.横向项目编号, 材料类型 = model.材料类型.Trim(), 材料标题 = model.材料标题.Trim(), 文件路径 = model.文件路径.Trim(),
                文件校验和 = string.IsNullOrWhiteSpace(model.文件校验和) ? null : model.文件校验和.Trim().ToLowerInvariant(), model.版本号, model.是否必需,
                上传人编号 = CurrentUser.编号
            });
            return new { 编号 = id };
        }

        [HttpPost]
        async public Task<object> 上传横向项目档案材料()
        {
            var filesNameList = await MyLib.UploadFile.Upload(Request.Form.Files, Env.WebRootPath, "upload/横向项目/档案材料", config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }
        #endregion
    }
}
