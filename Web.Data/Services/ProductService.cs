using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Web.Data.Attributes;
using Web.Data.Services.IServices;

namespace Web.Data.Services
{
    [TransientService]
    public class ProductService : IProductService
    {
        public List<string> GetProduction()
        {
            List<string> products = new List<string>() { "Product1", "Product2" };
            return products;
        }
    }
}
