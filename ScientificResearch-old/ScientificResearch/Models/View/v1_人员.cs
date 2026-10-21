using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// v1_人员[类]
    /// </summary>
    [Serializable]
	public partial class v1_人员 
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
		//public string 英文名 { get; set; }
        public string 英文名 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 性别 { get; set; }
        public string 性别 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 身份证 { get; set; }
        public string 身份证 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 出生日期 { get; set; }
        public DateTime? 出生日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 入职日期 { get; set; }
        public DateTime? 入职日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 手机 { get; set; }
        public string 手机 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string QQ { get; set; }
        public string QQ { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 邮箱 { get; set; }
        public string 邮箱 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 籍贯 { get; set; }
        public string 籍贯 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 毕业日期 { get; set; }
        public DateTime? 毕业日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 合同开始日期 { get; set; }
        public DateTime? 合同开始日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 合同结束日期 { get; set; }
        public DateTime? 合同结束日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 参加工作日期 { get; set; }
        public DateTime? 参加工作日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 专业技术获得日期 { get; set; }
        public DateTime? 专业技术获得日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public DateTime 职务任职日期 { get; set; }
        public DateTime? 职务任职日期 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 学位 { get; set; }
        public string 学位 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 学历 { get; set; }
        public string 学历 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 政治面貌 { get; set; }
        public string 政治面貌 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 技术职称类别 { get; set; }
        public string 技术职称类别 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 技术职称 { get; set; }
        public string 技术职称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 第二技术职称类别 { get; set; }
        public string 第二技术职称类别 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 第二技术职称 { get; set; }
        public string 第二技术职称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 婚姻情况 { get; set; }
        public string 婚姻情况 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 学制 { get; set; }
        public string 学制 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 学历性质 { get; set; }
        public string 学历性质 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 学习形式 { get; set; }
        public string 学习形式 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 人员类别 { get; set; }
        public string 人员类别 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 个人身份 { get; set; }
        public string 个人身份 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 民族 { get; set; }
        public string 民族 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 专业技术级别 { get; set; }
        public string 专业技术级别 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 所学专业名称 { get; set; }
        public string 所学专业名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 岗位性质 { get; set; }
        public string 岗位性质 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 职务名称 { get; set; }
        public string 职务名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 职务级别 { get; set; }
        public string 职务级别 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 岗位名称 { get; set; }
        public string 岗位名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 岗位类型 { get; set; }
        public string 岗位类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 学科门类 { get; set; }
        public string 学科门类 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 一级学科 { get; set; }
        public string 一级学科 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 二级学科 { get; set; }
        public string 二级学科 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 三级学科 { get; set; }
        public string 三级学科 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 人员类型 { get; set; }
        public string 人员类型 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 获得最高学历的院校 { get; set; }
        public string 获得最高学历的院校 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 获得最高学位的院校 { get; set; }
        public string 获得最高学位的院校 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 住宅地址 { get; set; }
        public string 住宅地址 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 学术特长 { get; set; }
        public string 学术特长 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 研究方向 { get; set; }
        public string 研究方向 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		//public string 个人获得的荣誉和奖项 { get; set; }
        public string 个人获得的荣誉和奖项 { get; set; }
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
        
          
        #endregion
	}
}