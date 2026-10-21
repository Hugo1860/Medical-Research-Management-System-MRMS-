using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 分页请求的类
    /// </summary>
    public class Paging
    {        
        /// <summary>
        /// 当前页 从1开始
        /// </summary>
        public int Index { get; set; } = 1;

        /// <summary>
        /// 页大小
        /// </summary>
        public int Size { get; set; } = 10;

        /// <summary>
        /// 排序类型,true->1是升序,false->0是降序
        /// </summary>
        public bool OrderType { get; set; } = true;
        //private string _OrderBy = "";
        /// <summary>
        /// 排序
        /// </summary>
        //public string OrderBy
        //{
        //    get { return _OrderBy; }
        //    set { _OrderBy = value; }
        //}
    }

    /// <summary>
    /// 返回分页的类型
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PagingResult<T>
    {
        /// <summary>
        /// 
        /// </summary>
        public int Total { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<T> List { get; set; }
    }

    /// <summary>
    /// 返回的结构
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ResultData<T>
    {
        /// <summary>
        /// 
        /// </summary>
        public T Data { get; set; }
    }
}