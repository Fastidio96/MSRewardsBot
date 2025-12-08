using System;

namespace MSRewardsBot.Server.Automation
{
    internal class BrowserConstants
    {
        public const string URL_DASHBOARD = "https://rewards.bing.com/";
        public const string URL_DASHBOARD_PTS_BREAKDOWN = "https://rewards.bing.com/status/pointsbreakdown";
        public const string URL_SEARCHES_HOMEPAGE = "https://www.bing.com/";
        public const string URL_SEARCHES = "https://www.bing.com/search?q=";
        public const string URL_EXPIRED_COOKIES = "https://login.live.com/";
        public const string URL_BLANK_PAGE = "about:blank";

        #region URL_DASHBOARD_PTS_BREAKDOWN
        public const string SELECTOR_EMAIL = @"document.querySelector('#mectrl_currentAccount_secondary').innerHTML.trim();";
        public const string SELECTOR_ACCOUNT_TOTAL_POINTS = @"document.querySelector('#balanceToolTipDiv > p > mee-rewards-counter-animation > span').innerText;";
        public const string SELECTOR_BREAKDOWN_PC_POINTS = @"document.querySelector('#userPointsBreakdown > div > div:nth-child(2) > div > div > div > div.pointsDetail > mee-rewards-user-points-details > div > div > div > div > p.pointsDetail.c-subheading-3.ng-binding').innerText;";
        public const string SELECTOR_BREAKDOWN_MOBILE_POINTS = @"document.querySelector('#userPointsBreakdown > div > div:nth-child(2) > div > div:nth-child(2) > div > div.pointsDetail > mee-rewards-user-points-details > div > div > div > div > p.pointsDetail.c-subheading-3.ng-binding').innerText;";
        public const string SELECTOR_ACCOUNT_LEVEL = @"document.querySelector('#meeGradientBanner > div > div > div > p').innerText;";
        public const string SELECTOR_ACCOUNT_LEVEL_POINTS = @"document.querySelector('#earningreport-level-heading > p.pointsDetail.c-subheading-3.ng-binding.ng-scope').innerText;";
        #endregion

        #region URL_SEARCHES_HOMEPAGE_PC
        public const string PC_CLICK_BING_HOMEPAGE_LOGIN_BTN = @"document.querySelector('#id_l').click();";
        public const string PC_CLICK_YES_GDPR_BTN = @"document.querySelector('#bnp_btn_accept')?.click();";
        public const string PC_CLICK_SEARCHBAR_TEXTAREA = @"document.querySelector('#sb_form_q').click();";
        public const string PC_APPEND_KEYWORD_SEARCHBAR_TEXTAREA = @"document.querySelector('#sb_form_q').value += ""{keyword}"";";
        public const string PC_CLICK_SUBMIT_SEARCHBAR_TEXTAREA = @"document.querySelector('#sb_form_go').click();";
        #endregion

        #region URL_SEARCHES_HOMEPAGE_MOBILE
        public const string MOBILE_CLICK_YES_GDPR_BTN = @"document.querySelector('#bnp_btn_accept')?.click();";
        public const string MOBILE_CLICK_SEARCHBAR_TEXTAREA = @"document.querySelector('#sb_form_q').click();";
        public const string MOBILE_APPEND_KEYWORD_SEARCHBAR_TEXTAREA = @"document.querySelector('#sb_form_q').value += ""{keyword}"";";
        public const string MOBILE_CLICK_SUBMIT_SEARCHBAR_TEXTAREA = @"document.querySelector('#sb_form_go').click();";
        #endregion

        #region Additional Points Dashboard
        public const string ADDITIONAL_PTS_IMAGE_LOCATOR = @".mee-icon-AddMedium";
        public const string ADDITIONAL_PTS_IMAGE_CLICK = @"document.querySelectorAll('{locator}')[{idx}].click();";
        public const string ADDITIONAL_PTS_CLAIM_PTS = @"document.querySelector('#user-pointclaim > button.button.ng-binding')?.click();";
        #endregion

        #region Timespans delay (milliseconds)
        public const int HUMAN_WRITING_MIN = 300;
        public const int HUMAN_WRITING_MAX = 700;
        public const int DELAY_CLICK = 2000;
        public const int HUMAN_CLICK_BTN_MIN = 200 + DELAY_CLICK;
        public const int HUMAN_CLICK_BTN_MAX = 500 + DELAY_CLICK;

        #endregion

        public const string UA_PC_FIREFOX = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:138.0) Gecko/20100101 Firefox/138.0";
        public const string UA_PC_CHROME = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36";
        public const string UA_PC_EDGE = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36 Edg/142.0.0.0";
        public const string UA_MOBILE_FIREFOX = @"Mozilla/5.0 (Android 16; Mobile; rv:145.0) Gecko/145.0 Firefox/145.0";
    }
}
