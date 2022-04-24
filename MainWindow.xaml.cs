using BlockClocksWindows;
using CefSharp;
using Newtonsoft.Json;
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

        public clockassetcontents ActiveClock { get; set; }
        public List<clockassetcontents> Clocks { get; set; } = new List<clockassetcontents>();  
        public string LinkedAddress { get; set; }
        public int ClockOpacity { get; set; }
        public bool ConfigShown { get; set; }
        public BlockClocksWindows.WindowSinker WS { get; set; }
        public appconfig AppConfig { get; set; }


        public MainWindow()
        {
            Instance = this;
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

            string clocks = Properties.Settings.Default.Clocks;

            if (!string.IsNullOrWhiteSpace(clocks))
            {
                Clocks = JsonConvert.DeserializeObject<List<clockassetcontents>>(clocks)?.OrderBy(c => c.name).ToList();
            }
            
            ClockOpacity = Properties.Settings.Default.ClockOpacity;            

            string activeclock = Properties.Settings.Default.ActiveClock;
            if (!string.IsNullOrWhiteSpace(activeclock))
            {
                ActiveClock = JsonConvert.DeserializeObject<clockassetcontents>(activeclock);
                UpdateClock();
            }

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
            if (Properties.Settings.Default.Top <= 0 ||
                Properties.Settings.Default.Left <= 0 ||
                Properties.Settings.Default.Height <= 0)
            {
                Properties.Settings.Default.Left = System.Windows.SystemParameters.PrimaryScreenHeight / 4;
                Properties.Settings.Default.Top = System.Windows.SystemParameters.PrimaryScreenHeight / 4;
                Properties.Settings.Default.Height = System.Windows.SystemParameters.PrimaryScreenHeight / 2;                
            }

            this.Top = Properties.Settings.Default.Top;
            this.Left = Properties.Settings.Default.Left;
            this.Height = Properties.Settings.Default.Height;
            this.Width = Properties.Settings.Default.Height;
        }

        public void UpdateClock()
        {
            SetClock(ActiveClock);
        }

        public void SetClock(clockassetcontents clock)
        {
            string base64 = "";
            foreach (var item in clock.onchain_metadata.files[0].src)
            {
                base64 += item;
            }

            byte[] data = Convert.FromBase64String(base64.Replace("data:text/html;base64,",""));
            string decodedString = Encoding.UTF8.GetString(data);            

            //circle background colour;
            string circle = $"ctx.translate(r+o, r+o);ctx.beginPath();ctx.arc(0, 0, r, 0, 2 * Math.PI);ctx.fillStyle = '{("#000000" + ClockOpacity.ToString("X").PadLeft(2,Convert.ToChar("0"))) ?? "#000"}';ctx.fill();ctx.translate(-r-o, -r-o);";

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

            Browser.Load(base64);

            ActiveClock = clock;
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
                        Quit.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    if (Minus.Visibility == Visibility.Visible)
                    {
                        Minus.Visibility = Visibility.Hidden;
                        Plus.Visibility = Visibility.Hidden;
                        Config.Visibility = Visibility.Hidden;
                        Quit.Visibility = Visibility.Hidden;
                    }
                }

                if (ActiveClock == null && !ConfigShown)
                {
                    ShowConfig();
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
            if (e.ChangedButton == MouseButton.Left && e.Source == Browser)
                this.DragMove();
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
            ShowConfig();
        }

        private void ShowConfig()
        {
            Topmost = false;
            ConfigShown = true;
            Config ConfigDialog = new Config();
            ConfigDialog.Show();         
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Top = this.Top;
            Properties.Settings.Default.Left = this.Left;
            Properties.Settings.Default.Height = this.Height;
            Properties.Settings.Default.Width = this.Width;

            Properties.Settings.Default.Save();
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
    }
}
