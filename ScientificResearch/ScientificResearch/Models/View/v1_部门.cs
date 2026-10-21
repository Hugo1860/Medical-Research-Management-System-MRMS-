using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// v1_部门[类]
    /// </summary>
    [Serializable]
	public partial class v1_部门 
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
		//public int 上级部门编号 { get; set; }
        public int 上级部门编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 名称 { get; set; }
        public string 名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 负责人编号 { get; set; }
        public int? 负责人编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 负责人姓名 { get; set; }
        public string 负责人姓名 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 排序值 { get; set; }
        public int 排序值 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 备注 { get; set; }
        public string 备注 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public bool 是否启用 { get; set; }
        public bool 是否启用 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 是否存在下级 { get; set; }
        public int 是否存在下级 { get; set; }
        
          
        #endregion
	}
}