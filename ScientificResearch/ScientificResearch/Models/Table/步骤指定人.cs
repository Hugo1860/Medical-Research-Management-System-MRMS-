using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 步骤指定人[暂时也不用这个类]
    /// </summary>
    [Serializable]
	public partial class 步骤指定人 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 步骤编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 指定人编号 { get; set; }
        
        
        #endregion
	}
}