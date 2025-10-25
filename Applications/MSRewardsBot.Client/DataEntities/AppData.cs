using System;

namespace MSRewardsBot.Client.DataEntities
{
    /// <summary>
    /// Stored into disk
    /// </summary>
    public class AppData
    {
        public AppData()
        {
        }

        public Guid? AuthToken { get; set; }

    }
}
