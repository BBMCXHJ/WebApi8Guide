using System.Reflection;
using Web.Data;
using Web.Data.Models;
using Web.Data.Services;
using Web.Data.Services.IServices;
using WebApi8Guide.Extensions;

namespace WebApi8Guide
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.Configure<AppSetting>(builder.Configuration.GetSection("ConnectionStrings"));

            // ****** 依赖注入：手动注入服务，并且指定服务的生命周期 ******
            //builder.Services.AddSingleton<IAppSettingService, AppSettingService>();
            //builder.Services.AddScoped<ITestService, TestService>();

            // ****** 依赖注入：使用反射依次注册服务，根据每个服务上方的自定义属性[SingletonService]来判断该服务的生命周期 ****** 
            builder.Services.AddServicesFromAssembly<IAppSettingService>(Assembly.GetAssembly(typeof(IAppSettingService)));
            builder.Services.AddServicesFromAssembly<ITestService>(Assembly.GetAssembly(typeof(ITestService)));
            builder.Services.AddServicesFromAssembly<IProductService>(Assembly.GetAssembly(typeof(IProductService)));

            builder.Services.AddControllers();

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
