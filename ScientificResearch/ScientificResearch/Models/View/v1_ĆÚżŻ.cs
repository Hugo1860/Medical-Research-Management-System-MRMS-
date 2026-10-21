using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// v1_期刊[类]
    /// </summary>
    [Serializable]
	public partial class v1_期刊 
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
		//public string 代码 { get; set; }
        public string 代码 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 名称 { get; set; }
        public string 名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 英文刊名 { get; set; }
        public string 英文刊名 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 主办单位 { get; set; }
        public string 主办单位 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 期刊系列 { get; set; }
        public string 期刊系列 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 收录情况 { get; set; }
        public string 收录情况 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 语种 { get; set; }
        public string 语种 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string JCR分区 { get; set; }
        public string JCR分区 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 编目名称 { get; set; }
        public string 编目名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 编目类别 { get; set; }
        public string 编目类别 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 年度 { get; set; }
        public int? 年度 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 创刊日期 { get; set; }
        public DateTime? 创刊日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 被引用次数 { get; set; }
        public int? 被引用次数 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public decimal 影响因子 { get; set; }
        public decimal? 影响因子 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public decimal 特征因子分值 { get; set; }
        public decimal? 特征因子分值 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string ISSN号 { get; set; }
        public string ISSN号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string CN号 { get; set; }
        public string CN号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 出版周期 { get; set; }
        public string 出版周期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 出版地 { get; set; }
        public string 出版地 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 开本 { get; set; }
        public string 开本 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 备注 { get; set; }
        public string 备注 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string AllName { get; set; }
        public string AllName { get; set; }
        
          
        #endregion
	}
}