using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// v2_步骤[类]
    /// </summary>
    [Serializable]
	public partial class v2_步骤 
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
		//public string 名称 { get; set; }
        public string 名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 步骤模板编号 { get; set; }
        public int 步骤模板编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 流程编号 { get; set; }
        public int 流程编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 状态 { get; set; }
        public int 状态 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 建立时间 { get; set; }
        public DateTime 建立时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 链接名称 { get; set; }
        public string 链接名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 链接路径 { get; set; }
        public string 链接路径 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 链接样式 { get; set; }
        public string 链接样式 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 操作人编号 { get; set; }
        public int? 操作人编号 { get; set; }
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
		//public DateTime 执行时间 { get; set; }
        public DateTime? 执行时间 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 操作说明 { get; set; }
        public string 操作说明 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 操作提示 { get; set; }
        public string 操作提示 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 状态说明 { get; set; }
        public string 状态说明 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 备注 { get; set; }
        public string 备注 { get; set; }
        
          
        #endregion
	}
}