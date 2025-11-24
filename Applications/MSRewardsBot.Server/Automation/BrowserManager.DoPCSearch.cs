using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using MSRewardsBot.Server.DataEntities;

namespace MSRewardsBot.Server.Automation
{
    public partial class BrowserManager
    {
        public async Task<bool> DoPCSearch(MSAccountServerData data, string keyword)
        {
            _logger.LogDebug("PC searches started for {User} | {Data}",
                data.Account.Email, data.Account.User.Username);

            if (!await NavigateToURLWithoutCheck(data, BrowserConstants.URL_SEARCHES_HOMEPAGE))
            {
                return false;
            }

            try
            {
                LogDebugAction("CLICK_BING_HOMEPAGE_LOGIN_BTN");
                await Task.Delay(GetRandomMsTimes(BrowserConstants.HUMAN_CLICK_BTN_MIN + 2000, BrowserConstants.HUMAN_CLICK_BTN_MAX + 5000));
                await data.Page.EvaluateAsync(BrowserConstants.CLICK_BING_HOMEPAGE_LOGIN_BTN);

                LogDebugAction("PAGE RELOAD");
                await Task.Delay(new TimeSpan(0, 0, Random.Shared.Next(3, 5)));
                await data.Page.ReloadAsync(new PageReloadOptions()
                {
                    Timeout = 15000,
                    WaitUntil = WaitUntilState.Load
                });

                LogDebugAction("CLICK_YES_GDPR_BTN");
                await Task.Delay(GetRandomMsTimes(BrowserConstants.HUMAN_CLICK_BTN_MIN, BrowserConstants.HUMAN_CLICK_BTN_MAX));
                await data.Page.EvaluateAsync(BrowserConstants.CLICK_YES_GDPR_BTN);

                LogDebugAction("CLICK_SEARCHBAR_TEXTAREA");
                await Task.Delay(GetRandomMsTimes(BrowserConstants.HUMAN_CLICK_BTN_MIN, BrowserConstants.HUMAN_CLICK_BTN_MAX));
                await data.Page.EvaluateAsync(BrowserConstants.CLICK_SEARCHBAR_TEXTAREA);

                LogDebugAction("WRITE_KEYWORD_HOMEPAGE_TEXTAREA");
                if (!await WriteAsHuman(data.Page, keyword))
                {
                    return false;
                }

                LogDebugAction("SUBMIT WITH ENTER SEARCHBAR_TEXTAREA");
                await data.Page.Keyboard.PressAsync("Enter", new KeyboardPressOptions()
                {
                    Delay = Random.Shared.Next(BrowserConstants.HUMAN_CLICK_BTN_MIN, BrowserConstants.HUMAN_CLICK_BTN_MAX)
                });

                LogDebugAction("SUBMIT SENT SUCCESSFULLY");
                if (!data.Page.Url.StartsWith(BrowserConstants.URL_SEARCHES))
                {
                    _logger.LogError("Submit not working");
                    return false;
                }

                LogDebugAction("Viewing page content & scrolling");
                for (int i = 0; i < 5; i++)
                {
                    await data.Page.Mouse.WheelAsync(0, Random.Shared.Next(200, 700));
                    await Task.Delay(Random.Shared.Next(200, 500));
                }

                LogDebugAction("Resetting start page for next request");
                await NavigateToURL(data, "about:blank");

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("Error on URL {url}: {e}", data.Page.Url, e.Message);
                return false;
            }
        }

        private async Task<bool> WriteAsHuman(IPage page, string keyword)
        {
            try
            {
                char[] split = keyword.ToCharArray();
                foreach (char cr in split)
                {
                    string js = BrowserConstants.APPEND_KEYWORD_SEARCHBAR_TEXTAREA.Replace("{keyword}", cr.ToString());
                    await page.EvaluateAsync(js);

                    await Task.Delay(GetRandomMsTimes(BrowserConstants.HUMAN_WRITING_MIN, BrowserConstants.HUMAN_WRITING_MAX));
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error on writing keyword {keyword}: {e}", keyword, ex.Message);
                return false;
            }
        }
    }
}
