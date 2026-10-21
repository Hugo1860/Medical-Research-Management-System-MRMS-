using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace ScientificResearch.Models
{
    public class 纵向项目申报增改
    {
        public 纵向项目申报 基本资料 { get; set; }
        public IEnumerable<项目参与人> 参与人列表 { get; set; }
        /// <summary>可选的申报计划。正式提交时若传入将校验计划发布窗口和申报类别。</summary>
        public long? 申报计划编号 { get; set; }
        /// <summary>院级项目专用申报批次；与纵向计划二选一。</summary>
        public long? 院级批次编号 { get; set; }
        /// <summary>
        /// 需要暂存=true,不暂存=false
        /// </summary>
        public bool IsHold { get; set; }
    }

    public class 纵向项目申报计划保存
    {
        public long? 编号 { get; set; }
        public int 计划年度 { get; set; }
        public int 申报类别 { get; set; } = 1;
        public string 计划名称 { get; set; }
        public DateTime 发布开始日期 { get; set; }
        public DateTime 截止日期 { get; set; }
        public string 计划说明 { get; set; }
        public string 状态 { get; set; } = "草稿";
    }

    public class 院级项目申报批次保存
    {
        public long? 编号 { get; set; }
        public int 计划年度 { get; set; }
        public string 批次名称 { get; set; }
        public string 项目类别 { get; set; } = "院级科研项目";
        public DateTime 发布开始日期 { get; set; }
        public DateTime 截止日期 { get; set; }
        public int 个人申报限额 { get; set; } = 3;
        public int? 批次总限额 { get; set; }
        public string 资格规则 { get; set; } = "{}";
        public string 评审方案 { get; set; } = "{}";
        public DateTime? 评审开始日期 { get; set; }
        public DateTime? 评审截止日期 { get; set; }
        public string 状态 { get; set; } = "草稿";
        public string 批次说明 { get; set; }
    }

    public class 伦理审批同步记录
    {
        public string 外部审批编号 { get; set; }
        public int 流程模板编号 { get; set; }
        public int 项目编号 { get; set; }
        public string 状态 { get; set; }
        public string 批文编号 { get; set; }
        public DateTime? 批准时间 { get; set; }
        public DateTime? 有效期至 { get; set; }
        public string 批文路径 { get; set; }
        public DateTime? 外部更新时间 { get; set; }
        public string 备注 { get; set; }
    }

    public class 伦理审批同步请求
    {
        public long 外部系统编号 { get; set; }
        public string 同步批次号 { get; set; }
        public IEnumerable<伦理审批同步记录> 审批列表 { get; set; }
    }

    public class 项目申报预检查项
    {
        public string 代码 { get; set; }
        public string 名称 { get; set; }
        public bool 通过 { get; set; }
        public bool 阻断提交 { get; set; }
        public string 提示 { get; set; }
    }

    public class 项目申报预检查结果
    {
        public bool 通过 { get; set; }
        public bool 材料完整 { get; set; }
        public long? 记录编号 { get; set; }
        public long? 申报计划编号 { get; set; }
        public IEnumerable<项目申报预检查项> 检查项 { get; set; }
    }

    public class 纵向项目任务书生成请求
    {
        public int 纵向项目立项编号 { get; set; }
        public string 任务书标题 { get; set; }
        /// <summary>可选 JSON；未提供时由立项、计划、预算和预期成果生成。</summary>
        public string 任务书内容 { get; set; }
        public string 文件路径 { get; set; }
        public string 文件校验和 { get; set; }
        public string 生成说明 { get; set; }
        public bool 上传修订 { get; set; }
    }

    public class 纵向项目任务书签署请求
    {
        public long 任务书编号 { get; set; }
        public int 签署人编号 { get; set; }
        public string 签署角色 { get; set; }
    }

    public class 纵向项目任务书确认请求
    {
        public long 任务书编号 { get; set; }
        public long 签署记录编号 { get; set; }
        public string 签署说明 { get; set; }
    }

    /// <summary>
    /// 请求把任务书提交给经批准的外部电子签章服务。
    /// 当前系统只实现契约门禁，不在本地伪造签章或法律效力。
    /// </summary>
    public class 纵向项目任务书电子签章请求
    {
        public long 任务书编号 { get; set; }
        public string 幂等键 { get; set; }
    }

    /// <summary>
    /// 请求把项目档案提交给经批准的正式电子档案服务。
    /// 本地文件归档与正式电子档案托管是两个不同的能力边界。
    /// </summary>
    public class 纵向项目正式电子档案提交请求
    {
        public int 纵向项目立项编号 { get; set; }
        public string 幂等键 { get; set; }
    }

    public class 纵向项目档案清单项保存
    {
        public int 纵向项目立项编号 { get; set; }
        public string 材料代码 { get; set; }
        public string 材料名称 { get; set; }
        public bool 是否必需 { get; set; } = true;
        public string 要求说明 { get; set; }
        public string 状态 { get; set; }
        public string 核验说明 { get; set; }
    }

    public class 项目评审意见新增
    {
        public int 流程模板编号 { get; set; }
        public int 项目编号 { get; set; }
        public int? 步骤编号 { get; set; }
        public string 意见类型 { get; set; }
        public string 意见内容 { get; set; }
        public bool 是否公开给申请人 { get; set; } = true;
    }

    public class 未获资助复盘新增
    {
        public int 纵向项目申报编号 { get; set; }
        public string 原因分类 { get; set; }
        public string 复盘结论 { get; set; }
        public string 改进措施 { get; set; }
        public int? 负责人编号 { get; set; }
    }

    public class 项目生命周期预检查项
    {
        public string 代码 { get; set; }
        public string 名称 { get; set; }
        public bool 通过 { get; set; }
        public bool 阻断提交 { get; set; }
        public string 提示 { get; set; }
    }

    public class 项目生命周期预检查结果
    {
        public bool 通过 { get; set; }
        public string 阶段 { get; set; }
        public int 项目编号 { get; set; }
        public IEnumerable<项目生命周期预检查项> 检查项 { get; set; }
    }

    public class 纵向项目总览Filter
    {
        public int? 分类 { get; set; }
        public string Like项目中文名称 { get; set; }
        public string Like项目英文名称 { get; set; }
        public int? 发起人编号 { get; set; }
    }

    public class 纵向项目申报Filter
    {
        public int? 编号 { get; set; }
        public int? 分类 { get; set; }
        public string Like项目名称 { get; set; }
        public bool? 是否立项 { get; set; }
    }

    public class 纵向项目立项增改
    {
        public 纵向项目立项 基本资料 { get; set; }
        public IEnumerable<项目参与人> 参与人列表 { get; set; }
        public IEnumerable<项目计划> 计划列表 { get; set; }
        public IEnumerable<项目经费预算> 经费预算列表 { get; set; }
        public IEnumerable<项目预期成果> 预期成果列表 { get; set; }
        /// <summary>
        /// 需要暂存=true,不暂存=false
        /// </summary>
        public bool IsHold { get; set; }
    }

    public class 纵向项目立项Filter
    {
        //public int? 编号 { get; set; }
        /// <summary>
        /// 分类: 1-科研项目 2-重点学科 3-人才培养
        /// </summary>
        public int? 分类 { get; set; }
        public string Like项目中文名称 { get; set; }
        public string Like项目英文名称 { get; set; }
        public int? 发起人编号 { get; set; }
    }

    public class 发起纵向项目中检
    {
        public int 纵向项目立项编号 { get; set; }
        public int 立项登记人编号 { get; set; }
    }

    public class 纵向项目中检完善
    {
        public 纵向项目中检 基本资料 { get; set; }
        /// <summary>
        /// 需要暂存=true,不暂存=false
        /// </summary>
        public bool IsHold { get; set; }
    }

    public class 纵向项目中检Filter
    {
        public int? 编号 { get; set; }
        public int? 分类 { get; set; }
        public string Like项目中文名称 { get; set; }
        public string Like项目英文名称 { get; set; }
        public int? 发起人编号 { get; set; }
    }

    public class 纵向项目变更增改
    {
        public 纵向项目变更 基本资料 { get; set; }
        /// <summary>
        /// 需要暂存=true,不暂存=false
        /// </summary>
        public bool IsHold { get; set; }
    }

    /// <summary>
    /// 注意:变更搜索条件是针对"关联的立项项目当前的信息"来的,而不是"当时的变更内容"或"已变更过的旧的立项内容";
    /// </summary>
    public class 纵向项目变更Filter
    {
        public int? 编号 { get; set; }
        public int? 分类 { get; set; }
        public string Like项目中文名称 { get; set; }
        public string Like项目英文名称 { get; set; }

        public string Like变更事由 { get; set; }
        public int? 发起人编号 { get; set; }

    }

    public class 发起纵向项目结题
    {
        public int 纵向项目立项编号 { get; set; }
        public int 立项发起人编号 { get; set; }
    }

    public class 纵向项目结题完善
    {
        public 纵向项目结题 基本资料 { get; set; }
        /// <summary>
        /// 需要暂存=true,不暂存=false
        /// </summary>
        public bool IsHold { get; set; }
    }

    public class 纵向项目结题Filter
    {
        public int? 编号 { get; set; }
        public int? 分类 { get; set; }
        public string Like项目中文名称 { get; set; }
        public string Like项目英文名称 { get; set; }
        public int? 发起人编号 { get; set; }

    }

    public class 纵向项目生命周期申请新增
    {
        public int 纵向项目立项编号 { get; set; }
        public string 事件类型 { get; set; }
        public DateTime? 新计划完成日期 { get; set; }
        public string 申请原因 { get; set; }
        public string 附件路径 { get; set; }
    }

    public class 纵向项目生命周期审核
    {
        public long 编号 { get; set; }
        public bool 通过 { get; set; }
        public string 审核意见 { get; set; }
    }

    public class 纵向项目研究报告新增
    {
        public int 纵向项目立项编号 { get; set; }
        public string 报告类型 { get; set; }
        public int 报告年度 { get; set; }
        public string 报告摘要 { get; set; }
        public string 报告正文 { get; set; }
        public string 文件路径 { get; set; }
        public bool 提交审核 { get; set; }
    }

    public class 纵向项目研究报告审核
    {
        public long 编号 { get; set; }
        public bool 通过 { get; set; }
        public string 审核意见 { get; set; }
    }

    public class 纵向项目档案材料新增
    {
        public int 纵向项目立项编号 { get; set; }
        public string 材料类型 { get; set; }
        public string 材料标题 { get; set; }
        public string 文件路径 { get; set; }
        public string 文件校验和 { get; set; }
        public int 版本号 { get; set; } = 1;
        public bool 是否必需 { get; set; }
    }

    public class 一键建立配套项目请求
    {
        public int 来源项目编号 { get; set; }
        public decimal? 配套经费 { get; set; }
        public string 备注 { get; set; }
    }

    public class 一键建立结余项目请求
    {
        public int 来源项目编号 { get; set; }
        public decimal? 结余金额 { get; set; }
        public string 备注 { get; set; }
    }

    public class 过程预警记录项
    {
        public int 编号 { get; set; }
        public string 项目类型 { get; set; }
        public int 项目编号 { get; set; }
        public string 项目名称 { get; set; }
        public int 负责人编号 { get; set; }
        public string 负责人姓名 { get; set; }
        public string 预警类型 { get; set; }
        public string 预警级别 { get; set; }
        public DateTime 节点截止日期 { get; set; }
        public int 剩余天数 { get; set; }
        public decimal 当前预算执行率 { get; set; }
        public string 预警提示内容 { get; set; }
        public string 处理状态 { get; set; }
        public string 通知渠道 { get; set; }
        public DateTime 最近巡检时间 { get; set; }
        public DateTime 建立时间 { get; set; }
    }

    public class 确认过程预警请求
    {
        public int 预警编号 { get; set; }
        public string 处理说明 { get; set; }
    }

    public class 登记伦理批文续展请求
    {
        public int 项目编号 { get; set; }
        public string 新批文编号 { get; set; }
        public string 申请类型 { get; set; } = "延期续展";
        public DateTime 新有效期至 { get; set; }
        public string 批件文件路径 { get; set; }
        public string 变更事由与说明 { get; set; }
        public int? 登记人编号 { get; set; }
    }
}
