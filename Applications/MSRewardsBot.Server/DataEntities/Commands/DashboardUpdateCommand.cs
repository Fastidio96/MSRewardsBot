using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Server.DataEntities.Commands
{
    public class DashboardUpdateCommand : CommandBase
    {
        public MSAccount Account { get; set; }

    }
}
