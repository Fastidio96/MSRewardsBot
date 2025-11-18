using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Server.DataEntities.Commands
{
    public class PCSearchCommand : CommandBase
    {
        public MSAccountServerData Data { get; set; }
        public string Keyword { get; set; }
    }
}
