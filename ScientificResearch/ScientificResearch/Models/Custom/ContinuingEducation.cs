using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ScientificResearch.Models;

public sealed class 进修年度计划保存
{
    public long? 编号 { get; set; }
    [Required] public int 年度 { get; set; }
    [Required] public int 部门编号 { get; set; }
    [Required] public string 专业方向 { get; set; }
    [Range(0, 100000)] public int 计划人数 { get; set; }
    [Range(0, 1000000000)] public decimal 预算总额 { get; set; }
    public string 备注 { get; set; }
}

public sealed class 进修申请保存
{
    public long? 编号 { get; set; }
    public long? 年度计划编号 { get; set; }
    [Required] public int 部门编号 { get; set; }
    [Required] public string 目标医院 { get; set; }
    [Required] public string 进修专业 { get; set; }
    [Required] public DateTime 进修开始日期 { get; set; }
    [Required] public DateTime 进修结束日期 { get; set; }
    [Range(0, 1000000000)] public decimal 培训费 { get; set; }
    [Range(0, 1000000000)] public decimal 交通费 { get; set; }
    [Range(0, 1000000000)] public decimal 住宿费 { get; set; }
    [Range(0, 1000000000)] public decimal 补助费 { get; set; }
    public string 经费来源 { get; set; }
    public string 申请说明 { get; set; }
    public string 附件路径 { get; set; }
    public string 备注 { get; set; }
}

public sealed class 进修审批请求
{
    [Required] public long 待办编号 { get; set; }
    [Required] public string 结果 { get; set; }
    public string 审批意见 { get; set; }
}

public sealed class 进修流程节点保存
{
    public int 序号 { get; set; }
    [Required] public string 名称 { get; set; }
    [Required] public string 类型 { get; set; } = "审批";
    public string 角色 { get; set; }
    public bool 是否会签 { get; set; }
    public bool 是否启用 { get; set; } = true;
}

public sealed class 进修流程配置保存
{
    public long? 编号 { get; set; }
    [Required] public string 名称 { get; set; }
    public string 变更说明 { get; set; }
    [Required] public IEnumerable<进修流程节点保存> 节点 { get; set; } = Array.Empty<进修流程节点保存>();
}

public sealed class 进修预算操作请求
{
    [Required] public long 申请编号 { get; set; }
}

public sealed class 进修编号请求
{
    public long? 编号 { get; set; }
    public long? 申请编号 { get; set; }
    public long? 计划编号 { get; set; }
    public long? 配置编号 { get; set; }
}
