using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Playwright;
using MSRewardsBot.Server.DataEntities;

namespace MSRewardsBot.Server.Automation
{
    public partial class BrowserManager
    {
        private async Task CreateChromeStealthContext(MSAccountServerData data, bool useUAforPC = true)
        {
            data.Context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = useUAforPC ? BrowserConstants.UA_PC_CHROME : BrowserConstants.UA_MOBILE_CHROME,
                ViewportSize = new() { Width = 1366, Height = 768 },
                Locale = CultureInfo.CurrentCulture.Name
            });

            // 1) Remove navigator.webdriver and related automation flags
            await data.Context.AddInitScriptAsync(@"
Object.defineProperty(navigator, 'webdriver', {
    get: () => undefined
});
");

            // 2) Provide believable plugins and languages
            await data.Context.AddInitScriptAsync(@"
Object.defineProperty(navigator, 'plugins', {
  get: () => [1, 2, 3, 4, 5]
});
Object.defineProperty(navigator, 'languages', {
  get: () => ['en-US', 'en']
});
Object.defineProperty(navigator, 'language', {
  get: () => 'en-US'
});
");

            // 3) Patch WebGL getParameter to return realistic vendor/renderer
            await data.Context.AddInitScriptAsync(@"
(() => {
  try {
    const getParameter = WebGLRenderingContext.prototype.getParameter;
    WebGLRenderingContext.prototype.getParameter = function (p) {
      // UNMASKED_VENDOR_WEBGL = 37445, UNMASKED_RENDERER_WEBGL = 37446
      if (p === 37445) return 'NVIDIA Corporation';
      if (p === 37446) return 'ANGLE (NVIDIA GeForce GTX 1050 Direct3D11 vs_5_0 ps_5_0)';
      return getParameter.apply(this, [p]);
    };
  } catch (e) { }
})();
");

            // 4) Ensure window.chrome exists and looks native
            await data.Context.AddInitScriptAsync(@"
(() => {
  if (!window.chrome) window.chrome = {};
  if (!window.chrome.runtime) window.chrome.runtime = {};
  if (!window.chrome.runtime.id) window.chrome.runtime.id = '';
})();
");

            // 5) Fake deviceMemory and hardwareConcurrency to realistic values
            await data.Context.AddInitScriptAsync(@"
Object.defineProperty(navigator, 'deviceMemory', { get: () => 8 });
Object.defineProperty(navigator, 'hardwareConcurrency', { get: () => 8 });
");

            // 6) Provide userAgentData shim when missing (modern browsers)
            await data.Context.AddInitScriptAsync(@"
(() => {
  try {
    if (!navigator.userAgentData) {
      Object.defineProperty(navigator, 'userAgentData', {
        get: () => ({
          brands: [{ brand: 'Chromium', version: '119' }, { brand: 'Google Chrome', version: '119' }],
          mobile: false,
          platform: 'Windows'
        })
      });
    }
  } catch (e) {}
})();
");

            // 7) Remove Playwright/CDP visible globals
            await data.Context.AddInitScriptAsync(@"
(() => {
  try {
    if (window.__pwInitScripts) delete window.__pwInitScripts;
    if (window.__playwright__binding__) delete window.__playwright__binding__;
    if (window.cdp) delete window.cdp;
  } catch (e) {}
})();
");

            // 8) Provide chrome.loadTimes and chrome.csi shims used by some detectors
            await data.Context.AddInitScriptAsync(@"
(() => {
  try {
    if (!window.chrome) window.chrome = {};
    if (!window.chrome.loadTimes) {
      window.chrome.loadTimes = function() {
        var now = Date.now() / 1000;
        return {
          requestTime: now,
          startLoadTime: now,
          commitLoadTime: now,
          finishDocumentLoadTime: now,
          finishLoadTime: now,
          navigationType: 'Other',
          connectionInfo: 'h2'
        };
      };
    }
    if (!window.chrome.csi) {
      window.chrome.csi = function() {
        return { startE: Date.now(), onloadT: Date.now(), pageT: 0, tran: 15 };
      };
    }
  } catch(e) {}
})();
");

            // 9) Worker patch: ensure workers see the same navigator-like values and no webdriver
            // This build the worker blob in the page, using navigator values captured at runtime
            await data.Context.AddInitScriptAsync(@"
(() => {
  try {
    const OriginalWorker = window.Worker;
    window.Worker = function(scriptUrl) {
      const ua = navigator.userAgent;
      const lang = navigator.language || 'en-US';
      const langs = JSON.stringify(navigator.languages || ['en-US','en']);
      const hc = navigator.hardwareConcurrency || 8;

      const patch = `Object.defineProperty(navigator,'webdriver',{get:()=>undefined});
Object.defineProperty(navigator,'language',{get:()=>${JSON.stringify(lang)}});
Object.defineProperty(navigator,'languages',{get:()=>${langs}});
Object.defineProperty(navigator,'hardwareConcurrency',{get:()=>${hc}});
Object.defineProperty(navigator,'userAgent',{get:()=>${JSON.stringify(ua)}});
`;

      const blob = new Blob([patch + '; importScripts(' + JSON.stringify(scriptUrl) + ');'], { type: 'application/javascript' });
      const url = URL.createObjectURL(blob);
      return new OriginalWorker(url);
    };

    window.Worker.prototype = OriginalWorker.prototype;
  } catch (e) {}
})();
");

            // 10) Invisible iframe getter patch: implement a native-like contentWindow getter
            await data.Context.AddInitScriptAsync(@"
(() => {
  try {
    const descriptor = Object.getOwnPropertyDescriptor(HTMLIFrameElement.prototype, 'contentWindow');
    if (!descriptor || (descriptor.get && descriptor.get.toString().includes('[native code]'))) return;
    Object.defineProperty(HTMLIFrameElement.prototype, 'contentWindow', {
      get: function() {
        try {
          if (this && this.contentDocument && this.contentDocument.defaultView) return this.contentDocument.defaultView;
        } catch (e) {}
        return this.ownerDocument ? this.ownerDocument.defaultView : window;
      },
      configurable: true
    });
  } catch (e) {}
})();
");

            // 11) Patch stack trace strings that detectors may use to fingerprint CDP evaluation frames
            await data.Context.AddInitScriptAsync(@"
(() => {
  try {
    const origPrepareStackTrace = Error.prepareStackTrace;
    Error.prepareStackTrace = function(err, stack) {
      if (origPrepareStackTrace) return origPrepareStackTrace(err, stack);
      return stack.map(f => f.toString()).join('\n');
    };
  } catch (e) {}
})();
");

            // 12) Remove potential suspicious iframe overrides done earlier by naive patches
            // (redundant safety removal for other libs)
            await data.Context.AddInitScriptAsync(@"
(() => {
  try {
    const suspicious = ['__webdriver_evaluate', '__driver_evaluate', '__selenium_unwrapped'];
    suspicious.forEach(name => {
      try { if (window[name]) delete window[name]; } catch(e) {}
    });
  } catch(e) {}
})();
");

            // 13) Small timing/randomization helper in context to make sequences less deterministic
            await data.Context.AddInitScriptAsync(@"
(() => {
  window.__human_like_rand = function(min, max) {
    return Math.floor(Math.random() * (max - min + 1)) + min;
  };
})();
");
        }

        private async Task CreateFirefoxStealthContext(MSAccountServerData data, bool isMobile)
        {
            if (isMobile)
            {
                data.Context = await _browser.NewContextAsync(new BrowserNewContextOptions
                {
                    UserAgent = BrowserConstants.UA_MOBILE_FIREFOX,
                    ViewportSize = new ViewportSize() { Width = 915, Height = 412 },
                    DeviceScaleFactor = 2.625f,
                    HasTouch = true,
                    Locale = CultureInfo.CurrentCulture.Name
                });
            }
            else
            {
                data.Context = await _browser.NewContextAsync(new BrowserNewContextOptions
                {
                    UserAgent = BrowserConstants.UA_PC_FIREFOX,
                    ViewportSize = new ViewportSize() { Width = 1366, Height = 768 },
                    Locale = CultureInfo.CurrentCulture.Name
                });
            }

            // Fix hasWebdriverTrue
            await data.Context.AddInitScriptAsync(@"
                Object.defineProperty(navigator, 'webdriver', {
                    get: () => undefined
                });
                ");

            // Fix hasWebdriverInFrameTrue
            await data.Context.AddInitScriptAsync(@"
// Remove navigator.webdriver in every frame
(function() {
    Object.defineProperty(navigator, 'webdriver', {
        get: () => undefined
    });

    const iframeDescriptor = Object.getOwnPropertyDescriptor(
        HTMLIFrameElement.prototype,
        'contentWindow'
    );

    Object.defineProperty(HTMLIFrameElement.prototype, 'contentWindow', {
        get: function() {
            const win = iframeDescriptor.get.apply(this);
            try {
                Object.defineProperty(win.navigator, 'webdriver', {
                    get: () => undefined
                });
            } catch (e) {}
            return win;
        }
    });
})();
");
            // Fix hasInconsistentWorkerValues
            await data.Context.AddInitScriptAsync(@"
(function() {

    const realValues = {
        webdriver: navigator.webdriver,
        languages: navigator.languages,
        hardwareConcurrency: navigator.hardwareConcurrency,
        platform: navigator.platform,
        userAgent: navigator.userAgent
    };

    const OriginalWorker = window.Worker;

    window.Worker = function(scriptURL, options) {
        const blob = new Blob([`
            const realValues = ${JSON.stringify(realValues)};

            // Apply original values inside worker
            Object.defineProperty(navigator, 'webdriver', { get: () => realValues.webdriver });
            Object.defineProperty(navigator, 'languages', { get: () => realValues.languages });
            Object.defineProperty(navigator, 'hardwareConcurrency', { get: () => realValues.hardwareConcurrency });
            Object.defineProperty(navigator, 'platform', { get: () => realValues.platform });
            Object.defineProperty(navigator, 'userAgent', { get: () => realValues.userAgent });

            importScripts('${scriptURL}');
        `], { type: 'application/javascript' });

        const workerURL = URL.createObjectURL(blob);
        return new OriginalWorker(workerURL, options);
    };

})();
");

        }
    }
}
