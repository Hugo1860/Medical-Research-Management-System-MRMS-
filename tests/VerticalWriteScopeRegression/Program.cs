using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Npgsql;
using ScientificResearch.Business;
using ScientificResearch.Controllers;
using ScientificResearch.Infrastucture;
using ScientificResearch.Models;

var connection = Environment.GetEnvironmentVariable("VERTICAL_WRITE_TEST_CONNECTION");
if (string.IsNullOrWhiteSpace(connection) || Environment.GetEnvironmentVariable("VERTICAL_WRITE_TEST_ALLOW_FIXTURES") != "YES")
    throw new InvalidOperationException("Use an isolated migrated database and set VERTICAL_WRITE_TEST_ALLOW_FIXTURES=YES.");
SqlMapper.AddTypeHandler(new NpgsqlDateTimeTypeHandler());
await using var db = new NpgsqlConnection(connection); await db.OpenAsync();
var key = Random.Shared.Next(1500000000, 1800000000); var owner = key + 1; var stranger = key + 2;
var templates = new[] { 400, 410, 420, 430, 440 }; var stepTemplates = Enumerable.Range(0, 5).Select(i => key + 10 + i).ToArray();
var flows = new List<int>(); var steps = new List<int>(); var apps = new List<int>(); var projects = new List<int>(); var mids = new List<int>(); var changes = new List<int>(); var closes = new List<int>();
var config = new Dictionary<string, string?> { ["流程模板编号:纵向项目申报"]="400",["流程模板编号:纵向项目立项"]="410",["流程模板编号:纵向项目中检"]="420",["流程模板编号:纵向项目变更"]="430",["流程模板编号:纵向项目结题"]="440" };
using var services = new ServiceCollection().AddSingleton<IDatabaseConnectionFactory>(new FixedConnectionFactory(connection)).AddSingleton<IWebHostEnvironment>(new TestEnvironment()).AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(config).Build()).BuildServiceProvider();
YProjectController Controller(int user) { var ctx=new DefaultHttpContext { RequestServices=services }; ctx.Items[KeycloakUserMiddleware.CurrentUserItemKey]=new CurrentUser { 编号=user, DbKey="ScientificResearch_Model", 工号="synthetic-"+user, 部门编号=key }; return new YProjectController { ControllerContext=new ControllerContext { HttpContext=ctx } }; }
var checks=0;
void Check(bool value,string name) { if (!value) throw new Exception("FAIL: " + name); checks++; Console.WriteLine("PASS: " + name); }
async Task Reject(Func<Task> action,string name) { try { await action(); throw new Exception("FAIL: " + name + " accepted"); } catch (ForbiddenAccessException) { Check(true,name); } }
try {
 await db.ExecuteAsync("""
 INSERT INTO "部门" ("编号","上级部门编号","名称") VALUES (@key,0,'synthetic-write-scope');
 INSERT INTO "人员" ("编号","工号","密码","姓名","部门编号","是否启用") VALUES (@owner,'scope-write-'||@owner,'not-a-login','synthetic owner',@key,true),(@stranger,'scope-write-'||@stranger,'not-a-login','synthetic stranger',@key,true);
 """,new {key,owner,stranger});
 for (int i=0; i<5; i++) await db.ExecuteAsync("INSERT INTO \"步骤模板\" (\"编号\",\"流程模板编号\",\"步骤类型编号\",\"名称\",\"上级编号\") VALUES (@id,@template,400,'synthetic',0)",new{id=stepTemplates[i],template=templates[i]});
 for (int i=0; i<2; i++) {
  apps.Add(await db.QuerySingleAsync<int>("""INSERT INTO "纵向项目申报" ("分类","项目名称","申报人编号","申报年度","申请经费","计划开始日期","计划完成日期","是否立项") VALUES (1,@name,@person,2026,100,'2026-01-01','2028-01-01',false) RETURNING "编号" """,new{name=$"synthetic-write-{key}-{i}",person=i==0?owner:stranger}));
  projects.Add(await db.QuerySingleAsync<int>("""INSERT INTO "纵向项目立项" ("分类","项目编号","项目中文名称","负责人编号","成果归属部门编号","立项年度","登记日期","登记人编号","批准日期","计划开始日期","计划完成日期","学科门类","学科类型","研究类型","项目类型","项目级别","项目来源单位") VALUES (1,@code,@name,@person,@key,2026,CURRENT_DATE,@person,'2026-01-02','2026-02-01','2028-01-31','Medicine','Clinical','Applied','Research','Institutional','Synthetic') RETURNING "编号" """,new{code=$"synthetic-{key}-{i}",name=$"synthetic-write-{key}-{i}",person=i==0?owner:stranger,key}));
  mids.Add(await db.QuerySingleAsync<int>("INSERT INTO \"纵向项目中检\" (\"纵向项目编号\",\"备注\") VALUES (@parent,'original') RETURNING \"编号\"",new{parent=projects[i]}));
  changes.Add(await db.QuerySingleAsync<int>("""INSERT INTO "纵向项目变更" ("纵向项目编号","分类","立项年度","登记日期","登记人编号","批准日期","计划开始日期","计划完成日期","学科门类","学科类型","研究类型","项目类型","项目级别","项目来源单位","变更时间","变更事由") VALUES (@parent,1,2026,CURRENT_DATE,@owner,CURRENT_DATE,'2026-01-01','2028-01-01','Medicine','Clinical','Applied','Research','Institutional','Synthetic',now(),'original') RETURNING "编号" """,new{parent=projects[i],owner}));
  closes.Add(await db.QuerySingleAsync<int>("INSERT INTO \"纵向项目结题\" (\"纵向项目编号\",\"备注\") VALUES (@parent,'original') RETURNING \"编号\"",new{parent=projects[i]}));
 }
 var ids = new[] {apps[0],projects[0],mids[0],changes[0],closes[0]}; var otherIds = new[] {apps[1],projects[1],mids[1],changes[1],closes[1]}; var stages = new[] {"申报","立项","中检","变更","结题"};
 for (int i=0; i<5; i++) {
  var flow=await db.QuerySingleAsync<int>("INSERT INTO \"流程\" (\"流程模板编号\",\"项目编号\",\"发起人编号\",\"状态\",\"创建时间\") VALUES (@template,@project,@owner,0,now()) RETURNING \"编号\"",new{template=templates[i],project=ids[i],owner}); flows.Add(flow);
  var step=await db.QuerySingleAsync<int>("INSERT INTO \"步骤\" (\"步骤模板编号\",\"流程编号\",\"状态\",\"建立时间\") VALUES (@template,@flow,0,now()) RETURNING \"编号\"",new{template=stepTemplates[i],flow}); steps.Add(step);
  await db.ExecuteAsync("INSERT INTO \"步骤指定人\" (\"步骤编号\",\"指定人编号\") VALUES (@step,@owner)",new{step,owner});
 }
 var business = new WorkFlowBusiness(connection);
 for (int i=0;i<5;i++) {
  int j=i; var step=new 完成步骤{步骤编号=steps[j],状态值=1,备注="synthetic"};
  WorkFlowBusiness.纵向步骤绑定 Binding(int child,int? parent=null,int? template=null)=>new(){阶段=stages[j],流程模板编号=template??templates[j],子记录编号=child,立项编号=parent};
  await business.完成步骤<object>(step,owner,null,true,绑定:Binding(ids[j],j>=2?projects[0]:null)); Check(true,stages[j]+" valid active binding hold");
  await Reject(()=>business.完成步骤<object>(step,owner,null,true,绑定:Binding(otherIds[j],j>=2?projects[1]:null)),stages[j]+" foreign entity rejected");
  await Reject(()=>business.完成步骤<object>(step,owner,null,true,绑定:Binding(ids[j],j>=2?projects[0]:null,templates[(j+1)%5])),stages[j]+" wrong template rejected");
  await Reject(()=>business.完成步骤<object>(step,stranger,null,true,绑定:Binding(ids[j],j>=2?projects[0]:null)),stages[j]+" unauthorized operator rejected");
  if (j>=2) await Reject(()=>business.完成步骤<object>(step,owner,null,true,绑定:Binding(ids[j],projects[1])),stages[j]+" forged parent rejected");
  await db.ExecuteAsync("UPDATE \"步骤\" SET \"状态\"=1 WHERE \"编号\"=@id",new{id=steps[j]});
  await Reject(()=>business.完成步骤<object>(step,owner,null,true,绑定:Binding(ids[j],j>=2?projects[0]:null)),stages[j]+" ended step rejected");
  await db.ExecuteAsync("UPDATE \"步骤\" SET \"状态\"=0 WHERE \"编号\"=@id",new{id=steps[j]});
 }
 var controller=Controller(owner);
 await Reject(()=>controller.新增科研项目申报(new 纵向项目申报增改 { 基本资料=new 纵向项目申报{编号=apps[1]} }),"new application cannot overwrite existing id");
 foreach(var action in new Func<纵向项目立项增改,Task>[] {controller.新增科研项目立项,controller.新增重点学科立项,controller.新增人才培养立项})
   await Reject(()=>action(new 纵向项目立项增改{基本资料=new 纵向项目立项{编号=projects[1]}}),"new approval cannot overwrite existing id");
 await Reject(()=>controller.新增纵向项目变更(new 纵向项目变更增改 {基本资料=new 纵向项目变更{编号=changes[1],纵向项目编号=projects[0]}}),"new change cannot overwrite existing id");
 await Reject(()=>controller.新增纵向项目申报立项(new 纵向项目立项增改{基本资料=new 纵向项目立项{编号=apps[1]}}),"foreign application cannot be converted");
 var midModel=new 纵向项目中检{编号=mids[0],纵向项目编号=projects[0],备注="valid-hold"};
 await controller.完善纵向项目中检资料(new 完成步骤<纵向项目中检完善>{步骤编号=steps[2],状态值=1,备注="synthetic",附加数据=new 纵向项目中检完善{基本资料=midModel,IsHold=true}});
 Check(await db.QuerySingleAsync<string>("SELECT \"备注\" FROM \"纵向项目中检\" WHERE \"编号\"=@id",new{id=mids[0]})=="valid-hold","valid midterm hold persisted");
 midModel.编号=mids[1]; midModel.纵向项目编号=projects[0]; midModel.备注="forged";
 await Reject(()=>controller.完善纵向项目中检资料(new 完成步骤<纵向项目中检完善>{步骤编号=steps[2],状态值=1,备注="synthetic",附加数据=new 纵向项目中检完善{基本资料=midModel,IsHold=true}}),"cross-project midterm controller rejected");
 Check(await db.QuerySingleAsync<string>("SELECT \"备注\" FROM \"纵向项目中检\" WHERE \"编号\"=@id",new{id=mids[1]})=="original","foreign midterm unchanged");
 var appModel=await db.QuerySingleAsync<纵向项目申报>("SELECT * FROM \"纵向项目申报\" WHERE \"编号\"=@id",new{id=apps[0]});
 appModel.项目名称="valid-application-hold";
 await controller.完善纵向项目申报资料(new 完成步骤<纵向项目申报增改>{步骤编号=steps[0],状态值=1,备注="synthetic",附加数据=new 纵向项目申报增改{基本资料=appModel,参与人列表=Array.Empty<项目参与人>(),IsHold=true}});
 Check(await db.QuerySingleAsync<string>("SELECT \"项目名称\" FROM \"纵向项目申报\" WHERE \"编号\"=@id",new{id=apps[0]})=="valid-application-hold","valid application hold persisted");
 appModel.编号=apps[1]; appModel.项目名称="forged-application";
 await Reject(()=>controller.完善纵向项目申报资料(new 完成步骤<纵向项目申报增改>{步骤编号=steps[0],状态值=1,备注="synthetic",附加数据=new 纵向项目申报增改{基本资料=appModel,参与人列表=Array.Empty<项目参与人>(),IsHold=true}}),"foreign application controller rejected");
 Check(await db.QuerySingleAsync<string>("SELECT \"项目名称\" FROM \"纵向项目申报\" WHERE \"编号\"=@id",new{id=apps[1]})!="forged-application","foreign application unchanged");
 var projectModel=await db.QuerySingleAsync<纵向项目立项>("SELECT * FROM \"纵向项目立项\" WHERE \"编号\"=@id",new{id=projects[0]});
 projectModel.项目中文名称="valid-approval-hold"; projectModel.资助类型="自筹经费";
 纵向项目立项增改 ProjectData(纵向项目立项 p)=>new(){基本资料=p,参与人列表=Array.Empty<项目参与人>(),计划列表=Array.Empty<项目计划>(),经费预算列表=Array.Empty<项目经费预算>(),预期成果列表=Array.Empty<项目预期成果>(),IsHold=true};
 await controller.完善纵向项目立项资料(new 完成步骤<纵向项目立项增改>{步骤编号=steps[1],状态值=1,备注="synthetic",附加数据=ProjectData(projectModel)});
 Check(await db.QuerySingleAsync<string>("SELECT \"项目中文名称\" FROM \"纵向项目立项\" WHERE \"编号\"=@id",new{id=projects[0]})=="valid-approval-hold","valid approval hold persisted");
 projectModel.编号=projects[1]; projectModel.项目中文名称="forged-approval";
 await Reject(()=>controller.完善纵向项目立项资料(new 完成步骤<纵向项目立项增改>{步骤编号=steps[1],状态值=1,备注="synthetic",附加数据=ProjectData(projectModel)}),"foreign approval controller rejected");
 Check(await db.QuerySingleAsync<string>("SELECT \"项目中文名称\" FROM \"纵向项目立项\" WHERE \"编号\"=@id",new{id=projects[1]})!="forged-approval","foreign approval unchanged");
 var changeModel=await db.QuerySingleAsync<纵向项目变更>("SELECT * FROM \"纵向项目变更\" WHERE \"编号\"=@id",new{id=changes[0]});
 changeModel.变更事由="valid-change-hold";
 await controller.完善纵向项目变更资料(new 完成步骤<纵向项目变更增改>{步骤编号=steps[3],状态值=1,备注="synthetic",附加数据=new 纵向项目变更增改{基本资料=changeModel,IsHold=true}});
 Check(await db.QuerySingleAsync<string>("SELECT \"变更事由\" FROM \"纵向项目变更\" WHERE \"编号\"=@id",new{id=changes[0]})=="valid-change-hold","valid change hold persisted");
 changeModel.编号=changes[1]; changeModel.变更事由="forged-change";
 await Reject(()=>controller.完善纵向项目变更资料(new 完成步骤<纵向项目变更增改>{步骤编号=steps[3],状态值=1,备注="synthetic",附加数据=new 纵向项目变更增改{基本资料=changeModel,IsHold=true}}),"foreign change controller rejected");
 Check(await db.QuerySingleAsync<string>("SELECT \"变更事由\" FROM \"纵向项目变更\" WHERE \"编号\"=@id",new{id=changes[1]})=="original","foreign change unchanged");
 var closeModel=new 纵向项目结题{编号=closes[0],纵向项目编号=projects[0],备注="valid-close-hold"};
 await controller.完善纵向项目结题资料(new 完成步骤<纵向项目结题完善>{步骤编号=steps[4],状态值=1,备注="synthetic",附加数据=new 纵向项目结题完善{基本资料=closeModel,IsHold=true}});
 Check(await db.QuerySingleAsync<string>("SELECT \"备注\" FROM \"纵向项目结题\" WHERE \"编号\"=@id",new{id=closes[0]})=="valid-close-hold","valid closeout hold persisted");
 closeModel.编号=closes[1]; closeModel.备注="forged-close";
 await Reject(()=>controller.完善纵向项目结题资料(new 完成步骤<纵向项目结题完善>{步骤编号=steps[4],状态值=1,备注="synthetic",附加数据=new 纵向项目结题完善{基本资料=closeModel,IsHold=true}}),"foreign closeout controller rejected");
 Check(await db.QuerySingleAsync<string>("SELECT \"备注\" FROM \"纵向项目结题\" WHERE \"编号\"=@id",new{id=closes[1]})=="original","foreign closeout unchanged");
 await Reject(()=>controller.审核并应用纵向项目变更(new 完成步骤<int>{步骤编号=steps[3],状态值=1,备注="synthetic",附加数据=changes[1]}),"cross-project change application rejected");
 Check(await db.QuerySingleAsync<int>("SELECT \"状态\" FROM \"步骤\" WHERE \"编号\"=@id",new{id=steps[3]})==0,"rejected change application does not complete step");
 appModel.编号=apps[0]; appModel.项目名称="valid-application-complete";
 await controller.完善纵向项目申报资料(new 完成步骤<纵向项目申报增改>{步骤编号=steps[0],状态值=1,备注="synthetic",附加数据=new 纵向项目申报增改{基本资料=appModel,参与人列表=Array.Empty<项目参与人>(),IsHold=false}});
 projectModel.编号=projects[0]; projectModel.项目中文名称="valid-approval-complete";
 var projectData=ProjectData(projectModel); projectData.IsHold=false;
 await controller.完善纵向项目立项资料(new 完成步骤<纵向项目立项增改>{步骤编号=steps[1],状态值=1,备注="synthetic",附加数据=projectData});
 midModel.编号=mids[0]; midModel.备注="valid-mid-complete";
 await controller.完善纵向项目中检资料(new 完成步骤<纵向项目中检完善>{步骤编号=steps[2],状态值=1,备注="synthetic",附加数据=new 纵向项目中检完善{基本资料=midModel,IsHold=false}});
 changeModel.编号=changes[0]; changeModel.变更事由="valid-change-complete";
 await controller.完善纵向项目变更资料(new 完成步骤<纵向项目变更增改>{步骤编号=steps[3],状态值=1,备注="synthetic",附加数据=new 纵向项目变更增改{基本资料=changeModel,IsHold=false}});
 closeModel.编号=closes[0]; closeModel.备注="valid-close-complete";
 await controller.完善纵向项目结题资料(new 完成步骤<纵向项目结题完善>{步骤编号=steps[4],状态值=1,备注="synthetic",附加数据=new 纵向项目结题完善{基本资料=closeModel,IsHold=false}});
 Check(await db.QuerySingleAsync<int>("SELECT count(*)::integer FROM \"步骤\" WHERE \"编号\"=ANY(@steps) AND \"状态\"=1",new{steps=steps.ToArray()})==5,"all five legitimate controller actions complete their own steps");
 Check(await db.QuerySingleAsync<string>("SELECT \"项目名称\" FROM \"纵向项目申报\" WHERE \"编号\"=@id",new{id=apps[0]})=="valid-application-complete","application completion persisted");
 Check(await db.QuerySingleAsync<string>("SELECT \"项目中文名称\" FROM \"纵向项目立项\" WHERE \"编号\"=@id",new{id=projects[0]})=="valid-approval-complete","approval completion persisted");
 Check(await db.QuerySingleAsync<string>("SELECT \"备注\" FROM \"纵向项目中检\" WHERE \"编号\"=@id",new{id=mids[0]})=="valid-mid-complete","midterm completion persisted");
 Check(await db.QuerySingleAsync<string>("SELECT \"变更事由\" FROM \"纵向项目变更\" WHERE \"编号\"=@id",new{id=changes[0]})=="valid-change-complete","change completion persisted");
 Check(await db.QuerySingleAsync<string>("SELECT \"备注\" FROM \"纵向项目结题\" WHERE \"编号\"=@id",new{id=closes[0]})=="valid-close-complete","closeout completion persisted");
 Console.WriteLine($"Vertical write scope regression passed: {checks} checks; isolated PostgreSQL and real business/controllers.");
} finally {
 await db.ExecuteAsync("""
 DELETE FROM "步骤指定人" WHERE "步骤编号"=ANY(@steps);
 DELETE FROM "步骤" WHERE "编号"=ANY(@steps);
 DELETE FROM "流程" WHERE "编号"=ANY(@flows);
 DELETE FROM "步骤模板" WHERE "编号"=ANY(@stepTemplates);
 DELETE FROM "纵向项目中检" WHERE "编号"=ANY(@mids);
 DELETE FROM "纵向项目变更" WHERE "编号"=ANY(@changes);
 DELETE FROM "纵向项目结题" WHERE "编号"=ANY(@closes);
 DELETE FROM "纵向项目立项" WHERE "编号"=ANY(@projects);
 DELETE FROM "纵向项目申报" WHERE "编号"=ANY(@apps);
 DELETE FROM "人员" WHERE "编号" IN (@owner,@stranger);
 DELETE FROM "部门" WHERE "编号"=@key;
 """,new {steps=steps.ToArray(),flows=flows.ToArray(),stepTemplates,mids=mids.ToArray(),changes=changes.ToArray(),closes=closes.ToArray(),projects=projects.ToArray(),apps=apps.ToArray(),owner,stranger,key});
}
sealed class FixedConnectionFactory(string connectionString):IDatabaseConnectionFactory { public string Create(string databaseKey,string environmentName)=>connectionString; }
sealed class TestEnvironment:IWebHostEnvironment { public string ApplicationName{get;set;}="VerticalWriteScopeRegression"; public string EnvironmentName{get;set;}="Production"; public string ContentRootPath{get;set;}="/tmp"; public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider(); public string WebRootPath{get;set;}="/tmp"; public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider(); }
