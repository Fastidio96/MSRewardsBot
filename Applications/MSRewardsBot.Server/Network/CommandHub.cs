using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Common.DataEntities.Interfaces;
using MSRewardsBot.Server.Core;
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
        private readonly BusinessLayer _business;

        public CommandHub(ILogger<CommandHub> logger, IConnectionManager connectionManager, CommandHubProxy proxy, BusinessLayer bl)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _hubProxy = proxy;
            _business = bl;
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
            _logger.Log(LogLevel.Information, "Client disconnected: {ConnectionId}", Context.ConnectionId);
            _connectionManager.RemoveConnection(_connectionId);

            return base.OnDisconnectedAsync(exception);
        }

        public async Task<Guid> Login(User user)
        {
            _logger.LogInformation("Received command {CommandName} from {ConnectionId}", nameof(Login), _connectionId);

            Guid token = await _hubProxy.Login(user);
            UpdateConnectionInfo(token);

            return token;
        }

        public async Task<Guid> Register(User user)
        {
            _logger.LogInformation("Received command {CommandName} from {ConnectionId}", nameof(Register), _connectionId);

            Guid token = await _hubProxy.Register(user);
            UpdateConnectionInfo(token);

            return token;
        }

        public async Task<bool> Logout(Guid token)
        {
            _logger.LogInformation("Sent command {CommandName} to {ConnectionId}", nameof(Logout), _connectionId);

            RemoveConnectionByToken(token);
            bool res = await _hubProxy.Logout(token);

            return res;
        }

        public Task<User> GetUserInfo(Guid token)
        {
            _logger.LogInformation("Sent command {CommandName} to {ConnectionId}", nameof(GetUserInfo), _connectionId);

            UpdateConnectionInfo(token);
            return _hubProxy.GetUserInfo(token);
        }

        public Task<bool> InsertMSAccount(Guid token, MSAccount account)
        {
            _logger.LogInformation("Received command {CommandName} from {ConnectionId}", nameof(InsertMSAccount), _connectionId);
            return _hubProxy.InsertMSAccount(token, account);
        }



        private void UpdateConnectionInfo(Guid token)
        {
            if (token != Guid.Empty)
            {
                ClientInfo info = _connectionManager.GetConnection(_connectionId);

                User user = _business.GetUserInfo(token);
                if (user == null)
                {
                    return;
                }

                info.User = user;

                _connectionManager.UpdateConnection(_connectionId, info);
            }
        }

        private void RemoveConnectionByToken(Guid token)
        {
            if (token != Guid.Empty)
            {
                ClientInfo info = _connectionManager.GetConnection(_connectionId);

                User user = _business.GetUserInfo(token);
                if (user == null)
                {
                    return;
                }

                info.User = null;

                _connectionManager.UpdateConnection(_connectionId, info);
            }
        }
    }
}
