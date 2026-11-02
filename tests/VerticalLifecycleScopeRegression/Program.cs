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

SqlMapper.AddTypeHandler(new NpgsqlDateTimeTypeHandler());
var connection = Environment.GetEnvironmentVariable("VERTICAL_LIFECYCLE_TEST_CONNECTION");
if (string.IsNullOrWhiteSpace(connection) || Environment.GetEnvironmentVariable("VERTICAL_LIFECYCLE_TEST_ALLOW_FIXTURES") != "YES")
    throw new InvalidOperationException("Use an isolated migrated database and set VERTICAL_LIFECYCLE_TEST_ALLOW_FIXTURES=YES.");
await using var db = new NpgsqlConnection(connection); await db.OpenAsync();
var marker=Guid.NewGuid().ToString("N")[..12];
var id=Random.Shared.Next(1200000000, 1500000000);
var manager=id+1; var owner=id+2; var other=id+3; var participant=id+4; var direct=id+5; var roleUser=id+6; var unrelatedRole=id+7;
var stageDirect=id+8; var stageRole=id+9;
var people=new[]{manager,owner,other,participant,direct,roleUser,unrelatedRole,stageDirect,stageRole};
var projects=new List<int>(); var applications=new List<int>(); var flows=new List<int>(); var steps=new List<int>(); var midterms=new List<int>(); var changes=new List<int>(); var closes=new List<int>();
var stepTemplates=new[]{id+20,id+21,id+24,id+25,id+26}; var role=id+22; var irrelevantRole=id+23; var stageSpecificRole=id+27;
var cfg=new Dictionary<string,string?> { ["流程模板编号:纵向项目申报"]="400",["流程模板编号:纵向项目立项"]="410",["流程模板编号:纵向项目中检"]="420",["流程模板编号:纵向项目变更"]="430",["流程模板编号:纵向项目结题"]="440",["流程模板编号:横向项目申请"]="450" };
using var services=new ServiceCollection().AddSingleton<IDatabaseConnectionFactory>(new FixedConnectionFactory(connection)).AddSingleton<IWebHostEnvironment>(new TestEnvironment()).AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(cfg).Build()).BuildServiceProvider();
YProjectController Controller(int user) { var ctx=new DefaultHttpContext{RequestServices=services}; ctx.Items[KeycloakUserMiddleware.CurrentUserItemKey]=new CurrentUser{编号=user,DbKey="ScientificResearch_Model",工号="fixture-"+user,部门编号=id}; return new YProjectController{ControllerContext=new ControllerContext{HttpContext=ctx}}; }
var checks=0;
void Check(bool condition,string message) { if(!condition) throw new Exception("FAIL: "+message); checks++; Console.WriteLine("PASS: "+message); }
static JsonElement Data(object value)=>JsonSerializer.SerializeToElement(value);
static int Total(JsonElement value)=>value.GetProperty("Total").GetInt32();
static bool Has(JsonElement value,string key,int number)=>value.GetProperty("List").EnumerateArray().Any(x=>x.GetProperty(key).ValueKind!=JsonValueKind.Null && x.GetProperty(key).GetInt32()==number);
static bool Forbidden(object value)=>value is ObjectResult { StatusCode: 403 };
try {
 await db.ExecuteAsync("""
 INSERT INTO "部门" ("编号","上级部门编号","名称") VALUES (@id,0,@name);
 INSERT INTO "人员" ("编号","工号","密码","姓名","部门编号","是否启用") SELECT x, 'scope-'||x, 'not-a-login', 'Synthetic '||x, @id, true FROM unnest(@people::integer[]) x;
 INSERT INTO "角色" ("编号","名称") VALUES (@role,@roleName),(@irrelevantRole,@otherRoleName),(@stageSpecificRole,@stageRoleName);
 INSERT INTO "人员角色" ("人员编号","角色编号") SELECT @manager,"编号" FROM "角色" WHERE "名称"='系统管理员' LIMIT 1;
 INSERT INTO "人员角色" ("人员编号","角色编号") VALUES (@roleUser,@role),(@stageRole,@stageSpecificRole),(@unrelatedRole,@irrelevantRole);
 INSERT INTO "步骤模板" ("编号","流程模板编号","步骤类型编号","名称","上级编号") VALUES (@s0,400,400,@name,0),(@s1,410,400,@name,0);
 INSERT INTO "步骤角色" ("步骤模板编号","角色编号") VALUES (@s0,@role),(@s1,@role);
 """,new{id,name="fixture-"+marker,people,role,irrelevantRole,stageSpecificRole,stageRoleName="scope-stage-"+marker,roleName="scope-role-"+marker,otherRoleName="scope-other-"+marker,manager,roleUser,stageRole,unrelatedRole,s0=stepTemplates[0],s1=stepTemplates[1]});
 async Task<int> Flow(int template,int project,int initiator,int status) {var f=await db.QuerySingleAsync<int>("INSERT INTO \"流程\" (\"流程模板编号\",\"项目编号\",\"发起人编号\",\"状态\",\"创建时间\") VALUES (@template,@project,@initiator,@status,now()) RETURNING \"编号\"",new {template,project,initiator,status}); flows.Add(f); return f;}
 async Task Assign(int flow,int stepTemplate) {var s=await db.QuerySingleAsync<int>("INSERT INTO \"步骤\" (\"步骤模板编号\",\"流程编号\",\"状态\",\"建立时间\") VALUES (@stepTemplate,@flow,0,now()) RETURNING \"编号\"",new{stepTemplate,flow}); steps.Add(s); await db.ExecuteAsync("INSERT INTO \"步骤指定人\" (\"步骤编号\",\"指定人编号\") VALUES (@s,@direct)",new{s,direct});}
 for(int category=1;category<=3;category++) foreach(var person in new[]{owner,other}) {
   var app=await db.QuerySingleAsync<int>("""INSERT INTO "纵向项目申报" ("分类","项目名称","申报人编号","申报年度","申请经费","计划开始日期","计划完成日期","是否立项") VALUES (@category,@name,@person,2026,100,'2026-01-01','2028-01-01',false) RETURNING "编号" """,new{category,name=$"{marker}-{category}-{person}",person}); applications.Add(app);
   var flow=await Flow(400,app,person,0); if(category==1 && person==owner) await Assign(flow,stepTemplates[0]);
 }
 for(int i=0;i<3;i++) {var person=i==1?other:owner;
   var p=await db.QuerySingleAsync<int>("""INSERT INTO "纵向项目立项" ("分类","项目编号","项目中文名称","负责人编号","成果归属部门编号","立项年度","登记日期","登记人编号","批准日期","计划开始日期","计划完成日期","学科门类","学科类型","研究类型","项目类型","项目级别","项目来源单位") VALUES (1,@code,@name,@person,@id,2026,CURRENT_DATE,@manager,'2026-01-02','2026-02-01','2028-01-31','Medicine','Clinical','Applied','Research','Institutional','Synthetic') RETURNING "编号" """,new {code=$"fixture-{marker}-{i}",name=$"{marker}-{i}",person,id,manager}); projects.Add(p);
   var flow=await Flow(410,p,person,1); if(i==0) await Assign(flow,stepTemplates[1]);
 }
 await db.ExecuteAsync("INSERT INTO \"项目参与人\" (\"流程模板编号\",\"项目编号\",\"参与人编号\") VALUES (400,@app,@participant),(410,@project,@participant)",new{app=applications[0],project=projects[0],participant});
 for(int i=0;i<2;i++) {
   midterms.Add(await db.QuerySingleAsync<int>("INSERT INTO \"纵向项目中检\" (\"纵向项目编号\",\"备注\") VALUES (@p,@marker) RETURNING \"编号\"",new{p=projects[i],marker}));
   changes.Add(await db.QuerySingleAsync<int>("""INSERT INTO "纵向项目变更" ("纵向项目编号","分类","立项年度","登记日期","登记人编号","批准日期","计划开始日期","计划完成日期","学科门类","学科类型","研究类型","项目类型","项目级别","项目来源单位","变更时间","变更事由") VALUES (@p,1,2026,CURRENT_DATE,@manager,CURRENT_DATE,'2026-01-01','2028-01-01','Medicine','Clinical','Applied','Research','Institutional','Synthetic',now(),@marker) RETURNING "编号" """,new{p=projects[i],manager,marker}));
 }
 async Task<JsonElement> App(int user,int category,bool eligible=false,int index=1,int size=10,string? name=null,int? recordId=null) {
   var c=Controller(user); var p=new Paging{Index=index,Size=size}; var f=new 纵向项目申报Filter{Like项目名称=name??marker,编号=recordId};
   return Data(await ((category,eligible) switch { (1,false)=>c.分页获取科研项目申报(p,f),(2,false)=>c.分页获取重点学科申报(p,f),(3,false)=>c.分页获取人才培养申报(p,f),(1,true)=>c.分页获取可立项的科研项目申报(p,f),(2,true)=>c.分页获取可立项的重点学科申报(p,f),_=>c.分页获取可立项的人才培养申报(p,f) }));
 }
 foreach(var category in new[]{1,2,3}) foreach(var eligible in new[]{false,true}) {
   var a=applications[(category-1)*2]; var b=applications[(category-1)*2+1]; var label=$"application category={category} eligible={eligible}";
   var m=await App(manager,category,eligible); var o=await App(owner,category,eligible); var x=await App(other,category,eligible);
   Check(Total(m)==2 && Has(m,"编号",a) && Has(m,"编号",b),label+" manager");
   Check(Total(o)==1 && Has(o,"编号",a),label+" owner");
   Check(Total(x)==1 && Has(x,"编号",b),label+" other");
   Check(Total(await App(unrelatedRole,category,eligible))==0,label+" unrelated role denied");
   Check(Total(await App(manager,category,eligible,2,1))==2 && (await App(manager,category,eligible,2,1)).GetProperty("List").GetArrayLength()==1,label+" total and page");
   Check(Total(await App(owner,category,eligible,2,1))==1 && (await App(owner,category,eligible,2,1)).GetProperty("List").GetArrayLength()==0,label+" page does not reveal other");
   Check(Total(await App(owner,category,eligible,name:$"{marker}-{category}-{other}"))==0,label+" filtered foreign not revealed");
   Check(Total(await App(manager,category,eligible,recordId:b))==1 && Total(await App(owner,category,eligible,recordId:b))==0,label+" exact foreign ID cannot reveal record");
 }
 await db.ExecuteAsync("UPDATE \"纵向项目申报\" SET \"是否立项\"=true WHERE \"编号\"=@a",new{a=applications[1]});
 Check(Total(await App(manager,1,true))==1 && Total(await App(manager,1,false))==2,"eligible list excludes approved application while ordinary list retains it");
 await db.ExecuteAsync("UPDATE \"纵向项目申报\" SET \"是否立项\"=false WHERE \"编号\"=@a",new{a=applications[1]});
 foreach(var user in new[]{participant,direct,roleUser}) foreach(var eligible in new[]{false,true}) Check(Total(await App(user,1,eligible))==1 && Has(await App(user,1,eligible),"编号",applications[0]),$"application user {user-id} eligible={eligible} sees assigned or participating project");
 var appDetailOwner = await Controller(owner).获取纵向项目申报详情(applications[0]);
 Check(!Forbidden(appDetailOwner), "application detail accessible to owner");
 var appData = Data(appDetailOwner);
 Check(appData.TryGetProperty("申报基本信息", out var basicInfo) && basicInfo.GetProperty("编号").GetInt32() == applications[0], "application detail loads v2_纵向项目申报_详情 correctly");
 Check(basicInfo.GetProperty("姓名").GetString() == $"Synthetic {owner}", "application detail contains applicant name from view");
 var appDetailManager = await Controller(manager).获取纵向项目申报详情(applications[0]);
 Check(!Forbidden(appDetailManager), "application detail accessible to manager");
 var appDetailForbidden = await Controller(other).获取纵向项目申报详情(applications[0]);
 Check(Forbidden(appDetailForbidden), "application detail forbidden to unrelated user");
 async Task<JsonElement> List(int user,string stage,int index=1,int size=10,string? name=null) {
   var c=Controller(user);var p=new Paging{Index=index,Size=size};var n=name??marker;
   return Data(await (stage switch {"mid"=>c.分页获取纵向项目中检(p,new 纵向项目中检Filter{Like项目中文名称=n}),"change"=>c.分页获取纵向项目变更(p,new 纵向项目变更Filter{Like项目中文名称=n}),_=>c.分页获取纵向项目结题(p,new 纵向项目结题Filter{Like项目中文名称=n})}));
 }
 async Task<JsonElement> ListCompat(int user,string stage,string name) {
   var c=Controller(user);var p=new Paging{Index=1,Size=10};
   return Data(await (stage switch {"mid"=>c.分页获取纵向项目中检(p,new 纵向项目中检Filter{Like项目名称=name}),"change"=>c.分页获取纵向项目变更(p,new 纵向项目变更Filter{Like项目名称=name}),_=>c.分页获取纵向项目结题(p,new 纵向项目结题Filter{Like项目名称=name})}));
 }
 async Task CheckList(string stage) {
   var m=await List(manager,stage);var o=await List(owner,stage);var x=await List(other,stage);
   Check(Total(m)==3 && projects.All(p=>Has(m,"纵向项目立项编号",p)),stage+" manager includes all parent projects");
   Check(Total(o)==2 && Has(o,"纵向项目立项编号",projects[0]) && Has(o,"纵向项目立项编号",projects[2]),stage+" owner includes uninitiated child");
   Check(o.GetProperty("List").EnumerateArray().Any(x=>x.GetProperty("纵向项目立项编号").GetInt32()==projects[2] && x.GetProperty("编号").ValueKind==JsonValueKind.Null),stage+" uninitiated child remains null");
   Check(Total(x)==1 && Has(x,"纵向项目立项编号",projects[1]),stage+" other only sees own parent");
   foreach(var user in new[]{participant,direct,roleUser}) Check(Total(await List(user,stage))==1 && Has(await List(user,stage),"纵向项目立项编号",projects[0]),$"{stage} participant or current-step user {user-id}");
   Check(Total(await List(unrelatedRole,stage))==0,stage+" unrelated role denied");
   var page=await List(owner,stage,2,1);Check(Total(page)==2 && page.GetProperty("List").GetArrayLength()==1,stage+" total and paging after permission");
   Check(Total(await List(owner,stage,3,1))==2 && (await List(owner,stage,3,1)).GetProperty("List").GetArrayLength()==0,stage+" later page cannot reveal other");
   Check(Total(await List(owner,stage,name:$"{marker}-1"))==0,stage+" search cannot reveal foreign");
   Check(Total(await List(manager,stage,name:$"{marker}-1"))==1,stage+" search retains authorized results");
   Check(Total(await ListCompat(manager,stage,$"{marker}-1"))==1,stage+" Like项目名称 compat search finds match");
   Check(Total(await ListCompat(manager,stage,$"nonexistent-{marker}"))==0,stage+" Like项目名称 compat search returns empty for non-match");
 }
 async Task<object> Detail(int user,string stage,int p,int mid,int child)=>stage switch {"mid"=>await Controller(user).获取纵向项目中检详情(p,child),"change"=>await Controller(user).获取纵向项目变更详情(p,child,0),_=>await Controller(user).获取纵向项目结题详情(p,mid,child)};
 async Task CheckStageAccess(string stage,int template,int child,int templateIndex) {
   var st=stepTemplates[templateIndex];
   await db.ExecuteAsync("INSERT INTO \"步骤模板\" (\"编号\",\"流程模板编号\",\"步骤类型编号\",\"名称\",\"上级编号\") VALUES (@st,@template,400,@marker,0); INSERT INTO \"步骤角色\" (\"步骤模板编号\",\"角色编号\") VALUES (@st,@stageSpecificRole)",new{st,template,marker,stageSpecificRole});
   var flow=await Flow(template,child,owner,0);
   var step=await db.QuerySingleAsync<int>("INSERT INTO \"步骤\" (\"步骤模板编号\",\"流程编号\",\"状态\",\"建立时间\") VALUES (@st,@flow,0,now()) RETURNING \"编号\"",new{st,flow}); steps.Add(step);
   await db.ExecuteAsync("INSERT INTO \"步骤指定人\" (\"步骤编号\",\"指定人编号\") VALUES (@step,@stageDirect)",new{step,stageDirect});
   Check(Total(await List(stageDirect,stage))==1 && Has(await List(stageDirect,stage),"编号",child),stage+" stage direct assignee sees only assigned child");
   Check(!Forbidden(await Detail(stageDirect,stage,projects[0],stage=="mid"?child:0,child)),stage+" stage assignee can read corresponding detail");
   Check(Total(await List(stageRole,stage))==0,stage+" direct assignee takes precedence over role");
   Check(Forbidden(await Detail(stageRole,stage,projects[0],0,child)),stage+" unassigned role cannot read assigned detail");
   var otherStage=stage=="mid"?"change":"mid";
   Check(Total(await List(stageDirect,otherStage))==0,stage+" assignee cannot list other stage");
   Check(Forbidden(await Detail(stageDirect,otherStage,projects[0],0,otherStage=="mid"?midterms[0]:changes[0])),stage+" assignee cannot read other stage");
   var foreign=stage=="mid"?midterms[1]:stage=="change"?changes[1]:closes[1];
   Check(Forbidden(await Detail(stageDirect,stage,projects[1],0,foreign)),stage+" assignee cannot read foreign parent");
   await db.ExecuteAsync("DELETE FROM \"步骤指定人\" WHERE \"步骤编号\"=@step",new{step});
   Check(Total(await List(stageRole,stage))==1 && Has(await List(stageRole,stage),"编号",child),stage+" role assignee when no direct assignee");
   Check(!Forbidden(await Detail(stageRole,stage,projects[0],stage=="mid"?child:0,child)),stage+" role assignee can read detail");
   await db.ExecuteAsync("UPDATE \"步骤\" SET \"状态\"=1 WHERE \"编号\"=@step",new{step});
   Check(Total(await List(stageRole,stage))==0,stage+" completed step no longer grants list access");
   Check(Forbidden(await Detail(stageRole,stage,projects[0],0,child)),stage+" completed step no longer grants detail access");
   await db.ExecuteAsync("DELETE FROM \"步骤\" WHERE \"编号\"=@step; DELETE FROM \"流程\" WHERE \"编号\"=@flow",new{step,flow});
 }
 await CheckList("mid"); await CheckList("change");
 await CheckStageAccess("mid",420,midterms[0],2); await CheckStageAccess("change",430,changes[0],3);
 for(int i=0;i<2;i++) closes.Add(await db.QuerySingleAsync<int>("INSERT INTO \"纵向项目结题\" (\"纵向项目编号\",\"备注\") VALUES (@p,@marker) RETURNING \"编号\"",new{p=projects[i],marker}));
 await CheckList("close");
 await CheckStageAccess("close",440,closes[0],4);
 foreach(var stage in new[]{"mid","change","close"}) {
   var children=stage=="mid"?midterms:stage=="change"?changes:closes;
   Check(!Forbidden(await Detail(owner,stage,projects[0],midterms[0],children[0])),stage+" matching child visible to owner");
   foreach(var user in new[]{manager,participant,direct,roleUser}) Check(!Forbidden(await Detail(user,stage,projects[0],midterms[0],children[0])),$"{stage} detail user {user-id}");
   Check(Forbidden(await Detail(other,stage,projects[0],midterms[0],children[0])),stage+" unauthorized parent forbidden");
   Check(Forbidden(await Detail(owner,stage,projects[0],midterms[0],children[1])),stage+" cross-parent child forbidden");
 }
 Check(Forbidden(await Detail(owner,"close",projects[0],midterms[1],closes[0])),"closeout cross-parent midterm forbidden");
 Check(!Forbidden(await Detail(owner,"mid",projects[2],0,0)),"midterm zero child preserves uninitiated detail");
 Check(!Forbidden(await Detail(owner,"close",projects[2],0,0)),"closeout zero children preserve uninitiated detail");
 Console.WriteLine($"Vertical lifecycle scope regression passed: {checks} checks; real controllers and isolated PostgreSQL.");
} finally {
 await db.ExecuteAsync("""
 DELETE FROM "步骤指定人" WHERE "步骤编号"=ANY(@steps);
 DELETE FROM "步骤" WHERE "编号"=ANY(@steps);
 DELETE FROM "步骤角色" WHERE "步骤模板编号"=ANY(@stepTemplates);
 DELETE FROM "步骤模板" WHERE "编号"=ANY(@stepTemplates);
 DELETE FROM "流程" WHERE "编号"=ANY(@flows);
 DELETE FROM "项目参与人" WHERE ("流程模板编号"=400 AND "项目编号"=ANY(@applications)) OR ("流程模板编号"=410 AND "项目编号"=ANY(@projects));
 DELETE FROM "纵向项目中检" WHERE "编号"=ANY(@midterms);
 DELETE FROM "纵向项目变更" WHERE "编号"=ANY(@changes);
 DELETE FROM "纵向项目结题" WHERE "编号"=ANY(@closes);
 DELETE FROM "纵向项目立项" WHERE "编号"=ANY(@projects);
 DELETE FROM "纵向项目申报" WHERE "编号"=ANY(@applications);
 DELETE FROM "人员角色" WHERE "人员编号"=ANY(@people);
 DELETE FROM "角色" WHERE "编号" IN (@role,@irrelevantRole,@stageSpecificRole);
 DELETE FROM "人员" WHERE "编号"=ANY(@people);
 DELETE FROM "部门" WHERE "编号"=@id;
 """,new{steps=steps.ToArray(),stepTemplates,flows=flows.ToArray(),applications=applications.ToArray(),projects=projects.ToArray(),midterms=midterms.ToArray(),changes=changes.ToArray(),closes=closes.ToArray(),people,role,irrelevantRole,stageSpecificRole,id});
}
sealed class FixedConnectionFactory(string connectionString):IDatabaseConnectionFactory { public string Create(string databaseKey,string environmentName)=>connectionString; }
sealed class TestEnvironment:IWebHostEnvironment { public string ApplicationName{get;set;}="VerticalLifecycleScopeRegression"; public string EnvironmentName{get;set;}="Production"; public string ContentRootPath{get;set;}="/tmp"; public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider(); public string WebRootPath{get;set;}="/tmp"; public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider(); }
