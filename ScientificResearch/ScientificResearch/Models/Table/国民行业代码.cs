using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 国民行业代码[类]
    /// </summary>
    [Serializable]
	public partial class 国民行业代码 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 上级编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 代码 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 级别 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 排序 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 备注 { get; set; }
        
        
        #endregion
	}
}