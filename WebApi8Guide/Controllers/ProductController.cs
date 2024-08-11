using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Web.Data.Services.IServices;

namespace WebApi8Guide.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductController(IProductService productService)
        {
            _productService = productService;
        }
        [HttpGet]
        public List<string> GetProduction() 
        {
            return _productService.GetProduction();
        }
    }
}
