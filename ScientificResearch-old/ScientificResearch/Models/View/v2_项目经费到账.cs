using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// v2_项目经费到账[类]
    /// </summary>
    [Serializable]
	public partial class v2_项目经费到账 
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
		//public int 流程模板编号 { get; set; }
        public int 流程模板编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 项目编号 { get; set; }
        public int 项目编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 拨款单位 { get; set; }
        public string 拨款单位 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 到账日期 { get; set; }
        public DateTime 到账日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public decimal 到账金额 { get; set; }
        public decimal 到账金额 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public decimal 技术交易额 { get; set; }
        public decimal 技术交易额 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public decimal 管理费用 { get; set; }
        public decimal 管理费用 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public decimal 技术合同认定服务费 { get; set; }
        public decimal 技术合同认定服务费 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public decimal 增值税 { get; set; }
        public decimal 增值税 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 到账单位 { get; set; }
        public string 到账单位 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 经办人编号 { get; set; }
        public int 经办人编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 到账凭证路径 { get; set; }
        public string 到账凭证路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 建立时间 { get; set; }
        public DateTime? 建立时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 备注 { get; set; }
        public string 备注 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 经办人工号 { get; set; }
        public string 经办人工号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 经办人姓名 { get; set; }
        public string 经办人姓名 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 经办人部门编号 { get; set; }
        public int? 经办人部门编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 经办人部门名称 { get; set; }
        public string 经办人部门名称 { get; set; }
        
          
        #endregion
	}
}