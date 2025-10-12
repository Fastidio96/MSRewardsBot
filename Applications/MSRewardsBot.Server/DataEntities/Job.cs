using System;
using MSRewardsBot.Common.DataEntities.Commands;

namespace MSRewardsBot.Server.DataEntities
{
    public class Job
    {
        public DateTime SubmittedAt { get; set; }
        public JobStatus Status { get; set; }
        public JobPriority Priority { get; set; }

        public string ConnectionId { get; set; }
        public CommandBase Command { get; set; }

        public Job()
        {
            SubmittedAt = DateTime.Now;
            Status = JobStatus.Pending;
            Priority = JobPriority.Medium;
        }

        public Job(string connectionId) : this()
        {
            ConnectionId = connectionId;
        }
    }

    public enum JobPriority : byte
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Urgent = 3
    }

    public enum JobStatus : byte
    {
        Pending = 0,
        Success = 1,
        Failure = 2
    }
}
