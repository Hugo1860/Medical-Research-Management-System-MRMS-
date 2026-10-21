using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 角色权限[类]
    /// </summary>
    [Serializable]
	public partial class 角色权限 
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
		public int 权限编号 { get; set; }
        
        
        #endregion
	}
}