using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// v1_成果论文_列表[类]
    /// </summary>
    [Serializable]
	public partial class v1_成果论文_列表 
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
		//public string 论文标题 { get; set; }
        public string 论文标题 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 论文Online日期 { get; set; }
        public DateTime? 论文Online日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 论文正式出版日期 { get; set; }
        public DateTime? 论文正式出版日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 刊物名称 { get; set; }
        public string 刊物名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 认领人编号 { get; set; }
        public int? 认领人编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 第一作者 { get; set; }
        public string 第一作者 { get; set; }
        
          
        #endregion
	}
}