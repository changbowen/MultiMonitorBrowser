using CefSharp;
using CefSharp.Handler;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MultiMonitorBrowser
{
    class RequestHandler : DefaultRequestHandler
    {
        public override bool GetAuthCredentials(IWebBrowser browserControl, IBrowser browser, IFrame frame, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback)
        {
            string username = null, password = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var win = (MainWindow)Window.GetWindow((ChromiumWebBrowser)browserControl);
                username = win.BasicAuth.Key;
                password = App.ReplaceAndDecrypt(win.BasicAuth.Value, win.EncryptedStrings);
            });
            callback.Continue(username, password);
            return true;
        }
    }
}
