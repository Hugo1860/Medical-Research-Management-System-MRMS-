using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using ScientificResearch.Infrastucture;

namespace ScientificResearch.Controllers
{
    /// <summary>
    /// 科研文档共享、科研服务催办和统一消息队列。
    /// 外部短信、邮件、微信或企业微信适配器未配置时只会产生“未配置/待发送”队列，不伪造发送成功。
    /// </summary>
    public sealed class DocumentMessagingController : BaseController
    {
        private async Task<bool> IsManager() => await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办");

        private async Task<bool> CanReadDocument(long documentId)
        {
            if (await IsManager()) return true;
            return await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (
  SELECT 1 FROM ""科研文档"" d
  LEFT JOIN ""人员"" me ON me.""编号""=@人员编号
  WHERE d.""编号""=@文档编号 AND d.""是否启用""=TRUE
    AND (d.""授权模式""='公开' OR EXISTS (
      SELECT 1 FROM ""科研文档授权"" a
      WHERE a.""文档编号""=d.""编号"" AND a.""可下载""=TRUE AND a.""是否撤销""=FALSE
        AND (a.""有效开始"" IS NULL OR a.""有效开始""<=CURRENT_TIMESTAMP)
        AND (a.""有效结束"" IS NULL OR a.""有效结束"">=CURRENT_TIMESTAMP)
        AND (
          (a.""授权类型""='所有人' AND a.""授权对象编号""=0)
          OR (a.""授权类型""='人员' AND a.""授权对象编号""=@人员编号)
          OR (a.""授权类型""='部门' AND a.""授权对象编号""=me.""部门编号"")
          OR (a.""授权类型""='角色' AND EXISTS (
                SELECT 1 FROM ""人员角色"" pr
                WHERE pr.""人员编号""=@人员编号 AND pr.""角色编号""=a.""授权对象编号""
              ))
        )
    ))
);", new { 文档编号 = documentId, 人员编号 = CurrentUser.编号 });
        }

        [HttpGet]
        public async Task<object> 查询科研文档(string 文档分类 = null, string 关键词 = null, bool? 仅启用 = true)
        {
            var manager = await IsManager();
            return await Db.QueryAsync<object>(@"
SELECT d.""编号"",d.""文档编码"",d.""文档名称"",d.""文档分类"",d.""文档说明"",d.""授权模式"",d.""是否启用"",
       d.""创建时间"",d.""更新时间"",v.""编号"" AS ""当前版本编号"",v.""版本号"",v.""文件名称"",v.""文件大小"",v.""SHA256"",v.""版本状态"",
       COALESCE(dl.""下载次数"",0) AS ""下载次数""
  FROM ""科研文档"" d
  LEFT JOIN ""科研文档版本"" v ON v.""文档编号""=d.""编号"" AND v.""版本状态""='生效'
  LEFT JOIN LATERAL (SELECT count(*) AS ""下载次数"" FROM ""科研文档下载日志"" x WHERE x.""文档编号""=d.""编号"" AND x.""下载结果""='允许') dl ON TRUE
  LEFT JOIN ""人员"" me ON me.""编号""=@人员编号
 WHERE (@仅启用 IS NULL OR d.""是否启用""=@仅启用)
   AND (@文档分类 IS NULL OR d.""文档分类""=@文档分类)
   AND (@关键词 IS NULL OR d.""文档名称"" ILIKE '%'||@关键词||'%' OR d.""文档编码"" ILIKE '%'||@关键词||'%')
   AND (@管理者 OR d.""授权模式""='公开' OR EXISTS (
      SELECT 1 FROM ""科研文档授权"" a
      WHERE a.""文档编号""=d.""编号"" AND a.""可下载""=TRUE AND a.""是否撤销""=FALSE
        AND (a.""有效开始"" IS NULL OR a.""有效开始""<=CURRENT_TIMESTAMP)
        AND (a.""有效结束"" IS NULL OR a.""有效结束"">=CURRENT_TIMESTAMP)
        AND ((a.""授权类型""='所有人' AND a.""授权对象编号""=0)
          OR (a.""授权类型""='人员' AND a.""授权对象编号""=@人员编号)
          OR (a.""授权类型""='部门' AND a.""授权对象编号""=me.""部门编号"")
          OR (a.""授权类型""='角色' AND EXISTS (SELECT 1 FROM ""人员角色"" pr WHERE pr.""人员编号""=@人员编号 AND pr.""角色编号""=a.""授权对象编号"")))
   ))
 ORDER BY d.""是否启用"" DESC,d.""更新时间"" DESC,d.""编号"" DESC;", new
            {
                文档分类 = 空值(文档分类), 关键词 = 空值(关键词), 仅启用,
                人员编号 = CurrentUser.编号, 管理者 = manager
            });
        }

        [HttpPost]
        public async Task<object> 保存科研文档([FromBody] 科研文档保存 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权维护科研文档" });
            if (model == null || string.IsNullOrWhiteSpace(model.文档编码) || string.IsNullOrWhiteSpace(model.文档名称))
                throw new ArgumentException("文档编码和名称不能为空");
            var args = new
            {
                model.编号, 文档编码 = model.文档编码.Trim(), 文档名称 = model.文档名称.Trim(),
                文档分类 = 空值(model.文档分类) ?? "未分类", model.文档说明,
                授权模式 = string.IsNullOrWhiteSpace(model.授权模式) ? "按授权" : model.授权模式.Trim(),
                model.是否启用, 创建人编号 = CurrentUser.编号
            };
            if (model.编号.HasValue)
            {
                var changed = await Db.ExecuteAsync(@"
UPDATE ""科研文档"" SET ""文档编码""=@文档编码,""文档名称""=@文档名称,""文档分类""=@文档分类,""文档说明""=@文档说明,
       ""授权模式""=@授权模式,""是否启用""=@是否启用,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号;", args);
                if (changed == 0) return NotFound(new { error = "文档不存在" });
                return new { 编号 = model.编号.Value };
            }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研文档"" (""文档编码"",""文档名称"",""文档分类"",""文档说明"",""授权模式"",""是否启用"",""创建人编号"")
VALUES (@文档编码,@文档名称,@文档分类,@文档说明,@授权模式,@是否启用,@创建人编号) RETURNING ""编号"";", args);
            return new { 编号 = id };
        }

        [HttpGet]
        public async Task<object> 查询科研文档版本(long 文档编号)
        {
            if (!await CanReadDocument(文档编号)) return StatusCode(403, new { error = "无权查看该文档" });
            return await Db.QueryAsync<object>(@"
SELECT ""编号"",""文档编号"",""版本号"",""文件名称"",""文件大小"",""SHA256"",""版本状态"",""上传人编号"",""发布日期"",""创建时间""
  FROM ""科研文档版本"" WHERE ""文档编号""=@文档编号 ORDER BY ""创建时间"" DESC,""编号"" DESC;", new { 文档编号 });
        }

        [HttpPost]
        public async Task<object> 保存科研文档版本([FromBody] 科研文档版本保存 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权维护文档版本" });
            if (model == null || model.文档编号 <= 0 || string.IsNullOrWhiteSpace(model.版本号) || string.IsNullOrWhiteSpace(model.文件名称) || string.IsNullOrWhiteSpace(model.文件路径))
                throw new ArgumentException("文档、版本号、文件名称和文件路径不能为空");
            if (model.版本状态 == "生效")
                await Db.ExecuteAsync("UPDATE \"科研文档版本\" SET \"版本状态\"='撤销' WHERE \"文档编号\"=@文档编号 AND \"版本状态\"='生效' AND (\"编号\"<>@编号 OR @编号 IS NULL);", new { model.文档编号, model.编号 });
            var args = new
            {
                model.编号, model.文档编号, 版本号 = model.版本号.Trim(), 文件名称 = model.文件名称.Trim(), 文件路径 = model.文件路径.Trim(),
                model.文件大小, model.SHA256, 版本状态 = string.IsNullOrWhiteSpace(model.版本状态) ? "草稿" : model.版本状态.Trim(),
                model.发布日期, 上传人编号 = CurrentUser.编号
            };
            if (model.编号.HasValue)
            {
                var changed = await Db.ExecuteAsync(@"
UPDATE ""科研文档版本"" SET ""文档编号""=@文档编号,""版本号""=@版本号,""文件名称""=@文件名称,""文件路径""=@文件路径,
""文件大小""=@文件大小,""SHA256""=@SHA256,""版本状态""=@版本状态,""发布日期""=@发布日期 WHERE ""编号""=@编号;", args);
                if (changed == 0) return NotFound(new { error = "文档版本不存在" });
                return new { 编号 = model.编号.Value };
            }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研文档版本"" (""文档编号"",""版本号"",""文件名称"",""文件路径"",""文件大小"",""SHA256"",""版本状态"",""上传人编号"",""发布日期"")
VALUES (@文档编号,@版本号,@文件名称,@文件路径,@文件大小,@SHA256,@版本状态,@上传人编号,@发布日期) RETURNING ""编号"";", args);
            return new { 编号 = id };
        }

        [HttpPost]
        public async Task<object> 保存科研文档授权([FromBody] 科研文档授权保存 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权维护文档授权" });
            if (model == null || model.文档编号 <= 0 || string.IsNullOrWhiteSpace(model.授权类型)) throw new ArgumentException("文档授权字段无效");
            var args = new
            {
                model.编号, model.文档编号, 授权类型 = model.授权类型.Trim(), model.授权对象编号,
                model.可下载, model.有效开始, model.有效结束, model.是否撤销, 授权人编号 = CurrentUser.编号
            };
            if (model.编号.HasValue)
            {
                await Db.ExecuteAsync(@"
UPDATE ""科研文档授权"" SET ""文档编号""=@文档编号,""授权类型""=@授权类型,""授权对象编号""=@授权对象编号,""可下载""=@可下载,
""有效开始""=@有效开始,""有效结束""=@有效结束,""是否撤销""=@是否撤销,""撤销时间""=CASE WHEN @是否撤销 THEN CURRENT_TIMESTAMP ELSE NULL END WHERE ""编号""=@编号;", args);
                return new { 编号 = model.编号.Value };
            }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研文档授权"" (""文档编号"",""授权类型"",""授权对象编号"",""可下载"",""有效开始"",""有效结束"",""是否撤销"",""授权人编号"")
VALUES (@文档编号,@授权类型,@授权对象编号,@可下载,@有效开始,@有效结束,@是否撤销,@授权人编号) RETURNING ""编号"";", args);
            return new { 编号 = id };
        }

        [HttpGet]
        public async Task<object> 查询科研文档授权(long 文档编号)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权查看文档授权" });
            return await Db.QueryAsync<object>(@"
SELECT a.""编号"",a.""文档编号"",a.""授权类型"",a.""授权对象编号"",a.""可下载"",a.""有效开始"",a.""有效结束"",a.""是否撤销"",a.""撤销时间"",a.""授权时间""
  FROM ""科研文档授权"" a WHERE a.""文档编号""=@文档编号 ORDER BY a.""是否撤销"",a.""授权时间"" DESC;", new { 文档编号 });
        }

        [HttpGet]
        public async Task<IActionResult> 下载科研文档(long 版本编号)
        {
            var row = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT v.""编号"" AS ""版本编号"",v.""文档编号"",v.""文件名称"",v.""文件路径"",d.""文档名称""
  FROM ""科研文档版本"" v JOIN ""科研文档"" d ON d.""编号""=v.""文档编号""
 WHERE v.""编号""=@版本编号 AND v.""版本状态""='生效' AND d.""是否启用""=TRUE;", new { 版本编号 });
            if (row == null) return NotFound(new { error = "文档版本不存在或未生效" });
            if (!await CanReadDocument((long)row.文档编号))
            {
                await Db.ExecuteAsync(@"INSERT INTO ""科研文档下载日志"" (""文档编号"",""版本编号"",""下载人编号"",""下载结果"",""客户端IP"",""用户代理"") VALUES (@文档编号,@版本编号,@下载人编号,'拒绝',@客户端IP,@用户代理);", new { 文档编号 = (long)row.文档编号, 版本编号, 下载人编号 = CurrentUser.编号, 客户端IP = HttpContext.Connection.RemoteIpAddress?.ToString(), 用户代理 = Request.Headers.UserAgent.ToString() });
                return StatusCode(403, new { error = "无权下载该文档" });
            }
            var webRoot = Path.GetFullPath(Env.WebRootPath);
            var configured = Convert.ToString(row.文件路径) ?? string.Empty;
            var fullPath = Path.GetFullPath(Path.IsPathRooted(configured) ? configured : Path.Combine(webRoot, configured.Replace('/', Path.DirectorySeparatorChar)));
            if (!fullPath.StartsWith(webRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && !string.Equals(fullPath, webRoot, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "文件路径不在站点存储目录内" });
            if (!System.IO.File.Exists(fullPath))
            {
                await Db.ExecuteAsync(@"INSERT INTO ""科研文档下载日志"" (""文档编号"",""版本编号"",""下载人编号"",""下载结果"",""客户端IP"",""用户代理"") VALUES (@文档编号,@版本编号,@下载人编号,'文件不存在',@客户端IP,@用户代理);", new { 文档编号 = (long)row.文档编号, 版本编号, 下载人编号 = CurrentUser.编号, 客户端IP = HttpContext.Connection.RemoteIpAddress?.ToString(), 用户代理 = Request.Headers.UserAgent.ToString() });
                return NotFound(new { error = "文件尚未上传到站点存储" });
            }
            await Db.ExecuteAsync(@"INSERT INTO ""科研文档下载日志"" (""文档编号"",""版本编号"",""下载人编号"",""下载结果"",""客户端IP"",""用户代理"") VALUES (@文档编号,@版本编号,@下载人编号,'允许',@客户端IP,@用户代理);", new { 文档编号 = (long)row.文档编号, 版本编号, 下载人编号 = CurrentUser.编号, 客户端IP = HttpContext.Connection.RemoteIpAddress?.ToString(), 用户代理 = Request.Headers.UserAgent.ToString() });
            return PhysicalFile(fullPath, "application/octet-stream", Convert.ToString(row.文件名称));
        }

        [HttpGet]
        public async Task<object> 查询科研文档下载日志(long? 文档编号 = null, int 数量 = 100)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权查看下载日志" });
            return await Db.QueryAsync<object>(@"
SELECT l.""编号"",l.""文档编号"",d.""文档名称"",l.""版本编号"",l.""下载人编号"",p.""姓名"" AS ""下载人姓名"",l.""下载结果"",l.""客户端IP"",l.""创建时间""
  FROM ""科研文档下载日志"" l JOIN ""科研文档"" d ON d.""编号""=l.""文档编号"" JOIN ""人员"" p ON p.""编号""=l.""下载人编号""
 WHERE (@文档编号 IS NULL OR l.""文档编号""=@文档编号) ORDER BY l.""创建时间"" DESC LIMIT @数量;", new { 文档编号, 数量 = Math.Clamp(数量, 1, 500) });
        }

        [HttpPost]
        public async Task<object> 撤销科研导入导出日志([FromBody] 科研导入导出撤销 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权撤销导入导出日志" });
            if (model == null || model.编号 <= 0 || string.IsNullOrWhiteSpace(model.撤销原因)) throw new ArgumentException("撤销编号和原因不能为空");
            var changed = await Db.ExecuteAsync(@"
UPDATE ""科研导入导出日志"" SET ""状态""='已撤销',""撤销时间""=CURRENT_TIMESTAMP,""撤销原因""=@撤销原因,""撤销人编号""=@撤销人编号
 WHERE ""编号""=@编号 AND ""是否可撤销""=TRUE AND ""状态""<>'已撤销';", new { model.编号, model.撤销原因, 撤销人编号 = CurrentUser.编号 });
            if (changed == 0) return BadRequest(new { error = "日志不存在、不可撤销或已经撤销" });
            return new { 编号 = model.编号, 状态 = "已撤销" };
        }

        [HttpGet]
        public async Task<object> 查询消息通道()
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权查看消息通道" });
            return await Db.QueryAsync<object>(@"
SELECT ""编号"",""通道类型"",""通道名称"",""服务地址"",""凭据引用"",""通道状态"",""是否启用"",""最后测试时间"",""最后测试状态"",""最后测试提示"",""更新时间""
  FROM ""消息通道"" ORDER BY ""是否启用"" DESC,""通道类型"",""编号"";");
        }

        [HttpPost]
        public async Task<object> 保存消息通道([FromBody] 消息通道保存 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权维护消息通道" });
            if (model == null || string.IsNullOrWhiteSpace(model.通道类型) || string.IsNullOrWhiteSpace(model.通道名称)) throw new ArgumentException("消息通道类型和名称不能为空");
            var args = new { model.编号, 通道类型 = model.通道类型.Trim(), 通道名称 = model.通道名称.Trim(), model.服务地址, model.凭据引用, 配置JSON = string.IsNullOrWhiteSpace(model.配置JSON) ? "{}" : model.配置JSON, 通道状态 = string.IsNullOrWhiteSpace(model.通道状态) ? "待配置" : model.通道状态.Trim(), model.是否启用, 创建人编号 = CurrentUser.编号 };
            if (model.是否启用 && model.通道状态 != "已验证") throw new ArgumentException("只有已验证通道才能启用");
            if (model.编号.HasValue)
            {
                var changed = await Db.ExecuteAsync(@"
UPDATE ""消息通道"" SET ""通道类型""=@通道类型,""通道名称""=@通道名称,""服务地址""=@服务地址,""凭据引用""=@凭据引用,""配置JSON""=CAST(@配置JSON AS jsonb),""通道状态""=@通道状态,""是否启用""=@是否启用,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号;", args);
                if (changed == 0) return NotFound(new { error = "消息通道不存在" });
                return new { 编号 = model.编号.Value };
            }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""消息通道"" (""通道类型"",""通道名称"",""服务地址"",""凭据引用"",""配置JSON"",""通道状态"",""是否启用"",""创建人编号"")
VALUES (@通道类型,@通道名称,@服务地址,@凭据引用,CAST(@配置JSON AS jsonb),@通道状态,@是否启用,@创建人编号) RETURNING ""编号"";", args);
            return new { 编号 = id };
        }

        [HttpGet]
        public async Task<object> 查询消息模板(bool? 仅启用 = true)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权查看消息模板" });
            return await Db.QueryAsync<object>(@"SELECT ""编号"",""模板编码"",""模板名称"",""消息类型"",""标题模板"",""正文模板"",""变量定义"",""是否启用"",""更新时间"" FROM ""消息模板"" WHERE (@仅启用 IS NULL OR ""是否启用""=@仅启用) ORDER BY ""更新时间"" DESC;", new { 仅启用 });
        }

        [HttpPost]
        public async Task<object> 保存消息模板([FromBody] 消息模板保存 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权维护消息模板" });
            if (model == null || string.IsNullOrWhiteSpace(model.模板编码) || string.IsNullOrWhiteSpace(model.模板名称) || string.IsNullOrWhiteSpace(model.标题模板) || string.IsNullOrWhiteSpace(model.正文模板)) throw new ArgumentException("消息模板字段不能为空");
            var args = new { model.编号, 模板编码 = model.模板编码.Trim(), 模板名称 = model.模板名称.Trim(), 消息类型 = string.IsNullOrWhiteSpace(model.消息类型) ? "待办提醒" : model.消息类型.Trim(), model.标题模板, model.正文模板, 变量定义 = string.IsNullOrWhiteSpace(model.变量定义) ? "{}" : model.变量定义, model.是否启用, 创建人编号 = CurrentUser.编号 };
            if (model.编号.HasValue)
            {
                var changed = await Db.ExecuteAsync(@"
UPDATE ""消息模板"" SET ""模板编码""=@模板编码,""模板名称""=@模板名称,""消息类型""=@消息类型,""标题模板""=@标题模板,""正文模板""=@正文模板,""变量定义""=CAST(@变量定义 AS jsonb),""是否启用""=@是否启用,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号;", args);
                if (changed == 0) return NotFound(new { error = "消息模板不存在" });
                return new { 编号 = model.编号.Value };
            }
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""消息模板"" (""模板编码"",""模板名称"",""消息类型"",""标题模板"",""正文模板"",""变量定义"",""是否启用"",""创建人编号"")
VALUES (@模板编码,@模板名称,@消息类型,@标题模板,@正文模板,CAST(@变量定义 AS jsonb),@是否启用,@创建人编号) RETURNING ""编号"";", args);
            return new { 编号 = id };
        }

        [HttpPost]
        public async Task<object> 加入消息发送队列([FromBody] 消息队列保存 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权加入消息发送队列" });
            if (model == null || string.IsNullOrWhiteSpace(model.消息类型) || string.IsNullOrWhiteSpace(model.消息标题)) throw new ArgumentException("消息类型和标题不能为空");
            var channel = await Db.QuerySingleOrDefaultAsync<dynamic>("SELECT \"编号\",\"通道类型\" FROM \"消息通道\" WHERE \"是否启用\"=TRUE AND \"通道状态\"='已验证' ORDER BY \"编号\" LIMIT 1;");
            long? channelId = channel == null ? null : (long?)channel.编号;
            var status = channelId.HasValue ? "待发送" : "未配置";
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""消息发送队列"" (""通道编号"",""模板编号"",""消息类型"",""业务模块"",""业务编号"",""接收人编号"",""接收地址"",""消息标题"",""消息正文"",""请求载荷"",""队列状态"",""创建人编号"")
VALUES (@通道编号,@模板编号,@消息类型,@业务模块,@业务编号,@接收人编号,@接收地址,@消息标题,@消息正文,CAST(@请求载荷 AS jsonb),@队列状态,@创建人编号) RETURNING ""编号"";", new
            {
                通道编号 = channelId, model.模板编号, model.消息类型, model.业务模块, model.业务编号, model.接收人编号, model.接收地址, model.消息标题, model.消息正文,
                请求载荷 = string.IsNullOrWhiteSpace(model.请求载荷) ? "{}" : model.请求载荷, 队列状态 = status, 创建人编号 = CurrentUser.编号
            });
            await Db.ExecuteAsync(@"INSERT INTO ""消息发送尝试"" (""队列编号"",""尝试序号"",""尝试状态"",""响应摘要"") VALUES (@队列编号,0,@尝试状态,@响应摘要);", new { 队列编号 = id, 尝试状态 = status == "未配置" ? "未配置" : "排队", 响应摘要 = status == "未配置" ? "没有启用且已验证的真实消息通道；未发送" : "已进入真实适配器队列，尚未获得外部回执" });
            return new { 编号 = id, 队列状态 = status, 真实发送 = false };
        }

        [HttpGet]
        public async Task<object> 查询消息发送队列(string 队列状态 = null, int 数量 = 100)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权查看消息队列" });
            return await Db.QueryAsync<object>(@"
SELECT q.""编号"",q.""通道编号"",c.""通道名称"",c.""通道类型"",q.""模板编号"",q.""消息类型"",q.""业务模块"",q.""业务编号"",q.""接收人编号"",p.""姓名"" AS ""接收人姓名"",q.""接收地址"",q.""消息标题"",q.""队列状态"",q.""重试次数"",q.""最大重试次数"",q.""下次重试时间"",q.""外部消息编号"",q.""真实回执时间"",q.""最后错误"",q.""创建时间"",q.""更新时间""
  FROM ""消息发送队列"" q LEFT JOIN ""消息通道"" c ON c.""编号""=q.""通道编号"" LEFT JOIN ""人员"" p ON p.""编号""=q.""接收人编号""
 WHERE (@队列状态 IS NULL OR q.""队列状态""=@队列状态) ORDER BY q.""创建时间"" DESC LIMIT @数量;", new { 队列状态 = 空值(队列状态), 数量 = Math.Clamp(数量, 1, 500) });
        }

        [HttpPost]
        public async Task<object> 重试消息发送([FromBody] 消息重试 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权重试消息" });
            if (model == null || model.队列编号 <= 0) throw new ArgumentException("队列编号无效");
            var channel = await Db.QuerySingleOrDefaultAsync<long?>("SELECT \"编号\" FROM \"消息通道\" WHERE \"是否启用\"=TRUE AND \"通道状态\"='已验证' ORDER BY \"编号\" LIMIT 1;");
            var changed = await Db.ExecuteAsync(@"
UPDATE ""消息发送队列"" SET ""通道编号""=COALESCE(""通道编号"",@通道编号),""队列状态""=CASE WHEN @通道编号 IS NULL THEN '未配置' ELSE '待发送' END,""重试次数""=""重试次数""+1,""下次重试时间""=CASE WHEN @通道编号 IS NULL THEN NULL ELSE CURRENT_TIMESTAMP END,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@队列编号 AND ""队列状态"" IN ('发送失败','未配置','待发送') AND ""重试次数""<""最大重试次数"";", new { model.队列编号, 通道编号 = channel });
            if (changed == 0) return BadRequest(new { error = "消息不存在、已达最大重试次数或当前状态不可重试" });
            return new { 队列编号 = model.队列编号, 队列状态 = channel.HasValue ? "待发送" : "未配置", 真实发送 = false };
        }

        [HttpPost]
        public async Task<object> 登记消息真实回执([FromBody] 消息真实回执 model)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权登记消息回执" });
            if (model == null || model.队列编号 <= 0 || string.IsNullOrWhiteSpace(model.外部消息编号)) throw new ArgumentException("队列编号和外部消息编号不能为空");
            var changed = await Db.ExecuteAsync(@"
UPDATE ""消息发送队列"" SET ""队列状态""='已发送',""外部消息编号""=@外部消息编号,""真实回执时间""=CURRENT_TIMESTAMP,""最后错误""=NULL,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@队列编号 AND ""队列状态"" IN ('待发送','发送中');", new { model.队列编号, model.外部消息编号 });
            if (changed == 0) return BadRequest(new { error = "队列不存在或状态不允许登记回执" });
            await Db.ExecuteAsync(@"INSERT INTO ""消息发送尝试"" (""队列编号"",""尝试序号"",""尝试状态"",""响应摘要"",""外部消息编号"") SELECT @队列编号,""重试次数"",'成功','由真实外部适配器回执确认',@外部消息编号 FROM ""消息发送队列"" WHERE ""编号""=@队列编号;", new { model.队列编号, model.外部消息编号 });
            return new { 队列编号 = model.队列编号, 队列状态 = "已发送", 真实发送 = true };
        }

        [HttpGet]
        public async Task<object> 查询消息发送尝试(long 队列编号)
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权查看消息尝试" });
            return await Db.QueryAsync<object>(@"SELECT ""编号"",""队列编号"",""尝试序号"",""尝试状态"",""响应摘要"",""外部消息编号"",""创建时间"" FROM ""消息发送尝试"" WHERE ""队列编号""=@队列编号 ORDER BY ""尝试序号"";", new { 队列编号 });
        }

        [HttpPost]
        public async Task<object> 催办科研服务申请([FromBody] 科研服务催办保存 model)
        {
            if (model == null || model.服务申请编号 <= 0) throw new ArgumentException("服务申请编号无效");
            var row = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT a.""编号"",a.""申请人编号"",a.""当前处理人编号"",a.""状态"",s.""服务名称""
  FROM ""科研服务申请"" a JOIN ""科研服务事项"" s ON s.""编号""=a.""服务事项编号"" WHERE a.""编号""=@服务申请编号;", new { model.服务申请编号 });
            if (row == null) return NotFound(new { error = "服务申请不存在" });
            var manager = await IsManager();
            if (!manager && (int)row.申请人编号 != CurrentUser.编号) return StatusCode(403, new { error = "只能催办自己的服务申请" });
            if (row.状态 is "已完成" or "已取消") return BadRequest(new { error = "终态服务申请不可催办" });
            var receiver = row.当前处理人编号 == null ? (int?)null : (int)row.当前处理人编号;
            var channel = await Db.QuerySingleOrDefaultAsync<long?>("SELECT \"编号\" FROM \"消息通道\" WHERE \"是否启用\"=TRUE AND \"通道状态\"='已验证' ORDER BY \"编号\" LIMIT 1;");
            var queueStatus = channel.HasValue ? "待发送" : "未配置";
            var queueId = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""消息发送队列"" (""通道编号"",""消息类型"",""业务模块"",""业务编号"",""接收人编号"",""消息标题"",""消息正文"",""队列状态"",""创建人编号"")
VALUES (@通道编号,'科研服务催办','科研服务',@业务编号,@接收人编号,@消息标题,@消息正文,@队列状态,@创建人编号) RETURNING ""编号"";", new { 通道编号 = channel, 业务编号 = model.服务申请编号.ToString(), 接收人编号 = receiver, 消息标题 = $"科研服务催办：{row.服务名称}", 消息正文 = model.催办原因, 队列状态 = queueStatus, 创建人编号 = CurrentUser.编号 });
            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""科研服务催办日志"" (""服务申请编号"",""催办人编号"",""接收人编号"",""催办原因"",""消息队列编号"")
VALUES (@服务申请编号,@催办人编号,@接收人编号,@催办原因,@消息队列编号) RETURNING ""编号"";", new { model.服务申请编号, 催办人编号 = CurrentUser.编号, 接收人编号 = receiver, model.催办原因, 消息队列编号 = queueId });
            return new { 编号 = id, 消息队列编号 = queueId, 队列状态 = queueStatus, 真实发送 = false };
        }

        [HttpGet]
        public async Task<object> 查询科研服务催办日志(long 服务申请编号)
        {
            var own = await Db.QuerySingleOrDefaultAsync<bool?>("SELECT \"申请人编号\"=@人员编号 OR \"当前处理人编号\"=@人员编号 FROM \"科研服务申请\" WHERE \"编号\"=@服务申请编号;", new { 服务申请编号, 人员编号 = CurrentUser.编号 });
            if (own != true && !await IsManager()) return StatusCode(403, new { error = "无权查看服务催办日志" });
            return await Db.QueryAsync<object>(@"
SELECT b.""编号"",b.""服务申请编号"",b.""催办人编号"",u.""姓名"" AS ""催办人姓名"",b.""接收人编号"",r.""姓名"" AS ""接收人姓名"",b.""催办原因"",b.""消息队列编号"",q.""队列状态"",b.""创建时间""
  FROM ""科研服务催办日志"" b LEFT JOIN ""人员"" u ON u.""编号""=b.""催办人编号"" LEFT JOIN ""人员"" r ON r.""编号""=b.""接收人编号"" LEFT JOIN ""消息发送队列"" q ON q.""编号""=b.""消息队列编号""
 WHERE b.""服务申请编号""=@服务申请编号 ORDER BY b.""创建时间"" DESC;", new { 服务申请编号 });
        }

        private static string 空值(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed class 科研文档保存 { public long? 编号 { get; set; } public string 文档编码 { get; set; } public string 文档名称 { get; set; } public string 文档分类 { get; set; } public string 文档说明 { get; set; } public string 授权模式 { get; set; } = "按授权"; public bool 是否启用 { get; set; } = true; }
    public sealed class 科研文档版本保存 { public long? 编号 { get; set; } public long 文档编号 { get; set; } public string 版本号 { get; set; } public string 文件名称 { get; set; } public string 文件路径 { get; set; } public long? 文件大小 { get; set; } public string SHA256 { get; set; } public string 版本状态 { get; set; } = "草稿"; public DateTime? 发布日期 { get; set; } }
    public sealed class 科研文档授权保存 { public long? 编号 { get; set; } public long 文档编号 { get; set; } public string 授权类型 { get; set; } public int 授权对象编号 { get; set; } public bool 可下载 { get; set; } = true; public DateTime? 有效开始 { get; set; } public DateTime? 有效结束 { get; set; } public bool 是否撤销 { get; set; } }
    public sealed class 科研导入导出撤销 { public long 编号 { get; set; } public string 撤销原因 { get; set; } }
    public sealed class 消息通道保存 { public long? 编号 { get; set; } public string 通道类型 { get; set; } public string 通道名称 { get; set; } public string 服务地址 { get; set; } public string 凭据引用 { get; set; } public string 配置JSON { get; set; } public string 通道状态 { get; set; } = "待配置"; public bool 是否启用 { get; set; } }
    public sealed class 消息模板保存 { public long? 编号 { get; set; } public string 模板编码 { get; set; } public string 模板名称 { get; set; } public string 消息类型 { get; set; } public string 标题模板 { get; set; } public string 正文模板 { get; set; } public string 变量定义 { get; set; } public bool 是否启用 { get; set; } = true; }
    public sealed class 消息队列保存 { public long? 模板编号 { get; set; } public string 消息类型 { get; set; } public string 业务模块 { get; set; } public string 业务编号 { get; set; } public int? 接收人编号 { get; set; } public string 接收地址 { get; set; } public string 消息标题 { get; set; } public string 消息正文 { get; set; } public string 请求载荷 { get; set; } }
    public sealed class 消息重试 { public long 队列编号 { get; set; } }
    public sealed class 消息真实回执 { public long 队列编号 { get; set; } public string 外部消息编号 { get; set; } }
    public sealed class 科研服务催办保存 { public long 服务申请编号 { get; set; } public string 催办原因 { get; set; } }
}
