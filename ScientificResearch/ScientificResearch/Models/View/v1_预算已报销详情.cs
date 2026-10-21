using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// v1_预算已报销详情[类]
    /// </summary>
    [Serializable]
	public partial class v1_预算已报销详情 
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
		//public int 流程模板编号 { get; set; }
        public int 流程模板编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 项目编号 { get; set; }
        public int 项目编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 项目支出类型 { get; set; }
        public string 项目支出类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 项目支出内容 { get; set; }
        public string 项目支出内容 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 财务科目 { get; set; }
        public string 财务科目 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public decimal 批准经费 { get; set; }
        public decimal? 批准经费 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public decimal 配套经费 { get; set; }
        public decimal? 配套经费 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 编制依据 { get; set; }
        public string 编制依据 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public decimal 经费 { get; set; }
        public decimal? 经费 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public decimal 已报销金额 { get; set; }
        public decimal 已报销金额 { get; set; }
        
          
        #endregion
	}
}