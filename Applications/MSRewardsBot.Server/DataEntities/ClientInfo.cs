using System;
using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Server.DataEntities
{
    public class ClientInfo
    {
        /// <summary>
        /// Do not set! It's set automatically
        /// </summary>
        public DateTime LastUpdatedInfo { get; set; }
        public DateTime LastServerCheck {  get; set; }

        public string ConnectionId { get; set; }
        public User User { get; set; }

        public string IP {  get; set; }
        public Version Version { get; set; }

        public DateTime LastVersionRequest { get; set; }
        public DateTime LastSendUpdateFile { get; set; }

    }
}
