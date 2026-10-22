using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ScientificResearch.Models
{

    public class 成果增改<T>
    {
        public T 基本资料 { get; set; }
        public IEnumerable<成果项目> 相关项目 { get; set; }
        public IEnumerable<成果作者> 作者 { get; set; }
        /// <summary>
        /// 需要暂存=true,不暂存=false
        /// </summary>
        public bool IsHold { get; set; }
    }

    public class 论文Filter
    {
        public int? 编号 { get; set; }
        public string Like论文标题 { get; set; }
        public string Like第一作者 { get; set; }
        public string Like刊物名称 { get; set; }
        public int? 年度 { get; set; }
        public bool? Null认领人编号 { get; set; }
    }

    public class 论文认领请求
    {
        public int 论文编号 { get; set; }
        public bool IsHold { get; set; }
    }

    public class 论文期刊识别请求
    {
        public string 刊物名称 { get; set; }
        public string ISSN号 { get; set; }
        public string CN号 { get; set; }
    }

    public class 论文采集结果同步请求
    {
        public long 外部系统编号 { get; set; }
        public string 同步批次号 { get; set; }
        public IEnumerable<论文采集结果同步记录> 论文列表 { get; set; }
    }

    public class Crossref论文采集请求
    {
        public long 外部系统编号 { get; set; }
        public string 关键词 { get; set; }
        public string 作者 { get; set; }
        public string 第一作者 { get; set; }
        public string 所有作者 { get; set; }
        public string 通讯作者 { get; set; }
        public DateTime? 从日期 { get; set; }
        public DateTime? 到日期 { get; set; }
        public int 行数 { get; set; } = 20;
    }

    public class PubMed论文采集请求
    {
        public long 外部系统编号 { get; set; }
        public string 关键词 { get; set; }
        public string 作者 { get; set; }
        public DateTime? 从日期 { get; set; }
        public DateTime? 到日期 { get; set; }
        public int 行数 { get; set; } = 20;
    }

    public class 论文采集结果同步记录
    {
        public string 外部论文编号 { get; set; }
        public string 论文标题 { get; set; }
        public string 作者 { get; set; }
        public string 刊物名称 { get; set; }
        public string ISSN号 { get; set; }
        public string CN号 { get; set; }
        public DateTime? 论文Online日期 { get; set; }
        public DateTime? 论文正式出版日期 { get; set; }
        public int? 年度 { get; set; }
        public int? 作者人数 { get; set; }
        public string 关键字 { get; set; }
        public string 论文摘要 { get; set; }
        public object 原始数据 { get; set; }
    }

    public class 专利代理通知请求
    {
        public int 专利编号 { get; set; }
        public long 外部系统编号 { get; set; }
        public string 事件类型 { get; set; } = "申请";
        public string 说明 { get; set; }
    }


    public class 著作Filter
    {
        public int? 编号 { get; set; }
        public string Like著作名称 { get; set; }
    }

    public class 专利Filter
    {
        public int? 编号 { get; set; }
        public string Like专利名称 { get; set; }
    }

    public class 获奖Filter
    {
        public int? 编号 { get; set; }
        public string Like获奖名称 { get; set; }
    }

    public class 成果总览Filter
    {
        public string Like成果名称 { get; set; }
        public int? 发起人编号 { get; set; }
    }

    public class 科研成果关联新增
    {
        public int 成果类型 { get; set; }
        public int 成果编号 { get; set; }
        public string 关联类型 { get; set; }
        public int? 流程模板编号 { get; set; }
        public int? 项目编号 { get; set; }
        public string 外部对象类型 { get; set; }
        public string 外部对象编号 { get; set; }
        public string 关联说明 { get; set; }
    }

    public class 科研成果作者核验新增
    {
        public int 成果类型 { get; set; }
        public int 成果编号 { get; set; }
        public int 作者编号 { get; set; }
        public int? 作者顺序 { get; set; }
        public string 作者显示姓名 { get; set; }
        public string 单位名称 { get; set; }
        public string ORCID { get; set; }
        public string 身份状态 { get; set; }
        public string 核验说明 { get; set; }
    }

    public class 科研成果治理更新
    {
        public int 成果类型 { get; set; }
        public int 成果编号 { get; set; }
        public string 状态 { get; set; }
        public int? 规范成果类型 { get; set; }
        public int? 规范成果编号 { get; set; }
        public string 治理原因 { get; set; }
        /// <summary>撤稿/更正状态必须提供可追溯来源；系统默认仅登记为待核验，不自动声称已核验。</summary>
        public string 来源类型 { get; set; }
        public string 来源凭证编号 { get; set; }
        public string 来源凭证摘要 { get; set; }
        public string 来源文件路径 { get; set; }
        public string 来源文件SHA256 { get; set; }
        public string 来源系统 { get; set; }
    }

    public class 科研成果状态凭证核验
    {
        public long 状态凭证编号 { get; set; }
        public string 核验结果 { get; set; }
        public string 核验说明 { get; set; }
    }

    public class 科研单位规范保存
    {
        public long? 编号 { get; set; }
        public string 规范名称 { get; set; }
        public string 统一社会信用代码 { get; set; }
        public string 状态 { get; set; } = "启用";
        public string 备注 { get; set; }
    }

    public class 科研单位别名保存
    {
        public long? 编号 { get; set; }
        public long 规范单位编号 { get; set; }
        public string 别名 { get; set; }
        public string 来源 { get; set; }
        public bool 是否启用 { get; set; } = true;
    }

    public class 科研成果作者单位规范
    {
        public int 成果类型 { get; set; }
        public int 成果编号 { get; set; }
        public int 作者编号 { get; set; }
        public long? 规范单位编号 { get; set; }
        public string 单位规范状态 { get; set; } = "已规范";
    }

    public class 论文导入
    {
        #region 属性

        /// <summary>
        /// 论文标题
        /// </summary>
        [DisplayName("Title")]
        public string 论文标题 { get; set; }

        /// <summary>
		/// 作者 ;分割的多个作者,保留第一个作为第一作者,在系统里面找到唯一一个则解析为人员编号放到论文作者里面去;
		/// </summary>
        [DisplayName("Author")]
        public string 作者 { get; set; }

        public string 第一作者 { get; set; }
        public string 所有作者 { get; set; }
        public string 通讯作者 { get; set; }

        public int 作者人数 { get; set; }

        /// <summary>
        /// 年度
        /// </summary>
        [DisplayName("Year")]
        public int? 年度 { get; set; }

        /// <summary>
        /// 关键字
        /// </summary>
        [DisplayName("Keywords")]
        public string 关键字 { get; set; }

        /// <summary>
		/// 论文摘要
		/// </summary>
		[DisplayName("Abstract")]
        public string 论文摘要 { get; set; }

        /// <summary>
        /// 卷号
        /// </summary>
        [DisplayName("Volume")]
        public int? 卷号 { get; set; }

        /// <summary>
		/// 期号
		/// </summary>
		[DisplayName("Issue")]
        public int? 期号 { get; set; }

        /// <summary>
        /// 页码范围
        /// </summary>
        [DisplayName("Pages")]
        public string 页码范围 { get; set; }

        /// <summary>
		/// 
		/// </summary>
        public int? 页码范围起 { get; set; }

        /// <summary>
		/// 
		/// </summary>
        public int? 页码范围止 { get; set; }

        /// <summary>
        /// 刊物名称,
        /// </summary>
        [DisplayName("Journal")]
        public string 刊物名称 { get; set; }

        #endregion
    }

    public class 论文自定义导入
    {
        #region 属性

        [DisplayName("论文标题(必填)")]
        public string 论文标题 { get; set; }
        [DisplayName("论文标题类型")]
        public string 论文标题类型 { get; set; }
        [DisplayName("论文Online日期")]
        public DateTime? 论文Online日期 { get; set; }
        [DisplayName("论文正式出版日期")]
        public DateTime? 论文正式出版日期 { get; set; }
        /// <summary>
		/// 作者 ;分割的多个作者,保留第一个作为第一作者,在系统里面找到唯一一个则解析为人员编号放到论文作者里面去;
		/// </summary>
        [DisplayName("第一作者")]
        public string 作者 { get; set; }
        public string 第一作者 { get; set; }
        public string 所有作者 { get; set; }
        public string 通讯作者 { get; set; }
        [DisplayName("作者人数")]
        public int? 作者人数 { get; set; }
        [DisplayName("他引次数")]
        public int? 他引次数 { get; set; }
        [DisplayName("年度")]
        public int? 年度 { get; set; }
        [DisplayName("收稿日期")]
        public DateTime? 收稿日期 { get; set; }
        [DisplayName("论文所属栏目")]
        public string 论文所属栏目 { get; set; }
        [DisplayName("论文字数(万字)")]
        public float? 论文字数 { get; set; }
        [DisplayName("计划来源")]
        public string 计划来源 { get; set; }
        [DisplayName("论文版面")]
        public string 论文版面 { get; set; }
        [DisplayName("学科门类")]
        public string 学科门类 { get; set; }
        [DisplayName("一级学科")]
        public string 一级学科 { get; set; }
        [DisplayName("二级学科")]
        public string 二级学科 { get; set; }
        [DisplayName("三级学科")]
        public string 三级学科 { get; set; }
        [DisplayName("关键字")]
        public string 关键字 { get; set; }
        [DisplayName("论文摘要")]
        public string 论文摘要 { get; set; }
        [DisplayName("论文类型")]
        public string 论文类型 { get; set; }
        [DisplayName("卷号")]
        public int? 卷号 { get; set; }
        [DisplayName("期号")]
        public int? 期号 { get; set; }
        /// <summary>
        /// 页码范围
        /// </summary>
        [DisplayName("页码范围")]
        public string 页码范围 { get; set; }
        public int? 页码范围起 { get; set; }
        public int? 页码范围止 { get; set; }
        /// <summary>
        /// 刊物名称,
        /// </summary>
        [DisplayName("刊物名称")]
        public string 刊物名称 { get; set; }

        #endregion
    }

    public class 论文网推项目项
    {
        public long 编号 { get; set; }
        public string 外部论文编号 { get; set; }
        public int? 论文编号 { get; set; }
        public string 论文标题 { get; set; }
        public string 作者 { get; set; }
        public string 刊物名称 { get; set; }
        public string ISSN号 { get; set; }
        public string CN号 { get; set; }
        public string 中科院分区 { get; set; }
        public string 中科院大类学科 { get; set; }
        public bool? 是否Top期刊 { get; set; }
        public decimal? 影响因子 { get; set; }
        public string JCR分区 { get; set; }
        public int? 推荐认领人编号 { get; set; }
        public string 推荐认领人姓名 { get; set; }
        public decimal? 消歧置信度 { get; set; }
        public string 消歧命中依据 { get; set; }
        public string 网推状态 { get; set; }
        public string 状态 { get; set; }
        public DateTime? 最后同步时间 { get; set; }
    }

    public class 论文网推巡检请求
    {
        public long? 外部系统编号 { get; set; }
    }

    public class 一键确认认领网推请求
    {
        public long 采集结果编号 { get; set; }
        public int? 认领人编号 { get; set; }
        public string 作者位次 { get; set; } = "第一作者";
        public string 认领说明 { get; set; }
    }

    public class 机构别名配置项
    {
        public long 编号 { get; set; }
        public long 规范单位编号 { get; set; }
        public string 规范单位名称 { get; set; }
        public string 别名 { get; set; }
        public string 来源 { get; set; }
        public bool 是否启用 { get; set; }
    }

    public class 学者消歧配置项
    {
        public int 编号 { get; set; }
        public int 人员编号 { get; set; }
        public string 工号 { get; set; }
        public string 姓名 { get; set; }
        public string 英文拼音署名 { get; set; }
        public string ORCID { get; set; }
        public string 科室名称 { get; set; }
        public bool 是否主要署名 { get; set; }
    }

    public class 添加机构别名请求
    {
        public string 别名 { get; set; }
        public string 来源 { get; set; } = "科管录入";
    }

    public class 添加学者消歧请求
    {
        public int 人员编号 { get; set; }
        public string 英文拼音署名 { get; set; }
        public string ORCID { get; set; }
        public string 科室名称 { get; set; }
    }
}

namespace ScientificResearch.Models
{
    /// <summary>成果转化申请、合同收益和收益分配。</summary>
    public class 科研成果转化保存
    {
        public long? 编号 { get; set; }
        public int 成果类型 { get; set; }
        public int 成果编号 { get; set; }
        public string 转化名称 { get; set; }
        public string 转化方式 { get; set; } = "技术转让";
        public string 合作单位 { get; set; }
        public string 合同编号 { get; set; }
        public DateTime? 合同签署日期 { get; set; }
        public decimal 合同金额 { get; set; }
        public string 申请说明 { get; set; }
    }

    public class 科研成果转化审批
    {
        public long 转化编号 { get; set; }
        public string 状态 { get; set; }
        public string 审批意见 { get; set; }
    }

    public class 科研成果转化收益保存
    {
        public long? 编号 { get; set; }
        public long 转化编号 { get; set; }
        public string 收益类型 { get; set; } = "合同款";
        public decimal 计划金额 { get; set; }
        public decimal 实际金额 { get; set; }
        public DateTime? 收款日期 { get; set; }
        public string 收款凭证号 { get; set; }
        public string 备注 { get; set; }
    }

    public class 科研成果转化收益分配保存
    {
        public long? 编号 { get; set; }
        public long 收益编号 { get; set; }
        public int 人员编号 { get; set; }
        public decimal 分配比例 { get; set; }
        public decimal 分配金额 { get; set; }
        public string 备注 { get; set; }
    }

    public class 科研成果转化财务回写
    {
        public long 分配编号 { get; set; }
        public string 财务回写状态 { get; set; }
        public string 财务凭证号 { get; set; }
        public string 财务回写说明 { get; set; }
    }
}
