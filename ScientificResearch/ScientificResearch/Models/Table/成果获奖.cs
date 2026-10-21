using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 成果获奖[相关项目,相关论文,都用关联来示   论文获奖先不管   完成单位数量默认1,前台在1的时候加上备注(独立申报)   303   类]
    /// </summary>
    [Serializable]
	public partial class 成果获奖 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 获奖名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 获奖级别 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime 获奖日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 获奖类别 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 奖励情况 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 联系人 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 联系人手机 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 项目开始时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 项目结束时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 应用开始时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 应用结束时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 年度 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 批文时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 颁奖单位 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 完成人数 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 获奖证书编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 完成单位数量 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 获奖摘要 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 备注 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 相关文件路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 获奖单位文件路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 获奖批文 { get; set; }
        
        
        #endregion
	}
}