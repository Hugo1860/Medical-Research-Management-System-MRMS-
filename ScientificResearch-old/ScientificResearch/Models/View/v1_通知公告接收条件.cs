using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// v1_通知公告接收条件[类]
    /// </summary>
    [Serializable]
	public partial class v1_通知公告接收条件 
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
		//public int 通知公告编号 { get; set; }
        public int 通知公告编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 接收者类型 { get; set; }
        public int 接收者类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 接收者类型名称 { get; set; }
        public string 接收者类型名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public int 接收者编号 { get; set; }
        public int 接收者编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 接收者名称 { get; set; }
        public string 接收者名称 { get; set; }
        
          
        #endregion
	}
}