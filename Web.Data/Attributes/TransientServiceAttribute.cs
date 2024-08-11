using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.Data.Attributes
{
    /// <summary>
    /// 标记服务：被该服务标记的Service将会被认为是瞬时的
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TransientServiceAttribute : Attribute
    {
    }
}
