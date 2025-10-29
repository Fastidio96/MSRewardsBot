using System;
using System.Threading.Tasks;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Common.DataEntities.Interfaces;
using MSRewardsBot.Server.Core;

namespace MSRewardsBot.Server.Network
{
    public class CommandHubProxy : IBotAPI
    {
        private readonly BusinessLayer _business;

        public CommandHubProxy(BusinessLayer bl)
        {
            _business = bl;
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
    }
}
