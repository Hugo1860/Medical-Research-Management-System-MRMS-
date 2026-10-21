using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;


namespace ScientificResearch.Models
{
    public class 主办讲座Filter
    {
        public int? 编号 { get; set; }
        public int? 主办人编号 { get; set; }
        public string Like主办人姓名 { get; set; }
        public string Like活动名称 { get; set; }
        public bool? 是否反馈过 { get; set; }
    }

    public class 讲座反馈Filter
    {
        public int? 编号 { get; set; }
        //[Required(ErrorMessage = "讲座编号必填")]
        public string Like活动名称 { get; set; }
        public string Like主办人姓名 { get; set; }
        public string Like专家姓名 { get; set; }
        public int? 讲座编号 { get; set; }
    }

    public class 参加会议Filter
    {
        public int? 编号 { get; set; }
        public int? 申请人编号 { get; set; }
        public string Like申请人姓名 { get; set; }
        public string Like会议名称 { get; set; }
        public bool? 是否反馈过 { get; set; }
    }

    public class 参加会议反馈Filter
    {
        public int? 编号 { get; set; }
        //[Required(ErrorMessage ="参加会议编号必填")]
        public int? 参加会议编号 { get; set; }
        public string Like申请人姓名 { get; set; }
        public string Like会议名称 { get; set; }
    }
}
