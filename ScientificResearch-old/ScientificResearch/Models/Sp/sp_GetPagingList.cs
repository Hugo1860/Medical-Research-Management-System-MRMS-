using System;
using System.Data;

namespace ScientificResearch.Models
{
    /// <summary>
    /// sp_GetPagingList[类]
    /// </summary>
    [Serializable]
	public class sp_GetPagingList 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public string tbName { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string tbFields { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string keyFields { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public bool OrderType { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int PageSize { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int PageIndex { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string OrderStr { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string strWhere { get; set; }
        
          
        #endregion
	}
}