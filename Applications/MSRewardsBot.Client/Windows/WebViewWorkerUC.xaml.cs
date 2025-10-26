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
        private ConcurrentQueue<OperationBase> _queue;
        private bool _start;
        private bool _isWebViewReady;

        private OperationBase _currentOp;
        Thread _thread;

        public WebView2 WebView { get; set; }

        public event EventHandler InitCompleted;

        public WebViewWorkerUC()
        {
            InitializeComponent();

            _queue = new ConcurrentQueue<OperationBase>();
            WebView = webview;

            _start = false;
            _isWebViewReady = false;

#if DEBUG
            webview.SourceChanged += delegate (object? sender, CoreWebView2SourceChangedEventArgs e)
            { Debug.WriteLine(string.Format("{0} >> {1}", DateTime.Now.ToString("HH:mm:ss:fff"), webview.Source.ToString())); };
#endif

            webview.NavigationCompleted += Webview_DetectInit_NavigationCompleted;
            webview.NavigationCompleted += Webview_NavigationCompleted;
        }


        private void Webview_DetectInit_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _isWebViewReady = true;
            this.webview.NavigationCompleted -= Webview_DetectInit_NavigationCompleted;

            InitCompleted?.Invoke(this, e);
        }

        public void Start(OperationBase operation)
        {
            _queue.Enqueue(operation);

            if (!_start)
            {
                _start = true;

                _thread = new Thread(ProcessOperations);
                _thread.Name = "WebView Worker";
                _thread.Start();
            }
        }

        private void ProcessOperations()
        {
            while (_start)
            {
                while (_queue.Count > 0)
                {
                    try
                    {
                        if (!_isWebViewReady)
                        {
                            Thread.Sleep(50);
                            continue;
                        }

                        if (_currentOp == null)
                        {
                            _currentOp = _queue.First();
                        }

                        if (_currentOp.IsCompleted)
                        {
                            if (!_queue.TryDequeue(out _))
                            {
                                Debug.WriteLine("Cannot dequeue the current op!");
                            }
                            _currentOp = null;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(_currentOp.Url))
                            {
                                if (!_currentOp.IsStarted)
                                {
                                    _currentOp.IsStarted = true;

                                    Dispatcher.Invoke(delegate ()
                                    {
                                        WebView.CoreWebView2.Navigate(_currentOp.Url);
                                    });
                                }
                            }
                            else
                            {
                                Operation operation = _currentOp as Operation;
                                if (operation != null)
                                {
                                    if (!operation.IsStarted)
                                    {
                                        operation.IsCompleted = operation.OnSuccess.Invoke(WebView);
                                    }

                                    _currentOp.IsCompleted = operation.IsCompleted;
                                }
                            }

                            Thread.Sleep(50);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        //Todo: log exception
                    }
                }

                Thread.Sleep(50);
            }
        }

        private void Webview_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (_currentOp != null)
            {
                string currentUrl = string.Format("{0}://{1}{2}", WebView.Source.Scheme, WebView.Source.Host, WebView.Source.AbsolutePath);
                bool isRequestedURL = currentUrl == _currentOp.Url || WebView.Source.ToString() == _currentOp.Url;

                Debug.WriteLine(string.Format("Is requested URL {0} >> {1}", isRequestedURL, currentUrl));

                Thread t = new Thread(delegate ()
                {
                    Operation op = _currentOp as Operation;
                    if (op != null)
                    {
                        if (op.WaitBeforeProceedTimeout > 0)
                        {
                            Thread.Sleep(op.WaitBeforeProceedTimeout);
                        }

                        if (isRequestedURL)
                        {
                            Dispatcher.Invoke(delegate ()
                            {
                                op.IsCompleted = op.OnSuccess?.Invoke(WebView) ?? true;
                            });
                        }
                        else
                        {
                            Dispatcher.Invoke(delegate ()
                            {
                                op.IsCompleted = op.OnFail?.Invoke(WebView) ?? true;
                            });
                        }

                    }
                });

                t.Name = nameof(Webview_NavigationCompleted);
                t.Start();
            }
        }


        public void Dispose()
        {
            Thread t = new Thread(delegate ()
            {
                _start = false;

                _queue.Clear();

                if (_currentOp != null && !_currentOp.IsCompleted)
                {
                    Thread.Sleep(5000); //WaitAsync 5s to finish the current operation before dispose
                }


                if (_thread != null)
                {
                    if (_thread.IsAlive)
                    {
                        if (!_thread.Join(5000))
                        {
                        }
                    }

                    _thread = null;
                }

                _currentOp = null;
                _queue = null;

                webview.NavigationCompleted -= Webview_DetectInit_NavigationCompleted;
                webview.NavigationCompleted -= Webview_NavigationCompleted;

                Dispatcher.InvokeAsync(delegate ()
                {
                    webview.CoreWebView2?.Stop();
                    webview.Dispose();
                    webview = null;
                });

                KillProcessWebView();
            });

            t.Name = "WebviewWorkerUC Dispose";
            t.Start();
        }

        private bool KillProcessWebView()
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
