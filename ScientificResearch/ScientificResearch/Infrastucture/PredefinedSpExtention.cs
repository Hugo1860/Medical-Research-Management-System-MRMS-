using Dapper;
using MyLib;
using ScientificResearch.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Dapper.SqlMapper;

namespace ScientificResearch.Infrastucture
{
    /// <summary>
    /// 预定义的主键,就是这个主键名称是固定的而已
    /// </summary>
    public class PredefindedKeyFields
    {
        public int 编号 { get; set; }
    }

    /// <summary>
    /// 一个Id+一个T的列表的结构,很多sp用,对接口来说,就不需要自定义post的传入类了;
    /// 当然要转换为sp类还是手动来的好;
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PredefindedIdList<T>
    {
        public int Id { get; set; }
        public IEnumerable<T> List { get; set; }
    }

    /// <summary>
    /// 类似上面那个
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TList"></typeparam>
    public class PredefindedModelList<T, TList>
    {
        public T model { get; set; }
        public IEnumerable<TList> list { get; set; }
    }

    /// <summary>
    /// GetModelById
    /// 一些预定义的sp的dapper调用
    /// </summary>
    public static class PredefinedSpExtention
    {
        /// <summary>
        /// 预定义的主键,非要叫"编号"而不叫"Id"我有啥法
        /// </summary>
        private const string PredefindedKeyFields = "编号";

        private static string RoutineSqlName<T>()
        {
            var name = typeof(T).Name;
            if (name.Any(char.IsControl) || name.Contains('"'))
            {
                throw new ArgumentException("例程名称无效", nameof(T));
            }

            return "\"" + name.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        private static string ToJsonPayload<T>(T model)
        {
            if (model == null) return "{}";

            var payload = new Dictionary<string, object>();
            foreach (var property in model.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var value = property.GetValue(model);
                if (value is DataTable table)
                {
                    var rows = table.Rows.Cast<DataRow>().Select(row =>
                        table.Columns.Cast<DataColumn>().ToDictionary(
                            column => column.ColumnName,
                            column => row[column] == DBNull.Value ? null : row[column]));
                    payload[property.Name] = rows.ToArray();
                }
                else
                {
                    payload[property.Name] = value;
                }
            }

            return JsonSerializer.Serialize(payload);
        }

        private static string QuoteIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Any(char.IsControl) || name.Contains('"'))
                throw new ArgumentException("数据库标识符无效", nameof(name));
            return "\"" + name.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        private static string QuoteFieldList(string fields, string fallback)
        {
            var value = string.IsNullOrWhiteSpace(fields) ? fallback : fields;
            if (value.Trim() == "*") return "*";
            var quoted = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(item =>
                {
                    var match = Regex.Match(item, "^(?<name>[\\p{L}\\p{N}_]+)(?:\\s+(?<direction>ASC|DESC))?$", RegexOptions.IgnoreCase);
                    if (!match.Success) throw new ArgumentException("排序字段无效");
                    var result = QuoteIdentifier(match.Groups["name"].Value);
                    if (match.Groups["direction"].Success)
                        result += " " + match.Groups["direction"].Value.ToUpperInvariant();
                    return result;
                });
            return string.Join(", ", quoted);
        }

        private static string BuildSource(string source)
        {
            var value = string.IsNullOrWhiteSpace(source) ? throw new ArgumentException("查询对象不能为空", nameof(source)) : source.Trim();
            var function = Regex.Match(value, "^(?<name>[\\p{L}\\p{N}_]+)\\((?<args>.*)\\)$", RegexOptions.Singleline);
            if (function.Success)
            {
                var args = ParseSourceArguments(function.Groups["args"].Value);
                return QuoteIdentifier(function.Groups["name"].Value) + "(" + string.Join(", ", args) + ")";
            }
            return QuoteIdentifier(value);
        }

        private static IEnumerable<string> ParseSourceArguments(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();

            var result = new List<string>();
            var index = 0;
            while (index < text.Length)
            {
                while (index < text.Length && char.IsWhiteSpace(text[index])) index++;
                if (index >= text.Length) break;

                if (text[index] == '\'')
                {
                    var start = index++;
                    var closed = false;
                    while (index < text.Length)
                    {
                        if (text[index] != '\'')
                        {
                            index++;
                            continue;
                        }

                        if (index + 1 < text.Length && text[index + 1] == '\'')
                        {
                            index += 2;
                            continue;
                        }

                        index++;
                        closed = true;
                        break;
                    }

                    if (!closed) throw new ArgumentException("查询参数中的字符串未闭合", nameof(text));
                    result.Add(text[start..index]);
                }
                else
                {
                    var start = index;
                    while (index < text.Length && !char.IsWhiteSpace(text[index]) && text[index] != ',') index++;
                    var token = text[start..index];
                    if (!Regex.IsMatch(token, "^(?:NULL|[-+]?\\d+)$", RegexOptions.IgnoreCase))
                        throw new ArgumentException("查询参数只能是整数、NULL 或 SQL 字符串字面量", nameof(text));
                    result.Add(token.Equals("NULL", StringComparison.OrdinalIgnoreCase)
                        ? "NULL"
                        : int.Parse(token, System.Globalization.CultureInfo.InvariantCulture).ToString(System.Globalization.CultureInfo.InvariantCulture));
                }

                while (index < text.Length && char.IsWhiteSpace(text[index])) index++;
                if (index >= text.Length) break;
                if (text[index] != ',') throw new ArgumentException("查询参数分隔符无效", nameof(text));
                index++;
            }

            return result;
        }

        private static string BuildWhere<TFilter>(TFilter filter, DynamicParameters parameters)
            where TFilter : class
        {
            if (filter == null) return "TRUE";
            var clauses = new List<string>();
            var index = 0;
            foreach (var property in filter.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var value = property.GetValue(filter);
                if (value == null) continue;
                var match = Regex.Match(property.Name, "^(?<prefix>NotEqual|PK|Begin|End|Like|UnLike|Null)?(?<name>[\\p{L}\\p{N}_]+)$");
                if (!match.Success) continue;
                var prefix = match.Groups["prefix"].Value;
                var rawName = match.Groups["name"].Value;
                if (prefix == "Null")
                {
                    var nullName = QuoteIdentifier(rawName);
                    clauses.Add($"{nullName} IS {(Convert.ToBoolean(value) ? string.Empty : "NOT ")}NULL");
                    continue;
                }
                var parameterName = "@filter" + index++;
                if (prefix is "Like" or "UnLike")
                    parameters.Add(parameterName, "%" + value + "%");
                else
                    parameters.Add(parameterName, value);
                if (property.Name == "LikeAllName")
                {
                    clauses.Add($"to_jsonb(data)::text {(prefix == "UnLike" ? "NOT " : string.Empty)}LIKE {parameterName}");
                    continue;
                }
                var fieldName = QuoteIdentifier(rawName);
                var op = prefix switch
                {
                    "NotEqual" => "<>",
                    "Begin" => ">=",
                    "End" => "<=",
                    "Like" => "LIKE",
                    "UnLike" => "NOT LIKE",
                    _ => "="
                };
                clauses.Add($"{fieldName} {op} {parameterName}");
            }
            return clauses.Count == 0 ? "TRUE" : string.Join(" AND ", clauses);
        }

        /// <summary>
        /// 把IEnumerable int类型的编号列表,转为PredefindedKeyFieldsList列表,后者可以转为一个dataTable,是只有一个字段 编号 的表
        /// </summary>
        /// <param name="idList"></param>
        /// <returns></returns>
        public static IEnumerable<PredefindedKeyFields> ToPredefindedKeyFieldsList(this IEnumerable<int> idList)
        {
            return idList.Select(i => new PredefindedKeyFields()
            {
                编号 = i
            });
        }

        #region 画蛇添足的
        ////private const string PredefinedNameOfTvpParameterInMergeSp = "tt";  //这个使用失败,动态的object的属性名称
        ///// <summary>
        ///// 这3个方法在codesmith映射sp的结构以后,就多余了
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <returns></returns>
        ////public static string GetMergeSpNameByClassName<T>() => $"sp_{typeof(T).Name}_增改";

        ///// <summary>
        ///// 单表增改的参数;也算预定义的了;
        ///// 这3个方法在codesmith映射sp的结构以后,就多余了
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="model"></param>
        ///// <returns></returns>
        ////public static object GetMergeSpParamByModel<T>(T model) where T : new()
        ////{
        ////    return new { tt = model.ToDataTable() };
        ////}

        ///// <summary>
        ///// 这3个方法在codesmith映射sp的结构以后
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="list"></param>
        ///// <returns></returns>
        ////public static object GetMergeSpParamByModel<T>(IEnumerable<T> list) where T : new()
        ////{
        ////    return new { tt = list.ToDataTable() };
        ////}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <returns></returns>
        ////public static string GetDeleteSpNameByClassName<T>() => $"sp_{typeof(T).Name}_删";
        ///// <summary>
        /////  画蛇添足
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <returns></returns>
        ////public static string GetEnableDisableSpNameByClassName<T>() => $"sp_{typeof(T).Name}_启禁";
        #endregion

        /// <summary>
        /// 通过主键获得对象,或者null
        /// T为表映射而来的类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        async public static Task<T> GetModelByIdSpAsync<T>(this IDbConnection cnn, int id)
        {
            var result = await cnn.QueryAsync<T>($"SELECT * FROM {BuildSource(typeof(T).Name)} WHERE {QuoteIdentifier(PredefindedKeyFields)} = @id ORDER BY {QuoteIdentifier(PredefindedKeyFields)} DESC", new { id });
            return result.FirstOrDefault();
        }

        /// <summary>
        /// sp_GetList,带搜索条件的
        /// T的作用有2:1默认用T的名称做TbName,2dapper返回类型的指定
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TFilter"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="filter"></param>
        /// <param name="tbName"></param>
        /// <param name="keyFields"></param>
        /// <param name="orderType"></param>
        /// <param name="orderStr"></param>
        /// <returns></returns>
        async public static Task<IEnumerable<T>> GetListSpAsync<T, TFilter>(
            this IDbConnection cnn,
            TFilter filter = null,
            string tbName = null,
            string keyFields = PredefindedKeyFields,
            bool orderType = false,
            string orderStr = "",
            string tbFields = "*") where TFilter : class
        {
            var parameters = new DynamicParameters();
            var source = BuildSource(tbName ?? typeof(T).Name);
            var where = BuildWhere(filter, parameters);
            var order = QuoteFieldList(string.IsNullOrWhiteSpace(orderStr) ? null : orderStr,
                keyFields ?? PredefindedKeyFields);
            if (orderType) order += " ASC";
            else order += " DESC";
            return await cnn.QueryAsync<T>($"SELECT {QuoteFieldList(tbFields, "*")} FROM {source} AS data WHERE {where} ORDER BY {order}", parameters);
        }

        /// <summary>
        /// 没有搜索条件的单独传参数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="tbName"></param>
        /// <param name="keyFields"></param>
        /// <param name="orderType"></param>
        /// <param name="orderStr"></param>
        /// <returns></returns>
        async public static Task<IEnumerable<T>> GetListSpAsync<T>(
            this IDbConnection cnn,
            string tbName = null,
            string keyFields = null,
            bool orderType = false,
            string orderStr = "")
        {
            return await cnn.GetListSpAsync<T, object>(null, tbName, keyFields, orderType, orderStr);
        }

        /// <summary>
        /// 有条件的分页获取
        /// T的作用有2:1默认用T的名称做TbName,2dapper返回类型的指定
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TFilter"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="paging"></param>
        /// <param name="filter"></param>
        /// <param name="tbName"></param>
        /// <param name="keyFields"></param>
        /// <param name="orderStr"></param>
        /// <returns></returns>
        async public static Task<PagingResult<T>> GetPagingListSpAsync<T, TFilter>(
            this IDbConnection cnn,
            Paging paging = null,
            TFilter filter = null,
            string tbName = null,
            string keyFields = null,
            string orderStr = "") where TFilter : class
        {
            if (paging == null) paging = new Paging();
            var parameters = new DynamicParameters();
            var source = BuildSource(tbName ?? typeof(T).Name);
            var where = BuildWhere(filter, parameters);
            var order = QuoteFieldList(string.IsNullOrWhiteSpace(orderStr) ? null : orderStr,
                keyFields ?? PredefindedKeyFields);
            order += paging.OrderType ? " ASC" : " DESC";
            parameters.Add("@offset", Math.Max(0, paging.Index - 1) * Math.Max(1, paging.Size));
            parameters.Add("@limit", Math.Max(1, paging.Size));
            var sql = $"SELECT COUNT(1) FROM {source} AS data WHERE {where}; SELECT {QuoteFieldList("*", "*")} FROM {source} AS data WHERE {where} ORDER BY {order} OFFSET @offset LIMIT @limit;";
            using var result = await cnn.QueryMultipleAsync(sql, parameters);
            var total = result.Read<int>().FirstOrDefault();
            var list = result.Read<T>().ToArray();
            return new PagingResult<T> { Total = total, List = list };
        }

        /// <summary>
        /// 没有搜索条件的,返回分页数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="paging"></param>
        /// <param name="tbName"></param>
        /// <param name="keyFields"></param>
        /// <param name="orderStr"></param>
        /// <returns></returns>
        async public static Task<PagingResult<T>> GetPagingListSpAsync<T>(
            this IDbConnection cnn,
            Paging paging = null,
            string tbName = null,
            string keyFields = null,
            string orderStr = "")
        {
            return await cnn.GetPagingListSpAsync<T, object>(paging, null, tbName, keyFields, orderStr);
        }

        /// <summary>
        /// 异步执行一个sp
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="model">sp映射而来的类对象,不能为null...</param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public static Task<int> ExecuteSpAsync<T>(this IDbConnection cnn, T model, IDbTransaction transaction = null)
        {
            var sql = $"SELECT {RoutineSqlName<T>()}(@payload::jsonb)";
            return cnn.QueryFirstAsync<int>(sql, new { payload = ToJsonPayload(model) }, transaction);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="model"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public static Task<IEnumerable<TOut>> QuerySpAsync<T, TOut>(this IDbConnection cnn, T model, IDbTransaction transaction = null)
        {
            var sql = $"SELECT * FROM {RoutineSqlName<T>()}(@payload::jsonb)";
            return cnn.QueryAsync<TOut>(sql, new { payload = ToJsonPayload(model) }, transaction);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="model"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public static Task<GridReader> QueryMultipleSpAsync<T>(this IDbConnection cnn, T model, IDbTransaction transaction = null)
        {
            return cnn.QueryMultipleAsync(typeof(T).Name, model, transaction, commandType: CommandType.StoredProcedure);
        }


        #region 根据sp中对删除和启禁用的命名约定,调用相应的sp的方法,已废弃,直接用上面的几个"dapper原生的操作sp的方法"+"映射而来的sp类"结合而得到的调用sp的方法
        ///// <summary>
        ///// 增改一个表.单个对象.其中sp名称和tt的名称是按约定从表名构成的;
        ///// 2018-5-10 将增改的sp里面生成的参数名都改为tt,并不需要变化对吧!
        ///// 2018-5-16 修改为可以返回插入后值的queryasync方法,sp里面也要做这个约定
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="cnn"></param>
        ///// <param name="model"></param>
        ///// <param name="transaction"></param>
        ///// <returns></returns>
        //async public static Task<int> MergeSpAsync<T>(this IDbConnection cnn, T model, IDbTransaction transaction = null) where T : new()
        //{
        //    //给obj动态添加动态属性
        //    //dynamic obj = new System.Dynamic.ExpandoObject();
        //    //((IDictionary<string, object>)obj).Add(ttName, model.ToDataTable());
        //    //await cnn.MyExecuteSpAsync(spName, (object)obj);
        //    var result = await cnn.QueryAsync<int>(
        //        GetMergeSpNameByClassName<T>(),
        //        GetMergeSpParamByModel<T>(model),
        //        transaction,
        //        commandType: CommandType.StoredProcedure);
        //    return result.FirstOrDefault();
        //}



        ///// <summary>
        ///// 上面的姊妹,增改多条记录.
        ///// 2018-5-16 修改为可以返回插入后值的queryasync方法
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="cnn"></param>
        ///// <param name="list"></param>
        ///// <param name="transaction"></param>
        ///// <returns></returns>
        //async public static Task<IEnumerable<int>> MergeSpAsync<T>(
        //    this IDbConnection cnn,
        //    IEnumerable<T> list,
        //    IDbTransaction transaction = null) where T : new()
        //{
        //    return await cnn.QueryAsync<int>(
        //        GetMergeSpNameByClassName<T>(),
        //        GetMergeSpParamByModel<T>(list),
        //        transaction,
        //        commandType: CommandType.StoredProcedure);
        //}

        /// <summary>
        /// 删除sp的:sp的名称和T(表名)的对应关系,sp参数名称,编号列表这个tt的名称和类型,这些都需要有约定
        /// 比上面好一点的地方是"tt_编号"这个sp的参数名是可以固定下来的;
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="idList"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        //async public static Task DeleteSpAsync<T>(
        //    this IDbConnection cnn,
        //    IEnumerable<int> idList,
        //    IDbTransaction transaction = null) where T : new()
        //{
        //    await cnn.ExecuteAsync(
        //        GetDeleteSpNameByClassName<T>(),
        //        new
        //        {
        //            tt_编号 = idList.Select(i => new 编号列表()
        //            {
        //                编号 = i
        //            }).ToDataTable()
        //        },
        //        transaction,
        //        commandType: CommandType.StoredProcedure);
        //}

        /// <summary>
        /// 启禁某个表的某条记录,对应的sp名有特定约定;
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cnn"></param>
        /// <param name="id"></param>
        /// <param name="state"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        //async public static Task EnableDisableSpAsync<T>(
        //    this IDbConnection cnn,
        //    int id,
        //    bool state,
        //    IDbTransaction transaction = null) where T : new()
        //{
        //    await cnn.ExecuteAsync(
        //        GetEnableDisableSpNameByClassName<T>(),
        //        new { 编号 = id, 是否启用 = state },
        //        transaction,
        //        commandType: CommandType.StoredProcedure);
        //}
        #endregion
    }
}
