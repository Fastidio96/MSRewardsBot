using System.Collections.Generic;
using System.Threading.Tasks;

namespace MSRewardsBot.Server.Automation
{
    public interface IKeywordProvider
    {
        Task<string> GetKeyword();
        IReadOnlyList<string> GetAll();
    }
}
