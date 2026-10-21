using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 成果项目[项目类型 1-纵向 2-横向   成果类型 1-论文 2 著作 3 专利 4文体 5 获奖   是否项目带来成果:   1项目得到成果   2成果引发项目   类]
    /// </summary>
    [Serializable]
	public partial class 成果项目 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 项目类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 项目编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 成果类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 成果编号 { get; set; }
        /// <summary>
		/// 1 项目带来成果 0 成果引发项目
		/// </summary>
        [DisplayName("1 项目带来成果 0 成果引发项目")]
		public bool? 是否项目带来成果 { get; set; }
        
        
        #endregion
	}
}