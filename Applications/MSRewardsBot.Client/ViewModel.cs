using System.Threading.Tasks;
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

        public Task Init()
        {
            Microsoft.Playwright.Program.Main(["install"]);

            return _connection.ConnectAsync();
        }

        public void SetInstanceAppInfo(AppInfo appInfo)
        {
            _appInfo = appInfo;
        }
    }
}
