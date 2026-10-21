using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 参加会议[210类]
    /// </summary>
    [Serializable]
	public partial class 参加会议 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 会议名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 会议主办单位 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 会议承办单位 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 研讨领域 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 学科类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 会议地址 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime 会议开始时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime 会议结束时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 往返时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 会议网址 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 会议简介及申请理由 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 申请人编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 参会相关文件路径 { get; set; }
        
        
        #endregion
	}
}