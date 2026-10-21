using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 字典[类]
    /// </summary>
    [Serializable]
	public partial class 字典 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 分类 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 值 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 上级编号 { get; set; }
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
		public string 备注 { get; set; }
        
        
        #endregion
	}
}