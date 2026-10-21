using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScientificResearch.Models
{
    /// <summary>
    /// 完成步骤时,提交的数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class 完成步骤<T>
    {
        /// <summary>
        /// 完成哪个步骤
        /// </summary>
        public int 步骤编号 { get; set; }
        /// <summary>
        /// 完整的状态值,步骤类型状态
        /// </summary>
        public int 状态值 { get; set; }

        public string 备注 { get; set; }
        /// <summary>
        /// 附加动作的传入数据,比如任职打回去修改时,修改后的数据
        /// </summary>
        public T 附加数据 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public 完成步骤 toSimple()
        {
            return new 完成步骤()
            {
                步骤编号 = this.步骤编号,
                状态值 = this.状态值,
                备注 = this.备注
            };
        }
    }

    public class 完成步骤
    {
        /// <summary>
        /// 完成哪个步骤
        /// </summary>
        public int 步骤编号 { get; set; }
        /// <summary>
        /// 完整的状态值,步骤类型状态
        /// </summary>
        public int 状态值 { get; set; }

        public string 备注 { get; set; }

    }

    public class 专家利益冲突检查结果
    {
        public bool 通过 { get; set; }
        public int 流程模板编号 { get; set; }
        public int 项目编号 { get; set; }
        public string 说明 { get; set; }
    }

    public class 流程Filter
    {
        public bool? 是否可处理 { get; set; }
    }
}
