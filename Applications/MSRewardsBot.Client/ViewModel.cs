using System;
using System.Threading.Tasks;
using MSRewardsBot.Client.DataEntities;
using MSRewardsBot.Client.Services;
using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Client
{
    public class ViewModel
    {
        private Guid _token;

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

        public void SetAuthToken(Guid token)
        {
            _token = token;
        }


        public Task<Guid> Login(User user)
        {
            return _connection.Login(user);
        }

        public Task<Guid> Register(User user)
        {
            return _connection.Register(user);
        }
    }
}
