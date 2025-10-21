using System;
using System.Threading.Tasks;
using MSRewardsBot.Client.DataEntities;
using MSRewardsBot.Client.Services;
using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Client
{
    public class ViewModel
    {
        private ConnectionService _connection;
        private FileManager _fileManager;

        private AppData _appData;
        private AppInfo _appInfo;

        public ViewModel()
        {
            _connection = new ConnectionService();
            _fileManager = new FileManager();
        }

        public async void Init()
        {
            Microsoft.Playwright.Program.Main(["install"]);

            await _connection.ConnectAsync();
            _fileManager.LoadData(out _appData);
        }

        public void SetInstanceAppInfo(AppInfo appInfo)
        {
            _appInfo = appInfo;
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

            return true;
        }
    }
}
