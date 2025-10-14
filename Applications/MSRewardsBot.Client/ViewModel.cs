using MSRewardsBot.Client.Services;

namespace MSRewardsBot.Client
{
    public class ViewModel
    {
        private ConnectionService _connection;

        public ViewModel()
        {
            _connection = new ConnectionService();
        }

        public async void Init()
        {
            Microsoft.Playwright.Program.Main(["install"]);

            await _connection.ConnectAsync();
        }
    }
}
