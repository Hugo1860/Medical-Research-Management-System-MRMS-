using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 权限[类]
    /// </summary>
    [Serializable]
	public partial class 权限 
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
		public string 路径 { get; set; }
        /// <summary>
		/// 0 菜单 1-各种input
		/// </summary>
        [DisplayName("0 菜单 1-各种input")]
		public bool 类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 图标 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 备注 { get; set; }
        
        
        #endregion
	}
}