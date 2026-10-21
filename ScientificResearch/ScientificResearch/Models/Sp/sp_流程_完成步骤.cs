using System;
using System.Data;

namespace ScientificResearch.Models
{
    /// <summary>
    /// sp_流程_完成步骤[类]
    /// </summary>
    [Serializable]
	public class sp_流程_完成步骤 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 步骤编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 状态 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 操作人 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 备注 { get; set; }
        
          
        #endregion
	}
}