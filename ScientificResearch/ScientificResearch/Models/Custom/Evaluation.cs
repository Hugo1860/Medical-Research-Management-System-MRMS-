using System;
using System.Collections.Generic;

namespace ScientificResearch.Models
{
    public class 科研评价规则保存
    {
        public long? 编号 { get; set; }
        public string 规则名称 { get; set; }
        public int 版本号 { get; set; } = 1;
        public string 适用岗位 { get; set; }
        public string 适用学科 { get; set; }
        public string 适用研究类型 { get; set; }
        public string 评价周期 { get; set; } = "年度";
        public DateTime? 生效日期 { get; set; }
        public DateTime? 失效日期 { get; set; }
        public string 状态 { get; set; } = "草稿";
        public string 规则说明 { get; set; }
    }

    public class 科研评价规则项保存
    {
        public long? 编号 { get; set; }
        public long 规则编号 { get; set; }
        public string 项目代码 { get; set; }
        public string 项目名称 { get; set; }
        public string 项目分类 { get; set; }
        public string 计分依据 { get; set; }
        public decimal 权重 { get; set; } = 1;
        public decimal 最高分 { get; set; } = 100;
        public string 证据类型 { get; set; }
        public int 排序 { get; set; }
        public bool 是否启用 { get; set; } = true;
    }

    public class 科研评价明细保存
    {
        public long 规则项编号 { get; set; }
        public string 证据类型 { get; set; }
        public string 证据编号 { get; set; }
        public string 证据标题 { get; set; }
        public decimal 原始值 { get; set; }
        public string 解释 { get; set; }
        public string 核验状态 { get; set; } = "待核验";
    }

    public class 科研评价生成
    {
        public int 人员编号 { get; set; }
        public long 规则编号 { get; set; }
        public int? 评价年度 { get; set; }
        public DateTime 周期开始日期 { get; set; }
        public DateTime 周期结束日期 { get; set; }
        public string 适用岗位 { get; set; }
        public string 适用学科 { get; set; }
        public string 适用研究类型 { get; set; }
        public string 结果摘要 { get; set; }
        public List<科研评价明细保存> 明细 { get; set; } = new List<科研评价明细保存>();
    }

    public class 科研评价复核
    {
        public long 评价结果编号 { get; set; }
        public string 状态 { get; set; }
        public string 复核意见 { get; set; }
    }

    public class 科研评价申诉保存
    {
        public long 评价结果编号 { get; set; }
        public string 申诉理由 { get; set; }
        public string 期望处理 { get; set; }
        public string 补充证据说明 { get; set; }
    }

    public class 科研评价申诉处理
    {
        public long 申诉编号 { get; set; }
        public string 状态 { get; set; }
        public string 处理意见 { get; set; }
    }

    public class 科研评价奖励保存
    {
        public long? 编号 { get; set; }
        public long 评价结果编号 { get; set; }
        public int 人员编号 { get; set; }
        public string 奖励名称 { get; set; }
        public string 奖励级别 { get; set; }
        public decimal? 奖励金额 { get; set; }
        public string 状态 { get; set; } = "待发放";
        public DateTime? 发放日期 { get; set; }
        public string 发放凭证号 { get; set; }
        public string 财务回写状态 { get; set; } = "待回写";
        public string 财务凭证号 { get; set; }
        public string 财务回写说明 { get; set; }
        public string 发放批次号 { get; set; }
        public string 支付状态 { get; set; } = "待支付";
        public string 支付凭证号 { get; set; }
        public string 支付失败原因 { get; set; }
        public DateTime? 支付时间 { get; set; }
        public string 备注 { get; set; }
    }

    public class 科研绩效动态公式保存
    {
        public int? 编号 { get; set; }
        public long 规则编号 { get; set; }
        public string 指标代码 { get; set; }
        public string 指标名称 { get; set; }
        public string 指标类别 { get; set; }
        public decimal 基础分值 { get; set; } = 100;
        public string 公式表达式 { get; set; }
        public decimal 一作比例 { get; set; } = 0.50m;
        public decimal 通讯比例 { get; set; } = 0.30m;
        public decimal 共一比例 { get; set; } = 0.10m;
        public decimal 参研比例 { get; set; } = 0.10m;
        public string 分区加成配置JSON { get; set; }
        // 新公式必须经评价管理角色显式启用，避免未审核规则直接参与测算。
        public bool 是否启用 { get; set; } = false;
    }

    public class 年度科研绩效测算请求
    {
        public int 评价年度 { get; set; } = 2026;
        public long? 规则编号 { get; set; }
        public int? 人员编号 { get; set; }
    }

    public class 自定义报表查询请求
    {
        public string 模板代码 { get; set; } = "GK-HOSPITAL-ANNUAL";
        public int? 年度 { get; set; } = 2026;
        public int? 部门编号 { get; set; }
    }

    public class 移动协同消息配置保存
    {
        public int? 编号 { get; set; }
        public string 通道类型 { get; set; }
        public string 通道名称 { get; set; }
        public string Webhook地址 { get; set; }
        public string 应用ID { get; set; }
        public string 应用密钥 { get; set; }
        public string 基础配置JSON { get; set; }
        // 外部移动通道必须完成真实配置和握手后才允许启用。
        public bool 是否启用 { get; set; } = false;
    }

    public class 移动待办推送请求
    {
        public string 消息类型 { get; set; } = "待办审批";
        public string 业务模块 { get; set; } = "工作流";
        public string 业务编号 { get; set; }
        public int 接收人编号 { get; set; }
        public string 消息标题 { get; set; }
        public string 消息摘要 { get; set; }
        public string 卡片链接 { get; set; }
    }
}

namespace ScientificResearch.Models
{
    public class 科研评价奖励发放
    {
        public long 奖励编号 { get; set; }
        public string 发放凭证号 { get; set; }
        public DateTime? 发放日期 { get; set; }
        public string 财务回写状态 { get; set; }
        public string 财务凭证号 { get; set; }
        public string 财务回写说明 { get; set; }
        public string 发放批次号 { get; set; }
        public string 支付状态 { get; set; } = "已支付";
        public string 支付凭证号 { get; set; }
        public string 支付失败原因 { get; set; }
        public DateTime? 支付时间 { get; set; }
    }

    public class 科研评价年度批次保存
    {
        public long? 编号 { get; set; }
        public int 评价年度 { get; set; }
        public string 批次名称 { get; set; } = "年度科研绩效评价";
        public int 版本号 { get; set; } = 1;
        public string 状态 { get; set; } = "草稿";
        public DateTime? 公示开始日期 { get; set; }
        public DateTime? 公示结束日期 { get; set; }
        public string 公示说明 { get; set; }
        public string 备注 { get; set; }
    }

    public class 科研评价年度批次操作
    {
        public long 批次编号 { get; set; }
        public string 公示说明 { get; set; }
        public DateTime? 公示开始日期 { get; set; }
        public DateTime? 公示结束日期 { get; set; }
    }

    /// <summary>
    /// 创建工资/财务支付批次时必须显式提供同一人员、同一评价年度的奖励编号。
    /// 该请求只创建待提交批次，不代表已向财务系统付款。
    /// </summary>
    public class 科研评价奖励支付批次创建
    {
        public string 批次号 { get; set; }
        public string 财务系统代码 { get; set; }
        public string 财务请求幂等键 { get; set; }
        public List<long> 奖励编号列表 { get; set; } = new List<long>();
    }

    /// <summary>登记财务/工资系统已受理的奖励支付批次。此接口不调用外部系统。</summary>
    public class 科研评价奖励支付批次提交
    {
        public long 批次编号 { get; set; }
        public string 外部批次号 { get; set; }
        public string 提交幂等键 { get; set; }
        public string 提交说明 { get; set; }
    }

    /// <summary>接收财务系统对单笔奖励的成功或失败回写。</summary>
    public class 科研评价奖励财务回写
    {
        public long 批次编号 { get; set; }
        public long 奖励编号 { get; set; }
        public string 外部流水号 { get; set; }
        public string 回写幂等键 { get; set; }
        public string 状态 { get; set; } = "成功";
        public string 财务凭证号 { get; set; }
        public string 支付凭证号 { get; set; }
        public string 发放凭证号 { get; set; }
        public string 失败原因 { get; set; }
        public string 回写说明 { get; set; }
        public DateTime? 支付时间 { get; set; }
    }

    /// <summary>登记外部财务批次的对账结果；对账只追加审计流水，不伪造支付状态。</summary>
    public class 科研评价奖励支付批次对账
    {
        public long 批次编号 { get; set; }
        public string 外部批次号 { get; set; }
        public string 对账幂等键 { get; set; }
        public int 外部奖励笔数 { get; set; }
        public int 外部成功笔数 { get; set; }
        public int 外部失败笔数 { get; set; }
        public decimal 外部总额 { get; set; }
        public string 对账说明 { get; set; }
    }
}
