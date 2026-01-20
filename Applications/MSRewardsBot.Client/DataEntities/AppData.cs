using System;

namespace MSRewardsBot.Client.DataEntities
{
    /// <summary>
    /// Stored on disk
    /// </summary>
    public class AppData
    {
        public AppData()
        {
        }

        public Guid? AuthToken { get; set; }

        public string ServerHost { get; set; }
        public string ServerPort { get; set; }
        public bool IsHttpsEnabled { get; set; }
    }
}
