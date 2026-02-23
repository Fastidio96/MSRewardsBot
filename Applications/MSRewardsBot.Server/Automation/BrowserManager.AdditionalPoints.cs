using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using MSRewardsBot.Server.DataEntities;

namespace MSRewardsBot.Server.Automation
{
    public partial class BrowserManager
    {
        /// <summary>
        /// Get additional points from the dashboard by clicking on the cards
        /// </summary>
        public async Task<bool> GetAdditionalPoints(MSAccountServerData data)
        {
            _logger.LogInformation("Getting additional points for {Email} | {User}",
               data.Account.Email, data.Account.User.Username);

            if (!await NavigateToURL(data, BrowserConstants.URL_DASHBOARD))
            {
                return false;
            }

            try
            {
                int previousPoints = data.Stats.TotalAccountPoints;

                await WaitRandomMs(BrowserConstants.HUMAN_ACTION_MIN, BrowserConstants.HUMAN_ACTION_MAX);

                int count = await data.Page.Locator(BrowserConstants.ADDITIONAL_PTS_IMAGE_LOCATOR).CountAsync();
                if (count == 0)
                {
                    _logger.LogInformation("No additional points found from the dashboard for {Email} | {User}",
                        data.Account.Email, data.Account.User.Username);

                    return true;
                }

                try
                {
                    IReadOnlyList<ILocator> locators = await data.Page.Locator(BrowserConstants.ADDITIONAL_PTS_IMAGE_LOCATOR).AllAsync();
                    foreach (ILocator loc in locators)
                    {
                        if (!await loc.IsVisibleAsync())
                        {
                            continue;
                        }

                        await loc.ScrollIntoViewIfNeededAsync();
                        await WaitRandomMs(1200, 2000);

                        try
                        {
                            IPage newPage = await data.Page.Context.RunAndWaitForPageAsync(async () =>
                            {
                                await loc.ClickAsync();
                            });

                            await WaitRandomMs(1500, 5000);
                            await HumanScroll(newPage);
                            await WaitRandomMs(3000, 4500);
                            await newPage.CloseAsync();
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error while getting additional points. {err}", ex.Message);
                    return false;
                }

                await WaitRandomMs(2500, 5000);

                ILocator claimPts = data.Page.Locator(BrowserConstants.ADDITIONAL_PTS_CLAIM_PTS);
                if (await claimPts.IsVisibleAsync())
                {
                    await claimPts.ScrollIntoViewIfNeededAsync();
                    await claimPts.ClickAsync();
                }

                await WaitRandomMs(3000, 7000);

                string totPts = await data.Page.Locator(BrowserConstants.SELECTOR_ACCOUNT_TOTAL_POINTS).InnerTextAsync();
                totPts = totPts.Trim();

                if (!int.TryParse(totPts, out int totalPts))
                {
                    _logger.LogWarning("Cannot get total points from account for {Email} | {User}",
                        data.Account.Email, data.Account.User.Username);
                    return false;
                }

                data.Account.Stats.TotalAccountPoints = totalPts;
                int gainedPts = data.Stats.TotalAccountPoints - previousPoints;

                _logger.LogInformation("Gained {pts} points from the dashboard for {Email} | {User}",
                    gainedPts, data.Account.Email, data.Account.User.Username);

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("Getting additional error: {e}", e.Message);
                return false;
            }
        }
    }
}
