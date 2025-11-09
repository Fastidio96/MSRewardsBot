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
            _logger.Log(LogLevel.Information, "Checking dependencies..");
            Microsoft.Playwright.Program.Main(["install"]);
        }

        public async void Start()
        {
            Install();

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync
            (
//#if DEBUG
//                new BrowserTypeLaunchOptions()
//                {
//                    Headless = false
//                }
//#endif
            );
            _context = await _browser.NewContextAsync();
        }

        private async Task<bool> PrepareLoggedSession(MSAccount account)
        {
            if (account.Cookies == null || account.Cookies.Count == 0)
            {
                return false;
            }

            try
            {
                List<Cookie> cookies = ConvertToPWCookies(account.Cookies);
                await _context.AddCookiesAsync(cookies);

                _page = await _context.NewPageAsync();
                await _page.GotoAsync(BrowserConstants.URL_DASHBOARD);
                await _page.WaitForURLAsync(BrowserConstants.URL_DASHBOARD);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error on {MethodName}: {Message}", nameof(PrepareLoggedSession), ex.Message);
                return false;
            }
        }

        public async Task<MSAccount> DashboardUpdate(MSAccount account)
        {
            if (!await PrepareLoggedSession(account))
            {
                return null;
            }

            if (string.IsNullOrEmpty(account.Email))
            {
                account.Email = await _page.EvaluateAsync<string>(BrowserConstants.SELECTOR_EMAIL);
            }

            string pcPoints = await _page.EvaluateAsync<string>(BrowserConstants.SELECTOR_BREAKDOWN_PC_POINTS);
            pcPoints = pcPoints.Trim();
            string[] sub = pcPoints.Split('/');

            if (sub.Length == 2)
            {
                if (int.TryParse(sub[0], out int currentPts))
                {
                    account.Stats.CurrentPointsPCSearches = currentPts;
                }
                if (int.TryParse(sub[1], out int maxPts))
                {
                    account.Stats.MaxPointsPCSearches = maxPts;
                }
            }

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
