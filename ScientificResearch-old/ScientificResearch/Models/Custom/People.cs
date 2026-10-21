using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ScientificResearch.Models
{
    public class 任职增改
    {
        public 任职 基本资料 { get; set; }
        /// <summary>
        /// 需要暂存=true,不暂存=false
        /// </summary>
        public bool IsHold { get; set; }
    }

    public class 人员Filter
    {
        public int? 部门编号 { get; set; }
        //public string Like姓名 { get; set; }
        public bool? 是否启用 { get; set; }
        //public string Like部门名称 { get; set; }
        /// <summary>
        /// 同时对姓名和部门名称做搜索
        /// </summary>
        public string LikeAllName { get; set; }
    }

    public class HR人员同步记录
    {
        public string 外部人员编号 { get; set; }
        public string 工号 { get; set; }
        public string 姓名 { get; set; }
        public string 部门名称 { get; set; }
        public string 岗位名称 { get; set; }
        public string 手机 { get; set; }
        public string 邮箱 { get; set; }
        public bool 是否在职 { get; set; } = true;
        public DateTime? 外部更新时间 { get; set; }
    }

    public class HR人员同步请求
    {
        public long 外部系统编号 { get; set; }
        public string 同步批次号 { get; set; }
        public bool 是否全量 { get; set; }
        public IEnumerable<HR人员同步记录> 人员列表 { get; set; }
    }

    public class 任职Filter
    {
        public int? 编号 { get; set; }
        public int? 任职人员编号 { get; set; }
        public string Like任职团体名称 { get; set; }
        public string Like所任职务 { get; set; }
    }

    public class 人员科研能力保存
    {
        public int 人员编号 { get; set; }
        public string 研究专长 { get; set; }
        public string 研究方向标签 { get; set; }
        public string 技术服务能力 { get; set; }
        public string 合作意愿 { get; set; }
        public string 合作说明 { get; set; }
        /// <summary>可接受合作的时间段或排班说明，例如“周二/周四 18:00 后”。</summary>
        public string 可用时间 { get; set; }
        public decimal 每周可用小时 { get; set; }
        public int 可承担项目数 { get; set; } = 3;
        public bool 当前可接项目 { get; set; } = true;
        public bool 可公开展示 { get; set; } = true;
    }

    public class 科研合作人查询
    {
        public string 关键词 { get; set; }
        public string 技术服务 { get; set; }
        public string 合作意愿 { get; set; }
        public int? 流程模板编号 { get; set; }
        public int? 项目编号 { get; set; }
        public decimal? 最低每周可用小时 { get; set; }
        public int? 数量 { get; set; }
    }

    public class 科研团队查询
    {
        public string 关键词 { get; set; }
        public string 状态 { get; set; }
        public int? 负责人编号 { get; set; }
        public int? 数量 { get; set; }
    }

    public class 人员导师关系保存
    {
        public int 导师编号 { get; set; }
        public int 学员编号 { get; set; }
        public string 关系类型 { get; set; }
        public DateTime? 开始日期 { get; set; }
        public DateTime? 结束日期 { get; set; }
        public string 状态 { get; set; }
        public string 备注 { get; set; }
    }

    /// <summary>按星期维护专家可接受评审或合作的时间段。</summary>
    public class 专家可用时间保存
    {
        public long? 编号 { get; set; }
        public int 专家编号 { get; set; }
        public short 星期 { get; set; }
        public TimeSpan 开始时间 { get; set; }
        public TimeSpan 结束时间 { get; set; }
        public string 时区 { get; set; } = "Asia/Shanghai";
        public bool 是否启用 { get; set; } = true;
        public string 备注 { get; set; }
    }

    /// <summary>外部专家邀请只登记队列，不包含邮件/身份服务密钥。</summary>
    public class 外部专家邀请创建
    {
        public int? 专家编号 { get; set; }
        public string 外部姓名 { get; set; }
        public string 外部邮箱 { get; set; }
        public string 外部机构 { get; set; }
        public string 邀请场景 { get; set; }
        public int? 流程模板编号 { get; set; }
        public int? 项目编号 { get; set; }
        public long? 外部系统编号 { get; set; }
        public string 幂等键 { get; set; }
        public string 说明 { get; set; }
    }

    /// <summary>外部专家适配器取得真实回执后更新邀请状态。</summary>
    public class 外部专家邀请回执
    {
        public long 邀请编号 { get; set; }
        public string 状态 { get; set; }
        public string 外部消息编号 { get; set; }
        public string 响应摘要 { get; set; }
        public string 失败原因 { get; set; }
        public bool 真实回执 { get; set; }
    }

    public class 科研团队保存
    {
        public long? 编号 { get; set; }
        public string 团队名称 { get; set; }
        public int 负责人编号 { get; set; }
        public string 研究方向 { get; set; }
        public string 技术服务范围 { get; set; }
        public string 合作说明 { get; set; }
        public string 状态 { get; set; }
    }

    public class 科研团队成员保存
    {
        public long 团队编号 { get; set; }
        public int 人员编号 { get; set; }
        public string 成员角色 { get; set; }
        public DateTime? 加入日期 { get; set; }
        public DateTime? 退出日期 { get; set; }
        public string 状态 { get; set; }
        public string 备注 { get; set; }
    }

    public class 人员项目分工保存
    {
        public int 流程模板编号 { get; set; }
        public int 项目编号 { get; set; }
        public int 人员编号 { get; set; }
        public string 分工类型 { get; set; }
        public decimal? 工作量占比 { get; set; }
        public DateTime? 开始日期 { get; set; }
        public DateTime? 结束日期 { get; set; }
        public string 状态 { get; set; }
        public string 备注 { get; set; }
    }

    [Serializable]
    public class 人员导入
    {
        [DisplayName("工号(不能重复)")]
        [Required]
        public string 工号 { get; set; }
        [DisplayName("密码")]
        [Required]
        public string 密码 { get; set; }
        [DisplayName("姓名")]
        [Required]
        public string 姓名 { get; set; }
        [DisplayName("部门名称")]
        [Required]
        public string 部门名称 { get; set; }
        [DisplayName("英文名")]
        public string 英文名 { get; set; }
        [DisplayName("性别")]
        public string 性别 { get; set; }
        [DisplayName("身份证")]
        public string 身份证 { get; set; }
        [DisplayName("出生日期")]
        public DateTime? 出生日期 { get; set; }
        [DisplayName("入职日期")]
        public DateTime? 入职日期 { get; set; }
        [DisplayName("手机")]
        public string 手机 { get; set; }
        [DisplayName("QQ")]
        public string QQ { get; set; }
        [DisplayName("邮箱")]
        public string 邮箱 { get; set; }
        [DisplayName("籍贯")]
        public string 籍贯 { get; set; }
        [DisplayName("毕业日期")]
        public DateTime? 毕业日期 { get; set; }
        [DisplayName("合同开始日期")]
        public DateTime? 合同开始日期 { get; set; }
        [DisplayName("合同结束日期")]
        public DateTime? 合同结束日期 { get; set; }
        [DisplayName("参加工作日期")]
        public DateTime? 参加工作日期 { get; set; }
        [DisplayName("专业技术获得日期")]
        public DateTime? 专业技术获得日期 { get; set; }
        [DisplayName("职务任职日期")]
        public DateTime? 职务任职日期 { get; set; }
        [DisplayName("学位")]
        public string 学位 { get; set; }
        [DisplayName("学历")]
        public string 学历 { get; set; }
        [DisplayName("政治面貌")]
        public string 政治面貌 { get; set; }
        [DisplayName("技术职称类别")]
        public string 技术职称类别 { get; set; }
        [DisplayName("技术职称")]
        public string 技术职称 { get; set; }
        [DisplayName("第二技术职称类别")]
        public string 第二技术职称类别 { get; set; }
        [DisplayName("第二技术职称")]
        public string 第二技术职称 { get; set; }
        [DisplayName("婚姻情况")]
        public string 婚姻情况 { get; set; }
        [DisplayName("学制")]
        public string 学制 { get; set; }
        [DisplayName("学历性质")]
        public string 学历性质 { get; set; }
        [DisplayName("学习形式")]
        public string 学习形式 { get; set; }
        [DisplayName("人员类别")]
        public string 人员类别 { get; set; }
        [DisplayName("个人身份")]
        public string 个人身份 { get; set; }
        [DisplayName("民族")]
        public string 民族 { get; set; }
        [DisplayName("专业技术级别")]
        public string 专业技术级别 { get; set; }
        [DisplayName("所学专业名称")]
        public string 所学专业名称 { get; set; }
        [DisplayName("岗位性质")]
        public string 岗位性质 { get; set; }
        [DisplayName("职务名称")]
        public string 职务名称 { get; set; }
        [DisplayName("职务级别")]
        public string 职务级别 { get; set; }
        [DisplayName("岗位名称")]
        public string 岗位名称 { get; set; }
        [DisplayName("岗位类型")]
        public string 岗位类型 { get; set; }
        [DisplayName("学科门类")]
        public string 学科门类 { get; set; }
        [DisplayName("一级学科")]
        public string 一级学科 { get; set; }
        [DisplayName("二级学科")]
        public string 二级学科 { get; set; }
        [DisplayName("三级学科")]
        public string 三级学科 { get; set; }
        [DisplayName("人员类型")]
        public string 人员类型 { get; set; }
        [DisplayName("获得最高学历的院校")]
        public string 获得最高学历的院校 { get; set; }
        [DisplayName("获得最高学位的院校")]
        public string 获得最高学位的院校 { get; set; }
        [DisplayName("住宅地址")]
        public string 住宅地址 { get; set; }
        [DisplayName("学术特长")]
        public string 学术特长 { get; set; }
        [DisplayName("研究方向")]
        public string 研究方向 { get; set; }
        [DisplayName("个人获得的荣誉和奖项")]
        public string 个人获得的荣誉和奖项 { get; set; }
        [DisplayName("备注")]
        public string 备注 { get; set; }
    }
}
