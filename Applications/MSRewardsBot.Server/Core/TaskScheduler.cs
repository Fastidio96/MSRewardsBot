using System;
using System.Threading;
using MSRewardsBot.Common.Utilities;
using MSRewardsBot.Server.Automation;
using MSRewardsBot.Server.DataEntities;
using MSRewardsBot.Server.DataEntities.Commands;

namespace MSRewardsBot.Server.Core
{
    public class TaskScheduler : IDisposable
    {
        public ConcurrentPriorityQueue<Job, JobPriority> Queue { get; private set; }
        private Thread _threadScheduler;

        private readonly BrowserManager _browser;

        private bool _isDisposing = false;

        public TaskScheduler(BrowserManager browser)
        {
            Queue = new ConcurrentPriorityQueue<Job, JobPriority>();
            _browser = browser;

            Init();
        }

        private void Init()
        {
            _threadScheduler = new Thread(Loop);
            _threadScheduler.IsBackground = false;
            _threadScheduler.Name = nameof(TaskScheduler);
            _threadScheduler.Start();
        }

        private async void Loop()
        {
            while (!_isDisposing)
            {
                if (!Queue.TryDequeue(out Job job, out JobPriority priority))
                {
                    Thread.Sleep(1000);
                    continue;
                }

                bool? isPass = null;

                if (job.Command is DashboardUpdateCommand command)
                {
                    isPass = await _browser.DashboardUpdate(command.Account);
                }



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

                Thread.Sleep(100);
            }
        }

        public void Dispose()
        {
            _isDisposing = true;
        }
    }
}
