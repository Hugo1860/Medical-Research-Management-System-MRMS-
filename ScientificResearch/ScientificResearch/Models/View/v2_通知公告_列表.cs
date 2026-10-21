using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// v2_通知公告_列表[类]
    /// </summary>
    [Serializable]
	public partial class v2_通知公告_列表 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		//public int 编号 { get; set; }
        public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 通知类型 { get; set; }
        public string 通知类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 通知标识 { get; set; }
        public string 通知标识 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 发送人编号 { get; set; }
        public int 发送人编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 通知名称 { get; set; }
        public string 通知名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 关闭时间 { get; set; }
        public DateTime 关闭时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 相关文件路径 { get; set; }
        public string 相关文件路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 接收人数 { get; set; }
        public int 接收人数 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 已接收人数 { get; set; }
        public int 已接收人数 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public bool 是否启用 { get; set; }
        public bool 是否启用 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 建立时间 { get; set; }
        public DateTime? 建立时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 备注 { get; set; }
        public string 备注 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 发送人工号 { get; set; }
        public string 发送人工号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 发送人姓名 { get; set; }
        public string 发送人姓名 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 发送人部门编号 { get; set; }
        public int? 发送人部门编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 发送人部门名称 { get; set; }
        public string 发送人部门名称 { get; set; }
        
          
        #endregion
	}
}