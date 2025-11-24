using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Common.DataEntities.Stats;
using MSRewardsBot.Server.DataEntities;

namespace MSRewardsBot.Server.Automation
{
    public partial class BrowserManager
    {
        public async Task<bool> DashboardUpdate(MSAccountServerData data)
        {
            _logger.LogInformation("Dashboard update started for {User} | {Data}", data.Account.User.Username, data.Account.Email);

            if (!await StartLoggedSession(data))
            {
                return false;
            }

            //if (!await NavigateToURL(data, BrowserConstants.URL_DASHBOARD_PTS_BREAKDOWN))
            if (!await NavigateToURL(data, "https://deviceandbrowserinfo.com/are_you_a_bot"))
            //if (!await NavigateToURL(data, "https://deviceandbrowserinfo.com/info_device"))
            {
                return false;
            }

            bool res = true;

            try
            {
                if (string.IsNullOrEmpty(data.Account.Email))
                {
                    data.Account.Email = await data.Page.EvaluateAsync<string>(BrowserConstants.SELECTOR_EMAIL);
                    _logger.LogInformation("New account email found. {Email}", data.Account.Email);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error: {e}", e.Message);
                res = false;
            }

            try
            {
                string pcPoints = await data.Page.EvaluateAsync<string>(BrowserConstants.SELECTOR_BREAKDOWN_PC_POINTS);
                pcPoints = pcPoints.Trim();
                string[] sub = pcPoints.Split('/');

                if (sub.Length == 2)
                {
                    if (int.TryParse(sub[0], out int currentPts))
                    {
                        _logger.LogDebug("Found value for {stat}: {val}", nameof(MSAccountStats.CurrentPointsPCSearches), currentPts);
                        data.Account.Stats.CurrentPointsPCSearches = currentPts;
                    }
                    if (int.TryParse(sub[1], out int maxPts))
                    {
                        _logger.LogDebug("Found value for {stat}: {val}", nameof(MSAccountStats.MaxPointsPCSearches), maxPts);
                        data.Account.Stats.MaxPointsPCSearches = maxPts;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error: {e}", e.Message);
                res = false;
            }

            try
            {
                string accLevel = await data.Page.EvaluateAsync<string>(BrowserConstants.SELECTOR_ACCOUNT_LEVEL);
                accLevel = accLevel.Trim();
                string nLev = accLevel.Substring(accLevel.Length - 1, 1);
                if (int.TryParse(nLev, out int accountLevel))
                {
                    _logger.LogDebug("Found value for {stat}: {val}", nameof(MSAccountStats.CurrentAccountLevel), accountLevel);
                    data.Account.Stats.CurrentAccountLevel = accountLevel;
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error: {e}", e.Message);
                res = false;
            }

            try
            {
                string accLevelRatioPts = await data.Page.EvaluateAsync<string>(BrowserConstants.SELECTOR_ACCOUNT_LEVEL_POINTS);
                accLevelRatioPts = accLevelRatioPts.Trim();
                string[] split = accLevelRatioPts.Split('/');
                string nLevPts = split[0].Trim();

                if (int.TryParse(nLevPts, out int accountPtsLevel))
                {
                    _logger.LogDebug("Found value for {stat}: {val}", nameof(MSAccountStats.CurrentAccountLevelPoints), accountPtsLevel);
                    data.Account.Stats.CurrentAccountLevelPoints = accountPtsLevel;
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error: {e}", e.Message);
                res = false;
            }

            return res;
        }
    }
}
