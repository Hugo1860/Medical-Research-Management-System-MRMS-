using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// v1_角色的权限[类]
    /// </summary>
    [Serializable]
	public partial class v1_角色的权限 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		//public int 编号 { get; set; }
        public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 上级编号 { get; set; }
        public int 上级编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 名称 { get; set; }
        public string 名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 路径 { get; set; }
        public string 路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public bool 类型 { get; set; }
        public bool 类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 图标 { get; set; }
        public string 图标 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 备注 { get; set; }
        public string 备注 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public bool 拥有 { get; set; }
        public bool? 拥有 { get; set; }
        
          
        #endregion
	}
}