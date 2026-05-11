using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Common.Utilities;
using MSRewardsBot.Server.Automation;
using MSRewardsBot.Server.Core.Factories;
using MSRewardsBot.Server.DataEntities;
using MSRewardsBot.Server.DataEntities.Commands;
using MSRewardsBot.Server.Network;

namespace MSRewardsBot.Server.Core
{
    public class Server : IDisposable
    {
        private readonly ILogger<Server> _logger;
        private readonly IOptions<Settings> _settings;
        private readonly BusinessFactory _businessFactory;
        private readonly RealTimeData _rt;
        private readonly IConnectionManager _connectionManager;
        private readonly CommandHubProxy _commandHubProxy;
        private readonly BrowserManager _browser;
        private readonly TaskScheduler _taskScheduler;

        private readonly IKeywordProvider _keywordProvider;
        private readonly KeywordStore _keywordStore;

        private Task _mainLoopTask;
        private CancellationTokenSource _cts;

        public Server
        (
            ILogger<Server> logger,
            IOptions<Settings> settings,
            BusinessFactory businessFactory,
            RealTimeData rt,
            IConnectionManager connectionManager,
            CommandHubProxy commandHubProxy,
            BrowserManager browser,
            TaskScheduler taskScheduler
        )
        {
            _logger = logger;
            _settings = settings;
            _businessFactory = businessFactory;
            _rt = rt;
            _connectionManager = connectionManager;
            _commandHubProxy = commandHubProxy;
            _browser = browser;
            _taskScheduler = taskScheduler;

            _keywordStore = new KeywordStore(_settings.Value.KeywordsListCountries);
            _keywordProvider = new KeywordProvider(_keywordStore);
        }

        public async Task Start()
        {
            await _browser.Init();

            _cts = new CancellationTokenSource();
            _mainLoopTask = Task.Run(() => AccountLoopAsync(_cts.Token));
        }

        private async Task AccountLoopAsync(CancellationToken ct)
        {
            _logger.LogDebug("Accounts loop started");

            DateTime now = DateTime.Now;
            List<MSAccount> accounts;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (DateTimeUtilities.HasElapsed(DateTime.Now, _keywordStore.LastRefresh, _settings.Value.KeywordsListRefresh))
                    {
                        if (await _keywordStore.RefreshList())
                        {
                            _logger.LogInformation("Keywords list refreshed");
                        }
                    }

                    if (DateTime.Now.Day > now.Day) // Triggered when the next day occurs
                    {
                        _logger.LogWarning("Next day occurred. Removing all jobs and resetting stats..");

                        _taskScheduler.RemoveAllJobs(); // Reset all jobs queued
                        foreach (KeyValuePair<int, MSAccountServerData> cache in _rt.CacheMSAccStats) // Force to update stats
                        {
                            cache.Value.IsFirstTimeUpdateStats = true;
                            cache.Value.Stats.LastDashboardUpdate = DateTime.MinValue;
                            cache.Value.Stats.LastDashboardPointsCheck = DateTime.MinValue;
                            cache.Value.Stats.LastSearchesCheck = DateTime.MinValue;
                        }
                    }

                    using (ScopedBusiness scope = _businessFactory.Create())
                    {
                        accounts = scope.Business.GetAllMSAccounts();
                    }

                    // Drop cache entries for accounts that no longer exist in DB (deleted by user)
                    // or whose cookies have been refreshed via UpdateMSAccountCookies (cache pre-removed by Business).
                    // Unsubscribe events to avoid pushing stale data to the client.
                    foreach (int cachedId in _rt.CacheMSAccStats.Keys.ToList())
                    {
                        if (accounts.Any(a => a.DbId == cachedId))
                        {
                            continue;
                        }
                        if (_rt.CacheMSAccStats.TryRemove(cachedId, out MSAccountServerData removed))
                        {
                            removed.Account.Stats.PropertyChanged -= MsAccountStats_PropertyChanged;
                            removed.Account.PropertyChanged -= MsAccount_PropertyChanged;
                        }
                    }

                    foreach (MSAccount acc in accounts)
                    {
                        now = DateTime.Now;

                        if (acc.IsAccountBanned)
                        {
                            continue;
                        }

                        if (acc.Cookies.Count == 0 || acc.IsCookiesExpired)
                        {
                            _logger.LogWarning("No valid cookies found for account {Email} | {Username}. Skipping..",
                                acc.Email, acc.User.Username);
                            continue;
                        }

                        // Use GetOrAdd so the (insert + side-effects) is atomic w.r.t. concurrent
                        // TryRemove from DeleteMSAccount/UpdateMSAccountCookies. Side-effects (event
                        // subscriptions, UserId/MSAccountId assignment) only run on actual insertion.
                        bool inserted = false;
                        MSAccountServerData cache = _rt.CacheMSAccStats.GetOrAdd(acc.DbId, _ =>
                        {
                            inserted = true;
                            acc.Stats.UserId = acc.UserId;
                            acc.Stats.MSAccountId = acc.DbId;
                            acc.Stats.PropertyChanged += MsAccountStats_PropertyChanged;
                            acc.PropertyChanged += MsAccount_PropertyChanged;
                            return new MSAccountServerData()
                            {
                                Account = acc,
                                IsFirstTimeUpdateStats = true,
                                Stats = acc.Stats
                            };
                        });

                        // GetOrAdd may invoke the factory more than once under contention. If our
                        // factory ran but the dictionary kept another instance, undo our subscriptions
                        // so they don't fire on a now-orphaned MSAccount instance.
                        if (inserted && !ReferenceEquals(cache.Account, acc))
                        {
                            acc.Stats.PropertyChanged -= MsAccountStats_PropertyChanged;
                            acc.PropertyChanged -= MsAccount_PropertyChanged;
                        }

                        if (DateTimeUtilities.HasElapsed(now, cache.Stats.LastDashboardUpdate, _settings.Value.DashboardCheck))
                        {
                            cache.Stats.LastDashboardUpdate = now;
                            AddJobDashboardUpdate(cache);
                        }

                        if (cache.IsFirstTimeUpdateStats)
                        {
                            continue;
                        }

                        if (DateTimeUtilities.HasElapsed(now, cache.Stats.LastDashboardPointsCheck, _settings.Value.DashboardPointsCheck))
                        {
                            cache.Stats.LastDashboardPointsCheck = now;

                            _taskScheduler.AddJob(now, new Job(new AdditionalPointsCommand()
                            {
                                Data = cache,
                                OnSuccess = delegate ()
                                {
                                    _logger.LogInformation("Job {name} successed for {user}",
                                        nameof(AdditionalPointsCommand), acc.Email);
                                },
                                OnFail = delegate ()
                                {
                                    _logger.LogWarning("Job {name} failed for {user}",
                                        nameof(AdditionalPointsCommand), acc.Email);

                                    if (acc.IsCookiesExpired || acc.IsAccountBanned)
                                    {
                                        return;
                                    }

                                    cache.Stats.LastDashboardPointsCheck = DateTime.MinValue;
                                }
                            }));
                        }

                        if (DateTimeUtilities.HasElapsed(now, cache.Stats.LastSearchesCheck, _settings.Value.SearchesCheck))
                        {
                            cache.Stats.LastSearchesCheck = now;

                            if (cache.Stats.PCSearchesToDo > 0)
                            {
                                DateTime start = now;

                                for (int i = 0; i < cache.Stats.PCSearchesToDo; i++)
                                {
                                    start = start.AddSeconds
                                    (
                                        Random.Shared.Next(_settings.Value.MinSecsWaitBetweenSearches, _settings.Value.MaxSecsWaitBetweenSearches)
                                    );

                                    string keyword = await _keywordProvider.GetKeyword();
                                    if (keyword == null)
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
                                                _logger.LogInformation("Job {name} successed (with keyword {keyword}) for {user}",
                                                    nameof(PCSearchCommand), keyword, acc.Email);

                                                cache.Stats.PCSearchCompleted();

                                                if (acc.IsCookiesExpired || acc.IsAccountBanned)
                                                {
                                                    return;
                                                }


                                                if (cache.Stats.PCSearchesToDo >= cache.Stats.MaxPointsPCSearches)
                                                {
                                                    AddJobDashboardUpdate(cache);
                                                }
                                            },
                                            OnFail = delegate ()
                                            {
                                                _logger.LogWarning("Job {name} failed for {user}",
                                                    nameof(PCSearchCommand), acc.Email);

                                                if (acc.IsCookiesExpired || acc.IsAccountBanned)
                                                {
                                                    return;
                                                }

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
                                                _logger.LogInformation("Job {name} successed (with keyword {keyword}) for {user}",
                                                    nameof(MobileSearchCommand), keyword, acc.Email);

                                                cache.Stats.MobileSearchCompleted();

                                                if (acc.IsCookiesExpired || acc.IsAccountBanned)
                                                {
                                                    return;
                                                }

                                                if (cache.Stats.MobileSearchesToDo >= cache.Stats.MaxPointsMobileSearches)
                                                {
                                                    AddJobDashboardUpdate(cache);
                                                }
                                            },
                                            OnFail = delegate ()
                                            {
                                                _logger.LogWarning("Job {name} failed for {user}",
                                                    nameof(MobileSearchCommand), acc.Email);

                                                if (acc.IsCookiesExpired || acc.IsAccountBanned)
                                                {
                                                    return;
                                                }

                                                AddJobDashboardUpdate(cache);
                                            }
                                        });

                                    _taskScheduler.AddJob(start, job);

                                    cache.Stats.LastSearchesCheck = start;
                                }
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, ex, "Error in AccountLoop iteration");
                }

                try
                {
                    await Task.Delay(1000, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private void AddJobDashboardUpdate(MSAccountServerData data)
        {
            Job job = new Job(
                                new DashboardUpdateCommand()
                                {
                                    Data = data,
                                    OnSuccess = delegate ()
                                    {
                                        _logger.LogInformation("Job {name} successed for {user}",
                                                nameof(DashboardUpdateCommand), data.Account.Email);

                                        data.IsFirstTimeUpdateStats = false;
                                    },
                                    OnFail = delegate ()
                                    {
                                        _logger.LogWarning("Job {name} failed", nameof(DashboardUpdateCommand));

                                        if (data.Account.IsCookiesExpired || data.Account.IsAccountBanned)
                                        {
                                            return;
                                        }

                                        data.Stats.LastDashboardUpdate = DateTime.MinValue; // RetryAsync again after failure
                                    }
                                });

            _taskScheduler.AddJob(DateTime.Now, job);
        }

        private async void MsAccountStats_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Swallow exceptions to avoid taking down the process
            // when the client connection is dropped mid-push.
            try
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
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error pushing stats update to client");
            }
        }

        private async void MsAccount_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (sender is MSAccount account)
                {
                    ClientInfo info = _connectionManager.GetConnection(account.UserId);
                    if (info == null)
                    {
                        return;
                    }

                    MSAccount payload = new MSAccount
                    {
                        DbId = account.DbId,
                        UserId = account.UserId,
                        Email = account.Email,
                        IsCookiesExpired = account.IsCookiesExpired,
                        IsAccountBanned = account.IsAccountBanned,
                        Stats = account.Stats
                    };

                    await _commandHubProxy.SendUpdateMSAccount(info.ConnectionId, payload, e.PropertyName);
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error pushing account update to client");
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();

            try
            {
                // Wait for the loop to actually finish so we don't unsubscribe events
                // mid-iteration (which can race with PropertyChanged invocations on the
                // browser threads).
                _mainLoopTask?.Wait(5000);
            }
            catch
            {
                // task may have faulted; nothing actionable here
            }

            _cts?.Dispose();
            _cts = null;
            _mainLoopTask = null;

            if (_rt?.CacheMSAccStats != null)
            {
                foreach (KeyValuePair<int, MSAccountServerData> acc in _rt.CacheMSAccStats)
                {
                    acc.Value.Account.Stats.PropertyChanged -= MsAccountStats_PropertyChanged;
                    acc.Value.Account.PropertyChanged -= MsAccount_PropertyChanged;
                }
            }
        }
    }
}
