using System;
using MSRewardsBot.Server.DataEntities.Commands;

namespace MSRewardsBot.Server.DataEntities
{
    public class Job
    {
        public DateTime SubmittedAt { get; set; }
        public JobStatus Status { get; set; }
        public string ConnectionId { get; set; }
        public CommandBase Command { get; set; }

        public Job(string connectionId, CommandBase command)
        {
            SubmittedAt = DateTime.Now;
            Status = JobStatus.Pending;
            ConnectionId = connectionId;
            Command = command;
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
