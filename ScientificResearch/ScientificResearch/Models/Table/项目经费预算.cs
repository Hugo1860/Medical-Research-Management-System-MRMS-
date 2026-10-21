using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 项目经费预算[类]
    /// </summary>
    [Serializable]
	public partial class 项目经费预算 
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
		public string 项目支出类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 项目支出内容 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 财务科目 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal? 批准经费 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal? 配套经费 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 编制依据 { get; set; }
        
        
        #endregion
	}
}