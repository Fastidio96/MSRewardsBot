using Microsoft.Playwright;
using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Server.DataEntities
{
    public class MSAccountServerData
    {
        public IBrowserContext Context { get; set; }
        public IPage Page { get; set; }

        public bool IsFirstTimeUpdateStats { get; set; }

        public MSAccount Account { get; set; }
        public MSAccountStats Stats { get; set; }
    }
}
