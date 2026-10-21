using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 登录日志[类]
    /// </summary>
    [Serializable]
	public partial class 登录日志 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 工号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 姓名 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 登录时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string IP { get; set; }
        
        
        #endregion
	}
}