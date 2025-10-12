using System.Threading.Tasks;

namespace MSRewardsBot.Common.DataEntities.Interfaces
{
    public interface IBotAPI
    {
        public Task SendTestMessage(string m);
    }
}
