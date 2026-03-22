using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Common.DataEntities.Interfaces;
using MSRewardsBot.Server.Core.Factories;

namespace MSRewardsBot.Server.Network
{
    public class CommandHubProxy : IBotAPI
    {
        private readonly ILogger<CommandHubProxy> _logger;
        private readonly BusinessFactory _businessFactory;
        private readonly IHubContext<CommandHub> _hubContext;
        private readonly IConnectionManager _connectionManager;

        public CommandHubProxy(ILogger<CommandHubProxy> logger, BusinessFactory businessFactory, IHubContext<CommandHub> hubContext, IConnectionManager connectionManager)
        {
            _logger = logger;
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

        internal async Task SendUpdateMSAccountStats(string connectionId, MSAccountStats accountStat, string propertyName)
        {
            await _hubContext.Clients.Client(connectionId).SendAsync(nameof(SendUpdateMSAccountStats), accountStat, propertyName);
        }

        internal async Task RequestClientVersion(string connectionId)
        {
            _logger.LogDebug("Requesting client version from {id}..", connectionId);
            await _hubContext.Clients.Client(connectionId).SendAsync(nameof(RequestClientVersion), connectionId);
        }

        public void SendClientVersion(string connectionId, Version version)
        {
            _logger.LogDebug("Received new client version {ver} from {id}..", version, connectionId);
            _connectionManager.UpdateClientVersion(connectionId, version);
        }

        public Task SendClientUpdateFile(string connectionId, byte[] file)
        {
            _logger.LogDebug("Sending update package to client {id}..", connectionId);
            return _hubContext.Clients.Client(connectionId).SendAsync(nameof(SendClientUpdateFile), file);
        }
    }
}
