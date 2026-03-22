using System;
using System.Collections.Generic;

namespace MSRewardsBot.Server.DataEntities.Updater
{
    public class ReleaseInfo
    {
        public List<Asset> Assets { get; set; }
        public string TagName { get; set; }
        public Version Version { get; set; }
    }
}
