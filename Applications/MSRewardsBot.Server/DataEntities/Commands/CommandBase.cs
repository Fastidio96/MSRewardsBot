using System;

namespace MSRewardsBot.Server.DataEntities.Commands
{
    public abstract class CommandBase
    {
        public MSAccountServerData Data { get; set; }

        public virtual Action? OnSuccess { get; set; }
        public virtual Action? OnFail { get; set; }
    }
}
