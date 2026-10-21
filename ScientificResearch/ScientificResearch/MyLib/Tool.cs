using Microsoft.AspNetCore.Http;
//using Microsoft.International.Converters.PinYinConverter;
using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace MyLib
{
    /// <summary>
    /// 一些工具,可以日后继续分类整理
    /// </summary>
    public static class Tool
    {
        //public static string GetHeaderValue(string key)
        //{
        //    return Dal.Basic.GetHeaderValue(key);
        //}

        #region 拼接sql
        /// <summary>
        /// 根据实体拼接SQL语句 修改|新增
        /// </summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="model">模型数据</param>
        /// <param name="keyFiled">主键</param>
        /// <param name="keyFildValue">主键值</param>
        /// <param name="table">表名</param>
        /// <param name="NotActionFields">不需要操作的字段</param>
        /// <returns></returns>
        public static string GetSql<T>(T model, string keyFiled, string keyFildValue, string table, List<string> NotActionFields, out List<NpgsqlParameter> Parameter)
        {
            bool isUpdate = false;
            if (!string.IsNullOrEmpty(keyFildValue))
            {
                if (!keyFildValue.Equals("0"))
                {
                    isUpdate = true;
                }
            }
            Parameter = new List<NpgsqlParameter>();
            string sql = "";
            #region 拼接sql语句及参数

            System.Reflection.PropertyInfo[] pros = model.GetType().GetProperties();
            StringBuilder sb = new StringBuilder();
            StringBuilder sbName = new StringBuilder();
            pros = pros.Where(x => !NotActionFields.Contains(x.Name)).ToArray<System.Reflection.PropertyInfo>();
            for (int i = 0; i < pros.Length; i++)
            {

                Type type = typeof(T);
                type = pros[i].PropertyType;
                if (pros[i].PropertyType.FullName.IndexOf("System") > -1)
                {
                    //if (pros[i].GetValue(model, null) != null)
                    //{

                    object value = pros[i].GetValue(model, null);

                    if (type.FullName.IndexOf("System.DateTime") > -1 && Convert.ToDateTime(value) == DateTime.MinValue)
                    {
                        continue;
                    }
                    string paramterName = GetDisParameter(Parameter, pros[i].Name);
                    if (isUpdate)
                    {
                        if (!pros[i].Name.Equals(keyFiled))
                        {
                            if (pros[i].Name.Equals("ModifyNumber"))
                            {
                                sbName.Append(string.Format("ModifyNumber=ModifyNumber+1", pros[i].Name));
                                sb.Append(",");
                                sbName.Append(",");
                            }
                            else
                            {

                                Parameter.Add(new NpgsqlParameter("@" + paramterName, value));

                                sbName.Append(string.Format("{0}=@{1}", pros[i].Name, paramterName));
                                sb.Append(",");
                                sbName.Append(",");
                            }

                        }
                    }
                    else
                    {
                        if (!pros[i].Name.Equals(keyFiled))
                        {
                            Parameter.Add(new NpgsqlParameter("@" + paramterName, value));
                            sb.Append("@" + paramterName);
                            sbName.Append(pros[i].Name);
                            sb.Append(",");
                            sbName.Append(",");
                        }
                        else
                        {
                            if (pros[i].PropertyType.FullName.Equals("System.String"))
                            {
                                value = Guid.NewGuid().ToString();
                                Parameter.Add(new NpgsqlParameter("@" + paramterName, value));
                                sb.Append("@" + paramterName);
                                sbName.Append(pros[i].Name);
                                sb.Append(",");
                                sbName.Append(",");
                            }
                        }

                    }
                }
                // }
            }
            if (isUpdate)
            {
                sql = string.Format("update {3} set {0}  where {2}='{1}';\r\n",
                    sbName.ToString().Trim(','), keyFildValue, keyFiled, table);
            }
            else
            {
                sql = string.Format("insert into {2} ({0}) values({1});\r\n",
                   sbName.ToString().TrimEnd(','), sb.ToString().TrimEnd(','), table);

            }
            #endregion
            return sql;
        }


        /// <summary>
        /// 根据实体拼接SQL语句 修改|新增
        /// </summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="model">模型数据</param>
        /// <param name="keyFiled">主键</param>
        /// <param name="keyFildValue">主键值</param>
        /// <param name="table">表名</param>
        /// <param name="NotActionFields">不需要操作的字段</param>
        /// <returns></returns>
        public static string GetSql<T>(T model, string keyFiled, string keyFildValue, string table, List<string> NotActionFields,
             List<NpgsqlParameter> nowParameter, out List<NpgsqlParameter> Parameter)
        {
            bool isUpdate = false;
            if (!string.IsNullOrEmpty(keyFildValue))
            {
                if (!keyFildValue.Equals("0"))
                {
                    isUpdate = true;
                }
            }
            Parameter = new List<NpgsqlParameter>();
            string sql = "";
            #region 拼接sql语句及参数

            System.Reflection.PropertyInfo[] pros = model.GetType().GetProperties();
            StringBuilder sb = new StringBuilder();
            StringBuilder sbName = new StringBuilder();
            pros = pros.Where(x => !NotActionFields.Contains(x.Name)).ToArray<System.Reflection.PropertyInfo>();
            for (int i = 0; i < pros.Length; i++)
            {

                Type type = typeof(T);
                type = pros[i].PropertyType;

                if (pros[i].PropertyType.FullName.IndexOf("System") > -1)
                {
                    //if (pros[i].GetValue(model, null) != null)
                    //{

                    object value = pros[i].GetValue(model, null);
                    if (type.FullName.IndexOf("System.DateTime") > -1 && Convert.ToDateTime(value) == DateTime.MinValue)
                    {
                        continue;
                    }
                    string paramterName = GetDisParameter(nowParameter, pros[i].Name);
                    if (isUpdate)
                    {
                        if (!pros[i].Name.Equals(keyFiled))
                        {
                            if (pros[i].Name.Equals("ModifyNumber"))
                            {
                                sbName.Append(string.Format("ModifyNumber=ModifyNumber+1", pros[i].Name));
                                sb.Append(",");
                                sbName.Append(",");
                            }
                            else
                            {

                                nowParameter.Add(new NpgsqlParameter("@" + paramterName, value));

                                sbName.Append(string.Format("{0}=@{1}", pros[i].Name, paramterName));
                                sb.Append(",");
                                sbName.Append(",");
                            }

                        }
                    }
                    else
                    {
                        if (!pros[i].Name.Equals(keyFiled))
                        {
                            nowParameter.Add(new NpgsqlParameter("@" + paramterName, value));
                            sb.Append("@" + paramterName);
                            sbName.Append(pros[i].Name);
                            sb.Append(",");
                            sbName.Append(",");
                        }
                        else
                        {
                            if (pros[i].PropertyType.FullName.Equals("System.String"))
                            {
                                value = Guid.NewGuid().ToString();
                                nowParameter.Add(new NpgsqlParameter("@" + paramterName, value));
                                sb.Append("@" + paramterName);
                                sbName.Append(pros[i].Name);
                                sb.Append(",");
                                sbName.Append(",");
                            }
                        }

                    }
                }
                // }
            }
            if (isUpdate)
            {
                sql = string.Format("update {3} set {0}  where {2}='{1}';\r\n",
                    sbName.ToString().Trim(','), keyFildValue, keyFiled, table);
            }
            else
            {
                sql = string.Format("insert into {2} ({0}) values({1});\r\n",
                   sbName.ToString().TrimEnd(','), sb.ToString().TrimEnd(','), table);

            }
            #endregion
            Parameter = nowParameter;
            return sql;
        }

        #endregion
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Parameter"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetDisParameter(List<NpgsqlParameter> Parameter, string name)
        {
            foreach (NpgsqlParameter parm in Parameter)
            {
                if (parm.ParameterName.Equals("@" + name))
                {
                    name = name + "1";
                    break;
                }
            }
            return name;
        }

        #region DataTable|DataRow 和实体间的转换
        /// <summary>
        /// DataTable转换为对象列表
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="entity"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static List<T> DataTableToList<T>(T entity, DataTable dt) where T : new()
        {
            List<T> lists = new List<T>();
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    lists.Add(DataRowToModel(new T(), row));
                }
            }
            return lists;
        }
        /// <summary>
        /// DataRow转换为对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public static T DataRowToModel<T>(T entity, DataRow row) where T : new()
        {
            //初始化 如果为null
            if (entity == null)
            {
                entity = new T();
            }
            //得到类型
            Type type = typeof(T);
            //取得属性集合
            PropertyInfo[] pi = type.GetProperties();
            foreach (PropertyInfo item in pi)
            {
                if (item.PropertyType.ToString().IndexOf("System.") > -1)
                {
                    //给属性赋值
                    if (row[item.Name] != null && row[item.Name] != DBNull.Value)
                    {
                        if (item.PropertyType == typeof(System.Nullable<System.DateTime>))
                        {
                            item.SetValue(entity, Convert.ToDateTime(row[item.Name].ToString()), null);
                        }
                        else
                        {
                            Type itemType = item.PropertyType;
                            if (itemType.IsGenericType && itemType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))//判断convertsionType是否为nullable泛型类  
                            {
                                //如果type为nullable类，声明一个NullableConverter类，该类提供从Nullable类到基础基元类型的转换  
                                System.ComponentModel.NullableConverter nullableConverter = new System.ComponentModel.NullableConverter(itemType);
                                //将type转换为nullable对的基础基元类型  
                                itemType = nullableConverter.UnderlyingType;
                            }
                            item.SetValue(entity, Convert.ChangeType(row[item.Name], itemType), null);
                        }
                    }
                }
            }
            return entity;
        }

        /// <summary>
        /// 将对象值转换给另一个对象
        /// 这个可以用MyObject中的修改一下
        /// </summary>
        /// <typeparam name="Tout"></typeparam>
        /// <typeparam name="Tin"></typeparam>
        /// <param name="objIn"></param>
        /// <param name="objOut"></param>
        /// <returns></returns>
        public static Tout ModelToModel<Tout, Tin>(Tin objIn, Tout objOut = null) where Tout : class, new()
        {
            objOut = objOut ?? new Tout();
            Type type = typeof(Tout);

            PropertyInfo[] propertyInfosOfObjIn = objIn.GetType().GetProperties();
            PropertyInfo[] propertyInfosOfObjOut = type.GetProperties();

            foreach (PropertyInfo pInfoOfObjOut in propertyInfosOfObjOut)
            {
                if (pInfoOfObjOut.PropertyType.ToString().IndexOf("System.") > -1)
                {
                    for (int i = 0; i < propertyInfosOfObjIn.Length; i++)
                    {

                        if (pInfoOfObjOut.Name == propertyInfosOfObjIn[i].Name)
                        {
                            Type itemType = pInfoOfObjOut.PropertyType;
                            if (itemType.IsGenericType && itemType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))//判断convertsionType是否为nullable泛型类  
                            {
                                //如果type为nullable类，声明一个NullableConverter类，该类提供从Nullable类到基础基元类型的转换  
                                System.ComponentModel.NullableConverter nullableConverter = new System.ComponentModel.NullableConverter(itemType);
                                //将type转换为nullable对的基础基元类型  
                                itemType = nullableConverter.UnderlyingType;
                            }
                            try
                            {
                                pInfoOfObjOut.SetValue(objOut, Convert.ChangeType(propertyInfosOfObjIn[i].GetValue(objIn, null), itemType), null);
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                    }
                }
            }
            return objOut;
        }

        /// <summary>
        /// Convert a List{T} to a DataTable.
        /// </summary>
        public static DataTable ModelToDataTable<T>(List<T> items)
        {
            var tb = new DataTable(typeof(T).Name);

            PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in props)
            {
                Type t = GetCoreType(prop.PropertyType);
                tb.Columns.Add(prop.Name, t);
            }
            if (items != null)
            {
                foreach (T item in items)
                {
                    var values = new object[props.Length];

                    for (int i = 0; i < props.Length; i++)
                    {
                        Type t = GetCoreType(props[i].PropertyType);
                        object value = props[i].GetValue(item, null);
                        if (t.Name.Equals("DateTime"))
                        {
                            if (DateTime.Parse(value.ToString()).Equals(DateTime.MinValue))
                            {
                                value = null;
                            }
                        }
                        values[i] = value;
                    }

                    tb.Rows.Add(values);
                }
            }
            return tb;
        }

        /// <summary>
        /// Determine of specified type is nullable
        /// </summary>
        public static bool IsNullable(Type t)
        {
            return !t.IsValueType || (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        /// <summary>
        /// Return underlying type if type is Nullable otherwise return the type
        /// </summary>
        public static Type GetCoreType(Type t)
        {
            if (t != null && IsNullable(t))
            {
                if (!t.IsValueType)
                {
                    return t;
                }
                else
                {
                    return Nullable.GetUnderlyingType(t);
                }
            }
            else
            {
                return t;
            }
        }
        #endregion

        #region 对象转为DataTable
        /// <summary>
        /// list转datatable
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static DataTable ToDataTable<TResult>(this IEnumerable<TResult> value) where TResult : new()
        {
            if (value == null) value = new List<TResult>();
            //创建属性的集合
            List<PropertyInfo> pList = new List<PropertyInfo>();
            Type type = typeof(TResult);
            DataTable dt = new DataTable();
            Array.ForEach<PropertyInfo>(type.GetProperties(), p => { pList.Add(p); dt.Columns.Add(p.Name); });
            foreach (var item in value)
            {
                //创建一个DataRow实例
                DataRow row = dt.NewRow();
                pList.ForEach(p => row[p.Name] = p.GetValue(item, null));
                dt.Rows.Add(row);
            }
            return dt;
        }
        /// <summary>
        /// 对象转datatable
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static DataTable ToDataTable<TResult>(this TResult value) where TResult : new()
        {
            if (value == null) value = new TResult();
            //创建属性的集合
            List<PropertyInfo> pList = new List<PropertyInfo>();
            Type type = typeof(TResult);
            DataTable dt = new DataTable();
            Array.ForEach<PropertyInfo>(type.GetProperties(), p => { pList.Add(p); dt.Columns.Add(p.Name); });
            //创建一个DataRow实例
            DataRow row = dt.NewRow();
            pList.ForEach(p => row[p.Name] = p.GetValue(value, null));
            dt.Rows.Add(row);
            return dt;
        }

        #endregion

        #region 废弃,根据sp名称、持有sp参数值的对象，获得sp的参数列表
        /// <summary>
        /// 根据sp名称、持有sp参数值的对象，获得sp的参数列表，
        /// </summary>
        /// <param name="T">sp生成的参数class</param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static List<NpgsqlParameter> getNpgsqlParameterFromObj<T>(List<object> obj)
        {
            //var result = new List<NpgsqlParameter>();
            //var parameterList = typeof(T).GetProperties().ToList().Select(i => i.Name).ToList();

            ////4.24有了sp的参数class
            //foreach(var objInList in obj)
            //{
            //    var dataList = objInList.GetType().GetProperties().ToList();

            //    var temp = from aParameter in parameterList
            //               join aProp in dataList
            //               on aParameter equals aProp.Name
            //               let value = aProp.GetValue(objInList)
            //               where value !=null 
            //               select new NpgsqlParameter("@" + aParameter, value);
            //    result = result.Concat(temp).ToList();
            //}

            var result = new List<NpgsqlParameter>();
            var parameterList = typeof(T).GetProperties().ToList().Select(i => i.Name).ToList();

            foreach (var parameterName in parameterList)
            {
                //var parameterName = parameter["Parameter_name"].ToString();
                foreach (var item in obj)
                {
                    var t = item.GetType();
                    foreach (var prop in t.GetProperties())
                    {
                        if (parameterName == prop.Name)
                        {
                            var type = prop.PropertyType;
                            var propValue = prop.GetValue(item);
                            if (type.FullName.IndexOf("System.DateTime") > -1 && Convert.ToDateTime(propValue) == DateTime.MinValue)
                            {
                                //propValue = (DateTime)System.Data.SqlTypes.SqlDateTime.MinValue;
                                propValue = DBNull.Value;
                            }

                            if (propValue != null && !result.Exists(i => i.ParameterName == "@" + parameterName))
                            {
                                result.Add(new NpgsqlParameter("@" + parameterName, propValue));
                                //parameterList.Remove(parameterName);
                            }

                        }
                    }
                }
            }


            return result;
        }
        /// <summary>
        /// 根据实体获取存储过程参数列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        public static List<NpgsqlParameter> GetNpgsqlParameterFromModel<T>(T model)
        {

            var Parameters = new List<NpgsqlParameter>();
            System.Reflection.PropertyInfo[] pros = model.GetType().GetProperties();
            for (int i = 0; i < pros.Length; i++)
            {
                Type type = typeof(T);
                type = pros[i].PropertyType;
                if (pros[i].PropertyType.FullName.IndexOf("System") > -1)
                {
                    object value = pros[i].GetValue(model, null);
                    if (type.FullName.IndexOf("System.DateTime") > -1 && Convert.ToDateTime(value) == DateTime.MinValue)
                    {
                        value = DBNull.Value;
                    }
                    Parameters.Add(new NpgsqlParameter("@" + pros[i].Name, value));
                }
            }
            return Parameters;
        }
        #endregion

        #region 汉字转换为拼音
        ///// <summary>  
        ///// 从字符串中获取汉字拼音首字母，不是汉字则原样输出  
        ///// 原来在在Microsoft.International.Converters.PinYinConverter这包
        ///// </summary>  
        ///// <param name="transName"></param>  
        ///// <returns></returns>  
        //public static string GetFirstPYLetter(string transName)
        //{
        //    string r = string.Empty;
        //    foreach (char obj in transName)
        //    {
        //        try
        //        {
        //            ChineseChar chineseChar = new ChineseChar(obj);
        //            string t = chineseChar.Pinyins[0].ToString();
        //            r += t.Substring(0, 1);
        //        }
        //        catch
        //        {
        //            r += obj.ToString();
        //        }
        //    }
        //    return r.ToUpper();
        //}

        ///// <summary>  
        ///// 把汉字转换成拼音(全拼)  
        ///// </summary>  
        ///// <param name="transName">汉字字符串</param>  
        ///// <returns>转换后的拼音(全拼)字符串</returns>  
        //public static string GetAllPYLetters(string transName)
        //{
        //    string r = string.Empty;
        //    foreach (char obj in transName)
        //    {
        //        try
        //        {
        //            ChineseChar chineseChar = new ChineseChar(obj);
        //            string t = chineseChar.Pinyins[0].ToString();
        //            string p = t.Substring(0, t.Length - 1);
        //            p = p.Substring(0, 1) + (p.Substring(1, p.Length - 1)).ToLower();
        //            r += p;
        //        }
        //        catch
        //        {
        //            r += obj.ToString();
        //        }
        //    }
        //    return r;

        //}

        #endregion

        #region 获取当前时间戳
        public static Int64 GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds);
        }
        #endregion

        #region 废弃,用httpclient,见MyHttpLib,http方式提交数据方法(HTTP接口公用方法)
        /// <summary>
        /// http方式提交数据方法(HTTP接口公用方法)
        /// </summary>
        /// <param name="url">提交地址</param>
        /// <param name="param">参数信息</param>
        /// <returns></returns>
        public static string PostData(string url, string param)
        {
            try
            {
                byte[] bs = Encoding.ASCII.GetBytes(param);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);
                req.Method = "POST";
                req.Timeout = 20000;
                req.ContentType = "application/x-www-form-urlencoded";
                req.ContentLength = bs.Length;
                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(bs, 0, bs.Length);
                }
                using (WebResponse wr = req.GetResponse())
                {
                    System.IO.Stream responseStream = wr.GetResponseStream();
                    System.IO.StreamReader reader = new System.IO.StreamReader(responseStream, Encoding.UTF8);
                    string srcString = reader.ReadToEnd();
                    return srcString;
                }
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        #endregion

        #region 废弃,用httpclient,见MyHttpLib,http方式(GET)提交数据方法(HTTP接口公用方法)
        /// <summary>
        /// http方式(GET)提交数据方法(HTTP接口公用方法)
        /// </summary>
        /// <param name="url">提交地址</param>
        /// <param name="param">参数信息</param>
        /// <returns></returns>
        public static string PostDataForGet(string url, string param)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url + "?" + param);
                req.Method = "GET";
                using (WebResponse wr = req.GetResponse())
                {
                    System.IO.Stream responseStream = wr.GetResponseStream();
                    System.IO.StreamReader reader = new System.IO.StreamReader(responseStream, Encoding.UTF8);
                    string srcString = reader.ReadToEnd();
                    return srcString;
                }
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        #endregion

        #region 流方式POST数据
        /// <summary>
        /// Post 数据至服务端
        /// </summary>
        /// <param name="postContent">提交内容</param>
        /// <param name="url">提交地址</param>
        /// <returns></returns>
        public static string PostHttp(string postContent, string url)
        {
            string resultstr = string.Empty;
            System.Text.Encoding encode = System.Text.Encoding.GetEncoding("gb2312");
            System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.Timeout = 20000;
            Byte[] bytes = encode.GetBytes(postContent);
            req.ContentLength = bytes.Length;
            Stream sendStream = req.GetRequestStream();
            sendStream.Write(bytes, 0, bytes.Length);
            sendStream.Close();
            System.Net.WebResponse rep = req.GetResponse();
            Stream getStream = rep.GetResponseStream();
            using (StreamReader sr = new StreamReader(getStream, System.Text.Encoding.UTF8))
            {
                resultstr = sr.ReadToEnd();
                sr.Close();
            }
            getStream.Close();
            rep.Close();
            return resultstr;
        }
        #endregion

        #region md5加密
        /// <summary>
        /// md5
        /// </summary>
        /// <param name="encryptString"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        public static string MD5(string encryptString, string salt = "")
        {
            byte[] result = Encoding.Default.GetBytes(encryptString + salt);
            using var md5 = System.Security.Cryptography.MD5.Create();
            byte[] output = md5.ComputeHash(result);
            string encryptResult = BitConverter.ToString(output).Replace("-", "");
            return encryptResult;
        }

        ///<summary>
        ///生成随机字符串 
        ///</summary>
        ///<param name="length">目标字符串的长度</param>
        ///<param name="useNum">是否包含数字，1=包含，默认为包含</param>
        ///<param name="useLow">是否包含小写字母，1=包含，默认为包含</param>
        ///<param name="useUpp">是否包含大写字母，1=包含，默认为包含</param>
        ///<param name="useSpe">是否包含特殊字符，1=包含，默认为不包含</param>
        ///<param name="custom">要包含的自定义字符，直接输入要包含的字符列表</param>
        ///<returns>指定长度的随机字符串</returns>
        public static string GetRandomString(int length = 10, bool useNum = true, bool useLow = true, bool useUpp = true, bool useSpe = true, string custom = "")
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));

            string str = custom ?? string.Empty;
            if (useNum == true) { str += "0123456789"; }
            if (useLow == true) { str += "abcdefghijklmnopqrstuvwxyz"; }
            if (useUpp == true) { str += "ABCDEFGHIJKLMNOPQRSTUVWXYZ"; }
            if (useSpe == true) { str += "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~"; }
            if (length == 0) return string.Empty;
            if (str.Length == 0) throw new ArgumentException("至少启用一种随机字符集", nameof(length));

            var result = new char[length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = str[RandomNumberGenerator.GetInt32(str.Length)];
            }
            return new string(result);
        }
        #endregion

        /// <summary>
        /// 是否是Base64字符串
        /// </summary>
        /// <param name="eStr"></param>
        /// <returns></returns>
        public static bool IsBase64(string eStr)
        {
            if ((eStr.Length % 4) != 0)
            {
                return false;
            }
            if (!Regex.IsMatch(eStr, "^[A-Z0-9/+=]*$", RegexOptions.IgnoreCase))
            {
                return false;
            }
            return true;
        }

    }
}
