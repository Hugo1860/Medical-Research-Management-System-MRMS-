//和log4net_cmm2_bak.xml配合做的错误记录,现在在startup里面抓取到错误就记录.
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net;
//using System.Net.Http;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Web;
//using System.Web.Http;

//using System.Text;


//namespace Account
//{
//    /// <summary>
//    /// API自定义错误消息处理委托类。
//    /// 用于处理访问不到对应API地址的情况，对错误进行自定义操作。
//    /// </summary>
//    public class ErrorMessageDelegatingHandler : DelegatingHandler
//    {
//        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
//        {
//            return base.SendAsync(request, cancellationToken).ContinueWith<HttpResponseMessage>((responseToCompleteTask) =>
//            {
//                HttpResponseMessage response = responseToCompleteTask.Result;
//                HttpError error = null;
//                response.TryGetContentValue<HttpError>(out error);

//                if (error != null)
//                {
//                    //记录错误日志
//                    string msg = error.Message;
//                    Int64 timeStamp = Cmm2.MyLib.Tool.GetTimeStamp();
//                    string ExceptionType = "";
//                    string ExceptionMessage = "";
//                    if (error.Count > 1)
//                    {
//                        ExceptionMessage = error["ExceptionMessage"].ToString();
//                        ExceptionType = error["ExceptionType"].ToString();
//                        msg = string.Format("{1}{0}{2}{0}{3}", "\r\n", ExceptionMessage, ExceptionType, error["StackTrace"].ToString());

//                    }
//                    HttpContextBase context = (HttpContextBase)request.Properties["MS_HttpContext"];//获取传统context     
//                    HttpRequestBase Request = context.Request;
//                    string req = "";
//                    if (Request.HttpMethod == "GET" || Request.HttpMethod == "Delete")
//                    {

//                    }
//                    else
//                    {
//                        // byte[] byts = new byte[Request.InputStream.Length];
//                        // Request.InputStream.Read(byts, 0, byts.Length);
//                        // req = System.Text.Encoding.UTF8.GetString((byts));

//                        //byte[] byts = new byte[Request.InputStream.Length];
//                        //Request.InputStream.Read(byts, 0, byts.Length);
//                        //req = System.Text.Encoding.Default.GetString(byts);

//                        StringBuilder sb = new StringBuilder("{");
//                        for (var i = 0; i < Request.Form.AllKeys.Length; i++)
//                        {
//                            if (i > 0)
//                            {
//                                sb.Append(",");
//                            }
//                            string key = Request.Form.AllKeys[i];
//                            string value = Request.Form[key];
//                            sb.Append(string.Format("{0}:{1}", key, value));

//                        }
//                        sb.Append("}");
//                        req = sb.ToString();
//                    }

//                    msg = string.Format("{2}{0}登录信息：{5}{0}URL:{4}{0}提交的数据:{1}{0}错误信息:{3}{0}",
//                            "\r\n",
//                            req,
//                            timeStamp,
//                            msg,
//                            Request.RawUrl,
//                            "现在还没有登录信息");

//                    new Exception(msg).Error();

//                    //response.StatusCode = HttpStatusCode.OK;
//                    //response.Content = new StringContent(result.ToJson());
//                }
//                return response;
//            });
//        }
//    }
//}