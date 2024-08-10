﻿using Microsoft.Extensions.Configuration;
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
        public static AppSetting _appSettings;
        public AppSettingService(IConfiguration configuration)
        {
            _appSettings = configuration.GetSection("ConnectionStrings").Get<AppSetting>();
        }

        public string GetAppName()
        {
            return _appSettings.AppName;
        }

        public AppSetting GetAppSettings()
        {
            return _appSettings;
        }

        public string GetConnectionString()
        {
            return _appSettings.DefaultDb;
        }
    }
}
