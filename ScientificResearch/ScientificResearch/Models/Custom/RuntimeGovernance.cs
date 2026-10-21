using System;

namespace ScientificResearch.Models
{
    public class 流程版本保存
    {
        public long? 编号 { get; set; }
        public int 流程模板编号 { get; set; }
        public int 版本号 { get; set; } = 1;
        public string 状态 { get; set; } = "草稿";
        public string 流程定义 { get; set; } = "{}";
        public string 变更说明 { get; set; }
        public DateTime? 生效时间 { get; set; }
    }

    public class 数据导出任务保存
    {
        public long? 编号 { get; set; }
        public string 导出范围 { get; set; } = "完整数据库";
        public string 状态 { get; set; } = "申请中";
        public string 文件路径 { get; set; }
        public string 文件校验 { get; set; }
        public long? 记录数 { get; set; }
        public string 错误信息 { get; set; }
    }

    public class 流程版本发布
    {
        public long 流程版本编号 { get; set; }
    }
}
