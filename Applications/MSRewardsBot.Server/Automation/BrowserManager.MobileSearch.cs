using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using MSRewardsBot.Server.DataEntities;

namespace MSRewardsBot.Server.Automation
{
    public partial class BrowserManager
    {
        public async Task<bool> MobileSearch(MSAccountServerData data, string keyword)
        {
            _logger.LogDebug("Mobile search started for {User} | {Data}",
                data.Account.Email, data.Account.User.Username);

            if (!await NavigateToURL(data, BrowserConstants.URL_SEARCHES_HOMEPAGE))
            {
                return false;
            }

            try
            {
                if (!await WriteSearchAsHuman(data.Page, keyword))
                {
                    _logger.LogError("Auto typing failed");
                    return false;
                }

                await data.Page.Keyboard.PressAsync("Enter", new KeyboardPressOptions()
                {
                    Delay = Random.Shared.Next(BrowserConstants.HUMAN_ACTION_MIN, BrowserConstants.HUMAN_ACTION_MAX)
                });

                if (!data.Page.Url.StartsWith(BrowserConstants.URL_SEARCHES))
                {
                    _logger.LogError("Submit not working");
                    return false;
                }

                if (await data.Page.Locator(BrowserConstants.BTN_YES_GDPR).IsVisibleAsync())
                {
                    await WaitRandomMs(BrowserConstants.HUMAN_ACTION_MIN, BrowserConstants.HUMAN_ACTION_MAX);
                    await data.Page.Locator(BrowserConstants.BTN_YES_GDPR).ClickAsync();
                }

                await HumanScroll(data.Page);

                await WaitRandomMs(1000, 3000);

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("Error on URL {url}: {e}", data.Page.Url, e.Message);
                return false;
            }
        }
    }
}
