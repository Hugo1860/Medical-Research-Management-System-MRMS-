using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 通知公告接收条件[类]
    /// </summary>
    [Serializable]
	public partial class 通知公告接收条件 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 通知公告编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 接收者类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 接收者编号 { get; set; }
        
        
        #endregion
	}
}