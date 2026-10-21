using Microsoft.AspNetCore.Hosting;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;


namespace MyLib
{
    /// <summary>
    /// xls文件操作
    /// </summary>
    public class MyXls
    {
        /// <summary>
        /// 上传excel,导入其中的数据,第一页,根据T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <param name="workSheetsName"></param>
        /// <param name="columnHeaderAt">列头在哪一行</param>
        /// <param name="dataStartFrom">数据从哪一行开始</param>
        /// <returns></returns>
        public static IEnumerable<T> Import<T>(string fileName, string workSheetsName = "sheet1", int columnHeaderAt = 1, int dataStartFrom = 2) where T : new()
        {
            var propertys = typeof(T).GetProperties();

            //ExcelPackage package = new ExcelPackage(new FileStream(fileName, FileMode.Open));
            ExcelPackage package = new ExcelPackage(new FileInfo(fileName));

            //第一页
            //ExcelWorksheet sheet = package.Workbook.Worksheets[1];
            ExcelWorksheet sheet = package.Workbook.Worksheets[workSheetsName];

            var listData = new List<T>();
            for (int row = dataStartFrom, n = sheet.Dimension.End.Row; row <= n; row++)
            {
                listData.Add(new T());
            }
            //轮询列
            for (int column = sheet.Dimension.Start.Column, k = sheet.Dimension.End.Column; column <= k; column++)
            {
                var columnName = sheet.GetValue<string>(columnHeaderAt, column);

                //轮询T的属性
                for (int i = 0; i < propertys.Count(); i++)
                {
                    var displayName = propertys[i].GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;

                    //如果找到了对应的属性
                    if (displayName == columnName)
                    {
                        //取该属性
                        var property = propertys[i];
                        //轮询行
                        for (int row = dataStartFrom, n = sheet.Dimension.End.Row; row <= n; row++)
                        {

                            var value = sheet.GetValue(row, column);
                            if (value == null) continue;
                            try
                            {
                                //给listData里面的容器赋值;
                                if (!property.PropertyType.IsGenericType)
                                {
                                    //非泛型
                                    property.SetValue(listData[row - dataStartFrom], Convert.ChangeType(value, property.PropertyType), null);
                                }
                                else
                                {
                                    Type genericTypeDefinition = property.PropertyType.GetGenericTypeDefinition();
                                    if (genericTypeDefinition == typeof(Nullable<>))
                                    //泛型Nullable<>
                                    {
                                        property.SetValue(listData[row - dataStartFrom], (value == null) ? null : Convert.ChangeType(value, Nullable.GetUnderlyingType(property.PropertyType)), null);
                                    }
                                    else
                                    {
                                        throw new Exception($"导入不解析对象");
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                throw new Exception($"第{row}行,第{column}列,数据缺失或错误");
                            }
                        }
                    }
                }

            }

            return listData;

        }


        /// <summary>
        /// 根据IEnumerable T类型的数据导出
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="folder"></param>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string Export<T>(string folder, IEnumerable<T> list, string name) where T : new()
        {
            //string folder = _hostingEnvironment.WebRootPath;

            var t = typeof(T);
            //var displayNameOfT = t.GetCustomAttribute<DisplayNameAttribute>().DisplayName;
            //var displayNameOfT = string.Join(",", displayNameListOfT.Select(i => i.DisplayName));
            //if (string.IsNullOrWhiteSpace(displayNameOfT)) displayNameOfT = "未指定类型显示名称";
            //var pName = t.GetProperty("Name");
            //var displayName = pName.GetCustomAttribute<DisplayNameAttribute>();

            var displayNameOfT = Path.Combine("excel", name);

            string sFileName = $"{displayNameOfT}{Guid.NewGuid()}.xlsx";
            FileInfo file = new FileInfo(Path.Combine(folder, sFileName));

            using (ExcelPackage package = new ExcelPackage(file))
            {
                // 添加worksheet
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(displayNameOfT);

                var propertys = t.GetProperties();
                for (int i = 0; i < propertys.Count(); i++)
                {
                    var displayName = propertys[i].GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
                    if (string.IsNullOrWhiteSpace(displayName) || displayName == "未指定")
                    {
                        continue;
                    }
                    worksheet.Cells[1, i + 1].Value = displayName;

                    //获取DisplayFormatAttribute的DataFormatString
                    //var displayFormat = propertys[i].GetCustomAttribute<DisplayFormatAttribute>()?.DataFormatString;

                    var propertyName = propertys[i].Name;
                    for (int j = 0; j < list.Count(); j++)
                    {

                        var item = list.Skip(j).FirstOrDefault();
                        var value = item.GetType().GetProperty(propertyName).GetValue(item)?.ToString();
                        //var value = item.GetType().GetProperty(propertyName).GetValue(item);
                        //var displayValue = displayFormat==null ? value : string.Format( displayFormat,value);
                        //worksheet.Cells[j + 2, i + 1].Value = displayValue;

                        //2018/2/26 写死了有"金额"字样则做特定格式化.应该根据sql的额外描述,获取DisplayFormatAttribute的DataFormatString,然后由propertys[i](确定是哪种数值或datetime,别的不行)和DataFormatString来使用string.Format格式化.c#的反射其实很烦又臃肿.
                        //if (displayName.Contains("金额"))
                        //{
                        //    value = string.Format("{0:C2}", Convert.ToInt32(value));
                        //}

                        worksheet.Cells[j + 2, i + 1].Value = value;
                    }
                }

                ////添加头
                //worksheet.Cells[1, 1].Value = "ID";
                //worksheet.Cells[1, 2].Value = "Name";
                //worksheet.Cells[1, 3].Value = "Url";
                ////添加值
                //worksheet.Cells["A2"].Value = 1000;
                //worksheet.Cells["B2"].Value = "LineZero";
                //worksheet.Cells["C2"].Value = "http://www.cnblogs.com/linezero/";

                //worksheet.Cells["A3"].Value = 1001;
                //worksheet.Cells["B3"].Value = "LineZero GitHub";
                //worksheet.Cells["C3"].Value = "https://github.com/linezero";
                //worksheet.Cells["C3"].Style.Font.Bold = true;

                package.Save();
            }

            return sFileName;
            //return File(sFileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
    }
}
