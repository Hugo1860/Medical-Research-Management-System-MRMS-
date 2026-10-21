using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ScientificResearch.Models
{
    public class 角色中的人员
    {
        public int 编号 { get; set; }
        public string 工号 { get; set; }
        public string 姓名 { get; set; }
        public int 部门编号 { get; set; }
        public string 部门名称 { get; set; }
    }

    public class 某角色中的人员
    {
        public int 角色编号 { get; set; }
        public IEnumerable<int> 人员编号列表 { get; set; }
    }

    public class 增删角色中的人员
    {
        public int 角色编号 { get; set; }
        public IEnumerable<int> 将增加的人员编号列表 { get; set; }
        public IEnumerable<int> 将删除的人员编号列表 { get; set; }
    }

    public class 某角色的权限
    {
        public int 角色编号 { get; set; }
        public IEnumerable<int> 权限编号列表 { get; set; }
    }

    public class 字段授权规则请求
    {
        public long 编号 { get; set; }
        public int 角色编号 { get; set; }
        public string 资源 { get; set; }
        public string 字段 { get; set; }
        public bool 是否可读 { get; set; }
        public bool 是否可写 { get; set; }
    }

    public class 权限变更通知已读请求
    {
        public IEnumerable<long> 编号列表 { get; set; }
    }
}
