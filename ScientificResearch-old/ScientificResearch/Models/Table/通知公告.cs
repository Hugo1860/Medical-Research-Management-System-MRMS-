using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 通知公告[类]
    /// </summary>
    [Serializable]
	public partial class 通知公告 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 通知类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 通知标识 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 发送人编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 通知名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime 关闭时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 相关文件路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 通知内容 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 接收人数 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 已接收人数 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public bool 是否启用 { get; set; }
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