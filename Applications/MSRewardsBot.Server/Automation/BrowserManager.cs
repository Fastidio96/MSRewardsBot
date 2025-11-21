using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Common.DataEntities.Stats;
using MSRewardsBot.Server.DataEntities;

namespace MSRewardsBot.Server.Automation
{
    public class BrowserManager : IDisposable
    {
        private readonly ILogger<BrowserManager> _logger;
        private IPlaywright _playwright;
        private IBrowser _browser;

        public BrowserManager(ILogger<BrowserManager> logger)
        {
            _logger = logger;
        }

        public async void Init()
        {
            _logger.Log(LogLevel.Information, "Checking and installing browser dependencies..");

            Microsoft.Playwright.Program.Main(["install"]);

            _logger.Log(LogLevel.Information, "Dependencies installed successfully");

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync
            (
#if DEBUG
            //new BrowserTypeLaunchOptions()
            //{
            //    Headless = false
            //}
#endif
            );

            _logger.Log(LogLevel.Information, "BrowserManager init completed");
        }

        public async Task<bool> CreateContext(MSAccountServerData data)
        {
            _logger.LogDebug("Creating new context for {Data} | {User}", data.Account.Email, data.Account.User.Username);

            data.Context = await _browser.NewContextAsync(new BrowserNewContextOptions()
            {
                UserAgent = BrowserConstants.UA_EDGE
            });

            data.Page = await data.Context.NewPageAsync();

            if (!await StartLoggedSession(data))
            {
                _logger.LogWarning("Cannot install cookies for {Data} | {User}", data.Account.Email, data.Account.User.Username);

            }

            _logger.LogDebug("Cookies installed for {Data} | {User}", data.Account.Email, data.Account.User.Username);
            return true;
        }

        private async Task<bool> StartLoggedSession(MSAccountServerData data)
        {
            if (data.Account.Cookies == null || data.Account.Cookies.Count == 0)
            {
                _logger.LogError("Cannot proceed. No cookies for account {Data} | {User} found.",
                    data.Account.Email, data.Account.User.Username);
                return false;
            }

            await data.Context.AddCookiesAsync(ConvertToPWCookies(data.Account.Cookies));
            return true;
        }

        public async Task CloseLoggedSession(IBrowserContext context)
        {
            try
            {
                await context.ClearCookiesAsync();
                await context.CloseAsync();

                _logger.LogDebug("Context closed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error {e}", ex.Message);
            }
        }

        private async Task<bool> NavigateToURL(MSAccountServerData data, string url)
        {
            if (data.Page == null)
            {
                return false;
            }

            try
            {
                await data.Page.GotoAsync(url);
                await data.Page.WaitForURLAsync(url, new PageWaitForURLOptions()
                {
                    WaitUntil = WaitUntilState.Load,
                    Timeout = 15000
                });

                _logger.LogDebug("Navigated to {url}", url);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error on {MethodName}: {Message}", nameof(NavigateToURL), ex.Message);
                return false;
            }
        }

        private async Task<bool> NavigateToURLWithoutCheck(MSAccountServerData data, string url)
        {
            if (data.Page == null)
            {
                return false;
            }

            try
            {
                Random rnd = new Random();
                int secs = rnd.Next(5, 30);
                IResponse response = await data.Page.GotoAsync(url, new PageGotoOptions()
                {
                    WaitUntil = WaitUntilState.Load,
                    Timeout = 15000
                }).WaitAsync(new TimeSpan(0, 0, secs));
                if (response == null || !response.Ok)
                {
                    _logger.LogWarning("Failed to Navigate to {url} - response is {res}", url, response?.StatusText);
                    return false;
                }

                _logger.LogDebug("Navigated to {url} with a waiting of {s} seconds for {usr} | {email}",
                    url, secs, data.Account.User.Username, data.Account.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error on {MethodName}: {Message}", nameof(NavigateToURL), ex.Message);
                return false;
            }
        }

        public async Task<bool> DashboardUpdate(MSAccountServerData data)
        {
            _logger.LogInformation("Dashboard update started for {User} | {Data}", data.Account.User.Username, data.Account.Email);

            if (!await StartLoggedSession(data))
            {
                return false;
            }

            if (!await NavigateToURL(data, BrowserConstants.URL_DASHBOARD_PTS_BREAKDOWN))
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
                Random rnd = new Random();

                _logger.LogDebug("Logged action {time} {action} ", DateTime.Now.ToString("HH:mm:ss:fff"), "CLICK_BING_HOMEPAGE_LOGIN_BTN");
                await Task.Delay(new TimeSpan(0, 0, rnd.Next(5, 10)));
                await data.Page.EvaluateAsync(BrowserConstants.CLICK_BING_HOMEPAGE_LOGIN_BTN);

                _logger.LogDebug("Logged action {time} {action} ", DateTime.Now.ToString("HH:mm:ss:fff"), "PAGE RELOAD");
                await Task.Delay(new TimeSpan(0, 0, rnd.Next(3, 5)));
                await data.Page.ReloadAsync(new PageReloadOptions()
                {
                    Timeout = 15000,
                    WaitUntil = WaitUntilState.Load
                });

                _logger.LogDebug("Logged action {time} {action} ", DateTime.Now.ToString("HH:mm:ss:fff"), "CLICK_YES_GDPR_BTN");
                await Task.Delay(new TimeSpan(0, 0, rnd.Next(2, 5)));
                await data.Page.EvaluateAsync(BrowserConstants.CLICK_YES_GDPR_BTN);

                _logger.LogDebug("Logged action {time} {action} ", DateTime.Now.ToString("HH:mm:ss:fff"), "After CLICK_BING_HOMEPAGE_LOGIN_BTN");
                await Task.Delay(new TimeSpan(0, 0, rnd.Next(5, 10)));

                _logger.LogDebug("Logged action {time} {action} ", DateTime.Now.ToString("HH:mm:ss:fff"), "CLICK_SEARCHBAR_TEXTAREA");
                await Task.Delay(new TimeSpan(0, 0, rnd.Next(1, 3)));
                await data.Page.EvaluateAsync(BrowserConstants.CLICK_SEARCHBAR_TEXTAREA);

                _logger.LogDebug("Logged action {time} {action} ", DateTime.Now.ToString("HH:mm:ss:fff"), "WRITE_KEYWORD_HOMEPAGE_TEXTAREA");
                string js = BrowserConstants.WRITE_KEYWORD_SEARCHBAR_TEXTAREA.Replace("{keyword}", keyword);
                await data.Page.EvaluateAsync(js);

                _logger.LogDebug("Logged action {time} {action} ", DateTime.Now.ToString("HH:mm:ss:fff"), "CLICK_SUBMIT_SEARCHBAR_TEXTAREA");
                await Task.Delay(new TimeSpan(0, 0, rnd.Next(1, 5)));
                await data.Page.EvaluateAsync(BrowserConstants.CLICK_SUBMIT_SEARCHBAR_TEXTAREA);

                await Task.Delay(new TimeSpan(0, 0, rnd.Next(5, 10)));

                _logger.LogDebug("Logged action {time} {action} ", DateTime.Now.ToString("HH:mm:ss:fff"), "After Enter press");

                await NavigateToURL(data, "about:blank");
                _logger.LogDebug("Resetting start page for next request");

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("Error on URL {url}: {e}", data.Page.Url, e.Message);
                return false;
            }
        }

        private List<Cookie> ConvertToPWCookies(IEnumerable<AccountCookie> cookies)
        {
            List<Cookie> result = new List<Cookie>();
            foreach (var c in cookies)
            {
                Cookie cookie = new Cookie()
                {
                    Domain = c.Domain,
                    Path = c.Path,
                    Value = c.Value,
                    Secure = c.Secure,
                    HttpOnly = c.HttpOnly,
                    Name = c.Name
                };

                if (c.Expires.HasValue && (c.Expires > DateTime.UtcNow))
                {
                    cookie.Expires = (float)((DateTimeOffset)c.Expires.Value).ToUnixTimeSeconds();
                }

                if (Enum.TryParse(typeof(SameSiteAttribute), c.SameSite, out object attr))
                {
                    cookie.SameSite = (SameSiteAttribute)attr;
                }

                result.Add(cookie);
            }

            return result;
        }

        public async void Dispose()
        {
            try
            {
                foreach (IBrowserContext ctx in _browser.Contexts)
                {
                    await ctx.CloseAsync();
                    await ctx.DisposeAsync();
                }

                await _browser.CloseAsync();
            }
            catch
            {
            }
        }
    }
}
