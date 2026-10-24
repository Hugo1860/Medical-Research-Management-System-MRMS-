using System;
using System.Collections.Generic;

namespace ScientificResearch.Models
{
    // --- 医疗新技术新项目与适宜技术 ---

    public class 医疗新技术申报增改
    {
        public long? 编号 { get; set; }
        public string 项目名称 { get; set; }
        public string 申报类别 { get; set; } = "临床新技术"; // 临床新技术、临床新项目、微创新技术、护理新技术
        public string 技术难度等级 { get; set; } = "三级"; // 一级、二级、三级、四级
        public string 风险等级 { get; set; } = "中风险"; // 低风险、中风险、高风险
        public int? 负责人编号 { get; set; }
        public int? 科室编号 { get; set; }
        public string 团队成员 { get; set; } = "[]"; // JSON 数组
        public int? 申报年度 { get; set; }
        public DateTime 计划开始日期 { get; set; }
        public DateTime 计划完成日期 { get; set; }
        public int 预期应用例数 { get; set; } = 20;
        public string 技术适应症 { get; set; }
        public string 禁忌症及防范预案 { get; set; }
        public string 附件路径 { get; set; }
        public bool 提交审批 { get; set; } = false;
    }

    public class 医疗新技术准入审查提交
    {
        public long 编号 { get; set; }
        public string 审核部门 { get; set; } // "医务处" 或 "科教处"
        public bool 通过 { get; set; }
        public string 意见 { get; set; }
        public string 伦理审查编号 { get; set; }
        public string 伦理审查状态 { get; set; }
    }

    public class 医疗新技术随访登记
    {
        public long? 编号 { get; set; }
        public long 新技术编号 { get; set; }
        public string 患者姓名脱敏 { get; set; }
        public string 病历号 { get; set; }
        public DateTime 应用日期 { get; set; }
        public int? 主要术者编号 { get; set; }
        public string 主要术者姓名 { get; set; }
        public string 临床疗效 { get; set; } = "有效"; // 显效、有效、改善、无效
        public bool 是否发生不良反应 { get; set; } = false;
        public string 不良反应分级 { get; set; } = "无"; // 无、轻度、中度、重度(SAE)
        public string 不良反应描述及处置 { get; set; }
        public DateTime? 随访日期 { get; set; }
    }

    public class 医疗新技术转常规评奖申请
    {
        public long? 编号 { get; set; }
        public long 新技术编号 { get; set; }
        public string 申请类型 { get; set; } = "期满转常规"; // 期满转常规、院内新技术评奖
        public string 拟申报奖项 { get; set; }
        public string 总结报告路径 { get; set; }
        public int 累计开展例数 { get; set; }
        public decimal? 显效率 { get; set; }
        public decimal? 并发症发生率 { get; set; }
        public string 学术产出说明 { get; set; }
    }

    public class 医疗新技术转常规审批
    {
        public long 编号 { get; set; }
        public bool 通过 { get; set; }
        public string 审批意见 { get; set; }
    }

    public class 医共体适宜技术推广登记
    {
        public long? 编号 { get; set; }
        public string 技术名称 { get; set; }
        public int? 牵头科室编号 { get; set; }
        public int? 牵头专家编号 { get; set; }
        public string 受援基层单位 { get; set; }
        public string 推广形式 { get; set; } = "现场培训";
        public DateTime 推广日期 { get; set; }
        public int 培训场次 { get; set; } = 1;
        public int 受益医务人员数 { get; set; } = 0;
        public int 受益患者人次 { get; set; } = 0;
        public string 讲义及签到材料路径 { get; set; }
        public string 推广成效说明 { get; set; }
    }

    // --- 科普成果与典型案例库 ---

    public class 科普成果与案例登记
    {
        public long? 编号 { get; set; }
        public string 分类 { get; set; } // 医学科普作品、临床典型案例、护理小发明
        public string 形式类型 { get; set; } // 文章、短视频、科普讲座、疑难病案、质控案例、护理改良器具、实用新型转化
        public string 成果名称 { get; set; }
        public int? 第一作者编号 { get; set; }
        public string 第一作者姓名 { get; set; }
        public int? 所属科室编号 { get; set; }
        public string 合作作者 { get; set; } = "[]";
        public DateTime 完成日期 { get; set; }
        public string 发布载体与平台 { get; set; }
        public int 受众传播量与成效 { get; set; } = 0;
        public string 成果摘要 { get; set; }
        public string 附件路径 { get; set; }
    }

    public class 科普成果审核
    {
        public long 编号 { get; set; }
        public bool 通过 { get; set; }
        public string 审核意见 { get; set; }
    }

    // --- 职称晋升科教业绩审查 ---

    public class 职称晋升核验查询
    {
        public int 人员编号 { get; set; }
        public string 拟晋升职称 { get; set; } = "副主任医师";
        public DateTime? 现职称任职日期 { get; set; }
    }

    public class 职称审查单生成请求
    {
        public int 人员编号 { get; set; }
        public string 拟晋升职称 { get; set; }
        public DateTime? 现职称任职日期 { get; set; }
    }

    // --- 医共体合作 ---

    public class 医共体合作单位增改
    {
        public long? 编号 { get; set; }
        public string 单位名称 { get; set; }
        public string 单位类型 { get; set; } = "乡镇卫生院";
        public string 行政区划 { get; set; }
        public int 床位数 { get; set; }
        public int 卫技人员数 { get; set; }
        public string 对接联系人 { get; set; }
        public string 联系电话 { get; set; }
        public DateTime? 合作协议签署日期 { get; set; }
        public DateTime? 协议有效期至 { get; set; }
        public string 合作状态 { get; set; } = "正常合作";
        public string 备注 { get; set; }
    }

    public class 医共体人员进修帮扶登记
    {
        public long? 编号 { get; set; }
        public string 业务类型 { get; set; } = "基层来院进修"; // 基层来院进修、总院下乡帮扶、联合技术攻关
        public long 合作单位编号 { get; set; }
        public string 人员姓名 { get; set; }
        public string 人员职称 { get; set; }
        public string 专业学科 { get; set; }
        public DateTime 开始日期 { get; set; }
        public DateTime 结束日期 { get; set; }
        public int? 带教导师编号 { get; set; }
        public string 考核鉴定结果 { get; set; } = "合格";
        public string 主要帮扶与学习成效 { get; set; }
        public string 附件路径 { get; set; }
    }

    // --- 简易伦理审查与免除知情向导 ---

    public class 简易伦理向导申请
    {
        public int 流程模板编号 { get; set; }
        public int 项目编号 { get; set; }
        public string 场景类型 { get; set; } = "回顾性病历分析"; // 回顾性病历分析、无创临床观察与残余标本、问卷与认知调查
        public string 申请标题 { get; set; }
        public string 研究目的 { get; set; }
        public string 样本纳入与排除标准 { get; set; }
        public string 预期样本量 { get; set; }
        public string 观察指标与数据项 { get; set; }
        public string 免除知情同意依据 { get; set; }
        public string 隐私与去标识化措施 { get; set; }
        public bool 直接提交审批 { get; set; } = false;
    }
}
