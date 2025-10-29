using System;

namespace MSRewardsBot.Server.DataEntities.Commands
{
    public abstract class CommandBase
    {
        public Action? OnSuccess { get; set; }
        public Action? OnFail { get; set; }
    }
}
