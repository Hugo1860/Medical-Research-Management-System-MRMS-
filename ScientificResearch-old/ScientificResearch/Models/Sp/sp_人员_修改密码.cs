using System;
using System.Data;

namespace ScientificResearch.Models
{
    /// <summary>
    /// sp_人员_修改密码[类]
    /// </summary>
    [Serializable]
	public class sp_人员_修改密码 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public string 工号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 旧密码 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 新密码 { get; set; }
        
          
        #endregion
	}
}