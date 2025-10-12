using System;

namespace MSRewardsBot.Server.DataEntities
{
    public class ClientInfo
    {
        public DateTime LastUpdatedInfo { get; set; }
        public string ConnectionId { get; set; }
        public string Username { get; set; }
        public Version CurrentVersion { get; set; }
        public DateTime LastDashboardUpdate { get; set; }

    }
}
