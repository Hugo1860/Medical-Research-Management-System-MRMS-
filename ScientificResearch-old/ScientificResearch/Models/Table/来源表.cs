using System;
using System.Data;
using System.ComponentModel;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 来源表[其实就是自顶一个的objectid   流程模板编号也完全可以用这个id   更高层次的sourcetype也可以用;      10-人员   20-部门   30-角色      100-任职   200-主办讲座   201 讲座反馈   210 参加会议   211 参加会议反馈      300 成果论文   310 论文认领   这里的论文认领完全不必新开一个流程,其实就是成果论文的流程,只不过附加动作不是新增一个论文而已;   301 成果著作   302成果专利   303成果获奖      400纵向项目申报   410纵向项目立项   420纵向项目中检   430纵向项目变更   440纵向项目结题      450横向项目   460横向项目变更   470横向项目结题类]
    /// </summary>
    [Serializable]
	public partial class 来源表 
	{       
              
        #region 属性
        /// <summary>
		/// 
		/// </summary>
		public int 编号 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 名称 { get; set; }
        /// <summary>
		/// 
		/// </summary>
		public string 备注 { get; set; }
        
        
        #endregion
	}
}