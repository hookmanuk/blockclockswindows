using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace BlockClocksWindows
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private System.Windows.Forms.NotifyIcon m_notifyIcon;
        public List<clockassetcontents> Clocks { get; set; } = new List<clockassetcontents>();

        public clockassetcontents ActiveClock { get; set; }

        public static App Instance { get; set; }

        public List<MainWindow> MainWindows { get; set; } = new List<MainWindow>();

        public Config ConfigDialog { get; set; }

        public bool ConfigShown { get; set; }

        public App()
        {
            Instance = this;

            string clocks = BlockClocksWindows.Properties.Settings.Default.Clocks;

            if (!string.IsNullOrWhiteSpace(clocks))
            {
                Clocks = JsonConvert.DeserializeObject<List<clockassetcontents>>(clocks)?.OrderBy(c => c.name).ToList();
            }

            if (BlockClocksWindows.Properties.Settings.Default.NFTItems.Count == 0)
            {
                ShowConfig();

                BlockClocksWindows.Properties.Settings.Default.NFTItems.Add(
                    new NFTItem());
            }

            foreach (var item in BlockClocksWindows.Properties.Settings.Default.NFTItems)
            {
                AddWindow(item);
            }

            m_notifyIcon = new System.Windows.Forms.NotifyIcon();            
            m_notifyIcon.BalloonTipTitle = "Cardano Block Clocks";
            m_notifyIcon.Text = "Cardano Block Clocks";
            m_notifyIcon.Icon = new System.Drawing.Icon("logoicon_kiI_icon.ico");
            m_notifyIcon.Click += new EventHandler(m_notifyIcon_Click);
            m_notifyIcon.Visible = true;

            //how to do onboarding??
            //ShowConfig();            
        }

        void m_notifyIcon_Click(object sender, EventArgs e)
        {            
            ShowConfig();            
        }

        public void AddWindow(NFTItem item)
        {
            MainWindow window1 = new MainWindow(item);
            window1.Show();
            window1.UpdateClock();
            MainWindows.Add(window1);
            
            window1.Topmost = !item.BackgroundStyle;            
        }

        public void ShowConfig()
        {
            if (!ConfigShown)
            {
                foreach (var item in MainWindows)
                {
                    item.Topmost = false;
                }
                ConfigDialog = new Config();
                ConfigDialog.Show();

                ConfigShown = true;
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            foreach (var window in MainWindows)
            {
                NFTItem nft = BlockClocksWindows.Properties.Settings.Default.NFTItems.FirstOrDefault(n => n == window.NFTItem);

                nft.Top = window.Top;
                nft.Left = window.Left;
                nft.Width = window.Width;
                nft.Height = window.Height;
            }
            
            BlockClocksWindows.Properties.Settings.Default.Save();
        }
    }
}
