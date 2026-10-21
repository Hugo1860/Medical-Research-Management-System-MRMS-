using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MyLib
{
    /// <summary>
    /// 
    /// </summary>
    public static class MyObject
    {

        /// <summary>
        /// 利用反射来判断对象是否包含某个属性
        /// </summary>
        /// <param name="obj">object</param>
        /// <param name="propertyName">需要判断的属性</param>
        /// <returns>是否包含</returns>
        public static bool ContainProperty(this object obj, string propertyName)
        {
            if (obj != null && !string.IsNullOrEmpty(propertyName))
            {
                PropertyInfo _findedPropertyInfo = obj.GetType().GetProperty(propertyName);
                return (_findedPropertyInfo != null);
            }
            return false;
        }
        /// <summary>
        /// 利用反射根据对象和属性名取对应属性的值,注意返回null和=null的object是不一样的;
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static object GetValueByPropertyName(this object obj, string propertyName)
        {
            //string PropertyVaule = string.Empty;
            //Type tpEntity = obj.GetType();
            //PropertyInfo[] pis = tpEntity.GetProperties();
            //var a = pis.FirstOrDefault(m => m.Name == propertyName);

            //if (a != null)
            if (obj.ContainProperty(propertyName))
            {
                return obj.GetType().GetProperty(propertyName).GetValue(obj, null);

                //if (!a.PropertyType.IsGenericType)
                //{
                //    //非泛型
                //    a.SetValue(listData[row - 2], Convert.ChangeType(value, a.PropertyType), null);
                //}
                //else
                //{
                //    //泛型Nullable<>
                //    Type genericTypeDefinition = a.PropertyType.GetGenericTypeDefinition();
                //    if (genericTypeDefinition == typeof(Nullable<>))
                //    {
                //        a.SetValue(listData[row - 2], (value == null) ? null : Convert.ChangeType(value, Nullable.GetUnderlyingType(a.PropertyType)), null);
                //    }
                //}

            }
            else
            {
                return null;
            }

            //return PropertyVaule;
        }

        /// <summary>
        /// 利用反射根据对象和属性名为对应的属性赋值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        //public static void SetValueByPropertyName<T>(this object obj, string feildname, T Value)
        //{
        //    PropertyInfo[] propertys = obj.GetType().GetProperties();
        //    foreach (var p in propertys)
        //    {
        //        if (p.Name == feildname)
        //        {
        //            p.SetValue(obj, Convert.ChangeType(Value, p.PropertyType), null);
        //        }
        //    }
        //}

        public static void SetValueByPropertyName(this object obj, string fieldName, object value)
        {
            if (obj.ContainProperty(fieldName))
            {
                var p = obj.GetType().GetProperty(fieldName);

                if (p.PropertyType.ToString().IndexOf("System.") > -1)
                {
                    if (!p.PropertyType.IsGenericType)
                    //非泛型
                    {
                        p.SetValue(obj, Convert.ChangeType(value, p.PropertyType), null);
                    }
                    else
                    //泛型,系统自带的泛型nullable<>,List<>等
                    {
                        //Nullable<>
                        if (p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            //这里可以try catch一下
                            p.SetValue(obj, value == null ? null : Convert.ChangeType(value, Nullable.GetUnderlyingType(p.PropertyType)), null);
                        }
                        else
                        {
                            //非Nullable的泛型
                        }
                    }
                }
                else
                {
                    //属性是自定义的类型;
                }


            }
            else
            {
                throw new Exception("给对象特定属性赋值错误:无此属性");
            }
        }
    }
}
