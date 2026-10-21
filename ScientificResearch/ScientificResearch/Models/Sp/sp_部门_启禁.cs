using System;
using System.Data;

namespace ScientificResearch.Models
{
    /// <summary>
    /// sp_部门_启禁[类]
    /// </summary>
    [Serializable]
	public class sp_部门_启禁 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public bool 是否启用 { get; set; }
        
          
        #endregion
	}
}