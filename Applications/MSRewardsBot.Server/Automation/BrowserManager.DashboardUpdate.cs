using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Server.DataEntities;

namespace MSRewardsBot.Server.Automation
{
    public partial class BrowserManager
    {
        public async Task<bool> DashboardUpdate(MSAccountServerData data)
        {
            _logger.LogInformation("Dashboard update started for {User} | {Data}", data.Account.User.Username, data.Account.Email);

            if (!await NavigateToURL(data, BrowserConstants.URL_DASHBOARD_PTS_BREAKDOWN))
            {
                return false;
            }

            await WaitRandomMs(5000, 10000);

            bool res = true;

            try
            {
                ILocator selector = data.Page.Locator(BrowserConstants.SELECTOR_ACCOUNT_BANNED);
                if (await selector.CountAsync() > 0)
                {
                    _logger.LogWarning("Account {Data} is banned!", data.Account.Email);
                    data.Account.IsAccountBanned = true;

                    return true;
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error: {e}", e.Message);
                res = false;
            }

            try
            {
                if (string.IsNullOrEmpty(data.Account.Email))
                {
                    data.Account.Email = await data.Page.Locator(BrowserConstants.SELECTOR_EMAIL).InnerHTMLAsync();
                    data.Account.Email = data.Account.Email.Trim();
                    _logger.LogInformation("New account email found. {Email}", data.Account.Email);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error: {e}", e.Message);
                res = false;
            }

            if (!await GetAccTotalPoints(data))
            {
                res = false;
            }

            try
            {
                string accLevel = await data.Page.Locator(BrowserConstants.SELECTOR_ACCOUNT_LEVEL).InnerTextAsync();
                accLevel = accLevel.Trim();
                string nLev = accLevel.Substring(accLevel.Length - 1, 1);
                if (int.TryParse(nLev, out int accountLevel))
                {
                    _logger.LogDebug("Found value for {stat}: {val}", nameof(MSAccountStats.CurrentAccountLevel), accountLevel);
                    data.Account.Stats.CurrentAccountLevel = accountLevel;
                }
                else
                {
                    _logger.LogWarning("Cannot parse {stat}", nameof(MSAccountStats.CurrentAccountLevel));
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error: {e}", e.Message);
                res = false;
            }

            try
            {
                string pcPoints = await data.Page.Locator(BrowserConstants.SELECTOR_BREAKDOWN_PC_POINTS).InnerTextAsync();
                pcPoints = pcPoints.Trim();
                string[] sub = pcPoints.Split('/');

                if (sub.Length == 2)
                {
                    if (int.TryParse(sub[0], out int currentPts))
                    {
                        _logger.LogDebug("Found value for {stat}: {val}", nameof(MSAccountStats.CurrentPointsPCSearches), currentPts);
                        data.Account.Stats.CurrentPointsPCSearches = currentPts;
                    }
                    else
                    {
                        _logger.LogWarning("Cannot parse {stat}", nameof(MSAccountStats.CurrentPointsPCSearches));
                    }

                    if (int.TryParse(sub[1], out int maxPts))
                    {
                        _logger.LogDebug("Found value for {stat}: {val}", nameof(MSAccountStats.MaxPointsPCSearches), maxPts);
                        data.Account.Stats.MaxPointsPCSearches = maxPts;
                    }
                    else
                    {
                        _logger.LogWarning("Cannot parse {stat}", nameof(MSAccountStats.MaxPointsPCSearches));
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error: {e}", e.Message);
                res = false;
            }

            if (data.Account.Stats.CurrentAccountLevel < 2)
            {
                try
                {
                    string accLevelRatioPts = await data.Page.Locator(BrowserConstants.SELECTOR_ACCOUNT_LEVEL_POINTS).InnerTextAsync();
                    accLevelRatioPts = accLevelRatioPts.Trim();
                    string[] split = accLevelRatioPts.Split('/');
                    string nLevPts = split[0].Trim();

                    if (int.TryParse(nLevPts, out int accountPtsLevel))
                    {
                        _logger.LogDebug("Found value for {stat}: {val}", nameof(MSAccountStats.CurrentAccountLevelPoints), accountPtsLevel);
                        data.Account.Stats.CurrentAccountLevelPoints = accountPtsLevel;
                    }
                    else
                    {
                        _logger.LogWarning("Cannot parse {stat}", nameof(MSAccountStats.CurrentAccountLevelPoints));
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("Error: {e}", e.Message);
                    res = false;
                }
            }
            else
            {
                data.Account.Stats.CurrentAccountLevelPoints = 500; // Reached max level points

                try
                {
                    string mobilePoints = await data.Page.Locator(BrowserConstants.SELECTOR_BREAKDOWN_MOBILE_POINTS).InnerTextAsync();
                    mobilePoints = mobilePoints.Trim();
                    string[] sub = mobilePoints.Split('/');

                    if (sub.Length == 2)
                    {
                        if (int.TryParse(sub[0], out int currentPts))
                        {
                            _logger.LogDebug("Found value for {stat}: {val}", nameof(MSAccountStats.CurrentPointsMobileSearches), currentPts);
                            data.Account.Stats.CurrentPointsMobileSearches = currentPts;
                        }
                        else
                        {
                            _logger.LogWarning("Cannot parse {stat}", nameof(MSAccountStats.CurrentPointsMobileSearches));
                        }

                        if (int.TryParse(sub[1], out int maxPts))
                        {
                            _logger.LogDebug("Found value for {stat}: {val}", nameof(MSAccountStats.MaxPointsMobileSearches), maxPts);
                            data.Account.Stats.MaxPointsMobileSearches = maxPts;
                        }
                        else
                        {
                            _logger.LogWarning("Cannot parse {stat}", nameof(MSAccountStats.MaxPointsMobileSearches));
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("Error: {e}", e.Message);
                    res = false;
                }
            }

            return res;
        }

        private async Task<bool> GetAccTotalPoints(MSAccountServerData data)
        {
            try
            {
                if (data.Page.Url != BrowserConstants.URL_DASHBOARD && data.Page.Url != BrowserConstants.URL_DASHBOARD_PTS_BREAKDOWN)
                {
                    await NavigateToURL(data, BrowserConstants.URL_DASHBOARD);
                    await WaitRandomMs(2000, 3000); // Wait for the animation to finish
                }

                ILocator locTotPts = data.Page.Locator(BrowserConstants.SELECTOR_ACCOUNT_TOTAL_POINTS);
                await locTotPts.WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Visible });

                string totPts = await locTotPts.InnerTextAsync();
                totPts = totPts.Trim().Replace(",", "");

                if (int.TryParse(totPts, out int totalPts))
                {
                    _logger.LogDebug("Found value for {stat}: {val}", nameof(MSAccountStats.TotalAccountPoints), totalPts);
                    data.Account.Stats.TotalAccountPoints = totalPts;
                }
                else
                {
                    _logger.LogWarning("Cannot parse {stat}", nameof(MSAccountStats.TotalAccountPoints));
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("Error: {e}", e.Message);
                return false;
            }
        }
    }
}
