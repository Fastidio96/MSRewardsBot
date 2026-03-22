using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        private AppData _appData;
        private AppInfo _appInfo;

        private MainWindow _mainWindow;
        private SplashScreenWindow _splashScreenWindow;
        private MSLoginWindow _msLoginWindow;
        private UserLoginWindow _userLoginWindow;
        private SettingsWindow _settingsWindow;

        public ViewModel()
        {
        }

        public async void Init()
        {
            _appInfo = new AppInfo();

            if (Directory.Exists(FileManager.TempFolderUpdaterPath)) // Cleanup any update temp folder
            {
                Directory.Delete(FileManager.TempFolderUpdaterPath, true);
            }

            bool validData = FileManager.LoadData(out _appData) && IsValidConnectionSettings(_appData);
            if (!validData)
            {
                _appInfo.IsUserLogged = false;
                EditSettings();
            }
            else
            {
                _splashScreenWindow = new SplashScreenWindow(this, _appInfo);
                _splashScreenWindow.Show();

                _connection = new ConnectionService(_appInfo, _appData);
                await _connection.ConnectAsync();

                _appInfo.IsUserLogged = _token != Guid.Empty &&
                    await _connection.LoginWithToken(_token);
            }

            _mainWindow = new MainWindow(this, _splashScreenWindow, _appInfo);
            App.Current.MainWindow = _mainWindow;

            if (validData)
            {
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

        public async Task Logout(bool deleteData = true)
        {
            if (_token != Guid.Empty)
            {
                await _connection.Logout(_token);
            }

            if (deleteData)
            {
                FileManager.SaveData(new AppData());
            }

            RestartApp();
        }

        public void RestartApp()
        {
            Dispose();

            Process.Start(Environment.ProcessPath);
            Environment.Exit(0);
        }

        public void EditSettings()
        {
            if (_settingsWindow != null && _settingsWindow.IsVisible)
            {
                return;
            }

            _settingsWindow = new SettingsWindow(this, _appData);
            _settingsWindow.Show();
        }

        public bool IsValidConnectionSettings(AppData data)
        {
            return IsValidConnectionSettings(data.ServerHost, data.ServerPort);
        }

        public bool IsValidConnectionSettings(string host, string port)
        {
            if (string.IsNullOrEmpty(host) || Uri.CheckHostName(host) == UriHostNameType.Unknown)
            {
                return false;
            }

            if (port.Length != 5 || !int.TryParse(port, out _))
            {
                return false;
            }

            return true;
        }

        public async Task<bool> SaveSettings()
        {
            if (!FileManager.SaveData(_appData))
            {
                return false;
            }

            _appData.AuthToken = null;

            await Logout(false);
            return true;
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

        public void ApplyUpdate()
        {
            PrepareTempFolder();

            Process.Start(new ProcessStartInfo()
            {
                FileName = Path.Combine(FileManager.TempFolderUpdaterPath, FileManager.UPDATER_NAME),
                Arguments = FileManager.AppFolderPath
            });

            Environment.Exit(0);
        }

        private void PrepareTempFolder()
        {
            if (Directory.Exists(FileManager.TempFolderUpdaterPath))
            {
                Directory.Delete(FileManager.TempFolderUpdaterPath, true);
            }

            Directory.CreateDirectory(FileManager.TempFolderUpdaterPath);

            DirectoryInfo dir = new DirectoryInfo(FileManager.LocalFolderUpdaterPath);
            foreach (FileInfo file in dir.GetFiles()) // Copy all updater file into temp
            {
                File.Copy(file.FullName, Path.Combine(FileManager.TempFolderUpdaterPath, file.Name), true);
            }

            FileInfo updater = new FileInfo(FileManager.LocalUpdatePackagePath);
            File.Copy(FileManager.LocalUpdatePackagePath, Path.Combine(FileManager.TempFolderUpdaterPath, updater.Name), true); // Copy zip package into temp
            File.Delete(FileManager.LocalUpdatePackagePath);
        }

        public async void Dispose()
        {
            if (_connection != null)
            {
                await _connection.DisconnectAsync(); // Closes connection gracefully with the server
            }

            Utils.KillWebViewProcess(); // Clean any garbage process made by web view (thanks microsoft)
        }
    }
}
