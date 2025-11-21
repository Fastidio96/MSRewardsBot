using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using MSRewardsBot.Client.DataEntities;
using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Client.Windows
{
    /// <summary>
    /// Interaction logic for MSLoginWindow.xaml
    /// </summary>
    public partial class MSLoginWindow : Window
    {
        private ViewModel _vm;

        public MSLoginWindow(ViewModel vm)
        {
            InitializeComponent();

            _vm = vm;
            webViewWorker.InitCompleted += WebViewWorker_InitCompleted;
            this.Closed += MSLoginWindow_Closed;
        }

        private void MSLoginWindow_Closed(object? sender, EventArgs e)
        {
            webViewWorker?.Dispose();
        }

        private void WebViewWorker_InitCompleted(object? sender, EventArgs e)
        {
            webViewWorker.InitCompleted -= WebViewWorker_InitCompleted;
            this.webViewWorker.WebView.CoreWebView2.CookieManager.DeleteAllCookies();

            webViewWorker.WebView.NavigationCompleted += WebView_NavigationCompleted;
            webViewWorker.WebView.CoreWebView2.Navigate(Costants.URL_LOGIN);
        }

        private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess && (webViewWorker.WebView.Source.Host == Costants.URL_HOST_LOGGED))
            {
                webViewWorker.WebView.NavigationCompleted -= WebView_NavigationCompleted;
                GatherCookies();
            }
        }

        private async void GatherCookies()
        {
            List<CoreWebView2Cookie> webviewCoookies = await this.webViewWorker.WebView.CoreWebView2.CookieManager.GetCookiesAsync(Costants.URL_LOGIN);
            if (webviewCoookies == null || webviewCoookies.Count == 0)
            {
                return;
            }

            List<AccountCookie> cookies = new List<AccountCookie>();
            foreach (CoreWebView2Cookie c in webviewCoookies)
            {
                cookies.Add(new AccountCookie()
                {
                    Domain = c.Domain,
                    Expires = c.Expires,
                    HttpOnly = c.IsHttpOnly,
                    Name = c.Name,
                    Value = c.Value,
                    Path = c.Path,
                    SameSite = c.SameSite.ToString(),
                    Secure = c.IsSecure
                });
            }

            if(!await _vm.InsertMSAccount(cookies))
            {
                Utils.ShowMessage("Unable to save ms account!");
            }

            this.Close();
            webViewWorker?.Dispose();
        }
    }
}
