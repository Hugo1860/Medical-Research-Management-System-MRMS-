using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScientificResearch.Business
{
    public class TestBusiness
    {

        //public TestBusiness(IConfiguration configuration)
        //{
        //    Configuration = configuration;
        //}
        //public IConfiguration Configuration { get; }

        //public string GetConfig()
        //{
        //    return Configuration.ToString();
        //}

        public object TestJObject()
        {
            JObject staff = JObject.Parse(JsonConvert.SerializeObject(new {age = 1}));
          
            var x = staff["age"];
            var y = x.ToString();
            return staff.ToString();
        }
    }
}
