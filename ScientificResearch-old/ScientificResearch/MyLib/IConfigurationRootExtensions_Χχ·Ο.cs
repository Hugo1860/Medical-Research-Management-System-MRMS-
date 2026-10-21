//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Configuration;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace MyLib
//{
//    /// <summary>
//    /// 完全可以不用了,使用Configuration.GetValue<T>即可解决问题
//    /// </summary>
//    public static class IConfigurationRootExtensions
//    {

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="root"></param>
//        /// <param name="path"></param>
//        /// <returns></returns>
//        public static T Get<T>(this IConfigurationRoot root, string path)
//        {
//            return root.GetSection(path).Get<T>();
//            //2018-4-2 不能像下面这样用jsonString来解析,如果是string这些就麻烦了.用上面的section自带的Get<T>来;
//            //var value = root.GetSection(path).Get<T>()
//            //    .Value;
//            //if(typeof( T) ==typeof(string) ){ return value; }
//            ////default 生成类型的默认值。 对于引用类型，将为 NULL；对于值类型，将为零；对于结构，将为 0 位模式。
//            //return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value);
//        }
//    }
//}
