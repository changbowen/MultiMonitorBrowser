using CefSharp;
using CefSharp.Structs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MultiMonitorBrowser
{
    //remember DisplayHandler is created on the UI thread.
    class DisplayHandler : IDisplayHandler, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private object _favIcon;
        /// <summary>
        /// For binding to System.Windows.Window.Icon.
        /// </summary>
        public object FavIcon {
            get { return _favIcon; }
            set { _favIcon = value; OnPropertyChanged("FavIcon"); }
        }

        private BitmapDecoder _decoder;
        private BitmapDecoder Decoder {
            get => _decoder;
            set {
                if (_decoder != null) _decoder.DownloadCompleted -= decoderDownloadCompleted;
                _decoder = value;
                if (_decoder != null) _decoder.DownloadCompleted += decoderDownloadCompleted;
            }
        }

        private void decoderDownloadCompleted(object sender, EventArgs e)
        {
            FavIcon = Decoder.Frames.OrderBy(f => f.Width).FirstOrDefault();
            Decoder = null;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.Invoke(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
            else PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public void OnAddressChanged(IWebBrowser chromiumWebBrowser, AddressChangedEventArgs addressChangedArgs)
        {
        }

        public bool OnAutoResize(IWebBrowser chromiumWebBrowser, IBrowser browser, CefSharp.Structs.Size newSize)
        {
            return false;
        }

        public bool OnConsoleMessage(IWebBrowser chromiumWebBrowser, ConsoleMessageEventArgs consoleMessageArgs)
        {
            return false;
        }

        public void OnFaviconUrlChange(IWebBrowser chromiumWebBrowser, IBrowser browser, IList<string> urls)
        {
            var baseUrl = new Uri(browser.MainFrame.Url).GetLeftPart(UriPartial.Authority);
            Application.Current.Dispatcher.Invoke(() =>
            {
                Decoder = BitmapDecoder.Create(new Uri(baseUrl + "/favicon.ico"), BitmapCreateOptions.None, BitmapCacheOption.OnDemand);
            });
        }

        public void OnFullscreenModeChange(IWebBrowser chromiumWebBrowser, IBrowser browser, bool fullscreen)
        {
        }

        public void OnLoadingProgressChange(IWebBrowser chromiumWebBrowser, IBrowser browser, double progress)
        {
        }

        public void OnStatusMessage(IWebBrowser chromiumWebBrowser, StatusMessageEventArgs statusMessageArgs)
        {
        }

        public void OnTitleChanged(IWebBrowser chromiumWebBrowser, TitleChangedEventArgs titleChangedArgs)
        {
        }

        public bool OnTooltipChanged(IWebBrowser chromiumWebBrowser, ref string text)
        {
            return false;
        }
    }
}
