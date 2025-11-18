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

        private readonly IKeywordProvider _keywordProvider;
        private readonly KeywordStore _keywordStore;

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

            _keywordProvider = new KeywordProvider();
            _keywordStore = new KeywordStore();
        }

        public void Start()
        {
            _browser.Init();
            _taskScheduler = new TaskScheduler(_browser, _business);

            _clientsThread = new Thread(ClientLoop);
            _clientsThread.Name = nameof(ClientLoop);

            _mainThread = new Thread(CoreLoop);
            _mainThread.Name = nameof(CoreLoop);

            _clientsThread.Start();
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


        private async void CoreLoop()
        {
            _logger.LogInformation("Core loop thread started");

            Dictionary<int, MSAccountServerData> cacheMSAccStats = new Dictionary<int, MSAccountServerData>();

            while (!_isDisposing)
            {
                if (DateTimeUtilities.HasElapsed(DateTime.Now, _keywordStore.LastRefresh, Settings.KeywordsListRefresh))
                {
                    await _keywordStore.RefreshList();
                }

                List<MSAccount> accounts = _business.GetAllMSAccounts();
                foreach (MSAccount acc in accounts)
                {
                    if (acc.Cookies.Count == 0)
                    {
                        _logger.LogWarning("No cookies found for account {Email} | {Username}. Skipping..", acc.Email, acc.User.Username);
                        continue;
                    }

                    if (!cacheMSAccStats.TryGetValue(acc.DbId, out MSAccountServerData cache))
                    {
                        cache = new MSAccountServerData()
                        {
                            Account = acc,
                            Stats = acc.Stats
                        };

                        if (!await _browser.CreateContext(cache))
                        {
                            _logger.LogError("Cannot create context for {Email} | {Username}!", acc.Email, acc.User.Username);
                            continue;
                        }

                        cacheMSAccStats.Add(acc.DbId, cache);
                    }

                    DateTime now = DateTime.Now;
                    if (DateTimeUtilities.HasElapsed(now, cache.Stats.LastDashboardCheck, Settings.DashboardCheck))
                    {
                        cache.Stats.LastDashboardCheck = now; //Fix for not queueing the same job while we wait for the job's completion
                        if (DateTimeUtilities.HasElapsed(now, cache.Stats.LastDashboardUpdate, Settings.DashboardUpdate))
                        {
                            Job job = new Job(
                                new DashboardUpdateCommand()
                                {
                                    Data = cache,
                                    OnSuccess = delegate ()
                                    {
                                        cache.Stats.LastSearchesCheck = DateTime.MinValue; // Triggers search check
                                    },
                                    OnFail = delegate ()
                                    {
                                        _logger.LogWarning("Job {name} failed", nameof(DashboardUpdateCommand));
                                        cache.Stats.LastDashboardCheck = DateTime.MinValue; // Retry again after failure
                                    }
                                });

                            _taskScheduler.AddJob(now, job);
                            _logger.LogInformation("Added job {name} on {time}", nameof(DashboardUpdateCommand), now);
                        }
                    }

                    if (DateTimeUtilities.HasElapsed(now, cache.Stats.LastSearchesCheck, Settings.SearchesCheck))
                    {
                        cache.Stats.LastSearchesCheck = now;
                        if (cache.Stats.PCSearchesToDo > 0)
                        {
                            DateTime start = now;
                            Random rnd = new Random();

                            for (int i = 0; i < cache.Stats.PCSearchesToDo; i++)
                            {
                                if (i != 0)
                                {
                                    start = start.AddSeconds(rnd.Next(60, 180));
                                }

                                string keyword = _keywordProvider.GetKeyword();

                                Job job = new Job(
                                    new PCSearchCommand()
                                    {
                                        Data = cache,
                                        Keyword = keyword,
                                        OnSuccess = delegate ()
                                        {
                                            _logger.LogDebug("Job {name} succeded (with keyword {keyword}) for {user}",
                                                nameof(PCSearchCommand), keyword, acc.Email);

                                            cache.Stats.PCSearchCompleted();
                                        },
                                        OnFail = delegate ()
                                        {
                                            _logger.LogWarning("Job {name} failed", nameof(PCSearchCommand));
                                            cache.Stats.LastDashboardCheck = DateTime.MinValue; // Reload stats
                                        }
                                    });

                                _taskScheduler.AddJob(start, job);
                                _logger.LogInformation("Added job {name} (with keyword {keyword}) on {time} for {user}",
                                    nameof(PCSearchCommand), keyword, start, acc.Email);

                                cache.Stats.LastSearchesCheck = start;
                            }
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

            if (_browser != null)
            {
                _browser.Dispose();
            }
        }
    }
}
