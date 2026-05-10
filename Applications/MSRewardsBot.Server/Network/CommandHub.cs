using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Common.DataEntities.Interfaces;
using MSRewardsBot.Server.DataEntities.Attributes;

namespace MSRewardsBot.Server.Network
{
    /// <summary>
    /// https://www.linkedin.com/pulse/wpf-real-time-communication-made-easy-signalr-net-core-bandara-64htc/
    /// </summary>
    public class CommandHub : Hub, IBotAPI
    {
        private readonly CommandHubProxy _hubProxy;

        public CommandHub(CommandHubProxy proxy)
        {
            _hubProxy = proxy;
        }


        public Task<bool> LoginWithToken(Guid token)
        {
            return _hubProxy.LoginWithToken(token);
        }

        public Task<Guid> Login(User user)
        {
            return _hubProxy.Login(user);
        }

        public Task<Guid> Register(User user)
        {
            return _hubProxy.Register(user);
        }

        [LoggedOn]
        public Task<bool> Logout(Guid token)
        {
            return _hubProxy.Logout(token);
        }

        [LoggedOn]
        public Task<User> GetUserInfo(Guid token)
        {
            return _hubProxy.GetUserInfo(token);
        }

        [LoggedOn]
        public Task<bool> InsertMSAccount(Guid token, MSAccount account)
        {
            return _hubProxy.InsertMSAccount(token, account);
        }

        [LoggedOn]
        public Task<bool> DeleteMSAccount(Guid token, int msAccountId)
        {
            return _hubProxy.DeleteMSAccount(token, msAccountId);
        }

        [LoggedOn]
        public Task<bool> UpdateMSAccountCookies(Guid token, int msAccountId, List<AccountCookie> cookies)
        {
            return _hubProxy.UpdateMSAccountCookies(token, msAccountId, cookies);
        }

        public void SendClientVersion(string connectionId, Version version)
        {
            _hubProxy.SendClientVersion(connectionId, version);
        }
    }
}
