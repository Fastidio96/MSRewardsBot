using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Common.DataEntities.Stats;
using MSRewardsBot.Common.Utilities;
using MSRewardsBot.Server.Automation;
using MSRewardsBot.Server.DataEntities;
using MSRewardsBot.Server.DataEntities.Commands;
using MSRewardsBot.Server.Network;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace MSRewardsBot.Server.Core
{
    public class Server : IDisposable
    {
        private readonly ILogger<Server> _logger;
        private readonly IConnectionManager _connectionManager;
        private readonly CommandHubProxy _commandHubProxy;
        private readonly BrowserManager _browser;
        private readonly BusinessLayer _business;
        private readonly IServiceProvider _serviceProvider;

        private readonly IKeywordProvider _keywordProvider;
        private readonly KeywordStore _keywordStore;

        private Thread _mainThread;
        private Thread _clientsThread;
        private bool _isDisposing = false;

        private TaskScheduler _taskScheduler;

        public Dictionary<int, MSAccountServerData> CacheMSAccStats;

        public Server
        (
            ILogger<Server> logger,
            IConnectionManager connectionManager,
            CommandHubProxy commandHubProxy,
            BrowserManager browser,
            BusinessLayer bl,
            IServiceProvider service
        )
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _commandHubProxy = commandHubProxy;
            _browser = browser;
            _business = bl;
            _serviceProvider = service;

            CacheMSAccStats = new Dictionary<int, MSAccountServerData>();

            _keywordStore = new KeywordStore();
            _keywordProvider = new KeywordProvider(_keywordStore);
        }

        public void Start()
        {
            _browser.Init();

            _taskScheduler = new TaskScheduler(_browser, _business, _serviceProvider.GetRequiredService<ILogger<TaskScheduler>>());

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


        private async void CoreLoop()
        {
            _logger.LogInformation("Core loop thread started");

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

                    if (!CacheMSAccStats.TryGetValue(acc.DbId, out MSAccountServerData cache))
                    {
                        cache = new MSAccountServerData()
                        {
                            Account = acc,
                            Stats = acc.Stats
                        };

                        acc.Stats.UserId = acc.UserId;
                        acc.Stats.MSAccountId = acc.DbId;
                        acc.Stats.PropertyChanged += MsAccountStats_PropertyChanged;

                        if (!await _browser.CreateContext(cache))
                        {
                            _logger.LogError("Cannot create context for {Email} | {Username}!", acc.Email, acc.User.Username);
                            continue;
                        }

                        CacheMSAccStats.Add(acc.DbId, cache);
                    }

                    DateTime now = DateTime.Now;
                    if (DateTimeUtilities.HasElapsed(now, cache.Stats.LastDashboardCheck, Settings.DashboardCheck))
                    {
                        cache.Stats.LastDashboardCheck = now; //Fix for not queueing the same job while we wait for the job's completion
                        if (DateTimeUtilities.HasElapsed(now, cache.Stats.LastDashboardUpdate, Settings.DashboardUpdate))
                        {
                            AddJobDashboardUpdate(cache);
                        }
                    }

                    if (DateTimeUtilities.HasElapsed(now, cache.Stats.LastSearchesCheck, Settings.SearchesCheck))
                    {
                        cache.Stats.LastSearchesCheck = now;
                        if (cache.Stats.PCSearchesToDo > 0)
                        {
                            DateTime start = now;

                            for (int i = 0; i < cache.Stats.PCSearchesToDo; i++)
                            {
                                if (i != 0)
                                {
                                    start = start.AddSeconds(Random.Shared.Next(60, 180));
                                }

                                string keyword = await _keywordProvider.GetKeyword();

                                Job job = new Job(
                                    new PCSearchCommand()
                                    {
                                        Data = cache,
                                        Keyword = keyword,
                                        OnSuccess = delegate ()
                                        {
                                            _logger.LogDebug("Job {name} succeded (with keyword {keyword}) for {user}",
                                                nameof(PCSearchCommand), keyword, acc.Email);

                                            if(i == cache.Stats.PCSearchesToDo - 1)
                                            {
                                                AddJobDashboardUpdate(cache);
                                            }

                                            cache.Stats.PCSearchCompleted();
                                        },
                                        OnFail = delegate ()
                                        {
                                            _logger.LogWarning("Job {name} failed", nameof(PCSearchCommand));

                                            cache.Stats.PCSearchFailed();
                                            AddJobDashboardUpdate(cache);
                                        }
                                    });

                                _taskScheduler.AddJob(start, job);

                                cache.Stats.LastSearchesCheck = start;
                            }
                        }
                    }
                }

                Thread.Sleep(1000);
            }

            foreach (KeyValuePair<int, MSAccountServerData> acc in CacheMSAccStats)
            {
                acc.Value.Account.Stats.PropertyChanged -= MsAccountStats_PropertyChanged;
            }

            CacheMSAccStats.Clear();
            CacheMSAccStats = null;
        }

        private void AddJobDashboardUpdate(MSAccountServerData data)
        {
            Job job = new Job(
                                new DashboardUpdateCommand()
                                {
                                    Data = data,
                                    OnSuccess = delegate ()
                                    {
                                        _logger.LogDebug("Job {name} succeded for {user}",
                                                nameof(PCSearchCommand), data.Account.Email);

                                        data.Stats.LastSearchesCheck = DateTime.MinValue; // Triggers search check
                                    },
                                    OnFail = delegate ()
                                    {
                                        _logger.LogWarning("Job {name} failed", nameof(DashboardUpdateCommand));

                                        data.Stats.LastDashboardCheck = DateTime.MinValue; // Retry again after failure
                                    }
                                });

            _taskScheduler.AddJob(DateTime.Now, job);
        }

        private async void MsAccountStats_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is MSAccountStats stats)
            {
                ClientInfo info = _connectionManager.GetConnection(stats.UserId);
                if (info == null)
                {
                    return;
                }

                await _commandHubProxy.SendUpdateMSAccountStats(info.ConnectionId, stats, e.PropertyName);
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
