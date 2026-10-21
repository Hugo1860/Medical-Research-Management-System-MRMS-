using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 成果论文[科研管理员一次审核即可;   是否与项目相关是和项目关联决定   注意:导入时刊物必须已经存在;类]
    /// </summary>
    [Serializable]
	public partial class 成果论文 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 认领人编号 { get; set; }
        /// <summary>
		/// Title
		/// </summary>
        [DisplayName("Title")]
		public string 论文标题 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 论文标题类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 论文Online日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 论文正式出版日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 他引次数 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 年度 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 收稿日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 论文所属栏目 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal? 版面费 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal? 超额版面费 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 超额费用来源 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 奖励情况 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal? 论文字数 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 计划来源 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 论文版面 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 学科门类 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 一级学科 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 二级学科 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 三级学科 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 作者人数 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 关键字 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 论文摘要 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 备注 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 刊物编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 卷号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 期号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 页码范围起 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 页码范围止 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 论文类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public bool? 是否与行业联合发表 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public bool? 是否与地方联合发表 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public bool? 是否与国际联合发表 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public bool? 是否跨学科 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public bool? 是否与本专业相关 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 检索信息页相关文件路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 论文正文路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 其他相关文件路径 { get; set; }
        
        
        #endregion
	}
}