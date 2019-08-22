using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Timers;
using System.Windows;
using System.Xml.Linq;
using Microsoft.Win32;

namespace MultiMonitorBrowser
{
    public partial class App : Application
    {
        //public static readonly string AppDir = AppDomain.CurrentDomain.BaseDirectory;
        //public static readonly string AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        public static readonly string AppLaunchPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs) + @"\Carl Chang\" + nameof(MultiMonitorBrowser) + ".appref-ms";
        public static readonly string ConfigPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + nameof(MultiMonitorBrowser) + "_config.xml";
        private static byte[] EncryptKey;
        private static byte[] EncryptIV;
#if DEBUG
        private static readonly Timer timer = new Timer(20000); //20s
#else
        private static readonly Timer timer = new Timer(300000); //5 min
#endif
        private static FileSystemWatcher watcher;

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        private static XElement[] Monitors;
        private static NativeMethods.DisplayInfo[] Screens;

        public App()
        {
            //Monitor parent process exit and close subprocesses if parent process exits first
            //This will at some point in the future becomes the default
            CefSharpSettings.SubprocessExitIfParentProcessClosed = true;

            var settings = new CefSettings()
            {
                IgnoreCertificateErrors = true,
#if DEBUG
                //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                //CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
                LogFile = "Debug.log", //You can customise this path
                LogSeverity = LogSeverity.Verbose // You can change the log level
#endif
            };

            //settings.CefCommandLineArgs.Add("enable-media-stream", "1");
            //settings.SetOffScreenRenderingBestPerformanceArgs();
            //Cef.Initialize(settings);
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //create shortcut
            CreateShortcut(nameof(MultiMonitorBrowser) + " Encrypt", "-encrypt");

            //check config file
            if (!File.Exists(ConfigPath)) { RecreateConfig(); return; }

            //processing arguments
            if ((e.Args.Length > 0 && e.Args[0].Contains("encrypt")) ||
                (AppDomain.CurrentDomain.SetupInformation.ActivationArguments?.ActivationData?.Length > 0 &&
                 AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData[0].Contains("encrypt")))
            {
                //read encryption keys
                var config = XElement.Load(ConfigPath);
                var key = config.Attribute("encryptionKey")?.Value;
                var iv = config.Attribute("encryptionIV")?.Value;
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(iv)) { RecreateConfig(); return; }

                EncryptKey = Convert.FromBase64String(key);
                EncryptIV = Convert.FromBase64String(iv);
                new EncryptWindow().Show();
                return;
            }

            //initialize timer
            timer.Elapsed += CheckCycle;
#if (!DEBUG)
            timer.Start();
#endif

            //at last subscribe to display settings change
            SystemEvents.DisplaySettingsChanged += CheckCycle;

            //monitor config file change
            watcher = new FileSystemWatcher {
                Path = Path.GetDirectoryName(ConfigPath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                Filter = Path.GetFileName(ConfigPath),
            };
            watcher.Changed += CheckCycle;
            watcher.Renamed += CheckCycle;
            watcher.EnableRaisingEvents = true;

            //free resources when exit
            Current.Exit += (s, ee) => {
                SystemEvents.DisplaySettingsChanged -= CheckCycle;
                watcher.Changed -= CheckCycle;
                watcher.Renamed -= CheckCycle;
                timer.Elapsed -= CheckCycle;
            };

            //initialize windows for the first time
            CheckCycle();
        }

        /// <summary>
        /// This will provide a chance to reset the configuration file then exit.
        /// </summary>
        private void RecreateConfig()
        {
            var answer = MessageBox.Show(@"There was an error loading the configuration file. A new one can be created with default values. You need to apply your own customizations after.
Proceed with the creation of the configuration file? Please note that the old one will be overwritten.
Clicking No will open the existing configuration file.", nameof(MultiMonitorBrowser), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation);
            switch (answer)
            {
                case MessageBoxResult.Yes:
                    using (var cfgsmp = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(nameof(MultiMonitorBrowser) + ".config_sample.xml"))
                    {
                        XDocument.Load(cfgsmp).Save(ConfigPath);
                    }
                    System.Diagnostics.Process.Start("explorer.exe", ConfigPath);
                    break;
                case MessageBoxResult.No:
                    System.Diagnostics.Process.Start("explorer.exe", ConfigPath);
                    break;
                case MessageBoxResult.None:
                case MessageBoxResult.Cancel:
                default:
                    break;
            }

            if (Dispatcher.CheckAccess())
                Current.Shutdown();
            else
                Dispatcher.Invoke(() => Current.Shutdown());
        }

        private void CheckCycle(object sender = null, EventArgs e = null)
        {
            //when config file is changed, only fire when it is modified or renamed to
            if (e?.GetType() == typeof(FileSystemEventArgs) && ((FileSystemEventArgs)e).ChangeType != WatcherChangeTypes.Changed) return;
            else if (e?.GetType() == typeof(RenamedEventArgs) && ((RenamedEventArgs)e).FullPath != ConfigPath) return;

            try {
                watcher.EnableRaisingEvents = false;
#if DEBUG
                Console.WriteLine("Cycle firing at " + DateTime.Now);
#endif
                //(re)load configuration and update changes
                System.Threading.Thread.Sleep(1000);//wait a bit to avoid file locks
                var config = XElement.Load(ConfigPath);

                //hide and reset cursor position
                if (config.Attribute("hide-mouse")?.Value != "false")
                    Dispatcher.Invoke(() => System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.None);
                else
                    Dispatcher.Invoke(() => System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow);

                SetCursorPos(0, 0);

                //configure auto run
                if (config.Attribute("auto-run")?.Value == "true")
                    Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true)
                        .SetValue(nameof(MultiMonitorBrowser), "\"" + AppLaunchPath + "\"");
                else
                    Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true)
                        .DeleteValue(nameof(MultiMonitorBrowser), false);

                //update values
                EncryptKey = Convert.FromBase64String(config.Attribute("encryptionKey")?.Value);
                EncryptIV = Convert.FromBase64String(config.Attribute("encryptionIV")?.Value);

                Monitors = config.Elements("Monitor").ToArray();
                Screens = NativeMethods.GetDisplayMonitors(true);

                Dispatcher.Invoke(() =>
                {
                    var wins = Current.Windows.Cast<Window>().OfType<MainWindow>().ToDictionary(w => w.DisplayDeviceName);
                    var timestamp = DateTime.UtcNow;

                    //update windows
                    //for (int id = 0; id < Math.Min(Monitors.Length, Screens.Length); id++)
                    foreach (var mon in Monitors) {
                        //load config
                        var id = int.Parse(mon.Attribute("id").Value);
                        if (Screens.Length <= id) continue;

                        var scaling = mon.Attribute("scaling-override")?.Value;
                        var source = mon.Element("Source").Value.Trim();
                        var screen = Screens[id];

                        var encryStrs = mon.Element("EncryptedStrings")?.Elements().Select(
                            ele => new KeyValuePair<string, string>(ele.Attribute("name").Value, ele.Attribute("value").Value));

                        var scriptEle = mon.Element("ScriptAfterLoad");
                        var scriptBody = (scriptEle?.FirstNode as XCData)?.Value.Trim();
                        var scriptKeepRun = scriptEle?.Attribute("keep-running")?.Value;

                        var basAuthName = mon.Element("BasicAuth")?.Attribute("username")?.Value;
                        var basAuthPass = mon.Element("BasicAuth")?.Attribute("password")?.Value;

                        MainWindow win;
                        if (wins.ContainsKey(screen.DeviceName)) {
                            //make sure existing windows is at the correct position
                            win = wins[screen.DeviceName];
                            if (win.Source != source) {
                                win.Source = source;
                                win.Browser.Address = source;
                            }
                        }
                        else {
                            //create window if not already exist
                            win = new MainWindow
                            {
                                ID = id,
                                DisplayDeviceName = screen.DeviceName,
                                Source = source,
                                EncryptedStrings = encryStrs.ToArray()
                            };
                            win.Browser.Address = source;
                            if (basAuthName != null)
                                win.BasicAuth = new KeyValuePair<string, string>(basAuthName, basAuthPass);
                            if (scriptBody != null) {
                                win.ScriptAfterLoad = scriptBody;
                                if (scriptKeepRun != null && scriptKeepRun == "true") win.ScriptKeepRunning = true;
                            }
                            wins.Add(win.DisplayDeviceName, win);
                            win.Show();
                        }
                        win.Left = screen.MonitorArea.Left;
                        win.Top = screen.MonitorArea.Top;
                        win.Width = screen.MonitorArea.Width;
                        win.Height = screen.MonitorArea.Height;
                        if (double.TryParse(scaling, out double scaling_factor))
                            win.ScaleFactor = scaling_factor * 10 - 10;
                        else win.ScaleFactor = screen.Dpi / 96d * 10 - 10; //page scaling in high DPI
                        win.UpdateTime = timestamp;
#if (!DEBUG)
                        win.Topmost = true;
#endif
                    }
                    //remove windows not needed
                    foreach (var win in wins.Values) {
                        if (win.UpdateTime != timestamp) {
                            win.Close();
                        }
                    }
                    wins = null;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, nameof(MultiMonitorBrowser), MessageBoxButton.OK, MessageBoxImage.Error);
                RecreateConfig();
            }
            finally {
                watcher.EnableRaisingEvents = true;
            }
            
        }

        public static string Encrypt(string decryptedText)
        {
            byte[] encrypted;
            using (var rij = new RijndaelManaged() { Key = EncryptKey, IV = EncryptIV })
            {
                var encryptor = rij.CreateEncryptor();
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(decryptedText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            return Convert.ToBase64String(encrypted);
        }

        public static string Decrypt(string encryptedText)
        {
            string decrypted;
            using (var rij = new RijndaelManaged() { Key = EncryptKey, IV = EncryptIV })
            {
                var decryptor = rij.CreateDecryptor();
                using (var msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedText)))
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (var srDecrypt = new StreamReader(csDecrypt))
                        {
                            decrypted = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }
            return decrypted;
        }

        /// <summary>
        /// Returns new string with all instances of encrypted strings in textBody replaced with decrypted values.
        /// </summary>
        public static string ReplaceAndDecrypt(string textBody, IEnumerable<KeyValuePair<string, string>> encryptedStrings) {
            if (textBody?.Length > 0 && encryptedStrings.Any()) {
                foreach (var kvp in encryptedStrings)
                    textBody = textBody.Replace(kvp.Key, Decrypt(kvp.Value));
            }
            return textBody;
        }

        public static void CreateShortcut(string linkName, string args)
        {
            //var shDesktop = (object)"Desktop";
            var shell = new IWshRuntimeLibrary.WshShell();
            //var shortcutAddress = (string)shell.SpecialFolders.Item(ref shDesktop) + @"\Notepad.lnk";
            var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(Path.GetDirectoryName(AppLaunchPath) + "\\" + linkName + ".lnk");
            shortcut.Description = "Encrypt plain text for MultiMonitorBrowser.";
            //shortcut.Hotkey = "Ctrl+Shift+N";
            shortcut.TargetPath = AppLaunchPath;
            shortcut.Arguments = args;
            shortcut.Save();
        }
    }
}
