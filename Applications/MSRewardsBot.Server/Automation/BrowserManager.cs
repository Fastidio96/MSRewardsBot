using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Common.Utilities;
using MSRewardsBot.Server.Core;
using MSRewardsBot.Server.DataEntities;

namespace MSRewardsBot.Server.Automation
{
    public partial class BrowserManager : IAsyncDisposable
    {
        private readonly ILogger<BrowserManager> _logger;
        private readonly IOptions<Settings> _settings;
        private readonly RealTimeData _rt;

        private IPlaywright _playwright;
        private IBrowser _browser;

        private DateTime _lastUsed;
        private Task _idleCheckTask;
        private CancellationTokenSource _idleCheckCts;

        private readonly SemaphoreSlim _browserLock = new SemaphoreSlim(1, 1);

        public BrowserManager(ILogger<BrowserManager> logger, IOptions<Settings> settings, RealTimeData rt)
        {
            _logger = logger;
            _settings = settings;
            _rt = rt;
        }

        public async Task Init()
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
                throw new Exception("Cannot install PW dependecies!");
            }

            await CreateBrowser();

            _lastUsed = DateTime.Now;

            _idleCheckCts = new CancellationTokenSource();
            _idleCheckTask = Task.Run(() => IdleCheckLoopAsync(_idleCheckCts.Token));

            _logger.Log(LogLevel.Information, "BrowserManager init completed");
        }

        private async Task CreateBrowser()
        {
            await _browserLock.WaitAsync();

            try
            {
                if (_playwright == null)
                {
                    _playwright = await Playwright.CreateAsync();
                }

                if (_browser == null)
                {
                    if (_settings.Value.UseFirefox)
                    {
                        Dictionary<string, object> args = new Dictionary<string, object>()
                        {
                            ["network.http.http3.enabled"] = false,
                            ["security.webauth.webauthn"] = false,
                            ["media.autoplay.default"] = 0,
                            ["media.autoplay.blocking_policy"] = 0,
                            ["browser.shell.checkDefaultBrowser"] = false,
                            ["startup.homepage_welcome_url"] = BrowserConstants.URL_BLANK_PAGE,
                            ["startup.homepage_welcome_url.additional"] = "",
                            ["browser.startup.firstrunSkipsHomepage"] = false,
                            ["extensions.autoDisableScopes"] = 15,
                            ["extensions.systemAddon.update.enabled"] = false
                        };

                        if (RuntimeEnvironment.IsDocker())
                        {
                            args.Add("layers.gpu-process.enabled", false);
                        }

                        _browser = await _playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions()
                        {
#if DEBUG
                            //Headless = false,
#endif
                            FirefoxUserPrefs = args
                        });
                    }
                    else
                    {
                        List<string> args =
                        [
                            "--no-default-browser-check",
                            "--disable-extensions",
                            "--disable-blink-features=AutomationControlled",
                            "--disable-infobars",
                            "--no-default-browser-check",
                            "--disable-extensions"
                        ];

                        if (RuntimeEnvironment.IsDocker())
                        {
                            args.Add("--disable-dev-shm-usage");
                        }

                        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions()
                        {
#if DEBUG
                            //Headless = false,
#endif
                            Args = args
                        });
                    }
                }
            }
            finally
            {
                _browserLock.Release();
            }
        }

        private async Task CloseBrowser()
        {
            await _browserLock.WaitAsync();

            try
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
            finally
            {
                _browserLock.Release();
            }
        }

        public async Task RebootBrowser()
        {
            await CloseBrowser();
            await CreateBrowser();

            _logger.LogDebug("Browser rebooted");
        }

        private async Task IdleCheckLoopAsync(CancellationToken ct)
        {
            _logger.LogDebug("BrowserManager idle check loop started");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (_browser != null && DateTime.Now - _lastUsed > new TimeSpan(0, 0, _settings.Value.MaxSecsWaitBetweenSearches + 60))
                    {
                        _logger.LogDebug("Browser idle timeout reached, closing...");
                        await CloseBrowser();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, ex, "Error in IdleCheckLoop");
                }

                try
                {
                    await Task.Delay(1000, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        public async Task<bool> CreateContext(MSAccountServerData data, bool isMobile)
        {
            _logger.LogDebug("Creating new context for {Data} | {User}", data.Account.Email, data.Account.User.Username);

            _lastUsed = DateTime.Now;
            try
            {
                await CreateBrowser();

                if (_settings.Value.UseFirefox)
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
                    await DeleteContext(data);
                    return false;
                }

#if DEBUG
                //await TestCommand(data);
#endif
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
            if (data.Context == null)
            {
                return;
            }

            await data.Context.CloseAsync();
            await data.Context.DisposeAsync();

            data.Context = null;
            data.Page = null;

            _logger.LogDebug("Deleted context for {Email} | {User}", data.Account.Email, data.Account.User.Username);
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

            if (!await NavigateToURL(data, BrowserConstants.URL_DASHBOARD))
            {
                _logger.LogError("Cannot proceed. Redirect failed for {Email} | {User}.",
                    data.Account.Email, data.Account.User.Username);
                return false;
            }

            return true;
        }

        private async Task<bool> NavigateToURL(MSAccountServerData data, string url, bool force = false)
        {
            try
            {
                if (force || data.Page.Url != url)
                {
                    IResponse response = await data.Page.GotoAsync(url, new PageGotoOptions()
                    {
                        WaitUntil = WaitUntilState.NetworkIdle,
                        Timeout = 15000
                    });
                    if (url != BrowserConstants.URL_BLANK_PAGE)
                    {
                        if (response == null || !response.Ok)
                        {
                            _logger.LogWarning("Failed to navigate to {url}. Returned status {code}", url, response?.Status);
                            return false;
                        }
                        else if (data.Page.Url.StartsWith(BrowserConstants.URL_MS_CHECK))
                        {
                            await data.Page.ClickAsync(BrowserConstants.SELECTOR_BUTTON_MS_CHECK);
                            await data.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                        }

                        if (data.Page.Url.StartsWith(BrowserConstants.URL_EXPIRED_COOKIES))
                        {
                            _logger.LogError("Failed to navigate to {url} for {email} | {user}. The cookies are expired. The user needs to login again",
                                url, data.Account.Email, data.Account.User.Username);

                            data.Account.IsCookiesExpired = true;
                            return false;
                        }
                    }

                    await WaitRandomMs(3500, 5000);
                }

                await data.Page.BringToFrontAsync();

                _logger.LogDebug("Navigated to {url} for {email} | {user}",
                    data.Page.Url, data.Account.Email, data.Account.User.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while navigating to {url}: {Message}", url, ex.Message);
                return false;
            }
        }

        private Task WaitRandomMs(int min, int max)
        {
            return Task.Delay(new TimeSpan(0, 0, 0, 0, Random.Shared.Next(min, max)));
        }

        private async Task HumanScroll(IPage page)
        {
            await page.BringToFrontAsync();

            int diff = Random.Shared.Next(7, 12); //pixel difference
            int delta = Random.Shared.Next(300, 1200) / diff; //how much time needs to be executed

            for (int i = 0; i < delta; i++)
            {
                await page.Mouse.WheelAsync(0, diff);

                if (Random.Shared.Next(0, 2) == 1)
                {
                    await Task.Delay(Random.Shared.Next(3, 8));
                }
            }
        }

        // Scrolls top-bottom in viewport-sized steps until the count of `selector` stops growing,
        // then returns to the top. Needed because Firefox headless does not trigger
        // IntersectionObserver-based lazy loading on cards below the initial viewport.
        private async Task ScrollUntilLocatorStable(IPage page, string selector, int maxIterations = 14)
        {
            await page.BringToFrontAsync();

            ILocator loc = page.Locator(selector);

            // Reset to top
            await page.Mouse.WheelAsync(0, -100000);
            await WaitRandomMs(300, 600);

            int previousCount = -1;
            int stableHits = 0;

            for (int i = 0; i < maxIterations; i++)
            {
                await page.Mouse.WheelAsync(0, 700);
                await WaitRandomMs(400, 700);

                int currentCount = await loc.CountAsync();
                if (currentCount == previousCount)
                {
                    stableHits++;
                    if (stableHits >= 2 && currentCount > 0)
                    {
                        break;
                    }
                }
                else
                {
                    stableHits = 0;
                    previousCount = currentCount;
                }
            }

            await page.Mouse.WheelAsync(0, -100000);
            await WaitRandomMs(500, 900);
        }

        private async Task<bool> WriteSearchAsHuman(IPage page, string keyword)
        {
            //Wait for the animation to finish
            await WaitRandomMs(1000, 2000);

            await WaitRandomMs(BrowserConstants.HUMAN_ACTION_MIN, BrowserConstants.HUMAN_ACTION_MAX);

            try
            {
                ILocator searchbar = page.Locator(BrowserConstants.SEARCHBAR_TEXTAREA);
                await searchbar.WaitForAsync();
                await searchbar.FocusAsync();

                foreach (char cr in keyword.ToCharArray())
                {
                    await page.Keyboard.TypeAsync(cr.ToString());
                    await WaitRandomMs(BrowserConstants.HUMAN_WRITING_MIN, BrowserConstants.HUMAN_WRITING_MAX);
                }

                await WaitRandomMs(BrowserConstants.HUMAN_ACTION_MIN, BrowserConstants.HUMAN_ACTION_MAX);
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
            foreach (AccountCookie c in cookies)
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
                    cookie.Expires = ((DateTimeOffset)c.Expires.Value).ToUnixTimeSeconds();
                }

                if (Enum.TryParse(typeof(SameSiteAttribute), c.SameSite, out object attr))
                {
                    cookie.SameSite = (SameSiteAttribute)attr;
                }

                result.Add(cookie);
            }

            return result;
        }

        public async ValueTask DisposeAsync()
        {
            // Stop the idle check loop FIRST so it can't race with our teardown.
            _idleCheckCts?.Cancel();
            try
            {
                if (_idleCheckTask != null)
                {
                    await _idleCheckTask.WaitAsync(TimeSpan.FromSeconds(5));
                }
            }
            catch
            {
                // task may have faulted or timed out; nothing actionable
            }
            _idleCheckCts?.Dispose();
            _idleCheckCts = null;
            _idleCheckTask = null;

            // Acquire the same lock CreateBrowser/CloseBrowser use, so we don't tear down a browser
            // while another caller is mid-create. WaitAsync without a timeout would deadlock if the
            // lock was leaked; bound it.
            bool lockTaken = false;
            try
            {
                lockTaken = await _browserLock.WaitAsync(TimeSpan.FromSeconds(10));
            }
            catch
            {
                lockTaken = false;
            }

            try
            {
                if (_browser != null)
                {
                    try
                    {
                        foreach (IBrowserContext ctx in _browser.Contexts)
                        {
                            await ctx.CloseAsync();
                            await ctx.DisposeAsync();
                        }

                        await _browser.CloseAsync();
                        await _browser.DisposeAsync();
                        _browser = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error, ex, "Error while disposing browser");
                    }
                }

                _playwright?.Dispose();
                _playwright = null;
            }
            finally
            {
                if (lockTaken)
                {
                    _browserLock.Release();
                }
                _browserLock.Dispose();
            }
        }
    }
}
