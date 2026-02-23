using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using MSRewardsBot.Server.DataEntities;

namespace MSRewardsBot.Server.Automation
{
    public partial class BrowserManager
    {
        public async Task<bool> PCSearch(MSAccountServerData data, string keyword)
        {
            _logger.LogDebug("PC search started for {User} | {Data}",
                data.Account.Email, data.Account.User.Username);

            if (!await NavigateToURL(data, BrowserConstants.URL_SEARCHES_HOMEPAGE))
            {
                return false;
            }

            try
            {
                await WaitRandomMs(BrowserConstants.HUMAN_ACTION_MIN + 2000, BrowserConstants.HUMAN_ACTION_MAX + 5000);

                if (await data.Page.Locator(BrowserConstants.PC_CLICK_BING_HOMEPAGE_LOGIN_BTN).IsVisibleAsync())
                {
                    await data.Page.Locator(BrowserConstants.PC_CLICK_BING_HOMEPAGE_LOGIN_BTN).ClickAsync();
                }
                else
                {
                    _logger.LogWarning("PC_CLICK_BING_HOMEPAGE_LOGIN_BTN not found!");
                }

                await WaitRandomMs(3000, 5000);
                await data.Page.ReloadAsync(new PageReloadOptions()
                {
                    Timeout = 15000,
                    WaitUntil = WaitUntilState.Load
                });

                if (await data.Page.Locator(BrowserConstants.BTN_YES_GDPR).IsVisibleAsync())
                {
                    await WaitRandomMs(BrowserConstants.HUMAN_ACTION_MIN, BrowserConstants.HUMAN_ACTION_MAX);
                    await data.Page.Locator(BrowserConstants.BTN_YES_GDPR).ClickAsync();
                }

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
