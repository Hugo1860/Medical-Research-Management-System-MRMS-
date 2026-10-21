using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 步骤[类]
    /// </summary>
    [Serializable]
	public partial class 步骤 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 步骤模板编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 流程编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 状态 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime 建立时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 操作人编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 执行时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 备注 { get; set; }
        
        
        #endregion
	}
}