using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BlockClocksWindows
{
    /// <summary>
    /// Interaction logic for Config.xaml
    /// </summary>
    public partial class Config : Window
    {
        public static Config Instance { get; set; }        
        private int checkcount = 0;
        private int WalletSecretCount = 0;
        public const string POLICYCLOCK = "ed6bde8a9c920d77e39b41db84381409dae6e2f6557e56ae97fcfe3b";
        public NFTItem ActiveItem { get; set; }
        public MainWindow ActiveWindow { get { return App.Instance.MainWindows.FirstOrDefault(w => w.NFTItem == ActiveItem); } }

        public Config()
        {
            Instance = this;
            InitializeComponent();
            Topmost = true;

            Deactivated += Window_Deactivated;

            Clock.DataContext = App.Instance;
            WalletAddress.DataContext = MainWindow.Instance;

            Random r = new Random(Guid.NewGuid().GetHashCode());
            ADAAmount.Text = (((float)r.Next(20000, 21000))/((float)10000)).ToString();
            //ADAAmount.Text = "2.0019";

            //if (App.Instance.ActiveClock?.onchain_metadata.type == "Aw0k3n Algorithm Special Edition Mashup" ||
            //    App.Instance.ActiveClock?.onchain_metadata.type == "Custom nft mashup")
            //{
            //    TransparencyLabel.Visibility = Visibility.Visible;
            //    Transparency.Visibility = Visibility.Visible;
            //}

            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.LinkedAddress))
            {
                Tabs.SelectedItem = LayoutTab;             
            }

            this.DataContext = this.ActiveItem;

            Clock.GetBindingExpression(ComboBox.SelectedValueProperty)
                             .UpdateTarget();            
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            // The Window was deactivated 
            Topmost = false; // set topmost false first
            Topmost = true; // then set topmost true again.
        }

        void AllowUIToUpdate()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new DispatcherOperationCallback(delegate (object parameter)
            {
                frame.Continue = false;
                return null;
            }), null);

            Dispatcher.PushFrame(frame);
            //EDIT:
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                                          new Action(delegate { }));
        }

        private void WalletAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            FindNFTs();
        }

        private void FindNFTs()
        {
            if (!string.IsNullOrWhiteSpace(WalletAddress.Text))
            {
                try
                {
                    App.Instance.Clocks.Clear();

                    checkcount = 0;
                    Status.Content = "Getting NFTs...";
                    AllowUIToUpdate();
                    List<assetinfo> assets = new List<assetinfo>();
                    getWalletAssetListPage(100, 1, assets);

                    if (!App.Instance.Clocks.Exists(c => c.policy_id == POLICYCLOCK))
                    {
                        App.Instance.Clocks.Clear();
                    }

                    Status.Content = App.Instance.Clocks.Count + " compatible NFTs Found";

                    Clock.GetBindingExpression(ComboBox.ItemsSourceProperty)
                              .UpdateTarget();

                    Properties.Settings.Default.Clocks = JsonConvert.SerializeObject(App.Instance.Clocks);
                    Properties.Settings.Default.LinkedAddress = WalletAddress.Text;

                    Properties.Settings.Default.Save();
                }
                catch (Exception ex)
                {
                    Status.Content = "Error getting NFTs " + ex.Message;
                }
            }
        }

        private void TransactionAddress_LostFocus(object sender, RoutedEventArgs e)
        {
                        
            if (!string.IsNullOrWhiteSpace(TransactionAddress.Text))
            {
                try
                {
                    CardanoManager.Instance.GetTransactionUTXOs(TransactionAddress.Text,
                    (transactionResult) =>
                    {
                        bool blnValidAmount = false;
                        string amountToFind = Convert.ToInt32((Convert.ToDecimal(ADAAmount.Text) * 1000000)).ToString();
                        //amountToFind = "2114100";
                        //tx = 514a5a6dc726c2088f0b76629de60365c5cc3ab1e6151728f96973040778a0a8

                        foreach (var item in transactionResult.outputs)
                        {
                            if (item.amount[0].quantity == amountToFind)
                            {
                                blnValidAmount = true;
                                break;
                            }
                        }

                        if (blnValidAmount)
                        {
                            Status.Content = "Transaction amount valid, looking up wallet details...";

                            string address = transactionResult.inputs[0].address;

                            CardanoManager.Instance.GetWalletStakeFromAddress(address, (stake) =>
                            {
                                WalletAddress.Text = stake;
                                Properties.Settings.Default.LinkedAddress = stake;
                                FindNFTs();
                            },
                            (error) =>
                            {
                                Status.Content = "!! Transaction not valid " + error + " !!";
                            });
                        }
                        else
                        {
                            Status.Content = "!! Transaction amount does not match !!";
                        }
                    },
                    (error) =>
                    {
                        Status.Content = "!! Transaction not yet found, please try again in a minute !!";
                    });
                }
                catch (Exception)
                {
                    Status.Content = "Error with transaction";
                }
            }
        }

        private void getWalletAssetListPage(int count, int page, List<assetinfo> assets)
        {
            GetWalletAssetListFromBlockchain(count, page, (List<assetinfo> assetspage) => {
                assets.AddRange(assetspage);
                if (assetspage.Count == count)
                {
                    getWalletAssetListPage(count, page + 1, assets);
                }                
            });
        }

        private void GetWalletAssetListFromBlockchain(int count, int page, System.Action<List<assetinfo>> callback)
        {
            CardanoManager.Instance.GetWalletAssetListFromBlockchain(WalletAddress.Text, "asc", count, page, (assets) =>
            {
                foreach (assetinfo item in assets)
                {
                    checkcount++;
                    Status.Content = "Checking NFT " + checkcount;
                    AllowUIToUpdate();                    

                    Task t = Task.Run(async delegate
                    {
                        await Task.Delay(1);
                        CardanoManager.Instance.GetNFTInfoFromBlockchain(item.id, false, (contents) =>
                        {
                            //bool valid = false;
                            //foreach (var policy in MainWindow.Instance.AppConfig.policyids)
                            //{
                            //    if (contents.Contains($"\"policy_id\":\"{policy}\""))
                            //    {
                            //        valid = true;
                            //        break;
                            //    }
                            //}
                            if (!string.IsNullOrWhiteSpace(contents))
                            {
                                try
                                {
                                    clockassetcontents assetcontents = JsonConvert.DeserializeObject<clockassetcontents>(contents);
                                    CardanoManager.Instance.ParseMetadata(assetcontents, contents);
                                    App.Instance.Clocks.Add(assetcontents);                                    
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine("Errored");
                                    //couldn't decode                                
                                }

                            }
                            else
                            {
                                Debug.WriteLine("No content");
                                //nothing here??
                            }
                        });
                    });
                    t.Wait(500);
                }                

                App.Instance.Clocks = App.Instance.Clocks.OrderBy(c => c.name).ToList();

                callback(assets);
            });
        }

        

        private void Clock_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ActiveItem = (NFTItem)NFTItems.SelectedItem;
            if (App.Instance.ActiveClock != null && ActiveItem != null)
            {
                ActiveItem.NFT = App.Instance.ActiveClock.name;
                ActiveItem.Asset = App.Instance.ActiveClock.asset;

                JsonConvert.SerializeObject(App.Instance.ActiveClock);

                if (App.Instance.ActiveClock.onchain_metadata != null &&
                    (App.Instance.ActiveClock.onchain_metadata.type == "Aw0k3n Algorithm Special Edition Mashup" ||
                    App.Instance.ActiveClock.onchain_metadata.type == "Custom nft mashup"))
                {
                    ActiveWindow.ClockOpacity = 0;
                    TransparencyLabel.Visibility = Visibility.Visible;
                    Transparency.Visibility = Visibility.Visible;
                }
                else
                {
                    ActiveWindow.ClockOpacity = 255;
                    TransparencyLabel.Visibility = Visibility.Collapsed;
                    Transparency.Visibility = Visibility.Collapsed;
                }

                ActiveWindow.UpdateClock();
                Properties.Settings.Default.Save();

                CheckIsClock();
                RefreshList();
            }
        }       

        private void CheckIsClock()
        {
            if (App.Instance.ActiveClock.policy_id == POLICYCLOCK)
            {
                ShowUTCLabel.Visibility = Visibility.Visible;
                ShowUTC.Visibility = Visibility.Visible;
            }
            else
            {
                ShowUTCLabel.Visibility = Visibility.Collapsed;
                ShowUTC.Visibility = Visibility.Collapsed;
            }
        }       

        private void Transparency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ActiveWindow.ClockOpacity= Convert.ToInt32(e.NewValue);
            Properties.Settings.Default.ClockOpacity = ActiveWindow.ClockOpacity;

            Properties.Settings.Default.Save();

            ActiveWindow?.UpdateClock();
        }

        private void ShowUTC_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.UTCDetails = true;

            Properties.Settings.Default.Save();

            ActiveWindow?.UpdateClock();
        }

        private void ShowUTC_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.UTCDetails = false;

            Properties.Settings.Default.Save();

            ActiveWindow?.UpdateClock();
        }

        private void Refresh_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            FindNFTs();
        }

        private void AlwaysOnTop_Checked(object sender, RoutedEventArgs e)
        {
            ActiveWindow?.SetToForeground();
            ActiveItem.BackgroundStyle = false;
            Properties.Settings.Default.Save();
        }

        private void AlwaysInBackground_Checked(object sender, RoutedEventArgs e)
        {
            ActiveWindow?.SetToBackground();
            ActiveItem.BackgroundStyle = true;
            Properties.Settings.Default.Save();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {            
            this.Activate();
            this.Topmost = false;
            this.Topmost = true;
            this.Focus();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var item in App.Instance.MainWindows)
            {
                NFTItem nft = Properties.Settings.Default.NFTItems.Where(n => n == item.NFTItem).FirstOrDefault();
                if (!nft.BackgroundStyle)
                {
                    item.Topmost = true;
                }
            }            
        }

        private void AddWindow_Click(object sender, RoutedEventArgs e)
        {
            NFTItem item = new NFTItem();
            if (AlwaysOnTop.IsChecked.HasValue)
            {
                item.BackgroundStyle = !AlwaysOnTop.IsChecked.Value;                
            }
            Properties.Settings.Default.NFTItems.Add(item);

            App.Instance.AddWindow(item);

            RefreshList(true);
        }

        private void RemoveWindow_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveItem != null && ActiveWindow != null)
            {
                Properties.Settings.Default.NFTItems.Remove(ActiveItem);
                ActiveWindow.Close();
                App.Instance.MainWindows.Remove(ActiveWindow);
                

                RefreshList(true);
            }            
        }

        public void ChangeActiveWindow(MainWindow window)
        {            
            ActiveItem = window.NFTItem;
            NFTItems.SelectedItem = ActiveItem;
        }

        private void RefreshList(bool updateSelected = false)
        {
            NFTItems.Items.Refresh();

            if (updateSelected)
            {
                if (NFTItems.Items.Count > 0)
                {
                    NFTItems.SelectedItem = NFTItems.Items[NFTItems.Items.Count - 1];
                }
            }

            Properties.Settings.Default.Save();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            // Begin dragging the window
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Close_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
            App.Instance.ConfigShown = false;
        }

        private void NFTItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ActiveItem = (NFTItem)NFTItems.SelectedItem;

            if (ActiveItem != null)
            {

                App.Instance.ActiveClock = App.Instance.Clocks.Where(c => c.name == ActiveItem.NFT).FirstOrDefault();

                if (App.Instance.ActiveClock != null)
                {
                    Clock.GetBindingExpression(ComboBox.SelectedValueProperty)
                                      .UpdateTarget();

                    AlwaysOnTop.IsChecked = !ActiveItem.BackgroundStyle;
                    AlwaysInBackground.IsChecked = ActiveItem.BackgroundStyle;
                }
            }
        }

        private void QuitApp_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            App.Instance.Shutdown();
        }
    }

    public class BoolRadioConverter : IValueConverter
    {
        public bool Inverse { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = (bool)value;

            return this.Inverse ? !boolValue : boolValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = (bool)value;

            if (!boolValue)
            {
                // We only care when the user clicks a radio button to select it.
                return null;
            }

            return !this.Inverse;
        }
    }
}
