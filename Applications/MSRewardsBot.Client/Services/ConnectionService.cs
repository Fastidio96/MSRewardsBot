using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using MSRewardsBot.Client.DataEntities;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Common.DataEntities.Interfaces;
using MSRewardsBot.Common.DataEntities.Stats;
using MSRewardsBot.Common.Utilities;

namespace MSRewardsBot.Client.Services
{
    public class ConnectionService
    {
        private HubConnection _connection { get; set; }
        private readonly AppInfo _appInfo;
        private readonly AppData _appData;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private bool _isDisposing = false;

        public ConnectionService(AppInfo info, AppData data)
        {
            _appInfo = info;
            _appData = data;
        }

        public async Task ConnectAsync()
        {
            await Task.Run(async () =>
            {
                if (_connection != null)
                {
                    if (_connection.State != HubConnectionState.Disconnected)
                    {
                        return;
                    }
                }

                _connection = new HubConnectionBuilder()
                    .WithUrl(NetworkUtilities.GetConnectionString(
                        _appData.IsHttpsEnabled, _appData.ServerHost, _appData.ServerPort, true
                    ))
                    .WithAutomaticReconnect()
                    .Build();

                _connection.Closed += Connection_Closed;
                _isDisposing = false;

                await TryConnect();
                _appInfo.ConnectedToServer = true;

                _disposables.Add(_connection.On<Guid>(nameof(IBotAPI.GetUserInfo), GetUserInfo));
                _disposables.Add(_connection.On<bool>(nameof(IBotAPI.Logout), delegate ()
                {
                    _appInfo.IsUserLogged = false;
                    return true;
                }));
                _disposables.Add(_connection.On("SendUpdateMSAccountStats", delegate (MSAccountStats changedAcc, string propertyName)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        _appInfo.Accounts.FirstOrDefault(c => c.DbId == changedAcc.MSAccountId)
                            ?.Stats.ChangeProperty(changedAcc, propertyName);
                    });
                }));
                _disposables.Add(_connection.On("RequestClientVersion", async delegate (string clientId)
                {
                    await SendClientVersion(clientId, _appInfo.Version);
                }));
                _disposables.Add(_connection.On<byte[]>("SendClientUpdateFile", SaveUpdateFile));
            });
        }

        private async Task TryConnect()
        {
            bool exit = false;
            while (_connection != null && (!exit || _isDisposing))
            {
                _appInfo.ConnectionState = _connection.State;

                try
                {
                    await _connection.StartAsync();
                    exit = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    await Task.Delay(2500);
                }

                _appInfo.ConnectionState = _connection.State;
            }
        }

        public async Task DisconnectAsync()
        {
            _isDisposing = true;

            if (_connection == null)
            {
                return;
            }

            if (_connection.State == HubConnectionState.Connected)
            {
                await _connection.StopAsync();
            }

            foreach (IDisposable d in _disposables)
            {
                d.Dispose();
            }

            await _connection.DisposeAsync();

            _appInfo.ConnectionState = _connection.State;

            _connection.Closed -= Connection_Closed;
            _connection = null;
        }

        private async Task Connection_Closed(Exception? arg)
        {
            _appInfo.ConnectedToServer = false;

            await TryConnect();
        }

        public Task<bool> LoginWithToken(Guid token)
        {
            return _connection.InvokeAsync<bool>(nameof(IBotAPI.LoginWithToken), token);
        }

        public Task<Guid> Login(User user)
        {
            return _connection.InvokeAsync<Guid>(nameof(IBotAPI.Login), user);
        }

        public Task<Guid> Register(User user)
        {
            return _connection.InvokeAsync<Guid>(nameof(IBotAPI.Register), user);
        }

        public Task<User> GetUserInfo(Guid token)
        {
            return _connection.InvokeAsync<User>(nameof(IBotAPI.GetUserInfo), token);
        }

        public Task<bool> InsertMSAccount(Guid token, MSAccount account)
        {
            return _connection.InvokeAsync<bool>(nameof(IBotAPI.InsertMSAccount), token, account);
        }

        public Task<bool> Logout(Guid token)
        {
            return _connection.InvokeAsync<bool>(nameof(IBotAPI.Logout), token);
        }

        public Task SendClientVersion(string clientId, Version version)
        {
            return _connection.InvokeAsync(nameof(SendClientVersion), clientId, version);
        }

        public void SaveUpdateFile(byte[] file)
        {
            try
            {
                ZipArchiveEntry entry;
                using (Stream sr = new MemoryStream(file))
                using (ZipArchive zip = new ZipArchive(sr, ZipArchiveMode.Create))
                {
                    entry = zip.CreateEntry("update");
                    entry.ExtractToFile(Path.Combine(FileManager.GetFolderApp, "update.zip"), true);
                }

                FileManager.ApplyUpdate();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}

