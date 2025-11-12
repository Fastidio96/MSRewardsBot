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
                _todo = (SortedList<DateTime, Job>)_todo.OrderBy(t => t.Key);
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
                _todo = (SortedList<DateTime, Job>)_todo.OrderBy(t => t.Key);
            }
        }

        private async void Loop()
        {
            while (!_isDisposing)
            {
                foreach (KeyValuePair<DateTime, Job> todo in _todo)
                {
                    if (todo.Key > DateTime.Now)
                    {
                        Thread.Sleep(1000);
                        break;
                    }

                    Job job = todo.Value;
                    bool? isPass = null;

                    if (job.Command is DashboardUpdateCommand command)
                    {
                        MSAccount acc = await _browser.DashboardUpdate(command.Account);
                        isPass = acc != null;

                        job.Status = isPass == true ?
                            JobStatus.Success : JobStatus.Failure;

                        if (isPass == true)
                        {
                            acc.Stats.LastDashboardUpdate = DateTime.Now;
                            if (!_business.UpdateMSAccount(acc))
                            {
                                acc.Stats.LastDashboardUpdate = DateTime.MinValue;
                                isPass = false;
                            }
                        }
                    }



                    if (job.Status != JobStatus.Pending)
                    {
                        if (isPass.HasValue)
                        {
                            if (isPass.Value)
                            {
                                job.Command.OnSuccess?.Invoke();
                            }
                            else
                            {
                                job.Command.OnFail?.Invoke();
                            }
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
