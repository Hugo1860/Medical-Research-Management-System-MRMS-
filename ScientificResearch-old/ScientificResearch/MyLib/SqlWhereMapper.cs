using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MyLib
{
    //调用方法,bool用int来,
    //public class TestObject
    //{
    //    public string Name { get; set; }
    //    public string Likeadress { get; set; }
    //    public int Beginage { get; set; }
    //    public bool big { get; set; }
    //    public string Endbirthday { get; set; }
    //}

    //static void Main(string[] args)
    //{
    //    var strSql = "select * from Users where ";
    //    var where = SqlWhereMapper.toWhere<TestObject>(new TestObject() { Name = "liyang", Likeadress = "东坡", Beginage = 40, big = false, Endbirthday = "1977-11-26" });
    //    Console.WriteLine("{0} {1}", strSql, where);
    //    Console.WriteLine();
    //}

    public class SqlWhereMapper
    {
        //提供一个Dictionary，以及类型映射
        protected string pSymbol = "@";//参数符号
        protected string cSymbol = "+";//连接符号 
        protected string dSymbol = "'";
        private static Regex reg = new Regex(@"^(?<tab>\w+\.)?(?<Pre>(NotEqual|PK|Begin|End|Like|UnLike|Null))?(?<Key>\w+)$");
        private IDictionary<string, object> entity;//条件字典
        private Dictionary<string, Type> typeMapper = new Dictionary<string, Type>();

        public SqlWhereMapper(IDictionary<string, object> entity)
        {
            this.entity = entity;
        }

        public void LoadTypeMapper<T>(params string[] keys)
        {
            var type = typeof(T);
            foreach (var key in keys)
            {
                typeMapper[key] = type;
            }
        }

        public static string toWhere<T>(T whereObject)
        {
            if (whereObject == null) return "1=1";
            var dic = new Dictionary<string, object>();

            //var strSql = "select * from Users where ";
            //var testObject = new TestObject() { Name = "liyang", Likeadress = "东坡", Beginage = 40, big = 1, Endbirthday = "1977-11-26" };
            Type t = whereObject.GetType();
            PropertyInfo[] PropertyList = t.GetProperties();
            foreach (PropertyInfo item in PropertyList)
            {
                //string name = item.Name;
                //object value = item.GetValue(testObject, null);

                dic[item.Name] = item.GetValue(whereObject, null);
            }


            //dic["BeginAge"] = "18";//Age>=18
            //dic["EndAge"] = 80;//Age<=80
            //dic["LikeName"] = "玲";//Name包含'玲'
            //dic["IsGirl"] =true;//IsGirl = true 
            //dic["BeginLoginTime"] = DateTime.Now.AddDays(-7);//LoginTime >= 七天前
            //dic["EndLoginTime"] = DateTime.Now.ToString();//LoginTime <= 今天
            //部分类型若不正确，则需要手动映射
            SqlWhereMapper mapper = new SqlWhereMapper(dic);
            //mapper.LoadTypeMapper<DateTime>("LoginTime");
            //mapper.LoadTypeMapper<int>("Age");
            //mapper.LoadTypeMapper<bool>("IsGirl");
            //看看生成的条件语句
            return mapper.Where();
            //Console.WriteLine("{0} {1}", strSql, where);
            //Console.WriteLine();
        }

        public string Where()
        {
            var strSql = new StringBuilder();
            var changType = new Dictionary<string, Type>();
            var isString = false;
            object value;

            strSql.AppendFormat(" 1=1 AND");

            foreach (var single in entity)
            {
                //获取到属性对应的字段名
                var match = reg.Match(single.Key);
                if (!match.Success)
                {
                    continue;
                }
                var pre = match.Groups["Pre"].Value;//获取命名规则中的比较谓词
                var colName = match.Groups["Key"].Value;//获取字段名称
                var table = match.Groups["tab"].Value;//获取字段表名限定

                if (single.Value == null) continue;

                if (single.Value.GetType() == typeof(bool))
                {
                    if ((bool)single.Value == true)
                    {
                        value = 1;
                    }
                    else
                    {
                        value = 0;
                    }
                }
                else
                {
                    value = single.Value;
                }

                //若该字段的类型为字符串，检测在Mapper中的类型
                if (single.Value.GetType() == typeof(string))
                {
                    isString = true;
                    Type t;
                    if (typeMapper.TryGetValue(colName, out t) && t != typeof(string))
                        changType[single.Key] = t;
                }

                strSql.AppendFormat(" {0} AND", Builder(pre, colName, table, value, isString));//拼接SQL条件
                isString = false;
            }
            if (strSql.Length > 0) strSql.Length -= 3;
            foreach (var ct in changType)
            {
                entity[ct.Key] = Convert.ChangeType(entity[ct.Key], ct.Value);
            }
            return strSql.ToString();
        }

        private string Builder(string pre, string colName, string table, object colValue = null, bool isString = false)
        {
            switch (pre)
            {
                case "NotEqual": return NotEqual(colName, table, colValue, isString);
                case "Like": return Like(colName, table, colValue, isString);
                case "UnLike": return Like(colName, table, colValue, isString, false);
                case "Begin": return Begin(colName, table, colValue, isString);
                case "End": return End(colName, table, colValue, isString);
                //case "Equal": return Equal(colName, table);
                case "Null":
                    //bool value = false;
                    //if (colValue != null && bool.TryParse(colValue.ToString(), out value) && value) ;
                    
                    //前面把bool值转为了1和0(为了类似IsEnabled等值用bool类型传值)
                    //true则is null ; false 则is not null
                    bool value = (int)colValue == 1 ? true : false;
                    //if (colValue != null) bool.TryParse(colValue.ToString(), out value)  ;

                    return Null(colName, table, value);
                default: return Equal(colName, table, colValue, isString);
            }
        }

        #region SQL拼接
        protected virtual string Equal(string colName, string tab, object colValue, bool isString = false)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}{1} = {2}", tab, colName, ToSqlLiteral(colValue, isString));
        }

        protected virtual string NotEqual(string colName, string tab, object colValue, bool isString = false)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}{1} <> {2}", tab, colName, ToSqlLiteral(colValue, isString));
        }

        protected virtual string Like(string colName, string tab, object colValue, bool isString = false, bool not = true)
        {
            var escapedValue = colValue?.ToString()?.Replace("'", "''", StringComparison.Ordinal) ?? string.Empty;
            return string.Format(CultureInfo.InvariantCulture, "{0}{1} {2}like '%{3}%'", tab, colName, not ? string.Empty : "not ", escapedValue);
        }
        //protected virtual string Like(string colName, string tab, bool not = true)
        //{
        //    return string.Format("{0}{1} {2}like '%'{3}{4}{1}{3}'%'", tab, colName, not ? string.Empty : "not ", cSymbol, pSymbol);
        //}
        protected virtual string Begin(string colName, string tab, object colValue, bool isString = false)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}{1} >= {2}", tab, colName, ToSqlLiteral(colValue, isString));
        }
        protected virtual string End(string colName, string tab, object colValue, bool isString = false)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}{1} <= {2}", tab, colName, ToSqlLiteral(colValue, isString));
        }
        protected virtual string Null(string colName, string tab, bool isNull)
        {
            return string.Format("{0}{1} is {2}null", tab, colName, isNull ? string.Empty : "not ");
        }

        private static string ToSqlLiteral(object value, bool isString)
        {
            if (value == null) return "null";
            if (!isString) return Convert.ToString(value, CultureInfo.InvariantCulture);
            return "'" + value.ToString().Replace("'", "''", StringComparison.Ordinal) + "'";
        }
        #endregion
    }
}
