using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// v2_任职[类]
    /// </summary>
    [Serializable]
	public partial class v2_任职 
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
		//public DateTime 任职开始时间 { get; set; }
        public DateTime 任职开始时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 任职截止时间 { get; set; }
        public DateTime? 任职截止时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 任职人员编号 { get; set; }
        public int 任职人员编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 工号 { get; set; }
        public string 工号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 姓名 { get; set; }
        public string 姓名 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 部门名称 { get; set; }
        public string 部门名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 任职团体名称 { get; set; }
        public string 任职团体名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 所任职务 { get; set; }
        public string 所任职务 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 相关文件路径 { get; set; }
        public string 相关文件路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 建立时间 { get; set; }
        public DateTime 建立时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 备注 { get; set; }
        public string 备注 { get; set; }
        
          
        #endregion
	}
}