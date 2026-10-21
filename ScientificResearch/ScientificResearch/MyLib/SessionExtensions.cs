using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyLib
{
    /// <summary>
    /// 操作session
    /// </summary>
    public static class SessionExtensions
    {
        /// <summary>
        /// 加了个泛型转化的set
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        /// <summary>
        /// 加了个泛型转化的get
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            //default 生成类型的默认值。 对于引用类型，将为 NULL；对于值类型，将为零；对于结构，将为 0 位模式。
            return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value);
        }
    }
}
