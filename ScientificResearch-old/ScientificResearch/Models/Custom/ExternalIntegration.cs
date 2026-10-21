using System;

namespace ScientificResearch.Models
{
    public class 外部系统保存
    {
        public long? 编号 { get; set; }
        public string 系统代码 { get; set; }
        public string 系统名称 { get; set; }
        public string 系统类型 { get; set; }
        public string 基础地址 { get; set; }
        public string 认证方式 { get; set; } = "无";
        public string 认证引用 { get; set; }
        public bool 是否启用 { get; set; }
        public int 超时秒数 { get; set; } = 15;
        public int 最大重试次数 { get; set; } = 3;
    }

    public class 外部同步任务创建
    {
        public long 外部系统编号 { get; set; }
        public string 业务类型 { get; set; }
        public string 方向 { get; set; } = "推送";
        public string 业务主键 { get; set; }
        public string 幂等键 { get; set; }
        public string 请求负载 { get; set; } = "{}";
        public int? 最大重试次数 { get; set; }
    }

    public class 外部同步任务操作
    {
        public long 编号 { get; set; }
        public string 说明 { get; set; }
    }
}
