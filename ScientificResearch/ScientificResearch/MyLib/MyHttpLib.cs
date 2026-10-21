using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MyLib
{
    /// <summary>
    /// http的封装帮助类
    /// </summary>
    public static class MyHttpLib
    {
        /// <summary>
        /// 将json字串转为url后的参数字串;
        /// </summary>
        /// <param name="jsonQuery"></param>
        /// <param name="isFirst"></param>
        /// <returns></returns>
        public static string JsonToQueryParam(string jsonQuery, bool isFirst = true)
        {
            string str = isFirst ? "?" : "&";
            str += jsonQuery.Replace(":", "=").Replace("{", "").
                        Replace("}", "").Replace(",", "&").
                            Replace("\"", "");
            return str;
        }

        /// <summary>
        /// 将object转为url后的参数字串,可以传多个object,不用管具体类型,可以序列化json即可
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ObjToQueryParam(params object[] obj)
        {
            var result = string.Empty;

            for (int i = 0; i < obj.Length; i++)
            {
                result += JsonToQueryParam(JsonConvert.SerializeObject(obj[i]), i == 0);
            }
            return result;
        }

        /// <summary>
        /// 使用httpClient的GetAsync获取get请求相应
        /// </summary>
        /// <typeparam name="TResult">返回类型的解析类</typeparam>
        /// <param name="url">请求url,不用包括参数</param>
        /// <param name="data">参数对象,可以多个</param>
        /// <returns></returns>
        public static async Task<TResult> MyGetAsync<TResult>(string url, params object[] data)
        {
            var httpClient = new HttpClient();

            //请求并,如果验证非200返回则报错;
            var response = await httpClient.GetAsync(url + MyHttpLib.ObjToQueryParam(data));
            response.EnsureSuccessStatusCode();

            //解析返回内容到string
            var responseString = await response.Content.ReadAsStringAsync();

            //解析为指定的对象;
            var responseObj = JsonConvert.DeserializeObject<TResult>(responseString);
            return responseObj;
        }

        /// <summary>
        /// 使用httpclient的postAsync,使用json作为contentType
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static async Task<TResult> MyPostAsync<TResult>(string url, object data)
        {
            var content = new StringContent(JsonConvert.SerializeObject(data));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var httpClient = new HttpClient();

            var response = await httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JsonConvert.DeserializeObject<TResult>(responseString);
            return responseObj;
        }
    }
}
