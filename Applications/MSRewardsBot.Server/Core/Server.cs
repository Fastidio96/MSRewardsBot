using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Common.Utilities;
using MSRewardsBot.Server.Automation;
using MSRewardsBot.Server.DataEntities;
using MSRewardsBot.Server.DataEntities.Commands;
using MSRewardsBot.Server.Network;

namespace MSRewardsBot.Server.Core
{
    public class Server : IDisposable
    {
        private readonly ILogger<Server> _logger;
        private readonly IConnectionManager _connectionManager;
        private readonly CommandHubProxy _commandHub;
        private readonly BrowserManager _browser;
        private readonly BusinessLayer _business;

        private Thread _mainThread;
        private bool _isDisposing = false;

        private TaskScheduler _taskScheduler;

        public Server
        (
            ILogger<Server> logger,
            IConnectionManager connectionManager,
            CommandHubProxy commandHubProxy,
            BrowserManager browser,
            BusinessLayer bl
        )
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _commandHub = commandHubProxy;
            _browser = browser;
            _business = bl;
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
            _browser.Start();

            using (_taskScheduler = new TaskScheduler(_browser))
            {
                while (!_isDisposing)
                {
                    foreach (ClientInfo client in _connectionManager.GetClients())
                    {
                        DateTime now = DateTime.Now;

                        if (DateTimeUtilities.HasElapsed(now, client.LastServerCheck, new TimeSpan(0, 5, 0)))
                        {
                            if (client.User == null)
                            {
                                Thread.Sleep(1000);
                                continue;
                            }

                            client.LastServerCheck = now;

                            foreach (MSAccount acc in client.User.MSAccounts)
                            {
                                if (DateTimeUtilities.HasElapsed(now, acc.LastDashboardUpdate, new TimeSpan(12, 0, 0)))
                                {
                                    Job job = new Job(client.ConnectionId,
                                        new DashboardUpdateCommand()
                                        {
                                            Account = acc,
                                            OnSuccess = delegate ()
                                            {
                                                acc.LastDashboardUpdate = now;
                                            }
                                        });

                                    _taskScheduler.Queue.Enqueue(job, JobPriority.Medium);
                                }
                            }
                        }
                    }

                    Thread.Sleep(1000);
                }
            }
        }

        public void Dispose()
        {
            _isDisposing = true;

            if (_mainThread != null)
            {
                if (_mainThread.IsAlive)
                {
                    _mainThread.Join(5000);
                }

                _mainThread = null;
            }
        }
    }
}
