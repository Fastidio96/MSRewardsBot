using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using MSRewardsBot.Client.DataEntities;
using MSRewardsBot.Common;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Common.DataEntities.Interfaces;
using MSRewardsBot.Common.DataEntities.Stats;

namespace MSRewardsBot.Client.Services
{
    public class ConnectionService
    {
        private HubConnection _connection { get; set; }
        private AppInfo _appInfo;

        private List<IDisposable> _disposables = new List<IDisposable>();

        public ConnectionService(AppInfo info)
        {
            _appInfo = info;
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
                    .WithUrl(Env.GetClientConnection())
                    .WithAutomaticReconnect()
                    .Build();

                _connection.Closed += Connection_Closed;

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
            });
        }

        private async Task TryConnect()
        {
            bool exit = false;
            while (_connection != null && !exit)
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
            if (_connection == null || _connection.State != HubConnectionState.Connected)
            {
                return;
            }

            await _connection.StopAsync();

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
    }
}
