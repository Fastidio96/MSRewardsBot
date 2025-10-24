using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Common.DataEntities.Accounting;
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
        private readonly CommandHubProxy _hubProxy;

        public CommandHub(ILogger<CommandHub> logger, IConnectionManager connectionManager, CommandHubProxy proxy)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _hubProxy = proxy;
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

        public Task SendTestMessage(string m)
        {
            _logger.Log(LogLevel.Information, m);
            _hubProxy.SetConnectionId(_connectionId);
            return _hubProxy.SendTestMessage(m);
        }

        public Task<Guid> Login(User user)
        {
            return _hubProxy.Login(user);
        }

        public Task<Guid> Register(User user)
        {
            return _hubProxy.Register(user);
        }

        public Task<User> GetUserInfo(Guid token)
        {
            return _hubProxy.GetUserInfo(token);
        }
    }
}
