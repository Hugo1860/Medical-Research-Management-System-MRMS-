namespace ScientificResearch.Models;

public sealed class 权限代理申请
{
    public int 被代理人编号 { get; set; }
    public string 代理范围 { get; set; }
    public DateTime? 开始时间 { get; set; }
    public DateTime? 结束时间 { get; set; }
    public string 备注 { get; set; }
}

public sealed class 权限代理撤销
{
    public long 编号 { get; set; }
    public string 原因 { get; set; }
}

public sealed class 权限代理回收
{
    public int 人员编号 { get; set; }
    public string 原因 { get; set; }
}
