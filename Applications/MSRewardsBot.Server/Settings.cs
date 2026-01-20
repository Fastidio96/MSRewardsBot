using System;

namespace MSRewardsBot.Server
{
    public class Settings
    {
        /// <summary>
        /// Disable this for saving performances
        /// </summary>
        public bool IsClientUpdaterEnabled { get; set; } = false;

        /// <summary>
        /// Chromium is more stable than firefox but can be reconized as a bot
        /// </summary>
        public bool UseFirefox { get; set; } = true;

        /// <summary>
        /// Min random time between searches
        /// </summary>
        public int MinSecsWaitBetweenSearches { get; set; } = 180;

        /// <summary>
        /// Max random time between searches
        /// </summary>
        public int MaxSecsWaitBetweenSearches { get; set; } = 600;

        /// <summary>
        /// Define the amount of time the program check for the dashboard action
        /// </summary>
        public TimeSpan DashboardCheck { get; set; } = new TimeSpan(12, 0, 0);

        /// <summary>
        /// Define the amount of time the program check if there any dashboard update(stats) job to do
        /// </summary>
        public TimeSpan DashboardUpdate { get; set; } = new TimeSpan(0, 15, 0);

        /// <summary>
        /// Define the amount of time the program check if there any search job to do
        /// </summary>
        public TimeSpan SearchesCheck { get; set; } = new TimeSpan(18, 0, 0);

        /// <summary>
        /// Define the amount of time keyword list should be refreshed
        /// </summary>
        public TimeSpan KeywordsListRefresh { get; set; } = new TimeSpan(3, 0, 0);

        /// <summary>
        /// More countries mean more keywords to search for.
        /// For each country, 50 keywords will be downloaded.
        /// Each keyword is consumed after use; when all keywords are exhausted, the list restarts from the beginning.
        /// The intended behavior, however, is that once a keyword is consumed, it should never be reused.
        /// </summary>
        public string[] KeywordsListCountries { get; set; } = ["IT", "US", "GB", "DE", "FR", "ES"];
    }
}
