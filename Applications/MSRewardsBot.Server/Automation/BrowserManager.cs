using System;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

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
            _browser = await _playwright.Chromium.LaunchAsync();
            _context = await _browser.NewContextAsync();
            _page = await _context.NewPageAsync();
        }



        public void Dispose()
        {
            _browser.CloseAsync();
        }
    }
}
