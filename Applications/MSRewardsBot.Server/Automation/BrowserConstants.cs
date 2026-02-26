namespace MSRewardsBot.Server.Automation
{
    internal class BrowserConstants
    {
        #region URLs
        public const string URL_DASHBOARD = "https://rewards.bing.com/";
        public const string URL_DASHBOARD_PTS_BREAKDOWN = "https://rewards.bing.com/status/pointsbreakdown";
        public const string URL_SEARCHES_HOMEPAGE = "https://www.bing.com/";
        public const string URL_SEARCHES = "https://www.bing.com/search?q=";
        public const string URL_EXPIRED_COOKIES = "https://login.live.com/";
        public const string URL_BLANK_PAGE = "about:blank";
        #endregion

        #region Dashboard selectors
        public const string SELECTOR_EMAIL = "#mectrl_currentAccount_secondary";
        public const string SELECTOR_ACCOUNT_TOTAL_POINTS = "#balanceToolTipDiv > p > mee-rewards-counter-animation > span";
        public const string SELECTOR_BREAKDOWN_PC_POINTS = "#userPointsBreakdown > div > div:nth-child(2) > div > div:nth-child(1) > div > div.pointsDetail > mee-rewards-user-points-details > div > div > div > div > p.pointsDetail.c-subheading-3.ng-binding";
        public const string SELECTOR_BREAKDOWN_MOBILE_POINTS = "#userPointsBreakdown > div > div:nth-child(2) > div > div:nth-child(2) > div > div.pointsDetail > mee-rewards-user-points-details > div > div > div > div > p.pointsDetail.c-subheading-3.ng-binding";
        public const string SELECTOR_ACCOUNT_LEVEL = "#meeGradientBanner > div > div > div > p";
        public const string SELECTOR_ACCOUNT_LEVEL_POINTS = "#earningreport-level-heading > p.pointsDetail.c-subheading-3.ng-binding.ng-scope";
        public const string SELECTOR_ACCOUNT_BANNED = "#fraudErrorBody";
        #endregion

        #region Searches on homepage selectors
        public const string PC_CLICK_BING_HOMEPAGE_LOGIN_BTN = "#id_l";
        public const string BTN_YES_GDPR = "#bnp_btn_accept";
        public const string SEARCHBAR_TEXTAREA = "#sb_form_q";
        #endregion

        #region Additional points dashboard selectors
        public const string ADDITIONAL_PTS_IMAGE_LOCATOR = ".mee-icon-AddMedium";
        public const string ADDITIONAL_PTS_CLAIM_PTS = "#user-pointclaim > button.button.ng-binding";
        #endregion

        #region Timespans delay (ms)
        public const int HUMAN_DELAY = 2000;
        public const int HUMAN_WRITING_MIN = 300;
        public const int HUMAN_WRITING_MAX = 700;
        public const int HUMAN_ACTION_MIN = 200 + HUMAN_DELAY;
        public const int HUMAN_ACTION_MAX = 500 + HUMAN_DELAY;
        #endregion

        #region User Agents
        public const string UA_PC_FIREFOX = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:138.0) Gecko/20100101 Firefox/138.0";
        public const string UA_MOBILE_FIREFOX = @"Mozilla/5.0 (Android 16; Mobile; rv:145.0) Gecko/145.0 Firefox/145.0";
        public const string UA_PC_CHROME = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36";
        public const string UA_MOBILE_CHROME = @"Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.7499.53 Mobile Safari/537.36";
        #endregion
    }
}
