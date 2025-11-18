using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MSRewardsBot.Common.DataEntities.Accounting;
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

        private bool _isDisposing = false;
        private Lock _lock = new Lock();

        public TaskScheduler(BrowserManager browser, BusinessLayer bl)
        {
            _todo = new SortedList<DateTime, Job>();

            _browser = browser;
            _business = bl;

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
            while (_todo.ContainsKey(dt)) //Find first available space (index)
            {
                dt.AddMicroseconds(1);
            }

            using (_lock.EnterScope())
            {
                _todo.Add(dt, job);
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
                    if (todo.Key > DateTime.Now)
                    {
                        Thread.Sleep(1000);
                        break;
                    }

                    Job job = todo.Value;

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
                    else if (job.Command is PCSearchCommand pcCMD)
                    {
                        job.Status = await _browser.DoPCSearch(pcCMD.Data, pcCMD.Keyword) ?
                            JobStatus.Success : JobStatus.Failure;
                    }



                    if (job.Status != JobStatus.Pending)
                    {
                        if (job.Status == JobStatus.Success)
                        {
                            job.Command.OnSuccess?.Invoke();
                        }
                        else
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
