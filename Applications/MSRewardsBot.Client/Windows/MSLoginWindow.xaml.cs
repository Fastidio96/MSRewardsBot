using System;
using System.Collections.Generic;
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
        private readonly ViewModel _vm;
        private readonly int? _refreshAccountId;
        private event EventHandler InitCompleted;

        public MSLoginWindow(ViewModel vm, int? refreshAccountId)
        {
            InitializeComponent();

            _vm = vm;
            _refreshAccountId = refreshAccountId;

            if (_refreshAccountId.HasValue)
            {
                this.Title = "Re-login MS account";
            }

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
            this.Closed -= MSLoginWindow_Closed;

            if (webview != null)
            {
                webview.NavigationCompleted -= Webview_DetectInit_NavigationCompleted;
                webview.NavigationCompleted -= WebView_NavigationCompleted;
                webview.Dispose();
                webview = null;
            }

            Utils.KillWebViewProcess();
        }

        private void WebViewWorker_InitCompleted(object? sender, EventArgs e)
        {
            InitCompleted -= WebViewWorker_InitCompleted;
            webview.CoreWebView2.CookieManager.DeleteAllCookies();

            webview.NavigationCompleted += WebView_NavigationCompleted;
            webview.CoreWebView2.Navigate(AppConstants.URL_LOGIN);
        }

        private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess && (webview.Source.Host == AppConstants.URL_HOST_LOGGED))
            {
                webview.NavigationCompleted -= WebView_NavigationCompleted;
                GatherCookies();
            }
        }

        private async void GatherCookies()
        {
            List<CoreWebView2Cookie> webviewCoookies = await webview.CoreWebView2.CookieManager.GetCookiesAsync(AppConstants.URL_LOGIN);
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

            bool ok = _refreshAccountId.HasValue
                ? await _vm.UpdateMSAccountCookies(_refreshAccountId.Value, cookies)
                : await _vm.InsertMSAccount(cookies);

            if (!ok)
            {
                Utils.ShowMessage(_refreshAccountId.HasValue
                    ? "Unable to refresh ms account cookies!"
                    : "Unable to save ms account!");
            }
            else
            {
                await _vm.GetUserInfo();
            }

            this.Close(); // MSLoginWindow_Closed handles webview disposal
        }
    }
}
