using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// v1_成果获奖_列表[类]
    /// </summary>
    [Serializable]
	public partial class v1_成果获奖_列表 
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
		//public string 获奖名称 { get; set; }
        public string 获奖名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 获奖日期 { get; set; }
        public DateTime 获奖日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 获奖级别 { get; set; }
        public string 获奖级别 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 获奖类别 { get; set; }
        public string 获奖类别 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 颁奖单位 { get; set; }
        public string 颁奖单位 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 年度 { get; set; }
        public int? 年度 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 第一完成人 { get; set; }
        public string 第一完成人 { get; set; }
        
          
        #endregion
	}
}