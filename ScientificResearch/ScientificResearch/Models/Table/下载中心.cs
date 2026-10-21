using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 下载中心[类]
    /// </summary>
    [Serializable]
	public partial class 下载中心 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 文件名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 文件类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 文件路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public bool 是否启用 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 上传人编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 建立时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 备注 { get; set; }
        
        
        #endregion
	}
}