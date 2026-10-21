using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 经费模版项目支出内容[类]
    /// </summary>
    [Serializable]
	public partial class 经费模版项目支出内容 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 所属经费模版项目支出类型编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 项目支出内容 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 财务科目 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 备注 { get; set; }
        
        
        #endregion
	}
}