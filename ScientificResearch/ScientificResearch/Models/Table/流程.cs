using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 流程[类]
    /// </summary>
    [Serializable]
	public partial class 流程 
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
		public int 发起人编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 状态 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime 创建时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 备注 { get; set; }
        
        
        #endregion
	}
}