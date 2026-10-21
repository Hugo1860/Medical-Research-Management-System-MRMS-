using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScientificResearch.Models
{
    public class 字典Filter
    {
        public string 分类 { get; set; }
        public int? 上级编号 { get; set; }
        public string Like值 { get; set; }
    }

    public class 部门Filter
    {
        public string Like名称 { get; set; }
        public bool? 是否启用 { get; set; }
        public int? 上级部门编号 { get; set; }
    }

    public class 国民行业代码Filter
    {
        public int 上级编号 { get; set; }
        public string Like名称 { get; set; }
    }

    public class 合同买方性质Filter
    {
        public int 上级编号 { get; set; }
        public string Like名称 { get; set; }
    }

    public class 技术领域Filter
    {
        public int 上级编号 { get; set; }
        public string Like名称 { get; set; }
    }

    public class 学科Filter
    {
        public int 上级编号 { get; set; }
        public string Like名称 { get; set; }
    }

    public class 区域Filter
    {
        public int 上级编号 { get; set; }
        public string Like名称 { get; set; }
    }

    public class 项目分类Filter
    {
        public int 上级编号 { get; set; }
        public string Like名称 { get; set; }
    }

    public class 期刊Filter
    {
        //public string Like名称 { get; set; }
        //public string Like英文刊名 { get; set; }
        public string Like主办单位 { get; set; }
        public int? 年度 { get; set; }

        /// <summary>
        /// 包括名称,英文刊名,主办单文
        /// </summary>
        public string LikeAllName { get; set; }
    }

    public class 经费模版Filter
    {
        public string Like名称 { get; set; }
    }

    public class 经费模版项目支出类型Filter
    {
        public int? 所属经费模版编号 { get; set; }
        public int? 编号 { get; set; }
    }

    public class 经费模版项目支出类型增改详情
    {
        public 经费模版项目支出类型 基本信息 { get; set; }
        public IEnumerable<经费模版项目支出内容> 经费模版项目支出内容列表 { get; set; }
    }

    public class 经费模版项目支出类型详情
    {
        public 经费模版项目支出类型 基本信息 { get; set; }
        public IEnumerable<经费模版项目支出内容> 经费模版项目支出内容列表 { get; set; }
        public IEnumerable<string> 可选支出内容列表 { get; set; }
    }

    public class 经费模版项目支出内容Filter
    {
        public int 所属经费模版项目支出类型编号 { get; set; }
    }
}
