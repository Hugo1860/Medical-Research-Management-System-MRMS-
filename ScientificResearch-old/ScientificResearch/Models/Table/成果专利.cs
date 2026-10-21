using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 成果专利[302类]
    /// </summary>
    [Serializable]
	public partial class 成果专利 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 专利名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 专利国别 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 专利类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 专利范围 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public bool? 是否授权 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 代理机构 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 年度 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 专利权人名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 申请号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime 申请日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 公开号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 公开日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 授权机构 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 专利号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 国际专利主分类号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 授权公告日 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 证书号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public bool? 是否职务发明 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public bool? 是否生效 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 发明人数 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal? 奖励金额 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal? 超额费用 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 超额费用来源 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 专利摘要 { get; set; }
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
		public decimal? 代理费金额 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 代理费发票号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 代理费付款日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal? 代理资助金额 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 代理办理资助日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 代理费到账日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public bool? 申请费是否减缓 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal? 申请费金额 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 申请费收据号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 申请费支付日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal? 申请费资助金额 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 申请费办理资助日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 申请费到账日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public bool? 申请审查费是否延缓 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal? 申请审查费用 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 申请审查费收据编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 申请审查费付款日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal? 申请审查费资助金额 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 申请审查费资助日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 申请审查费到账日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public bool? 授权费是否减缓 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal? 授权费用金额 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 授权费收据号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 授权费付款日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal? 授权费资助金额 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 授权费办理资助日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 授权费到账日期 { get; set; }
        
        
        #endregion
	}
}