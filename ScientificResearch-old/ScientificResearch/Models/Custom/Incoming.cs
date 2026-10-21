using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ScientificResearch.Models
{
    public class 项目经费到账Filter
    {
        public int 流程模板编号 { get; set; }
        public int 项目编号 { get; set; }
    }

    public class 纵向项目经费到账增改
    {
        public 纵向项目经费到账 基本资料 { get; set; }
        /// <summary>
        /// 需要暂存=true,不暂存=false
        /// </summary>
        public bool IsHold { get; set; }
    }

    public class 横向项目经费到账增改
    {
        public 横向项目经费到账 基本资料 { get; set; }
        /// <summary>
        /// 需要暂存=true,不暂存=false
        /// </summary>
        public bool IsHold { get; set; }
    }

    [Serializable]
    public partial class 纵向项目经费到账
    {

        #region 属性
       
        /// <summary>
		/// 
		/// </summary>
		public int 项目编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 拨款单位 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime 到账日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal 到账金额 { get; set; }
       
        /// <summary>
		/// 
		/// </summary>
		public string 到账单位 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 经办人编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 到账凭证路径 { get; set; }
        
        /// <summary>
		/// 
		/// </summary>
		public string 备注 { get; set; }


        #endregion
    }

    [Serializable]
    public partial class 横向项目经费到账
    {

        #region 属性

        /// <summary>
		/// 
		/// </summary>
		public int 项目编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 拨款单位 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public DateTime 到账日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal 到账金额 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal 技术交易额 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal 管理费用 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal 技术合同认定服务费 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public decimal 增值税 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 到账单位 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public int 经办人编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 到账凭证路径 { get; set; }

        /// <summary>
		/// 
		/// </summary>
		public string 备注 { get; set; }


        #endregion
    }
}
