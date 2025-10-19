using System;
using System.Threading.Tasks;
using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Common.DataEntities.Interfaces
{
    public interface IBotAPI
    {
        public Task SendTestMessage(string m);

        public Task<Guid> Login(User user);
        public Task<Guid> Register(User user);
    }
}
