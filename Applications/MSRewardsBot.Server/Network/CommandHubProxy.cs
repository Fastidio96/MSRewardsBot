using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Common.DataEntities.Interfaces;
using MSRewardsBot.Server.Core;
using MSRewardsBot.Server.DataEntities;

namespace MSRewardsBot.Server.Network
{
    public class CommandHubProxy : IBotAPI
    {
        private readonly ILogger<CommandHubProxy> _logger;
        private readonly BusinessLayer _business;
        private readonly IConnectionManager _connectionManager;

        private string _connectionId;

        public CommandHubProxy(ILogger<CommandHubProxy> logger, BusinessLayer bl, IConnectionManager connection)
        {
            _logger = logger;
            _business = bl;
            _connectionManager = connection;
        }

        public void SetConnectionId(string connectionId)
        {
            _connectionId = connectionId;
        }


        public Task<Guid> Login(User user)
        {
            Guid token = _business.Login(user);
            if (token != Guid.Empty)
            {
                ClientInfo info = _connectionManager.GetConnection(_connectionId);
                info.Username = user.Username;

                _connectionManager.UpdateConnection(_connectionId, info);
            }

            return Task.FromResult(token);
        }

        public Task<Guid> Register(User user)
        {
            Guid token = _business.Register(user);
            if (token != Guid.Empty)
            {
                ClientInfo info = _connectionManager.GetConnection(_connectionId);
                info.Username = user.Username;

                _connectionManager.UpdateConnection(_connectionId, info);
            }

            return Task.FromResult(token);
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
