using System;

namespace ScientificResearch.Models
{
    public class 科研瓶颈事件保存
    {
        public long? 编号 { get; set; }
        public int? 流程模板编号 { get; set; }
        public int? 项目编号 { get; set; }
        public int? 人员编号 { get; set; }
        public string 事件类型 { get; set; }
        public string 原因代码 { get; set; }
        public string 标题 { get; set; }
        public string 描述 { get; set; }
        public string 严重程度 { get; set; } = "中";
        public string 状态 { get; set; } = "开放";
        public DateTime? 开始时间 { get; set; }
        public DateTime? 解决时间 { get; set; }
        public decimal? 等待天数 { get; set; }
        public int? 责任人编号 { get; set; }
        public string 来源类型 { get; set; }
        public string 来源编号 { get; set; }
    }

    public class 科研资源使用记录保存
    {
        public long? 编号 { get; set; }
        public int? 流程模板编号 { get; set; }
        public int? 项目编号 { get; set; }
        public int? 人员编号 { get; set; }
        public string 资源类型 { get; set; }
        public string 资源名称 { get; set; }
        public DateTime? 申请时间 { get; set; }
        public DateTime? 计划开始时间 { get; set; }
        public DateTime? 实际开始时间 { get; set; }
        public DateTime? 实际结束时间 { get; set; }
        public string 状态 { get; set; } = "申请中";
        public decimal? 数量 { get; set; }
        public decimal? 使用小时 { get; set; }
        public decimal? 等待小时 { get; set; }
        public string 备注 { get; set; }
    }

    public class 科研统计指标口径保存
    {
        public long? 编号 { get; set; }
        public string 指标代码 { get; set; }
        public string 指标名称 { get; set; }
        public string 维度 { get; set; }
        public string 统计单位 { get; set; } = "条";
        public string 口径说明 { get; set; }
        public int 版本号 { get; set; } = 1;
        public string 状态 { get; set; } = "草稿";
    }
}
