using System;

namespace ScientificResearch.Models
{
    public class 生物样本资产项
    {
        public int 编号 { get; set; }
        public string 样本编码 { get; set; }
        public string 样本类型 { get; set; }
        public string 疾病诊断 { get; set; }
        public string 解剖部位 { get; set; }
        public string 储存温度 { get; set; }
        public string 储存位置 { get; set; }
        public string 规格单位 { get; set; }
        public decimal 初始数量 { get; set; }
        public decimal 在库余量 { get; set; }
        public decimal 单价成本 { get; set; }
        public string 质控等级 { get; set; }
        public string 知情同意书编号 { get; set; }
        public string 伦理批文号 { get; set; }
        public string 状态 { get; set; }
        public DateTime? 采集日期 { get; set; }
        public DateTime? 入库日期 { get; set; }
        public string 备注 { get; set; }
        public DateTime 建立时间 { get; set; }
    }

    public class 生物样本借还项
    {
        public int 编号 { get; set; }
        public int 样本编号 { get; set; }
        public string 样本编码 { get; set; }
        public string 样本类型 { get; set; }
        public string 疾病诊断 { get; set; }
        public string 规格单位 { get; set; }
        public int? 项目编号 { get; set; }
        public string 项目名称 { get; set; }
        public int 申请人编号 { get; set; }
        public string 申请人姓名 { get; set; }
        public string 申请用途 { get; set; }
        public decimal 申请数量 { get; set; }
        public decimal 出库数量 { get; set; }
        public decimal 归还数量 { get; set; }
        public decimal 消耗数量 { get; set; }
        public decimal 单价成本 { get; set; }
        public decimal 分摊总金额 { get; set; }
        public string 伦理批文号 { get; set; }
        public string 伦理核验结果 { get; set; }
        public string 借还状态 { get; set; }
        public DateTime 申请时间 { get; set; }
        public DateTime? 出库时间 { get; set; }
        public string 出库经办人 { get; set; }
        public DateTime? 归还时间 { get; set; }
        public string 归还验收人 { get; set; }
        public string 核销说明 { get; set; }
    }

    public class 申请样本领用请求
    {
        public int 样本编号 { get; set; }
        public int? 项目编号 { get; set; }
        public string 申请用途 { get; set; }
        public decimal 申请数量 { get; set; }
    }

    public class 核准样本出库请求
    {
        public int 台账编号 { get; set; }
        public decimal? 实际出库数量 { get; set; }
        public string 出库经办人 { get; set; }
    }

    public class 登记样本归还与消耗请求
    {
        public int 台账编号 { get; set; }
        public decimal 归还数量 { get; set; }
        public decimal 消耗数量 { get; set; }
        public string 验收人 { get; set; }
        public string 核销说明 { get; set; }
    }

    public class 新增样本资产请求
    {
        public string 样本编码 { get; set; }
        public string 样本类型 { get; set; }
        public string 疾病诊断 { get; set; }
        public string 解剖部位 { get; set; }
        public string 储存温度 { get; set; } = "-80℃";
        public string 储存位置 { get; set; }
        public string 规格单位 { get; set; } = "管";
        public decimal 初始数量 { get; set; } = 1.00m;
        public decimal 单价成本 { get; set; } = 150.00m;
        public string 质控等级 { get; set; } = "一级合格";
        public string 知情同意书编号 { get; set; }
        public string 伦理批文号 { get; set; }
        public string 备注 { get; set; }
    }
}
