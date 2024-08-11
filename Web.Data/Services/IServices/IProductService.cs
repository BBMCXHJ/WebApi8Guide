using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Web.Data.Attributes;

namespace Web.Data.Services.IServices
{
    public interface IProductService
    {
        List<string> GetProduction();

    }
}
