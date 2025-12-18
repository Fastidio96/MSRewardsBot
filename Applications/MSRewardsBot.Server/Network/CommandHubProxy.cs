using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Common.DataEntities.Interfaces;
using MSRewardsBot.Common.DataEntities.Stats;
using MSRewardsBot.Server.Core.Factories;
using MSRewardsBot.Server.DataEntities;

namespace MSRewardsBot.Server.Network
{
    public class CommandHubProxy : IBotAPI
    {
        private readonly BusinessFactory _businessFactory;
        private readonly IHubContext<CommandHub> _hubContext;
        private readonly IConnectionManager _connectionManager;

        public CommandHubProxy(BusinessFactory businessFactory, IHubContext<CommandHub> hubContext, IConnectionManager connectionManager)
        {
            _businessFactory = businessFactory;
            _hubContext = hubContext;
            _connectionManager = connectionManager;
        }

        public Task<bool> LoginWithToken(Guid token)
        {
            using (ScopedBusiness scope = _businessFactory.Create())
            {
                return Task.FromResult(scope.Business.Login(token));
            }
        }
        public Task<Guid> Login(User user)
        {
            using (ScopedBusiness scope = _businessFactory.Create())
            {
                return Task.FromResult(scope.Business.Login(user));
            }
        }

        public Task<Guid> Register(User user)
        {
            using (ScopedBusiness scope = _businessFactory.Create())
            {
                return Task.FromResult(scope.Business.Register(user));
            }
        }

        public Task<User> GetUserInfo(Guid token)
        {
            using (ScopedBusiness scope = _businessFactory.Create())
            {
                return Task.FromResult(scope.Business.GetUserInfo(token));
            }
        }

        public Task<bool> InsertMSAccount(Guid token, MSAccount account)
        {
            using (ScopedBusiness scope = _businessFactory.Create())
            {
                return Task.FromResult(scope.Business.InsertMSAccount(token, account));
            }
        }

        public Task<bool> Logout(Guid token)
        {
            using (ScopedBusiness scope = _businessFactory.Create())
            {
                return Task.FromResult(scope.Business.Logout(token));
            }
        }

        internal Task SendUpdateMSAccountStats(string connectionId, MSAccountStats accountStat, string propertyName)
        {
            return _hubContext.Clients.Client(connectionId).SendAsync(nameof(SendUpdateMSAccountStats), accountStat, propertyName);
        }

        internal Task RequestClientVersion(string connectionId)
        {
            return _hubContext.Clients.Client(connectionId).SendAsync(nameof(RequestClientVersion), connectionId);
        }

        public Task SendClientVersion(string connectionId, Version version)
        {
            ClientInfo clientInfo = _connectionManager.GetConnection(connectionId);
            clientInfo.Version = version;
            clientInfo.LastVersionRequest = DateTime.Now;

            _connectionManager.UpdateConnection(connectionId, clientInfo);

            return Task.FromResult(clientInfo);
        }

        public Task SendClientUpdateFile(string connectionId, byte[] file)
        {
            return _hubContext.Clients.Client(connectionId).SendAsync(nameof(SendClientUpdateFile), file);
        }
    }
}
