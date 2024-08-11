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

            // ****** ����ע�룺�ֶ�ע����񣬲���ָ��������������� ******
            //builder.Services.AddSingleton<IAppSettingService, AppSettingService>();
            //builder.Services.AddScoped<ITestService, TestService>();

            // ****** ����ע�룺ʹ�÷�������ע����񣬸���ÿ�������Ϸ����Զ�������[SingletonService]���жϸ÷������������ ****** 
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
