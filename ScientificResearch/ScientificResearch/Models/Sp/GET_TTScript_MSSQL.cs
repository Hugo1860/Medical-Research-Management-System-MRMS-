using System;
using System.Data;

namespace ScientificResearch.Models
{
    /// <summary>
    /// GET_TTScript_MSSQL[类]
    /// </summary>
    [Serializable]
	public class GET_TTScript_MSSQL 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public string DBNAME { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string TBNAME { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string SQL { get; set; }
        
          
        #endregion
	}
}