using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.Data.Models
{
    /// <summary>
    /// 映射appsettings.json中的配置
    /// </summary>
    public class AppSetting
    {
        public string DefaultDb { get; set; }

        public string AppName { get; set; }

    }
}
