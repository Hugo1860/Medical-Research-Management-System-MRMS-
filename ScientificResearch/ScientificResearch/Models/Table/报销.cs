using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 报销[600类]
    /// </summary>
    [Serializable]
	public partial class 报销 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 流程模板编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 项目编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 报销编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal 报销金额 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 报销方式 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 公务卡号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 公务卡所属人姓名 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 报销人编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime 报销时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 相关文件路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime 申请时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 建立时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 报销经费用途 { get; set; }
        
        
        #endregion
	}
}