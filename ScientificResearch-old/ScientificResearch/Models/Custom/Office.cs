using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;


namespace ScientificResearch.Models
{
    public class 通知公告增改
    {
        public 通知公告 基本资料 { get; set; }
        public IEnumerable<通知公告接收条件> 通知公告接收条件列表 { get; set; }
    }

    public class 通知公告接收条件Filter
    {
        public int? 通知公告编号 { get; set; }
    }

    public class 通知公告启禁
    {
        public int 编号 { get; set; }
        public int 发送人编号 { get; set; }
        public bool 是否启用 { get; set; }
    }

    public class 通知公告Filter
    {
        public string 通知类型 { get; set; }
        public string Like通知名称 { get; set; }
        public string Like通知内容 { get; set; }
        public string Like发送人姓名 { get; set; }
    }

    public class 某人员可查看的通知公告Filter
    {
        public bool? 是否已接收 { get; set; }
        public string 通知类型 { get; set; }
        public string Like通知名称 { get; set; }
        public string Like通知内容 { get; set; }
        public string Like发送人姓名 { get; set; }
    }

    //[ModelMetadataType(typeof(下载中心MD))]
    [ModelMetadataType(typeof(下载中心MD))]
    public partial class 下载中心 {}

    public class 下载中心MD
    {
        [Required(ErrorMessage ="请提供文件路径")]
        public string 文件路径 { get; set; }

        //这个在ModelMetadataType下没起作用啊;
        [JsonIgnore]
        public int 上传人编号 { get; set; }
    }

    public class 下载中心启禁
    {
        public int 编号 { get; set; }
        public int 上传人编号 { get; set; }
        public bool 是否启用 { get; set; }
    }

    public class 下载中心Filter
    {
        public bool? 是否启用 { get; set; }
        public string Like文件名称 { get; set; }
        public string Like文件类型 { get; set; }
        
        public string Like上传人姓名 { get; set; }
    }
}
