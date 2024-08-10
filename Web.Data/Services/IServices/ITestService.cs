using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.Data.Services.IServices
{
    public interface ITestService
    {
        public string GetServerName();

        public string GetAppName();
    }
}
