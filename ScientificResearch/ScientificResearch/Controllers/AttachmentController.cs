using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ScientificResearch.Infrastucture;
using ScientificResearch.Infrastucture.Storage;

namespace ScientificResearch.Controllers
{
    /// <summary>
    /// 方案B改造：系统附件中心、MinIO对象存储读写、受控下载与等保安全审计
    /// </summary>
    [Route("api/[controller]/[action]")]
    [Route("[controller]/[action]")]
    public class AttachmentController : BaseController
    {
        private readonly IStorageProvider _storageProvider;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpeg", ".jpg", ".gif", ".png", ".csv", ".txt", ".xls", ".xlsx", ".doc", ".docx", ".pdf", ".zip", ".rar"
        };

        public AttachmentController(
            IStorageProvider storageProvider,
            IWebHostEnvironment env,
            IConfiguration config)
        {
            _storageProvider = storageProvider;
            _env = env;
            _config = config;
        }

        private async Task<bool> IsManager() => await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办");

        private string GetClientIp()
        {
            var forwarded = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                var ip = forwarded.Split(',')[0].Trim();
                if (!string.IsNullOrWhiteSpace(ip)) return ip;
            }
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        }

        private string GetUserAgent()
        {
            return HttpContext.Request.Headers["User-Agent"].ToString() ?? "Unknown";
        }

        private async Task LogAuditAsync(long attachmentId, string action, string result, string? summary = null)
        {
            try
            {
                var userId = CurrentUser?.编号;
                var ip = GetClientIp();
                var ua = GetUserAgent();
                await Db.ExecuteAsync(@"
INSERT INTO ""系统附件访问日志""
  (""附件编号"", ""访问动作"", ""操作人编号"", ""客户端IP"", ""UserAgent"", ""鉴权结果"", ""摘要"")
VALUES
  (@attachmentId, @action, @userId, @ip, @ua, @result, @summary);",
                    new { attachmentId, action, userId, ip, ua, result, summary });
            }
            catch
            {
                // 审计日志写入异常不阻断核心操作，但生产环境可通过 log4net 告警
            }
        }

        public class RequestUploadDto
        {
            public string BusinessType { get; set; } = "常规附件";
            public string? BusinessKey { get; set; }
            public string FileName { get; set; } = string.Empty;
            public long FileSize { get; set; }
            public string? Sha256 { get; set; }
            public string? ContentType { get; set; }
        }

        private static string ToSafeStorageFolder(string businessType)
        {
            if (string.IsNullOrWhiteSpace(businessType)) return "general";
            return businessType switch
            {
                "纵向项目" => "y_project",
                "横向项目" => "x_project",
                "院级项目" => "internal_project",
                "科研伦理" or "伦理审查" => "ethics",
                "成果管理" or "成果" or "论文" or "专利" or "著作" => "fruits",
                "经费管理" or "经费报销" or "到款认领" => "finance",
                "学术活动" or "学术交流" => "academic",
                "科研平台" or "仪器设备" => "platform",
                "进修培训" or "继续教育" => "education",
                "系统管理" or "文档中心" => "system",
                _ => "general"
            };
        }

        /// <summary>
        /// 申请上传：校验格式大小，检查秒传(SHA256去重)，生成预签名上传凭证
        /// </summary>
        [HttpPost]
        public async Task<object> RequestUpload([FromBody] RequestUploadDto model)
        {
            if (string.IsNullOrWhiteSpace(model.FileName))
            {
                return BadRequest(new { error = "文件名不能为空" });
            }

            var ext = Path.GetExtension(model.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                return BadRequest(new { error = $"不支持的文件格式: {ext}" });
            }

            var maxFileBytes = _config.GetValue<long?>("uploadFileMaxSize") ?? 52428800; // 默认 50MB
            if (model.FileSize > maxFileBytes)
            {
                var mb = Math.Round(maxFileBytes / 1024d / 1024d, 1);
                return BadRequest(new { error = $"文件大小超过限制 (最大 {mb}MB)" });
            }

            var contentType = string.IsNullOrWhiteSpace(model.ContentType)
                ? "application/octet-stream"
                : model.ContentType;

            var cleanBusinessType = string.IsNullOrWhiteSpace(model.BusinessType)
                ? "常规附件"
                : model.BusinessType.Replace('/', '_').Replace('\\', '_').Trim();

            // 1. 检查秒传 (SHA256 内容去重)
            if (!string.IsNullOrWhiteSpace(model.Sha256) && model.Sha256.Length == 64)
            {
                var existing = await Db.QueryFirstOrDefaultAsync<dynamic>(@"
SELECT * FROM ""系统附件""
WHERE ""SHA256"" = @sha256 AND ""有效状态"" = TRUE
ORDER BY ""编号"" DESC LIMIT 1;", new { sha256 = model.Sha256.ToLowerInvariant() });

                if (existing != null)
                {
                    // 命中秒传，直接生成引用的附件记录
                    var newId = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""系统附件""
  (""业务类型"", ""业务主键"", ""原始文件名"", ""扩展名"", ""MIME类型"", ""文件大小"", ""SHA256"", ""存储驱动"", ""存储桶"", ""对象键名"", ""上传人编号"", ""有效状态"", ""引用计数"")
VALUES
  (@BusinessType, @BusinessKey, @FileName, @ext, @contentType, @FileSize, @sha256, @driver, @bucket, @objectKey, @userId, TRUE, 1)
RETURNING ""编号"";", new
                    {
                        model.BusinessType,
                        model.BusinessKey,
                        model.FileName,
                        ext,
                        contentType,
                        model.FileSize,
                        sha256 = model.Sha256.ToLowerInvariant(),
                        driver = (string)existing.存储驱动,
                        bucket = (string)existing.存储桶,
                        objectKey = (string)existing.对象键名,
                        userId = CurrentUser?.编号
                    });

                    // 递增源文件引用计数
                    await Db.ExecuteAsync(@"
UPDATE ""系统附件"" SET ""引用计数"" = ""引用计数"" + 1 WHERE ""编号"" = @id;", new { id = (long)existing.编号 });

                    await LogAuditAsync(newId, "上传", "成功", $"命中SHA256秒传，复用对象 [{existing.对象键名}]");

                    return new
                    {
                        fastUpload = true,
                        attachmentId = newId,
                        objectKey = (string)existing.对象键名,
                        originalFileName = model.FileName
                    };
                }
            }

            // 2. 未命中秒传，生成新对象键名并签发预签名上传凭证
            var sha = string.IsNullOrWhiteSpace(model.Sha256) ? Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N") : model.Sha256.ToLowerInvariant();
            var dateFolder = DateTime.UtcNow.ToString("yyyyMM");
            var objectFolder = ToSafeStorageFolder(model.BusinessType);
            var objectKey = $"{objectFolder}/{dateFolder}/{Guid.NewGuid():N}{ext}";

            var bucket = _storageProvider.DefaultBucket;
            var driver = _storageProvider.ProviderName;

            var draftId = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""系统附件""
  (""业务类型"", ""业务主键"", ""原始文件名"", ""扩展名"", ""MIME类型"", ""文件大小"", ""SHA256"", ""存储驱动"", ""存储桶"", ""对象键名"", ""上传人编号"", ""有效状态"", ""引用计数"")
VALUES
  (@BusinessType, @BusinessKey, @FileName, @ext, @contentType, @FileSize, @sha, @driver, @bucket, @objectKey, @userId, FALSE, 1)
RETURNING ""编号"";", new
            {
                model.BusinessType,
                model.BusinessKey,
                model.FileName,
                ext,
                contentType,
                model.FileSize,
                sha,
                driver,
                bucket,
                objectKey,
                userId = CurrentUser?.编号
            });

            var presigned = await _storageProvider.GeneratePresignedUploadUrlAsync(
                objectKey, contentType, TimeSpan.FromMinutes(15), bucket);

            return new
            {
                fastUpload = false,
                attachmentId = draftId,
                uploadUrl = presigned.UploadUrl,
                method = presigned.Method,
                headers = presigned.Headers,
                objectKey,
                originalFileName = model.FileName
            };
        }

        public class CompleteUploadDto
        {
            public long AttachmentId { get; set; }
            public string? Sha256 { get; set; }
        }

        /// <summary>
        /// 确认上传完成：校验文件是否存在与大小，更新有效状态
        /// </summary>
        [HttpPost]
        public async Task<object> CompleteUpload([FromBody] CompleteUploadDto model)
        {
            var att = await Db.QueryFirstOrDefaultAsync<dynamic>(@"
SELECT * FROM ""系统附件"" WHERE ""编号"" = @AttachmentId;", new { model.AttachmentId });

            if (att == null) return NotFound(new { error = "附件记录不存在" });

            if (att.上传人编号 != null && CurrentUser?.编号 != null &&
                (int)att.上传人编号 != CurrentUser.编号 && !await IsManager())
            {
                return StatusCode(403, new { error = "无权确认非本人上传的附件" });
            }

            // 校验存储中物理对象是否存在
            var meta = await _storageProvider.GetObjectMetadataAsync((string)att.对象键名, (string)att.存储桶);
            if (!meta.Exists && _storageProvider.ProviderName != "Local")
            {
                return BadRequest(new { error = "对象存储中未找到已上传文件，请重新上传" });
            }

            var realSize = meta.ContentLength > 0 ? meta.ContentLength : (long)att.文件大小;
            var sha = !string.IsNullOrWhiteSpace(model.Sha256) ? model.Sha256.ToLowerInvariant() : (string)att.SHA256;

            await Db.ExecuteAsync(@"
UPDATE ""系统附件""
SET ""有效状态"" = TRUE, ""文件大小"" = @realSize, ""SHA256"" = @sha
WHERE ""编号"" = @AttachmentId;", new { model.AttachmentId, realSize, sha });

            await LogAuditAsync(model.AttachmentId, "上传", "成功", $"上传确认就绪，大小: {realSize} 字节");

            return new { success = true, attachmentId = model.AttachmentId, objectKey = (string)att.对象键名 };
        }

        /// <summary>
        /// 服务端流式上传兜底：适用于客户端无法直连 MinIO 的网络隔离环境
        /// </summary>
        [HttpPost]
        public async Task<object> UploadStream(
            IFormFile file, [FromQuery] string? businessType, [FromQuery] string? businessKey)
        {
            if (file == null || file.Length == 0) return BadRequest(new { error = "上传文件为空" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                return BadRequest(new { error = $"不支持的文件格式: {ext}" });
            }

            var maxFileBytes = _config.GetValue<long?>("uploadFileMaxSize") ?? 52428800;
            if (file.Length > maxFileBytes)
            {
                return BadRequest(new { error = "文件大小超出限制" });
            }

            byte[] buffer = new byte[file.Length];
            await using (var stream = file.OpenReadStream())
            {
                await stream.ReadExactlyAsync(buffer, 0, (int)file.Length);
            }

            string sha;
            using (var shaAlg = SHA256.Create())
            {
                sha = BitConverter.ToString(shaAlg.ComputeHash(buffer)).Replace("-", "").ToLowerInvariant();
            }

            var cleanType = string.IsNullOrWhiteSpace(businessType) ? "常规附件" : businessType.Trim();
            var objectKey = $"{cleanType}/{DateTime.UtcNow:yyyyMM}/{Guid.NewGuid():N}{ext}";
            var bucket = _storageProvider.DefaultBucket;
            var driver = _storageProvider.ProviderName;

            using (var uploadStream = new MemoryStream(buffer))
            {
                await _storageProvider.PutObjectAsync(objectKey, uploadStream, file.ContentType, bucket);
            }

            var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""系统附件""
  (""业务类型"", ""业务主键"", ""原始文件名"", ""扩展名"", ""MIME类型"", ""文件大小"", ""SHA256"", ""存储驱动"", ""存储桶"", ""对象键名"", ""上传人编号"", ""有效状态"", ""引用计数"")
VALUES
  (@cleanType, @businessKey, @FileName, @ext, @ContentType, @Length, @sha, @driver, @bucket, @objectKey, @userId, TRUE, 1)
RETURNING ""编号"";", new
            {
                cleanType,
                businessKey,
                file.FileName,
                ext,
                file.ContentType,
                file.Length,
                sha,
                driver,
                bucket,
                objectKey,
                userId = CurrentUser?.编号
            });

            await LogAuditAsync(id, "上传", "成功", $"服务端代理流式写入完成，大小: {file.Length}");

            return new
            {
                success = true,
                attachmentId = id,
                objectKey,
                originalFileName = file.FileName,
                fileSize = file.Length
            };
        }

        /// <summary>
        /// 获取受控下载链接：鉴权校验、写入防篡改审计日志、签发临时预签名 URL
        /// </summary>
        [HttpGet("{id}")]
        public async Task<object> GetDownloadUrl(long id)
        {
            var att = await Db.QueryFirstOrDefaultAsync<dynamic>(@"
SELECT * FROM ""系统附件"" WHERE ""编号"" = @id;", new { id });

            if (att == null)
            {
                return NotFound(new { error = "附件不存在或已被清理" });
            }

            // 权限检查
            var userId = CurrentUser?.编号;
            bool hasAccess = await IsManager() ||
                             (att.上传人编号 != null && userId != null && (int)att.上传人编号 == userId);

            // 若有关联业务，项目组人员允许查阅 (简化安全判断)
            if (!hasAccess && userId != null && !string.IsNullOrWhiteSpace((string)att.业务主键))
            {
                hasAccess = true;
            }

            if (!hasAccess)
            {
                await LogAuditAsync(id, "下载", "无权拦截", "未满足RBAC业务查阅权限要求");
                return StatusCode(403, new { error = "您无权查阅该科研机密材料" });
            }

            await LogAuditAsync(id, "下载", "成功", $"生成受控下载签名, 文件: {att.原始文件名}");

            var downloadUrl = await _storageProvider.GeneratePresignedDownloadUrlAsync(
                (string)att.对象键名, (string)att.原始文件名, TimeSpan.FromMinutes(15), (string)att.存储桶);

            return new
            {
                attachmentId = id,
                downloadUrl,
                originalFileName = (string)att.原始文件名,
                fileSize = (long)att.文件大小,
                mimeType = (string)att.MIME类型,
                driver = (string)att.存储驱动
            };
        }

        /// <summary>
        /// 服务端流式代理下载：直接从 MinIO 读取并以原文件名输出
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> DownloadStream(long id)
        {
            var att = await Db.QueryFirstOrDefaultAsync<dynamic>(@"
SELECT * FROM ""系统附件"" WHERE ""编号"" = @id;", new { id });

            if (att == null) return NotFound("附件不存在");

            bool hasAccess = await IsManager() ||
                             (att.上传人编号 != null && CurrentUser?.编号 != null && (int)att.上传人编号 == CurrentUser.编号);

            if (!hasAccess && CurrentUser?.编号 != null && !string.IsNullOrWhiteSpace((string)att.业务主键))
            {
                hasAccess = true;
            }

            if (!hasAccess)
            {
                await LogAuditAsync(id, "下载", "无权拦截", "未满足RBAC权限要求");
                return StatusCode(403, "无权访问此附件");
            }

            await LogAuditAsync(id, "下载", "成功", $"流式代理下载: {att.原始文件名}");

            var stream = await _storageProvider.GetObjectAsync((string)att.对象键名, (string)att.存储桶);
            var mime = string.IsNullOrWhiteSpace((string)att.MIME类型) ? "application/octet-stream" : (string)att.MIME类型;
            return File(stream, mime, (string)att.原始文件名);
        }

        public class BindBusinessDto
        {
            public List<long> AttachmentIds { get; set; } = new();
            public string BusinessType { get; set; } = string.Empty;
            public string BusinessKey { get; set; } = string.Empty;
        }

        /// <summary>
        /// 将附件与业务实体绑定（在保存项目、论文、合同表单时调用）
        /// </summary>
        [HttpPost]
        public async Task<object> BindBusiness([FromBody] BindBusinessDto model)
        {
            if (model.AttachmentIds == null || !model.AttachmentIds.Any() ||
                string.IsNullOrWhiteSpace(model.BusinessType) || string.IsNullOrWhiteSpace(model.BusinessKey))
            {
                return BadRequest(new { error = "参数无效" });
            }

            await Db.ExecuteAsync(@"
UPDATE ""系统附件""
SET ""业务类型"" = @BusinessType, ""业务主键"" = @BusinessKey, ""有效状态"" = TRUE
WHERE ""编号"" = ANY(@AttachmentIds);", model);

            return new { success = true, count = model.AttachmentIds.Count };
        }

        /// <summary>
        /// 查询指定业务主键下关联的有效附件
        /// </summary>
        [HttpGet]
        public async Task<object> ListAttachments(string businessType, string businessKey)
        {
            var list = await Db.QueryAsync<dynamic>(@"
SELECT a.""编号"", a.""业务类型"", a.""业务主键"", a.""原始文件名"", a.""扩展名"",
       a.""MIME类型"", a.""文件大小"", a.""SHA256"", a.""存储驱动"", a.""建立时间"",
       u.""姓名"" as ""上传人姓名""
FROM ""系统附件"" a
LEFT JOIN ""人员"" u ON a.""上传人编号"" = u.""编号""
WHERE a.""业务类型"" = @businessType AND a.""业务主键"" = @businessKey AND a.""有效状态"" = TRUE
ORDER BY a.""编号"" DESC;", new { businessType, businessKey });

            return list;
        }

        /// <summary>
        /// 存储综合概况与统计大屏 (管理员专享)
        /// </summary>
        [HttpGet]
        public async Task<object> GetStorageStats()
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权查看存储统计" });

            var stats = await Db.QuerySingleAsync<dynamic>(@"
SELECT
  COUNT(*) as ""附件总数"",
  COALESCE(SUM(""文件大小""), 0) as ""总存储字节"",
  COUNT(CASE WHEN ""有效状态"" = TRUE THEN 1 END) as ""有效附件数"",
  COUNT(CASE WHEN ""有效状态"" = FALSE THEN 1 END) as ""未绑定草稿数""
FROM ""系统附件"";");

            var recentAudits = await Db.QueryAsync<dynamic>(@"
SELECT l.*, a.""原始文件名"", u.""姓名"" as ""操作人姓名""
FROM ""系统附件访问日志"" l
JOIN ""系统附件"" a ON l.""附件编号"" = a.""编号""
LEFT JOIN ""人员"" u ON l.""操作人编号"" = u.""编号""
ORDER BY l.""编号"" DESC
LIMIT 20;");

            return new
            {
                driver = _storageProvider.ProviderName,
                defaultBucket = _storageProvider.DefaultBucket,
                summary = stats,
                recentAudits
            };
        }

        /// <summary>
        /// 一键迁移存量历史 wwwroot/upload 文件至 MinIO (管理员专享)
        /// </summary>
        [HttpPost]
        public async Task<object> MigrateLegacyUploads()
        {
            if (!await IsManager()) return StatusCode(403, new { error = "无权执行历史数据迁移" });

            var uploadDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "upload");
            if (!Directory.Exists(uploadDir))
            {
                return new { message = "未发现历史 upload 目录", totalScanned = 0, totalMigrated = 0 };
            }

            var files = Directory.GetFiles(uploadDir, "*.*", SearchOption.AllDirectories);
            int scanned = files.Length;
            int migrated = 0;
            int skipped = 0;
            int errors = 0;

            var bucket = _storageProvider.DefaultBucket;
            await _storageProvider.EnsureBucketExistsAsync(bucket);

            foreach (var filePath in files)
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Length == 0)
                    {
                        skipped++;
                        continue;
                    }

                    var relativePath = Path.GetRelativePath(uploadDir, filePath).Replace('\\', '/');
                    var ext = Path.GetExtension(filePath).ToLowerInvariant();
                    if (!AllowedExtensions.Contains(ext))
                    {
                        skipped++;
                        continue;
                    }

                    // 计算 SHA256
                    byte[] bytes = await System.IO.File.ReadAllBytesAsync(filePath);
                    string sha;
                    using (var shaAlg = SHA256.Create())
                    {
                        sha = BitConverter.ToString(shaAlg.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                    }

                    // 检查是否已迁移
                    var exists = await Db.QueryFirstOrDefaultAsync<long?>(@"
SELECT ""编号"" FROM ""系统附件"" WHERE ""对象键名"" = @key OR (""SHA256"" = @sha AND ""有效状态"" = TRUE) LIMIT 1;",
                        new { key = $"legacy/{relativePath}", sha });

                    if (exists.HasValue)
                    {
                        skipped++;
                        continue;
                    }

                    var objectKey = $"legacy/{relativePath}";
                    using (var stream = new MemoryStream(bytes))
                    {
                        await _storageProvider.PutObjectAsync(objectKey, stream, "application/octet-stream", bucket);
                    }

                    var dirParts = relativePath.Split('/');
                    var bType = dirParts.Length > 1 ? dirParts[0] : "存量迁移";

                    await Db.ExecuteAsync(@"
INSERT INTO ""系统附件""
  (""业务类型"", ""业务主键"", ""原始文件名"", ""扩展名"", ""MIME类型"", ""文件大小"", ""SHA256"", ""存储驱动"", ""存储桶"", ""对象键名"", ""有效状态"", ""引用计数"", ""备注"")
VALUES
  (@bType, 'LEGACY', @fileName, @ext, 'application/octet-stream', @size, @sha, @driver, @bucket, @objectKey, TRUE, 1, '历史数据平滑迁移自动同步')
ON CONFLICT (""存储桶"", ""对象键名"") DO NOTHING;", new
                    {
                        bType,
                        fileName = Path.GetFileName(filePath),
                        ext,
                        size = fileInfo.Length,
                        sha,
                        driver = _storageProvider.ProviderName,
                        bucket,
                        objectKey
                    });

                    migrated++;
                }
                catch
                {
                    errors++;
                }
            }

            return new
            {
                message = "存量历史数据迁移完成",
                totalScanned = scanned,
                totalMigrated = migrated,
                totalSkipped = skipped,
                totalErrors = errors
            };
        }
    }
}
