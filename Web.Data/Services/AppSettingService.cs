using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Web.Data.Models;
using Web.Data.Services.IServices;

namespace Web.Data.Services
{
    public class AppSettingService : IAppSettingService
    {
        public static IOptionsMonitor<AppSetting> _appSettings;
        public AppSettingService(IOptionsMonitor<AppSetting> configuration) => _appSettings = configuration;

        public string GetAppName() => _appSettings.CurrentValue.AppName;

        public AppSetting GetAppSettings() => _appSettings.CurrentValue;

        public string GetConnectionString() => _appSettings.CurrentValue.DefaultDb;
    }
}
