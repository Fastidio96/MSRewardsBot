using System.Collections.Generic;

namespace MSRewardsBot.Server.Automation
{
    public interface IKeywordProvider
    {
        string GetRandom();
        IReadOnlyList<string> GetAll();
    }
}
