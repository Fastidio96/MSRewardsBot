using System;
using MSRewardsBot.Server.DataEntities.Commands;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace MSRewardsBot.Server.DataEntities
{
    public class Job
    {
        public DateTime SubmittedAt { get; set; }
        public JobStatus Status { get; set; }
        public string ConnectionId { get; set; }
        public CommandBase Command { get; set; }

        public Job(CommandBase command)
        {
            SubmittedAt = DateTime.Now;
            Status = JobStatus.Pending;
            Command = command;
        }

        public Job(string connectionId, CommandBase command) : this(command)
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
