using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 人员角色[类]
    /// </summary>
    [Serializable]
	public partial class 人员角色 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 角色编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 人员编号 { get; set; }
        
        
        #endregion
	}
}