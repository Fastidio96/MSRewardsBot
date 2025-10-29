using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Server.Automation
{
    public class BrowserManager : IDisposable
    {
        private readonly ILogger<BrowserManager> _logger;
        private IPlaywright _playwright;
        private IBrowser _browser;
        private IBrowserContext _context;
        private IPage _page;

        private const string URL_DASHBOARD = "";

        public BrowserManager(ILogger<BrowserManager> logger)
        {
            _logger = logger;

            Init();
        }

        private void Init()
        {
            _logger.Log(LogLevel.Information, "Checking dependencies..");
            Microsoft.Playwright.Program.Main(["install"]);
        }

        public async void Start()
        {
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
        }

        public async Task<bool> DashboardUpdate(MSAccount account)
        {
            List<Cookie> cookies = ConvertToPWCookies(account.Cookies);
            await _context.AddCookiesAsync(cookies);

            _page = await _context.NewPageAsync();
            await _page.GotoAsync(URL_DASHBOARD);


            return false;
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

                if (c.Expires.HasValue)
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
