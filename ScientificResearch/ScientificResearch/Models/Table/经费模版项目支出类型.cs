using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 经费模版项目支出类型[类]
    /// </summary>
    [Serializable]
	public partial class 经费模版项目支出类型 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 所属经费模版编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 项目支出类型 { get; set; }
        /// <summary>
		/// 0-100 %
		/// </summary>
        [DisplayName("0-100 %")]
		public int 经费占比 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 备注 { get; set; }
        
        
        #endregion
	}
}