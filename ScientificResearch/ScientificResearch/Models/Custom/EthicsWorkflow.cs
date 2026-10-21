using System;
using System.ComponentModel.DataAnnotations;

namespace ScientificResearch.Models;

public sealed class 伦理申请保存
{
    public int? 编号 { get; set; }
    [Required] public int 流程模板编号 { get; set; }
    [Required] public int 项目编号 { get; set; }
    [Required] public string 申请类型 { get; set; }
    [Required] public string 申请标题 { get; set; }
    [Required] public string 研究方案摘要 { get; set; }
    public string 受试者风险等级 { get; set; }
    public string 知情同意说明 { get; set; }
    public string 附件路径 { get; set; }
    public string 备注 { get; set; }
}

public sealed class 伦理申请受理
{
    [Required] public long 申请编号 { get; set; }
    [Required] public string 结果 { get; set; }
    public string 退回原因 { get; set; }
}

public sealed class 伦理委员意见保存
{
    [Required] public long 申请编号 { get; set; }
    public long? 会议编号 { get; set; }
    [Required] public string 意见结论 { get; set; }
    public bool 是否利益冲突 { get; set; }
    public string 意见内容 { get; set; }
    [Range(0, 100)] public decimal? 评分 { get; set; }
}

public sealed class 伦理决定保存
{
    [Required] public long 申请编号 { get; set; }
    [Required] public string 决定 { get; set; }
    [Required] public string 决定说明 { get; set; }
    public string 批件编号 { get; set; }
    public DateTime? 有效期至 { get; set; }
    public string 决定书路径 { get; set; }
}

public sealed class 伦理持续记录保存
{
    [Required] public long 申请编号 { get; set; }
    [Required] public string 记录类型 { get; set; }
    public DateTime? 发生日期 { get; set; }
    public DateTime? 下次审查日期 { get; set; }
    public string 严重程度 { get; set; }
    [Required] public string 事件说明 { get; set; }
    public string 处理结论 { get; set; }
    public string 附件路径 { get; set; }
}

public sealed class 伦理持续记录审核
{
    [Required] public long 记录编号 { get; set; }
    [Required] public string 结果 { get; set; }
    public string 处理结论 { get; set; }
}

public sealed class 伦理会议保存
{
    public long? 编号 { get; set; }
    [Required] public string 会议序号 { get; set; }
    [Required] public DateTime 会议时间 { get; set; }
    public string 会议地点 { get; set; }
    public int? 主持人编号 { get; set; }
    public string 状态 { get; set; } = "计划中";
    public int 法定人数 { get; set; } = 3;
    public string 表决规则 { get; set; } = "有效意见过半数";
    public bool 是否匿名表决 { get; set; } = true;
    public string 议程说明 { get; set; }
    public string 议程路径 { get; set; }
    public string 备注 { get; set; }
}

public sealed class 伦理会议议程保存
{
    [Required] public long 会议编号 { get; set; }
    [Required] public long 申请编号 { get; set; }
    public int 排序号 { get; set; } = 1;
    public string 议题说明 { get; set; }
    public string 状态 { get; set; } = "已排期";
}

/// <summary>伦理委员会正式指派、回避和出席登记。</summary>
public sealed class 伦理会议委员保存
{
    [Required] public long 会议编号 { get; set; }
    [Required] public int 委员编号 { get; set; }
    public string 委员角色 { get; set; } = "委员";
    public string 状态 { get; set; } = "正式指派";
    public bool 是否回避 { get; set; }
    public string 回避原因 { get; set; }
    public string 出席状态 { get; set; } = "未确认";
}

/// <summary>由会议主席确认本次会议具备正式表决资格。</summary>
public sealed class 伦理会议主席确认
{
    [Required] public long 会议编号 { get; set; }
    public string 确认说明 { get; set; }
}

public sealed class 伦理会议纪要保存
{
    [Required] public long 会议编号 { get; set; }
    [Required] public string 纪要正文 { get; set; }
    public string 出席委员摘要 { get; set; }
    public string 决议摘要 { get; set; }
    public string 附件路径 { get; set; }
    public string 状态 { get; set; } = "草稿";
}

public sealed class 伦理电子档案保存
{
    public long? 申请编号 { get; set; }
    public long? 会议编号 { get; set; }
    public long? 决定编号 { get; set; }
    [Required] public string 档案类型 { get; set; }
    public string 文件名称 { get; set; }
    [Required] public string 文件路径 { get; set; }
    public string 文件校验值 { get; set; }
    public string 备注 { get; set; }
}
