using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Common.DataEntities.Interfaces;
using MSRewardsBot.Common.DataEntities.Stats;
using MSRewardsBot.Server.Core;
using MSRewardsBot.Server.DataEntities;

namespace MSRewardsBot.Server.Network
{
    public class CommandHubProxy : IBotAPI
    {
        private readonly BusinessLayer _business;
        private readonly IHubContext<CommandHub> _hubContext;

        public CommandHubProxy(BusinessLayer bl, IHubContext<CommandHub> hubContext)
        {
            _business = bl;
            _hubContext = hubContext;
        }

        public Task<bool> LoginWithToken(Guid token)
        {
            return Task.FromResult(_business.Login(token));
        }
        public Task<Guid> Login(User user)
        {
            return Task.FromResult(_business.Login(user));
        }

        public Task<Guid> Register(User user)
        {
            return Task.FromResult(_business.Register(user));
        }

        public Task<User> GetUserInfo(Guid token)
        {
            return Task.FromResult(_business.GetUserInfo(token));
        }

        public Task<bool> InsertMSAccount(Guid token, MSAccount account)
        {
            return Task.FromResult(_business.InsertMSAccount(token, account));
        }

        internal Task SendUpdateMSAccountStats(string connectionId, MSAccountStats accountStat, string propertyName)
        {
            return _hubContext.Clients.Client(connectionId).SendAsync(nameof(SendUpdateMSAccountStats), accountStat, propertyName);
        }

        public Task<bool> Logout(Guid token)
        {
            return Task.FromResult(_business.Logout(token));
        }
    }
}
