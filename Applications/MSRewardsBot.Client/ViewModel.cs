using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MSRewardsBot.Client.DataEntities;
using MSRewardsBot.Client.Services;
using MSRewardsBot.Client.Windows;
using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Client
{
    public class ViewModel : IDisposable
    {
        public bool IsLogged => _appInfo.IsUserLogged;
        private Guid _token => !_appData.AuthToken.HasValue ? Guid.Empty : _appData.AuthToken.Value;

        private ConnectionService _connection;
        private FileManager _fileManager;

        private AppData _appData;
        private AppInfo _appInfo;

        private MainWindow _mainWindow;
        private SplashScreenWindow _splashScreenWindow;
        private MSLoginWindow _MSLoginWindow;
        private UserLoginWindow _userLoginWindow;

        public ViewModel(SplashScreenWindow splashScreenWindow)
        {
            _splashScreenWindow = splashScreenWindow;
        }

        public async void Init()
        {
            _appInfo = new AppInfo();

            _connection = new ConnectionService(_appInfo);
            _fileManager = new FileManager();

            await _connection.ConnectAsync();
            if (!_fileManager.LoadData(out _appData))
            {
                _appInfo.IsUserLogged = false;
                return;
            }
            else
            {
                _appInfo.IsUserLogged = _token != Guid.Empty &&
                    await _connection.LoginWithToken(_token);
            }

            _mainWindow = new MainWindow(this, _splashScreenWindow, _appInfo);
            App.Current.MainWindow = _mainWindow;

            if (_appInfo.IsUserLogged)
            {
                _mainWindow.Show();
                await GetUserInfo();
            }
            else
            {
                _userLoginWindow = new UserLoginWindow(this, _splashScreenWindow);
                _userLoginWindow.Show();
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
            if (_token != Guid.Empty)
            {
                _connection.Logout(_token);
            }

            _appData = new AppData();
            _fileManager.SaveData(_appData);

            Dispose();
            Init();
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

        public void Dispose()
        {
            if (_userLoginWindow != null)
            {
                if (_userLoginWindow.IsVisible)
                {
                    _userLoginWindow.Close();
                }

                _userLoginWindow = null;
            }

            if (_MSLoginWindow != null)
            {
                if (_MSLoginWindow.IsVisible)
                {
                    _MSLoginWindow.Close();
                }

                _MSLoginWindow = null;
            }

            if (_mainWindow != null)
            {
                if (_mainWindow.IsVisible)
                {
                    _mainWindow.Close();
                }

                _mainWindow = null;
                App.Current.MainWindow = null;
            }

            if (_connection != null)
            {
                _connection.DisconnectAsync().Wait();
                _connection = null;
            }

            _fileManager = null;
            _appData = null;
            _appInfo = null;
        }
    }
}
