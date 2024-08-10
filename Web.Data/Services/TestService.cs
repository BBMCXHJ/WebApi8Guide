using System;
using System.Data;
using Web.Data.Services.IServices;
using Web.Data.SQLHelper;

namespace Web.Data.Services
{
    public class TestService : ITestService
    {
        private readonly IAppSettingService _appSettingsService;

        public TestService(IAppSettingService appSettingsService) => _appSettingsService = appSettingsService;

        /// <summary>
        /// 获取数据库服务器名
        /// </summary>
        /// <returns>string</returns>
        public string GetServerName() => SqlHelper.ExecuteScalar<string>(_appSettingsService.GetConnectionString(), CommandType.Text, "SELECT @@SERVERNAME");

        public string GetAppName() => _appSettingsService.GetAppName();
    }
}
