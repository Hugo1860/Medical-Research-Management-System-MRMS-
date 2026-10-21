using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 成果作者[类]
    /// </summary>
    [Serializable]
	public partial class 成果作者 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 成果类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 成果编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 作者级别 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 作者编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 工作量占比 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 完成字数 { get; set; }
        
        
        #endregion
	}
}