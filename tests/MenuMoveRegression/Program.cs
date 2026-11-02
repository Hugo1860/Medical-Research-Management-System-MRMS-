using System.Reflection;
using Dapper;
using Npgsql;
using ScientificResearch.Controllers;
using ScientificResearch.Models;

var source = Environment.GetEnvironmentVariable("MENU_MOVE_TEST_CONNECTION");
if (string.IsNullOrWhiteSpace(source) || Environment.GetEnvironmentVariable("MENU_MOVE_TEST_ALLOW_FIXTURES") != "YES")
    throw new InvalidOperationException("Use a disposable PostgreSQL database and enable the fixture switch.");
var migrationPath = Environment.GetEnvironmentVariable("MENU_MOVE_TEST_MIGRATION") ?? "database/postgresql/migrations/120_science_office_menu_relocation.sql";
var migration = await File.ReadAllTextAsync(migrationPath);
var schema = "menu_move_" + Guid.NewGuid().ToString("N");
await using var admin = new NpgsqlConnection(source); await admin.OpenAsync();
await admin.ExecuteAsync($"CREATE SCHEMA \"{schema}\";");
try
{
    var scoped = new NpgsqlConnectionStringBuilder(source) { SearchPath = schema, Pooling = false }.ConnectionString;
    await using var db = new NpgsqlConnection(scoped); await db.OpenAsync();
    await db.ExecuteAsync("""
        CREATE TABLE "权限" ("编号" integer primary key, "上级编号" integer not null, "名称" text not null, "路径" text, "类型" boolean not null, "图标" text, "备注" text);
        CREATE TABLE "角色权限" ("角色编号" integer, "权限编号" integer);
        CREATE TABLE "系统迁移版本" ("编号" integer primary key, "版本" integer, "脚本" text, "检查摘要" text, "应用时间" timestamp);
        INSERT INTO "系统迁移版本" ("编号","版本") VALUES (1,119);
        INSERT INTO "权限" VALUES
          (1,0,'科研办公',NULL,true,NULL,NULL),(2,0,'纵向项目',NULL,true,NULL,NULL),
          (207,2,'教育部科研统计','科研办公/科研办公/教育部科研统计.html',true,NULL,NULL),
          (209,2,'首页组件与业务催办','科研办公/科研办公/首页组件与业务催办.html',true,NULL,NULL),
          (210,2,'科研文档共享与消息中心','科研办公/科研办公/科研文档共享与消息中心.html',true,NULL,NULL),
          (211,2,'另一个功能','科研办公/科研办公/别的.html',true,NULL,NULL);
        INSERT INTO "角色权限" VALUES (10,207),(10,209),(11,210),(12,211),(13,1),(13,210);
        """);
    var roleBefore = (await db.QueryAsync<string>("SELECT concat(\"角色编号\",':',\"权限编号\") FROM \"角色权限\" ORDER BY 1")).ToArray();
    await db.ExecuteAsync(migration); await db.ExecuteAsync(migration);
    Check(await db.QuerySingleAsync<int>("SELECT count(*)::int FROM \"权限\" WHERE \"编号\" IN (207,209,210) AND \"上级编号\"=1") == 3, "real migration places all three under office; replay is idempotent");
    Check(await db.QuerySingleAsync<int>("SELECT \"上级编号\" FROM \"权限\" WHERE \"编号\"=211") == 2, "unrelated menu stays unchanged");
    var roleAfter = (await db.QueryAsync<string>("SELECT concat(\"角色编号\",':',\"权限编号\") FROM \"角色权限\" ORDER BY 1")).ToArray();
    Check(roleBefore.SequenceEqual(roleAfter), "migration leaves all role grants unchanged");
    Check(await db.QuerySingleAsync<int>("SELECT \"版本\" FROM \"系统迁移版本\" WHERE \"编号\"=1") == 120, "schema version recorded");
    await db.ExecuteAsync("UPDATE \"系统迁移版本\" SET \"版本\"=121 WHERE \"编号\"=1");
    await db.ExecuteAsync(migration);
    Check(await db.QuerySingleAsync<int>("SELECT \"版本\" FROM \"系统迁移版本\" WHERE \"编号\"=1") == 121, "replay does not downgrade a newer schema version");
    var helper = typeof(AccessController).GetMethod("EnsureOfficeMenuParentAsync", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new Exception("Menu parent helper missing");
    async Task<List<权限>> Display(int role)
    {
        var granted = await db.QueryAsync<权限>("SELECT p.* FROM \"权限\" p JOIN \"角色权限\" r ON r.\"权限编号\"=p.\"编号\" WHERE r.\"角色编号\"=@role", new { role });
        var task = (Task<List<权限>>)helper.Invoke(null, new object[] { db, granted })!;
        return await task;
    }
    var onlyLeaves = await Display(10); var oneLeaf = await Display(11); var unrelated = await Display(12); var parentGranted = await Display(13);
    Check(onlyLeaves.Select(x => x.编号).Order().SequenceEqual(new[]{1,207,209}), "child-only role receives display parent and only its granted leaves");
    Check(oneLeaf.Select(x => x.编号).Order().SequenceEqual(new[]{1,210}), "separate child-only role cannot see sibling entries");
    Check(unrelated.Select(x => x.编号).SequenceEqual(new[]{211}), "unrelated role receives no office parent or children");
    Check(parentGranted.Count(x=>x.编号==1)==1 && parentGranted.Count==2, "already granted office parent is not duplicated");
    await db.ExecuteAsync("UPDATE \"权限\" SET \"名称\"='不匹配' WHERE \"编号\"=210; UPDATE \"权限\" SET \"上级编号\"=2 WHERE \"编号\"=207;");
    try { await db.ExecuteAsync(migration); throw new Exception("mismatched menu was accepted"); }
    catch (PostgresException e) when (e.SqlState == "P0001") { await db.ExecuteAsync("ROLLBACK"); }
    Check(await db.QuerySingleAsync<int>("SELECT \"上级编号\" FROM \"权限\" WHERE \"编号\"=207") == 2, "identity mismatch aborts before any menu mutation");
    Console.WriteLine("PASS menu relocation: production migration and display helper, isolated synthetic PostgreSQL schema");
}
finally { await admin.ExecuteAsync($"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE;"); }
static void Check(bool value, string name) { if(!value) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); }
