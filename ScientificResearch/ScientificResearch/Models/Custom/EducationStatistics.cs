using System;

namespace ScientificResearch.Models;

public sealed class 教育部统计任务保存
{
    public long? 编号 { get; set; }
    public int 统计年度 { get; set; }
    public string 任务代码 { get; set; }
    public string 任务名称 { get; set; }
    public DateTime? 截止日期 { get; set; }
    public DateTime? 数据截止日期 { get; set; }
    public string 状态 { get; set; } = "草稿";
    public long? 当前版本编号 { get; set; }
    public string 说明 { get; set; }
}

public sealed class 教育部统计版本保存
{
    public long? 编号 { get; set; }
    public long 任务编号 { get; set; }
    public int 版本号 { get; set; } = 1;
    public string 模板代码 { get; set; }
    public string 模板名称 { get; set; }
    public string 字段定义 { get; set; } = "[]";
    public string 状态 { get; set; } = "草稿";
    public string 变更说明 { get; set; }
}

public sealed class 教育部统计数据包导入
{
    public long? 编号 { get; set; }
    public long 任务编号 { get; set; }
    public long 版本编号 { get; set; }
    public string 包名称 { get; set; }
    public string 文件类型 { get; set; }
    public string 文件路径 { get; set; }
    public string 文件校验和 { get; set; }
    /// <summary>用户实际上传并解析出的 JSON 数组。只登记文件元数据时留空，状态保持“待导入”。</summary>
    public string 数据内容 { get; set; }
    public string 说明 { get; set; }
}

public sealed class 教育部统计字段调整
{
    public long 数据包编号 { get; set; }
    public int 记录序号 { get; set; }
    public string 记录标识 { get; set; }
    public string 字段代码 { get; set; }
    public string 新值 { get; set; }
    public string 调整原因 { get; set; }
    public string 请求编号 { get; set; }
}

public sealed class 教育部统计导出任务保存
{
    public long? 编号 { get; set; }
    public long 任务编号 { get; set; }
    public long 版本编号 { get; set; }
    public long? 数据包编号 { get; set; }
    public string 导出格式 { get; set; } = "xlsx";
    public string 状态 { get; set; } = "申请中";
    public string 文件路径 { get; set; }
    public string 文件校验和 { get; set; }
    public int? 记录数 { get; set; }
    public string 错误信息 { get; set; }
}
