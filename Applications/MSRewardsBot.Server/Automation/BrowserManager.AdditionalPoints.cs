using System;
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

                // Force lazy-loaded cards (headless browser does not auto-trigger IntersectionObserver below the fold)
                await ScrollUntilLocatorStable(data.Page, BrowserConstants.ADDITIONAL_PTS_IMAGE_LOCATOR);

                ILocator allIcons = data.Page.Locator(BrowserConstants.ADDITIONAL_PTS_IMAGE_LOCATOR);
                int initialCount = await allIcons.CountAsync();
                if (initialCount == 0)
                {
                    _logger.LogInformation("No additional points found from the dashboard for {Email} | {User}",
                        data.Account.Email, data.Account.User.Username);

                    return true;
                }

                _logger.LogDebug("Found {n} additional point icons", initialCount);

                async Task<bool> ProcessIconAsync(ILocator loc)
                {
                    try
                    {
                        await loc.ScrollIntoViewIfNeededAsync(new LocatorScrollIntoViewIfNeededOptions { Timeout = 5000 });
                        await WaitRandomMs(1200, 2000);

                        if (!await loc.IsVisibleAsync())
                        {
                            return false;
                        }

                        IPage newPage = await data.Page.Context.RunAndWaitForPageAsync(async () =>
                        {
                            await loc.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
                        });

                        await WaitRandomMs(1500, 5000);
                        await HumanScroll(newPage);
                        await WaitRandomMs(3000, 4500);
                        await newPage.CloseAsync();
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }

                try
                {
                    // Iterate by always targeting the next still-present icon. After a card is completed,
                    // the dashboard replaces .mee-icon-AddMedium with the checkmark, so count decreases
                    // and Nth(skipIndex) points to the next uncompleted card. If processing fails without
                    // removing the icon (stuck card), advance skipIndex to skip past it.
                    int skipIndex = 0;
                    int safetyIterations = 0;
                    const int maxIterations = 30;

                    while (safetyIterations++ < maxIterations)
                    {
                        int currentCount = await allIcons.CountAsync();
                        if (skipIndex >= currentCount)
                        {
                            break;
                        }

                        bool processed = await ProcessIconAsync(allIcons.Nth(skipIndex));

                        int countAfter = await allIcons.CountAsync();
                        if (!processed || countAfter >= currentCount)
                        {
                            // card did not disappear from the dashboard, skip it to avoid an infinite loop
                            skipIndex++;
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

                if (!await GetAccTotalPoints(data))
                {
                    _logger.LogWarning("Cannot get total points from account for {Email} | {User}",
                        data.Account.Email, data.Account.User.Username);
                    return false;
                }

                int gainedPts = data.Stats.TotalAccountPoints - previousPoints;
                if (gainedPts > 0)
                {
                    _logger.LogInformation("Gained {pts} points from the dashboard for {Email} | {User}",
                    gainedPts, data.Account.Email, data.Account.User.Username);
                }
                else
                {
                    _logger.LogInformation("No additional points found from the dashboard for {Email} | {User}",
                        data.Account.Email, data.Account.User.Username);
                }

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
