using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Server.DataEntities;

namespace MSRewardsBot.Server.Automation
{
    public partial class BrowserManager : IDisposable
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

            int exitCode = Microsoft.Playwright.Program.Main(["install"]);
            if (exitCode == 0)
            {
                _logger.Log(LogLevel.Information, "Dependencies installed successfully");
            }
            else
            {
                _logger.LogCritical("Cannot install dependencies");
            }

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Firefox.LaunchAsync
            (new BrowserTypeLaunchOptions()
            {
#if DEBUG
                //Headless = false,
#endif
                Args =
                    [
                        //"--disable-blink-features=AutomationControlled",
                        "--disable-infobars",
                        "--no-default-browser-check",
                        "--disable-extensions"
                    ]
            }
            );

            _logger.Log(LogLevel.Information, "BrowserManager init completed");
        }

        public async Task<bool> CreateContext(MSAccountServerData data)
        {
            _logger.LogDebug("Creating new context for {Data} | {User}", data.Account.Email, data.Account.User.Username);

            //await CreateChromeStealthContext(data);
            await CreateFirefoxStealthContext(data);

            //data.Context = await _browser.NewContextAsync(new BrowserNewContextOptions()
            //{
            //    UserAgent = BrowserConstants.UA_PC_EDGE
            //});

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

                // Wait for the animations to finish
                await Task.Delay(Random.Shared.Next(5000, 7500));

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
                int secs = Random.Shared.Next(10, 60);
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

        private void LogDebugAction(string actionName)
        {
            _logger.LogDebug("Logged action {time} {action} ", DateTime.Now.ToString("mm:ss:fff"), actionName.ToUpper());
        }
        private TimeSpan GetRandomMsTimes(int min, int max)
        {
            return new TimeSpan(0, 0, 0, 0, Random.Shared.Next(min, max));
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
