using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Common.DataEntities.Stats;
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
        private Thread _clientsThread;
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
            _browser.Start();
            _taskScheduler = new TaskScheduler(_browser, _business);

            _clientsThread = new Thread(ClientLoop);
            _clientsThread.Name = nameof(ClientLoop);

            _mainThread = new Thread(CoreLoop);
            _mainThread.Name = nameof(CoreLoop);

            //_clientsThread.Start();
            _mainThread.Start();
        }

        private void ClientLoop()
        {
            _logger.LogInformation("Clients loop thread started");

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
                    }
                }

                Thread.Sleep(1000);
            }
        }


        private void CoreLoop()
        {
            _logger.LogInformation("Core loop thread started");

            Dictionary<int, MSAccountStats> cacheMSAccStats = new Dictionary<int, MSAccountStats>();

            while (!_isDisposing)
            {
                List<MSAccount> accounts = _business.GetAllMSAccounts();
                foreach (MSAccount acc in accounts)
                {
                    if(acc.Cookies.Count == 0)
                    {
                        _logger.LogWarning("No cookies found for account {Email} | {Username}. Skipping..", acc.Email, acc.User.Username);
                        continue;
                    }

                    if(!cacheMSAccStats.TryGetValue(acc.DbId, out MSAccountStats cache))
                    {
                        cache = acc.Stats;
                        cacheMSAccStats.Add(acc.DbId, cache);
                    }

                    DateTime now = DateTime.Now;
                    if (DateTimeUtilities.HasElapsed(now, cache.LastServerCheck, new TimeSpan(1, 0, 0)))
                    {
                        cache.LastServerCheck = now; //Fix for not queueing the same job while we wait for the job's completion
                        if (DateTimeUtilities.HasElapsed(now, cache.LastDashboardUpdate, new TimeSpan(0, 5, 0)))
                        {
                            Job job = new Job(
                                new DashboardUpdateCommand()
                                {
                                    Account = acc,
                                    OnFail = delegate ()
                                    {
                                        cache.LastServerCheck = DateTime.MinValue; // Retry again after failure
                                    }
                                });

                            _taskScheduler.AddJob(now, job);
                        }
                    }

                    
                }

                Thread.Sleep(1000);
            }

            cacheMSAccStats.Clear();
            cacheMSAccStats = null;
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

            if (_taskScheduler != null)
            {
                _taskScheduler.Dispose();
                _taskScheduler = null;
            }

            if(_browser != null)
            {
                _browser.Dispose();
            }
        }
    }
}
