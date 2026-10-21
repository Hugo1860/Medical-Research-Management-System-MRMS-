using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 任职[类]
    /// </summary>
    [Serializable]
	public partial class 任职 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime 任职开始时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 任职截止时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 任职人员编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 任职团体名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 所任职务 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 相关文件路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime 建立时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 备注 { get; set; }
        
        
        #endregion
	}
}