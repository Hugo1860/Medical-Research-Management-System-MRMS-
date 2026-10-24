using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using ScientificResearch.Infrastucture;
using ScientificResearch.Models;

namespace ScientificResearch.Controllers;

/// <summary>
/// 独立科研伦理审查流程。该控制器只登记真实申请、委员会意见和决定，
/// 不使用示例数据；外部伦理系统同步仍由 YProjectController 负责。
/// </summary>
public sealed class EthicsController : BaseController
{
    private int 纵向申报模板 => config.GetValue<int>("流程模板编号:纵向项目申报");
    private int 纵向立项模板 => config.GetValue<int>("流程模板编号:纵向项目立项");
    private int 横向申请模板 => config.GetValue<int>("流程模板编号:横向项目申请");

    /// <summary>查询当前人员可见的伦理申请。伦理委员会角色可查看全院申请。</summary>
    [HttpGet]
    public async Task<object> 查询伦理申请(string 状态 = null, string 申请类型 = null, string 关键词 = null, int? 项目编号 = null, int? 数量 = null)
    {
        var committee = await 是伦理管理角色();
        var limit = Math.Clamp(数量 ?? 100, 1, 500);
        return await Db.QueryAsync<object>(@"
SELECT a.""编号"",a.""流程模板编号"",a.""项目编号"",a.""申请人编号"",p.""姓名"" AS ""申请人姓名"",
       COALESCE(y.""项目中文名称"",ya.""项目名称"",x.""合同名称"",'未命名项目') AS ""项目名称"",
       a.""申请类型"",a.""申请标题"",a.""受试者风险等级"",a.""状态"",a.""提交时间"",a.""受理时间"",a.""决定时间"",a.""批件编号"",a.""有效期至"",a.""建立时间"",a.""更新时间"",
       CASE WHEN @管理角色 THEN '伦理管理' WHEN a.""申请人编号""=@人员编号 THEN '本人' ELSE '被指派委员（最小披露）' END AS ""可见范围""
  FROM ""伦理审查申请"" a
  LEFT JOIN ""人员"" p ON p.""编号""=a.""申请人编号""
  LEFT JOIN ""纵向项目立项"" y ON a.""流程模板编号""=@纵向立项模板 AND y.""编号""=a.""项目编号""
  LEFT JOIN ""纵向项目申报"" ya ON a.""流程模板编号""=@纵向申报模板 AND ya.""编号""=a.""项目编号""
  LEFT JOIN ""横向项目"" x ON a.""流程模板编号""=@横向申请模板 AND x.""编号""=a.""项目编号""
 WHERE (@状态 IS NULL OR a.""状态""=@状态)
   AND (@申请类型 IS NULL OR a.""申请类型""=@申请类型)
   AND (@项目编号 IS NULL OR a.""项目编号""=@项目编号)
   AND (@关键词 IS NULL OR a.""申请标题"" ILIKE '%'||@关键词||'%' OR a.""批件编号"" ILIKE '%'||@关键词||'%' OR p.""姓名"" ILIKE '%'||@关键词||'%')
   AND (@管理角色 OR a.""申请人编号""=@人员编号 OR EXISTS (
       SELECT 1 FROM ""伦理审查会议议程"" g
       JOIN ""伦理审查会议委员"" c ON c.""会议编号""=g.""会议编号""
      WHERE g.""申请编号""=a.""编号"" AND c.""委员编号""=@人员编号
        AND c.""状态"" IN ('正式指派','回避')
   ))
 ORDER BY CASE a.""状态"" WHEN '审查中' THEN 1 WHEN '待受理' THEN 2 WHEN '需补正' THEN 3 ELSE 4 END,a.""更新时间"" DESC,a.""编号"" DESC
 LIMIT @数量;", new
        {
            状态 = 空值(状态), 申请类型 = 空值(申请类型), 关键词 = 空值(关键词), 项目编号,
            纵向立项模板, 纵向申报模板, 横向申请模板, 管理角色 = committee,
            人员编号 = CurrentUser.编号, 数量 = limit
        });
    }

    /// <summary>提交初始审查、修正案、年度跟踪或延期续展申请。</summary>
    [HttpPost]
    public async Task<object> 提交伦理申请([FromBody] 伦理申请保存 model)
    {
        校验申请(model);
        if (!await CanAccessProjectAsync(model.流程模板编号, model.项目编号)) return ProjectAccessDenied();
        var title = model.申请标题.Trim();
        var summary = model.研究方案摘要.Trim();
        var applicationType = model.申请类型.Trim();
        if (title.Length > 200 || summary.Length > 20000) throw new ArgumentException("伦理申请标题或方案摘要超出长度限制");
        var risk = string.IsNullOrWhiteSpace(model.受试者风险等级) ? "一般风险" : model.受试者风险等级.Trim();
        if (risk is not ("低风险" or "一般风险" or "高风险")) throw new ArgumentException("受试者风险等级无效");
        if (model.编号.HasValue)
        {
            var changed = await Db.ExecuteAsync(@"
UPDATE ""伦理审查申请"" SET ""申请类型""=@申请类型,""申请标题""=@申请标题,""研究方案摘要""=@研究方案摘要,
       ""受试者风险等级""=@受试者风险等级,""知情同意说明""=@知情同意说明,""附件路径""=@附件路径,""备注""=@备注,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@编号 AND ""申请人编号""=@申请人编号 AND ""状态"" IN ('草稿','需补正');", new
            {
                model.编号, 申请类型 = applicationType, 申请标题 = title, 研究方案摘要 = summary, 受试者风险等级 = risk,
                知情同意说明 = 空值(model.知情同意说明), 附件路径 = 空值(model.附件路径), 备注 = 空值(model.备注), 申请人编号 = CurrentUser.编号
            });
            if (changed == 0) return NotFound(new { error = "伦理申请不存在、无权修改或当前状态不可编辑" });
            return new { 编号 = model.编号.Value, 状态 = "草稿", message = "伦理申请草稿已保存" };
        }
        var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""伦理审查申请"" (""流程模板编号"",""项目编号"",""申请人编号"",""申请类型"",""申请标题"",""研究方案摘要"",""受试者风险等级"",""知情同意说明"",""附件路径"",""备注"",""建立人编号"")
VALUES (@流程模板编号,@项目编号,@申请人编号,@申请类型,@申请标题,@研究方案摘要,@受试者风险等级,@知情同意说明,@附件路径,@备注,@建立人编号)
RETURNING ""编号"";", new
        {
            model.流程模板编号, model.项目编号, 申请人编号 = CurrentUser.编号, 申请类型 = applicationType,
            申请标题 = title, 研究方案摘要 = summary, 受试者风险等级 = risk,
            知情同意说明 = 空值(model.知情同意说明), 附件路径 = 空值(model.附件路径), 备注 = 空值(model.备注), 建立人编号 = CurrentUser.编号
        });
        return new { 编号 = id, 状态 = "草稿", message = "伦理申请草稿已创建，请提交后进入委员会受理" };
    }

    /// <summary>获取免除知情同意与简易快审向导的推荐配置与预设文本模板。</summary>
    [HttpGet]
    public object 获取免除知情同意与快审向导配置(string 场景类型 = null)
    {
        var templates = new List<object>
        {
            new
            {
                场景类型 = "回顾性病历分析",
                适用范围 = "利用既往临床诊疗中已记录的病历资料、化验数据、影像学检查结果开展的回顾性总结，不增加患者额外检查或干预风险（二级医院占比超80%）。",
                建议审查类型 = "简易快审",
                建议风险等级 = "低风险",
                默认研究目的 = "通过对既往临床确诊病例的诊疗过程及随访数据进行回顾性整理分析，探讨临床疗效、危险因素与预后指标，为临床规范化诊疗提供现实数据支持。",
                默认纳入标准 = "符合临床诊断标准；于本院接受规范诊治并具备完整出院病案及随访记录者。",
                默认排除标准 = "病历资料严重缺失、随访失访或无法核实关键临床转归指标者。",
                默认数据项 = "年龄、性别、主诉病程、既往史、入院检验化验指标、影像学特征、治疗方案、出院转归及并发症随访记录。",
                免除知情同意依据 = "依据国家卫健委《涉及人的生命科学和医学研究伦理审查办法》第三十二条规定：本研究仅使用既往临床诊疗中获得的病史数据与医疗记录开展回顾性研究；研究不大于最小风险；免除知情同意不会对受试者合法权益造成不利影响；因受试者已出院或流动性大，无法逐一重新取得书面知情同意，故申请免除知情同意。",
                隐私与去标识化措施 = "1. 从病案系统调取数据后，立即剥离姓名、身份证号、联系电话、家庭住址等直接个人标识符，使用唯一科研代号（如 Case-001）替换；\n2. 仅在医院内网局域网封闭电脑上进行脱敏统计，严禁上传公网云盘；\n3. 研究报告与发表论文仅展现汇总统计指标（百分比、均值、相关系数等），任何第三方均无法反向识别受试者个体。"
            },
            new
            {
                场景类型 = "无创临床观察与残余标本",
                适用范围 = "利用临床已完成常规诊疗检验后即将废弃的残余血液/体液/组织标本进行体外补充检测，未额外采集或伤害受试者身体。",
                建议审查类型 = "简易快审",
                建议风险等级 = "低风险",
                默认研究目的 = "利用临床常规诊疗后残留的生物样本，开展新型生物标志物体外检测评价与辅助诊断效能分析。",
                默认纳入标准 = "临床常规诊疗留样且检验后剩余满足实验要求的生物样本。",
                默认排除标准 = "标本溶血、污染、量不足或临床原检验项目未完成者。",
                默认数据项 = "样本检测指标数值、标本采集时间、患者年龄段及基本临床分型。",
                免除知情同意依据 = "标本均为临床常规检验后拟废弃的剩余样本，未对受试者增加任何额外的侵入性创伤或健康风险；样本检测结果不直接用于临床诊疗计费或改变既定治疗方案，符合伦理法规中残余标本研究豁免知情同意的法定条件。",
                隐私与去标识化措施 = "1. 实验室接收样本时进行双盲编码，实验操作人员接触不到受试者身份信息；\n2. 剩余样本检测完毕后按医疗废物规范集中销毁，不作他用；\n3. 实验数据以组群汇总统计，不披露受试者个体关联映射表。"
            },
            new
            {
                场景类型 = "问卷调查与认知度调研",
                适用范围 = "面向门诊/住院患者或基层医务人员开展的无创满意度调查、疾病知晓度与健康素养测评，不涉及敏感疾病隐私或个人财产利益。",
                建议审查类型 = "简易快审",
                建议风险等级 = "低风险",
                默认研究目的 = "评估患者对疾病防治知识的认知水平、健康宣教接受度及诊疗依从性，为优化临床医患沟通与科室服务流程提供参考。",
                默认纳入标准 = "自愿参与问卷调研的门诊/住院患者、家属或医护人员，具备基本理解与表述能力。",
                默认排除标准 = "存在认知障碍、意识不清或拒绝参与问卷填写者。",
                默认数据项 = "问卷题目得分、宣教认知评分、满意度评价及一般人口学基本特征（年龄段、文化程度）。",
                免除知情同意依据 = "调查问卷首页明确告知研究目的、自愿参与原则及随时退出权利；问卷全程采取不记名自填形式，被调查人填写并提交问卷即视为同意参与，申请豁免签署书面知情同意书。",
                隐私与去标识化措施 = "1. 问卷不收集姓名、电话、工号及IP网络定位信息；\n2. 问卷纸质件或电子端汇总后统一归档加密存储，仅供本项目课题组统计分析；\n3. 公开发表的成果仅展现总体分布规律，不泄露特定人员回答记录。"
            }
        };

        if (!string.IsNullOrWhiteSpace(场景类型))
        {
            var match = templates.FirstOrDefault(t => ((dynamic)t).场景类型 == 场景类型.Trim());
            if (match != null) return match;
        }

        return templates;
    }

    /// <summary>生成符合二级医院规范的免除知情同意与简易快审审查材料 HTML（可直接打印或转为 Word 存档）。</summary>
    [HttpPost]
    public async Task<object> 生成简易伦理申请文档HTML([FromBody] 简易伦理向导申请 request)
    {
        if (request == null) throw new ArgumentException("申请向导参数不能为空");
        var user = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT p.""姓名"", p.""工号"", d.""部门名称"" AS ""科室名称"", p.""技术职称"", p.""电话""
  FROM ""人员"" p
  LEFT JOIN ""部门"" d ON d.""编号""=p.""部门编号""
 WHERE p.""编号""=@编号;", new { 编号 = CurrentUser.编号 });

        var authorName = user?.姓名 ?? "申请人";
        var deptName = user?.科室名称 ?? "临床科室";
        var title = string.IsNullOrWhiteSpace(request.申请标题) ? $"{deptName}{request.场景类型}伦理快审申请" : request.申请标题.Trim();
        var scene = string.IsNullOrWhiteSpace(request.场景类型) ? "回顾性病历分析" : request.场景类型.Trim();
        var docNo = $"EC-FAST-{DateTime.Now:yyyyMMdd}-{Math.Abs((title + CurrentUser.编号).GetHashCode()) % 9000 + 1000}";

        var html = $@"<!DOCTYPE html>
<html lang=""zh-CN"">
<head>
<meta charset=""utf-8"">
<title>{title} - 简易伦理快审与免除知情同意申请表</title>
<style>
  body {{ font-family: SimSun, 'Songti SC', serif; margin: 40px; color: #222; line-height: 1.8; }}
  h1 {{ text-align: center; font-size: 22px; font-weight: bold; margin-bottom: 4px; }}
  h2 {{ text-align: center; font-size: 16px; font-weight: normal; margin-bottom: 25px; color: #555; }}
  table {{ width: 100%; border-collapse: collapse; margin-top: 15px; margin-bottom: 25px; }}
  th, td {{ border: 1px solid #333; padding: 8px 12px; font-size: 13.5px; vertical-align: middle; }}
  th {{ background-color: #f7f9fa; font-weight: bold; width: 22%; }}
  .section-title {{ font-weight: bold; font-size: 15px; margin-top: 20px; margin-bottom: 8px; border-left: 4px solid #1677ff; padding-left: 8px; }}
  .sign-box {{ margin-top: 30px; display: flex; justify-content: space-between; }}
  .sign-item {{ width: 45%; border-top: 1px solid #333; padding-top: 8px; font-size: 13.5px; }}
  .highlight {{ background-color: #f0f7ff; padding: 10px; border: 1px dashed #91caff; border-radius: 4px; margin: 10px 0; }}
</style>
</head>
<body>
  <h1>医学伦理委员会简易审查（快审）与免除知情同意申请表</h1>
  <h2>文书编号：{docNo} &nbsp;&nbsp;|&nbsp;&nbsp; 申请日期：{DateTime.Now:yyyy年MM月dd日}</h2>

  <div class=""section-title"">一、研究基本信息</div>
  <table>
    <tr>
      <th>研究项目名称</th>
      <td colspan=""3""><strong>{title}</strong></td>
    </tr>
    <tr>
      <th>主要研究者 (PI)</th>
      <td>{authorName} ({user?.技术职称 ?? "医师"})</td>
      <th>所在科室</th>
      <td>{deptName}</td>
    </tr>
    <tr>
      <th>联系电话</th>
      <td>{user?.电话 ?? "医院内线"}</td>
      <th>研究场景分类</th>
      <td><span style=""color:#0958d9;font-weight:bold;"">{scene}</span></td>
    </tr>
    <tr>
      <th>建议审查方式</th>
      <td><strong>简易快审（主任委员/主审委员快审）</strong></td>
      <th>受试者风险等级</th>
      <td><span style=""color:#389e0d;font-weight:bold;"">低风险（不大于最小风险）</span></td>
    </tr>
  </table>

  <div class=""section-title"">二、研究方案简要说明</div>
  <table>
    <tr>
      <th>研究目的与背景</th>
      <td colspan=""3"">{(string.IsNullOrWhiteSpace(request.研究目的) ? "详见课题设计方案" : request.研究目的.Trim())}</td>
    </tr>
    <tr>
      <th>纳入与排除标准</th>
      <td colspan=""3"">{(string.IsNullOrWhiteSpace(request.样本纳入与排除标准) ? "符合临床诊断指标者纳入，资料缺失者排除" : request.样本纳入与排除标准.Trim())}</td>
    </tr>
    <tr>
      <th>预期样本例数</th>
      <td>{(string.IsNullOrWhiteSpace(request.预期样本量) ? "拟调取 50~200 例" : request.预期样本量.Trim())}</td>
      <th>主要提取数据项</th>
      <td>{(string.IsNullOrWhiteSpace(request.观察指标与数据项) ? "常规病史、辅助化验指标及转归" : request.观察指标与数据项.Trim())}</td>
    </tr>
  </table>

  <div class=""section-title"">三、免除知情同意理由与法律法规依据</div>
  <div class=""highlight"">
    <p><strong>申请免除知情同意之法定依据：</strong></p>
    <p>{(string.IsNullOrWhiteSpace(request.免除知情同意依据) ? "符合《涉及人的生命科学和医学研究伦理审查办法》第三十二条免除知情同意规定。" : request.免除知情同意依据.Trim())}</p>
  </div>

  <div class=""section-title"">四、受试者隐私保护与去标识化措施</div>
  <table>
    <tr>
      <th>数据安全与脱敏承诺</th>
      <td colspan=""3"">{(string.IsNullOrWhiteSpace(request.隐私与去标识化措施) ? "严格执行医院数据安全规范，数据去标识化后脱敏留存于局域网电脑。" : request.隐私与去标识化措施.Trim())}</td>
    </tr>
  </table>

  <div class=""section-title"">五、主审委员与伦理委员会快审决定（委员会填写）</div>
  <table>
    <tr>
      <th style=""height: 80px;"">主审委员意见</th>
      <td colspan=""3"">
        [ &nbsp; ] 同意免除知情同意，批准开展研究 &nbsp;&nbsp;&nbsp;&nbsp;
        [ &nbsp; ] 建议修改后重报 &nbsp;&nbsp;&nbsp;&nbsp;
        [ &nbsp; ] 建议转入全委员会会议审查<br/><br/>
        主审委员签名：____________________ &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; 日期：_______年____月____日
      </td>
    </tr>
    <tr>
      <th style=""height: 60px;"">伦理委员会盖章</th>
      <td colspan=""3"">
        医学伦理委员会（签章）： &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; 批件签发日期：_______年____月____日
      </td>
    </tr>
  </table>

  <div class=""sign-box"">
    <div class=""sign-item"">
      主要研究者 (PI) 承诺签名：____________________<br/>
      日期：{DateTime.Now:yyyy年MM月dd日}
    </div>
    <div class=""sign-item"">
      科室主任审核意见：&nbsp;同意申报 [ &nbsp; ]<br/>
      科主任签名：____________________
    </div>
  </div>
</body>
</html>";

        return new
        {
            文书编号 = docNo,
            项目名称 = title,
            主要研究者 = authorName,
            科室 = deptName,
            html
        };
    }

    /// <summary>一键将简易向导填报内容创建为伦理快审申请（低风险、免除知情），并支持直接递交委员会待受理队列。</summary>
    [HttpPost]
    public async Task<object> 提交简易快审申请([FromBody] 简易伦理向导申请 request)
    {
        if (request == null) throw new ArgumentException("简易快审向导参数不能为空");
        if (request.流程模板编号 <= 0 || request.项目编号 <= 0) throw new ArgumentException("必须指定关联的纵向/横向项目编号");
        if (!await CanAccessProjectAsync(request.流程模板编号, request.项目编号)) return ProjectAccessDenied();

        var title = string.IsNullOrWhiteSpace(request.申请标题) ? $"{request.场景类型}简易伦理快审申请" : request.申请标题.Trim();
        var scene = string.IsNullOrWhiteSpace(request.场景类型) ? "回顾性病历分析" : request.场景类型.Trim();

        var schemeSummary = $@"【研究场景】{scene}（简易快审/免除知情同意向导生成）
【研究目的】{(string.IsNullOrWhiteSpace(request.研究目的) ? "回顾性临床数据分析与规律探讨" : request.研究目的.Trim())}
【纳入与排除标准】{(string.IsNullOrWhiteSpace(request.样本纳入与排除标准) ? "符合临床诊断指标者纳入，数据缺失严重者排除" : request.样本纳入与排除标准.Trim())}
【预期样本量】{(string.IsNullOrWhiteSpace(request.预期样本量) ? "约50~200例" : request.预期样本量.Trim())}
【提取指标项】{(string.IsNullOrWhiteSpace(request.观察指标与数据项) ? "常规临床病案指标" : request.观察指标与数据项.Trim())}
【隐私脱敏与保密】{(string.IsNullOrWhiteSpace(request.隐私与去标识化措施) ? "去标识化科研编码，局域网离线统计" : request.隐私与去标识化措施.Trim())}";

        var waiverRation = string.IsNullOrWhiteSpace(request.免除知情同意依据)
            ? "符合《涉及人的生命科学和医学研究伦理审查办法》第三十二条免除知情同意规定，不大于最小风险，申请豁免知情同意签署。"
            : request.免除知情同意依据.Trim();

        var initialStatus = request.直接提交审批 ? "待受理" : "草稿";

        var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""伦理审查申请"" (""流程模板编号"",""项目编号"",""申请人编号"",""申请类型"",""申请标题"",""研究方案摘要"",""受试者风险等级"",""知情同意说明"",""状态"",""提交时间"",""建立人编号"")
VALUES (@流程模板编号,@项目编号,@申请人编号,'初始审查',@申请标题,@研究方案摘要,'低风险',@知情同意说明,@状态,CASE WHEN @直接提交 THEN CURRENT_TIMESTAMP ELSE NULL END,@建立人编号)
RETURNING ""编号"";", new
        {
            request.流程模板编号, request.项目编号, 申请人编号 = CurrentUser.编号,
            申请标题 = title, 研究方案摘要 = schemeSummary,
            知情同意说明 = waiverRation, 状态 = initialStatus,
            直接提交 = request.直接提交审批, 建立人编号 = CurrentUser.编号
        });

        if (request.直接提交审批)
        {
            await 记录伦理审计(id, null, null, null, "简易快审向导一键提交", "草稿", "待受理", new
            {
                场景 = scene,
                受试者风险等级 = "低风险",
                向导生成 = true
            });
        }

        return new
        {
            编号 = id,
            状态 = initialStatus,
            申请标题 = title,
            message = request.直接提交审批 ? "简易伦理快审已成功提交，进入伦理委员会待受理队列" : "简易伦理申请草稿已生成，可继续完善或上传附件"
        };
    }

    /// <summary>将草稿提交到委员会受理队列，提交后申请人不可直接修改。</summary>
    [HttpPost]
    public async Task<object> 提交伦理审查([FromBody] long 申请编号)
    {
        if (申请编号 <= 0) throw new ArgumentException("申请编号无效");
        var application = await 获取申请(申请编号);
        if (application == null) return NotFound(new { error = "伦理申请不存在" });
        if ((int)application.申请人编号 != CurrentUser.编号 && !await 是伦理管理角色()) return StatusCode(403, new { error = "无权提交该伦理申请" });
        var changed = await Db.ExecuteAsync(@"
UPDATE ""伦理审查申请"" SET ""状态""='待受理',""提交时间""=CURRENT_TIMESTAMP,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@申请编号 AND ""状态"" IN ('草稿','需补正');", new { 申请编号 });
        if (changed == 0) return BadRequest(new { error = "申请当前状态不可提交" });
        return new { 编号 = 申请编号, 状态 = "待受理", message = "伦理申请已提交，等待伦理委员会秘书受理" };
    }

    /// <summary>委员会秘书受理或退回补正。</summary>
    [HttpPost]
    public async Task<object> 受理伦理申请([FromBody] 伦理申请受理 model)
    {
        if (!await 是伦理管理角色()) return StatusCode(403, new { error = "仅科技办、科研管理员或科研处负责人可以受理申请" });
        if (model == null || model.申请编号 <= 0 || model.结果 is not ("受理" or "退回补正")) throw new ArgumentException("受理结果无效");
        if (model.结果 == "退回补正" && string.IsNullOrWhiteSpace(model.退回原因)) throw new ArgumentException("退回补正必须填写原因");
        var changed = await Db.ExecuteAsync(@"
UPDATE ""伦理审查申请"" SET ""状态""=@状态,""受理时间""=CASE WHEN @状态='审查中' THEN CURRENT_TIMESTAMP ELSE ""受理时间"" END,
       ""退回原因""=@退回原因,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@申请编号 AND ""状态""='待受理';", new { model.申请编号, 状态 = model.结果 == "受理" ? "审查中" : "需补正", 退回原因 = model.结果 == "退回补正" ? model.退回原因.Trim() : null });
        if (changed == 0) return BadRequest(new { error = "申请不存在或当前不是待受理状态" });
        return new { 编号 = model.申请编号, 状态 = model.结果 == "受理" ? "审查中" : "需补正" };
    }

    /// <summary>伦理委员提交意见；利益冲突时只能选择回避。</summary>
    [HttpPost]
    public async Task<object> 登记伦理委员意见([FromBody] 伦理委员意见保存 model)
    {
        if (!await HasAnyRoleAsync("专家", "伦理委员", "伦理委员会委员", "科技办", "科研科负责人", "科研处处长", "科研管理员", "系统管理员")) return StatusCode(403, new { error = "无权登记伦理委员意见" });
        if (model == null || model.申请编号 <= 0 || model.意见结论 is not ("同意" or "修改后同意" or "不同意" or "回避")) throw new ArgumentException("委员意见结论无效");
        if (model.是否利益冲突 && model.意见结论 != "回避") throw new ArgumentException("存在利益冲突时必须回避，不能提交实质性意见");
        if (!model.会议编号.HasValue || model.会议编号.Value <= 0) throw new ArgumentException("委员意见必须关联已排期的伦理委员会会议");
        var scheduled = await Db.QuerySingleAsync<bool>(@"SELECT EXISTS (SELECT 1 FROM ""伦理审查会议议程"" g JOIN ""伦理审查会议"" m ON m.""编号""=g.""会议编号"" WHERE g.""会议编号""=@会议编号 AND g.""申请编号""=@申请编号 AND g.""状态"" IN ('已排期','已审查') AND m.""状态"" IN ('计划中','进行中'));", new { model.会议编号, model.申请编号 });
        if (!scheduled) throw new ArgumentException("该申请尚未排入有效的伦理委员会会议议程");
        var application = await 获取申请(model.申请编号);
        if (application == null) return NotFound(new { error = "伦理申请不存在" });
        if ((string)application.状态 != "审查中") return BadRequest(new { error = "仅审查中的伦理申请可以登记委员意见" });
        var assignment = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""状态"",""是否回避"",""出席状态"" FROM ""伦理审查会议委员""
 WHERE ""会议编号""=@会议编号 AND ""委员编号""=@委员编号;", new { model.会议编号, 委员编号 = CurrentUser.编号 });
        if (assignment == null) return StatusCode(403, new { error = "委员必须先由委员会正式指派到本次会议" });
        if (model.意见结论 == "回避")
        {
            if ((string)assignment.状态 != "回避" || !(bool)assignment.是否回避)
                return BadRequest(new { error = "请先登记利益冲突回避，再提交回避意见" });
        }
        else if ((string)assignment.状态 != "正式指派" || (bool)assignment.是否回避 || (string)assignment.出席状态 != "出席")
        {
            return BadRequest(new { error = "只有正式指派、已确认出席且未回避的委员可以提交有效意见" });
        }
        await using var connection = new Npgsql.NpgsqlConnection(DbConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
        var changed = await connection.ExecuteAsync(@"
INSERT INTO ""伦理审查委员意见"" (""申请编号"",""会议编号"",""委员编号"",""是否利益冲突"",""意见结论"",""意见内容"",""评分"")
VALUES (@申请编号,@会议编号,@委员编号,@是否利益冲突,@意见结论,@意见内容,@评分)
ON CONFLICT (""申请编号"",""委员编号"") DO UPDATE SET ""会议编号""=EXCLUDED.""会议编号"",""是否利益冲突""=EXCLUDED.""是否利益冲突"",""意见结论""=EXCLUDED.""意见结论"",""意见内容""=EXCLUDED.""意见内容"",""评分""=EXCLUDED.""评分"",""提交时间""=CURRENT_TIMESTAMP;", new
        {
            model.申请编号, model.会议编号, 委员编号 = CurrentUser.编号, model.是否利益冲突, model.意见结论,
            意见内容 = 空值(model.意见内容), model.评分
        }, transaction);
        await 记录伦理审计(connection, transaction, model.申请编号, model.会议编号, null, null, "委员意见", (string)assignment.状态, model.意见结论,
            new { model.是否利益冲突, model.意见结论, model.评分 }, CurrentUser.编号);
        await transaction.CommitAsync();
        return new { success = changed > 0, 申请编号 = model.申请编号, message = "伦理委员意见已保存并纳入审查记录" };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>登记委员会正式决定，并在批准时回写纵向项目伦理状态与有效期。</summary>
    [HttpPost]
    public async Task<object> 登记伦理决定([FromBody] 伦理决定保存 model)
    {
        if (!await 是伦理管理角色()) return StatusCode(403, new { error = "无权登记伦理委员会决定" });
        if (model == null || model.申请编号 <= 0 || model.决定 is not ("批准" or "修改后批准" or "补正后再审" or "拒绝" or "终止")) throw new ArgumentException("伦理决定无效");
        if (string.IsNullOrWhiteSpace(model.决定说明)) throw new ArgumentException("决定说明不能为空");
        if (model.决定 is "批准" or "修改后批准")
        {
            if (string.IsNullOrWhiteSpace(model.批件编号) || !model.有效期至.HasValue) throw new ArgumentException("批准决定必须填写批件编号和有效期");
            if (model.有效期至.Value.Date < DateTime.Today) throw new ArgumentException("伦理批件有效期不能早于当前日期");
        }
        var application = await 获取申请(model.申请编号);
        if (application == null) return NotFound(new { error = "伦理申请不存在" });
        if ((string)application.状态 != "审查中") return BadRequest(new { error = "仅审查中的伦理申请可以登记正式决定" });
        var evidence = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT m.""编号"" AS ""会议编号"",m.""法定人数"",m.""表决规则"",m.""状态"" AS ""会议状态"",m.""纪要状态"",
       m.""主席确认人编号"",m.""主席确认时间"",g.""状态"" AS ""议程状态""
  FROM ""伦理审查会议议程"" g JOIN ""伦理审查会议"" m ON m.""编号""=g.""会议编号""
 WHERE g.""申请编号""=@申请编号 AND g.""状态""='已审查' AND m.""状态""='已完成'
 ORDER BY g.""编号"" DESC LIMIT 1;", new { model.申请编号 });
        if (evidence == null) return BadRequest(new { error = "伦理决定必须基于已完成且议程已审查的委员会会议" });
        if ((string)evidence.纪要状态 is not ("已确认" or "已归档") || evidence.主席确认人编号 == null || evidence.主席确认时间 == null)
            return BadRequest(new { error = "伦理正式决定必须具备已确认会议纪要和主席确认" });
        var vote = await Db.QuerySingleAsync<dynamic>(@"
SELECT COUNT(*) FILTER (WHERE c.""状态""='正式指派' AND NOT c.""是否回避"" AND c.""出席状态""='出席')::integer AS ""出席人数"",
       COUNT(*) FILTER (WHERE c.""状态""='正式指派' AND NOT c.""是否回避"" AND c.""出席状态""='出席' AND o.""意见结论""='同意' AND NOT o.""是否利益冲突"")::integer AS ""同意票"",
       COUNT(*) FILTER (WHERE c.""状态""='正式指派' AND NOT c.""是否回避"" AND c.""出席状态""='出席' AND o.""意见结论""='修改后同意' AND NOT o.""是否利益冲突"")::integer AS ""修改后同意票"",
       COUNT(*) FILTER (WHERE c.""状态""='正式指派' AND NOT c.""是否回避"" AND c.""出席状态""='出席' AND o.""意见结论""='不同意' AND NOT o.""是否利益冲突"")::integer AS ""不同意票"",
       COUNT(*) FILTER (WHERE c.""状态""='正式指派' AND NOT c.""是否回避"" AND c.""出席状态""='出席' AND o.""意见结论"" IN ('同意','修改后同意','不同意') AND NOT o.""是否利益冲突"")::integer AS ""有效意见数"",
       COUNT(*) FILTER (WHERE o.""意见结论""='回避')::integer AS ""回避票""
  FROM ""伦理审查会议委员"" c LEFT JOIN ""伦理审查委员意见"" o ON o.""会议编号""=c.""会议编号"" AND o.""委员编号""=c.""委员编号"" AND o.""申请编号""=@申请编号
 WHERE c.""会议编号""=@会议编号;", new { 申请编号 = model.申请编号, 会议编号 = (long)evidence.会议编号 });
        var quorum = (int)evidence.法定人数;
        if ((int)vote.出席人数 < quorum || (int)vote.有效意见数 < quorum)
            return BadRequest(new { error = $"伦理正式决定未达到法定人数或有效委员意见数（出席 {vote.出席人数}，有效意见 {vote.有效意见数}，法定人数 {quorum}）" });
        var approveVotes = (int)vote.同意票 + (int)vote.修改后同意票;
        if ((model.决定 is "批准" or "修改后批准") && approveVotes <= (int)vote.不同意票)
            return BadRequest(new { error = "批准类伦理决定必须获得有效意见过半数同意票" });
        if ((model.决定 is "拒绝" or "终止") && (int)vote.不同意票 <= approveVotes)
            return BadRequest(new { error = "拒绝或终止类伦理决定必须获得有效意见过半数不同意票" });
        await using var connection = new Npgsql.NpgsqlConnection(DbConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var result = model.决定 switch
            {
                "批准" or "修改后批准" => "已批准",
                "补正后再审" => "需补正",
                "拒绝" => "已拒绝",
                _ => "已撤回"
            };
            var decisionId = await connection.QuerySingleAsync<long>(@"
INSERT INTO ""伦理审查决定"" (""申请编号"",""决定"",""决定说明"",""批件编号"",""批准时间"",""有效期至"",""决定书路径"",""决定人编号"")
VALUES (@申请编号,@决定,@决定说明,@批件编号,@批准时间,@有效期至,@决定书路径,@决定人编号)
RETURNING ""编号"";", new
            {
                model.申请编号, model.决定, 决定说明 = model.决定说明.Trim(), 批件编号 = 空值(model.批件编号),
                批准时间 = model.决定 is "批准" or "修改后批准" ? DateTime.Now : (DateTime?)null,
                有效期至 = model.有效期至?.Date, 决定书路径 = 空值(model.决定书路径), 决定人编号 = CurrentUser.编号
            }, transaction);
            await connection.ExecuteAsync(@"
INSERT INTO ""伦理审查会议表决"" (""会议编号"",""申请编号"",""同意票"",""修改后同意票"",""不同意票"",""回避票"",""有效意见数"",""法定人数"",""表决规则"",""表决结果"",""主席确认人编号"",""主席确认时间"",""建立人编号"")
VALUES (@会议编号,@申请编号,@同意票,@修改后同意票,@不同意票,@回避票,@有效意见数,@法定人数,@表决规则,@表决结果,@主席确认人编号,@主席确认时间,@建立人编号)
ON CONFLICT (""会议编号"",""申请编号"") DO UPDATE SET ""同意票""=EXCLUDED.""同意票"",""修改后同意票""=EXCLUDED.""修改后同意票"",""不同意票""=EXCLUDED.""不同意票"",""回避票""=EXCLUDED.""回避票"",""有效意见数""=EXCLUDED.""有效意见数"",""表决结果""=EXCLUDED.""表决结果"",""建立人编号""=EXCLUDED.""建立人编号"",""建立时间""=CURRENT_TIMESTAMP;", new
            {
                会议编号 = (long)evidence.会议编号, model.申请编号,
                同意票 = (int)vote.同意票, 修改后同意票 = (int)vote.修改后同意票, 不同意票 = (int)vote.不同意票,
                回避票 = (int)vote.回避票, 有效意见数 = (int)vote.有效意见数, 法定人数 = quorum,
                表决规则 = (string)evidence.表决规则, 表决结果 = model.决定,
                主席确认人编号 = (int)evidence.主席确认人编号, 主席确认时间 = (DateTime)evidence.主席确认时间,
                建立人编号 = CurrentUser.编号
            }, transaction);
            await connection.ExecuteAsync(@"
UPDATE ""伦理审查申请"" SET ""状态""=@状态,""决定时间""=CURRENT_TIMESTAMP,""批件编号""=@批件编号,""有效期至""=@有效期至,""批件路径""=@决定书路径,""退回原因""=NULL,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@申请编号;", new
            {
                model.申请编号, 状态 = result, 批件编号 = 空值(model.批件编号), 有效期至 = model.有效期至?.Date,
                决定书路径 = 空值(model.决定书路径)
            }, transaction);
            if ((int)application.流程模板编号 == 纵向立项模板 && result == "已批准")
            {
                await connection.ExecuteAsync(@"
UPDATE ""纵向项目立项"" SET ""是否需要伦理评审""=TRUE,""是否需要伦理批文""=TRUE,""伦理批文编号""=@批件编号,""伦理批文时间""=CURRENT_TIMESTAMP,""伦理批文有效期至""=@有效期至,""伦理批文路径""=@决定书路径,""伦理审查状态""='有效'
 WHERE ""编号""=@项目编号;", new { application.项目编号, 批件编号 = model.批件编号.Trim(), 有效期至 = model.有效期至.Value.Date, 决定书路径 = 空值(model.决定书路径) }, transaction);
            }
            else if ((int)application.流程模板编号 == 横向申请模板 && result == "已批准")
            {
                await connection.ExecuteAsync("UPDATE \"横向项目\" SET \"是否需要伦理评审\"=TRUE WHERE \"编号\"=@项目编号;", new { application.项目编号 }, transaction);
            }
            var decisionAudit = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                model.决定, model.决定说明, 同意票 = (int)vote.同意票, 修改后同意票 = (int)vote.修改后同意票,
                不同意票 = (int)vote.不同意票, 回避票 = (int)vote.回避票, 有效意见数 = (int)vote.有效意见数, 法定人数 = quorum
            });
            await connection.ExecuteAsync(@"
INSERT INTO ""伦理审查治理审计"" (""申请编号"",""会议编号"",""决定编号"",""动作"",""旧状态"",""新状态"",""详情"",""操作人编号"")
VALUES (@申请编号,@会议编号,@决定编号,@动作,@旧状态,@新状态,CAST(@详情 AS jsonb),@操作人编号);", new
            {
                model.申请编号, 会议编号 = (long)evidence.会议编号, 决定编号 = decisionId, 动作 = "正式决定",
                旧状态 = (string)application.状态, 新状态 = result, 详情 = decisionAudit, 操作人编号 = CurrentUser.编号
            }, transaction);
            await transaction.CommitAsync();
            return new { success = true, 编号 = model.申请编号, 状态 = result, message = result == "已批准" ? "伦理决定已登记，项目合规状态已更新" : "伦理决定已登记" };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>提交年度跟踪审查、方案修正案、知情同意修订、不良事件或偏离方案记录。</summary>
    [HttpPost]
    public async Task<object> 提交伦理持续记录([FromBody] 伦理持续记录保存 model)
    {
        校验持续记录(model);
        var application = await 获取申请(model.申请编号);
        if (application == null) return NotFound(new { error = "伦理申请不存在" });
        if ((string)application.状态 != "已批准") return BadRequest(new { error = "仅已批准的伦理申请可以提交持续审查或不良事件记录" });
        if ((int)application.申请人编号 != CurrentUser.编号 && !await 是伦理管理角色()) return StatusCode(403, new { error = "无权提交该项目的持续伦理记录" });
        await using var connection = new Npgsql.NpgsqlConnection(DbConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
        var id = await connection.QuerySingleAsync<long>(@"
INSERT INTO ""伦理持续审查记录"" (""申请编号"",""记录类型"",""发生日期"",""下次审查日期"",""严重程度"",""事件说明"",""处理结论"",""附件路径"",""提交人编号"")
VALUES (@申请编号,@记录类型,@发生日期,@下次审查日期,@严重程度,@事件说明,@处理结论,@附件路径,@提交人编号)
RETURNING ""编号"";", new
        {
            model.申请编号, 记录类型 = model.记录类型.Trim(), 发生日期 = model.发生日期?.Date, 下次审查日期 = model.下次审查日期?.Date,
            严重程度 = 空值(model.严重程度), 事件说明 = model.事件说明.Trim(), 处理结论 = 空值(model.处理结论), 附件路径 = 空值(model.附件路径), 提交人编号 = CurrentUser.编号
        }, transaction);
        await 记录伦理审计(connection, transaction, model.申请编号, null, null, id, "持续记录提交", (string)application.状态, "待审查",
            new { model.记录类型, model.严重程度, model.发生日期 }, CurrentUser.编号);
        await transaction.CommitAsync();
        return new { 编号 = id, 状态 = "待审查", message = "持续伦理记录已提交" };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>委员会审核持续记录；不良事件和方案偏离必须填写处理结论。</summary>
    [HttpPost]
    public async Task<object> 审核伦理持续记录([FromBody] 伦理持续记录审核 model)
    {
        if (!await 是伦理管理角色()) return StatusCode(403, new { error = "无权审核持续伦理记录" });
        if (model == null || model.记录编号 <= 0 || model.结果 is not ("审查中" or "已批准" or "需补正" or "已关闭")) throw new ArgumentException("持续记录审核结果无效");
        if (string.IsNullOrWhiteSpace(model.处理结论)) throw new ArgumentException("持续记录审核必须填写处理结论");
        var followup = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT r.""编号"",r.""申请编号"",a.""状态"" AS ""申请状态""
  FROM ""伦理持续审查记录"" r JOIN ""伦理审查申请"" a ON a.""编号""=r.""申请编号""
 WHERE r.""编号""=@记录编号;", new { model.记录编号 });
        if (followup == null) return NotFound(new { error = "持续记录不存在或未关联伦理申请" });
        if ((string)followup.申请状态 is not ("已批准" or "审查中"))
            return BadRequest(new { error = "仅有效伦理申请的持续记录可以审核" });
        await using var connection = new Npgsql.NpgsqlConnection(DbConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
        var changed = await connection.ExecuteAsync(@"
UPDATE ""伦理持续审查记录"" SET ""状态""=@状态,""处理结论""=COALESCE(@处理结论,""处理结论""),""审核人编号""=@审核人编号,""审核时间""=CURRENT_TIMESTAMP,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@记录编号 AND ""状态"" IN ('待审查','审查中');", new { model.记录编号, 状态 = model.结果, 处理结论 = 空值(model.处理结论), 审核人编号 = CurrentUser.编号 }, transaction);
        if (changed == 0) return NotFound(new { error = "持续记录不存在或当前状态不可审核" });
        await 记录伦理审计(connection, transaction, (long)followup.申请编号, null, null, model.记录编号, "持续记录审核", null, model.结果,
            new { model.结果, model.处理结论 }, CurrentUser.编号);
        await transaction.CommitAsync();
        return new { success = true, 编号 = model.记录编号, 状态 = model.结果 };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>获取申请详情、委员意见、决定和持续审查记录。</summary>
    [HttpGet]
    public async Task<object> 获取伦理申请详情(long 申请编号)
    {
        if (申请编号 <= 0) throw new ArgumentException("申请编号无效");
        var application = await 获取申请(申请编号);
        if (application == null) return NotFound(new { error = "伦理申请不存在" });
        if (!await 有权访问申请(application)) return StatusCode(403, new { error = "无权查看该伦理申请" });
        var manager = await 是伦理管理角色();
        var assignedReviewer = !manager && await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (
    SELECT 1 FROM ""伦理审查会议议程"" g
    JOIN ""伦理审查会议委员"" c ON c.""会议编号""=g.""会议编号""
   WHERE g.""申请编号""=@申请编号 AND c.""委员编号""=@人员编号
     AND c.""状态"" IN ('正式指派','回避')
);", new { 申请编号, 人员编号 = CurrentUser.编号 });
        var opinions = await Db.QueryAsync<object>(@"
SELECT o.""编号"",o.""会议编号"",o.""委员编号"",p.""姓名"" AS ""委员姓名"",o.""是否利益冲突"",o.""意见结论"",o.""意见内容"",o.""评分"",o.""提交时间""
  FROM ""伦理审查委员意见"" o LEFT JOIN ""人员"" p ON p.""编号""=o.""委员编号""
 WHERE o.""申请编号""=@申请编号 AND (@隐藏其他委员意见=false OR o.""委员编号""=@人员编号)
 ORDER BY o.""提交时间"" DESC;", new { 申请编号, 隐藏其他委员意见 = assignedReviewer, 人员编号 = CurrentUser.编号 });
        var decisions = await Db.QueryAsync<object>(@"SELECT * FROM ""伦理审查决定"" WHERE ""申请编号""=@申请编号 ORDER BY ""建立时间"" DESC;", new { 申请编号 });
        var followups = await Db.QueryAsync<object>(@"SELECT * FROM ""伦理持续审查记录"" WHERE ""申请编号""=@申请编号 ORDER BY ""建立时间"" DESC;", new { 申请编号 });
        return new { 申请 = application, 委员意见 = opinions, 决定 = decisions, 持续记录 = followups,
            可见范围 = manager ? "伦理管理" : assignedReviewer ? "被指派委员（最小披露）" : "申请人/项目范围",
            是否委员指派 = assignedReviewer };
    }

    /// <summary>查询有效期临近或已过期的伦理批件及待跟踪审查记录。</summary>
    [HttpGet]
    public async Task<object> 查询伦理到期提醒(int? 提前天数 = null)
    {
        var days = Math.Clamp(提前天数 ?? 60, 0, 365);
        var committee = await 是伦理管理角色();
        var approvals = await Db.QueryAsync<object>(@"
SELECT a.""编号"" AS ""申请编号"",a.""项目编号"",a.""申请标题"",a.""批件编号"",a.""有效期至"",a.""申请人编号"",p.""姓名"" AS ""申请人姓名"",
       CASE WHEN a.""有效期至"" < CURRENT_DATE THEN '已过期' ELSE '即将到期' END AS ""提醒状态"",
       (a.""有效期至"" - CURRENT_DATE) AS ""剩余天数""
  FROM ""伦理审查申请"" a LEFT JOIN ""人员"" p ON p.""编号""=a.""申请人编号""
 WHERE a.""状态""='已批准' AND a.""有效期至"" IS NOT NULL AND a.""有效期至"" <= CURRENT_DATE + @天数
   AND (@管理角色 OR a.""申请人编号""=@人员编号) ORDER BY a.""有效期至"" ASC;", new { 天数 = days, 管理角色 = committee, 人员编号 = CurrentUser.编号 });
        var followups = await Db.QueryAsync<object>(@"
SELECT r.""编号"",r.""申请编号"",r.""记录类型"",r.""下次审查日期"",r.""状态"",a.""申请标题""
  FROM ""伦理持续审查记录"" r JOIN ""伦理审查申请"" a ON a.""编号""=r.""申请编号""
 WHERE r.""下次审查日期"" IS NOT NULL AND r.""下次审查日期"" <= CURRENT_DATE + @天数 AND r.""状态"" IN ('待审查','审查中')
   AND (@管理角色 OR a.""申请人编号""=@人员编号) ORDER BY r.""下次审查日期"" ASC;", new { 天数 = days, 管理角色 = committee, 人员编号 = CurrentUser.编号 });
        return new { 提前天数 = days, 批件提醒 = approvals, 持续审查提醒 = followups };
    }

    /// <summary>返回匿名伦理表决汇总；不返回委员编号、姓名或单票身份。</summary>
    [HttpGet]
    public async Task<object> 获取伦理匿名表决(long 会议编号)
    {
        if (会议编号 <= 0) throw new ArgumentException("会议编号无效");
        var manager = await 是伦理管理角色();
        var assigned = !manager && await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (SELECT 1 FROM ""伦理审查会议委员"" WHERE ""会议编号""=@会议编号 AND ""委员编号""=@人员编号
 AND ""状态"" IN ('正式指派','回避'));", new { 会议编号, 人员编号 = CurrentUser.编号 });
        if (!manager && !assigned) return StatusCode(403, new { error = "仅管理角色或被指派委员可以查看匿名表决汇总" });
        var meeting = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"",""是否匿名表决"" FROM ""伦理审查会议"" WHERE ""编号""=@会议编号;", new { 会议编号 });
        if (meeting == null) return NotFound(new { error = "伦理会议不存在" });
        var votes = await Db.QueryAsync<object>(@"
SELECT g.""申请编号"",a.""申请标题"",
       COUNT(*) FILTER (WHERE o.""意见结论""='同意' AND NOT o.""是否利益冲突"")::integer AS ""同意票"",
       COUNT(*) FILTER (WHERE o.""意见结论""='修改后同意' AND NOT o.""是否利益冲突"")::integer AS ""修改后同意票"",
       COUNT(*) FILTER (WHERE o.""意见结论""='不同意' AND NOT o.""是否利益冲突"")::integer AS ""不同意票"",
       COUNT(*) FILTER (WHERE o.""意见结论""='回避' OR o.""是否利益冲突"")::integer AS ""回避票"",
       COUNT(*) FILTER (WHERE o.""意见结论"" IN ('同意','修改后同意','不同意') AND NOT o.""是否利益冲突"")::integer AS ""有效意见数""
  FROM ""伦理审查会议议程"" g
  JOIN ""伦理审查申请"" a ON a.""编号""=g.""申请编号""
  LEFT JOIN ""伦理审查会议委员"" c ON c.""会议编号""=g.""会议编号"" AND c.""状态""='正式指派' AND NOT c.""是否回避"" AND c.""出席状态""='出席'
  LEFT JOIN ""伦理审查委员意见"" o ON o.""会议编号""=g.""会议编号"" AND o.""申请编号""=g.""申请编号"" AND o.""委员编号""=c.""委员编号""
 WHERE g.""会议编号""=@会议编号 AND g.""状态""<>'已撤回'
 GROUP BY g.""申请编号"",a.""申请标题"" ORDER BY g.""申请编号"";", new { 会议编号 });
        return new { 会议编号, 是否匿名表决 = (bool)meeting.是否匿名表决, 匿名 = true, 表决 = votes,
            message = "仅返回汇总票数，不返回委员身份" };
    }

    /// <summary>提供统计看板和移动端使用的伦理持续记录、严重不良事件和逾期汇总。</summary>
    [HttpGet]
    public async Task<object> 获取伦理持续记录汇总()
    {
        if (!await 是伦理管理角色()) return StatusCode(403, new { error = "无权查看伦理持续记录汇总" });
        var summary = await Db.QuerySingleAsync<dynamic>(@"
SELECT COUNT(*)::integer AS ""总记录数"",
       COUNT(*) FILTER (WHERE ""记录类型""='不良事件' AND ""严重程度"" IN ('严重','危及生命'))::integer AS ""严重不良事件数"",
       COUNT(*) FILTER (WHERE ""记录类型""='不良事件' AND ""状态"" IN ('待审查','审查中'))::integer AS ""待处理不良事件数"",
       COUNT(*) FILTER (WHERE ""下次审查日期"" < CURRENT_DATE AND ""状态"" IN ('待审查','审查中'))::integer AS ""逾期持续审查数"",
       COUNT(*) FILTER (WHERE ""记录类型""='方案修正案')::integer AS ""修正案数""
  FROM ""伦理持续审查记录"";");
        var severe = await Db.QueryAsync<object>(@"
SELECT r.""编号"",r.""申请编号"",a.""申请标题"",r.""发生日期"",r.""严重程度"",r.""状态"",r.""处理结论""
  FROM ""伦理持续审查记录"" r JOIN ""伦理审查申请"" a ON a.""编号""=r.""申请编号""
 WHERE r.""记录类型""='不良事件' AND r.""严重程度"" IN ('严重','危及生命')
 ORDER BY r.""发生日期"" DESC NULLS LAST,r.""编号"" DESC LIMIT 20;");
        return new { 汇总 = summary, 严重事件 = severe, 生成时间 = DateTime.Now };
    }

    /// <summary>查询伦理委员会会议排期、议程数量和纪要状态。</summary>
    [HttpGet]
    public async Task<object> 查询伦理会议(string 状态 = null, DateTime? 开始时间 = null, DateTime? 结束时间 = null)
    {
        if (!await 是伦理管理角色()) return StatusCode(403, new { error = "仅伦理委员会管理角色可以查询会议排期" });
        if (!string.IsNullOrWhiteSpace(状态) && 状态.Trim() is not ("计划中" or "进行中" or "已完成" or "已取消")) throw new ArgumentException("会议状态无效");
        return await Db.QueryAsync<object>(@"
SELECT m.""编号"",m.""会议序号"",m.""会议时间"",m.""会议地点"",m.""主持人编号"",p.""姓名"" AS ""主持人姓名"",
       m.""状态"",m.""议程说明"",m.""议程路径"",m.""会议纪要路径"",m.""纪要状态"",m.""纪要提交时间"",m.""纪要确认时间"",m.""是否匿名表决"",m.""备注"",
       COUNT(g.""编号"")::integer AS ""议程数量""
  FROM ""伦理审查会议"" m
  LEFT JOIN ""人员"" p ON p.""编号""=m.""主持人编号""
  LEFT JOIN ""伦理审查会议议程"" g ON g.""会议编号""=m.""编号"" AND g.""状态""='已排期'
 WHERE (CAST(@状态 AS varchar) IS NULL OR m.""状态""=CAST(@状态 AS varchar))
   AND (CAST(@开始时间 AS timestamp without time zone) IS NULL OR m.""会议时间"">=CAST(@开始时间 AS timestamp without time zone))
   AND (CAST(@结束时间 AS timestamp without time zone) IS NULL OR m.""会议时间""<CAST(@结束时间 AS timestamp without time zone) + INTERVAL '1 day')
 GROUP BY m.""编号"",p.""姓名""
 ORDER BY m.""会议时间"" DESC,m.""编号"" DESC;", new { 状态 = 空值(状态), 开始时间, 结束时间 });
    }

    /// <summary>新建或调整伦理委员会会议排期。会议序号是可审计的业务编号。</summary>
    [HttpPost]
    public async Task<object> 保存伦理会议([FromBody] 伦理会议保存 model)
    {
        if (!await 是伦理管理角色()) return StatusCode(403, new { error = "仅伦理委员会管理角色可以维护会议排期" });
        if (model == null || string.IsNullOrWhiteSpace(model.会议序号) || model.会议时间 == default) throw new ArgumentException("会议序号和会议时间均为必填项");
        var status = string.IsNullOrWhiteSpace(model.状态) ? "计划中" : model.状态.Trim();
        if (status is not ("计划中" or "进行中" or "已完成" or "已取消")) throw new ArgumentException("会议状态无效");
        if (model.法定人数 <= 0 || model.法定人数 > 99) throw new ArgumentException("法定人数必须在 1 到 99 之间");
        var rule = string.IsNullOrWhiteSpace(model.表决规则) ? "有效意见过半数" : model.表决规则.Trim();
        if (rule != "有效意见过半数") throw new ArgumentException("当前仅支持有效意见过半数表决规则");
        if (model.编号.HasValue)
        {
            var existingMeeting = await Db.QuerySingleOrDefaultAsync<dynamic>("SELECT \"状态\" FROM \"伦理审查会议\" WHERE \"编号\"=@编号;", new { 编号 = model.编号.Value });
            if (existingMeeting == null) return NotFound(new { error = "伦理会议不存在" });
            if ((string)existingMeeting.状态 is "已完成" or "已取消")
                return BadRequest(new { error = "已完成或已取消的会议不可直接编辑，请通过受控更正流程留痕" });
        }
        if (status == "已完成")
        {
            var ready = await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (
  SELECT 1 FROM ""伦理审查会议纪要"" n
   WHERE n.""会议编号""=@会议编号 AND n.""状态"" IN ('已确认','已归档')
) AND EXISTS (
  SELECT 1 FROM ""伦理审查会议议程"" g
   WHERE g.""会议编号""=@会议编号 AND g.""状态""='已审查'
);", new { 会议编号 = model.编号 ?? 0 });
            if (!ready) return BadRequest(new { error = "会议必须先完成议程审查并确认会议纪要，才能标记为已完成" });
        }
        if (model.编号.HasValue)
        {
            var changed = await Db.ExecuteAsync(@"
UPDATE ""伦理审查会议"" SET ""会议序号""=@会议序号,""会议时间""=@会议时间,""会议地点""=@会议地点,
       ""主持人编号""=@主持人编号,""状态""=@状态,""法定人数""=@法定人数,""表决规则""=@表决规则,""是否匿名表决""=@是否匿名表决,
       ""议程说明""=@议程说明,""议程路径""=@议程路径,""备注""=@备注,
       ""主席确认人编号""=NULL,""主席确认时间""=NULL,""主席确认说明""=NULL
 WHERE ""编号""=@编号;", new
            {
                编号 = model.编号.Value, 会议序号 = model.会议序号.Trim(), model.会议时间,
                会议地点 = 空值(model.会议地点), model.主持人编号, 状态 = status,
                model.法定人数, 表决规则 = rule, model.是否匿名表决, 议程说明 = 空值(model.议程说明), 议程路径 = 空值(model.议程路径), 备注 = 空值(model.备注)
            });
            if (changed == 0) return NotFound(new { error = "伦理会议不存在" });
            return new { 编号 = model.编号.Value, 状态 = status, message = "伦理会议排期已更新" };
        }
        var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""伦理审查会议"" (""会议序号"",""会议时间"",""会议地点"",""主持人编号"",""状态"",""法定人数"",""表决规则"",""是否匿名表决"",""议程说明"",""议程路径"",""备注"",""建立人编号"")
VALUES (@会议序号,@会议时间,@会议地点,@主持人编号,@状态,@法定人数,@表决规则,@是否匿名表决,@议程说明,@议程路径,@备注,@建立人编号)
RETURNING ""编号"";", new
        {
            会议序号 = model.会议序号.Trim(), model.会议时间, 会议地点 = 空值(model.会议地点), model.主持人编号,
            状态 = status, model.法定人数, 表决规则 = rule, model.是否匿名表决, 议程说明 = 空值(model.议程说明), 议程路径 = 空值(model.议程路径), 备注 = 空值(model.备注), 建立人编号 = CurrentUser.编号
        });
        return new { 编号 = id, 状态 = status, message = "伦理委员会会议已排期" };
    }

    /// <summary>将已受理的伦理申请加入会议议程，或将议程标记为撤回。</summary>
    [HttpPost]
    public async Task<object> 维护伦理会议议程([FromBody] 伦理会议议程保存 model)
    {
        if (!await 是伦理管理角色()) return StatusCode(403, new { error = "仅伦理委员会管理角色可以维护会议议程" });
        if (model == null || model.会议编号 <= 0 || model.申请编号 <= 0) throw new ArgumentException("会议编号和申请编号均为必填项");
        var agendaState = string.IsNullOrWhiteSpace(model.状态) ? "已排期" : model.状态.Trim();
        if (agendaState is not ("已排期" or "已审查" or "已撤回")) throw new ArgumentException("议程状态无效");
        if (model.排序号 <= 0) throw new ArgumentException("议程排序号必须大于零");
        var meeting = await Db.QuerySingleOrDefaultAsync<dynamic>("SELECT \"编号\",\"状态\" FROM \"伦理审查会议\" WHERE \"编号\"=@编号;", new { 编号 = model.会议编号 });
        if (meeting == null) return NotFound(new { error = "伦理会议不存在" });
        if ((string)meeting.状态 is "已完成" or "已取消") return BadRequest(new { error = "已完成或已取消的会议不能调整议程" });
        var application = await 获取申请(model.申请编号);
        if (application == null) return NotFound(new { error = "伦理申请不存在" });
        if ((string)application.状态 != "审查中") return BadRequest(new { error = "只有审查中的伦理申请可以排入委员会议程" });
        await Db.ExecuteAsync(@"
INSERT INTO ""伦理审查会议议程"" (""会议编号"",""申请编号"",""排序号"",""议题说明"",""状态"",""建立人编号"")
VALUES (@会议编号,@申请编号,@排序号,@议题说明,@状态,@建立人编号)
ON CONFLICT (""会议编号"",""申请编号"") DO UPDATE SET ""排序号""=EXCLUDED.""排序号"",""议题说明""=EXCLUDED.""议题说明"",""状态""=EXCLUDED.""状态"";", new
        {
            model.会议编号, model.申请编号, model.排序号, 议题说明 = 空值(model.议题说明), 状态 = agendaState, 建立人编号 = CurrentUser.编号
        });
        return new { success = true, 会议编号 = model.会议编号, 申请编号 = model.申请编号, 状态 = agendaState };
    }

    /// <summary>登记委员会正式委员、回避和出席状态。普通委员只能维护自己的出席/回避信息。</summary>
    [HttpPost]
    public async Task<object> 维护伦理会议委员([FromBody] 伦理会议委员保存 model)
    {
        var manager = await 是伦理委员会管理角色();
        if (!manager && (model == null || model.委员编号 != CurrentUser.编号))
            return StatusCode(403, new { error = "仅伦理委员会管理角色可以指派其他委员，委员只能维护自己的出席或回避信息" });
        if (model == null || model.会议编号 <= 0 || model.委员编号 <= 0) throw new ArgumentException("会议编号和委员编号均为必填项");
        var role = string.IsNullOrWhiteSpace(model.委员角色) ? "委员" : model.委员角色.Trim();
        var status = string.IsNullOrWhiteSpace(model.状态) ? "正式指派" : model.状态.Trim();
        var attendance = string.IsNullOrWhiteSpace(model.出席状态) ? "未确认" : model.出席状态.Trim();
        if (role is not ("主席" or "委员" or "秘书")) throw new ArgumentException("委员角色无效");
        if (status is not ("正式指派" or "回避" or "替补" or "已撤销")) throw new ArgumentException("委员指派状态无效");
        if (attendance is not ("未确认" or "出席" or "缺席" or "请假")) throw new ArgumentException("委员出席状态无效");
        if (status == "回避" && (string.IsNullOrWhiteSpace(model.回避原因) || !model.是否回避))
            throw new ArgumentException("回避必须标记利益冲突并填写回避原因");
        if (status != "回避" && model.是否回避) throw new ArgumentException("标记利益冲突时委员状态必须为回避");
        var meeting = await Db.QuerySingleOrDefaultAsync<dynamic>("SELECT \"编号\",\"状态\" FROM \"伦理审查会议\" WHERE \"编号\"=@会议编号;", new { model.会议编号 });
        if (meeting == null) return NotFound(new { error = "伦理会议不存在" });
        if ((string)meeting.状态 is "已完成" or "已取消") return BadRequest(new { error = "已完成或已取消的会议不能调整委员指派" });
        var existing = await Db.QuerySingleOrDefaultAsync<dynamic>("SELECT \"状态\",\"委员角色\",\"是否回避\",\"出席状态\" FROM \"伦理审查会议委员\" WHERE \"会议编号\"=@会议编号 AND \"委员编号\"=@委员编号;", new { model.会议编号, model.委员编号 });
        if (!manager && existing == null) return StatusCode(403, new { error = "委员尚未被正式指派，不能自行登记状态" });
        if (!manager)
        {
            if ((string)existing.状态 != "正式指派" || (bool)existing.是否回避)
                return StatusCode(403, new { error = "回避或撤销后的委员不能自行恢复指派状态" });
            if (status is not ("回避" or "正式指派")) return StatusCode(403, new { error = "委员只能登记正式指派、回避或出席状态" });
        }
        await using var connection = new Npgsql.NpgsqlConnection(DbConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
        await connection.ExecuteAsync(@"
INSERT INTO ""伦理审查会议委员"" (""会议编号"",""委员编号"",""委员角色"",""状态"",""是否回避"",""回避原因"",""出席状态"",""指派人编号"",""回避人编号"",""回避时间"",""出席确认人编号"",""出席确认时间"")
VALUES (@会议编号,@委员编号,@委员角色,@状态,@是否回避,@回避原因,@出席状态,@指派人编号,
        CASE WHEN @是否回避 THEN @操作人编号 ELSE NULL END,
        CASE WHEN @是否回避 THEN CURRENT_TIMESTAMP ELSE NULL END,
        CASE WHEN @出席状态 <> '未确认' THEN @操作人编号 ELSE NULL END,
        CASE WHEN @出席状态 <> '未确认' THEN CURRENT_TIMESTAMP ELSE NULL END)
ON CONFLICT (""会议编号"",""委员编号"") DO UPDATE SET
 ""委员角色""=CASE WHEN @管理角色 THEN EXCLUDED.""委员角色"" ELSE ""伦理审查会议委员"".""委员角色"" END,
 ""状态""=EXCLUDED.""状态"",""是否回避""=EXCLUDED.""是否回避"",""回避原因""=EXCLUDED.""回避原因"",
 ""回避人编号""=EXCLUDED.""回避人编号"",""回避时间""=EXCLUDED.""回避时间"",""出席状态""=EXCLUDED.""出席状态"",
 ""出席确认人编号""=EXCLUDED.""出席确认人编号"",""出席确认时间""=EXCLUDED.""出席确认时间"",""更新时间""=CURRENT_TIMESTAMP;", new
        {
            model.会议编号, model.委员编号, 委员角色 = role, 状态 = status, 是否回避 = status == "回避",
            回避原因 = status == "回避" ? model.回避原因.Trim() : null, 出席状态 = attendance,
            指派人编号 = CurrentUser.编号, 操作人编号 = CurrentUser.编号, 管理角色 = manager
        }, transaction);
        var oldAssignmentStatus = existing == null ? null : (string)existing.状态;
        await 记录伦理审计(connection, transaction, null, model.会议编号, null, null, status == "回避" ? "委员回避" : "委员指派/出席", oldAssignmentStatus, status,
            new { model.委员编号, 委员角色 = role, 出席状态 = attendance, model.是否回避, model.回避原因 }, CurrentUser.编号);
        await transaction.CommitAsync();
        return new { success = true, model.会议编号, model.委员编号, 委员角色 = role, 状态 = status, 出席状态 = attendance };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>由会议主席在纪要确认后确认正式表决资格。</summary>
    [HttpPost]
    public async Task<object> 确认伦理会议主席([FromBody] 伦理会议主席确认 model)
    {
        if (model == null || model.会议编号 <= 0) throw new ArgumentException("会议编号无效");
        var meeting = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"",""状态"",""主持人编号"",""纪要状态"" FROM ""伦理审查会议"" WHERE ""编号""=@会议编号;", new { model.会议编号 });
        if (meeting == null) return NotFound(new { error = "伦理会议不存在" });
        if ((int?)meeting.主持人编号 != CurrentUser.编号 && !await HasAnyRoleAsync("系统管理员"))
            return StatusCode(403, new { error = "只有会议主席可以确认正式表决资格" });
        if ((string)meeting.状态 != "已完成") return BadRequest(new { error = "会议必须先标记为已完成" });
        if ((string)meeting.纪要状态 is not ("已确认" or "已归档")) return BadRequest(new { error = "会议纪要必须先由委员会确认" });
        var agenda = await Db.QuerySingleAsync<dynamic>(@"
SELECT COUNT(*)::integer AS ""总数"", COUNT(*) FILTER (WHERE ""状态""='已审查')::integer AS ""已审查数""
  FROM ""伦理审查会议议程"" WHERE ""会议编号""=@会议编号 AND ""状态""<>'已撤回';", new { model.会议编号 });
        if ((int)agenda.总数 == 0 || (int)agenda.总数 != (int)agenda.已审查数)
            return BadRequest(new { error = "会议所有有效议程必须完成审查后才能由主席确认" });
        await using var connection = new Npgsql.NpgsqlConnection(DbConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
        var changed = await connection.ExecuteAsync(@"
UPDATE ""伦理审查会议"" SET ""主席确认人编号""=@确认人编号,""主席确认时间""=CURRENT_TIMESTAMP,""主席确认说明""=@主席确认说明
 WHERE ""编号""=@会议编号 AND ""状态""='已完成';", new { model.会议编号, 确认人编号 = CurrentUser.编号, 主席确认说明 = 空值(model.确认说明) }, transaction);
        await 记录伦理审计(connection, transaction, null, model.会议编号, null, null, "主席确认", null, "已确认", new { model.确认说明 }, CurrentUser.编号);
        await transaction.CommitAsync();
        return new { success = changed > 0, model.会议编号, 状态 = "已确认", message = "伦理委员会主席已确认正式表决资格" };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>查询会议的申请议程、委员意见、会议纪要和归档记录。</summary>
    [HttpGet]
    public async Task<object> 获取伦理会议详情(long 会议编号)
    {
        if (会议编号 <= 0) throw new ArgumentException("会议编号无效");
        var manager = await 是伦理管理角色();
        var assignedReviewer = !manager && await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (SELECT 1 FROM ""伦理审查会议委员"" WHERE ""会议编号""=@会议编号 AND ""委员编号""=@人员编号
 AND ""状态"" IN ('正式指派','回避'));", new { 会议编号, 人员编号 = CurrentUser.编号 });
        if (!manager && !assignedReviewer) return StatusCode(403, new { error = "仅管理角色或被正式指派的委员可以查看会议详情" });
        var meeting = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT m.*,p.""姓名"" AS ""主持人姓名"" FROM ""伦理审查会议"" m LEFT JOIN ""人员"" p ON p.""编号""=m.""主持人编号"" WHERE m.""编号""=@会议编号;", new { 会议编号 });
        if (meeting == null) return NotFound(new { error = "伦理会议不存在" });
        var agenda = await Db.QueryAsync<object>(@"
SELECT g.*,a.""申请标题"",a.""申请类型"",a.""状态"" AS ""申请状态"",p.""姓名"" AS ""申请人姓名""
  FROM ""伦理审查会议议程"" g JOIN ""伦理审查申请"" a ON a.""编号""=g.""申请编号""
  LEFT JOIN ""人员"" p ON p.""编号""=a.""申请人编号""
 WHERE g.""会议编号""=@会议编号 AND (@管理角色 OR EXISTS (
       SELECT 1 FROM ""伦理审查会议委员"" c
        WHERE c.""会议编号""=g.""会议编号"" AND c.""委员编号""=@人员编号
          AND c.""状态"" IN ('正式指派','回避')
   )) ORDER BY g.""排序号"",g.""编号"";", new { 会议编号, 管理角色 = manager, 人员编号 = CurrentUser.编号 });
        var minutes = await Db.QuerySingleOrDefaultAsync<object>("SELECT * FROM \"伦理审查会议纪要\" WHERE \"会议编号\"=@会议编号;", new { 会议编号 });
        var archives = await Db.QueryAsync<object>("SELECT * FROM \"伦理电子档案\" WHERE \"会议编号\"=@会议编号 ORDER BY \"建立时间\" DESC;", new { 会议编号 });
        return new { 会议 = meeting, 议程 = agenda, 纪要 = minutes, 电子档案 = archives,
            可见范围 = manager ? "伦理管理" : "被指派委员（最小披露）", 是否委员指派 = assignedReviewer };
    }

    /// <summary>保存会议纪要草稿或提交待确认；确认必须使用单独接口。</summary>
    [HttpPost]
    public async Task<object> 保存伦理会议纪要([FromBody] 伦理会议纪要保存 model)
    {
        if (!await 是伦理管理角色()) return StatusCode(403, new { error = "仅伦理委员会管理角色可以维护会议纪要" });
        if (model == null || model.会议编号 <= 0 || string.IsNullOrWhiteSpace(model.纪要正文)) throw new ArgumentException("会议编号和纪要正文均为必填项");
        var status = string.IsNullOrWhiteSpace(model.状态) ? "草稿" : model.状态.Trim();
        if (status is not ("草稿" or "待确认")) throw new ArgumentException("会议纪要只能保存为草稿或待确认");
        var meeting = await Db.QuerySingleOrDefaultAsync<dynamic>("SELECT \"编号\",\"状态\" FROM \"伦理审查会议\" WHERE \"编号\"=@编号;", new { 编号 = model.会议编号 });
        if (meeting == null) return NotFound(new { error = "伦理会议不存在" });
        if ((string)meeting.状态 == "已取消") return BadRequest(new { error = "已取消的会议不能提交纪要" });
        var existing = await Db.QuerySingleOrDefaultAsync<dynamic>("SELECT \"状态\" FROM \"伦理审查会议纪要\" WHERE \"会议编号\"=@会议编号;", new { model.会议编号 });
        if (existing != null && (string)existing.状态 is "已确认" or "已归档") return BadRequest(new { error = "已确认或已归档的会议纪要不可覆盖" });
        await using var connection = new Npgsql.NpgsqlConnection(DbConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
        await connection.ExecuteAsync(@"
INSERT INTO ""伦理审查会议纪要"" (""会议编号"",""纪要正文"",""出席委员摘要"",""决议摘要"",""附件路径"",""状态"",""记录人编号"")
VALUES (@会议编号,@纪要正文,@出席委员摘要,@决议摘要,@附件路径,@状态,@记录人编号)
ON CONFLICT (""会议编号"") DO UPDATE SET ""纪要正文""=EXCLUDED.""纪要正文"",""出席委员摘要""=EXCLUDED.""出席委员摘要"",""决议摘要""=EXCLUDED.""决议摘要"",""附件路径""=EXCLUDED.""附件路径"",""状态""=EXCLUDED.""状态"",""更新时间""=CURRENT_TIMESTAMP,""确认人编号""=NULL,""确认时间""=NULL; 
UPDATE ""伦理审查会议"" SET ""纪要状态""=@状态,""纪要提交时间""=CURRENT_TIMESTAMP,""纪要确认时间""=NULL,""纪要确认人编号""=NULL WHERE ""编号""=@会议编号;", new
        {
            model.会议编号, 纪要正文 = model.纪要正文.Trim(), 出席委员摘要 = 空值(model.出席委员摘要), 决议摘要 = 空值(model.决议摘要),
            附件路径 = 空值(model.附件路径), 状态 = status, 记录人编号 = CurrentUser.编号
        }, transaction);
        await 记录伦理审计(connection, transaction, null, model.会议编号, null, null, "会议纪要", null, status,
            new { model.会议编号, 状态 = status }, CurrentUser.编号);
        await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        return new { success = true, 会议编号 = model.会议编号, 状态 = status, message = status == "待确认" ? "会议纪要已提交确认" : "会议纪要草稿已保存" };
    }

    /// <summary>确认会议纪要，确认后只允许通过正式档案更正流程留痕。</summary>
    [HttpPost]
    public async Task<object> 确认伦理会议纪要([FromBody] long 会议编号)
    {
        if (!await 是伦理管理角色()) return StatusCode(403, new { error = "仅伦理委员会管理角色可以确认会议纪要" });
        if (会议编号 <= 0) throw new ArgumentException("会议编号无效");
        await using var connection = new Npgsql.NpgsqlConnection(DbConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
        var changed = await connection.ExecuteAsync(@"
UPDATE ""伦理审查会议纪要"" SET ""状态""='已确认',""确认人编号""=@确认人编号,""确认时间""=CURRENT_TIMESTAMP,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""会议编号""=@会议编号 AND ""状态""='待确认';
UPDATE ""伦理审查会议"" SET ""纪要状态""='已确认',""纪要确认人编号""=@确认人编号,""纪要确认时间""=CURRENT_TIMESTAMP WHERE ""编号""=@会议编号 AND ""纪要状态""='待确认';", new { 会议编号, 确认人编号 = CurrentUser.编号 }, transaction);
        if (changed < 1) return BadRequest(new { error = "会议不存在或纪要当前不是待确认状态" });
        await 记录伦理审计(connection, transaction, null, 会议编号, null, null, "会议纪要确认", "待确认", "已确认", new { 会议编号 }, CurrentUser.编号);
        await transaction.CommitAsync();
        return new { success = true, 会议编号, 状态 = "已确认", message = "会议纪要已确认" };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>查询正式伦理电子档案。普通人员只能查看自己项目的档案。</summary>
    [HttpGet]
    public async Task<object> 查询伦理电子档案(long? 申请编号 = null, long? 会议编号 = null, string 档案类型 = null, string 状态 = null)
    {
        var manager = await 是伦理管理角色();
        return await Db.QueryAsync<object>(@"
SELECT e.*,a.""申请标题"",a.""申请人编号"",m.""会议序号""
  FROM ""伦理电子档案"" e
  LEFT JOIN ""伦理审查申请"" a ON a.""编号""=e.""申请编号""
  LEFT JOIN ""伦理审查会议"" m ON m.""编号""=e.""会议编号""
 WHERE (@申请编号 IS NULL OR e.""申请编号""=@申请编号)
   AND (@会议编号 IS NULL OR e.""会议编号""=@会议编号)
   AND (@档案类型 IS NULL OR e.""档案类型""=@档案类型)
   AND (@状态 IS NULL OR e.""状态""=@状态)
   AND (@管理角色 OR a.""申请人编号""=@人员编号 OR EXISTS (
       SELECT 1 FROM ""伦理审查会议议程"" g
       JOIN ""伦理审查会议委员"" c ON c.""会议编号""=g.""会议编号""
      WHERE g.""申请编号""=e.""申请编号"" AND c.""委员编号""=@人员编号
        AND c.""状态"" IN ('正式指派','回避')
   ) OR EXISTS (
       SELECT 1 FROM ""伦理审查会议委员"" c
      WHERE c.""会议编号""=e.""会议编号"" AND c.""委员编号""=@人员编号
        AND c.""状态"" IN ('正式指派','回避')
   ))
 ORDER BY e.""建立时间"" DESC,e.""编号"" DESC;", new { 申请编号, 会议编号, 档案类型 = 空值(档案类型), 状态 = 空值(状态), 管理角色 = manager, 人员编号 = CurrentUser.编号 });
    }

    /// <summary>登记并立即归档受控文件路径，记录申请、会议或决定关联。</summary>
    [HttpPost]
    public async Task<object> 归档伦理电子材料([FromBody] 伦理电子档案保存 model)
    {
        if (!await 是伦理管理角色()) return StatusCode(403, new { error = "仅伦理委员会管理角色可以归档电子材料" });
        if (model == null || string.IsNullOrWhiteSpace(model.档案类型) || string.IsNullOrWhiteSpace(model.文件路径)) throw new ArgumentException("档案类型和文件路径均为必填项");
        if (!model.申请编号.HasValue && !model.会议编号.HasValue && !model.决定编号.HasValue) throw new ArgumentException("电子档案必须关联申请、会议或决定");
        var archiveType = model.档案类型.Trim();
        if (archiveType is not ("申请材料" or "会议议程" or "会议纪要" or "决定书" or "持续审查" or "不良事件" or "修正案" or "其他")) throw new ArgumentException("档案类型无效");
        var archivePath = model.文件路径.Trim();
        if (archivePath.Length > 2000) throw new ArgumentException("电子档案路径超出限制");
        if (System.Text.RegularExpressions.Regex.IsMatch(archivePath, @"(^|[/\\])(demo|sample|模拟|样例)([/\\]|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            throw new ArgumentException("电子档案不能使用示例或演示文件路径");
        var checksum = 空值(model.文件校验值);
        if (checksum != null && !System.Text.RegularExpressions.Regex.IsMatch(checksum, "^[0-9a-fA-F]{64}$"))
            throw new ArgumentException("文件校验值必须是64位SHA-256十六进制摘要");
        if (archiveType is "会议议程" or "会议纪要" or "决定书" && checksum == null)
            throw new ArgumentException("正式伦理档案必须填写64位SHA-256文件校验值");
        if (checksum == null) throw new ArgumentException("伦理电子档案必须填写文件SHA-256校验值");
        var fileEvidence = await 校验受控伦理文件(archivePath, checksum);
        if (archiveType == "决定书" && !model.决定编号.HasValue) throw new ArgumentException("决定书档案必须关联伦理委员会决定");
        if ((archiveType is "会议议程" or "会议纪要") && !model.会议编号.HasValue) throw new ArgumentException("会议档案必须关联伦理委员会会议");
        if ((archiveType is "申请材料" or "持续审查" or "不良事件" or "修正案") && !model.申请编号.HasValue) throw new ArgumentException("申请类档案必须关联伦理审查申请");
        if (model.会议编号.HasValue && archiveType == "会议纪要")
        {
            var confirmed = await Db.QuerySingleAsync<bool>("SELECT EXISTS (SELECT 1 FROM \"伦理审查会议纪要\" WHERE \"会议编号\"=@会议编号 AND \"状态\" IN ('已确认','已归档'));", new { model.会议编号 });
            if (!confirmed) throw new ArgumentException("会议纪要必须先确认后才能归档");
        }
        await using var connection = new Npgsql.NpgsqlConnection(DbConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
        var id = await connection.QuerySingleAsync<long>(@"
INSERT INTO ""伦理电子档案"" (""申请编号"",""会议编号"",""决定编号"",""档案类型"",""文件名称"",""文件路径"",""文件校验值"",""状态"",""实际校验状态"",""校验时间"",""文件大小"",""内容类型"",""校验错误"",""备注"",""归档人编号"",""归档时间"",""建立人编号"")
VALUES (@申请编号,@会议编号,@决定编号,@档案类型,@文件名称,@文件路径,@文件校验值,'已归档','已核验',CURRENT_TIMESTAMP,@文件大小,@内容类型,NULL,@备注,@归档人编号,CURRENT_TIMESTAMP,@建立人编号)
RETURNING ""编号"";", new
        {
            model.申请编号, model.会议编号, model.决定编号, 档案类型 = archiveType, 文件名称 = 空值(model.文件名称),
            文件路径 = archivePath, 文件校验值 = checksum, 文件大小 = fileEvidence.Size, 内容类型 = fileEvidence.ContentType,
            备注 = 空值(model.备注), 归档人编号 = CurrentUser.编号, 建立人编号 = CurrentUser.编号
        }, transaction);
        if (model.会议编号.HasValue && archiveType == "会议纪要")
        {
            await connection.ExecuteAsync(@"
UPDATE ""伦理审查会议纪要"" SET ""状态""='已归档',""更新时间""=CURRENT_TIMESTAMP
 WHERE ""会议编号""=@会议编号 AND ""状态""='已确认';
UPDATE ""伦理审查会议"" SET ""纪要状态""='已归档' WHERE ""编号""=@会议编号 AND ""纪要状态""='已确认';", new { model.会议编号 }, transaction);
            
        }
        await 记录伦理审计(connection, transaction, model.申请编号, model.会议编号, model.决定编号, null,
            "电子档案归档", null, "已归档", new { archiveType, archivePath, fileEvidence.Size, fileEvidence.ContentType }, CurrentUser.编号);
        await transaction.CommitAsync();
        return new { success = true, 编号 = id, 状态 = "已归档", 实际校验状态 = "已核验", 文件大小 = fileEvidence.Size,
            内容类型 = fileEvidence.ContentType, message = "伦理电子材料已完成服务端文件校验并归档" };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<(long Size, string ContentType)> 校验受控伦理文件(string archivePath, string expectedChecksum)
    {
        var storageProvider = config.GetValue<string>("EthicsArchive:Provider");
        if (!string.IsNullOrWhiteSpace(storageProvider) && !string.Equals(storageProvider, "Local", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("伦理档案对象存储适配器尚未启用，当前只允许受控本地对象存储");
        var root = config.GetValue<string>("EthicsArchive:LocalRoot");
        if (string.IsNullOrWhiteSpace(root)) root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var fullRoot = Path.GetFullPath(root);
        var relative = archivePath.Trim().Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(fullRoot, relative));
        var rootPrefix = fullRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("伦理档案路径超出受控文件根目录");
        var requireExisting = config.GetValue("EthicsArchive:RequireExistingObject", true);
        if (requireExisting && !System.IO.File.Exists(fullPath)) throw new ArgumentException("受控伦理档案对象不存在，未执行归档");
        var info = new FileInfo(fullPath);
        var maxBytes = config.GetValue<long>("EthicsArchive:MaxFileBytes", config.GetValue<long>("uploadFileMaxSize", 10485760));
        if (info.Length <= 0) throw new ArgumentException("受控伦理档案不能是空文件");
        if (maxBytes > 0 && info.Length > maxBytes) throw new ArgumentException("受控伦理档案超过允许大小");
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fullPath, out var contentType)) contentType = "application/octet-stream";
        await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream));
        if (!string.Equals(hash, expectedChecksum.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("受控伦理档案SHA-256校验不一致，未执行归档");
        return (info.Length, contentType);
    }

    private async Task<dynamic> 获取申请(long id) => await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT ""编号"",""流程模板编号"",""项目编号"",""申请人编号"",""申请类型"",""申请标题"",""研究方案摘要"",""受试者风险等级"",""知情同意说明"",""附件路径"",""状态"",""提交时间"",""受理时间"",""决定时间"",""批件编号"",""有效期至"",""批件路径"",""退回原因"",""备注""
  FROM ""伦理审查申请"" WHERE ""编号""=@id;", new { id });

    private async Task<bool> 有权访问申请(dynamic application)
    {
        if (await 是伦理管理角色()) return true;
        if ((int)application.申请人编号 == CurrentUser.编号) return true;
        var assigned = await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS (
    SELECT 1 FROM ""伦理审查会议议程"" g
    JOIN ""伦理审查会议委员"" c ON c.""会议编号""=g.""会议编号""
   WHERE g.""申请编号""=@申请编号 AND c.""委员编号""=@人员编号
     AND c.""状态"" IN ('正式指派','回避')
);", new { 申请编号 = (long)application.编号, 人员编号 = CurrentUser.编号 });
        if (assigned) return true;
        return await CanAccessProjectAsync((int)application.流程模板编号, (int)application.项目编号);
    }

    // 兼容既有科研治理角色，同时支持部署方建立独立的伦理秘书/委员角色；正式环境由 Keycloak 角色映射决定实际授权。
    private async Task<bool> 是伦理管理角色() => await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办", "科研科负责人", "科研处处长", "伦理秘书", "伦理委员会秘书");

    private async Task<bool> 是伦理委员会管理角色() => await HasAnyRoleAsync("系统管理员", "科研管理员", "科技办", "科研科负责人", "科研处处长", "伦理秘书", "伦理委员会秘书");

    private static void 校验申请(伦理申请保存 model)
    {
        if (model == null || model.流程模板编号 <= 0 || model.项目编号 <= 0 || string.IsNullOrWhiteSpace(model.申请类型) || string.IsNullOrWhiteSpace(model.申请标题) || string.IsNullOrWhiteSpace(model.研究方案摘要)) throw new ArgumentException("流程模板、项目编号、申请类型、标题和研究方案摘要均为必填项");
        if (model.申请类型.Trim() is not ("初始审查" or "修正案" or "年度跟踪审查" or "延期续展")) throw new ArgumentException("伦理申请类型无效");
    }

    private static void 校验持续记录(伦理持续记录保存 model)
    {
        if (model == null || model.申请编号 <= 0 || string.IsNullOrWhiteSpace(model.记录类型) || string.IsNullOrWhiteSpace(model.事件说明)) throw new ArgumentException("申请编号、记录类型和事件说明均为必填项");
        if (model.记录类型.Trim() is not ("年度跟踪审查" or "方案修正案" or "知情同意修订" or "不良事件" or "偏离方案")) throw new ArgumentException("持续记录类型无效");
        if (!string.IsNullOrWhiteSpace(model.严重程度) && model.严重程度.Trim() is not ("一般" or "严重" or "危及生命")) throw new ArgumentException("不良事件严重程度无效");
        if (model.记录类型.Trim() == "不良事件")
        {
            if (string.IsNullOrWhiteSpace(model.严重程度)) throw new ArgumentException("不良事件必须填写严重程度");
            if (!model.发生日期.HasValue) throw new ArgumentException("不良事件必须填写发生日期");
        }
    }

    private async Task 记录伦理审计(long? 申请编号, long? 会议编号, long? 决定编号, long? 持续记录编号,
        string 动作, string 旧状态, string 新状态, object 详情)
    {
        var payload = Newtonsoft.Json.JsonConvert.SerializeObject(详情 ?? new { });
        await Db.ExecuteAsync(@"
INSERT INTO ""伦理审查治理审计"" (""申请编号"",""会议编号"",""决定编号"",""持续记录编号"",""动作"",""旧状态"",""新状态"",""详情"",""操作人编号"")
VALUES (@申请编号,@会议编号,@决定编号,@持续记录编号,@动作,@旧状态,@新状态,CAST(@详情 AS jsonb),@操作人编号);", new
        {
            申请编号, 会议编号, 决定编号, 持续记录编号, 动作, 旧状态, 新状态, 详情 = payload, 操作人编号 = CurrentUser.编号
        });
    }

    private static async Task 记录伦理审计(Npgsql.NpgsqlConnection connection, Npgsql.NpgsqlTransaction transaction,
        long? 申请编号, long? 会议编号, long? 决定编号, long? 持续记录编号,
        string 动作, string 旧状态, string 新状态, object 详情, int 操作人编号)
    {
        var payload = Newtonsoft.Json.JsonConvert.SerializeObject(详情 ?? new { });
        await connection.ExecuteAsync(@"
INSERT INTO ""伦理审查治理审计"" (""申请编号"",""会议编号"",""决定编号"",""持续记录编号"",""动作"",""旧状态"",""新状态"",""详情"",""操作人编号"")
VALUES (@申请编号,@会议编号,@决定编号,@持续记录编号,@动作,@旧状态,@新状态,CAST(@详情 AS jsonb),@操作人编号);", new
        {
            申请编号, 会议编号, 决定编号, 持续记录编号, 动作, 旧状态, 新状态, 详情 = payload, 操作人编号
        }, transaction);
    }

    private static string 空值(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
