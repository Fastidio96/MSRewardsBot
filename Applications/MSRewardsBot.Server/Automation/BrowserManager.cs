using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Server.Core;
using MSRewardsBot.Server.DataEntities;

namespace MSRewardsBot.Server.Automation
{
    public partial class BrowserManager : IDisposable
    {
        private readonly ILogger<BrowserManager> _logger;
        private readonly RealTimeData _rt;

        private IPlaywright _playwright;
        private IBrowser _browser;

        private DateTime _lastUsed;
        private Thread _idleCheckThread;

        private bool _isDisposing = false;

        public BrowserManager(ILogger<BrowserManager> logger, RealTimeData rt)
        {
            _logger = logger;
            _rt = rt;
        }

        public async void Init()
        {
            _logger.Log(LogLevel.Information, "Checking and installing browser dependencies..");

            int exitCode = Microsoft.Playwright.Program.Main(["install"]);
            if (exitCode == 0)
            {
                _logger.Log(LogLevel.Information, "Dependencies installed successfully");
            }
            else
            {
                _logger.LogCritical("Cannot install dependencies");
            }

            await CreateBrowser();

            _lastUsed = DateTime.Now;

            _idleCheckThread = new Thread(IdleCheckLoop);
            _idleCheckThread.Name = nameof(IdleCheckLoop);
            _idleCheckThread.Start();

            _logger.Log(LogLevel.Information, "BrowserManager init completed");
        }

        private async Task CreateBrowser()
        {
            if (_playwright == null)
            {
                _playwright = await Playwright.CreateAsync();
            }

            if (_browser == null)
            {
                if (Settings.UseFirefox)
                {
                    _browser = await _playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions()
                    {
#if DEBUG
                        //Headless = false,
#endif
                        Args =
                        [
                            "--disable-infobars",
                            "--no-default-browser-check",
                            "--disable-extensions"
                        ],
                        FirefoxUserPrefs = new Dictionary<string, object>()
                        {
                            ["network.http.http3.enabled"] = false,
                        }
                    });
                }
                else
                {
                    _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions()
                    {
#if DEBUG
                        //Headless = false,
#endif
                        Args =
                        [
                            "--disable-blink-features=AutomationControlled",
                            "--disable-infobars",
                            "--no-default-browser-check",
                            "--disable-extensions"
                        ]
                    });
                }
            }
        }

        private async Task CloseBrowser()
        {
            _logger.LogDebug("Deleting context references..");
            foreach (KeyValuePair<int, MSAccountServerData> data in _rt.CacheMSAccStats) // Delete refs before disposing
            {
                await DeleteContext(data.Value);
            }

            if (_browser != null)
            {
                await _browser.CloseAsync();
                await _browser.DisposeAsync();
                _browser = null;

                _logger.LogDebug("Browser disposed");
            }

            _playwright?.Dispose();
            _playwright = null;

            _logger.LogDebug("Playwright disposed");
        }

        private async void IdleCheckLoop()
        {
            _logger.LogDebug("Idle check thread started");

            while (!_isDisposing)
            {
                if (_browser != null && DateTime.Now - _lastUsed > new TimeSpan(0, 0, Settings.MaxSecsWaitBetweenSearches + 60))
                {
                    _logger.LogInformation("Browser idle timeout reached, closing...");
                    await CloseBrowser();
                }

                Thread.Sleep(1000);
            }
        }

        public async Task<bool> CreateContext(MSAccountServerData data, bool isMobile)
        {
            _logger.LogDebug("Creating new context for {Data} | {User}", data.Account.Email, data.Account.User.Username);

            _lastUsed = DateTime.Now;
            try
            {
                await CreateBrowser();

                if (Settings.UseFirefox)
                {
                    await CreateFirefoxStealthContext(data, isMobile);
                }
                else
                {
                    await CreateChromeStealthContext(data, isMobile);
                }

                data.Page = await data.Context.NewPageAsync();

                if (!await StartLoggedSession(data))
                {
                    _logger.LogWarning("Cannot install cookies for {Email} | {User}", data.Account.Email, data.Account.User.Username);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Critical, ex, "Error on CreateContext");

                await DeleteContext(data);

                if (_browser == null || !_browser.IsConnected)
                {
                    await CloseBrowser();
                }

                return false;
            }

            _logger.LogDebug("Cookies installed for {Email} | {User}", data.Account.Email, data.Account.User.Username);
            return true;
        }

        public async Task DeleteContext(MSAccountServerData data)
        {
            _logger.LogDebug("Deleting context for {Email} | {User}", data.Account.Email, data.Account.User.Username);

            if (data.Page != null)
            {
                await data.Context.DisposeAsync();
                data.Page = null;
            }

            if (data.Context != null)
            {
                await data.Context.CloseAsync();
                data.Context = null;
            }
        }

        private async Task<bool> StartLoggedSession(MSAccountServerData data)
        {
            if (data.Account.Cookies == null || data.Account.Cookies.Count == 0)
            {
                _logger.LogError("Cannot proceed. No cookies for account {Email} | {User} found.",
                    data.Account.Email, data.Account.User.Username);
                return false;
            }

            await data.Context.AddCookiesAsync(ConvertToPWCookies(data.Account.Cookies));

            if (!await CheckIsLogged(data))
            {
                _logger.LogError("Cannot proceed. Expired cookies for {Email} | {User}.",
                    data.Account.Email, data.Account.User.Username);
                return false;
            }

            return true;
        }

        private async Task<bool> CheckIsLogged(MSAccountServerData data)
        {
            await NavigateToURL(data, BrowserConstants.URL_DASHBOARD);

            if (data.Page.Url.StartsWith(BrowserConstants.URL_EXPIRED_COOKIES))
            {
                data.Account.IsCookiesExpired = true;
                return false;
            }

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
                if (data.Page.Url != url)
                {
                    IResponse response = await data.Page.GotoAsync(url, new PageGotoOptions()
                    {
                        WaitUntil = WaitUntilState.Load,
                        Timeout = 15000
                    });
                    if (url != BrowserConstants.URL_BLANK_PAGE)
                    {
                        if (response == null || !response.Ok)
                        {
                            _logger.LogWarning("Failed to navigate to {url}", url);
                            return false;
                        }
                    }

                    await Task.Delay(GetRandomMsTimes(3500, 5000));
                }

                await data.Page.BringToFrontAsync();

                _logger.LogDebug("Navigated to {url} for {email} | {user}",
                    url, data.Account.Email, data.Account.User.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while navigating to {url}: {Message}", url, ex.Message);
                return false;
            }
        }

        private void LogDebugAction(string actionName)
        {
            //_logger.LogTrace("Logged action {time} {action} ", DateTime.Now.ToString("mm:ss:fff"), actionName.ToUpper());
        }
        private TimeSpan GetRandomMsTimes(int min, int max)
        {
            return new TimeSpan(0, 0, 0, 0, Random.Shared.Next(min, max));
        }

        private async Task<bool> WriteAsHuman(IPage page, string keyword, string selectorSearchbar)
        {
            //Wait for the animation to finish
            await Task.Delay(GetRandomMsTimes(1000, 1600));

            try
            {
                char[] split = keyword.ToCharArray();
                foreach (char cr in split)
                {
                    string js = selectorSearchbar.Replace("{keyword}", cr.ToString());
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
            _isDisposing = true;

            try
            {
                foreach (IBrowserContext ctx in _browser.Contexts)
                {
                    await ctx.CloseAsync();
                    await ctx.DisposeAsync();
                }

                await _browser.CloseAsync();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error while disposing browser");
            }

            if (_idleCheckThread != null)
            {
                if (_idleCheckThread.IsAlive)
                {
                    _idleCheckThread.Join(5000);
                }

                _idleCheckThread = null;
            }
        }
    }
}
