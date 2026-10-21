using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// v1_成果专利_列表[类]
    /// </summary>
    [Serializable]
	public partial class v1_成果专利_列表 
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
		//public string 专利名称 { get; set; }
        public string 专利名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 专利权人名称 { get; set; }
        public string 专利权人名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 专利类型 { get; set; }
        public string 专利类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public bool 是否授权 { get; set; }
        public bool? 是否授权 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 年度 { get; set; }
        public int? 年度 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 第一作者 { get; set; }
        public string 第一作者 { get; set; }
        
          
        #endregion
	}
}