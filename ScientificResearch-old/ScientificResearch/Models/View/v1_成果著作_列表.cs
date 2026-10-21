using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// v1_成果著作_列表[类]
    /// </summary>
    [Serializable]
	public partial class v1_成果著作_列表 
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
		//public string 著作名称 { get; set; }
        public string 著作名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 著作出版日期 { get; set; }
        public DateTime 著作出版日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 年度 { get; set; }
        public int 年度 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 出版社名称 { get; set; }
        public string 出版社名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 著作类型 { get; set; }
        public string 著作类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 主编 { get; set; }
        public string 主编 { get; set; }
        
          
        #endregion
	}
}