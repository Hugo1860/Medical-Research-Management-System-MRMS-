using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 项目计划[类]
    /// </summary>
    [Serializable]
	public partial class 项目计划 
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
		public DateTime 开始日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime 结束日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 计划内容 { get; set; }
        
        
        #endregion
	}
}