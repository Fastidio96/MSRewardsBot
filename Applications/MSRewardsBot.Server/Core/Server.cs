using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Common.Utilities;
using MSRewardsBot.Server.DataEntities;
using MSRewardsBot.Server.Network;

namespace MSRewardsBot.Server.Core
{
    public class Server : IDisposable
    {
        private readonly ILogger<Server> _logger;
        private readonly IConnectionManager _connectionManager;
        private readonly CommandHubProxy _commandHub;

        private Thread _mainThread;
        private bool _isDisposing = false;

        private TaskScheduler _taskScheduler;

        public Server(ILogger<Server> logger, IConnectionManager connectionManager, CommandHubProxy commandHubProxy)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _commandHub = commandHubProxy;
        }

        public void Start()
        {
            _mainThread = new Thread(Loop);
            _mainThread.Name = "Core loop";
            _mainThread.Start();

            _logger.Log(LogLevel.Information, "Main thread started.");
        }

        private void Loop()
        {
            _connectionManager.ClientConnected += ConnectionManager_ClientConnected;
            _connectionManager.ClientDisconnected += ConnectionManager_ClientDisconnected;

            using (_taskScheduler = new TaskScheduler())
            {
                while (!_isDisposing)
                {
                    foreach (ClientInfo client in _connectionManager.GetClients())
                    {
                        DateTime now = DateTime.Now;

                        if (DateTimeUtilities.HasElapsed(now, client.LastDashboardUpdate, new TimeSpan(0, 5, 0)))
                        {
                            //Send command dashboard update to the client
                            _commandHub.SetConnectionId(client.ConnectionId);
                            client.LastDashboardUpdate = now;
                        }


                    }

                    Thread.Sleep(1000);
                }
            }
        }

        private void ConnectionManager_ClientConnected(object? sender, ClientArgs e)
        {
            Job job = new Job(e.ConnectionId);
            _taskScheduler.Queue.Enqueue(job, job.Priority);
        }

        private void ConnectionManager_ClientDisconnected(object? sender, ClientArgs e)
        {
            //_taskScheduler.Queue.Enqueue(new Job<LoginRequest>(e.ConnectionId));
        }

        public void Dispose()
        {
            _isDisposing = true;

            _connectionManager.ClientConnected -= ConnectionManager_ClientConnected;
            _connectionManager.ClientDisconnected -= ConnectionManager_ClientDisconnected;
        }
    }
}
