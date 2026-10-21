using System;
using System.Data;

namespace ScientificResearch.Models
{
    /// <summary>
    /// sp_流程_发起[类]
    /// </summary>
    [Serializable]
	public class sp_流程_发起 
	{       
              
        #region 属性
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
		public int 发起人编号 { get; set; }
        
          
        #endregion
	}
}