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
                LogTraceAction("MOBILE_CLICK_SEARCHBAR_TEXTAREA");
                await Task.Delay(GetRandomMsTimes(BrowserConstants.HUMAN_CLICK_BTN_MIN, BrowserConstants.HUMAN_CLICK_BTN_MAX));
                await data.Page.EvaluateAsync(BrowserConstants.MOBILE_CLICK_SEARCHBAR_TEXTAREA);

                LogTraceAction("WRITE_KEYWORD_HOMEPAGE_TEXTAREA");
                if (!await WriteAsHuman(data.Page, keyword, BrowserConstants.MOBILE_APPEND_KEYWORD_SEARCHBAR_TEXTAREA))
                {
                    return false;
                }

                LogTraceAction("SUBMIT WITH ENTER SEARCHBAR_TEXTAREA");
                await data.Page.Keyboard.PressAsync("Enter", new KeyboardPressOptions()
                {
                    Delay = Random.Shared.Next(BrowserConstants.HUMAN_CLICK_BTN_MIN, BrowserConstants.HUMAN_CLICK_BTN_MAX)
                });

                LogTraceAction("SUBMIT SENT SUCCESSFULLY");
                if (!data.Page.Url.StartsWith(BrowserConstants.URL_SEARCHES))
                {
                    _logger.LogError("Submit not working");
                    return false;
                }

                LogTraceAction("MOBILE_CLICK_YES_GDPR_BTN");
                await Task.Delay(GetRandomMsTimes(BrowserConstants.HUMAN_CLICK_BTN_MIN, BrowserConstants.HUMAN_CLICK_BTN_MAX));
                await data.Page.EvaluateAsync(BrowserConstants.MOBILE_CLICK_YES_GDPR_BTN);

                LogTraceAction("Viewing page content & scrolling");
                for (int i = 0; i < 5; i++)
                {
                    await data.Page.Mouse.WheelAsync(0, Random.Shared.Next(500, 900));
                    await Task.Delay(Random.Shared.Next(500, 1000));
                }
                await Task.Delay(Random.Shared.Next(1000, 3000));

                LogTraceAction("Resetting start page for next request");
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
