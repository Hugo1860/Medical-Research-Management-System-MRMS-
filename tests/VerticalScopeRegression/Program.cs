using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Npgsql;
using ScientificResearch.Controllers;
using ScientificResearch.Infrastucture;
using ScientificResearch.Models;

var connection = Environment.GetEnvironmentVariable("VERTICAL_SCOPE_TEST_CONNECTION");
if (string.IsNullOrWhiteSpace(connection) || Environment.GetEnvironmentVariable("VERTICAL_SCOPE_TEST_ALLOW_FIXTURES") != "YES")
    throw new InvalidOperationException("Use an isolated migrated database and set VERTICAL_SCOPE_TEST_ALLOW_FIXTURES=YES.");
await using var db = new NpgsqlConnection(connection); await db.OpenAsync();
var marker=Guid.NewGuid().ToString("N")[..16]; // 64-bit fixture marker keeps the approval number within varchar(50).
var id=Random.Shared.Next(1200000000,1500000000); var manager=id; var owner=id+1; var other=id+2; var participant=id+3;
var ids = new List<int>();
using var services = new ServiceCollection().AddSingleton<IDatabaseConnectionFactory>(new FixedConnectionFactory(connection))
    .AddSingleton<IWebHostEnvironment>(new TestEnvironment())
    .AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["流程模板编号:纵向项目立项"]="410" }).Build())
    .BuildServiceProvider();
YProjectController Controller(int user) {
 var context=new DefaultHttpContext { RequestServices=services };
 context.Items[KeycloakUserMiddleware.CurrentUserItemKey]=new CurrentUser { 编号=user, DbKey="ScientificResearch_Model", 工号="scope-"+user, 部门编号=id };
 return new YProjectController { ControllerContext=new ControllerContext { HttpContext=context } };
}
static void Check(bool value,string name) { if(!value) throw new Exception("FAIL: "+name); Console.WriteLine("PASS: "+name); }
static JsonElement Data(object v) => JsonSerializer.SerializeToElement(v);
try {
 await db.ExecuteAsync("""
 INSERT INTO "部门" ("编号","上级部门编号","名称") VALUES (@id,0,@name);
 INSERT INTO "人员" ("编号","工号","密码","姓名","部门编号","是否启用") VALUES
 (@manager,@managerCode,'not-a-login','Scope Manager',@id,true),(@owner,@ownerCode,'not-a-login','Scope Owner',@id,true),
 (@other,@otherCode,'not-a-login','Scope Other',@id,true),(@participant,@participantCode,'not-a-login','Scope Participant',@id,true);
 INSERT INTO "人员角色" ("人员编号","角色编号") SELECT @manager,"编号" FROM "角色" WHERE "名称"='系统管理员' LIMIT 1;
 """,new { id,name="scope-"+marker,manager,owner,other,participant,managerCode="sm-"+marker,ownerCode="so-"+marker,otherCode="st-"+marker,participantCode="sp-"+marker });
 foreach(var user in new[]{owner,other}) {
   ids.Add(await db.QuerySingleAsync<int>("""
    INSERT INTO "纵向项目立项" ("分类","项目编号","项目中文名称","负责人编号","成果归属部门编号","立项年度","登记日期","登记人编号","批准日期","计划开始日期","计划完成日期",
      "学科门类","学科类型","研究类型","项目类型","项目级别","项目来源单位")
    VALUES (1,@code,@name,@user,@id,2026,CURRENT_DATE,@manager,'2026-01-02','2026-02-01','2028-01-31',
      'Medicine','Clinical','Applied','Research','Institutional','Synthetic UAT') RETURNING "编号";
    """,new { code=$"UAT-SCOPE-{marker}-{user}", name=$"Scope project {marker}-{user}", user,id,manager }));
 }
 async Task<JsonElement> List(int user, int index=1, int size=10, string? name=null, bool asc=true, string? ownerName=null) =>
   Data(await Controller(user).分页获取科研项目立项(new Paging { Index=index,Size=size,OrderType=asc },new 纵向项目立项Filter { Like项目中文名称=name ?? marker, Like负责人姓名=ownerName }));
 var managerAll=await List(manager); var own=await List(owner); var foreign=await List(other);
 Check(managerAll.GetProperty("Total").GetInt32()==2,"administrator sees both projects");
 Check(own.GetProperty("Total").GetInt32()==1 && own.GetProperty("List")[0].GetProperty("编号").GetInt32()==ids[0],"owner sees own project only");
 Check(foreign.GetProperty("Total").GetInt32()==1 && foreign.GetProperty("List")[0].GetProperty("编号").GetInt32()==ids[1],"other user sees only the other project");
 Check(own.GetProperty("List")[0].GetProperty("立项文号").GetString()==$"UAT-SCOPE-{marker}-{owner}","approval number uses actual business number");
 var page=await List(manager,2,1); Check(page.GetProperty("Total").GetInt32()==2 && page.GetProperty("List").GetArrayLength()==1,"total and page computed after scope filtering");
 var ownPage=await List(owner,2,1); Check(ownPage.GetProperty("Total").GetInt32()==1 && ownPage.GetProperty("List").GetArrayLength()==0,"other project never leaks onto later pages");
 var search=await List(manager,name:$"{marker}-{owner}"); Check(search.GetProperty("Total").GetInt32()==1,"name filter applies before pagination");
 var ownerSearch=await List(manager,ownerName:"Owner"); Check(ownerSearch.GetProperty("Total").GetInt32()==1 && ownerSearch.GetProperty("List")[0].GetProperty("编号").GetInt32()==ids[0],"owner name filter applies before pagination");
 var hiddenOwner=await List(owner,ownerName:"Other"); Check(hiddenOwner.GetProperty("Total").GetInt32()==0 && hiddenOwner.GetProperty("List").GetArrayLength()==0,"search cannot reveal another owner's project");
 var searchPage=await List(manager,index:2,size:1,ownerName:"Owner"); Check(searchPage.GetProperty("Total").GetInt32()==1 && searchPage.GetProperty("List").GetArrayLength()==0,"owner filter total and paging remain consistent");
 await db.ExecuteAsync("INSERT INTO \"项目参与人\" (\"编号\",\"流程模板编号\",\"项目编号\",\"参与人编号\") VALUES (@rowId,410,@project,@participant)",new {rowId=id+4,project=ids[0],participant});
 Check((await List(participant)).GetProperty("Total").GetInt32()==1,"authorized participant can list project");
 Console.WriteLine("Vertical project scope regression passed: controller, actual migrated PostgreSQL, synthetic identity.");
} finally {
 await db.ExecuteAsync("DELETE FROM \"项目参与人\" WHERE \"流程模板编号\"=410 AND \"项目编号\"=ANY(@ids); DELETE FROM \"纵向项目立项\" WHERE \"编号\"=ANY(@ids); DELETE FROM \"人员角色\" WHERE \"人员编号\"=ANY(@people); DELETE FROM \"人员\" WHERE \"编号\"=ANY(@people); DELETE FROM \"部门\" WHERE \"编号\"=@id;",new {ids=ids.ToArray(), people=new[]{manager,owner,other,participant}, id});
}
sealed class FixedConnectionFactory(string connectionString) : IDatabaseConnectionFactory { public string Create(string databaseKey,string environmentName)=>connectionString; }
sealed class TestEnvironment : IWebHostEnvironment {
 public string ApplicationName {get;set;}="VerticalScopeRegression"; public string EnvironmentName {get;set;}="Production"; public string ContentRootPath {get;set;}="/tmp"; public IFileProvider ContentRootFileProvider {get;set;}=new NullFileProvider(); public string WebRootPath {get;set;}="/tmp"; public IFileProvider WebRootFileProvider {get;set;}=new NullFileProvider();
}
