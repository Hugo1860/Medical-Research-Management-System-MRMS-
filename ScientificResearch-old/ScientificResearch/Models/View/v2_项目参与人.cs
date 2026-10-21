using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// v2_项目参与人[类]
    /// </summary>
    [Serializable]
	public partial class v2_项目参与人 
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
		//public int 流程模板编号 { get; set; }
        public int 流程模板编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 项目编号 { get; set; }
        public int 项目编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 参与人编号 { get; set; }
        public int 参与人编号 { get; set; }
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
		//public int 部门编号 { get; set; }
        public int 部门编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 部门名称 { get; set; }
        public string 部门名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 工作量占比 { get; set; }
        public int? 工作量占比 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 备注 { get; set; }
        public string 备注 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 建立时间 { get; set; }
        public DateTime? 建立时间 { get; set; }
        
          
        #endregion
	}
}