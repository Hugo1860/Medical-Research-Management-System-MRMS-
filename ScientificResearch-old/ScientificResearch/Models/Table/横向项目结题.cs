using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 横向项目结题[470类]
    /// </summary>
    [Serializable]
	public partial class 横向项目结题 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 横向项目编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 结题验收编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 结题验收时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public bool? 结题验收是否合格 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int? 结题验收参与人数 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 结题资料路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 备注 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime? 建立时间 { get; set; }
        
        
        #endregion
	}
}