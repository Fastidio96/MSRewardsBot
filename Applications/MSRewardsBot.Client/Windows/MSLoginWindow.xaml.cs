using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using MSRewardsBot.Client.DataEntities;

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

            this.Closing += LoginWindow_Closing;
            webViewWorker.InitCompleted += WebViewWorker_InitCompleted;
        }

        private void WebViewWorker_InitCompleted(object? sender, EventArgs e)
        {
            webViewWorker.Start(new Operation(null, Costants.URL_LOGIN));
            webViewWorker.WebView.NavigationCompleted += WebView_NavigationCompleted;
        }

        private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess && (webViewWorker.WebView.Source.Host == Costants.URL_HOST_LOGGED))
            {
                this.Closing -= LoginWindow_Closing;
                GatherCookies();
            }
            else
            {
                this.Visibility = Visibility.Visible;
            }
        }

        private void LoginWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; //Need to gather cookies before exiting
            GatherCookies();
        }

        private async void GatherCookies()
        {
            List<CoreWebView2Cookie> webviewCoookies = await this.webViewWorker.WebView.CoreWebView2.CookieManager.GetCookiesAsync(Costants.URL_LOGIN);

            if (webviewCoookies == null || webviewCoookies.Count == 0)
            {
                return;
            }

            foreach (CoreWebView2Cookie c in webviewCoookies)
            {
                //_vm.AppData.CurrentUser.Cookies.Add(c.ToSystemNetCookie());
            }

            this.Closing -= LoginWindow_Closing;
            this.Close();
            webViewWorker.Dispose();

            //_vm.AppData.CurrentUser.LastDBChange = DateTime.UtcNow; // Trigger save
            //_vm.AppData.CurrentUser.Status = UserStatus.Logged;
        }
    }
}
