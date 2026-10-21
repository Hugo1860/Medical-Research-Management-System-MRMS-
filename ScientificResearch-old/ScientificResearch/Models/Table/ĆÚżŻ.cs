using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 期刊[类]
    /// </summary>
    [Serializable]
	public partial class 期刊 
	{       
              
        #region 属性
        /// <summary>
		/// 编号
		/// </summary>
        [DisplayName("编号")]
		public int 编号 { get; set; }
        /// <summary>
		/// 代码
		/// </summary>
        [DisplayName("代码")]
		public string 代码 { get; set; }
        /// <summary>
		/// 名称(不加书名号;名称或英文刊名至少提供一个)
		/// </summary>
        [DisplayName("名称(不加书名号;名称或英文刊名至少提供一个)")]
		public string 名称 { get; set; }
        /// <summary>
		/// 英文刊名
		/// </summary>
        [DisplayName("英文刊名")]
		public string 英文刊名 { get; set; }
        /// <summary>
		/// 主办单位
		/// </summary>
        [DisplayName("主办单位")]
		public string 主办单位 { get; set; }
        /// <summary>
		/// 期刊系列
		/// </summary>
        [DisplayName("期刊系列")]
		public string 期刊系列 { get; set; }
        /// <summary>
		/// 收录情况
		/// </summary>
        [DisplayName("收录情况")]
		public string 收录情况 { get; set; }
        /// <summary>
		/// 语种
		/// </summary>
        [DisplayName("语种")]
		public string 语种 { get; set; }
        /// <summary>
		/// JCR分区
		/// </summary>
        [DisplayName("JCR分区")]
		public string JCR分区 { get; set; }
        /// <summary>
		/// 编目名称
		/// </summary>
        [DisplayName("编目名称")]
		public string 编目名称 { get; set; }
        /// <summary>
		/// 编目类别
		/// </summary>
        [DisplayName("编目类别")]
		public string 编目类别 { get; set; }
        /// <summary>
		/// 年度
		/// </summary>
        [DisplayName("年度")]
		public int? 年度 { get; set; }
        /// <summary>
		/// 创刊日期
		/// </summary>
        [DisplayName("创刊日期")]
		public DateTime? 创刊日期 { get; set; }
        /// <summary>
		/// 被引用次数
		/// </summary>
        [DisplayName("被引用次数")]
		public int? 被引用次数 { get; set; }
        /// <summary>
		/// 影响因子
		/// </summary>
        [DisplayName("影响因子")]
		public decimal? 影响因子 { get; set; }
        /// <summary>
		/// 特征因子分值
		/// </summary>
        [DisplayName("特征因子分值")]
		public decimal? 特征因子分值 { get; set; }
        /// <summary>
		/// ISSN号
		/// </summary>
        [DisplayName("ISSN号")]
		public string ISSN号 { get; set; }
        /// <summary>
		/// CN号
		/// </summary>
        [DisplayName("CN号")]
		public string CN号 { get; set; }
        /// <summary>
		/// 出版周期
		/// </summary>
        [DisplayName("出版周期")]
		public string 出版周期 { get; set; }
        /// <summary>
		/// 出版地
		/// </summary>
        [DisplayName("出版地")]
		public string 出版地 { get; set; }
        /// <summary>
		/// 开本
		/// </summary>
        [DisplayName("开本")]
		public string 开本 { get; set; }
        /// <summary>
		/// 备注
		/// </summary>
        [DisplayName("备注")]
		public string 备注 { get; set; }
        
        
        #endregion
	}
}