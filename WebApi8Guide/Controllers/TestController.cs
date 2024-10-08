using Microsoft.AspNetCore.Mvc;
using Web.Data.Services;
using Web.Data.Services.IServices;
using WebApi8Guide.Models;

namespace WebApi8Guide.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class TestController : ControllerBase
    {
        private readonly ITestService _testService;
        public TestController(ITestService testService) => _testService = testService;

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        [HttpGet]
        public List<WeatherForecast> GetWeathers() => 
            Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }).ToList();

        /// <summary>
        /// 接口测试数据库是否连接成功
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetServerName()
        {
            string serverName = _testService.GetServerName();
            return Ok(serverName);
        }

        public ActionResult<string> GetAppName()
        {
            string appName = _testService.GetAppName();
            return appName;
        }
    }
}
