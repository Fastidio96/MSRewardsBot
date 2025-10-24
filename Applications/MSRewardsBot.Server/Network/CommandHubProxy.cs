using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Common.DataEntities.Interfaces;
using MSRewardsBot.Server.Core;

namespace MSRewardsBot.Server.Network
{
    public class CommandHubProxy : IBotAPI
    {
        private readonly IHubContext<CommandHub> _commandHub;
        private readonly ILogger<CommandHubProxy> _logger;
        private readonly BusinessLayer _business;

        private string _connectionId;

        public CommandHubProxy(ILogger<CommandHubProxy> logger, IHubContext<CommandHub> commandHub, BusinessLayer bl)
        {
            _commandHub = commandHub;
            _logger = logger;
            _business = bl;
        }

        public void SetConnectionId(string connectionId)
        {
            _connectionId = connectionId;
        }


        public Task SendTestMessage(string m)
        {
            _logger.LogInformation("Sent command {CommandName} to {ConnectionId}", nameof(SendTestMessage), _connectionId);
            return _commandHub.Clients.Client(_connectionId).SendAsync(nameof(IBotAPI.SendTestMessage), m);
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
    }
}
