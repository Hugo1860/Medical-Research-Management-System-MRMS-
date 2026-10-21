using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ScientificResearch.Models
{
    public class 报销Filter
    {
        public int 流程模板编号 { get; set; }
        public int 项目编号 { get; set; }
    }

    public class 报销详情Filter
    {
        public int 报销编号 { get; set; }
    }

    public class 报销增改
    {
        public 报销 基本资料 { get; set; }
        public IEnumerable<报销详情> 报销详情列表 { get; set; }
        public bool IsHold { get; set; }
    }

    public class 预算异常对账处理请求
    {
        public int 流程模板编号 { get; set; }
        public int 项目编号 { get; set; }
        public string 异常类型 { get; set; }
        public string 状态 { get; set; }
        public int? 责任人编号 { get; set; }
        public string 处理说明 { get; set; }
    }

    public class 项目经费台账新增
    {
        public int 流程模板编号 { get; set; }
        public int 项目编号 { get; set; }
        public string 业务类型 { get; set; }
        public decimal 金额 { get; set; }
        public string 状态 { get; set; }
        public string 业务编号 { get; set; }
        public DateTime? 交易日期 { get; set; }
        public DateTime? 付款日期 { get; set; }
        public string 收款方 { get; set; }
        public string 凭证路径 { get; set; }
        public string 备注 { get; set; }
    }

    public class 财务流水同步记录
    {
        public string 外部单据号 { get; set; }
        public int 流程模板编号 { get; set; }
        public int 项目编号 { get; set; }
        public string 业务类型 { get; set; }
        public decimal 金额 { get; set; }
        public string 状态 { get; set; }
        public DateTime? 交易日期 { get; set; }
        public DateTime? 付款日期 { get; set; }
        public string 收款方 { get; set; }
        public string 凭证路径 { get; set; }
        public string 备注 { get; set; }
    }

    public class 财务流水同步请求
    {
        public long 外部系统编号 { get; set; }
        public string 同步批次号 { get; set; }
        public IEnumerable<财务流水同步记录> 流水列表 { get; set; }
    }

    public class 项目合同付款节点新增
    {
        public int 流程模板编号 { get; set; }
        public int 项目编号 { get; set; }
        public int 节点序号 { get; set; }
        public string 节点名称 { get; set; }
        public decimal 约定金额 { get; set; }
        public DateTime? 到期日期 { get; set; }
        public string 状态 { get; set; }
        public decimal 已付款金额 { get; set; }
        public string 合同编号 { get; set; }
        public string 备注 { get; set; }
    }

    public class 按到账维护项目预算项
    {
        public string 项目支出类型 { get; set; }
        public string 项目支出内容 { get; set; }
        public string 财务科目 { get; set; }
        public decimal 批准经费 { get; set; }
        public string 编制依据 { get; set; }
    }

    public class 按到账维护项目预算请求
    {
        public int 项目编号 { get; set; }
        public int? 到账编号 { get; set; }
        public string 到账批次 { get; set; }
        public IEnumerable<按到账维护项目预算项> 预算明细 { get; set; }
    }

    public class Hrp中间库协同请求
    {
        public int? 项目编号 { get; set; }
        public string 动作 { get; set; } = "push_only";
    }

    /// <summary>科研发票人工录入和查重请求。系统未接入 OCR 时必须由经办人录入发票号码、金额和供应商。</summary>
    public class 科研经费发票请求
    {
        public long? 编号 { get; set; }
        public int 流程模板编号 { get; set; }
        public int 项目编号 { get; set; }
        public int? 报销编号 { get; set; }
        public string 发票号码 { get; set; }
        public string 发票代码 { get; set; }
        public DateTime? 开票日期 { get; set; }
        public decimal 金额 { get; set; }
        public string 供应商名称 { get; set; }
        public string 供应商税号 { get; set; }
        public string 发票类型 { get; set; }
        public string 附件路径 { get; set; }
        public bool 允许重复 { get; set; }
    }

    public class 科研经费发票复核请求
    {
        public long 编号 { get; set; }
        public string 查重状态 { get; set; }
        public string 重复说明 { get; set; }
    }

    public class 项目经费预算调整申请请求
    {
        public int 流程模板编号 { get; set; }
        public int 项目编号 { get; set; }
        public string 申请原因 { get; set; }
        public IEnumerable<按到账维护项目预算项> 申请预算 { get; set; }
    }

    public class 项目经费预算调整审批请求
    {
        public long 申请编号 { get; set; }
        public bool 是否批准 { get; set; }
        public string 审批意见 { get; set; }
    }

    public class 项目经费年度结算请求
    {
        public int 流程模板编号 { get; set; }
        public int 项目编号 { get; set; }
        public int 结算年度 { get; set; }
        public string 结算说明 { get; set; }
    }

    public class 项目付款回写请求
    {
        public long 台账编号 { get; set; }
        public string 状态 { get; set; }
        public DateTime? 付款日期 { get; set; }
        public string 凭证路径 { get; set; }
        public string 备注 { get; set; }
    }
}
