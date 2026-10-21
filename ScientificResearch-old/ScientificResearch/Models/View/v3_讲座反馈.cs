using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// v3_讲座反馈[类]
    /// </summary>
    [Serializable]
	public partial class v3_讲座反馈 
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
		//public int 讲座编号 { get; set; }
        public int 讲座编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 活动名称 { get; set; }
        public string 活动名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 所属部门编号 { get; set; }
        public int? 所属部门编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 所属部门名称 { get; set; }
        public string 所属部门名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public decimal 活动经费 { get; set; }
        public decimal? 活动经费 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 会议类型 { get; set; }
        public string 会议类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 活动对象 { get; set; }
        public string 活动对象 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 开始时间 { get; set; }
        public DateTime 开始时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 结束时间 { get; set; }
        public DateTime 结束时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 主办人编号 { get; set; }
        public int 主办人编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 主讲人姓名 { get; set; }
        public string 主讲人姓名 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 主讲人国别 { get; set; }
        public string 主讲人国别 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 主讲人单位 { get; set; }
        public string 主讲人单位 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 主讲人学位 { get; set; }
        public string 主讲人学位 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 主讲人职称 { get; set; }
        public string 主讲人职称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 主讲人简历路径 { get; set; }
        public string 主讲人简历路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 讲座地点 { get; set; }
        public string 讲座地点 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 内容概述 { get; set; }
        public string 内容概述 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 主办人工号 { get; set; }
        public string 主办人工号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 主办人姓名 { get; set; }
        public string 主办人姓名 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 主办人部门名称 { get; set; }
        public string 主办人部门名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 专家姓名 { get; set; }
        public string 专家姓名 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 专家身份证号码 { get; set; }
        public string 专家身份证号码 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 专家护照号 { get; set; }
        public string 专家护照号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 专家联系方式 { get; set; }
        public string 专家联系方式 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public decimal 专家讲课费金额 { get; set; }
        public decimal? 专家讲课费金额 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 结算方式 { get; set; }
        public string 结算方式 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 活动持续分钟 { get; set; }
        public int 活动持续分钟 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 参会人数 { get; set; }
        public int 参会人数 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 讲座附件路径 { get; set; }
        public string 讲座附件路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 讲座效果 { get; set; }
        public string 讲座效果 { get; set; }
        
          
        #endregion
	}
}