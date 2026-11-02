using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Http;
using Npgsql;
using ScientificResearch.Controllers;
using ScientificResearch.Infrastucture;

var pdf = MakePdf("Readable proposal text 2026");
Check(ProposalPdfParser.ExtractText(pdf).Contains("Readable proposal text 2026"), "PDF extraction reads a compressed content stream");
Check(!Encoding.UTF8.GetString(pdf).Contains("Readable proposal text 2026"), "PDF fixture cannot pass through raw UTF8 decoding");
Check(string.IsNullOrWhiteSpace(ProposalPdfParser.ExtractText(MakePdf(""))), "image/empty text layer does not fabricate OCR output");
ExpectArgument(() => ProposalPdfParser.ExtractText(Encoding.UTF8.GetBytes("not a PDF")), "non-PDF content rejected");
ExpectArgument(() => ProposalPdfParser.ExtractText(new byte[ProposalPdfParser.MaxFileBytes + 1]), "oversized PDF rejected");
var extracted = ProposalPdfParser.ParseText(ProposalText("50 万元"));
Check(extracted.资助经费万元 == 50m && extracted.批准日期 == new DateTime(2025, 12, 10), "extracts explicit money unit and approval date");
Check(ProposalPdfParser.Validate(extracted).Count == 0, "complete text-layer fields validate");
Check(ProposalPdfParser.ParseText(ProposalText("500,000 元")).资助经费万元 == 50m, "yuan converted to ten-thousand yuan once");
Check(ProposalPdfParser.ParseText(ProposalText("500000")).资助经费万元 == null, "ambiguous missing unit remains unknown");
Check(ProposalPdfParser.ParseText(ProposalText("0 元")).资助经费万元 == 0m, "explicit zero funding retained");
var absent = ProposalPdfParser.ParseText("项目名称：Missing mandatory fields");
Check(absent.开始日期 == null && absent.结束日期 == null && absent.批准日期 == null && absent.资助经费万元 == null && absent.年度 == 0, "missing values stay missing");
Check(ProposalPdfParser.Validate(absent).ContainsKey("批准号") && ProposalPdfParser.Validate(absent).ContainsKey("项目日期"), "missing required values fail validation");
Console.WriteLine("PASS PDF parser: compressed PDF text, no fabricated OCR/defaults, unit conversion and mandatory fields");

ProposalUploadPolicy.Validate(Enumerable.Range(0, 5).Select(_ => SizedFile(20L * 1024 * 1024)).ToArray());
ProposalUploadPolicy.Validate(Enumerable.Range(0, 10).Select(_ => SizedFile(10L * 1024 * 1024)).ToArray());
ExpectArgument(() => ProposalUploadPolicy.Validate(Enumerable.Range(0, 6).Select(_ => SizedFile(18L * 1024 * 1024)).ToArray()), "six valid-sized files above the aggregate limit rejected");
ExpectArgument(() => ProposalUploadPolicy.Validate(new[] { SizedFile(20L * 1024 * 1024 + 1) }), "single file above limit rejected");
ExpectArgument(() => ProposalUploadPolicy.Validate(Enumerable.Range(0, 11).Select(_ => SizedFile(1)).ToArray()), "eleven small files rejected");
ExpectArgument(() => ProposalUploadPolicy.Validate(Array.Empty<IFormFile>()), "empty selection rejected");
ExpectArgument(() => ProposalUploadPolicy.Validate(new[] { SizedFile(0) }), "empty file rejected");
ExpectArgument(() => ProposalUploadPolicy.Validate(new[] { SizedFile(1, "wrong.txt") }), "non-PDF extension rejected");
Check(ProposalUploadPolicy.MaxRequestBytes > ProposalUploadPolicy.MaxTotalBytes, "multipart request budget leaves room above exact file limit");
Console.WriteLine("PASS upload limits: exact 100 MiB accepted, aggregate overflow, per-file size, count and extension rejected");

var database = Environment.GetEnvironmentVariable("PROPOSAL_IMPORT_TEST_CONNECTION");
if (string.IsNullOrWhiteSpace(database) || Environment.GetEnvironmentVariable("PROPOSAL_IMPORT_TEST_ALLOW_FIXTURES") != "YES")
{
    Console.Error.WriteLine("Database tests not run: set PROPOSAL_IMPORT_TEST_CONNECTION for a disposable migrated database and PROPOSAL_IMPORT_TEST_ALLOW_FIXTURES=YES.");
    return 2;
}
var schema = "proposal_import_regression_" + Guid.NewGuid().ToString("N");
var scopedConnection = new NpgsqlConnectionStringBuilder(database) { SearchPath = schema, Pooling = false }.ConnectionString;
await using var admin = new NpgsqlConnection(database);
await admin.OpenAsync();
await admin.ExecuteAsync($"CREATE SCHEMA \"{schema}\";");
try
{
    foreach (var table in new[] { "人员", "纵向项目立项", "纵向项目导入批次", "纵向项目导入明细" })
        await admin.ExecuteAsync($"CREATE TABLE \"{schema}\".\"{table}\" (LIKE public.\"{table}\" INCLUDING ALL);");
    await using var db = new NpgsqlConnection(scopedConnection);
    await db.OpenAsync();
    await db.ExecuteAsync("""
        INSERT INTO "人员" ("编号","工号","密码","姓名","部门编号","是否启用") VALUES
          (1,'IMPORT-1','not-a-login','Doctor_A',1,TRUE), (2,'IMPORT-2','not-a-login','Doctor_A',2,TRUE);
        """);
    var batch = await NewBatchAsync(db);
    var fieldsA = ValidFields("REG-A", 1);
    await ProposalImportStore.AddFilesAsync(db, batch, new[] { Upload("hash-a", fieldsA) });
    await ProposalImportStore.AddFilesAsync(db, batch, new[] { Upload("hash-a", new ExtractedProposalFields()) });
    Check(await db.QuerySingleAsync<int>("SELECT count(*) FROM \"纵向项目导入明细\";") == 1, "duplicate file is not inserted twice");
    Check(await db.QuerySingleAsync<int>("SELECT \"总文件数\" FROM \"纵向项目导入批次\" WHERE \"编号\"=@batch;", new { batch }) == 1, "batch count remains idempotent");
    await ExpectArgumentAsync(() => ProposalImportStore.ConfirmAsync(db, batch, 1), "unreviewed file blocks confirmation");
    var itemA = await DetailIdAsync(db, batch, "hash-a");
    var invalid = await ProposalImportStore.ReviewAsync(db, Review(batch, itemA, new ExtractedProposalFields()), 1);
    Check(!JsonSerializer.SerializeToElement(invalid).GetProperty("通过").GetBoolean(), "missing fields cannot become a warning/importable result");
    await ProposalImportStore.ReviewAsync(db, Review(batch, itemA, fieldsA), 1);
    await db.ExecuteAsync("UPDATE \"纵向项目导入明细\" SET \"文件存储路径\"='/legacy/wwwroot/original.pdf' WHERE \"编号\"=@itemA;", new { itemA });
    var reviewedBeforeRestore = await DetailExceptPathAsync(db, itemA);
    var restoredUpload = Upload("hash-a", new ExtractedProposalFields { 项目名称 = "overwrite attempt" });
    restoredUpload.文件存储路径 = "/protected/restored-original.pdf";
    await ProposalImportStore.AddFilesAsync(db, batch, new[] { restoredUpload });
    Check(await db.QuerySingleAsync<string>("SELECT \"解析字段\"->>'批准号' FROM \"纵向项目导入明细\" WHERE \"编号\"=@itemA;", new { itemA }) == "REG-A", "duplicate upload preserves manual review");
    Check(await db.QuerySingleAsync<string>("SELECT \"文件存储路径\" FROM \"纵向项目导入明细\" WHERE \"编号\"=@itemA;", new { itemA }) == restoredUpload.文件存储路径, "same-hash reupload repairs legacy path");
    Check(await DetailExceptPathAsync(db, itemA) == reviewedBeforeRestore, "legacy path repair preserves all reviewed metadata and statuses");
    Console.WriteLine("PASS import staging: duplicate upload/count, unreviewed blocking, validation and reviewed field preservation");

    await ProposalImportStore.AddFilesAsync(db, batch, new[] { new ParsedProposalUpload { 文件哈希 = "hash-b", 源文件名 = "scanned.pdf", 文件存储路径 = "/isolated/scanned.pdf", 解析状态 = "需人工" } });
    var itemB = await DetailIdAsync(db, batch, "hash-b");
    await ExpectArgumentAsync(() => ProposalImportStore.ConfirmAsync(db, batch, 1), "one unreviewed scan blocks entire batch");
    Check(await ProjectCountAsync(db) == 0, "no partial project imported before full review");
    var ambiguous = ValidFields("REG-B", null);
    var ambiguousResult = await ProposalImportStore.ReviewAsync(db, Review(batch, itemB, ambiguous), 1);
    Check(!JsonSerializer.SerializeToElement(ambiguousResult).GetProperty("通过").GetBoolean(), "ambiguous duplicate personnel names need an explicit identity");
    await ProposalImportStore.ReviewAsync(db, Review(batch, itemB, ValidFields("REG-B", 2)), 1);
    await db.ExecuteAsync("UPDATE \"人员\" SET \"是否启用\"=FALSE WHERE \"编号\"=2;");
    await ExpectArgumentAsync(() => ProposalImportStore.ConfirmAsync(db, batch, 1), "disabled owner rechecked at confirmation");
    Check(await ProjectCountAsync(db) == 0, "disabled owner blocks all projects atomically");
    await db.ExecuteAsync("UPDATE \"人员\" SET \"是否启用\"=TRUE WHERE \"编号\"=2;");

    await db.ExecuteAsync("""
        CREATE FUNCTION reject_project_fixture() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN IF NEW."项目编号"='REG-B' THEN RAISE EXCEPTION 'fixture insert failure' USING ERRCODE='23514'; END IF; RETURN NEW; END; $$;
        CREATE TRIGGER proposal_import_failure BEFORE INSERT ON "纵向项目立项" FOR EACH ROW EXECUTE FUNCTION reject_project_fixture();
        """);
    try { await ProposalImportStore.ConfirmAsync(db, batch, 1); throw new Exception("Expected second insert to fail"); }
    catch (PostgresException ex) when (ex.SqlState == "23514") { }
    Check(await ProjectCountAsync(db) == 0, "second insert failure rolls back first project");
    Check(await db.QuerySingleAsync<int>("SELECT count(*) FROM \"纵向项目导入明细\" WHERE \"处理状态\"='已导入';") == 0, "failed transaction rolls back detail statuses");
    await db.ExecuteAsync("DROP TRIGGER proposal_import_failure ON \"纵向项目立项\";");
    await using var concurrent = new NpgsqlConnection(scopedConnection);
    await concurrent.OpenAsync();
    await Task.WhenAll(ProposalImportStore.ConfirmAsync(db, batch, 1), ProposalImportStore.ConfirmAsync(concurrent, batch, 1));
    Check(await ProjectCountAsync(db) == 2, "concurrent confirmations import each reviewed document once");
    var imported = (await db.QueryAsync<dynamic>("SELECT * FROM \"纵向项目立项\" ORDER BY \"项目编号\";")).ToList();
    Check((int)imported[0].负责人编号 == 1 && (int)imported[1].负责人编号 == 2, "actual personnel identity is retained");
    Check((decimal)imported[0].批准经费 == 500000m, "verified money reaches actual project column in yuan");
    Check((DateOnly)imported[0].批准日期 == new DateOnly(2025,12,10) && (DateOnly)imported[0].计划开始日期 == new DateOnly(2026,1,1), "verified dates are not replaced with current/default dates");
    Check(await db.QuerySingleAsync<int>("SELECT \"导入成功数\" FROM \"纵向项目导入批次\" WHERE \"编号\"=@batch;", new { batch }) == 2, "completed batch counters match imported details");
    Console.WriteLine("PASS import commit: real owner mapping, owner recheck, rollback on partial failure, concurrent idempotence and original amounts/dates");

    var importedBeforeRestore = await DetailExceptPathAsync(db, itemA);
    var completedBatchBeforeRestore = await db.QuerySingleAsync<string>("SELECT to_jsonb(b)::text FROM \"纵向项目导入批次\" b WHERE \"编号\"=@batch;", new { batch });
    await ExpectArgumentAsync(() => ProposalImportStore.RestoreOriginalAsync(db, itemA, "different-content", "/protected/replacement.pdf"), "restoration rejects another document even after import");
    Check(await db.QuerySingleAsync<string>("SELECT \"文件存储路径\" FROM \"纵向项目导入明细\" WHERE \"编号\"=@itemA;", new { itemA }) == restoredUpload.文件存储路径, "rejected restoration leaves original path intact");
    await ProposalImportStore.RestoreOriginalAsync(db, itemA, "hash-a", "/protected/imported-original.pdf");
    Check(await db.QuerySingleAsync<string>("SELECT \"文件存储路径\" FROM \"纵向项目导入明细\" WHERE \"编号\"=@itemA;", new { itemA }) == "/protected/imported-original.pdf", "completed batch original can be restored");
    Check(await DetailExceptPathAsync(db, itemA) == importedBeforeRestore, "completed restoration preserves project identity and reviewed data");
    Check(await db.QuerySingleAsync<string>("SELECT to_jsonb(b)::text FROM \"纵向项目导入批次\" b WHERE \"编号\"=@batch;", new { batch }) == completedBatchBeforeRestore, "restoration never reopens a completed batch or changes its counters");
    Check(await ProjectCountAsync(db) == 2, "completed restoration does not import any additional project");
    Console.WriteLine("PASS original restoration: legacy reupload preserves review, hash mismatch rejected, completed batch stays completed");

    var second = await NewBatchAsync(db);
    await ProposalImportStore.AddFilesAsync(db, second, new[] { Upload("duplicate-project", ValidFields("REG-A", 1)) });
    var duplicateId = await DetailIdAsync(db, second, "duplicate-project");
    var duplicateReview = await ProposalImportStore.ReviewAsync(db, Review(second, duplicateId, ValidFields("REG-A", 1)), 1);
    Check(!JsonSerializer.SerializeToElement(duplicateReview).GetProperty("通过").GetBoolean(), "existing approval number is blocked across batches");
    await ExpectArgumentAsync(() => ProposalImportStore.ConfirmAsync(db, second, 1), "duplicate approval number cannot import");
    var brokenBatch = await NewBatchAsync(db);
    await ProposalImportStore.AddFilesAsync(db, brokenBatch, new[] { new ParsedProposalUpload { 文件哈希 = "broken", 源文件名 = "broken.pdf", 文件存储路径 = "/isolated/broken.pdf", 解析状态 = "解析失败" } });
    var brokenId = await DetailIdAsync(db, brokenBatch, "broken");
    await ExpectArgumentAsync(() => ProposalImportStore.ReviewAsync(db, Review(brokenBatch, brokenId, ValidFields("REG-C", 1)), 1), "unreadable PDF cannot be marked valid by editing fields");
    await ProposalImportStore.IgnoreAsync(db, new IgnoreProposalImportReq { 批次编号 = brokenBatch, 明细编号 = brokenId }, 1);
    Check(await db.QuerySingleAsync<string>("SELECT \"状态\" FROM \"纵向项目导入批次\" WHERE \"编号\"=@brokenBatch;", new { brokenBatch }) == "已关闭", "explicit ignore closes an all-ignored batch without creating projects");
    await ProposalImportStore.RestoreOriginalAsync(db, brokenId, "broken", "/protected/ignored-original.pdf");
    Check(await db.QuerySingleAsync<string>("SELECT \"状态\" FROM \"纵向项目导入批次\" WHERE \"编号\"=@brokenBatch;", new { brokenBatch }) == "已关闭", "restoring an ignored original never reopens a closed batch");
    Console.WriteLine("PASS duplicate approval and corrupt-document gates; ignored document never creates a project");
    Console.WriteLine("Proposal import regression passed: production parser/store, real migrated column definitions, disposable fixture schema; no hospital or supplier data used.");
    return 0;
}
finally { await admin.ExecuteAsync($"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE;"); }

static void Check(bool condition, string description) { if (!condition) throw new Exception("FAIL: " + description); }
static void ExpectArgument(Action action, string description) { try { action(); } catch (ArgumentException) { return; } throw new Exception("FAIL: " + description); }
static async Task ExpectArgumentAsync(Func<Task<object>> action, string description) { try { await action(); } catch (ArgumentException) { return; } throw new Exception("FAIL: " + description); }
static async Task<long> NewBatchAsync(NpgsqlConnection db) => await db.QuerySingleAsync<long>("INSERT INTO \"纵向项目导入批次\" (\"批次名称\",\"导入人编号\") VALUES ('isolated regression',1) RETURNING \"编号\";");
static async Task<long> DetailIdAsync(NpgsqlConnection db, long batch, string hash) => await db.QuerySingleAsync<long>("SELECT \"编号\" FROM \"纵向项目导入明细\" WHERE \"批次编号\"=@batch AND \"文件哈希\"=@hash;", new { batch, hash });
static async Task<int> ProjectCountAsync(NpgsqlConnection db) => await db.QuerySingleAsync<int>("SELECT count(*)::integer FROM \"纵向项目立项\";");
static async Task<string> DetailExceptPathAsync(NpgsqlConnection db, long id) => await db.QuerySingleAsync<string>("SELECT (to_jsonb(d)-'文件存储路径')::text FROM \"纵向项目导入明细\" d WHERE \"编号\"=@id;", new { id });
static IFormFile SizedFile(long size, string name = "original.pdf") => new FormFile(Stream.Null, 0, size, "files", name);
static ParsedProposalUpload Upload(string hash, ExtractedProposalFields fields) => new() { 文件哈希=hash, 源文件名=hash+".pdf", 文件存储路径="/isolated/"+hash+".pdf", 解析字段=fields };
static ReviewProposalImportReq Review(long batch, long detail, ExtractedProposalFields fields) => new() { 批次编号=batch, 明细编号=detail, 解析字段=fields, 已核对原件=true };
static ExtractedProposalFields ValidFields(string code, int? owner) { var fields = ProposalPdfParser.ParseText(ProposalText("50 万元")); fields.批准号=code; fields.负责人编号=owner; return fields; }
static string ProposalText(string funding) => $"""
    项目名称：Regression research project
    负责人：Doctor_A
    批准号：REG-2026
    立项年度：2026
    批准经费：{funding}
    批准日期：2025-12-10
    开始日期：2026-01-01
    结束日期：2028-12-31
    分类：科研项目
    学科门类：医学
    学科类型：临床医学
    研究类型：应用研究
    项目类型：基金项目
    项目级别：国家级
    项目来源单位：Test funding body
    """;

// A small real PDF with a Flate-compressed content stream; no sample document
// or local font is needed. Raw file decoding cannot retrieve the text.
static byte[] MakePdf(string text)
{
    var content = Encoding.ASCII.GetBytes(string.IsNullOrEmpty(text) ? "q Q" : $"BT /F1 12 Tf 50 700 Td ({text}) Tj ET");
    using var compressed = new MemoryStream();
    using (var zip = new ZLibStream(compressed, CompressionLevel.Optimal, true)) zip.Write(content);
    using var result = new MemoryStream();
    void Write(string value) => result.Write(Encoding.ASCII.GetBytes(value));
    var offsets = new List<long> { 0 };
    Write("%PDF-1.4\n");
    void Object(int id, string value) { offsets.Add(result.Position); Write($"{id} 0 obj\n{value}\nendobj\n"); }
    Object(1, "<< /Type /Catalog /Pages 2 0 R >>");
    Object(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
    Object(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>");
    offsets.Add(result.Position); Write($"4 0 obj\n<< /Length {compressed.Length} /Filter /FlateDecode >>\nstream\n");
    result.Write(compressed.ToArray()); Write("\nendstream\nendobj\n");
    Object(5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
    var xref = result.Position; Write("xref\n0 6\n0000000000 65535 f \n");
    for (var i=1; i<=5; i++) Write($"{offsets[i]:D10} 00000 n \n");
    Write($"trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
    return result.ToArray();
}
