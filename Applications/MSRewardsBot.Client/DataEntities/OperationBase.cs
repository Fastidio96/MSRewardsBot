using System;

namespace MSRewardsBot.Client.DataEntities
{
    public abstract class OperationBase : IDisposable
    {
        public event EventHandler OnOperationCompleted;

        public string Url { get; set; }
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                _isCompleted = value;
                if (IsCompleted)
                {
                    OnOperationCompleted?.Invoke(this, new EventArgs());
                }
            }
        }
        private bool _isCompleted;
        
        public bool IsStarted { get; set; }
        public Int32 WaitBeforeProceedTimeout { get; set; }

        public virtual void Dispose()
        {
            OnOperationCompleted = null;
        }
    }

}
