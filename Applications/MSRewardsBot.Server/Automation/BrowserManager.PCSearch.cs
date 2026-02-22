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
                await data.Page.Locator(BrowserConstants.PC_CLICK_BING_HOMEPAGE_LOGIN_BTN).ClickAsync();

                await Task.Delay(new TimeSpan(0, 0, Random.Shared.Next(3, 5)));
                await data.Page.ReloadAsync(new PageReloadOptions()
                {
                    Timeout = 15000,
                    WaitUntil = WaitUntilState.Load
                });

                await WaitRandomMs(BrowserConstants.HUMAN_ACTION_MIN, BrowserConstants.HUMAN_ACTION_MAX);
                await data.Page.Locator(BrowserConstants.BTN_YES_GDPR).ClickAsync();

                if (!await WriteSearchAsHuman(data.Page, keyword))
                {
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

                await NavigateToURL(data, BrowserConstants.URL_BLANK_PAGE);

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
