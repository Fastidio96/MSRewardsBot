using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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

        private AppData _appData;
        private AppInfo _appInfo;

        private MainWindow _mainWindow;
        private SplashScreenWindow _splashScreenWindow;
        private MSLoginWindow _msLoginWindow;
        private UserLoginWindow _userLoginWindow;

        public ViewModel()
        {
        }

        public async void Init()
        {
            _appInfo = new AppInfo();
            _connection = new ConnectionService(_appInfo);

            _splashScreenWindow = new SplashScreenWindow(_appInfo);
            _splashScreenWindow.Show();

            await _connection.ConnectAsync();
            if (!FileManager.LoadData(out _appData))
            {
                _appInfo.IsUserLogged = false;
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
            FileManager.SaveData(_appData);
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

        public async Task<bool> GetUserInfo()
        {
            User user = await _connection.GetUserInfo(_token);
            if (user == null)
            {
                await Logout();
                return false;
            }

            _appInfo.Username = user.Username;

            List<MSAccount> toAdd = user.MSAccounts
                .Where(a => _appInfo.Accounts
                .FirstOrDefault(acc => acc.DbId == a.DbId) == null)?.ToList();
            List<MSAccount> toRemove = _appInfo.Accounts
                .Where(a => user.MSAccounts
                .FirstOrDefault(acc => acc.DbId == a.DbId) == null)?.ToList();

            if (toAdd != null)
            {
                foreach (MSAccount account in toAdd)
                {
                    _appInfo.Accounts.Add(account);
                }
            }
            if (toRemove != null)
            {
                foreach (MSAccount account in toRemove)
                {
                    _appInfo.Accounts.Remove(account);
                }
            }

            return true;
        }

        public async Task Logout()
        {
            if (_token != Guid.Empty)
            {
                await _connection.Logout(_token);
            }

            FileManager.SaveData(new AppData());
            Dispose();

            ProcessStartInfo psi = new ProcessStartInfo
            {
                Arguments = "/C choice /C Y /N /D Y /T 1 & START \"\" \"" + Environment.ProcessPath + "\"",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "cmd.exe"
            };
            Process.Start(psi);
            Environment.Exit(0);
        }

        public void AddMSAccount()
        {
            if (_msLoginWindow != null && _msLoginWindow.IsVisible)
            {
                return;
            }

            _msLoginWindow = new MSLoginWindow(this);
            _msLoginWindow.Owner = App.Current.MainWindow;
            _msLoginWindow.Show();
        }

        public Task<bool> InsertMSAccount(List<AccountCookie> cookies)
        {
            MSAccount acc = new MSAccount()
            {
                Cookies = cookies
            };

            return _connection.InsertMSAccount(_token, acc);
        }

        public async void Dispose()
        {
            await _connection?.DisconnectAsync(); // Closes connection gracefully with the server
            Utils.KillWebViewProcess(); // Clean any garbage process made by web view (thanks microsoft)
            Environment.Exit(0);
        }
    }
}
