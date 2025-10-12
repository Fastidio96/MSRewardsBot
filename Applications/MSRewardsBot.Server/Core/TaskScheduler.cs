using System;
using System.Collections.Concurrent;
using System.Threading;
using MSRewardsBot.Server.DataEntities;
using MSRewardsBot.Common.Utilities;

namespace MSRewardsBot.Server.Core
{
    public class TaskScheduler : IDisposable
    {
        public ConcurrentPriorityQueue<Job, JobPriority> Queue { get; private set; }
        private Thread _threadScheduler;
        private bool _isDisposing = false;

        public TaskScheduler()
        {
            Queue = new ConcurrentPriorityQueue<Job, JobPriority>();

            Init();
        }

        private void Init()
        {
            _threadScheduler = new Thread(Loop);
            _threadScheduler.IsBackground = false;
            _threadScheduler.Name = "Thread TaskScheduler";
            _threadScheduler.Start();
        }

        private void Loop()
        {
            while (_isDisposing)
            {
                if (!Queue.TryDequeue(out Job job, out JobPriority priority))
                {
                    Thread.Sleep(1000);
                    continue;
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
