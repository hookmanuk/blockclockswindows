using BlockClocksWindows;
using CefSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BlockClocksWindows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; set; }
        Timer timer;
        Timer timerHover;
        private int ResetCount;       
        
        public string LinkedAddress { get; set; }
        public int ClockOpacity { get; set; }        
        public BlockClocksWindows.WindowSinker WS { get; set; }
        public appconfig AppConfig { get; set; }

        public NFTItem NFTItem { get; set; }

        //https://www.reddit.com/r/ipfs/comments/lvwn4o/ipfs_http_gateways_ranked_by_performance/
        //public const string IMAGEHOSTPREFIX = "https://ipfs.blockfrost.dev/ipfs/";
        //public const string IMAGEHOSTPREFIX = "https://ipfs.io/ipfs/";
        //public const string IMAGEHOSTPREFIX = "https://infura-ipfs.io/ipfs/";
        //public const string IMAGEHOSTPREFIX = "https://cloudflare-ipfs.com/ipfs/";

        private int prefixcount = 0;
        private string[] ImageHostPrefixes = {
            "https://nftstorage.link/ipfs/",
            "https://gateway.ipfs.io/ipfs/",          
            "https://ipfs.io/ipfs/",                        
            "https://dweb.link/ipfs/",
        };

        private string GetNextPrefix()
        {
            prefixcount++;
            if (prefixcount > ImageHostPrefixes.Length - 1)
            {
                prefixcount = 0;
            }
            return ImageHostPrefixes[prefixcount];
        }


        public MainWindow(NFTItem item)
        {
            Instance = this;

            NFTItem = item;

            new CardanoManager();            
            InitializeComponent();
            Topmost = true;
            ShowInTaskbar = false;

            Deactivated += Window_Deactivated;
            PreviewMouseDown += Window_MouseDown;            
            PreviewMouseUp += Window_MouseUp;

            timerHover = new Timer(2000);
            timerHover.AutoReset = true;
            timerHover.Elapsed += TimerHover_Elapsed;
            timerHover.Start();            
            
            ClockOpacity = Properties.Settings.Default.ClockOpacity;                        

            string linkedAddress = Properties.Settings.Default.LinkedAddress;
            if (!string.IsNullOrWhiteSpace(linkedAddress))
            {
                LinkedAddress = linkedAddress;
            }

            PositionClock();                        
            
            this.SourceInitialized += new EventHandler(OnSourceInitialized); 

            if (Properties.Settings.Default.BackgroundStyle)
            {
                SetToBackground();
            }

            try
            {
                string strAppConfig = System.IO.File.ReadAllText("config.ini");
                AppConfig = JsonConvert.DeserializeObject<appconfig>(strAppConfig);
            }
            catch (Exception)
            {
                AppConfig = new appconfig() { policyids = new string[] { BlockClocksWindows.Config.POLICYCLOCK } };
            }            
        }

        public void SetToBackground()
        {
            WS = new BlockClocksWindows.WindowSinker(this);
        }

        public void SetToForeground()
        {
            if (WS != null)
            {
                WS.DisableSinker();
                WS.Dispose();
            }
            this.Focus();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.R)
            {
                ResetCount++;

                if (ResetCount > 0)
                {
                    Properties.Settings.Default.Height = 0;
                    PositionClock();
                }
            }
        }

        private void PositionClock()
        {
            if (NFTItem.Top <= 0 ||
                NFTItem.Left <= 0 ||
                NFTItem.Height <= 0)
            {
                NFTItem.Left = System.Windows.SystemParameters.PrimaryScreenHeight / 4;
                NFTItem.Top = System.Windows.SystemParameters.PrimaryScreenHeight / 4;
                NFTItem.Height = System.Windows.SystemParameters.PrimaryScreenHeight / 2;                
            }

            this.Top = NFTItem.Top;
            this.Left = NFTItem.Left;
            this.Height = NFTItem.Height;
            this.Width = NFTItem.Height;
        }

        public void UpdateClock()
        {
            clockassetcontents clock = App.Instance.Clocks.FirstOrDefault(c => c.asset == NFTItem.Asset);
            if (clock != null)
            {
                SetClock(clock);
            }
        }

        public void SetClock(clockassetcontents clock)
        {
            string base64 = "";
            bool processed = false;
            if (clock.onchain_metadata != null && clock.onchain_metadata.files != null && clock.onchain_metadata.files.GetType().Name == "JArray")
            {
                var src = ((JArray)clock.onchain_metadata.files)[0]["src"];
                if (src != null)
                {
                    foreach (var item in src)
                    {
                        base64 += item;
                    }

                    string decodedString = null;

                    if (base64.StartsWith("data:text/html;utf8,"))
                    {
                        decodedString = base64.Substring(20);
                    }
                    else if (base64.Contains("data:text/html;base64,"))
                    {
                        base64 = base64.Replace("data:text/html;base64,", "");
                        byte[] data = Convert.FromBase64String(base64);
                        decodedString = Encoding.UTF8.GetString(data);
                    }                    

                    if (decodedString != null)
                    {
                        

                        //circle background colour;
                        string circle = $"ctx.translate(r+o, r+o);ctx.beginPath();ctx.arc(0, 0, r, 0, 2 * Math.PI);ctx.fillStyle = '{("#000000" + ClockOpacity.ToString("X").PadLeft(2, Convert.ToChar("0"))) ?? "#000"}';ctx.fill();ctx.translate(-r-o, -r-o);";

                        //needs code for circle background to not be transparent for some clocks
                        //if (clock.onchain_metadata.type == "Aw0k3n Algorithm Special Edition Mashup")
                        //{
                        decodedString = decodedString.Replace("var backc = '#000'", "var backc = '#0000'")
                                .Replace("var backc = '#EEE'", "var backc = '#EEE0'")
                                .Replace("ctx.fillStyle= backc;", circle + "ctx.strokeStyle=backc;ctx.fillStyle= backc;");
                        //}            

                        if (Properties.Settings.Default.UTCDetails)
                        {
                            decodedString = decodedString.Replace("</body>", "<script>initnft({ showinfo:true });</script></body>");
                        }

                        var plainTextBytes = Encoding.UTF8.GetBytes(decodedString);
                        base64 = "data:text/html;base64," + Convert.ToBase64String(plainTextBytes);

                        Browser.Visibility = Visibility.Visible;
                        Browser.Load(base64);

                        processed = true;

                        Image.Visibility = Visibility.Collapsed;
                    }
                }
            }
            if (!processed)
            {
                if (clock.ipfshash != null)
                {
                    string uri = GetNextPrefix() + clock.ipfshash;

                    getImage(uri);

                    processed = true;

                    Browser.Visibility = Visibility.Collapsed;
                }
            }

            if (processed)
            {
                ImagePlaceholder.Visibility = Visibility.Visible;
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("pack://application:,,/spinner-solid.png");
                bitmap.EndInit();
                ImagePlaceholder.Source = bitmap;                
            }
        }

        private void getImage(string uri)
        {
            // Create a BitmapSource  
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(uri);            
            bitmap.EndInit();            
            bitmap.DownloadCompleted += Bitmap_DownloadCompleted;
            bitmap.DownloadFailed += Bitmap_DownloadFailed;

            Image.Visibility = Visibility.Visible;
            Image.Source = bitmap;
        }

        private int _retries = 0;
        private void Bitmap_DownloadFailed(object sender, ExceptionEventArgs e)
        {
            _retries++;

            if (_retries < 20)
            {
                try
                {
                    //System.IO.Exception
                    var ex = e.ErrorException;
                    var webex = (System.Net.WebException)e.ErrorException;
                    var response = webex.Response.ResponseUri;
                    var uri = ((System.Net.HttpWebResponse)((System.Net.WebException)e.ErrorException).Response).ResponseUri.AbsoluteUri;

                    for (int i = 0; i < ImageHostPrefixes.Length; i++)
                    {
                        if (uri.StartsWith(ImageHostPrefixes[i]))
                        {
                            uri = uri.Substring(ImageHostPrefixes[i].Length);
                            var newprefixindex = i + 1;
                            if (newprefixindex > ImageHostPrefixes.Length - 1)
                            {
                                newprefixindex = 0;
                            }
                            uri = ImageHostPrefixes[newprefixindex] + uri;
                        }
                    }
                    getImage(uri);
                }
                catch (Exception)
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri("pack://application:,,/circle-exclamation-solid.png");
                    bitmap.EndInit();
                    ImagePlaceholder.Source = bitmap;
                }                
            }
            else
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("pack://application:,,/circle-exclamation-solid.png");
                bitmap.EndInit();
                ImagePlaceholder.Source = bitmap;
            }
            //throw new NotImplementedException();
        }

        private void Bitmap_DownloadCompleted(object sender, EventArgs e)
        {
            ImagePlaceholder.Visibility = Visibility.Collapsed;
        }

        private void TimerHover_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (this.IsMouseOver)
                {
                    if (Minus.Visibility == Visibility.Hidden)
                    {
                        Minus.Visibility = Visibility.Visible;
                        Plus.Visibility = Visibility.Visible;
                        Config.Visibility = Visibility.Visible;                        
                    }
                }
                else
                {
                    if (Minus.Visibility == Visibility.Visible)
                    {
                        Minus.Visibility = Visibility.Hidden;
                        Plus.Visibility = Visibility.Hidden;
                        Config.Visibility = Visibility.Hidden;                        
                    }
                }                
            }));
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            // The Window was deactivated 
            //Topmost = false; // set topmost false first
            //Topmost = true; // then set topmost true again.
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && (e.Source == Browser || e.Source == Image || e.Source == ImagePlaceholder))
                this.DragMove();

            if (App.Instance.ConfigDialog?.Visibility == Visibility.Visible)
            {
                App.Instance.ConfigDialog.ChangeActiveWindow(this);
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            timer?.Stop();
        }

        private void Minus_Click(object sender, RoutedEventArgs e)
        {
            timer = new Timer(100);
            timer.AutoReset = true;
            timer.Elapsed += Timer_Elapsed_Minus;
            timer.Start();
            Timer_Elapsed_Minus(sender, null);            
        }

        private void Timer_Elapsed_Minus(object sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                this.Width = this.Width * 0.98;
                this.Height = this.Height * 0.98;                
            }));
            
        }

        private void Plus_Click(object sender, RoutedEventArgs e)
        {
            timer = new Timer(100);
            timer.AutoReset = true;
            timer.Elapsed += Timer_Elapsed_Plus;
            timer.Start();
            Timer_Elapsed_Plus(sender, null);
        }

        private void Timer_Elapsed_Plus(object sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                this.Width = this.Width * 1.02;
                this.Height = this.Height * 1.02;
            }));

        }

        private void Config_Click(object sender, MouseButtonEventArgs e)
        {            
            App.Instance.ShowConfig();
        }

        

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Properties.Settings.Default.Top = this.Top;
            //Properties.Settings.Default.Left = this.Left;
            //Properties.Settings.Default.Height = this.Height;
            //Properties.Settings.Default.Width = this.Width;

            //Properties.Settings.Default.Save();
        }

        private void Quit_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            HwndSource source = (HwndSource)PresentationSource.FromVisual(this);
            source.AddHook(new HwndSourceHook(HandleMessages));
        }

        private IntPtr HandleMessages(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // 0x0112 == WM_SYSCOMMAND, 'Window' command message.
            // 0xF020 == SC_MINIMIZE, command to minimize the window.
            if (msg == 0x0112 && ((int)wParam & 0xFFF0) == 0xF020)
            {
                // Cancel the minimize.
                handled = true;                
            }

            return IntPtr.Zero;
        }       

        private void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                ImagePlaceholder.Visibility = Visibility.Collapsed;
            }));
        }
    }
}
