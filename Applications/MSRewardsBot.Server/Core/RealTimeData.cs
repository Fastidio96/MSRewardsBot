using System;
using System.Collections.Concurrent;
using MSRewardsBot.Server.DataEntities;

namespace MSRewardsBot.Server.Core
{
    public class RealTimeData : IDisposable
    {
        public ConcurrentDictionary<int, MSAccountServerData> CacheMSAccStats { get; private set; }

        public RealTimeData()
        {
            CacheMSAccStats = new ConcurrentDictionary<int, MSAccountServerData>();
        }

        public void Dispose()
        {
            CacheMSAccStats?.Clear();
            CacheMSAccStats = null;
        }
    }
}
