using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 流程模板[类]
    /// </summary>
    [Serializable]
	public partial class 流程模板 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 期限天数 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 备注 { get; set; }
        
        
        #endregion
	}
}