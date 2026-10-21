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
using System.IO;
using Dapper;

namespace ScientificResearch.Controllers
{
    public class PeopleController : BaseController
    {
        private int 专家角色组编号
        {
            get
            {
                return config.GetValue<int>("系统角色组编号:专家");
            }
        }

        private int 科研人员角色组编号
        {
            get
            {
                return config.GetValue<int>("系统角色组编号:科研人员");
            }
        }

        private int 学术任职流程模板编号
        {
            get
            {
                return config.GetValue<int>("流程模板编号:学术任职");
            }
        }

        /// <summary>
        /// 分页获取人员列表,人员管理时候用
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<PagingResult<v1_人员_带部门名>> 分页获取人员列表(Paging paging, 人员Filter filter) =>
            await Db.GetPagingListSpAsync<v1_人员_带部门名, 人员Filter>(paging, filter);

        [HttpGet]
        async public Task<v1_人员_带部门名> 获取某人员详情(int 人员编号)
        {
            var person = await Db.GetModelByIdSpAsync<v1_人员_带部门名>(人员编号);
            if (person == null) return null;
            if (!await HasFieldPermissionAsync("人员", "手机")) person.手机 = null;
            if (!await HasFieldPermissionAsync("人员", "邮箱")) person.邮箱 = null;
            if (!await HasFieldPermissionAsync("人员", "身份证")) person.身份证 = null;
            return person;
        }

        [HttpGet]
        async public Task<object> 获取人员详情辅助数据()
        {
            var 民族 = await Db.GetListSpAsync<string, 字典Filter>(new 字典Filter() { 分类 = "民族" }, tbName: "字典", tbFields: "值");
            var 政治面貌 = await Db.GetListSpAsync<string, 字典Filter>(new 字典Filter() { 分类 = "政治面貌" }, tbName: "字典", tbFields: "值");
            var 人员类别 = await Db.GetListSpAsync<string, 字典Filter>(new 字典Filter() { 分类 = "人员类别" }, tbName: "字典", tbFields: "值");
            var 人员类型 = await Db.GetListSpAsync<string, 字典Filter>(new 字典Filter() { 分类 = "人员类型" }, tbName: "字典", tbFields: "值");
            var 岗位名称 = await Db.GetListSpAsync<string, 字典Filter>(new 字典Filter() { 分类 = "岗位名称" }, tbName: "字典", tbFields: "值");
            var 岗位类型 = await Db.GetListSpAsync<string, 字典Filter>(new 字典Filter() { 分类 = "岗位类型" }, tbName: "字典", tbFields: "值");
            var 岗位性质 = await Db.GetListSpAsync<string, 字典Filter>(new 字典Filter() { 分类 = "岗位性质" }, tbName: "字典", tbFields: "值");
            var 学历 = await Db.GetListSpAsync<string, 字典Filter>(new 字典Filter() { 分类 = "学历" }, tbName: "字典", tbFields: "值");
            var 学位 = await Db.GetListSpAsync<string, 字典Filter>(new 字典Filter() { 分类 = "学位" }, tbName: "字典", tbFields: "值");
            var 学科门类 = await Db.GetListSpAsync<string, 字典Filter>(new 字典Filter() { 分类 = "学科门类" }, tbName: "字典", tbFields: "值");
            var 学制 = await Db.GetListSpAsync<string, 字典Filter>(new 字典Filter() { 分类 = "学制" }, tbName: "字典", tbFields: "值");
            var 所学专业 = await Db.GetListSpAsync<string, 字典Filter>(new 字典Filter() { 分类 = "所学专业" }, tbName: "字典", tbFields: "值");
            var 学习形式 = await Db.GetListSpAsync<string, 字典Filter>(new 字典Filter() { 分类 = "学习形式" }, tbName: "字典", tbFields: "值");
            var 学历性质 = await Db.GetListSpAsync<string, 字典Filter>(new 字典Filter() { 分类 = "学历性质" }, tbName: "字典", tbFields: "值");
            var 个人身份 = await Db.GetListSpAsync<string, 字典Filter>(new 字典Filter() { 分类 = "个人身份" }, tbName: "字典", tbFields: "值");
            var 职位 = await Db.GetListSpAsync<string, 字典Filter>(new 字典Filter() { 分类 = "职位" }, tbName: "字典", tbFields: "值");
            var 职位级别 = await Db.GetListSpAsync<string, 字典Filter>(new 字典Filter() { 分类 = "职位级别" }, tbName: "字典", tbFields: "值");
            var 专业技术级别 = await Db.GetListSpAsync<string, 字典Filter>(new 字典Filter() { 分类 = "专业技术级别" }, tbName: "字典", tbFields: "值");
            var 技术职称分类 = await Db.GetListSpAsync<string, 字典Filter>(new 字典Filter() { 分类 = "技术职称分类" }, tbName: "字典", tbFields: "值");

            return new
            {
                民族,
                政治面貌,
                人员类别,
                人员类型,
                岗位名称,
                岗位类型,
                岗位性质,
                学历,
                学位,
                学科门类,
                学制,
                所学专业,
                学习形式,
                学历性质,
                个人身份,
                职位,
                职位级别,
                专业技术级别,
                技术职称分类
            };
        }

        /// <summary>
        /// 分页获取启用状态的人员列表,使用人员时候用
        /// 这个可能应该缩减一下返回的字段.
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<PagingResult<v1_人员_带部门名>> 分页获取启用的人员列表(Paging paging, 人员Filter filter)
        {
            filter.是否启用 = true;
            return await Db.GetPagingListSpAsync<v1_人员_带部门名, 人员Filter>(paging, filter);
        }


        /// <summary>
        /// 获取所有人员列表,应该用不到
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<IEnumerable<v1_人员_带部门名>> 获取所有人员列表(人员Filter filter) =>
            await Db.GetListSpAsync<v1_人员_带部门名, 人员Filter>(filter);

        /// <summary>只读预览 HR 全量/增量同步影响，不写入人员、映射或代理数据。</summary>
        [HttpPost]
        async public Task<object> 预览HR人员同步([FromBody]HR人员同步请求 model)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员")) return StatusCode(403, new { error = "无权预览 HR 人员同步" });
            if (model == null || model.外部系统编号 <= 0 || string.IsNullOrWhiteSpace(model.同步批次号))
                throw new ArgumentException("外部系统编号和同步批次号不能为空");
            var rows = (model.人员列表 ?? Enumerable.Empty<HR人员同步记录>()).ToList();
            if (rows.Count > 2000) throw new ArgumentException("单批同步人员数不能超过 2000");
            if (!await Db.QuerySingleAsync<bool>("SELECT EXISTS (SELECT 1 FROM \"外部系统\" WHERE \"编号\"=@外部系统编号 AND \"系统类型\"='HR');", new { model.外部系统编号 }))
                return NotFound(new { error = "外部系统不存在或不是 HR 类型" });
            var validIds = rows.Where(item => !string.IsNullOrWhiteSpace(item.外部人员编号)
                    && !string.IsNullOrWhiteSpace(item.工号)
                    && !string.IsNullOrWhiteSpace(item.姓名)
                    && !string.IsNullOrWhiteSpace(item.部门名称)
                    && item.外部人员编号.Trim().Length <= 150
                    && item.工号.Trim().Length <= 50
                    && item.姓名.Trim().Length <= 50
                    && item.部门名称.Trim().Length <= 50)
                .Select(item => item.外部人员编号.Trim()).ToList();
            var duplicateIds = validIds.GroupBy(x => x, StringComparer.Ordinal).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            var invalidCount = rows.Count - validIds.Count;
            var missingCount = 0;
            if (model.是否全量 && invalidCount == 0 && duplicateIds.Length == 0)
                missingCount = await Db.QuerySingleAsync<int>(@"
SELECT count(*)::integer
  FROM ""人员外部映射"" m
 WHERE m.""外部系统编号""=@外部系统编号 AND m.""是否在职""=TRUE
   AND NOT (m.""外部人员编号"" = ANY(@外部人员编号));", new { model.外部系统编号, 外部人员编号 = validIds.Distinct(StringComparer.Ordinal).ToArray() });
            return new { model.同步批次号, model.是否全量, 总数 = rows.Count, 有效记录数 = validIds.Count, 无效记录数 = invalidCount, 重复外部人员编号 = duplicateIds, 预计禁用缺席人员数 = missingCount, 可安全执行 = invalidCount == 0 && duplicateIds.Length == 0 };
        }

        /// <summary>接收 HR 人员目录增量或全量同步，按外部人员编号幂等处理。</summary>
        [HttpPost]
        async public Task<object> 同步HR人员([FromBody]HR人员同步请求 model)
        {
            if (!await HasAnyRoleAsync("系统管理员", "科研管理员")) return StatusCode(403, new { error = "无权执行 HR 人员同步" });
            if (model == null || model.外部系统编号 <= 0 || string.IsNullOrWhiteSpace(model.同步批次号))
                throw new ArgumentException("外部系统编号和同步批次号不能为空");
            var rows = (model.人员列表 ?? Enumerable.Empty<HR人员同步记录>()).ToList();
            if (rows.Count > 2000) throw new ArgumentException("单批同步人员数不能超过 2000");
            if (!await Db.QuerySingleAsync<bool>("SELECT EXISTS (SELECT 1 FROM \"外部系统\" WHERE \"编号\"=@外部系统编号 AND \"系统类型\"='HR');", new { model.外部系统编号 }))
                return NotFound(new { error = "外部系统不存在或不是 HR 类型" });
            var 新增 = 0; var 更新 = 0; var 禁用 = 0; var 跳过 = 0; var 错误 = new List<string>();
            foreach (var item in rows)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(item.外部人员编号) || string.IsNullOrWhiteSpace(item.工号) || string.IsNullOrWhiteSpace(item.姓名) || string.IsNullOrWhiteSpace(item.部门名称))
                    { 错误.Add("缺少外部人员编号、工号、姓名或部门名称"); 跳过++; continue; }
                    if (item.外部人员编号.Length > 150 || item.工号.Trim().Length > 50 || item.姓名.Trim().Length > 50 || item.部门名称.Trim().Length > 50)
                    { 错误.Add($"人员 {item.工号} 字段超长"); 跳过++; continue; }
                    var departmentId = await Db.QuerySingleOrDefaultAsync<int?>("SELECT \"编号\" FROM \"部门\" WHERE \"上级部门编号\"=0 AND \"名称\"=@名称 LIMIT 1;", new { 名称 = item.部门名称.Trim() });
                    if (!departmentId.HasValue)
                        departmentId = await Db.QuerySingleAsync<int>("INSERT INTO \"部门\" (\"上级部门编号\",\"名称\",\"是否启用\") VALUES (0,@名称,TRUE) RETURNING \"编号\";", new { 名称 = item.部门名称.Trim() });
                    else
                        await Db.ExecuteAsync("UPDATE \"部门\" SET \"是否启用\"=TRUE WHERE \"编号\"=@编号;", new { 编号 = departmentId.Value });
                    var personId = await Db.QuerySingleOrDefaultAsync<int?>("SELECT \"编号\" FROM \"人员\" WHERE \"工号\"=@工号 LIMIT 1;", new { 工号 = item.工号.Trim() });
                    if (!personId.HasValue)
                    {
                        personId = await Db.QuerySingleAsync<int>(@"
INSERT INTO ""人员"" (""工号"",""密码"",""姓名"",""部门编号"",""手机"",""邮箱"",""岗位名称"",""是否启用"")
VALUES (@工号,@密码,@姓名,@部门编号,@手机,@邮箱,@岗位名称,@是否启用) RETURNING ""编号"";", new
                        {
                            工号 = item.工号.Trim(), 密码 = Guid.NewGuid().ToString("N"), 姓名 = item.姓名.Trim(), 部门编号 = departmentId.Value,
                            手机 = 空值(item.手机), 邮箱 = 空值(item.邮箱), 岗位名称 = 空值(item.岗位名称), 是否启用 = item.是否在职
                        });
                        新增++;
                    }
                    else
                    {
                        await Db.ExecuteAsync(@"
UPDATE ""人员"" SET ""姓名""=@姓名,""部门编号""=@部门编号,""手机""=@手机,""邮箱""=@邮箱,""岗位名称""=@岗位名称,""是否启用""=@是否启用 WHERE ""编号""=@编号;", new
                        {
                            编号 = personId.Value, 姓名 = item.姓名.Trim(), 部门编号 = departmentId.Value,
                            手机 = 空值(item.手机), 邮箱 = 空值(item.邮箱), 岗位名称 = 空值(item.岗位名称), 是否启用 = item.是否在职
                        });
                        更新++; if (!item.是否在职) 禁用++;
                    }
                    await Db.ExecuteAsync(@"
INSERT INTO ""人员外部映射"" (""外部系统编号"",""外部人员编号"",""人员编号"",""外部更新时间"",""是否在职"",""备注"")
VALUES (@外部系统编号,@外部人员编号,@人员编号,@外部更新时间,@是否在职,@备注)
ON CONFLICT (""外部系统编号"",""外部人员编号"") DO UPDATE SET ""人员编号""=EXCLUDED.""人员编号"",""外部更新时间""=EXCLUDED.""外部更新时间"",""最后同步时间""=CURRENT_TIMESTAMP,""是否在职""=EXCLUDED.""是否在职"",""备注""=EXCLUDED.""备注"";", new
                    {
                        model.外部系统编号, 外部人员编号 = item.外部人员编号.Trim(), 人员编号 = personId.Value,
                        item.外部更新时间, item.是否在职, 备注 = string.IsNullOrWhiteSpace(model.同步批次号) ? null : $"批次:{model.同步批次号.Trim()}"
                    });
                }
                catch (Exception ex)
                {
                    错误.Add($"人员 {item.工号}: {ex.Message}");
                }
            }
            if (model.是否全量)
            {
                var snapshotIds = rows
                    .Where(item => !string.IsNullOrWhiteSpace(item.外部人员编号)
                        && !string.IsNullOrWhiteSpace(item.工号)
                        && !string.IsNullOrWhiteSpace(item.姓名)
                        && !string.IsNullOrWhiteSpace(item.部门名称)
                        && item.外部人员编号.Trim().Length <= 150
                        && item.工号.Trim().Length <= 50
                        && item.姓名.Trim().Length <= 50
                        && item.部门名称.Trim().Length <= 50)
                    .Select(item => item.外部人员编号.Trim())
                    .ToList();
                var snapshotComplete = snapshotIds.Count == rows.Count
                    && snapshotIds.Count == snapshotIds.Distinct(StringComparer.Ordinal).Count();
                if (!snapshotComplete)
                {
                    错误.Add("全量同步包含无效或重复外部人员编号，未执行缺席人员禁用");
                }
                else
                {
                    var missingPeople = await Db.QueryAsync<int>(@"
SELECT m.""人员编号""
  FROM ""人员外部映射"" m
 WHERE m.""外部系统编号""=@外部系统编号
   AND m.""是否在职""=TRUE
   AND NOT (m.""外部人员编号"" = ANY(@外部人员编号));", new { model.外部系统编号, 外部人员编号 = snapshotIds.ToArray() });
                    foreach (var personId in missingPeople.Distinct())
                    {
                        await Db.ExecuteAsync(@"
UPDATE ""人员外部映射""
   SET ""是否在职""=FALSE,""最后同步时间""=CURRENT_TIMESTAMP,""备注""=@备注
 WHERE ""外部系统编号""=@外部系统编号 AND ""人员编号""=@人员编号;", new
                        {
                            model.外部系统编号, 人员编号 = personId,
                            备注 = $"全量批次:{model.同步批次号.Trim()} 缺席，已禁用"
                        });
                        var disabled = await Db.ExecuteAsync(@"
UPDATE ""人员"" p SET ""是否启用""=FALSE
 WHERE p.""编号""=@人员编号
   AND NOT EXISTS (
       SELECT 1 FROM ""人员外部映射"" m
       JOIN ""外部系统"" s ON s.""编号""=m.""外部系统编号""
       WHERE m.""人员编号""=p.""编号"" AND m.""是否在职""=TRUE AND s.""系统类型""='HR');", new { 人员编号 = personId });
                        if (disabled > 0) 禁用++;
                    }
                }
            }
            return new { model.同步批次号, 总数 = rows.Count, 新增, 更新, 禁用, 跳过, 错误数 = 错误.Count, 错误 };
        }

        /// <summary>
        /// 增改人员
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 增改人员([FromBody]人员 model) =>
            await Db.ExecuteSpAsync(new sp_人员_增改() { tt = model.ToDataTable() });

        [HttpPost]
        async public Task 导入人员()
        {
            //var filesNameList = await MyLib.UploadFile.Upload(
            //   Request.Form.Files,
            //   Env.WebRootPath,
            //   "upload/基础信息/导入人员",
            //   config.GetValue<int>("uploadFileMaxSize"));

            //var result = MyXls.Import<人员导入>(Path.Combine(Env.WebRootPath, filesNameList[0]));

            var f = "C:\\Users\\Administrator\\Desktop\\导入人员模板.xlsx";
            var result = MyXls.Import<人员导入>(f);

            //工号|密码|姓名|部门名称都空的删掉
            result = result.Where(i => !string.IsNullOrWhiteSpace(i.工号) ||
                                       !string.IsNullOrWhiteSpace(i.密码) ||
                                       !string.IsNullOrWhiteSpace(i.姓名) ||
                                       !string.IsNullOrWhiteSpace(i.部门名称));

            TryValidateModel(result);
            await Db.ExecuteSpAsync(new sp_人员_导入() { tt = result.ToDataTable() });
        }

        /// <summary>
        /// 启用人员
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 启用人员([FromBody]int id) =>
            await Db.ExecuteSpAsync(new sp_人员_启禁() { 编号 = id, 是否启用 = true });

        /// <summary>
        /// 禁用人员
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 禁用人员([FromBody]int id) =>
            await Db.ExecuteSpAsync(new sp_人员_启禁() { 编号 = id, 是否启用 = false });

        #region 专家人才
        /// <summary>
        /// 分页获取专家
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet]
        async public Task<PagingResult<v1_人员_带部门名>> 分页获取专家(Paging paging, 人员Filter filter)
        {
            return await Db.GetPagingListSpAsync<v1_人员_带部门名, 人员Filter>(paging, filter, $"tfn_角色中的人员({专家角色组编号})");
        }

        [HttpGet]
        async public Task<PagingResult<v1_人员_带部门名>> 分页获取不是专家的人员(Paging paging, 人员Filter filter) =>
            await Db.GetPagingListSpAsync<v1_人员_带部门名, 人员Filter>(paging, filter, $"tfn_角色中没有的人员({专家角色组编号})");

        /// <summary>
        /// 从现有人员增加专家
        /// </summary>
        /// <param name="编号列表"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 从现有人员增加专家([FromBody]IEnumerable<int> 编号列表)
        {
            await Db.ExecuteSpAsync(new sp_角色中的人员_增()
            {
                角色编号 = 专家角色组编号,
                人员编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
            });
        }

        /// <summary>
        /// 新增专家
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        async public Task 新增专家([FromBody]人员 model) =>
            await Db.ExecuteSpAsync(new sp_角色中的人员_新增人员同时到角色()
            {
                角色编号 = 专家角色组编号,
                tt = model.ToDataTable()
            });

        [HttpPost]
        async public Task 删除专家([FromBody]IEnumerable<int> 编号列表) =>
            await Db.ExecuteSpAsync(new sp_角色中的人员_删()
            {
                角色编号 = 专家角色组编号,
                人员编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
            });
        #endregion

        #region 科研人员
        [HttpGet]
        async public Task<PagingResult<v1_人员_带部门名>> 分页获取科研人员(Paging paging, 人员Filter filter)
        {
            return await Db.GetPagingListSpAsync<v1_人员_带部门名, 人员Filter>(paging, filter, $"tfn_角色中的人员({科研人员角色组编号})");
        }

        [HttpGet]
        async public Task<PagingResult<v1_人员_带部门名>> 分页获取非科研人员(Paging paging, 人员Filter filter) =>
            await Db.GetPagingListSpAsync<v1_人员_带部门名, 人员Filter>(paging, filter, $"tfn_角色中没有的人员({科研人员角色组编号})");

        [HttpPost]
        async public Task 从现有人员增加科研人员([FromBody]IEnumerable<int> 编号列表)
        {
            await Db.ExecuteSpAsync(new sp_角色中的人员_增()
            {
                角色编号 = 科研人员角色组编号,
                人员编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
            });
        }

        [HttpPost]
        async public Task 新增科研人员([FromBody]人员 model) =>
           await Db.ExecuteSpAsync(new sp_角色中的人员_新增人员同时到角色()
           {
               角色编号 = 科研人员角色组编号,
               tt = model.ToDataTable()
           });

        [HttpPost]
        async public Task 删除科研人员([FromBody]IEnumerable<int> 编号列表) =>
           await Db.ExecuteSpAsync(
               new sp_角色中的人员_删()
               {
                   角色编号 = 科研人员角色组编号,
                   人员编号 = 编号列表.ToPredefindedKeyFieldsList().ToDataTable()
               });
        #endregion

        #region 学术任职

        [HttpPost]
        async public Task 新增学术任职([FromBody]任职增改 model)
        {
            model.基本资料.任职人员编号 = CurrentUser.编号;
            await MyWorkFlowBusiness.发起流程(
                学术任职流程模板编号,
                new sp_任职_增改()
                {
                    tt = model.基本资料.ToDataTable()
                },
                CurrentUser.编号,
                model.IsHold);
        }

        [HttpPost]
        async public Task 完善学术任职([FromBody]完成步骤<任职增改> model) =>
            await MyWorkFlowBusiness.完成步骤(
                model.toSimple(), CurrentUser.编号,
                new sp_任职_增改()
                {
                    tt = model.附加数据.基本资料.ToDataTable()
                },
                model.附加数据.IsHold);

        [HttpPost]
        async public Task<object> 上传学术任职相关文件()
        {
            var filesNameList = await MyLib.UploadFile.Upload(
                Request.Form.Files,
                Env.WebRootPath,
                "upload/组织架构/学术任职",
                config.GetValue<int>("uploadFileMaxSize"));
            return filesNameList;
        }

        [HttpGet]
        async public Task<object> 分页获取我的学术任职(Paging paging, 任职Filter filter)
        {
            filter.任职人员编号 = CurrentUser.编号;
            return await Db.GetPagingListSpAsync<object, 任职Filter>(
                  paging,
                  filter,
                  $"tfn_任职({CurrentUser.编号},{学术任职流程模板编号})");
        }

        [HttpGet]
        async public Task<object> 分页获取学术任职(Paging paging, 任职Filter filter) =>
            await Db.GetPagingListSpAsync<object, 任职Filter>(
                paging,
                filter,
                $"tfn_任职({CurrentUser.编号},{学术任职流程模板编号})");

        [HttpGet]
        async public Task<object> 获取学术任职详情(int 任职编号)
        {
            var r1 = await Db.GetModelByIdSpAsync<v2_任职>(任职编号);
            var r2 = await Db.GetListSpAsync<object>($"tfn_步骤({学术任职流程模板编号},{任职编号})", orderType: true);
            return new { 学术任职详情 = r1, 审核情况 = r2 };
        }

        #endregion

        #region 人才与团队能力画像
        [HttpGet]
        async public Task<object> 获取人员科研能力画像(int 人员编号)
        {
            if (人员编号 <= 0) throw new ArgumentException("人员编号无效");
            var isManager = await 是人员管理角色();
            if (人员编号 != CurrentUser.编号 && !isManager) return StatusCode(403, new { error = "无权查看该人员能力画像" });
            var person = await Db.QuerySingleOrDefaultAsync<object>(@"
SELECT p.""编号"", p.""工号"", p.""姓名"", p.""部门编号"", d.""名称"" AS ""部门名称"", p.""技术职称"", p.""学科门类"", p.""研究方向""
  FROM ""人员"" p LEFT JOIN ""部门"" d ON d.""编号""=p.""部门编号"" WHERE p.""编号""=@人员编号;", new { 人员编号 });
            if (person == null) return NotFound(new { error = "人员不存在" });
            var capability = await Db.QuerySingleOrDefaultAsync<object>(@"
SELECT ""人员编号"", ""研究专长"", ""研究方向标签"", ""技术服务能力"", ""合作意愿"", ""合作说明"", ""可用时间"", ""每周可用小时"", ""可承担项目数"", ""当前可接项目"", ""可公开展示"", ""更新人编号"", ""更新时间""
  FROM ""人员科研能力"" WHERE ""人员编号""=@人员编号;", new { 人员编号 });
            var tags = await Db.QueryAsync<object>(@"
SELECT ""编号"", ""标签类型"", ""标签"", ""来源""
  FROM ""人员能力标签"" WHERE ""人员编号""=@人员编号
 ORDER BY ""标签类型"", ""标签"";", new { 人员编号 });
            var mentor = await Db.QueryAsync<object>(@"
SELECT r.""编号"", r.""导师编号"", m.""姓名"" AS ""导师姓名"", r.""学员编号"", s.""姓名"" AS ""学员姓名"", r.""关系类型"", r.""开始日期"", r.""结束日期"", r.""状态"", r.""备注""
  FROM ""人员导师关系"" r JOIN ""人员"" m ON m.""编号""=r.""导师编号"" JOIN ""人员"" s ON s.""编号""=r.""学员编号""
 WHERE r.""导师编号""=@人员编号 OR r.""学员编号""=@人员编号 ORDER BY r.""状态"", r.""开始日期"" DESC NULLS LAST;", new { 人员编号 });
            var teams = await Db.QueryAsync<object>(@"
SELECT t.""编号"", t.""团队名称"", t.""负责人编号"", t.""研究方向"", t.""技术服务范围"", t.""合作说明"", t.""状态"", m.""成员角色"", m.""加入日期"", m.""状态"" AS ""成员状态""
  FROM ""科研团队成员"" m JOIN ""科研团队"" t ON t.""编号""=m.""团队编号""
 WHERE m.""人员编号""=@人员编号 ORDER BY t.""团队名称"";", new { 人员编号 });
            var assignments = await Db.QueryAsync<object>(@"
SELECT a.""流程模板编号"", a.""项目编号"", a.""分工类型"", a.""工作量占比"", a.""开始日期"", a.""结束日期"", a.""状态"", a.""备注""
  FROM ""人员项目分工"" a
 WHERE a.""人员编号""=@人员编号
   AND (@管理者 OR ""fn_人员可访问项目""(@当前人员编号,a.""流程模板编号"",a.""项目编号""))
 ORDER BY a.""状态"", a.""开始日期"" DESC NULLS LAST;", new { 人员编号, 管理者 = isManager, 当前人员编号 = CurrentUser.编号 });
            await 记录画像访问审计($"/People/获取人员科研能力画像?人员编号={人员编号}");
            return new { 人员 = person, 科研能力 = capability, 能力标签 = tags, 导师关系 = mentor, 团队 = teams, 项目分工 = assignments };
        }

        [HttpPost]
        async public Task<object> 保存人员科研能力画像([FromBody]人员科研能力保存 model)
        {
            if (model == null || model.人员编号 <= 0) throw new ArgumentException("人员编号无效");
            if (model.人员编号 != CurrentUser.编号 && !await 是人员管理角色()) return StatusCode(403, new { error = "无权修改该人员能力画像" });
            if ((model.研究专长?.Length ?? 0) > 2000 || (model.研究方向标签?.Length ?? 0) > 1000 || (model.技术服务能力?.Length ?? 0) > 2000 || (model.合作说明?.Length ?? 0) > 1000 || (model.可用时间?.Length ?? 0) > 1000)
                throw new ArgumentException("能力画像字段超出长度限制");
            var willingness = string.IsNullOrWhiteSpace(model.合作意愿) ? "可合作" : model.合作意愿.Trim();
            if (willingness is not ("可合作" or "需邀约" or "暂不合作")) throw new ArgumentException("合作意愿无效");
            if (model.每周可用小时 < 0 || model.每周可用小时 > 168) throw new ArgumentException("每周可用小时必须在 0 至 168 之间");
            if (model.可承担项目数 < 0 || model.可承担项目数 > 100) throw new ArgumentException("可承担项目数必须在 0 至 100 之间");
            if (!await Db.QuerySingleAsync<bool>(@"SELECT EXISTS (SELECT 1 FROM ""人员"" WHERE ""编号""=@人员编号 AND ""是否启用""=TRUE);", new { model.人员编号 }))
                throw new ArgumentException("人员不存在或已停用");
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""人员科研能力"" (""人员编号"",""研究专长"",""研究方向标签"",""技术服务能力"",""合作意愿"",""合作说明"",""可用时间"",""每周可用小时"",""可承担项目数"",""当前可接项目"",""可公开展示"",""更新人编号"")
VALUES (@人员编号,@研究专长,@研究方向标签,@技术服务能力,@合作意愿,@合作说明,@可用时间,@每周可用小时,@可承担项目数,@当前可接项目,@可公开展示,@更新人编号)
ON CONFLICT (""人员编号"") DO UPDATE SET
  ""研究专长""=EXCLUDED.""研究专长"", ""研究方向标签""=EXCLUDED.""研究方向标签"", ""技术服务能力""=EXCLUDED.""技术服务能力"", ""合作意愿""=EXCLUDED.""合作意愿"", ""合作说明""=EXCLUDED.""合作说明"", ""可用时间""=EXCLUDED.""可用时间"", ""每周可用小时""=EXCLUDED.""每周可用小时"", ""可承担项目数""=EXCLUDED.""可承担项目数"", ""当前可接项目""=EXCLUDED.""当前可接项目"", ""可公开展示""=EXCLUDED.""可公开展示"", ""更新人编号""=EXCLUDED.""更新人编号"", ""更新时间""=CURRENT_TIMESTAMP
RETURNING ""编号"";", new
            {
                model.人员编号,
                研究专长 = string.IsNullOrWhiteSpace(model.研究专长) ? null : model.研究专长.Trim(),
                研究方向标签 = string.IsNullOrWhiteSpace(model.研究方向标签) ? null : model.研究方向标签.Trim(),
                技术服务能力 = string.IsNullOrWhiteSpace(model.技术服务能力) ? null : model.技术服务能力.Trim(), 合作意愿 = willingness,
                合作说明 = string.IsNullOrWhiteSpace(model.合作说明) ? null : model.合作说明.Trim(),
                可用时间 = string.IsNullOrWhiteSpace(model.可用时间) ? null : model.可用时间.Trim(), model.每周可用小时, model.可承担项目数, model.当前可接项目, model.可公开展示,
                更新人编号 = CurrentUser.编号
            });
            await 保存能力标签(model.人员编号, "研究专长", model.研究方向标签, model.研究专长);
            await 保存能力标签(model.人员编号, "技术服务", model.技术服务能力, null);
            return new { 编号 = id, 合作意愿 = willingness };
        }

        [HttpPost]
        async public Task<object> 保存人员导师关系([FromBody]人员导师关系保存 model)
        {
            if (!await 是人员管理角色()) return StatusCode(403, new { error = "无权维护导师关系" });
            if (model == null || model.导师编号 <= 0 || model.学员编号 <= 0 || model.导师编号 == model.学员编号)
                throw new ArgumentException("导师和学员编号无效且不能相同");
            var relation = string.IsNullOrWhiteSpace(model.关系类型) ? "科研导师" : model.关系类型.Trim();
            var status = string.IsNullOrWhiteSpace(model.状态) ? "有效" : model.状态.Trim();
            if (relation.Length > 30 || status is not ("有效" or "结束" or "待确认")) throw new ArgumentException("导师关系字段无效");
            if (model.开始日期.HasValue && model.结束日期.HasValue && model.结束日期.Value.Date < model.开始日期.Value.Date) throw new ArgumentException("结束日期不能早于开始日期");
            if ((model.备注?.Length ?? 0) > 1000) throw new ArgumentException("备注不能超过1000个字符");
            if (!await 人员均有效(model.导师编号, model.学员编号)) throw new ArgumentException("导师或学员不存在、已停用");
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""人员导师关系"" (""导师编号"",""学员编号"",""关系类型"",""开始日期"",""结束日期"",""状态"",""备注"",""建立人编号"")
VALUES (@导师编号,@学员编号,@关系类型,@开始日期,@结束日期,@状态,@备注,@建立人编号)
ON CONFLICT (""导师编号"",""学员编号"",""关系类型"") DO UPDATE SET ""开始日期""=EXCLUDED.""开始日期"", ""结束日期""=EXCLUDED.""结束日期"", ""状态""=EXCLUDED.""状态"", ""备注""=EXCLUDED.""备注""
RETURNING ""编号"";", new
            {
                model.导师编号, model.学员编号, 关系类型 = relation, 开始日期 = model.开始日期?.Date, 结束日期 = model.结束日期?.Date, 状态 = status,
                备注 = string.IsNullOrWhiteSpace(model.备注) ? null : model.备注.Trim(), 建立人编号 = CurrentUser.编号
            });
            return new { 编号 = id, 状态 = status };
        }

        [HttpGet]
        async public Task<object> 查找科研合作人(string 关键词 = null, string 技术服务 = null, string 合作意愿 = null,
            int? 数量 = null, int? 流程模板编号 = null, int? 项目编号 = null, decimal? 最低每周可用小时 = null)
        {
            return await 查询合作人Core(关键词, 技术服务, 合作意愿, 数量, 流程模板编号, 项目编号, 最低每周可用小时);
        }

        /// <summary>按项目访问范围和人员能力画像推荐团队成员。返回匹配分和当前负载，便于人工确认。</summary>
        [HttpGet]
        async public Task<object> 匹配科研团队成员(string 关键词 = null, string 技术服务 = null,
            int? 流程模板编号 = null, int? 项目编号 = null, decimal? 最低每周可用小时 = null, int? 数量 = null)
        {
            if (项目编号.HasValue != 流程模板编号.HasValue)
                throw new ArgumentException("项目编号和流程模板编号必须同时提供");
            return await 查询合作人Core(关键词, 技术服务, null, 数量, 流程模板编号, 项目编号, 最低每周可用小时);
        }

        private async Task<object> 查询合作人Core(string 关键词, string 技术服务, string 合作意愿, int? 数量,
            int? 流程模板编号, int? 项目编号, decimal? 最低每周可用小时)
        {
            if (项目编号.HasValue && !await 是人员管理角色() && !await CanAccessProjectAsync(流程模板编号.Value, 项目编号.Value))
                return ProjectAccessDenied();
            var limit = Math.Clamp(数量 ?? 50, 1, 200);
            var keyword = string.IsNullOrWhiteSpace(关键词) ? null : 关键词.Trim();
            var service = string.IsNullOrWhiteSpace(技术服务) ? null : 技术服务.Trim();
            var willingness = string.IsNullOrWhiteSpace(合作意愿) ? null : 合作意愿.Trim();
            var rows = await Db.QueryAsync<object>(@"
WITH workload AS (
    SELECT a.""人员编号"", count(*)::integer AS ""当前项目数""
      FROM ""人员项目分工"" a
     WHERE a.""状态"" = '有效'
     GROUP BY a.""人员编号""
), candidates AS (
    SELECT p.""编号"", p.""工号"", p.""姓名"", p.""部门编号"", d.""名称"" AS ""部门名称"", p.""技术职称"",
           c.""研究专长"", c.""研究方向标签"", c.""技术服务能力"", c.""合作意愿"", c.""合作说明"", c.""可用时间"",
           c.""每周可用小时"", c.""可承担项目数"", c.""当前可接项目"", COALESCE(w.""当前项目数"",0) AS ""当前项目数"",
           EXISTS (SELECT 1 FROM ""人员角色"" pr WHERE pr.""人员编号""=p.""编号"" AND pr.""角色编号""=@专家角色组编号) AS ""是否专家"",
           (
             CASE WHEN @关键词 IS NOT NULL AND concat_ws(' ',p.""姓名"",p.""工号"",c.""研究专长"",c.""研究方向标签"") ILIKE '%'||@关键词||'%' THEN 50 ELSE 0 END
             + CASE WHEN @技术服务 IS NOT NULL AND c.""技术服务能力"" ILIKE '%'||@技术服务||'%' THEN 35 ELSE 0 END
             + CASE WHEN c.""合作意愿"" = '可合作' THEN 10 WHEN c.""合作意愿"" = '需邀约' THEN 3 ELSE 0 END
             + CASE WHEN c.""当前可接项目"" THEN 5 ELSE 0 END
           )::integer AS ""匹配分数""
      FROM ""人员"" p JOIN ""人员科研能力"" c ON c.""人员编号""=p.""编号""
      LEFT JOIN ""部门"" d ON d.""编号""=p.""部门编号""
      LEFT JOIN workload w ON w.""人员编号""=p.""编号""
     WHERE p.""是否启用""=TRUE AND c.""可公开展示""=TRUE
       AND c.""合作意愿"" <> '暂不合作' AND c.""当前可接项目""=TRUE
       AND (@关键词 IS NULL OR concat_ws(' ',p.""姓名"",p.""工号"",c.""研究专长"",c.""研究方向标签"") ILIKE '%'||@关键词||'%' OR EXISTS (SELECT 1 FROM ""人员能力标签"" t WHERE t.""人员编号""=p.""编号"" AND t.""标签"" ILIKE '%'||@关键词||'%'))
       AND (@技术服务 IS NULL OR c.""技术服务能力"" ILIKE '%'||@技术服务||'%' OR EXISTS (SELECT 1 FROM ""人员能力标签"" t WHERE t.""人员编号""=p.""编号"" AND t.""标签类型""='技术服务' AND t.""标签"" ILIKE '%'||@技术服务||'%'))
       AND (@合作意愿 IS NULL OR c.""合作意愿""=@合作意愿)
       AND (@最低每周可用小时 IS NULL OR c.""每周可用小时"" >= @最低每周可用小时)
       AND COALESCE(w.""当前项目数"",0) < c.""可承担项目数""
)
SELECT *, GREATEST(""可承担项目数"" - ""当前项目数"", 0)::integer AS ""剩余项目容量""
  FROM candidates
 ORDER BY ""匹配分数"" DESC, ""每周可用小时"" DESC, ""姓名""
 LIMIT @数量;", new
            {
                关键词 = keyword, 技术服务 = service, 合作意愿 = willingness, 数量 = limit, 专家角色组编号,
                最低每周可用小时, 流程模板编号, 项目编号
            });
            await 记录画像访问审计($"/People/匹配科研团队成员?项目编号={项目编号?.ToString() ?? "none"}");
            return rows;
        }

        [HttpPost]
        async public Task<object> 保存科研团队([FromBody]科研团队保存 model)
        {
            if (!await 是人员管理角色()) return StatusCode(403, new { error = "无权维护科研团队" });
            if (model == null || string.IsNullOrWhiteSpace(model.团队名称) || model.负责人编号 <= 0) throw new ArgumentException("团队名称和负责人不能为空");
            if (model.团队名称.Trim().Length > 200 || (model.研究方向?.Length ?? 0) > 2000 || (model.技术服务范围?.Length ?? 0) > 2000 || (model.合作说明?.Length ?? 0) > 1000)
                throw new ArgumentException("团队字段超出长度限制");
            var status = string.IsNullOrWhiteSpace(model.状态) ? "筹建中" : model.状态.Trim();
            if (status is not ("筹建中" or "运行中" or "已解散")) throw new ArgumentException("团队状态无效");
            if (!await 人员均有效(model.负责人编号)) throw new ArgumentException("团队负责人不存在或已停用");
            if (model.编号.HasValue)
            {
                var updated = await Db.ExecuteAsync(@"
UPDATE ""科研团队"" SET ""团队名称""=@团队名称,""负责人编号""=@负责人编号,""研究方向""=@研究方向,""技术服务范围""=@技术服务范围,""合作说明""=@合作说明,""状态""=@状态,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@编号;", new
                {
                    编号 = model.编号.Value, 团队名称 = model.团队名称.Trim(), model.负责人编号,
                    研究方向 = string.IsNullOrWhiteSpace(model.研究方向) ? null : model.研究方向.Trim(), 技术服务范围 = string.IsNullOrWhiteSpace(model.技术服务范围) ? null : model.技术服务范围.Trim(),
                    合作说明 = string.IsNullOrWhiteSpace(model.合作说明) ? null : model.合作说明.Trim(), 状态 = status
                });
                if (updated == 0) throw new ArgumentException("团队不存在");
                return new { 编号 = model.编号.Value, 状态 = status };
            }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研团队"" (""团队名称"",""负责人编号"",""研究方向"",""技术服务范围"",""合作说明"",""状态"",""建立人编号"")
VALUES (@团队名称,@负责人编号,@研究方向,@技术服务范围,@合作说明,@状态,@建立人编号) RETURNING ""编号"";", new
            {
                团队名称 = model.团队名称.Trim(), model.负责人编号, 研究方向 = string.IsNullOrWhiteSpace(model.研究方向) ? null : model.研究方向.Trim(),
                技术服务范围 = string.IsNullOrWhiteSpace(model.技术服务范围) ? null : model.技术服务范围.Trim(), 合作说明 = string.IsNullOrWhiteSpace(model.合作说明) ? null : model.合作说明.Trim(), 状态 = status, 建立人编号 = CurrentUser.编号
            });
            return new { 编号 = id, 状态 = status };
        }

        [HttpPost]
        async public Task<object> 保存科研团队成员([FromBody]科研团队成员保存 model)
        {
            if (model == null || model.团队编号 <= 0 || model.人员编号 <= 0) throw new ArgumentException("团队和人员编号无效");
            var leader = await Db.QuerySingleOrDefaultAsync<int?>("SELECT \"负责人编号\" FROM \"科研团队\" WHERE \"编号\"=@团队编号;", new { model.团队编号 });
            if (!leader.HasValue) return NotFound(new { error = "团队不存在" });
            if (leader.Value != CurrentUser.编号 && !await 是人员管理角色()) return StatusCode(403, new { error = "无权维护该团队成员" });
            if (!await 人员均有效(model.人员编号)) throw new ArgumentException("人员不存在或已停用");
            var role = string.IsNullOrWhiteSpace(model.成员角色) ? "成员" : model.成员角色.Trim();
            var status = string.IsNullOrWhiteSpace(model.状态) ? "有效" : model.状态.Trim();
            if (role.Length > 50 || status is not ("有效" or "退出" or "待确认")) throw new ArgumentException("团队成员字段无效");
            if (model.加入日期.HasValue && model.退出日期.HasValue && model.退出日期.Value.Date < model.加入日期.Value.Date) throw new ArgumentException("退出日期不能早于加入日期");
            if ((model.备注?.Length ?? 0) > 1000) throw new ArgumentException("备注不能超过1000个字符");
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研团队成员"" (""团队编号"",""人员编号"",""成员角色"",""加入日期"",""退出日期"",""状态"",""备注"",""建立人编号"")
VALUES (@团队编号,@人员编号,@成员角色,@加入日期,@退出日期,@状态,@备注,@建立人编号)
ON CONFLICT (""团队编号"",""人员编号"") DO UPDATE SET ""成员角色""=EXCLUDED.""成员角色"", ""加入日期""=EXCLUDED.""加入日期"", ""退出日期""=EXCLUDED.""退出日期"", ""状态""=EXCLUDED.""状态"", ""备注""=EXCLUDED.""备注""
RETURNING ""编号"";", new
            {
                model.团队编号, model.人员编号, 成员角色 = role, 加入日期 = model.加入日期?.Date, 退出日期 = model.退出日期?.Date, 状态 = status,
                备注 = string.IsNullOrWhiteSpace(model.备注) ? null : model.备注.Trim(), 建立人编号 = CurrentUser.编号
            });
            return new { 编号 = id, 状态 = status };
        }

        [HttpPost]
        async public Task<object> 保存人员项目分工([FromBody]人员项目分工保存 model)
        {
            if (model == null || model.流程模板编号 <= 0 || model.项目编号 <= 0 || model.人员编号 <= 0 || string.IsNullOrWhiteSpace(model.分工类型))
                throw new ArgumentException("项目分工字段不完整");
            if ((model.工作量占比.HasValue && (model.工作量占比 < 0 || model.工作量占比 > 100)) || (model.备注?.Length ?? 0) > 1000)
                throw new ArgumentException("项目分工字段无效");
            if (!await 是人员管理角色() && !await CanAccessProjectAsync(model.流程模板编号, model.项目编号)) return ProjectAccessDenied();
            if (!await 人员均有效(model.人员编号)) throw new ArgumentException("分工人员不存在或已停用");
            var status = string.IsNullOrWhiteSpace(model.状态) ? "有效" : model.状态.Trim();
            if (status is not ("有效" or "结束" or "待确认")) throw new ArgumentException("分工状态无效");
            if (model.开始日期.HasValue && model.结束日期.HasValue && model.结束日期.Value.Date < model.开始日期.Value.Date) throw new ArgumentException("结束日期不能早于开始日期");
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""人员项目分工"" (""流程模板编号"",""项目编号"",""人员编号"",""分工类型"",""工作量占比"",""开始日期"",""结束日期"",""状态"",""备注"",""建立人编号"")
VALUES (@流程模板编号,@项目编号,@人员编号,@分工类型,@工作量占比,@开始日期,@结束日期,@状态,@备注,@建立人编号)
ON CONFLICT (""流程模板编号"",""项目编号"",""人员编号"",""分工类型"") DO UPDATE SET ""工作量占比""=EXCLUDED.""工作量占比"", ""开始日期""=EXCLUDED.""开始日期"", ""结束日期""=EXCLUDED.""结束日期"", ""状态""=EXCLUDED.""状态"", ""备注""=EXCLUDED.""备注""
RETURNING ""编号"";", new
            {
                model.流程模板编号, model.项目编号, model.人员编号, 分工类型 = model.分工类型.Trim(), model.工作量占比, 开始日期 = model.开始日期?.Date, 结束日期 = model.结束日期?.Date,
                状态 = status, 备注 = string.IsNullOrWhiteSpace(model.备注) ? null : model.备注.Trim(), 建立人编号 = CurrentUser.编号
            });
            return new { 编号 = id, 状态 = status };
        }

        /// <summary>分页前的轻量团队目录查询，成员和项目明细通过详情接口按权限读取。</summary>
        [HttpGet]
        async public Task<object> 查询科研团队(string 关键词 = null, string 状态 = null, int? 负责人编号 = null, int? 数量 = null)
        {
            var limit = Math.Clamp(数量 ?? 100, 1, 200);
            var rows = await Db.QueryAsync<object>(@"
SELECT t.""编号"", t.""团队名称"", t.""负责人编号"", p.""姓名"" AS ""负责人姓名"", t.""研究方向"",
       t.""技术服务范围"", t.""合作说明"", t.""状态"", count(m.""人员编号"") FILTER (WHERE m.""状态""='有效')::integer AS ""有效成员数""
  FROM ""科研团队"" t JOIN ""人员"" p ON p.""编号""=t.""负责人编号""
  LEFT JOIN ""科研团队成员"" m ON m.""团队编号""=t.""编号""
 WHERE (@关键词 IS NULL OR concat_ws(' ',t.""团队名称"",t.""研究方向"",t.""技术服务范围"") ILIKE '%'||@关键词||'%')
   AND (@状态 IS NULL OR t.""状态""=@状态) AND (@负责人编号 IS NULL OR t.""负责人编号""=@负责人编号)
 GROUP BY t.""编号"", p.""姓名""
 ORDER BY t.""状态"", t.""团队名称"" LIMIT @数量;", new
            {
                关键词 = string.IsNullOrWhiteSpace(关键词) ? null : 关键词.Trim(),
                状态 = string.IsNullOrWhiteSpace(状态) ? null : 状态.Trim(), 负责人编号, 数量 = limit
            });
            await 记录画像访问审计("/People/查询科研团队");
            return rows;
        }

        /// <summary>读取团队成员、导师关系和项目分工；涉及项目的分工按项目级访问权过滤。</summary>
        [HttpGet]
        async public Task<object> 获取科研团队详情(long 团队编号, int? 流程模板编号 = null, int? 项目编号 = null)
        {
            if (团队编号 <= 0) throw new ArgumentException("团队编号无效");
            if (项目编号.HasValue != 流程模板编号.HasValue) throw new ArgumentException("项目编号和流程模板编号必须同时提供");
            var team = await Db.QuerySingleOrDefaultAsync<object>(@"
SELECT t.""编号"", t.""团队名称"", t.""负责人编号"", p.""姓名"" AS ""负责人姓名"", t.""研究方向"", t.""技术服务范围"", t.""合作说明"", t.""状态""
  FROM ""科研团队"" t JOIN ""人员"" p ON p.""编号""=t.""负责人编号"" WHERE t.""编号""=@团队编号;", new { 团队编号 });
            if (team == null) return NotFound(new { error = "团队不存在" });
            var leaderId = await Db.QuerySingleAsync<int>("SELECT \"负责人编号\" FROM \"科研团队\" WHERE \"编号\"=@团队编号;", new { 团队编号 });
            var isManager = await 是人员管理角色();
            var isMember = await Db.QuerySingleAsync<bool>("SELECT EXISTS (SELECT 1 FROM \"科研团队成员\" WHERE \"团队编号\"=@团队编号 AND \"人员编号\"=@人员编号 AND \"状态\"='有效');", new { 团队编号, 人员编号 = CurrentUser.编号 });
            if (!isManager && leaderId != CurrentUser.编号 && !isMember) return StatusCode(403, new { error = "无权查看该科研团队" });
            if (项目编号.HasValue && !isManager && !await CanAccessProjectAsync(流程模板编号.Value, 项目编号.Value)) return ProjectAccessDenied();
            var members = await Db.QueryAsync<object>(@"
SELECT m.""编号"", m.""人员编号"", p.""工号"", p.""姓名"", d.""名称"" AS ""部门名称"", m.""成员角色"", m.""加入日期"", m.""退出日期"", m.""状态"", m.""备注"",
       c.""研究专长"", c.""技术服务能力"", c.""合作意愿"", c.""可用时间"", c.""每周可用小时""
  FROM ""科研团队成员"" m JOIN ""人员"" p ON p.""编号""=m.""人员编号""
  LEFT JOIN ""部门"" d ON d.""编号""=p.""部门编号"" LEFT JOIN ""人员科研能力"" c ON c.""人员编号""=p.""编号""
 WHERE m.""团队编号""=@团队编号 ORDER BY m.""状态"", p.""姓名"";", new { 团队编号 });
            var assignments = await Db.QueryAsync<object>(@"
SELECT a.""编号"", a.""流程模板编号"", a.""项目编号"", p.""姓名"", a.""人员编号"", a.""分工类型"", a.""工作量占比"", a.""开始日期"", a.""结束日期"", a.""状态"", a.""备注""
  FROM ""人员项目分工"" a JOIN ""人员"" p ON p.""编号""=a.""人员编号""
 WHERE EXISTS (SELECT 1 FROM ""科研团队成员"" m WHERE m.""团队编号""=@团队编号 AND m.""人员编号""=a.""人员编号"")
   AND (@流程模板编号 IS NULL OR a.""流程模板编号""=@流程模板编号)
   AND (@项目编号 IS NULL OR a.""项目编号""=@项目编号)
 ORDER BY a.""状态"", p.""姓名"";", new { 团队编号, 流程模板编号, 项目编号 });
            await 记录画像访问审计($"/People/获取科研团队详情?团队编号={团队编号}");
            return new { 团队 = team, 成员 = members, 项目分工 = assignments };
        }

        /// <summary>按人员或项目读取分工台账，项目参数存在时必须通过项目级访问检查。</summary>
        [HttpGet]
        async public Task<object> 查询人员项目分工(int? 人员编号 = null, int? 流程模板编号 = null, int? 项目编号 = null)
        {
            if (项目编号.HasValue != 流程模板编号.HasValue) throw new ArgumentException("项目编号和流程模板编号必须同时提供");
            var personId = 人员编号 ?? CurrentUser.编号;
            if (personId != CurrentUser.编号 && !await 是人员管理角色()) return StatusCode(403, new { error = "无权查看该人员项目分工" });
            if (项目编号.HasValue && !await 是人员管理角色() && !await CanAccessProjectAsync(流程模板编号.Value, 项目编号.Value)) return ProjectAccessDenied();
            var rows = await Db.QueryAsync<object>(@"
SELECT a.""编号"", a.""流程模板编号"", a.""项目编号"", a.""人员编号"", p.""姓名"", a.""分工类型"", a.""工作量占比"", a.""开始日期"", a.""结束日期"", a.""状态"", a.""备注""
  FROM ""人员项目分工"" a JOIN ""人员"" p ON p.""编号""=a.""人员编号""
 WHERE (@人员编号 IS NULL OR a.""人员编号""=@人员编号)
   AND (@流程模板编号 IS NULL OR a.""流程模板编号""=@流程模板编号)
   AND (@项目编号 IS NULL OR a.""项目编号""=@项目编号)
 ORDER BY a.""状态"", p.""姓名"", a.""项目编号"";", new { 人员编号 = personId, 流程模板编号, 项目编号 });
            await 记录画像访问审计("/People/查询人员项目分工");
            return rows;
        }

        /// <summary>按周查询专家可用时间；专家本人和人员管理角色可查看。</summary>
        [HttpGet]
        async public Task<object> 查询专家可用时间(int? 专家编号 = null)
        {
            var expertId = 专家编号 ?? CurrentUser.编号;
            if (expertId != CurrentUser.编号 && !await 是人员管理角色()) return StatusCode(403, new { error = "无权查看该专家可用时间" });
            return await Db.QueryAsync<object>(@"
SELECT t.""编号"", t.""专家编号"", p.""姓名"", t.""星期"", t.""开始时间"", t.""结束时间"", t.""时区"", t.""是否启用"", t.""备注""
  FROM ""专家可用时间"" t JOIN ""人员"" p ON p.""编号""=t.""专家编号""
 WHERE t.""专家编号""=@专家编号 ORDER BY t.""星期"", t.""开始时间"", t.""编号"";", new { 专家编号 = expertId });
        }

        /// <summary>保存专家周可用时间。该接口只维护本地可用时间，不代表外部专家已接受邀请。</summary>
        [HttpPost]
        async public Task<object> 保存专家可用时间([FromBody]专家可用时间保存 model)
        {
            if (model == null || model.专家编号 <= 0 || model.星期 < 1 || model.星期 > 7 || model.开始时间 >= model.结束时间)
                throw new ArgumentException("专家、星期和时间范围无效");
            if (model.专家编号 != CurrentUser.编号 && !await 是人员管理角色()) return StatusCode(403, new { error = "无权维护该专家可用时间" });
            if (!await 人员均有效(model.专家编号)) throw new ArgumentException("专家不存在或已停用");
            if (!await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (
    SELECT 1 FROM ""人员角色"" pr
    WHERE pr.""人员编号""=@专家编号 AND pr.""角色编号""=@专家角色组编号
);", new { model.专家编号, 专家角色组编号 }))
                throw new ArgumentException("该人员尚未纳入评审专家角色，不能维护专家可用时间");
            if ((model.备注?.Length ?? 0) > 500 || (model.时区?.Length ?? 0) > 64) throw new ArgumentException("可用时间字段超出限制");
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""专家可用时间"" (""专家编号"",""星期"",""开始时间"",""结束时间"",""时区"",""是否启用"",""备注"",""建立人编号"")
VALUES (@专家编号,@星期,@开始时间,@结束时间,@时区,@是否启用,@备注,@建立人编号)
ON CONFLICT (""专家编号"",""星期"",""开始时间"",""结束时间"") DO UPDATE SET ""时区""=EXCLUDED.""时区"",""是否启用""=EXCLUDED.""是否启用"",""备注""=EXCLUDED.""备注"",""更新时间""=CURRENT_TIMESTAMP
RETURNING ""编号"";", new
            {
                model.专家编号, model.星期, model.开始时间, model.结束时间,
                时区 = string.IsNullOrWhiteSpace(model.时区) ? "Asia/Shanghai" : model.时区.Trim(), model.是否启用,
                备注 = string.IsNullOrWhiteSpace(model.备注) ? null : model.备注.Trim(), 建立人编号 = CurrentUser.编号
            });
            return new { 编号 = id, 状态 = "已保存" };
        }

        /// <summary>查询外部专家邀请队列和真实回执审计。</summary>
        [HttpGet]
        async public Task<object> 查询外部专家邀请(long? 邀请编号 = null, string 状态 = null)
        {
            if (!await 是人员管理角色()) return StatusCode(403, new { error = "无权查看外部专家邀请" });
            var invitations = await Db.QueryAsync<object>(@"
SELECT i.*, p.""姓名"" AS ""专家姓名""
  FROM ""外部专家邀请"" i LEFT JOIN ""人员"" p ON p.""编号""=i.""专家编号""
 WHERE (@邀请编号 IS NULL OR i.""编号""=@邀请编号) AND (@状态 IS NULL OR i.""状态""=@状态)
 ORDER BY i.""创建时间"" DESC, i.""编号"" DESC LIMIT 200;", new { 邀请编号, 状态 = string.IsNullOrWhiteSpace(状态) ? null : 状态.Trim() });
            var audit = 邀请编号.HasValue ? await Db.QueryAsync<object>(@"
SELECT * FROM ""外部专家邀请审计"" WHERE ""邀请编号""=@邀请编号 ORDER BY ""操作时间"" DESC,""编号"" DESC;", new { 邀请编号 }) : Array.Empty<object>();
            return new { 邀请 = invitations, 审计 = audit };
        }

        /// <summary>登记外部专家邀请。未配置真实适配器时只进入“未配置”，不会伪造已发送。</summary>
        [HttpPost]
        async public Task<object> 创建外部专家邀请([FromBody]外部专家邀请创建 model)
        {
            if (!await 是人员管理角色()) return StatusCode(403, new { error = "无权创建外部专家邀请" });
            if (model == null || string.IsNullOrWhiteSpace(model.外部姓名) || string.IsNullOrWhiteSpace(model.外部邮箱) || string.IsNullOrWhiteSpace(model.邀请场景) || string.IsNullOrWhiteSpace(model.幂等键))
                throw new ArgumentException("外部姓名、邮箱、邀请场景和幂等键不能为空");
            if (model.外部姓名.Trim().Length > 100 || model.外部邮箱.Trim().Length > 320 || (model.外部机构?.Trim().Length ?? 0) > 200 || model.邀请场景.Trim().Length > 100 || model.幂等键.Trim().Length > 200)
                throw new ArgumentException("邀请字段超出限制");
            if (!System.Text.RegularExpressions.Regex.IsMatch(model.外部邮箱.Trim(), @"^[^\s@]+@[^\s@]+\.[^\s@]+$")) throw new ArgumentException("外部邮箱格式无效");
            if (model.流程模板编号.HasValue != model.项目编号.HasValue)
                throw new ArgumentException("项目邀请必须同时提供流程模板编号和项目编号");
            if (model.流程模板编号.HasValue && !await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (SELECT 1 FROM ""流程模板"" WHERE ""编号""=@流程模板编号);", new { model.流程模板编号 }))
                return NotFound(new { error = "流程模板不存在" });
            if (model.专家编号.HasValue)
            {
                if (!await 人员均有效(model.专家编号.Value)) return NotFound(new { error = "关联专家不存在或已停用" });
                if (!await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (
    SELECT 1 FROM ""人员角色"" pr
    WHERE pr.""人员编号""=@专家编号 AND pr.""角色编号""=@专家角色组编号
);", new { model.专家编号, 专家角色组编号 }))
                    return Conflict(new { error = "关联人员尚未纳入评审专家角色" });
            }
            if (model.外部系统编号.HasValue && !await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (SELECT 1 FROM ""外部系统"" WHERE ""编号""=@外部系统编号);", new { model.外部系统编号 }))
                return NotFound(new { error = "外部系统不存在" });
            var payload = Newtonsoft.Json.JsonConvert.SerializeObject(model);
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""外部专家邀请"" (""专家编号"",""外部姓名"",""外部邮箱"",""外部机构"",""邀请场景"",""流程模板编号"",""项目编号"",""外部系统编号"",""状态"",""幂等键"",""请求负载"",""响应摘要"",""创建人编号"")
VALUES (@专家编号,@外部姓名,@外部邮箱,@外部机构,@邀请场景,@流程模板编号,@项目编号,@外部系统编号,'未配置',@幂等键,@请求负载::jsonb,'未配置真实邮件或身份适配器，未发送外部邀请',@创建人编号)
ON CONFLICT (""幂等键"") DO UPDATE SET ""更新时间""=CURRENT_TIMESTAMP
RETURNING ""编号"";", new
            {
                model.专家编号, 外部姓名 = model.外部姓名.Trim(), 外部邮箱 = model.外部邮箱.Trim(), 外部机构 = string.IsNullOrWhiteSpace(model.外部机构) ? null : model.外部机构.Trim(),
                邀请场景 = model.邀请场景.Trim(), model.流程模板编号, model.项目编号, model.外部系统编号, 幂等键 = model.幂等键.Trim(), 请求负载 = payload, 创建人编号 = CurrentUser.编号
            });
            return new { 编号 = id, 状态 = "未配置", 真实发送 = false, 提示 = "外部专家邀请已登记但未发送；配置真实邮件/身份适配器并取得回执后才能标记已发送" };
        }

        /// <summary>由真实外部适配器回写邀请回执；本地接口禁止伪造已发送。</summary>
        [HttpPost]
        async public Task<object> 回写外部专家邀请回执([FromBody]外部专家邀请回执 model)
        {
            if (!await 是人员管理角色()) return StatusCode(403, new { error = "无权回写外部专家邀请" });
            if (model == null || model.邀请编号 <= 0 || string.IsNullOrWhiteSpace(model.状态)) throw new ArgumentException("邀请编号和状态不能为空");
            if (model.状态 is not ("已发送" or "发送失败" or "已接受" or "已拒绝" or "已取消")) throw new ArgumentException("邀请状态无效");
            if (model.状态 is "已发送" or "发送失败" or "已接受" or "已拒绝")
                if (!model.真实回执) return Conflict(new { error = "外部发送、失败或接受/拒绝状态必须来自真实适配器回执", realReceiptRequired = true });
            if (model.状态 is "已发送" or "已接受" or "已拒绝")
                if (string.IsNullOrWhiteSpace(model.外部消息编号)) return Conflict(new { error = "该状态必须提供真实外部消息编号", externalMessageIdRequired = true });
            if (model.状态 == "发送失败" && string.IsNullOrWhiteSpace(model.失败原因)) throw new ArgumentException("发送失败必须填写失败原因");
            var source = model.状态 == "已取消" ? "人工取消" : model.真实回执 ? "真实邮件服务" : "配置检查";
            var changed = await Db.ExecuteAsync(@"
UPDATE ""外部专家邀请"" SET ""状态""=@状态,""外部消息编号""=@外部消息编号,""回执来源""=@回执来源,""响应摘要""=@响应摘要,""失败原因""=@失败原因,""发送时间""=CASE WHEN @状态='已发送' THEN COALESCE(""发送时间"",CURRENT_TIMESTAMP) ELSE ""发送时间"" END,""完成时间""=CASE WHEN @状态 IN ('已发送','发送失败','已接受','已拒绝','已取消') THEN CURRENT_TIMESTAMP ELSE ""完成时间"" END,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@邀请编号;", new
            { model.邀请编号, model.状态, 外部消息编号 = 空值(model.外部消息编号), 回执来源 = source, 响应摘要 = 空值(model.响应摘要), 失败原因 = 空值(model.失败原因) });
            if (changed == 0) return NotFound(new { error = "邀请不存在" });
            return new { 编号 = model.邀请编号, 状态 = model.状态, 真实发送 = model.状态 == "已发送" && model.真实回执 };
        }

        #endregion

        private async Task<bool> 是人员管理角色() => await HasAnyRoleAsync("系统管理员", "科研管理员");

        private async Task<bool> 人员均有效(params int[] 人员编号列表)
        {
            if (人员编号列表 == null || 人员编号列表.Length == 0) return false;
            var count = await Db.QuerySingleAsync<int>(@"SELECT count(*)::integer FROM ""人员"" WHERE ""编号""=ANY(@人员编号列表) AND ""是否启用""=TRUE;", new { 人员编号列表 });
            return count == 人员编号列表.Distinct().Count();
        }

        private async Task 保存能力标签(int 人员编号, string 标签类型, params string[] 来源字段)
        {
            if (来源字段 == null || 标签类型 is not ("研究专长" or "技术服务")) return;
            var labels = 来源字段
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .SelectMany(value => value.Split(new[] { ',', '，', ';', '；', '、', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                .Select(value => value.Trim())
                .Where(value => value.Length > 0 && value.Length <= 100)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            await Db.ExecuteAsync("DELETE FROM \"人员能力标签\" WHERE \"人员编号\"=@人员编号 AND \"标签类型\"=@标签类型;", new { 人员编号, 标签类型 });
            foreach (var label in labels)
            {
                await Db.ExecuteAsync(@"
INSERT INTO ""人员能力标签"" (""人员编号"",""标签类型"",""标签"",""来源"",""建立人编号"")
VALUES (@人员编号,@标签类型,@标签,'人员维护',@建立人编号)
ON CONFLICT (""人员编号"",""标签类型"",""标签"") DO UPDATE SET ""建立人编号""=EXCLUDED.""建立人编号"";", new { 人员编号, 标签类型, 标签 = label, 建立人编号 = CurrentUser.编号 });
            }
        }

        private async Task 记录画像访问审计(string path)
        {
            try
            {
                await Db.ExecuteAsync(@"
INSERT INTO ""操作审计"" (""人员编号"",""请求编号"",""请求方法"",""请求路径"",""响应状态"",""客户端IP"",""耗时毫秒"",""是否成功"")
VALUES (@人员编号,@请求编号,'GET',LEFT(@请求路径,500),200,@客户端IP,0,TRUE);", new
                {
                    人员编号 = CurrentUser.编号,
                    请求编号 = string.IsNullOrWhiteSpace(HttpContext.TraceIdentifier) ? Guid.NewGuid().ToString("N") : HttpContext.TraceIdentifier,
                    请求路径 = path,
                    客户端IP = HttpContext.Connection.RemoteIpAddress?.ToString()
                });
            }
            catch (Exception ex)
            {
                Log?.Warn("科研能力画像访问审计写入失败", ex);
            }
        }

        private static string 空值(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    }
}
