using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using MSRewardsBot.Client.DataEntities;
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
            webview.NavigationCompleted -= Webview_DetectInit_NavigationCompleted;

            Dispatcher.InvokeAsync(delegate ()
            {
                webview.CoreWebView2?.Stop();
                webview.Dispose();
                webview = null;
            }).Wait();

            KillWebViewProcess();
        }

        private bool KillWebViewProcess()
        {
            try
            {
                Process[] ps = Process.GetProcessesByName("msedgewebview2");
                foreach (Process p in ps)
                {
                    p.Kill();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
