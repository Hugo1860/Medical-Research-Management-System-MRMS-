using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ScientificResearch.Infrastucture;
using ScientificResearch.Models;

namespace ScientificResearch.Controllers;

/// <summary>教育部科研统计任务、版本化模板、数据包校验、调整留痕和导出状态。</summary>
public sealed class EducationStatisticsController : BaseController
{
    private static readonly string[] 管理角色 = { "系统管理员", "科研管理员", "科技办" };

    private Task<bool> IsManager() => HasAnyRoleAsync(管理角色);

    /// <summary>获取符合国家卫健委《全国卫生健康科技统计年报》标准的科技数据汇总大盘（支持二级医院科技填报）。</summary>
    [HttpGet]
    public async Task<object> 获取卫健委科技统计年报(int? 年度 = null)
    {
        var targetYear = 年度 ?? DateTime.Now.Year;

        // 1. 卫生技术人力资源概况
        var personnelSummary = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT 
    COUNT(*) AS 总人数,
    COUNT(*) FILTER (WHERE ""技术职称"" LIKE '%主任医师%' OR ""技术职称"" LIKE '%主任护师%' OR ""技术职称"" LIKE '%正高%') AS 正高人数,
    COUNT(*) FILTER (WHERE ""技术职称"" LIKE '%副主任%' OR ""技术职称"" LIKE '%副高%') AS 副高人数,
    COUNT(*) FILTER (WHERE ""技术职称"" LIKE '%主治%' OR ""技术职称"" LIKE '%主管%' OR ""技术职称"" LIKE '%中级%') AS 中级人数,
    COUNT(*) FILTER (WHERE ""技术职称"" LIKE '%住院%' OR ""技术职称"" LIKE '%初级%' OR ""技术职称"" LIKE '%护士%' OR ""技术职称"" LIKE '%医师%') AS 初级人数
FROM ""人员"";");

        // 2. 纵向课题立项情况
        var projectRows = await Db.QueryAsync<dynamic>(@"
SELECT 
    COALESCE(""项目级别"", '其他') AS 项目级别,
    COUNT(*) AS 项目数,
    COALESCE(SUM(""批准经费""), 0) AS 经费总额
FROM ""纵向项目立项""
WHERE ""立项年度"" = @targetYear
GROUP BY ""项目级别"";", new { targetYear });

        // 3. 横向协作与技术服务
        var horizontalSummary = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT 
    COUNT(*) AS 项目数,
    COALESCE(SUM(""合同金额""), 0) AS 合同金额
FROM ""横向项目""
WHERE EXTRACT(YEAR FROM COALESCE(""签订时间"", ""建立时间"")) = @targetYear;", new { targetYear });

        // 4. 医疗新技术新项目（三新）与适宜技术推广
        var techSummary = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT 
    (SELECT COUNT(*) FROM ""医疗新技术新项目"" WHERE ""申报年度"" = @targetYear) AS 新技术立项数,
    (SELECT COUNT(*) FROM ""医疗新技术临床应用随访"" WHERE EXTRACT(YEAR FROM ""应用日期"") = @targetYear) AS 临床应用例数,
    (SELECT COUNT(*) FROM ""医疗新技术临床应用随访"" WHERE EXTRACT(YEAR FROM ""应用日期"") = @targetYear AND ""是否发生不良反应"" = true) AS 不良反应例数,
    (SELECT COUNT(*) FROM ""医疗新技术转常规与评奖"" WHERE ""审批状态"" = '已批准' AND EXTRACT(YEAR FROM ""创建时间"") = @targetYear) AS 转常规与获奖数;");

        // 5. 适宜技术下乡与医共体协同
        var appropriateTechSummary = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT 
    COUNT(*) AS 推广项目数,
    COALESCE(SUM(""培训场次""), 0) AS 培训总场次,
    COALESCE(SUM(""受益医务人员数""), 0) AS 培训基层医护数,
    COALESCE(SUM(""受益患者人次""), 0) AS 受益患者人次
FROM ""医共体适宜技术推广""
WHERE EXTRACT(YEAR FROM ""推广日期"") = @targetYear;", new { targetYear });

        // 6. 论文与科研成果
        var paperSummary = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT 
    COUNT(*) AS 论文总数,
    COUNT(*) FILTER (WHERE ""是否Top期刊"" = true OR ""中科院分区"" IN ('1区', '2区', 'SCI')) AS 高水平论著数,
    COUNT(*) FILTER (WHERE ""第一作者"" IS NOT NULL OR ""通讯作者"" IS NOT NULL) AS 第一或通讯作者数
FROM ""成果论文""
WHERE ""年度"" = @targetYear OR EXTRACT(YEAR FROM ""论文正式出版日期"") = @targetYear;");

        // 7. 科普成果、典型案例与护理小发明
        var popularScienceSummary = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT 
    COUNT(*) FILTER (WHERE ""分类"" = '医学科普作品') AS 科普作品数,
    COUNT(*) FILTER (WHERE ""分类"" = '临床典型案例') AS 典型案例数,
    COUNT(*) FILTER (WHERE ""分类"" = '护理小发明') AS 护理小发明数,
    COALESCE(SUM(""受众传播量与成效""), 0) AS 累计受益与传播量
FROM ""科普成果与临床案例库""
WHERE EXTRACT(YEAR FROM ""完成日期"") = @targetYear;");

        // 8. 成果专利与知识产权
        var patentCount = await Db.QuerySingleAsync<int>(@"
SELECT COUNT(*) FROM ""成果专利""
WHERE ""年度"" = @targetYear OR EXTRACT(YEAR FROM ""申请日期"") = @targetYear;");

        // 9. 伦理审查全覆盖情况
        var ethicsSummary = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT 
    COUNT(*) AS 审查总件数,
    COUNT(*) FILTER (WHERE ""受试者风险等级"" = '低风险') AS 简易快审或免除知情数,
    COUNT(*) FILTER (WHERE ""状态"" = '已批准') AS 审查批准数
FROM ""伦理审查申请""
WHERE EXTRACT(YEAR FROM ""建立时间"") = @targetYear;");

        // 10. 医共体进修与帮扶
        var communityTraining = await Db.QuerySingleOrDefaultAsync<dynamic>(@"
SELECT 
    COUNT(*) FILTER (WHERE ""业务类型"" = '基层来院进修') AS 接收基层进修人数,
    COUNT(*) FILTER (WHERE ""业务类型"" = '总院下乡帮扶') AS 下乡帮扶人次
FROM ""医共体人员进修与帮扶记录""
WHERE EXTRACT(YEAR FROM ""开始日期"") = @targetYear;");

        return new
        {
            统计年度 = targetYear,
            生成时间 = DateTime.Now,
            报表类型 = "国家卫生健康科技统计年报（二级公立医院适配版）",
            人力资源 = personnelSummary,
            纵向课题 = projectRows,
            横向协作 = horizontalSummary,
            医疗新技术 = techSummary,
            适宜技术推广 = appropriateTechSummary,
            论文成果 = paperSummary,
            科普与案例 = popularScienceSummary,
            知识产权 = patentCount,
            伦理合规 = ethicsSummary,
            医共体协同 = communityTraining
        };
    }

    /// <summary>生成二级公立医院绩效考核（国考）与等级医院评审（二甲/二优）科教专项佐证材料包数据与打印HTML。</summary>
    [HttpGet]
    public async Task<object> 获取二级公立医院国考与等级评审佐证包(int? 年度 = null)
    {
        var targetYear = 年度 ?? DateTime.Now.Year;

        // 国考与等级评审核心佐证指标提取
        var techProjects = await Db.QueryAsync<dynamic>(@"
SELECT t.""编号"", t.""项目名称"", t.""申报类别"", t.""技术难度等级"", t.""准入审批状态"" AS ""审批状态"",
       (SELECT COUNT(*) FROM ""医疗新技术临床应用随访"" f WHERE f.""新技术编号"" = t.""编号"") AS ""临床开展总例数"",
       t.""申报年度""
FROM ""医疗新技术新项目"" t
WHERE t.""申报年度"" = @targetYear
ORDER BY t.""编号"" DESC LIMIT 30;", new { targetYear });

        var appropriateTechs = await Db.QueryAsync<dynamic>(@"
SELECT t.""编号"", t.""技术名称"", t.""受援基层单位"", t.""推广形式"", t.""推广日期"", t.""培训场次"", t.""受益医务人员数"", t.""受益患者人次""
FROM ""医共体适宜技术推广"" t
WHERE EXTRACT(YEAR FROM t.""推广日期"") = @targetYear
ORDER BY t.""推广日期"" DESC LIMIT 30;", new { targetYear });

        var popularSciences = await Db.QueryAsync<dynamic>(@"
SELECT ""编号"", ""分类"", ""成果名称"", ""第一作者姓名"", ""发布载体与平台"", ""受众传播量与成效"", ""审核状态""
FROM ""科普成果与临床案例库""
WHERE EXTRACT(YEAR FROM ""完成日期"") = @targetYear
ORDER BY ""编号"" DESC LIMIT 30;", new { targetYear });

        var communityRecords = await Db.QueryAsync<dynamic>(@"
SELECT r.""编号"", r.""业务类型"", u.""单位名称"" AS ""合作单位"", r.""人员姓名"", r.""人员职称"", r.""专业学科"", r.""开始日期"", r.""结束日期"", r.""考核鉴定结果""
FROM ""医共体人员进修与帮扶记录"" r
LEFT JOIN ""医共体合作单位"" u ON u.""编号"" = r.""合作单位编号""
WHERE EXTRACT(YEAR FROM r.""开始日期"") = @targetYear
ORDER BY r.""开始日期"" DESC LIMIT 30;", new { targetYear });

        // 生成规范的排版文档 HTML
        var reportHtml = $@"<!DOCTYPE html>
<html lang=""zh-CN"">
<head>
<meta charset=""utf-8"">
<title>{targetYear}年度 二级公立医院绩效考核与等级评审科教佐证材料包</title>
<style>
  body {{ font-family: SimSun, 'Songti SC', serif; margin: 30px 40px; color: #111; line-height: 1.6; }}
  h1 {{ text-align: center; font-size: 24px; font-weight: bold; margin-bottom: 6px; }}
  .sub-title {{ text-align: center; font-size: 14px; color: #666; margin-bottom: 25px; }}
  .box {{ border: 1px solid #333; padding: 12px 18px; margin-bottom: 20px; border-radius: 4px; }}
  .box-title {{ font-weight: bold; font-size: 15px; margin-bottom: 8px; color: #003a8c; }}
  table {{ width: 100%; border-collapse: collapse; margin-top: 10px; margin-bottom: 15px; }}
  th, td {{ border: 1px solid #444; padding: 6px 10px; font-size: 13px; text-align: left; }}
  th {{ background-color: #f2f4f6; font-weight: bold; }}
  .badge {{ display: inline-block; padding: 2px 6px; font-size: 11px; border-radius: 3px; font-weight: normal; }}
  .badge-success {{ background-color: #f6ffed; border: 1px solid #b7eb8f; color: #52c41a; }}
  .badge-primary {{ background-color: #e6f7ff; border: 1px solid #91d5ff; color: #1890ff; }}
  .summary-grid {{ display: flex; justify-content: space-between; margin-bottom: 20px; }}
  .summary-card {{ width: 23%; border: 1px solid #d9d9d9; padding: 10px; text-align: center; border-radius: 4px; background: #fafafa; }}
  .summary-num {{ font-size: 22px; font-weight: bold; color: #1890ff; margin-top: 4px; }}
  .stamp-area {{ margin-top: 40px; display: flex; justify-content: space-between; font-size: 14px; }}
  .stamp-col {{ width: 45%; border-top: 1px dashed #666; padding-top: 10px; }}
</style>
</head>
<body>
  <h1>{targetYear}年度 二级公立医院绩效考核与等级评审</h1>
  <h2 style=""text-align:center;font-size:18px;font-weight:normal;margin-top:0;"">科教工作成效专项佐证材料档案包</h2>
  <div class=""sub-title"">生成日期：{DateTime.Now:yyyy年MM月dd日} &nbsp;|&nbsp; 编制单位：医院科教科 / 科技处</div>

  <div class=""summary-grid"">
    <div class=""summary-card"">
      <div>三新医疗新技术</div>
      <div class=""summary-num"">{techProjects.Count()} 项</div>
    </div>
    <div class=""summary-card"">
      <div>适宜技术下乡推广</div>
      <div class=""summary-num"">{appropriateTechs.Count()} 项</div>
    </div>
    <div class=""summary-card"">
      <div>科普/典型案例/小发明</div>
      <div class=""summary-num"">{popularSciences.Count()} 项</div>
    </div>
    <div class=""summary-card"">
      <div>基层进修与帮扶</div>
      <div class=""summary-num"">{communityRecords.Count()} 人次</div>
    </div>
  </div>

  <div class=""box"">
    <div class=""box-title"">【国考指标 48 / 等级评审条款 4.8】临床医疗新技术新项目准入与常规转化佐证清单</div>
    <table>
      <thead>
        <tr>
          <th>序号</th><th>技术新项目名称</th><th>申报类别</th><th>难度等级</th><th>准入状态</th><th>临床开展例数</th>
        </tr>
      </thead>
      <tbody>";

        int idx = 1;
        foreach (var t in techProjects)
        {
            reportHtml += $@"
        <tr>
          <td>{idx++}</td>
          <td><strong>{t.项目名称}</strong></td>
          <td>{t.申报类别}</td>
          <td>{t.技术难度等级}</td>
          <td><span class=""badge badge-success"">{t.审批状态}</span></td>
          <td>{t.临床开展总例数} 例</td>
        </tr>";
        }
        if (!techProjects.Any()) reportHtml += "<tr><td colspan=\"6\" style=\"text-align:center;color:#999;\">本年度暂无新技术新项目登记记录</td></tr>";

        reportHtml += $@"
      </tbody>
    </table>
  </div>

  <div class=""box"">
    <div class=""box-title"">【国考指标 49 / 医共体协同】面向基层乡镇卫生院适宜技术推广与对口培训佐证清单</div>
    <table>
      <thead>
        <tr>
          <th>序号</th><th>适宜技术名称</th><th>受援基层单位</th><th>推广形式</th><th>推广日期</th><th>培训场次</th><th>培训基层医护</th><th>受益患者</th>
        </tr>
      </thead>
      <tbody>";

        idx = 1;
        foreach (var a in appropriateTechs)
        {
            var pDate = a.推广日期 != null ? ((DateTime)a.推广日期).ToString("yyyy-MM-dd") : "-";
            reportHtml += $@"
        <tr>
          <td>{idx++}</td>
          <td><strong>{a.技术名称}</strong></td>
          <td>{a.受援基层单位}</td>
          <td>{a.推广形式}</td>
          <td>{pDate}</td>
          <td>{a.培训场次} 场</td>
          <td>{a.受益医务人员数} 人</td>
          <td>{a.受益患者人次} 人次</td>
        </tr>";
        }
        if (!appropriateTechs.Any()) reportHtml += "<tr><td colspan=\"8\" style=\"text-align:center;color:#999;\">本年度暂无适宜技术下乡推广记录</td></tr>";

        reportHtml += $@"
      </tbody>
    </table>
  </div>

  <div class=""box"">
    <div class=""box-title"">【特色加分项】医学科普作品、临床典型案例库与护理小发明转化佐证</div>
    <table>
      <thead>
        <tr>
          <th>序号</th><th>分类</th><th>成果名称</th><th>第一作者</th><th>发布载体与平台</th><th>受众传播量与成效</th><th>审核状态</th>
        </tr>
      </thead>
      <tbody>";

        idx = 1;
        foreach (var p in popularSciences)
        {
            reportHtml += $@"
        <tr>
          <td>{idx++}</td>
          <td><span class=""badge badge-primary"">{p.分类}</span></td>
          <td><strong>{p.成果名称}</strong></td>
          <td>{p.第一作者姓名}</td>
          <td>{p.发布载体与平台}</td>
          <td>{p.受众传播量与成效}</td>
          <td>{p.审核状态}</td>
        </tr>";
        }
        if (!popularSciences.Any()) reportHtml += "<tr><td colspan=\"7\" style=\"text-align:center;color:#999;\">本年度暂无科普与案例记录</td></tr>";

        reportHtml += $@"
      </tbody>
    </table>
  </div>

  <div class=""box"">
    <div class=""box-title"">【等级评审人才培养】县域医共体人员来院进修与总院下乡帮扶佐证清单</div>
    <table>
      <thead>
        <tr>
          <th>序号</th><th>业务类型</th><th>合作医疗单位</th><th>进修/帮扶人员</th><th>职称/学科</th><th>起止周期</th><th>考核鉴定</th>
        </tr>
      </thead>
      <tbody>";

        idx = 1;
        foreach (var c in communityRecords)
        {
            var sDate = c.开始日期 != null ? ((DateTime)c.开始日期).ToString("yyyy-MM-dd") : "-";
            var eDate = c.结束日期 != null ? ((DateTime)c.结束日期).ToString("yyyy-MM-dd") : "-";
            reportHtml += $@"
        <tr>
          <td>{idx++}</td>
          <td>{c.业务类型}</td>
          <td>{c.合作单位 ?? "基层卫生院"}</td>
          <td><strong>{c.人员姓名}</strong></td>
          <td>{c.人员职称} / {c.专业学科}</td>
          <td>{sDate} 至 {eDate}</td>
          <td><span class=""badge badge-success"">{c.考核鉴定结果}</span></td>
        </tr>";
        }
        if (!communityRecords.Any()) reportHtml += "<tr><td colspan=\"7\" style=\"text-align:center;color:#999;\">本年度暂无医共体进修与帮扶记录</td></tr>";

        reportHtml += $@"
      </tbody>
    </table>
  </div>

  <div class=""stamp-area"">
    <div class=""stamp-col"">
      科教科负责人审核（签名）：___________________<br/>
      日期：{DateTime.Now:yyyy年MM月dd日}
    </div>
    <div class=""stamp-col"">
      分管院领导审定（签章）：___________________<br/>
      日期：______年___月___日
    </div>
  </div>
</body>
</html>";

        return new
        {
            年度 = targetYear,
            新技术清单 = techProjects,
            适宜技术清单 = appropriateTechs,
            科普案例清单 = popularSciences,
            进修帮扶清单 = communityRecords,
            佐证包Html = reportHtml
        };
    }

    [HttpGet]
    public async Task<object> 查询统计任务(int? 统计年度 = null, string 状态 = null)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权查看教育部统计任务" });
        return await Db.QueryAsync<object>(@"
SELECT t.""编号"",t.""统计年度"",t.""任务代码"",t.""任务名称"",t.""截止日期"",t.""数据截止日期"",t.""状态"",t.""当前版本编号"",
       v.""版本号"" AS ""当前版本号"",t.""说明"",t.""创建人编号"",p.""姓名"" AS ""创建人姓名"",t.""创建时间"",t.""更新时间"",
       (SELECT count(*) FROM ""教育部统计数据包"" b WHERE b.""任务编号""=t.""编号"" AND b.""状态""<>'已作废') AS ""数据包数量"",
       (SELECT count(*) FROM ""教育部统计导出任务"" e WHERE e.""任务编号""=t.""编号"") AS ""导出任务数量""
  FROM ""教育部统计任务"" t
  LEFT JOIN ""教育部统计版本"" v ON v.""编号""=t.""当前版本编号""
  LEFT JOIN ""人员"" p ON p.""编号""=t.""创建人编号""
 WHERE (@统计年度 IS NULL OR t.""统计年度""=@统计年度)
   AND (@状态 IS NULL OR t.""状态""=@状态)
 ORDER BY t.""统计年度"" DESC,t.""更新时间"" DESC,t.""编号"" DESC;", new { 统计年度, 状态 = 空值(状态) });
    }

    [HttpPost]
    public async Task<object> 保存统计任务([FromBody] 教育部统计任务保存 model)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权维护教育部统计任务" });
        if (model == null || model.统计年度 is < 2000 or > 2200 || string.IsNullOrWhiteSpace(model.任务代码) || string.IsNullOrWhiteSpace(model.任务名称))
            throw new ArgumentException("统计年度、任务代码和任务名称不能为空或无效");
        var code = model.任务代码.Trim();
        var name = model.任务名称.Trim();
        if (code.Length > 100 || name.Length > 200 || (model.说明?.Length ?? 0) > 2000) throw new ArgumentException("统计任务字段超出限制");
        var status = string.IsNullOrWhiteSpace(model.状态) ? "草稿" : model.状态.Trim();
        if (status is not ("草稿" or "开放" or "已关闭")) throw new ArgumentException("保存时只能使用草稿、开放或已关闭状态");
        if (model.截止日期.HasValue && model.数据截止日期.HasValue && model.数据截止日期.Value.Date > model.截止日期.Value.Date)
            throw new ArgumentException("数据截止日期不能晚于申报截止日期");

        if (model.编号.HasValue)
        {
            var changed = await Db.ExecuteAsync(@"
UPDATE ""教育部统计任务"" SET ""统计年度""=@统计年度,""任务代码""=@任务代码,""任务名称""=@任务名称,
       ""截止日期""=@截止日期,""数据截止日期""=@数据截止日期,""状态""=@状态,""说明""=@说明,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@编号 AND ""状态"" IN ('草稿','开放');", new
            { model.编号, model.统计年度, 任务代码 = code, 任务名称 = name, model.截止日期, model.数据截止日期, 状态 = status, 说明 = 空值(model.说明) });
            if (changed == 0) return NotFound(new { error = "统计任务不存在或当前状态不可修改" });
            await 记录审计(model.编号, null, null, null, "统计任务修改", $"任务代码={code}", null);
            return new { 编号 = model.编号.Value, 状态 = status };
        }

        var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""教育部统计任务"" (""统计年度"",""任务代码"",""任务名称"",""截止日期"",""数据截止日期"",""状态"",""说明"",""创建人编号"")
VALUES (@统计年度,@任务代码,@任务名称,@截止日期,@数据截止日期,@状态,@说明,@创建人编号) RETURNING ""编号"";", new
        {
            model.统计年度, 任务代码 = code, 任务名称 = name, model.截止日期, model.数据截止日期, 状态 = status,
            说明 = 空值(model.说明), 创建人编号 = CurrentUser.编号
        });
        await 记录审计(id, null, null, null, "统计任务创建", $"任务代码={code}", null);
        return new { 编号 = id, 状态 = status };
    }

    [HttpGet]
    public async Task<object> 查询统计版本(long 任务编号)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权查看教育部统计版本" });
        return await Db.QueryAsync<object>(@"
SELECT v.""编号"",v.""任务编号"",v.""版本号"",v.""模板代码"",v.""模板名称"",v.""字段定义"",v.""状态"",v.""变更说明"",
       v.""创建人编号"",p.""姓名"" AS ""创建人姓名"",v.""创建时间"",v.""更新时间""
  FROM ""教育部统计版本"" v LEFT JOIN ""人员"" p ON p.""编号""=v.""创建人编号""
 WHERE v.""任务编号""=@任务编号 ORDER BY v.""版本号"" DESC;", new { 任务编号 });
    }

    [HttpPost]
    public async Task<object> 保存统计版本([FromBody] 教育部统计版本保存 model)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权维护教育部统计版本" });
        if (model == null || model.任务编号 <= 0 || model.版本号 <= 0 || string.IsNullOrWhiteSpace(model.模板代码) || string.IsNullOrWhiteSpace(model.模板名称))
            throw new ArgumentException("统计任务、版本号、模板代码和模板名称不能为空");
        if ((model.字段定义?.Length ?? 0) > 200000 || (model.变更说明?.Length ?? 0) > 2000) throw new ArgumentException("版本字段超出限制");
        var definition = ValidateFieldDefinition(model.字段定义);
        var status = string.IsNullOrWhiteSpace(model.状态) ? "草稿" : model.状态.Trim();
        if (status is not ("草稿" or "停用")) throw new ArgumentException("保存时只能使用草稿或停用状态，请通过发布接口生效");
        if (!await Db.QuerySingleAsync<bool>("SELECT EXISTS(SELECT 1 FROM \"教育部统计任务\" WHERE \"编号\"=@任务编号);", new { model.任务编号 }))
            return NotFound(new { error = "统计任务不存在" });

        if (model.编号.HasValue)
        {
            var changed = await Db.ExecuteAsync(@"
UPDATE ""教育部统计版本"" SET ""任务编号""=@任务编号,""版本号""=@版本号,""模板代码""=@模板代码,""模板名称""=@模板名称,
       ""字段定义""=CAST(@字段定义 AS jsonb),""状态""=@状态,""变更说明""=@变更说明,""更新时间""=CURRENT_TIMESTAMP
 WHERE ""编号""=@编号 AND ""状态""<>'生效';", new
            { model.编号, model.任务编号, model.版本号, 模板代码 = model.模板代码.Trim(), 模板名称 = model.模板名称.Trim(), 字段定义 = definition, 状态 = status, 变更说明 = 空值(model.变更说明) });
            if (changed == 0) return NotFound(new { error = "统计版本不存在或已生效，不能修改" });
            await 记录审计(null, model.编号, null, null, "统计版本修改", $"版本={model.版本号}", null);
            return new { 编号 = model.编号.Value, 状态 = status };
        }
        var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""教育部统计版本"" (""任务编号"",""版本号"",""模板代码"",""模板名称"",""字段定义"",""状态"",""变更说明"",""创建人编号"")
VALUES (@任务编号,@版本号,@模板代码,@模板名称,CAST(@字段定义 AS jsonb),@状态,@变更说明,@创建人编号) RETURNING ""编号"";", new
        {
            model.任务编号, model.版本号, 模板代码 = model.模板代码.Trim(), 模板名称 = model.模板名称.Trim(), 字段定义 = definition,
            状态 = status, 变更说明 = 空值(model.变更说明), 创建人编号 = CurrentUser.编号
        });
        await 记录审计(null, id, null, null, "统计版本创建", $"版本={model.版本号}", null);
        return new { 编号 = id, 状态 = status };
    }

    [HttpPost]
    public async Task<object> 发布统计版本(long 版本编号)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权发布教育部统计版本" });
        var taskId = await Db.QuerySingleOrDefaultAsync<long?>("SELECT \"任务编号\" FROM \"教育部统计版本\" WHERE \"编号\"=@版本编号 AND \"状态\"<>'停用';", new { 版本编号 });
        if (!taskId.HasValue) return NotFound(new { error = "统计版本不存在或已停用" });
        await Db.ExecuteAsync("UPDATE \"教育部统计版本\" SET \"状态\"='停用',\"更新时间\"=CURRENT_TIMESTAMP WHERE \"任务编号\"=@任务编号 AND \"状态\"='生效';", new { 任务编号 = taskId.Value });
        await Db.ExecuteAsync("UPDATE \"教育部统计版本\" SET \"状态\"='生效',\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@版本编号;", new { 版本编号 });
        await Db.ExecuteAsync("UPDATE \"教育部统计任务\" SET \"当前版本编号\"=@版本编号,\"状态\"=CASE WHEN \"状态\"='草稿' THEN '开放' ELSE \"状态\" END,\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@任务编号;", new { 版本编号, 任务编号 = taskId.Value });
        await 记录审计(taskId, 版本编号, null, null, "统计版本发布", "版本已生效；未向教育部发送外部回执", null);
        return new { 编号 = 版本编号, 任务编号 = taskId.Value, 状态 = "生效" };
    }

    [HttpGet]
    public async Task<object> 查询统计数据包(long? 任务编号 = null, string 状态 = null)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权查看教育部统计数据包" });
        return await Db.QueryAsync<object>(@"
SELECT b.""编号"",b.""任务编号"",t.""统计年度"",t.""任务代码"",b.""版本编号"",v.""版本号"",b.""包名称"",b.""文件类型"",b.""文件路径"",b.""文件校验和"",
       b.""状态"",b.""记录数"",b.""校验错误数"",b.""校验警告数"",b.""导入人编号"",p.""姓名"" AS ""导入人姓名"",b.""导入时间"",b.""更新时间"",b.""说明""
  FROM ""教育部统计数据包"" b JOIN ""教育部统计任务"" t ON t.""编号""=b.""任务编号"" JOIN ""教育部统计版本"" v ON v.""编号""=b.""版本编号""
  LEFT JOIN ""人员"" p ON p.""编号""=b.""导入人编号""
 WHERE (@任务编号 IS NULL OR b.""任务编号""=@任务编号) AND (@状态 IS NULL OR b.""状态""=@状态)
 ORDER BY b.""更新时间"" DESC,b.""编号"" DESC;", new { 任务编号, 状态 = 空值(状态) });
    }

    [HttpPost]
    public async Task<object> 登记统计数据包([FromBody] 教育部统计数据包导入 model)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权导入教育部统计数据包" });
        if (model == null || model.任务编号 <= 0 || model.版本编号 <= 0 || string.IsNullOrWhiteSpace(model.包名称)) throw new ArgumentException("任务、版本和数据包名称不能为空");
        if (model.包名称.Trim().Length > 300 || (model.文件路径?.Length ?? 0) > 1000 || (model.文件校验和?.Length ?? 0) > 128 || (model.数据内容?.Length ?? 0) > 50_000_000) throw new ArgumentException("数据包字段超出限制");
        var relation = await Db.QuerySingleOrDefaultAsync<int?>("SELECT 1 FROM \"教育部统计版本\" WHERE \"编号\"=@版本编号 AND \"任务编号\"=@任务编号 AND \"状态\"='生效';", new { model.版本编号, model.任务编号 });
        if (!relation.HasValue) return BadRequest(new { error = "统计版本不存在、任务不匹配或版本未生效" });
        var content = string.IsNullOrWhiteSpace(model.数据内容) ? "[]" : model.数据内容.Trim();
        var status = "待导入";
        var count = 0;
        if (content != "[]")
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) throw new ArgumentException("数据内容必须是 JSON 数组");
                count = doc.RootElement.GetArrayLength();
                if (count > 10000) throw new ArgumentException("单个数据包最多 10000 条记录");
                status = "待校验";
            }
            catch (JsonException) { throw new ArgumentException("数据内容不是有效 JSON 数组"); }
        }
        var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""教育部统计数据包"" (""任务编号"",""版本编号"",""包名称"",""文件类型"",""文件路径"",""文件校验和"",""原始内容"",""状态"",""记录数"",""导入人编号"",""导入时间"",""说明"")
VALUES (@任务编号,@版本编号,@包名称,@文件类型,@文件路径,@文件校验和,CAST(@内容 AS jsonb),@状态,@记录数,@导入人编号,CASE WHEN @状态='待校验' THEN CURRENT_TIMESTAMP ELSE NULL END,@说明) RETURNING ""编号"";", new
        {
            model.任务编号, model.版本编号, 包名称 = model.包名称.Trim(), 文件类型 = 空值(model.文件类型), 文件路径 = 空值(model.文件路径), 文件校验和 = 空值(model.文件校验和),
            内容 = content, 状态 = status, 记录数 = count, 导入人编号 = CurrentUser.编号, 说明 = 空值(model.说明)
        });
        await Db.ExecuteAsync("UPDATE \"教育部统计任务\" SET \"状态\"=CASE WHEN @状态='待校验' THEN '导入中' ELSE \"状态\" END,\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@任务编号 AND \"状态\" IN ('草稿','开放','导入中','待调整');", new { model.任务编号, 状态 = status });
        await 记录审计(model.任务编号, model.版本编号, id, null, "统计数据包登记", status == "待校验" ? "已登记用户提供的 JSON 数据，等待字段校验；未声称外部文件存在或已上报" : "仅登记文件元数据，等待实际数据导入", null);
        return new { 编号 = id, 状态 = status, 记录数 = count, 实际文件已校验 = false, 外部回执 = false };
    }

    [HttpGet]
    public async Task<object> 查询统计字段校验(long 数据包编号, string 严重程度 = null)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权查看教育部统计字段校验" });
        return await Db.QueryAsync<object>(@"
SELECT c.""编号"",c.""数据包编号"",c.""记录序号"",c.""记录标识"",c.""字段代码"",c.""严重程度"",c.""消息"",c.""实际值"",c.""状态"",c.""创建时间"",c.""处理人编号"",p.""姓名"" AS ""处理人姓名"",c.""处理时间""
  FROM ""教育部统计字段校验"" c LEFT JOIN ""人员"" p ON p.""编号""=c.""处理人编号""
 WHERE c.""数据包编号""=@数据包编号 AND (@严重程度 IS NULL OR c.""严重程度""=@严重程度)
 ORDER BY c.""记录序号"",CASE c.""严重程度"" WHEN '错误' THEN 1 ELSE 2 END,c.""编号"";", new { 数据包编号, 严重程度 = 空值(严重程度) });
    }

    [HttpPost]
    public async Task<object> 校验统计数据包(long 数据包编号)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权校验教育部统计数据包" });
        await using var connection = (NpgsqlConnection)Db;
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();
        var package = await connection.QuerySingleOrDefaultAsync<统计数据包行>(@"
SELECT b.""编号"",b.""任务编号"",b.""版本编号"",b.""状态"",b.""原始内容""::text AS ""内容Json"",v.""字段定义""::text AS ""字段定义Json""
  FROM ""教育部统计数据包"" b JOIN ""教育部统计版本"" v ON v.""编号""=b.""版本编号"" WHERE b.""编号""=@数据包编号 FOR UPDATE;", new { 数据包编号 }, tx);
        if (package == null) return NotFound(new { error = "统计数据包不存在" });
        if (package.状态 is "已作废") return BadRequest(new { error = "已作废数据包不可校验" });
        using var contentDoc = JsonDocument.Parse(string.IsNullOrWhiteSpace(package.内容Json) ? "[]" : package.内容Json);
        using var schemaDoc = JsonDocument.Parse(string.IsNullOrWhiteSpace(package.字段定义Json) ? "[]" : package.字段定义Json);
        if (contentDoc.RootElement.ValueKind != JsonValueKind.Array || schemaDoc.RootElement.ValueKind != JsonValueKind.Array) throw new ArgumentException("数据包内容或版本字段定义不是数组");
        var fields = ParseFieldDefinition(schemaDoc.RootElement);
        if (fields.Count == 0) throw new ArgumentException("统计版本尚未配置字段定义，无法校验");
        await connection.ExecuteAsync("UPDATE \"教育部统计数据包\" SET \"状态\"='校验中',\"更新时间\"=CURRENT_TIMESTAMP WHERE \"编号\"=@数据包编号; DELETE FROM \"教育部统计字段校验\" WHERE \"数据包编号\"=@数据包编号;", new { 数据包编号 }, tx);
        var errors = 0; var warnings = 0; var index = 0;
        foreach (var record in contentDoc.RootElement.EnumerateArray())
        {
            index++;
            var recordKey = GetRecordKey(record, index);
            if (record.ValueKind != JsonValueKind.Object)
            {
                await InsertValidation(connection, tx, 数据包编号, index, recordKey, null, "错误", "记录必须是 JSON 对象", record.GetRawText()); errors++; continue;
            }
            foreach (var field in fields)
            {
                if (!record.TryGetProperty(field.Code, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                {
                    if (field.Required) { await InsertValidation(connection, tx, 数据包编号, index, recordKey, field.Code, "错误", "必填字段缺失", null); errors++; }
                    continue;
                }
                var actual = value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
                if (!ValueMatches(field.Type, value)) { await InsertValidation(connection, tx, 数据包编号, index, recordKey, field.Code, "错误", $"字段类型应为 {field.Type}", actual); errors++; }
                if (field.EnumValues.Count > 0 && !field.EnumValues.Contains(actual, StringComparer.OrdinalIgnoreCase)) { await InsertValidation(connection, tx, 数据包编号, index, recordKey, field.Code, "错误", "字段值不在允许范围", actual); errors++; }
            }
            foreach (var property in record.EnumerateObject().Where(p => !fields.Any(f => f.Code == p.Name)))
            {
                await InsertValidation(connection, tx, 数据包编号, index, recordKey, property.Name, "警告", "未在版本字段定义中声明的字段", property.Value.GetRawText()); warnings++;
            }
        }
        var status = errors == 0 ? "校验通过" : "校验失败";
        await connection.ExecuteAsync(@"
UPDATE ""教育部统计数据包"" SET ""状态""=@状态,""记录数""=@记录数,""校验错误数""=@错误数,""校验警告数""=@警告数,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@数据包编号;
UPDATE ""教育部统计任务"" SET ""状态""=CASE WHEN @错误数=0 THEN '可导出' ELSE '待调整' END,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@任务编号;", new { 状态 = status, 记录数 = index, 错误数 = errors, 警告数 = warnings, 数据包编号, 任务编号 = package.任务编号 }, tx);
        await connection.ExecuteAsync(@"
INSERT INTO ""教育部统计操作审计"" (""任务编号"",""版本编号"",""数据包编号"",""操作类型"",""操作人编号"",""说明"")
VALUES (@任务编号,@版本编号,@数据包编号,'统计数据包字段校验',@操作人编号,@说明);", new { package.任务编号, package.版本编号, 数据包编号, 操作人编号 = CurrentUser.编号, 说明 = $"校验完成：{index} 条，错误 {errors}，警告 {warnings}；未向教育部发送外部回执" }, tx);
        await tx.CommitAsync();
        return new { 编号 = 数据包编号, 状态 = status, 记录数 = index, 校验错误数 = errors, 校验警告数 = warnings, 外部回执 = false };
    }

    [HttpPost]
    public async Task<object> 调整统计字段([FromBody] 教育部统计字段调整 model)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权调整教育部统计数据" });
        if (model == null || model.数据包编号 <= 0 || model.记录序号 <= 0 || string.IsNullOrWhiteSpace(model.字段代码) || string.IsNullOrWhiteSpace(model.调整原因)) throw new ArgumentException("数据包、记录序号、字段代码和调整原因不能为空");
        if ((model.新值?.Length ?? 0) > 4000 || model.调整原因.Length > 2000) throw new ArgumentException("调整字段超出限制");
        await using var connection = (NpgsqlConnection)Db;
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();
        var row = await connection.QuerySingleOrDefaultAsync<统计数据包行>("SELECT \"编号\",\"任务编号\",\"版本编号\",\"状态\",\"原始内容\"::text AS \"内容Json\" FROM \"教育部统计数据包\" WHERE \"编号\"=@数据包编号 FOR UPDATE;", new { model.数据包编号 }, tx);
        if (row == null) return NotFound(new { error = "统计数据包不存在" });
        if (row.状态 is "已作废") return BadRequest(new { error = "已作废数据包不可调整" });
        var array = JsonNode.Parse(row.内容Json ?? "[]") as JsonArray ?? throw new ArgumentException("数据包内容不是数组");
        if (model.记录序号 > array.Count) throw new ArgumentException("记录序号超出数据包范围");
        if (array[model.记录序号 - 1] is not JsonObject record) throw new ArgumentException("目标记录不是对象");
        var old = record[model.字段代码];
        JsonNode value;
        try { value = JsonNode.Parse(string.IsNullOrWhiteSpace(model.新值) ? "null" : model.新值) ?? JsonValue.Create<string>(null); }
        catch (JsonException) { value = JsonValue.Create(model.新值); }
        record[model.字段代码] = value;
        var newJson = value.ToJsonString();
        await connection.ExecuteAsync(@"
UPDATE ""教育部统计数据包"" SET ""原始内容""=CAST(@内容 AS jsonb),""状态""='待校验',""校验错误数""=0,""校验警告数""=0,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@数据包编号;
UPDATE ""教育部统计任务"" SET ""状态""='待调整',""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@任务编号;
INSERT INTO ""教育部统计调整留痕"" (""数据包编号"",""记录序号"",""记录标识"",""字段代码"",""原值"",""新值"",""调整原因"",""调整人编号"",""请求编号"")
VALUES (@数据包编号,@记录序号,@记录标识,@字段代码,CAST(@原值 AS jsonb),CAST(@新值 AS jsonb),@调整原因,@调整人编号,@请求编号);
INSERT INTO ""教育部统计操作审计"" (""任务编号"",""版本编号"",""数据包编号"",""操作类型"",""操作人编号"",""说明"",""请求编号"")
VALUES (@任务编号,@版本编号,@数据包编号,'统计数据调整',@调整人编号,@调整原因,@请求编号);", new
        {
            内容 = array.ToJsonString(), model.数据包编号, model.记录序号, 记录标识 = model.记录标识 ?? GetRecordKey(record, model.记录序号), model.字段代码,
            原值 = old?.ToJsonString() ?? "null", 新值 = newJson, model.调整原因, 调整人编号 = CurrentUser.编号, model.请求编号, 任务编号 = row.任务编号, 版本编号 = row.版本编号
        }, tx);
        await tx.CommitAsync();
        return new { 编号 = model.数据包编号, 状态 = "待校验", 调整已留痕 = true, 外部回执 = false };
    }

    [HttpGet]
    public async Task<object> 查询统计调整留痕(long 数据包编号)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权查看教育部统计调整留痕" });
        return await Db.QueryAsync<object>(@"
SELECT a.""编号"",a.""数据包编号"",a.""记录序号"",a.""记录标识"",a.""字段代码"",a.""原值"",a.""新值"",a.""调整原因"",a.""调整人编号"",p.""姓名"" AS ""调整人姓名"",a.""请求编号"",a.""创建时间""
  FROM ""教育部统计调整留痕"" a LEFT JOIN ""人员"" p ON p.""编号""=a.""调整人编号""
 WHERE a.""数据包编号""=@数据包编号 ORDER BY a.""创建时间"" DESC,a.""编号"" DESC;", new { 数据包编号 });
    }

    [HttpPost]
    public async Task<object> 创建统计导出任务([FromBody] 教育部统计导出任务保存 model)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权创建教育部统计导出任务" });
        if (model == null || model.任务编号 <= 0 || model.版本编号 <= 0) throw new ArgumentException("统计任务和版本不能为空");
        var format = string.IsNullOrWhiteSpace(model.导出格式) ? "xlsx" : model.导出格式.Trim().ToLowerInvariant();
        if (format is not ("xlsx" or "csv" or "xml")) throw new ArgumentException("导出格式无效");
        if (!model.数据包编号.HasValue) throw new ArgumentException("必须指定已校验的数据包");
        var valid = await Db.QuerySingleAsync<bool>(@"
SELECT EXISTS(SELECT 1 FROM ""教育部统计数据包"" WHERE ""编号""=@数据包编号 AND ""任务编号""=@任务编号 AND ""版本编号""=@版本编号 AND ""状态""='校验通过');", new { model.数据包编号, model.任务编号, model.版本编号 });
        if (!valid) return BadRequest(new { error = "只有任务/版本匹配且校验通过的数据包才能申请导出" });
        var id = await Db.QuerySingleAsync<long>(@"
INSERT INTO ""教育部统计导出任务"" (""任务编号"",""版本编号"",""数据包编号"",""导出格式"",""申请人编号"")
VALUES (@任务编号,@版本编号,@数据包编号,@导出格式,@申请人编号) RETURNING ""编号"";", new { model.任务编号, model.版本编号, model.数据包编号, 导出格式 = format, 申请人编号 = CurrentUser.编号 });
        await 记录审计(model.任务编号, model.版本编号, model.数据包编号, id, "统计导出任务创建", "已创建导出任务，实际文件由受控导出 worker 生成；未伪造上报回执", null);
        return new { 编号 = id, 状态 = "申请中", 外部回执 = false };
    }

    [HttpGet]
    public async Task<object> 查询统计导出任务(long? 任务编号 = null, string 状态 = null)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权查看教育部统计导出任务" });
        return await Db.QueryAsync<object>(@"
SELECT e.""编号"",e.""任务编号"",t.""统计年度"",t.""任务代码"",e.""版本编号"",v.""版本号"",e.""数据包编号"",e.""导出格式"",e.""状态"",e.""文件路径"",e.""文件校验和"",e.""记录数"",e.""错误信息"",e.""申请人编号"",p.""姓名"" AS ""申请人姓名"",e.""申请时间"",e.""完成时间"",e.""更新时间""
  FROM ""教育部统计导出任务"" e JOIN ""教育部统计任务"" t ON t.""编号""=e.""任务编号"" JOIN ""教育部统计版本"" v ON v.""编号""=e.""版本编号""
  LEFT JOIN ""人员"" p ON p.""编号""=e.""申请人编号""
 WHERE (@任务编号 IS NULL OR e.""任务编号""=@任务编号) AND (@状态 IS NULL OR e.""状态""=@状态)
 ORDER BY e.""申请时间"" DESC,e.""编号"" DESC;", new { 任务编号, 状态 = 空值(状态) });
    }

    [HttpPost]
    public async Task<object> 更新统计导出任务([FromBody] 教育部统计导出任务保存 model)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权更新教育部统计导出任务" });
        if (model == null || !model.编号.HasValue) throw new ArgumentException("导出任务编号无效");
        var status = string.IsNullOrWhiteSpace(model.状态) ? "生成中" : model.状态.Trim();
        if (status is not ("申请中" or "生成中" or "已完成" or "失败" or "已取消")) throw new ArgumentException("导出任务状态无效");
        if (status == "已完成" && (string.IsNullOrWhiteSpace(model.文件路径) || string.IsNullOrWhiteSpace(model.文件校验和))) throw new ArgumentException("完成导出必须提供文件路径和校验和");
        var changed = await Db.ExecuteAsync(@"
UPDATE ""教育部统计导出任务"" SET ""状态""=@状态,""文件路径""=@文件路径,""文件校验和""=@文件校验和,""记录数""=@记录数,""错误信息""=@错误信息,
       ""完成时间""=CASE WHEN @状态 IN ('已完成','失败','已取消') THEN CURRENT_TIMESTAMP ELSE ""完成时间"" END,""更新时间""=CURRENT_TIMESTAMP WHERE ""编号""=@编号;", new
        {
            编号 = model.编号.Value, 状态 = status, 文件路径 = 空值(model.文件路径), 文件校验和 = 空值(model.文件校验和), model.记录数, 错误信息 = 空值(model.错误信息)
        });
        if (changed == 0) return NotFound(new { error = "导出任务不存在" });
        var task = await Db.QuerySingleOrDefaultAsync<dynamic>("SELECT \"任务编号\",\"版本编号\",\"数据包编号\" FROM \"教育部统计导出任务\" WHERE \"编号\"=@编号;", new { 编号 = model.编号.Value });
        await 记录审计((long?)task?.任务编号, (long?)task?.版本编号, (long?)task?.数据包编号, model.编号, "统计导出任务状态更新", status == "已完成" ? "worker 已回填文件路径和校验和；仍未向教育部发送回执" : status, null);
        return new { 编号 = model.编号.Value, 状态 = status, 外部回执 = false };
    }

    [HttpGet]
    public async Task<object> 查询统计操作审计(long? 任务编号 = null, long? 数据包编号 = null, int 数量 = 200)
    {
        if (!await IsManager()) return StatusCode(403, new { error = "无权查看教育部统计操作审计" });
        var limit = Math.Clamp(数量, 1, 1000);
        return await Db.QueryAsync<object>(@"
SELECT a.""编号"",a.""任务编号"",a.""版本编号"",a.""数据包编号"",a.""导出任务编号"",a.""操作类型"",a.""操作人编号"",p.""姓名"" AS ""操作人姓名"",a.""说明"",a.""请求编号"",a.""创建时间""
  FROM ""教育部统计操作审计"" a LEFT JOIN ""人员"" p ON p.""编号""=a.""操作人编号""
 WHERE (@任务编号 IS NULL OR a.""任务编号""=@任务编号) AND (@数据包编号 IS NULL OR a.""数据包编号""=@数据包编号)
 ORDER BY a.""创建时间"" DESC,a.""编号"" DESC LIMIT @数量;", new { 任务编号, 数据包编号, 数量 = limit });
    }

    private async Task 记录审计(long? taskId, long? versionId, long? packageId, long? exportId, string type, string note, string requestId)
    {
        await Db.ExecuteAsync(@"
INSERT INTO ""教育部统计操作审计"" (""任务编号"",""版本编号"",""数据包编号"",""导出任务编号"",""操作类型"",""操作人编号"",""说明"",""请求编号"")
VALUES (@taskId,@versionId,@packageId,@exportId,@type,@operatorId,@note,@requestId);", new { taskId, versionId, packageId, exportId, type, operatorId = CurrentUser.编号, note, requestId });
    }

    private static string 空值(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ValidateFieldDefinition(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("字段定义不能为空");
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array) throw new ArgumentException("字段定义必须是 JSON 数组");
        _ = ParseFieldDefinition(doc.RootElement);
        return doc.RootElement.GetRawText();
    }

    private static List<FieldDefinition> ParseFieldDefinition(JsonElement root)
    {
        var list = new List<FieldDefinition>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in root.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("代码", out var codeProp) || !item.TryGetProperty("类型", out var typeProp))
                throw new ArgumentException("字段定义每项必须包含代码和类型");
            var code = codeProp.GetString()?.Trim();
            var type = typeProp.GetString()?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(code) || code.Length > 100 || !seen.Add(code)) throw new ArgumentException("字段代码不能为空且不能重复");
            if (type is not ("string" or "integer" or "number" or "decimal" or "date" or "boolean")) throw new ArgumentException($"不支持字段类型：{type}");
            var required = item.TryGetProperty("必填", out var requiredProp) && requiredProp.ValueKind == JsonValueKind.True;
            var enums = new List<string>();
            if (item.TryGetProperty("枚举", out var enumProp) && enumProp.ValueKind == JsonValueKind.Array) enums.AddRange(enumProp.EnumerateArray().Select(x => x.ToString()));
            list.Add(new FieldDefinition(code, type, required, enums));
        }
        return list;
    }

    private static bool ValueMatches(string type, JsonElement value)
    {
        if (type == "string") return value.ValueKind == JsonValueKind.String;
        if (type == "boolean") return value.ValueKind is JsonValueKind.True or JsonValueKind.False;
        if (type == "date") return value.ValueKind == JsonValueKind.String && DateTime.TryParse(value.GetString(), out _);
        if (type is "integer") return value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _);
        if (type is "number" or "decimal") return value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out _);
        return false;
    }

    private static string GetRecordKey(JsonElement record, int index)
    {
        if (record.ValueKind == JsonValueKind.Object)
            foreach (var name in new[] { "记录标识", "记录编号", "编号", "代码" })
                if (record.TryGetProperty(name, out var value)) return value.ToString();
        return index.ToString();
    }

    private static string GetRecordKey(JsonObject record, int index)
    {
        foreach (var name in new[] { "记录标识", "记录编号", "编号", "代码" })
            if (record.TryGetPropertyValue(name, out var value) && value != null) return value.ToString();
        return index.ToString();
    }

    private static async Task InsertValidation(NpgsqlConnection connection, NpgsqlTransaction tx, long packageId, int row, string key, string code, string severity, string message, string actual)
    {
        await connection.ExecuteAsync(@"
INSERT INTO ""教育部统计字段校验"" (""数据包编号"",""记录序号"",""记录标识"",""字段代码"",""严重程度"",""消息"",""实际值"")
VALUES (@packageId,@row,@key,@code,@severity,@message,@actual);", new { packageId, row, key, code, severity, message, actual = actual?.Length > 4000 ? actual[..4000] : actual }, tx);
    }

    private sealed class 统计数据包行
    {
        public long 编号 { get; set; }
        public long 任务编号 { get; set; }
        public long 版本编号 { get; set; }
        public string 状态 { get; set; }
        public string 内容Json { get; set; }
        public string 字段定义Json { get; set; }
    }

    private sealed record FieldDefinition(string Code, string Type, bool Required, List<string> EnumValues);
}
