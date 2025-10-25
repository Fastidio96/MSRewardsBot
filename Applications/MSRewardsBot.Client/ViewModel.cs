using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using MSRewardsBot.Client.DataEntities;
using MSRewardsBot.Client.Services;
using MSRewardsBot.Client.Windows;
using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Client
{
    public class ViewModel
    {
        public bool IsLogged =>
            _appData != null &&
            _appData.AuthToken.HasValue &&
            _appData.AuthToken.Value != Guid.Empty;

        private Guid _token => _appData.AuthToken.Value;

        private ConnectionService _connection;
        private FileManager _fileManager;

        private AppData _appData;
        private AppInfo _appInfo;

        public ViewModel(AppInfo appInfo)
        {
            _appInfo = appInfo;
            _appInfo.PropertyChanged += AppInfo_PropertyChanged;

            _connection = new ConnectionService(_appInfo);
            _fileManager = new FileManager();
        }

        private async void AppInfo_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppInfo.IsUserLogged) && IsLogged)
            {
                await GetUserInfo();
            }
        }

        public async Task Init()
        {
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

            return _appInfo.IsUserLogged;
        }

        public async Task<User> GetUserInfo()
        {
            User user = await _connection.GetUserInfo(_token);
            if (user == null)
            {
                Logout();
                return null;
            }

            _appInfo.Username = user.Username;

            _appInfo.Accounts.Clear();
            foreach (MSAccount acc in user.MSAccounts)
            {
                _appInfo.Accounts.Add(acc);
            }

            return user;
        }

        private void Logout()
        {
            _appData = new AppData();
            _fileManager.SaveData(_appData);

            //Environment.Exit(0);
        }

        public void AddMSAccount()
        {
            MSLoginWindow login = new MSLoginWindow(this);
            login.Owner = App.Current.MainWindow;
            login.Show();
        }
    }
}
