using Web.Data;
using Web.Data.Models;
using Web.Data.Services;
using Web.Data.Services.IServices;

namespace WebApi8Guide
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.Configure<AppSetting>(builder.Configuration.GetSection("ConnectionStrings"));

            builder.Services.AddSingleton<IAppSettingService, AppSettingService>();
            builder.Services.AddScoped<ITestService, TestService>();

            builder.Services.AddControllers();

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
