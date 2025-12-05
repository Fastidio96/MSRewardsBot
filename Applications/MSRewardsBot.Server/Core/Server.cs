using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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

            _mainThread = new Thread(AccountLoop);
            _mainThread.Name = nameof(AccountLoop);

            _clientsThread = new Thread(ClientLoop);
            _clientsThread.Name = nameof(ClientLoop);

            _mainThread.Start();
            _clientsThread.Start();
        }

        private async void ClientLoop()
        {
            _logger.LogInformation("Clients thread started");

            while (!_isDisposing)
            {
                foreach (ClientInfo client in _connectionManager.GetClients())
                {
                    DateTime now = DateTime.Now;

                    if (DateTimeUtilities.HasElapsed(now, client.LastServerCheck, new TimeSpan(0, 5, 0)))
                    {
                        client.LastServerCheck = now;

                        if (client.Version == null)
                        {
                            if (DateTimeUtilities.HasElapsed(now, client.LastVersionRequest, new TimeSpan(0, 15, 0)))
                            {
                                client.LastVersionRequest = now;
                                await _commandHubProxy.RequestClientVersion(client.ConnectionId);
                            }
                        }

                        if (_business.ClientNeedsToUpdate(client.Version))
                        {
                            _logger.LogInformation("The client {id} needs to update. Client version {ClientVersion} | Server version {ServerVersion}",
                                client.ConnectionId, client.Version, _business.LatestClientVersion);

                            if (DateTimeUtilities.HasElapsed(now, client.LastSendUpdateFile, new TimeSpan(0, 15, 0)))
                            {
                                _logger.LogInformation("Sending to client {id} update {ServerVersion}",
                                    client.ConnectionId, _business.LatestClientVersion);

                                client.LastSendUpdateFile = now;
                                await StartClientUpdate(client.ConnectionId);
                            }
                        }
                    }
                }

                Thread.Sleep(1000);
            }
        }

        private async void AccountLoop()
        {
            _logger.LogInformation("Accounts thread started");

            DateTime now = DateTime.Now;
            while (!_isDisposing)
            {
                if (DateTimeUtilities.HasElapsed(DateTime.Now, _keywordStore.LastRefresh, Settings.KeywordsListRefresh))
                {
                    await _keywordStore.RefreshList();
                }

                if(DateTime.Now.Day > now.Day) // Triggered when the next day occurs
                {
                    _logger.LogWarning("Next day occurred. Removing all jobs and resetting stats..");

                    _taskScheduler.RemoveAllJobs(); // Reset all jobs queued
                    foreach(KeyValuePair<int, MSAccountServerData> cache in CacheMSAccStats) // Force to update stats
                    {
                        cache.Value.Stats.LastDashboardCheck = DateTime.MinValue;
                        cache.Value.Stats.LastDashboardUpdate = DateTime.MinValue;
                        cache.Value.IsFirstTimeUpdateStats = true;
                    }
                }

                List<MSAccount> accounts = _business.GetAllMSAccounts();
                foreach (MSAccount acc in accounts)
                {
                    now = DateTime.Now;

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
                            IsFirstTimeUpdateStats = true,
                            Stats = acc.Stats
                        };

                        acc.Stats.UserId = acc.UserId;
                        acc.Stats.MSAccountId = acc.DbId;
                        acc.Stats.PropertyChanged += MsAccountStats_PropertyChanged;

                        CacheMSAccStats.Add(acc.DbId, cache);
                    }

                    if (DateTimeUtilities.HasElapsed(now, cache.Stats.LastDashboardCheck, Settings.DashboardCheck))
                    {
                        cache.Stats.LastDashboardCheck = now; //Fix for not queueing the same job while we wait for the job's completion
                        if (DateTimeUtilities.HasElapsed(now, cache.Stats.LastDashboardUpdate, Settings.DashboardUpdate))
                        {
                            AddJobDashboardUpdate(cache);
                        }
                    }

                    if (cache.IsFirstTimeUpdateStats)
                    {
                        continue;
                    }

                    if (DateTimeUtilities.HasElapsed(now, cache.Stats.LastSearchesCheck, Settings.SearchesCheck))
                    {
                        cache.Stats.LastSearchesCheck = now;

                        _taskScheduler.AddJob(now, new Job(new AdditionalPointsCommand()
                        {
                            Data = cache,
                            OnSuccess = delegate ()
                            {
                                _logger.LogDebug("Job {name} succeded for {user}",
                                    nameof(AdditionalPointsCommand), acc.Email);
                            },
                            OnFail = delegate ()
                            {
                                _logger.LogWarning("Job {name} failed for {user}",
                                    nameof(AdditionalPointsCommand), acc.Email);

                                cache.Stats.LastSearchesCheck = DateTime.Now;
                            }
                        }));

                        if (cache.Stats.PCSearchesToDo > 0)
                        {
                            DateTime start = now;

                            for (int i = 0; i < cache.Stats.PCSearchesToDo; i++)
                            {
                                start = start.AddSeconds(Random.Shared.Next(180, 600));

                                string keyword = await _keywordProvider.GetKeyword();
                                if(keyword == null)
                                {
                                    break;
                                }

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

                                            if (i == cache.Stats.PCSearchesToDo - 1)
                                            {
                                                AddJobDashboardUpdate(cache);
                                            }
                                        },
                                        OnFail = delegate ()
                                        {
                                            _logger.LogWarning("Job {name} failed for {user}",
                                                nameof(PCSearchCommand), acc.Email);

                                            cache.Stats.PCSearchFailed();
                                            AddJobDashboardUpdate(cache);
                                        }
                                    });

                                _taskScheduler.AddJob(start, job);

                                cache.Stats.LastSearchesCheck = start;
                            }
                        }

                        if (cache.Stats.MobileSearchesToDo > 0)
                        {
                            DateTime start = now;

                            for (int i = 0; i < cache.Stats.MobileSearchesToDo; i++)
                            {
                                start = start.AddSeconds(Random.Shared.Next(180, 600));

                                string keyword = await _keywordProvider.GetKeyword();
                                if (keyword == null)
                                {
                                    break;
                                }

                                Job job = new Job(
                                    new MobileSearchCommand()
                                    {
                                        Data = cache,
                                        Keyword = keyword,
                                        OnSuccess = delegate ()
                                        {
                                            _logger.LogDebug("Job {name} succeded (with keyword {keyword}) for {user}",
                                                nameof(MobileSearchCommand), keyword, acc.Email);

                                            cache.Stats.MobileSearchCompleted();

                                            if (i == cache.Stats.MobileSearchesToDo - 1)
                                            {
                                                AddJobDashboardUpdate(cache);
                                            }
                                        },
                                        OnFail = delegate ()
                                        {
                                            _logger.LogWarning("Job {name} failed for {user}", 
                                                nameof(MobileSearchCommand), acc.Email);

                                            cache.Stats.MobileSearchFailed();
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
                                                nameof(DashboardUpdateCommand), data.Account.Email);

                                        data.IsFirstTimeUpdateStats = false;
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

        private async Task<bool> StartClientUpdate(string connectionId)
        {
            byte[] file = _business.GetClientUpdateFile();
            if (file == null || file.Length == 0)
            {
                return false;
            }

            await _commandHubProxy.SendClientUpdateFile(connectionId, file);
            return true;
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
