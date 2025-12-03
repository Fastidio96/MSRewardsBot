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

                await Task.Delay(GetRandomMsTimes(BrowserConstants.HUMAN_CLICK_BTN_MIN, BrowserConstants.HUMAN_CLICK_BTN_MAX));
                ILocator elements = data.Page.Locator(".mee-icon-AddMedium");

                await Task.Delay(GetRandomMsTimes(2500, 5000));
                int count = await elements.CountAsync();

                if (count == 0)
                {
                    _logger.LogInformation("No additional points found from the dashboard for {Email} | {User}",
                        data.Account.Email, data.Account.User.Username);

                    return true;
                }

                for (int i = count - 1; i >= 0; i--)
                {
                    await data.Page.BringToFrontAsync();
                    await Task.Delay(GetRandomMsTimes(2500, 5000));

                    try
                    {
                        IPage newPage = await data.Page.Context.RunAndWaitForPageAsync(async () =>
                        {
                            await data.Page.EvaluateAsync($"document.querySelectorAll('.mee-icon-AddMedium')[{i}].click();");

                            await Task.Delay(GetRandomMsTimes(5000, 7000));
                        });

                        await newPage?.CloseAsync();
                    }
                    catch
                    {
                        continue;
                    }
                }

                await Task.Delay(GetRandomMsTimes(2500, 5000));

                string totPts = await data.Page.EvaluateAsync<string>(BrowserConstants.SELECTOR_ACCOUNT_TOTAL_POINTS);
                totPts = totPts.Trim();

                if (!int.TryParse(totPts, out int totalPts))
                {
                    _logger.LogError("Cannot get total points from account for {Email} | {User}",
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
