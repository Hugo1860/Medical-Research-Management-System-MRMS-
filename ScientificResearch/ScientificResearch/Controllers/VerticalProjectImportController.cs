using System.Security.Cryptography;
using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ScientificResearch.Infrastucture;

namespace ScientificResearch.Controllers;

/// <summary>PDF text extraction followed by explicit human verification and atomic project import.</summary>
public sealed class VerticalProjectImportController : BaseController
{
    private async Task<bool> IsManager() => await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办");
    private string ImportRoot => Path.GetFullPath(Path.Combine(Env.ContentRootPath, "App_Data", "ProposalImports"));

    [HttpGet]
    public async Task<object> 查询导入批次()
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权查看导入批次" });
        return await Db.QueryAsync<object>("""
            SELECT b.*, u."姓名" AS "导入人姓名" FROM "纵向项目导入批次" b
            JOIN "人员" u ON b."导入人编号"=u."编号" ORDER BY b."创建时间" DESC LIMIT 200;
            """);
    }

    [HttpGet]
    public async Task<object> 查询导入明细(long 批次编号)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权查看导入明细" });
        return await Db.QueryAsync<object>("""
            SELECT "编号","批次编号","源文件名","解析状态","文本摘要","解析字段","校验结果","校验状态","处理状态","导入纵向项目编号"
            FROM "纵向项目导入明细" WHERE "批次编号"=@批次编号 ORDER BY "编号";
            """, new { 批次编号 });
    }

    [HttpGet]
    public async Task<object> 查询导入负责人(string 关键词)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权查询负责人" });
        if (string.IsNullOrWhiteSpace(关键词)) return Array.Empty<object>();
        return await Db.QueryAsync<object>("""
            SELECT "编号","姓名","工号","部门编号" FROM "人员"
            WHERE "是否启用"=TRUE AND ("姓名" ILIKE '%' || @关键词 || '%' OR "工号" ILIKE '%' || @关键词 || '%')
            ORDER BY "姓名","编号" LIMIT 30;
            """, new { 关键词 = 关键词.Trim() });
    }

    [HttpGet]
    public async Task<IActionResult> 下载计划书(long 明细编号)
    {
        var file = await Db.QuerySingleOrDefaultAsync<ProposalImportFile>("""
            SELECT "文件存储路径","源文件名","导入纵向项目编号" FROM "纵向项目导入明细" WHERE "编号"=@明细编号;
            """, new { 明细编号 });
        if (file == null) return NotFound(new { error = "计划书不存在" });
        if (!await IsManager() && (!file.导入纵向项目编号.HasValue ||
            !await CanAccessProjectAsync(config.GetValue<int>("流程模板编号:纵向项目立项"), file.导入纵向项目编号.Value)))
            return StatusCode(403, new { error = "无权下载计划书" });
        var path = Path.GetFullPath(file.文件存储路径);
        if (!path.StartsWith(ImportRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal) || !System.IO.File.Exists(path))
            return NotFound(new { error = "原文件不在受控目录或已缺失，请由科研管理员在导入明细中补传同一份原件" });
        return PhysicalFile(path, "application/pdf", file.源文件名);
    }

    [HttpPost]
    public async Task<object> 创建导入批次([FromBody] CreateImportBatchReq model)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权创建导入批次" });
        if (model == null || string.IsNullOrWhiteSpace(model.批次名称) || model.批次名称.Trim().Length > 200)
            return BadRequest(new { error = "批次名称必填，最多 200 字" });
        var source = model.来源类型?.Trim();
        if (source is not ("国家自然科学基金" or "省部级" or "其他"))
            return BadRequest(new { error = "请选择有效的 PDF 计划书来源" });
        var id = await Db.QuerySingleAsync<long>("""
            INSERT INTO "纵向项目导入批次" ("批次名称","来源类型","状态","导入人编号")
            VALUES (@批次名称,@来源类型,'待校验',@导入人编号) RETURNING "编号";
            """, new { 批次名称 = model.批次名称.Trim(), 来源类型 = source, 导入人编号 = CurrentUser.编号 });
        return new { 编号 = id };
    }

    [HttpPost]
    [RequestSizeLimit(ProposalUploadPolicy.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ProposalUploadPolicy.MaxRequestBytes)]
    public async Task<object> 上传计划书([FromQuery] long 批次编号, [FromForm] List<IFormFile> files)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权上传导入文件" });
        if (批次编号 <= 0) return BadRequest(new { error = "请选择导入批次" });
        try { ProposalUploadPolicy.Validate(files); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        var state = await Db.QuerySingleOrDefaultAsync<string>("SELECT \"状态\" FROM \"纵向项目导入批次\" WHERE \"编号\"=@批次编号;", new { 批次编号 });
        if (state == null) return NotFound(new { error = "导入批次不存在" });
        if (state is "已导入" or "已关闭") return BadRequest(new { error = "此批次已完成或关闭，请创建新批次" });
        var rows = new List<ParsedProposalUpload>();
        foreach (var file in files)
        {
            var bytes = new byte[(int)file.Length];
            await using (var stream = file.OpenReadStream())
                await stream.ReadExactlyAsync(bytes, HttpContext.RequestAborted);
            var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            var name = Path.GetFileName(file.FileName.Replace('\\', '/'));
            var row = new ParsedProposalUpload { 文件哈希 = hash, 源文件名 = name };
            try
            {
                var text = ProposalPdfParser.ExtractText(bytes);
                row.解析字段 = ProposalPdfParser.ParseText(text);
                row.文本摘要 = text.Length > 2000 ? text[..2000] : text;
                row.解析状态 = string.IsNullOrWhiteSpace(text) ? "需人工" : "已解析";
                if (row.解析状态 == "需人工") row.解析提示 = "未发现可提取的文字层，请核对扫描原件并人工填写；未执行 OCR";
            }
            catch (Exception ex) when (ex is not OperationCanceledException && ex is not OutOfMemoryException)
            {
                row.解析状态 = "解析失败";
                row.解析提示 = "PDF 无法解析或已加密，请上传可读取的原文件";
            }
            row.文件存储路径 = await StoreOriginalAsync(批次编号, hash, bytes);
            rows.Add(row);
        }
        await using var db = new NpgsqlConnection(DbConnectionString);
        await db.OpenAsync(HttpContext.RequestAborted);
        try { return await ProposalImportStore.AddFilesAsync(db, 批次编号, rows); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost]
    [RequestSizeLimit(ProposalPdfParser.MaxFileBytes + 2 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = ProposalPdfParser.MaxFileBytes + 2 * 1024 * 1024)]
    public async Task<object> 补传原件([FromQuery] long 明细编号, [FromForm] IFormFile file)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权补传计划书原件" });
        if (明细编号 <= 0) return BadRequest(new { error = "请选择导入明细" });
        try { ProposalUploadPolicy.Validate(new[] { file }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        var existing = await Db.QuerySingleOrDefaultAsync<ProposalImportFile>("""
            SELECT "批次编号","文件哈希" FROM "纵向项目导入明细" WHERE "编号"=@明细编号;
            """, new { 明细编号 });
        if (existing == null) return NotFound(new { error = "导入明细不存在" });
        var bytes = new byte[(int)file.Length];
        await using (var stream = file.OpenReadStream())
            await stream.ReadExactlyAsync(bytes, HttpContext.RequestAborted);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (!string.Equals(hash, existing.文件哈希, StringComparison.Ordinal))
            return BadRequest(new { error = "文件与首次上传的原件不一致，不能替换；请补传同一份原始 PDF" });
        if (bytes.Length < 5 || !bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8))
            return BadRequest(new { error = "原件不是 PDF，不能作为计划书补传" });
        var path = await StoreOriginalAsync(existing.批次编号, hash, bytes);
        await using var db = new NpgsqlConnection(DbConnectionString);
        await db.OpenAsync(HttpContext.RequestAborted);
        try { return await ProposalImportStore.RestoreOriginalAsync(db, 明细编号, hash, path); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    private async Task<string> StoreOriginalAsync(long batchId, string hash, byte[] bytes)
    {
        var folder = Path.Combine(ImportRoot, batchId.ToString());
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, hash + ".pdf");
        // A matching content hash restores the original document without
        // replacing manually reviewed fields or any existing original file.
        if (!System.IO.File.Exists(path))
        {
            var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await System.IO.File.WriteAllBytesAsync(temporaryPath, bytes, HttpContext.RequestAborted);
                try { System.IO.File.Move(temporaryPath, path); }
                catch (IOException) when (System.IO.File.Exists(path)) { }
            }
            finally { if (System.IO.File.Exists(temporaryPath)) System.IO.File.Delete(temporaryPath); }
        }
        return path;
    }

    [HttpPost]
    public async Task<object> 保存人工复核([FromBody] ReviewProposalImportReq model)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权复核计划书" });
        if (model == null || !model.已核对原件 || model.明细编号 <= 0 || model.批次编号 <= 0)
            return BadRequest(new { error = "请核对原件后确认字段" });
        await using var db = new NpgsqlConnection(DbConnectionString);
        await db.OpenAsync(HttpContext.RequestAborted);
        try { return await ProposalImportStore.ReviewAsync(db, model, CurrentUser.编号); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost]
    public async Task<object> 忽略导入明细([FromBody] IgnoreProposalImportReq model)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权忽略导入明细" });
        if (model == null || model.明细编号 <= 0 || model.批次编号 <= 0) return BadRequest(new { error = "请选择导入明细" });
        await using var db = new NpgsqlConnection(DbConnectionString);
        await db.OpenAsync(HttpContext.RequestAborted);
        try { await ProposalImportStore.IgnoreAsync(db, model, CurrentUser.编号); return new { success = true }; }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost]
    public async Task<object> 确认批量导入([FromBody] ConfirmImportReq model)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权执行立项批量导入" });
        if (model == null || model.批次编号 <= 0) return BadRequest(new { error = "请选择导入批次" });
        await using var db = new NpgsqlConnection(DbConnectionString);
        await db.OpenAsync(HttpContext.RequestAborted);
        try { return await ProposalImportStore.ConfirmAsync(db, model.批次编号, CurrentUser.编号); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }
}

public sealed class CreateImportBatchReq
{
    public string 批次名称 { get; set; }
    public string 来源类型 { get; set; } = "国家自然科学基金";
}
public sealed class ConfirmImportReq { public long 批次编号 { get; set; } }
public sealed class IgnoreProposalImportReq { public long 批次编号 { get; set; } public long 明细编号 { get; set; } }
public sealed class ReviewProposalImportReq
{
    public long 批次编号 { get; set; }
    public long 明细编号 { get; set; }
    public bool 已核对原件 { get; set; }
    public ExtractedProposalFields 解析字段 { get; set; }
}
public sealed class ExtractedProposalFields
{
    public string 项目名称 { get; set; }
    public string 负责人 { get; set; }
    public int? 负责人编号 { get; set; }
    public string 依托单位 { get; set; }
    public string 批准号 { get; set; }
    public int 年度 { get; set; }
    public decimal? 资助经费万元 { get; set; }
    public DateTime? 批准日期 { get; set; }
    public DateTime? 开始日期 { get; set; }
    public DateTime? 结束日期 { get; set; }
    public int? 分类 { get; set; }
    public string 学科门类 { get; set; }
    public string 学科类型 { get; set; }
    public string 研究类型 { get; set; }
    public string 项目类型 { get; set; }
    public string 项目级别 { get; set; }
    public string 项目来源单位 { get; set; }
}
public sealed class ParsedProposalUpload
{
    public string 源文件名 { get; set; }
    public string 文件存储路径 { get; set; }
    public string 文件哈希 { get; set; }
    public string 文本摘要 { get; set; }
    public string 解析状态 { get; set; } = "已解析";
    public string 解析提示 { get; set; }
    public ExtractedProposalFields 解析字段 { get; set; } = new();
}
public sealed class ProposalImportFile
{
    public long 批次编号 { get; set; }
    public string 文件哈希 { get; set; }
    public string 文件存储路径 { get; set; }
    public string 源文件名 { get; set; }
    public int? 导入纵向项目编号 { get; set; }
}
public sealed class ProposalImportDetail
{
    public long 编号 { get; set; }
    public string 源文件名 { get; set; }
    public string 解析状态 { get; set; }
    public string 解析字段 { get; set; }
    public string 校验状态 { get; set; }
    public string 校验结果 { get; set; }
    public string 处理状态 { get; set; }
}

/// <summary>Shared production persistence path, also exercised against an isolated migrated schema.</summary>
public static class ProposalImportStore
{
    private static async Task<string> LockBatchAsync(NpgsqlConnection db, NpgsqlTransaction tx, long batchId)
    {
        var state = await db.QuerySingleOrDefaultAsync<string>("SELECT \"状态\" FROM \"纵向项目导入批次\" WHERE \"编号\"=@batchId FOR UPDATE;", new { batchId }, tx);
        if (state == null) throw new ArgumentException("导入批次不存在");
        return state;
    }
    private static void RequireEditable(string state)
    {
        if (state is "已导入" or "已关闭") throw new ArgumentException("此批次已完成或关闭");
    }
    private static Task RecountAsync(NpgsqlConnection db, NpgsqlTransaction tx, long batchId) => db.ExecuteAsync("""
        UPDATE "纵向项目导入批次" b SET "总文件数"=s.total,"解析成功数"=s.parsed,"解析失败数"=s.failed,"导入成功数"=s.imported,
          "状态"=CASE WHEN s.total=0 THEN '待校验' WHEN s.pending=0 AND s.imported>0 THEN '已导入'
                      WHEN s.pending=0 THEN '已关闭' WHEN s.unverified=0 THEN '已校验' ELSE '待校验' END,
          "更新时间"=CURRENT_TIMESTAMP
        FROM (SELECT count(*)::integer total,count(*) FILTER (WHERE "解析状态"='已解析')::integer parsed,
          count(*) FILTER (WHERE "解析状态"='解析失败')::integer failed,count(*) FILTER (WHERE "处理状态"='已导入')::integer imported,
          count(*) FILTER (WHERE "处理状态"='待处理')::integer pending,
          count(*) FILTER (WHERE "处理状态"='待处理' AND "校验状态"<>'通过')::integer unverified
          FROM "纵向项目导入明细" WHERE "批次编号"=@batchId) s WHERE b."编号"=@batchId;
        """, new { batchId }, tx);

    private static async Task<Dictionary<string,string>> ValidateAsync(NpgsqlConnection db, NpgsqlTransaction tx, ExtractedProposalFields fields, long batchId, long? detailId = null)
    {
        var errors = ProposalPdfParser.Validate(fields);
        if (fields == null) return errors;
        fields.项目名称 = fields.项目名称?.Trim(); fields.批准号 = fields.批准号?.Trim(); fields.负责人 = fields.负责人?.Trim();
        var owners = (await db.QueryAsync<ImportOwner>("""
            SELECT "编号","姓名","部门编号" FROM "人员" WHERE "是否启用"=TRUE
              AND ((@负责人编号 IS NOT NULL AND "编号"=@负责人编号) OR (@负责人编号 IS NULL AND btrim("姓名")=@负责人))
            ORDER BY "编号" LIMIT 2 FOR SHARE;
            """, new { fields.负责人编号, fields.负责人 }, tx)).ToList();
        if (owners.Count != 1 || owners[0].姓名.Trim() != fields.负责人) errors["负责人映射"] = "请选择姓名一致且启用的真实负责人；同名人员请按工号区分";
        else fields.负责人编号 = owners[0].编号;
        if (!string.IsNullOrWhiteSpace(fields.批准号))
        {
            var duplicate = await db.QuerySingleAsync<bool>("""
                SELECT EXISTS (SELECT FROM "纵向项目立项" WHERE btrim("项目编号")=@code)
                  OR EXISTS (SELECT FROM "纵向项目导入明细" WHERE "批次编号"=@batchId AND "处理状态"<>'已忽略'
                    AND (@detailId IS NULL OR "编号"<>@detailId) AND btrim("解析字段"->>'批准号')=@code);
                """, new { code = fields.批准号, batchId, detailId }, tx);
            if (duplicate) errors["批准号查重"] = "批准号已存在于立项库或此批次其他明细中";
        }
        return errors;
    }

    public static async Task<object> AddFilesAsync(NpgsqlConnection db, long batchId, IReadOnlyList<ParsedProposalUpload> files)
    {
        await using var tx = await db.BeginTransactionAsync();
        RequireEditable(await LockBatchAsync(db, tx, batchId));
        var added = 0;
        foreach (var file in files)
        {
            // Reuploading a legacy file moves its reference into protected
            // storage, preserving every reviewed field and all counters.
            if (await db.ExecuteAsync("""
                UPDATE "纵向项目导入明细" SET "文件存储路径"=@文件存储路径
                WHERE "批次编号"=@batchId AND "文件哈希"=@hash;
                """, new { batchId, hash = file.文件哈希, file.文件存储路径 }, tx) != 0) continue;
            var errors = await ValidateAsync(db, tx, file.解析字段, batchId);
            errors["人工复核"] = "请核对原文件后保存人工复核";
            if (!string.IsNullOrWhiteSpace(file.解析提示)) errors["文件解析"] = file.解析提示;
            added += await db.ExecuteAsync("""
                INSERT INTO "纵向项目导入明细" ("批次编号","源文件名","文件存储路径","文件哈希","解析状态","文本摘要","解析字段","校验结果","校验状态","处理状态")
                VALUES (@batchId,@源文件名,@文件存储路径,@文件哈希,@解析状态,@文本摘要,@fields::jsonb,@errors::jsonb,@validation,'待处理')
                ON CONFLICT ("批次编号","文件哈希") DO NOTHING;
                """, new { batchId, file.源文件名, file.文件存储路径, file.文件哈希, file.解析状态, file.文本摘要,
                    fields = JsonSerializer.Serialize(file.解析字段), errors = JsonSerializer.Serialize(errors),
                    validation = file.解析状态 == "解析失败" || errors.Keys.Any(k => k is not ("人工复核" or "文件解析")) ? "失败" : "待校验" }, tx);
        }
        await RecountAsync(db, tx, batchId);
        await tx.CommitAsync();
        return new { 批次编号 = batchId, 新增文件数 = added, 重复文件数 = files.Count - added };
    }

    public static async Task<object> RestoreOriginalAsync(NpgsqlConnection db, long detailId, string hash, string path)
    {
        await using var tx = await db.BeginTransactionAsync();
        var batchId = await db.QuerySingleOrDefaultAsync<long?>("""
            SELECT "批次编号" FROM "纵向项目导入明细" WHERE "编号"=@detailId;
            """, new { detailId }, tx);
        if (!batchId.HasValue) throw new ArgumentException("导入明细不存在");
        // Follow the same batch-first lock order as review and confirmation.
        // Completed batches may repair their document, but never reopen.
        await LockBatchAsync(db, tx, batchId.Value);
        var changed = await db.ExecuteAsync("""
            UPDATE "纵向项目导入明细" SET "文件存储路径"=@path
            WHERE "编号"=@detailId AND "文件哈希"=@hash;
            """, new { path, detailId, hash }, tx);
        if (changed != 1) throw new ArgumentException("文件与首次上传的原件不一致，不能替换");
        await tx.CommitAsync();
        return new { 明细编号 = detailId, 原件已补传 = true };
    }

    public static async Task<object> ReviewAsync(NpgsqlConnection db, ReviewProposalImportReq request, int reviewer)
    {
        await using var tx = await db.BeginTransactionAsync();
        RequireEditable(await LockBatchAsync(db, tx, request.批次编号));
        var item = await db.QuerySingleOrDefaultAsync<ProposalImportDetail>("""
            SELECT * FROM "纵向项目导入明细" WHERE "编号"=@明细编号 AND "批次编号"=@批次编号 FOR UPDATE;
            """, request, tx);
        if (item == null || item.处理状态 != "待处理") throw new ArgumentException("明细不存在或已处理");
        if (item.解析状态 == "解析失败") throw new ArgumentException("原 PDF 无法读取，请忽略此文件后重新上传可读取的原件");
        if (!request.已核对原件) throw new ArgumentException("请先核对原件");
        var errors = await ValidateAsync(db, tx, request.解析字段, request.批次编号, request.明细编号);
        var valid = errors.Count == 0;
        errors["人工复核"] = valid ? "已确认" : "未通过";
        errors["复核人编号"] = reviewer.ToString(); errors["复核时间"] = DateTime.UtcNow.ToString("O");
        await db.ExecuteAsync("""
            UPDATE "纵向项目导入明细" SET "解析字段"=@fields::jsonb,"校验结果"=@errors::jsonb,"校验状态"=@state WHERE "编号"=@id;
            """, new { fields = JsonSerializer.Serialize(request.解析字段), errors = JsonSerializer.Serialize(errors), state = valid ? "通过" : "失败", id = request.明细编号 }, tx);
        await RecountAsync(db, tx, request.批次编号); await tx.CommitAsync();
        return new { 通过 = valid, 校验结果 = errors };
    }

    public static async Task IgnoreAsync(NpgsqlConnection db, IgnoreProposalImportReq request, int reviewer)
    {
        await using var tx = await db.BeginTransactionAsync();
        RequireEditable(await LockBatchAsync(db, tx, request.批次编号));
        var changed = await db.ExecuteAsync("""
            UPDATE "纵向项目导入明细" SET "处理状态"='已忽略',"校验结果"="校验结果" || jsonb_build_object('忽略人编号',@reviewer,'忽略时间',CURRENT_TIMESTAMP)
            WHERE "编号"=@明细编号 AND "批次编号"=@批次编号 AND "处理状态"='待处理';
            """, new { request.明细编号, request.批次编号, reviewer }, tx);
        if (changed != 1) throw new ArgumentException("明细不存在或已处理");
        await RecountAsync(db, tx, request.批次编号); await tx.CommitAsync();
    }

    public static async Task<object> ConfirmAsync(NpgsqlConnection db, long batchId, int userId)
    {
        await using var tx = await db.BeginTransactionAsync();
        var state = await LockBatchAsync(db, tx, batchId);
        if (state == "已导入") { await tx.CommitAsync(); return new { 批次编号 = batchId, 成功导入项目数 = 0, 已完成 = true }; }
        RequireEditable(state);
        // Block concurrent project writers while checking approval numbers, even
        // if the other writer is not another PDF batch.
        await db.ExecuteAsync("LOCK TABLE \"纵向项目立项\" IN SHARE ROW EXCLUSIVE MODE;", transaction: tx);
        var items = (await db.QueryAsync<ProposalImportDetail>("""
            SELECT * FROM "纵向项目导入明细" WHERE "批次编号"=@batchId AND "处理状态"='待处理' ORDER BY "编号" FOR UPDATE;
            """, new { batchId }, tx)).ToList();
        if (items.Count == 0) throw new ArgumentException("没有可导入的明细，请先上传并完成人工复核");
        var checkedItems = new List<(ProposalImportDetail item, ExtractedProposalFields fields)>();
        foreach (var item in items)
        {
            var review = JsonSerializer.Deserialize<Dictionary<string,string>>(item.校验结果);
            if (item.校验状态 != "通过" || review?.GetValueOrDefault("人工复核") != "已确认" || item.解析状态 == "解析失败")
                throw new ArgumentException($"{item.源文件名} 尚未通过人工复核，本批次未导入任何项目");
            var fields = JsonSerializer.Deserialize<ExtractedProposalFields>(item.解析字段);
            var errors = await ValidateAsync(db, tx, fields, batchId, item.编号);
            if (errors.Count > 0) throw new ArgumentException($"{item.源文件名}：{string.Join("；", errors.Values)}；本批次未导入任何项目");
            checkedItems.Add((item, fields));
        }
        foreach (var (item, fields) in checkedItems)
        {
            var projectId = await db.QuerySingleAsync<int>("""
                INSERT INTO "纵向项目立项" ("分类","项目编号","项目中文名称","负责人编号","成果归属部门编号","立项年度","登记日期","登记人编号","批准日期",
                  "批准经费","配套经费","计划开始日期","计划完成日期","学科门类","学科类型","研究类型","项目类型","项目级别","项目来源单位","立项文件路径","备注","建立时间")
                SELECT @分类,@批准号,@项目名称,@负责人编号,p."部门编号",@年度,CURRENT_DATE,@userId,@批准日期,
                  @fund,0,@开始日期,@结束日期,@学科门类,@学科类型,@研究类型,@项目类型,@项目级别,@项目来源单位,@document,@note,CURRENT_TIMESTAMP
                  FROM "人员" p WHERE p."编号"=@负责人编号 AND p."是否启用"=TRUE RETURNING "编号";
                """, new { fields.分类, fields.批准号, fields.项目名称, fields.负责人编号, fields.年度, userId,
                    fields.批准日期, fund = fields.资助经费万元.Value * 10000m, fields.开始日期, fields.结束日期, fields.学科门类, fields.学科类型,
                    fields.研究类型, fields.项目类型, fields.项目级别, fields.项目来源单位,
                    document = $"/VerticalProjectImport/下载计划书?明细编号={item.编号}", note = $"计划书人工复核导入；批次 {batchId}，明细 {item.编号}" }, tx);
            await db.ExecuteAsync("""
                UPDATE "纵向项目导入明细" SET "处理状态"='已导入',"导入纵向项目编号"=@projectId WHERE "编号"=@id;
                """, new { projectId, id = item.编号 }, tx);
        }
        await RecountAsync(db, tx, batchId); await tx.CommitAsync();
        return new { 批次编号 = batchId, 成功导入项目数 = checkedItems.Count, 已完成 = true };
    }
    private sealed class ImportOwner { public int 编号 { get; set; } public string 姓名 { get; set; } public int 部门编号 { get; set; } }
}
