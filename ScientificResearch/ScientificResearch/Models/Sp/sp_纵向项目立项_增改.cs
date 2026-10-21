using System;
using System.Data;

namespace ScientificResearch.Models
{
    /// <summary>
    /// sp_纵向项目立项_增改[类]
    /// </summary>
    [Serializable]
	public class sp_纵向项目立项_增改 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 流程模板编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DataTable tt { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DataTable tt_项目参与人 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DataTable tt_项目经费预算 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DataTable tt_项目计划 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DataTable tt_项目预期成果 { get; set; }
        
          
        #endregion
	}
}