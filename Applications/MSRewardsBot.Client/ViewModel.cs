using System;
using System.Threading.Tasks;
using MSRewardsBot.Client.DataEntities;
using MSRewardsBot.Client.Services;
using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Client
{
    public class ViewModel
    {
        public bool IsLogged =>
            _appData != null &&
            _appData.AuthToken.HasValue &&
            _appData.AuthToken.Value != Guid.Empty;

        private ConnectionService _connection;
        private FileManager _fileManager;

        private AppData _appData;
        private AppInfo _appInfo;

        public ViewModel(AppInfo appInfo)
        {
            _appInfo = appInfo;

            _connection = new ConnectionService(_appInfo);
            _fileManager = new FileManager();
        }

        public async Task Init()
        {
            Microsoft.Playwright.Program.Main(["install"]);

            await _connection.ConnectAsync();
            _fileManager.LoadData(out _appData);

            _appInfo.IsUserLogged = IsLogged;
        }

        public void SetAuthToken(Guid token)
        {
            _appData.AuthToken = token;
            _fileManager.SaveData(_appData);
        }


        public async Task<bool> Login(User user)
        {
            Guid token = await _connection.Login(user);
            SetAuthToken(token);

            if (token == Guid.Empty)
            {
                return false;
            }

            _appInfo.IsUserLogged = true;
            return true;
        }

        public async Task<bool> Register(User user)
        {
            Guid token = await _connection.Register(user);
            SetAuthToken(token);

            if (token == Guid.Empty)
            {
                return false;
            }

            _appInfo.IsUserLogged = true;
            return true;
        }
    }
}
