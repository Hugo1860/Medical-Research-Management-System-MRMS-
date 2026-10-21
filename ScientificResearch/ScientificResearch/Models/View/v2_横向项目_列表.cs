using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// v2_横向项目_列表[类]
    /// </summary>
    [Serializable]
	public partial class v2_横向项目_列表 
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
		//public string 合同编号 { get; set; }
        public string 合同编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 合同名称 { get; set; }
        public string 合同名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public decimal 合同金额 { get; set; }
        public decimal 合同金额 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 签订时间 { get; set; }
        public DateTime 签订时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 项目负责人编号 { get; set; }
        public int 项目负责人编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 项目负责人工号 { get; set; }
        public string 项目负责人工号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 项目负责人姓名 { get; set; }
        public string 项目负责人姓名 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 项目负责人部门编号 { get; set; }
        public int? 项目负责人部门编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 项目负责人部门名称 { get; set; }
        public string 项目负责人部门名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 项目归属部门编号 { get; set; }
        public int? 项目归属部门编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 项目归属部门名称 { get; set; }
        public string 项目归属部门名称 { get; set; }
        
          
        #endregion
	}
}