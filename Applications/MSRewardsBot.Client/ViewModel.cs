using MSRewardsBot.Client.DataEntities;
using MSRewardsBot.Client.Services;

namespace MSRewardsBot.Client
{
    public class ViewModel
    {
        private ConnectionService _connection;
        private AppInfo _appInfo;

        public ViewModel()
        {
            _connection = new ConnectionService();
        }

        public async void Init()
        {
            Microsoft.Playwright.Program.Main(["install"]);

            await _connection.ConnectAsync();
        }

        public void SetInstanceAppInfo(AppInfo appInfo)
        {
            _appInfo = appInfo;
        }
    }
}
