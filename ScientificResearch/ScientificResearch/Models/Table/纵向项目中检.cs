using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 纵向项目中检[420类]
    /// </summary>
    [Serializable]
	public partial class 纵向项目中检 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 纵向项目编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 中检资料路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 备注 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 建立时间 { get; set; }
        
        
        #endregion
	}
}