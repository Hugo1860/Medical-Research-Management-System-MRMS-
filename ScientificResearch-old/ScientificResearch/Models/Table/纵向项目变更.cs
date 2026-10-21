using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 纵向项目变更[除了"纵向项目立项编号"对应纵向项目立项里面的编号,   其他在"纵向项目立项"里面的字段,和本都一一对应;   430   类]
    /// </summary>
    [Serializable]
	public partial class 纵向项目变更 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 变更内容简介 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 变更事由 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 变更备注 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime 变更时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 纵向项目编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 分类 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 项目登记情况 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 项目编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 项目中文名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 项目英文名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 项目分类 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 成果归属部门编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 负责人编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 立项年度 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime 登记日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 登记人编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime 批准日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal? 批准经费 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal? 配套经费 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 经费卡号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 项目等级 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime 计划开始日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime 计划完成日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 学科门类 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 学科类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 研究类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 项目类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 项目级别 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 项目来源单位 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 项目类别 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 档案编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 财务简称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 亚类说明 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 附注说明 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 纵向项目合同类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 社会经济目标 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public bool? 是否需要伦理评审 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public bool? 是否需要伦理批文 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 伦理批文编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 伦理批文时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 伦理批文路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 立项文件路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 备注 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 建立时间 { get; set; }
        
        
        #endregion
	}
}