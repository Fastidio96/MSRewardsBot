using System;
using Microsoft.Web.WebView2.Wpf;

namespace MSRewardsBot.Client.DataEntities
{
    public class Operation : OperationBase
    {
        public Func<WebView2, bool>? OnSuccess { get; set; }
        public Func<WebView2, bool>? OnFail { get; set; }

        public Operation(Func<WebView2, bool> onSuccess, string url, Func<WebView2, bool> onFail = null)
        {
            this.OnSuccess = onSuccess;
            this.OnFail = onFail;

            base.Url = url;
        }

        public Operation(Func<WebView2, bool> onSuccess, string url, Int32 waitBeforeProceed, Func<WebView2, bool> onFail = null)
        {
            this.OnSuccess = onSuccess;
            this.OnFail = onFail;

            base.Url = url;
            base.WaitBeforeProceedTimeout = waitBeforeProceed;
        }
    }
}
