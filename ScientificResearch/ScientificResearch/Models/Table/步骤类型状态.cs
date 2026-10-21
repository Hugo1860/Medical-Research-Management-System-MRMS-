using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 步骤类型状态[类]
    /// </summary>
    [Serializable]
	public partial class 步骤类型状态 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 步骤类型编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 状态值 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 操作说明 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 操作提示 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 状态说明 { get; set; }
        
        
        #endregion
	}
}