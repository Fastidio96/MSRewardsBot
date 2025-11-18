namespace MSRewardsBot.Server.Automation
{
    internal class BrowserConstants
    {
        public const string URL_DASHBOARD = "https://rewards.bing.com/";
        public const string URL_DASHBOARD_PTS_BREAKDOWN = "https://rewards.bing.com/status/pointsbreakdown";
        public const string URL_SEARCHES_HOMEPAGE = "https://www.bing.com/";
        public const string URL_SEARCHES = "https://www.bing.com/search?q=";

        #region URL_DASHBOARD_PTS_BREAKDOWN
        public const string SELECTOR_EMAIL = @"document.querySelector(""#mectrl_currentAccount_secondary"").innerHTML.trim()";
        
        public const string SELECTOR_BREAKDOWN_PC_POINTS = @"document.querySelector(""#userPointsBreakdown > div > div:nth-child(2) > div > div > div > div.pointsDetail > mee-rewards-user-points-details > div > div > div > div > p.pointsDetail.c-subheading-3.ng-binding"").innerText";

        public const string SELECTOR_ACCOUNT_LEVEL = @"document.querySelector(""#meeGradientBanner > div > div > div > p"").innerText";
        public const string SELECTOR_ACCOUNT_LEVEL_POINTS = @"document.querySelector(""#earningreport-level-heading > p.pointsDetail.c-subheading-3.ng-binding.ng-scope"").innerText";
        #endregion

        #region URL_SEARCHES_HOMEPAGE
        public const string CLICK_BING_HOMEPAGE_LOGIN_BTN = @"document.querySelector(""#id_l"").click()";
        public const string CLICK_YES_GDPR_BTN = @"document.querySelector(""#bnp_btn_accept"")?.click()";
        public const string CLICK_SEARCHBAR_TEXTAREA = @"document.querySelector(""#sb_form_q"").click()";
        public const string WRITE_KEYWORD_SEARCHBAR_TEXTAREA = @"document.querySelector(""#sb_form_q"").value = ""{keyword}""";
        public const string CLICK_SUBMIT_SEARCHBAR_TEXTAREA = @"document.querySelector(""#sb_form_go"").click()";
        #endregion

        public const string UA_CHROME = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36";
        public const string UA_EDGE = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36 Edg/142.0.0.0";
    }
}
