using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MyLib
{
    /// <summary>
    /// Redis操作
    /// </summary>
    public static class RedisHelper
    {
        #region 辅助方法

        #region string
        /// <summary>
        /// 将类型T的一个对象拉平为一个RedisKey
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static RedisKey ObjToRedisKey<T>(this T key)
        {
            string result = JsonConvert.SerializeObject(key);
            return (RedisKey)result;
        }

        /// <summary>
        /// 将类型T的一个对象拉平为一个RedisValue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static RedisValue ObjToRedisValue<T>(this T value)
        {
            string result = JsonConvert.SerializeObject(value);
            return (RedisValue)result;
        }

        /// <summary>
        /// 将RedisValue类型的value解码为一个T对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T RedisValueToObj<T>(this RedisValue value)
        {
            return value.IsNull ? default(T) : JsonConvert.DeserializeObject<T>(value);
        }

        /// <summary>
        /// 将RedisKey解码为一个T对象,一般是个string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T RedisKeyToObj<T>(this RedisKey key)
        {
            return JsonConvert.DeserializeObject<T>(key);
        }
        #endregion

        #region list
        /// <summary>
        /// RedisKey[]类型的key解码为一个List T类型的列表,一般是string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys"></param>
        /// <returns></returns>
        public static IEnumerable<T> RedisKeyArrToObjList<T>(this RedisKey[] keys)
        {
            return from item in keys select item.RedisKeyToObj<T>();
        }

        /// <summary>
        /// 将一个RedisValue[]类型的values解码为一个List T类型的列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static IEnumerable<T> RedisValueArrToObjList<T>(this RedisValue[] values)
        {
            return from item in values select item.RedisValueToObj<T>();
        }

        /// <summary>
        /// 将一个T的列表转为一个RedisKey[]
        /// </summary>
        /// <param name="objList"></param>
        /// <returns></returns>
        public static RedisKey[] ObjListToRedisKeyArr<T>(this IEnumerable<T> objList)
        {
            var result = from item in objList select item.ObjToRedisKey<T>();
            return result.ToArray();
        }

        /// <summary>
        /// 将一个T类型的列表转为一个 RedisValue[]
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objList"></param>
        /// <returns></returns>
        public static RedisValue[] ObjListToRedisValueArr<T>(this IEnumerable<T> objList)
        {
            var result = from item in objList select item.ObjToRedisValue<T>();
            return result.ToArray();
        }
        #endregion

        #region hash
        /// <summary>
        /// 特定类的对象 转化为HashEntry[]  此方法不支持List等对象，需另外封装
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static HashEntry[] OjbToHashEntryArr<T>(this T obj) where T : class, new()
        {
            var list = new List<HashEntry>();
            foreach (PropertyInfo p in obj.GetType().GetProperties())
            {
                var name = p.Name.ToString();
                var val = p.GetValue(obj);
                //list.Add(new HashEntry(name, val.ToString()));
                //上面的不能处理null
                list.Add(new HashEntry(name, JsonConvert.SerializeObject(val)));
            }
            return list.ToArray();
        }

        /// <summary>
        /// 将HashEntry[]转为指定类型的对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static T HashEntryArrToObj<T>(this HashEntry[] list) where T : class, new()
        {
            if (list.Count() == 0) return null;
            var o = new T();
            foreach (var item in list)
            {
                var name = item.Name;
                var value = item.Value;
                foreach (PropertyInfo p in o.GetType().GetProperties())
                {
                    var oName = p.Name.ToString();
                    if (oName == name)
                    {
                        //p.SetValue(o, Convert.ChangeType(value.ToString(), p.PropertyType));

                        if (!p.PropertyType.IsGenericType)
                        {
                            //非泛型
                            // p.SetValue(o, Convert.ChangeType(value.ToString(), p.PropertyType));
                            p.SetValue(o, Convert.ChangeType(value.RedisValueToObj<String>(), p.PropertyType), null);
                        }
                        else
                        {
                            //泛型Nullable<>
                            Type genericTypeDefinition = p.PropertyType.GetGenericTypeDefinition();
                            if (genericTypeDefinition == typeof(Nullable<>))
                            {
                                p.SetValue(o, string.IsNullOrEmpty(value) ? null : Convert.ChangeType(value, Nullable.GetUnderlyingType(p.PropertyType)), null);
                            }
                            else
                            {
                                //非Nullable的泛型,即属性中有子对象的情况,无法满足;
                            }
                        }
                    }
                }
            }
            return o;
        }
        #endregion

        /// <summary>
        /// 设置Hash,同时带expire
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="redisDb"></param>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <param name="expire"></param>
        public static void MyHashSet<T>(this IDatabase redisDb, string key, T obj, TimeSpan? expire) where T : class, new()
        {
            redisDb.HashSet(key,
                            obj.OjbToHashEntryArr(),
                            StackExchange.Redis.CommandFlags.FireAndForget);

            redisDb.KeyExpire(key, expire, StackExchange.Redis.CommandFlags.FireAndForget);
        }

        /// <summary>
        /// 设置set,同时带expire
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="redisDb"></param>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <param name="expire"></param>
        public static void MySetAdd<T>(this IDatabase redisDb, string key, IEnumerable<T> obj, TimeSpan? expire)
        {
            redisDb.SetAdd(key, obj.ObjListToRedisValueArr(), StackExchange.Redis.CommandFlags.FireAndForget);
            redisDb.KeyExpire(key, expire, StackExchange.Redis.CommandFlags.FireAndForget);
        }

        /// <summary>
        /// 设置listleftpush,同时带expire
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="redisDb"></param>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <param name="expire"></param>
        public static void MyListLeftPush<T>(this IDatabase redisDb, string key, IEnumerable<T> obj, TimeSpan? expire)
        {
            //redisDb.ListTrim(key, 1, -1);
            redisDb.ListLeftPush(key, obj.ObjListToRedisValueArr<T>(), StackExchange.Redis.CommandFlags.FireAndForget);
            redisDb.KeyExpire(key, expire, StackExchange.Redis.CommandFlags.FireAndForget);

        }

        #endregion 辅助方法
    }
}