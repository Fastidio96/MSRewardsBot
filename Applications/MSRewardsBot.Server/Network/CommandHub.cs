using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Common.DataEntities.Interfaces;
using MSRewardsBot.Server.DataEntities;

namespace MSRewardsBot.Server.Network
{
    /// <summary>
    /// https://www.linkedin.com/pulse/wpf-real-time-communication-made-easy-signalr-net-core-bandara-64htc/
    /// </summary>
    public class CommandHub : Hub, IBotAPI
    {
        private string _connectionId => Context.ConnectionId;
        private readonly ILogger<CommandHub> _logger;
        private readonly IConnectionManager _connectionManager;

        public CommandHub(ILogger<CommandHub> logger, IConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            _connectionManager.AddConnection(new ClientInfo()
            {
                ConnectionId = _connectionId
            });

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
            _connectionManager.RemoveConnection(_connectionId);

            return base.OnDisconnectedAsync(exception);
        }

        #region Commands

        //public void ReceiveTest(string message)
        //{
        //    _logger.LogInformation($"S{message}");
        //}

        public Task SendTestMessage(string m)
        {
            Task.Delay(2500);
            _logger.LogInformation($"Sent command {nameof(SendTestMessage)} to {_connectionId}");
            return Clients.Client(_connectionId).SendAsync(nameof(IBotAPI.SendTestMessage), m);
        }

        #endregion
    }
}
