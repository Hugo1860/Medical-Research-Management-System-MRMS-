using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 讲座反馈[类]
    /// </summary>
    [Serializable]
	public partial class 讲座反馈 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 讲座编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 专家姓名 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 专家身份证号码 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 专家护照号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 专家联系方式 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal? 专家讲课费金额 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 结算方式 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 活动持续分钟 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 参会人数 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 讲座附件路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 讲座效果 { get; set; }
        
        
        #endregion
	}
}