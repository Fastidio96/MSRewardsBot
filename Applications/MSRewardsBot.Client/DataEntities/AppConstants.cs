namespace MSRewardsBot.Client.DataEntities
{
    public class AppConstants
    {
#if DEBUG
        public const bool IS_PRODUCTION = false;
#else
        public const bool IS_PRODUCTION = true;
#endif

        public const string URL_LOGIN = "https://login.live.com/";
        public const string URL_HOST_LOGGED = "account.microsoft.com";
        public const string URL_POINTS_INFOS = "https://rewards.bing.com/pointsbreakdown";
        public const string URL_REWARDS = "https://rewards.bing.com/";
        public const string URL_LOGOUT = "https://www.microsoft.com/cascadeauth/account/signout?ru=https%3A%2F%2Fwww.microsoft.com%2Fit-it%2Frewards%2Fabout";

        public const string URL_SEARCH_FORMAT = "https://www.bing.com/search?q=what+is+";
    }
}
