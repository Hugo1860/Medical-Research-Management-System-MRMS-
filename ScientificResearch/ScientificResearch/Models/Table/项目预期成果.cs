using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 项目预期成果[类]
    /// </summary>
    [Serializable]
	public partial class 项目预期成果 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 流程模板编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 项目编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 预期成果 { get; set; }
        
        
        #endregion
	}
}