using System;

namespace MSRewardsBot.Server
{
    public class Settings
    {
        public static TimeSpan AuthTokenExpires = new TimeSpan(30, 0, 0, 0);

        public static TimeSpan DashboardCheck = new TimeSpan(1, 0, 0);
        public static TimeSpan DashboardUpdate = new TimeSpan(0, 15, 0);
        public static TimeSpan SearchesCheck = new TimeSpan(12, 0, 0);

        public static TimeSpan KeywordsListRefresh = new TimeSpan(3, 0, 0);
    }
}
