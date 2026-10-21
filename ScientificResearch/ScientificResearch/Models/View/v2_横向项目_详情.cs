using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// v2_横向项目_详情[类]
    /// </summary>
    [Serializable]
	public partial class v2_横向项目_详情 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		//public int 编号 { get; set; }
        public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 项目分类 { get; set; }
        public string 项目分类 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 项目类型 { get; set; }
        public string 项目类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合同编号 { get; set; }
        public string 合同编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合同名称 { get; set; }
        public string 合同名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public bool 是否为第一承建单位 { get; set; }
        public bool? 是否为第一承建单位 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 项目负责人编号 { get; set; }
        public int 项目负责人编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 项目归属部门编号 { get; set; }
        public int? 项目归属部门编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 单位名称 { get; set; }
        public string 单位名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 邮政编码 { get; set; }
        public string 邮政编码 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 法定代表人 { get; set; }
        public string 法定代表人 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 经费本编号 { get; set; }
        public string 经费本编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 联系人 { get; set; }
        public string 联系人 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 联系人电话 { get; set; }
        public string 联系人电话 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 通讯地址 { get; set; }
        public string 通讯地址 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public bool 是否需要伦理评审 { get; set; }
        public bool? 是否需要伦理评审 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 档案编号 { get; set; }
        public string 档案编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 财务简称 { get; set; }
        public string 财务简称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 课题来源 { get; set; }
        public string 课题来源 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合作方关系 { get; set; }
        public string 合作方关系 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合作方契约关系 { get; set; }
        public string 合作方契约关系 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合作方单位名称 { get; set; }
        public string 合作方单位名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合作方法人或组织机构代码 { get; set; }
        public string 合作方法人或组织机构代码 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合作方法定代表人 { get; set; }
        public string 合作方法定代表人 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合作方单位联系人 { get; set; }
        public string 合作方单位联系人 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合作方联系电话 { get; set; }
        public string 合作方联系电话 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合作方邮箱 { get; set; }
        public string 合作方邮箱 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合作方邮政编码 { get; set; }
        public string 合作方邮政编码 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合作方所在省 { get; set; }
        public string 合作方所在省 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合作方所在市 { get; set; }
        public string 合作方所在市 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合作方所在地区 { get; set; }
        public string 合作方所在地区 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public decimal 合同金额 { get; set; }
        public decimal 合同金额 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 签订时间 { get; set; }
        public DateTime 签订时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 付款方式 { get; set; }
        public string 付款方式 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 合同开始时间 { get; set; }
        public DateTime 合同开始时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 合同结束时间 { get; set; }
        public DateTime 合同结束时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合作方式 { get; set; }
        public string 合作方式 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 研究类型 { get; set; }
        public string 研究类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 技术领域 { get; set; }
        public string 技术领域 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合同类型 { get; set; }
        public string 合同类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 技术合同类型 { get; set; }
        public string 技术合同类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public bool 是否申请技术合同认定 { get; set; }
        public bool? 是否申请技术合同认定 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public decimal 合同技术交易额 { get; set; }
        public decimal? 合同技术交易额 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 知识产权 { get; set; }
        public string 知识产权 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 计划来源 { get; set; }
        public string 计划来源 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合作方所属国民经济行业 { get; set; }
        public string 合作方所属国民经济行业 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 技术服务的国民经济行业 { get; set; }
        public string 技术服务的国民经济行业 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合同的社会经济目标 { get; set; }
        public string 合同的社会经济目标 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合作方性质 { get; set; }
        public string 合作方性质 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public bool 是否高新性质 { get; set; }
        public bool? 是否高新性质 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合同认定登记承诺书路径 { get; set; }
        public string 合同认定登记承诺书路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合同文件路径 { get; set; }
        public string 合同文件路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 备注 { get; set; }
        public string 备注 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 建立时间 { get; set; }
        public DateTime? 建立时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 项目负责人工号 { get; set; }
        public string 项目负责人工号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 项目负责人姓名 { get; set; }
        public string 项目负责人姓名 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 项目负责人部门编号 { get; set; }
        public int? 项目负责人部门编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 项目负责人部门名称 { get; set; }
        public string 项目负责人部门名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 项目归属部门名称 { get; set; }
        public string 项目归属部门名称 { get; set; }
        
          
        #endregion
	}
}