using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Server.Automation;
using MSRewardsBot.Server.DataEntities;
using MSRewardsBot.Server.DataEntities.Commands;

namespace MSRewardsBot.Server.Core
{
    public class TaskScheduler : IDisposable
    {
        private SortedList<DateTime, Job> _todo;
        private Thread _threadScheduler;

        private readonly BrowserManager _browser;
        private readonly BusinessLayer _business;
        private readonly ILogger _logger;

        private bool _isDisposing = false;
        private Lock _lock = new Lock();

        public TaskScheduler(BrowserManager browser, BusinessLayer bl, ILogger<TaskScheduler> logger)
        {
            _todo = new SortedList<DateTime, Job>();

            _browser = browser;
            _business = bl;
            _logger = logger;

            Init();
        }

        private void Init()
        {
            _threadScheduler = new Thread(Loop);
            _threadScheduler.IsBackground = false;
            _threadScheduler.Name = nameof(TaskScheduler);
            _threadScheduler.Start();
        }

        public void AddJob(DateTime dt, Job job)
        {
            dt = new DateTime //Cleaned datetime from ms
            (
                dt.Year,
                dt.Month,
                dt.Day,
                dt.Hour,
                dt.Minute,
                dt.Second,
                0,
                dt.Kind
            );

            while (_todo.ContainsKey(dt)) //Find first available space (index)
            {
                dt = dt.AddSeconds(1);
            }

            if (DateTime.Now.Day != dt.Day)
            {
                return;
            }

            using (_lock.EnterScope())
            {
                _todo.Add(dt, job);
            }

            string dtCmd = dt.ToString("HH:mm:ss dd/MM/yyyy");
            if (job.Command is DashboardUpdateCommand cmdDash)
            {
                _logger.LogInformation("Added job {name} on {time} for {user}",
                    nameof(DashboardUpdateCommand), dtCmd, cmdDash.Data.Account.Email);
            }
            else if (job.Command is AdditionalPointsCommand cmdAdd)
            {
                _logger.LogInformation("Added job {name} on {time} for {user}",
                    nameof(AdditionalPointsCommand), dtCmd, cmdAdd.Data.Account.Email);
            }
            else if (job.Command is PCSearchCommand cmdPcSearch)
            {
                _logger.LogInformation("Added job {name} (with keyword {keyword}) on {time} for {user}",
                    nameof(PCSearchCommand), cmdPcSearch.Keyword, dtCmd, cmdPcSearch.Data.Account.Email);
            }
            else if (job.Command is MobileSearchCommand cmdMobileSearch)
            {
                _logger.LogInformation("Added job {name} (with keyword {keyword}) on {time} for {user}",
                    nameof(MobileSearchCommand), cmdMobileSearch.Keyword, dtCmd, cmdMobileSearch.Data.Account.Email);
            }
        }

        private void RemoveJob(DateTime key)
        {
            if (!_todo.ContainsKey(key))
            {
                return;
            }

            using (_lock.EnterScope())
            {
                _todo.Remove(key);
            }
        }

        public void RemoveAllJobs()
        {
            using (_lock.EnterScope())
            {
                _todo.Clear();
            }
        }

        private SortedList<DateTime, Job> GetTodoList()
        {
            SortedList<DateTime, Job> res = new SortedList<DateTime, Job>();
            using (_lock.EnterScope())
            {
                foreach (KeyValuePair<DateTime, Job> item in _todo)
                {
                    res.Add(item.Key, item.Value);
                }
            }

            return res;
        }

        private async void Loop()
        {
            while (!_isDisposing)
            {
                foreach (KeyValuePair<DateTime, Job> todo in GetTodoList())
                {
                    if (DateTime.Now.Day != todo.Key.Day)
                    {
                        break;
                    }

                    if (todo.Key > DateTime.Now)
                    {
                        break;
                    }

                    Job job = todo.Value;

                    if (!await _browser.CreateContext(job.Command.Data, job.Command is MobileSearchCommand))
                    {
                        job.Status = JobStatus.Failure;
                    }
                    else
                    {
                        if (job.Command is DashboardUpdateCommand dashCMD)
                        {
                            job.Status = await _browser.DashboardUpdate(dashCMD.Data) ?
                                JobStatus.Success : JobStatus.Failure;

                            if (job.Status == JobStatus.Success)
                            {
                                dashCMD.Data.Stats.LastDashboardUpdate = DateTime.Now;
                                if (!_business.UpdateMSAccount(dashCMD.Data.Account))
                                {
                                    dashCMD.Data.Stats.LastDashboardUpdate = DateTime.MinValue;
                                    job.Status = JobStatus.Failure;
                                }
                            }
                        }
                        else if (job.Command is AdditionalPointsCommand addCMD)
                        {
                            job.Status = await _browser.GetAdditionalPoints(addCMD.Data) ?
                                JobStatus.Success : JobStatus.Failure;
                        }
                        else if (job.Command is PCSearchCommand pcCMD)
                        {
                            job.Status = await _browser.PCSearch(pcCMD.Data, pcCMD.Keyword) ?
                                JobStatus.Success : JobStatus.Failure;
                        }
                        else if (job.Command is MobileSearchCommand mobileCMD)
                        {
                            job.Status = await _browser.MobileSearch(mobileCMD.Data, mobileCMD.Keyword) ?
                                JobStatus.Success : JobStatus.Failure;
                        }
                        else
                        {
                            _logger.LogError("Unknown command received! Command {cmd}", job.Command);
                            job.Status = JobStatus.CriticalFailure;
                        }
                    }

                    await _browser.DeleteContext(job.Command.Data);


                    if (job.Status != JobStatus.Pending)
                    {
                        if (job.Status == JobStatus.Success)
                        {
                            job.Command.OnSuccess?.Invoke();
                        }
                        else if (job.Status == JobStatus.Failure)
                        {
                            job.Command.OnFail?.Invoke();
                        }

                        RemoveJob(todo.Key);
                    }
                }

                Thread.Sleep(1000);
            }
        }

        public void Dispose()
        {
            _isDisposing = true;
            _todo.Clear();

            if (_threadScheduler != null)
            {
                if (_threadScheduler.IsAlive)
                {
                    _threadScheduler.Join(5000);
                }

                _threadScheduler = null;
            }
        }
    }
}
