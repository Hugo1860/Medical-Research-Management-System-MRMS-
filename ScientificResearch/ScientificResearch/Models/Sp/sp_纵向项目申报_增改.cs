using System;
using System.Data;

namespace ScientificResearch.Models
{
    /// <summary>
    /// sp_纵向项目申报_增改[类]
    /// </summary>
    [Serializable]
	public class sp_纵向项目申报_增改 
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
        
          
        #endregion
	}
}