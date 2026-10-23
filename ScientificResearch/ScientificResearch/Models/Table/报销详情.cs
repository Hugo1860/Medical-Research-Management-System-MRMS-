using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 报销详情[这个应该由选择的经费模版为基础,填写报销金额   财务科目这个一般都没显示,   判断一个项目对应的多个报销的更多个报销详情中某个项目支出内容已经报销了多少,   就按项目支出类型+项目支出内容分组类]
    /// </summary>
    [Serializable]
	public partial class 报销详情 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 报销编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 项目支出类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 项目支出内容 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 财务科目 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal 报销金额 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 备注 { get; set; }
        
        
        public string 投稿备案编号 { get; set; }
        #endregion
	}
}