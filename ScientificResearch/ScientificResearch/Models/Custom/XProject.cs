using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace ScientificResearch.Models
{
    public class 横向项目申请增改
    {
        public 横向项目 基本资料 { get; set; }
        public IEnumerable<项目参与人> 参与人列表 { get; set; }
        /// <summary>
        /// 对横向项目来说,配套经费不用管,=0就是了
        /// </summary>
        public IEnumerable<项目经费预算> 经费预算列表 { get; set; }
        /// <summary>
        /// 需要暂存=true,不暂存=false
        /// </summary>
        public bool IsHold { get; set; }
    }
    public class 横向项目Filter
    {
        public string Like合同编号 { get; set; }
        public string Like合同名称 { get; set; }
        public int? 发起人编号 { get; set; }

    }

    public class 横向项目变更增改
    {
        public 横向项目变更 基本资料 { get; set; }
        /// <summary>
        /// 需要暂存=true,不暂存=false
        /// </summary>
        public bool IsHold { get; set; }
    }

    public class 横向项目变更Filter
    {
        public string Like合同编号 { get; set; }
        public string Like合同名称 { get; set; }
        public string Like变更事由 { get; set; }
    }

    public class 横向项目结题增改
    {
        public int 横向项目申请编号 { get; set; }
        public int 申请发起人编号 { get; set; }
    }

    public class 横向项目结题完善
    {
        public 横向项目结题 基本资料 { get; set; }
        /// <summary>
        /// 需要暂存=true,不暂存=false
        /// </summary>
        public bool IsHold { get; set; }
    }

    public class 横向项目结题Filter
    {
        public string Like合同编号 { get; set; }
        public string Like合同名称 { get; set; }
    }

    /// <summary>横向项目延期、暂停或终止申请。附件只记录已上传文件路径，不代表外部合同已生效。</summary>
    public class 横向项目生命周期申请新增
    {
        public int 横向项目编号 { get; set; }
        public string 事件类型 { get; set; }
        public DateTime? 新合同结束日期 { get; set; }
        public string 申请原因 { get; set; }
        public string 附件路径 { get; set; }
    }

    public class 横向项目生命周期审核
    {
        public long 编号 { get; set; }
        public bool 通过 { get; set; }
        public string 审核意见 { get; set; }
    }

    public class 横向项目研究报告新增
    {
        public int 横向项目编号 { get; set; }
        public string 报告类型 { get; set; }
        public int 报告年度 { get; set; }
        public string 报告摘要 { get; set; }
        public string 报告正文 { get; set; }
        public string 文件路径 { get; set; }
        public bool 提交审核 { get; set; }
    }

    public class 横向项目研究报告审核
    {
        public long 编号 { get; set; }
        public bool 通过 { get; set; }
        public string 审核意见 { get; set; }
    }

    public class 横向项目档案材料新增
    {
        public int 横向项目编号 { get; set; }
        public string 材料类型 { get; set; }
        public string 材料标题 { get; set; }
        public string 文件路径 { get; set; }
        public string 文件校验和 { get; set; }
        public int 版本号 { get; set; } = 1;
        public bool 是否必需 { get; set; }
    }
}

