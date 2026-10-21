using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// v2_纵向项目申报_列表[类]
    /// </summary>
    [Serializable]
	public partial class v2_纵向项目申报_列表 
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
		//public int 分类 { get; set; }
        public int 分类 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 分类名称 { get; set; }
        public string 分类名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 项目名称 { get; set; }
        public string 项目名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 申报人编号 { get; set; }
        public int 申报人编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 工号 { get; set; }
        public string 工号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 姓名 { get; set; }
        public string 姓名 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 部门编号 { get; set; }
        public int? 部门编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 部门名称 { get; set; }
        public string 部门名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 学科门类 { get; set; }
        public string 学科门类 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 项目类型 { get; set; }
        public string 项目类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 项目级别 { get; set; }
        public string 项目级别 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 项目来源单位 { get; set; }
        public string 项目来源单位 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 项目类别 { get; set; }
        public string 项目类别 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 申报时间 { get; set; }
        public DateTime? 申报时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public decimal 申请经费 { get; set; }
        public decimal 申请经费 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 计划开始日期 { get; set; }
        public DateTime 计划开始日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 计划完成日期 { get; set; }
        public DateTime 计划完成日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public bool 是否立项 { get; set; }
        public bool? 是否立项 { get; set; }
        
          
        #endregion
	}
}