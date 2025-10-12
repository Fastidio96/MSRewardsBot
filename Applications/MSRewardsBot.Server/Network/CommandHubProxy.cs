using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Common.DataEntities.Interfaces;

namespace MSRewardsBot.Server.Network
{
    public class CommandHubProxy : IBotAPI
    {
        private readonly IHubContext<CommandHub> _commandHub;
        private readonly ILogger<CommandHubProxy> _logger;
        private string _connectionId;

        public CommandHubProxy(ILogger<CommandHubProxy> logger, IHubContext<CommandHub> commandHub)
        {
            _commandHub = commandHub;
            _logger = logger;
        }

        public void SetConnectionId(string connectionId)
        {
            _connectionId = connectionId;
        }


        public Task SendTestMessage(string m)
        {
            _logger.LogInformation($"Sent command {nameof(SendTestMessage)} to {_connectionId}");
            return _commandHub.Clients.Client(_connectionId).SendAsync(nameof(IBotAPI.SendTestMessage), m);
        }
    }
}
