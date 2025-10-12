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

        public CommandHubProxy(ILogger<CommandHubProxy> logger, IHubContext<CommandHub> commandHub)
        {
            _commandHub = commandHub;
            _logger = logger;
        }

        public Task SendTestMessage(string m)
        {
            Task.Delay(2500);
            _logger.LogInformation($"Sent command {nameof(SendTestMessage)} to {_connectionId}");
            return _commandHub.Clients.Client(_connectionId).SendAsync(nameof(IBotAPI.SendTestMessage), m);
        }
    }
}
