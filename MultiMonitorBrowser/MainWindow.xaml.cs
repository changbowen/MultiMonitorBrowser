using CefSharp;
using System;
using System.Collections.Generic;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Media.Imaging;
//using System.Windows.Navigation;
//using System.Windows.Shapes;

namespace MultiMonitorBrowser
{
    public partial class MainWindow : Window
    {
        public string ScriptAfterLoad { get; set; }
        public bool ScriptKeepRunning { get; set; } = false;

        private bool scriptExecuted = false;

        public KeyValuePair<string, string>[] EncryptedStrings { get; set; }
        public KeyValuePair<string, string> BasicAuth { get; set; }


        public int ID;
        public string DisplayDeviceName;
        public string Source;
        public DateTime UpdateTime;
 
        private double scaleFactor;
        public double ScaleFactor
        {
            get => scaleFactor;
            set { scaleFactor = value; Browser.ZoomLevel = value; }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWin_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Browser_FrameLoadEnd(object sender, CefSharp.FrameLoadEndEventArgs e)
        {
            Dispatcher.Invoke(() => Browser.ZoomLevel = ScaleFactor);

            if (ScriptAfterLoad == null) return;
            if (!ScriptKeepRunning && scriptExecuted) return;

            scriptExecuted = true;
            var task = e.Frame.EvaluateScriptAsync(App.ReplaceAndDecrypt(ScriptAfterLoad, EncryptedStrings)).ContinueWith(t =>
            {
                if (!t.IsFaulted)
                {
                    var response = t.Result;
#if DEBUG
                    Console.WriteLine(response.Success ? (response.Result ?? "null") : response.Message);
#endif
                }
            });
        }


        //public void UpdatePosition(NativeMethods.DisplayInfo dInfo)
        //{
        //    var hwnd = new System.Windows.Interop.WindowInteropHelper(this).EnsureHandle();
        //    var matrix = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice;
        //    if (!matrix.HasValue) return;
        //    var realRect = dInfo.MonitorArea;
        //    realRect.Transform(matrix.Value);
        //    Left = realRect.Left;
        //    Top = realRect.Top;
        //    Width = realRect.Width;
        //    Height = realRect.Height;
        //}
    }
}
