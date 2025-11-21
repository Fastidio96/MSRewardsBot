using System;
using System.Diagnostics;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace MSRewardsBot.Client.Windows
{
    /// <summary>
    /// Interaction logic for WebViewWorkerUC.xaml
    /// </summary>
    public partial class WebViewWorkerUC : UserControl, IDisposable
    {
        public WebView2 WebView { get; set; }

        public event EventHandler InitCompleted;

        public WebViewWorkerUC()
        {
            InitializeComponent();

            WebView = webview;
            webview.NavigationCompleted += Webview_DetectInit_NavigationCompleted;
        }


        private void Webview_DetectInit_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            this.webview.NavigationCompleted -= Webview_DetectInit_NavigationCompleted;
            InitCompleted?.Invoke(this, e);
        }

        public void Dispose()
        {
            if(webview != null)
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
