using System;
using System.Data;

namespace ScientificResearch.Models
{
    /// <summary>
    /// sp_成果著作_增改[类]
    /// </summary>
    [Serializable]
	public class sp_成果著作_增改 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public DataTable tt { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DataTable tt_成果项目 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DataTable tt_成果作者 { get; set; }
        
          
        #endregion
	}
}