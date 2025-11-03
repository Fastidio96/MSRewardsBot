using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using MSRewardsBot.Client.DataEntities;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Common.DataEntities.Interfaces;

namespace MSRewardsBot.Client.Services
{
    public class ConnectionService
    {
        private HubConnection _connection { get; set; }
        private AppInfo _appInfo;

        public ConnectionService(AppInfo info)
        {
            _appInfo = info;
        }

        public async Task ConnectAsync()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:10500/cmdhub")
                .WithAutomaticReconnect()
                .Build();

            _connection.Closed += Connection_Closed;

            await _connection.StartAsync();
            _appInfo.ConnectedToServer = true;

            _connection.On<Guid>(nameof(IBotAPI.GetUserInfo), GetUserInfo);
            _connection.On<bool>(nameof(IBotAPI.Logout), delegate ()
            {
                _appInfo.IsUserLogged = false;
                return true;
            });
            _connection.On("SendMSAccountsInfo", delegate (List<MSAccount> acc)
            {
                _appInfo.Accounts.Clear();
                foreach (MSAccount account in acc)
                {
                    _appInfo.Accounts.Add(account);
                }
            });
        }

        private Task Connection_Closed(Exception? arg)
        {
            _appInfo.ConnectedToServer = false;

            return Task.CompletedTask;
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
    }
}
