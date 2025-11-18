using System.Collections.Generic;

namespace MSRewardsBot.Server.Automation
{
    public interface IKeywordProvider
    {
        string GetKeyword();
        IReadOnlyList<string> GetAll();
    }
}
