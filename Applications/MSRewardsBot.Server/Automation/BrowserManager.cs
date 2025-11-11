using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Common.DataEntities.Stats;

namespace MSRewardsBot.Server.Automation
{
    public class BrowserManager : IDisposable
    {
        private readonly ILogger<BrowserManager> _logger;
        private IPlaywright _playwright;
        private IBrowser _browser;
        private IBrowserContext _context;
        private IPage _page;

        public BrowserManager(ILogger<BrowserManager> logger)
        {
            _logger = logger;
        }

        private void Install()
        {
            _logger.Log(LogLevel.Information, "Checking and installing browser dependencies..");
            Microsoft.Playwright.Program.Main(["install"]);
            _logger.Log(LogLevel.Information, "Dependencies installed successfully");
        }

        public async void Start()
        {
            Install();

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync
            (
#if DEBUG
                new BrowserTypeLaunchOptions()
                {
                    Headless = false
                }
#endif
            );
            _context = await _browser.NewContextAsync();
            _page = await _context.NewPageAsync();

            _logger.Log(LogLevel.Information, "BrowserManager init completed");
        }

        private async Task<bool> StartLoggedSession(MSAccount account)
        {
            if (account.Cookies == null || account.Cookies.Count == 0)
            {
                _logger.LogError("Cannot proceed. No cookies for account {Account} | {User} found.", account.Email, account.User.Username);
                return false;
            }

            try
            {
                List<Cookie> cookies = ConvertToPWCookies(account.Cookies);
                await _context.AddCookiesAsync(cookies);
                _logger.LogDebug("{count} cookies loaded", cookies.Count);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error on {MethodName}: {Message}", nameof(StartLoggedSession), ex.Message);
                return false;
            }
        }

        private Task CloseLoggedSession()
        {
            _logger.LogDebug("Clearing cookies from the page context");
            return _context.ClearCookiesAsync();
        }

        private async Task<bool> NavigateToURL(string url)
        {
            try
            {
                await _page.GotoAsync(url);
                await _page.WaitForURLAsync(url, new PageWaitForURLOptions()
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

        public async Task<MSAccount> DashboardUpdate(MSAccount account)
        {
            _logger.LogDebug("Dashboard update started for {Account} | {User}", account.Email, account.User.Username);

            if (!await StartLoggedSession(account))
            {
                return null;
            }

            if (!await NavigateToURL(BrowserConstants.URL_DASHBOARD_PTS_BREAKDOWN))
            {
                return null;
            }

            if (string.IsNullOrEmpty(account.Email))
            {
                account.Email = await _page.EvaluateAsync<string>(BrowserConstants.SELECTOR_EMAIL);
                _logger.LogInformation("New account email found. {Email}", account.Email);
            }

            string pcPoints = await _page.EvaluateAsync<string>(BrowserConstants.SELECTOR_BREAKDOWN_PC_POINTS);
            pcPoints = pcPoints.Trim();
            string[] sub = pcPoints.Split('/');

            if (sub.Length == 2)
            {
                if (int.TryParse(sub[0], out int currentPts))
                {
                    _logger.LogDebug("Found value for {stat}: {val}", nameof(MSAccountStats.CurrentPointsPCSearches), currentPts);
                    account.Stats.CurrentPointsPCSearches = currentPts;
                }
                if (int.TryParse(sub[1], out int maxPts))
                {
                    _logger.LogDebug("Found value for {stat}: {val}", nameof(MSAccountStats.MaxPointsPCSearches), maxPts);
                    account.Stats.MaxPointsPCSearches = maxPts;
                }
            }

            await CloseLoggedSession();

            return account;
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
            await _context.CloseAsync();
            await _browser.CloseAsync();
        }
    }
}
