namespace MSRewardsBot.Server.Automation
{
    internal class BrowserConstants
    {
        public const string URL_DASHBOARD = "https://rewards.bing.com/status/pointsbreakdown";

        public const string SELECTOR_BREAKDOWN_PC_POINTS = @"document.querySelector(""#userPointsBreakdown > div > div:nth-child(2) > div > div > div > div.pointsDetail > mee-rewards-user-points-details > div > div > div > div > p.pointsDetail.c-subheading-3.ng-binding"").innerText";
        public const string SELECTOR_EMAIL = @"document.querySelector(""#mectrl_currentAccount_secondary"").innerHTML.trim()";
    }
}
