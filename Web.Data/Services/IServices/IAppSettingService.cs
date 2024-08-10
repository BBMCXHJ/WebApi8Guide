using Web.Data.Models;

namespace Web.Data.Services.IServices
{
    public interface IAppSettingService
    {
        public AppSetting GetAppSettings();

        public string GetConnectionString();

        public string GetAppName();
    }
}
