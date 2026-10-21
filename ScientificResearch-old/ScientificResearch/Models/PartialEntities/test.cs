using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;

namespace ScientificResearch.Models
{
    [ModelMetadataType(typeof(RedPackMD))]
    public partial class test { }

    public class RedPackMD
    {
        [MaxLength(length: 100, ErrorMessage = "描述长度不能超过100")]
        public string Describe { get; set; }

        [MaxLength(length: 50, ErrorMessage = "口令长度不能超过50")]
        public string Word { get; set; }
    }
}
