using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 主办讲座[类]
    /// </summary>
    [Serializable]
	public partial class 主办讲座 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 活动名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 所属部门编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal? 活动经费 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 会议类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 活动对象 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime 开始时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime 结束时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 主办人编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 主讲人姓名 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 主讲人国别 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 主讲人单位 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 主讲人学位 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 主讲人职称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 主讲人简历路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 讲座地点 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 内容概述 { get; set; }
        
        
        #endregion
	}
}