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
        private event EventHandler InitCompleted;

        public MSLoginWindow(ViewModel vm)
        {
            InitializeComponent();

            _vm = vm;

            webview.NavigationCompleted += Webview_DetectInit_NavigationCompleted;
            InitCompleted += WebViewWorker_InitCompleted;

            this.Closed += MSLoginWindow_Closed;
        }
        private void Webview_DetectInit_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            this.webview.NavigationCompleted -= Webview_DetectInit_NavigationCompleted;
            InitCompleted?.Invoke(this, e);
        }

        private void MSLoginWindow_Closed(object? sender, EventArgs e)
        {
            webview?.Dispose();
        }

        private void WebViewWorker_InitCompleted(object? sender, EventArgs e)
        {
            InitCompleted -= WebViewWorker_InitCompleted;
            webview.CoreWebView2.CookieManager.DeleteAllCookies();

            webview.NavigationCompleted += WebView_NavigationCompleted;
            webview.CoreWebView2.Navigate(Costants.URL_LOGIN);
        }

        private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess && (webview.Source.Host == Costants.URL_HOST_LOGGED))
            {
                webview.NavigationCompleted -= WebView_NavigationCompleted;
                GatherCookies();
            }
        }

        private async void GatherCookies()
        {
            List<CoreWebView2Cookie> webviewCoookies = await webview.CoreWebView2.CookieManager.GetCookiesAsync(Costants.URL_LOGIN);
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

            if (!await _vm.InsertMSAccount(cookies))
            {
                Utils.ShowMessage("Unable to save ms account!");
            }
            else
            {
                await _vm.GetUserInfo();
            }

            this.Close();
            Dispose();
        }

        public void Dispose()
        {
            if (webview != null)
            {
                webview.NavigationCompleted -= Webview_DetectInit_NavigationCompleted;

                Dispatcher.InvokeAsync(delegate ()
                {
                    webview.CoreWebView2?.Stop();
                    webview.Dispose();
                    webview = null;
                }).Wait();
            }

            Utils.KillWebViewProcess();
        }
    }
}
