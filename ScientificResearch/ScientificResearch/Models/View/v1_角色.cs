using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// v1_角色[类]
    /// </summary>
    [Serializable]
	public partial class v1_角色 
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
		//public string 名称 { get; set; }
        public string 名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 备注 { get; set; }
        public string 备注 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 人数 { get; set; }
        public int? 人数 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 功能数 { get; set; }
        public int? 功能数 { get; set; }
        
          
        #endregion
	}
}