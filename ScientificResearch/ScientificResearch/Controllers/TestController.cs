using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ScientificResearch.Business;
using Newtonsoft.Json.Linq;
using ScientificResearch.Infrastucture;

namespace ScientificResearch.Controllers
{
    [Produces("application/json")]
    [Consumes("application/json", "multipart/form-data")]//此处为新增
    [Route("[controller]/[action]")]
    public class TestController : Controller
    {
        [HttpGet]
        public object GetJObejctString()
        {
            return new TestBusiness().TestJObject();
        }

        [HttpGet]
        public object Get通过类名创建类实例()
        {
            Type type = Type.GetType("ScientificResearch.Models.部门");
            //type.Assembly.CreateInstance(type.Name)
            var obj = type.Assembly.CreateInstance(type.FullName);
            //var obj =Convert.ChangeType( type.Assembly.CreateInstance(type.FullName),type);
            //var obj = (ScientificResearch.Models.部门)type.Assembly.CreateInstance("ScientificResearch.Models.部门");

            return obj;
        }

        /// <summary>
        /// 结果是字段和方法都不会被取到;
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public object GetPropertyinfo会不会取到类的方法()
        {
            
            var type = typeof(MyClass);
            var pinfos = type.GetProperties();
            var jobj = new JObject();
            foreach (var pinfo in pinfos)
            {
                jobj.Add(new JProperty( pinfo.Name, 1));
            }

            return jobj.ToObject<object>();
        }
        class MyClass
        {
            public int 字段;
            public int 属性 { get; set; }
            public int 方法() { return 1; }
        }
    }
}