using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using MSRewardsBot.Client.DataEntities;
using MSRewardsBot.Client.Services;
using MSRewardsBot.Client.Windows;
using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Client
{
    public class ViewModel
    {
        public bool IsLogged => _appInfo.IsUserLogged;
        private Guid _token => !_appData.AuthToken.HasValue ? Guid.Empty : _appData.AuthToken.Value;

        private ConnectionService _connection;
        private FileManager _fileManager;

        private AppData _appData;
        private AppInfo _appInfo;

        private MSLoginWindow _MSLoginWindow;

        public ViewModel(AppInfo appInfo)
        {
            _appInfo = appInfo;

            _connection = new ConnectionService(_appInfo);
            _fileManager = new FileManager();
        }


        public async Task Init()
        {
            await _connection.ConnectAsync();
            _fileManager.LoadData(out _appData);

            _appInfo.IsUserLogged = await _connection.LoginWithToken(_token);

            if (_appInfo.IsUserLogged)
            {
                await GetUserInfo();
            }
            else
            {
                Logout();
            }
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

            _connection.Logout(_token);

            if (Utils.ShowMessage("The application is closing.", "Info", MessageBoxImage.Information) == MessageBoxResult.OK)
            {
                Environment.Exit(0);
            }
        }

        public void AddMSAccount()
        {
            if (_MSLoginWindow != null && _MSLoginWindow.IsVisible)
            {
                return;
            }

            _MSLoginWindow = new MSLoginWindow(this);
            _MSLoginWindow.Owner = App.Current.MainWindow;
            _MSLoginWindow.Show();
        }

        public Task<bool> InsertMSAccount(List<AccountCookie> cookies)
        {
            MSAccount acc = new MSAccount()
            {
                Cookies = cookies
            };

            return _connection.InsertMSAccount(_token, acc);
        }
    }
}
