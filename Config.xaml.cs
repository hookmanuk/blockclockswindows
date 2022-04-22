using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        public Config()
        {
            Instance = this;
            InitializeComponent();
            Topmost = true;

            Deactivated += Window_Deactivated;

            Clock.DataContext = MainWindow.Instance;
            WalletAddress.DataContext = MainWindow.Instance;

            Random r = new Random(Guid.NewGuid().GetHashCode());
            ADAAmount.Text = (((float)r.Next(20000, 21000))/((float)10000)).ToString();
            //ADAAmount.Text = "2.0019";

            if (MainWindow.Instance.ActiveClock?.onchain_metadata.type == "Aw0k3n Algorithm Special Edition Mashup" ||
                MainWindow.Instance.ActiveClock?.onchain_metadata.type == "Custom nft mashup")
            {
                TransparencyLabel.Visibility = Visibility.Visible;
                Transparency.Visibility = Visibility.Visible;
            }

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
                    MainWindow.Instance.Clocks.Clear();

                    checkcount = 0;
                    Status.Content = "Getting NFTs...";
                    AllowUIToUpdate();
                    List<assetinfo> assets = new List<assetinfo>();
                    getWalletAssetListPage(100, 1, assets);

                    Status.Content = MainWindow.Instance.Clocks.Count + " Clocks Found";

                    Clock.GetBindingExpression(ComboBox.ItemsSourceProperty)
                              .UpdateTarget();

                    Properties.Settings.Default.Clocks = JsonConvert.SerializeObject(MainWindow.Instance.Clocks);
                    Properties.Settings.Default.LinkedAddress = WalletAddress.Text;

                    Properties.Settings.Default.Save();
                }
                catch (Exception)
                {
                    Status.Content = "Error finding wallet";
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
                    Status.Content = "Checking NFT " + checkcount++;
                    AllowUIToUpdate();
                    CardanoManager.Instance.GetNFTInfoFromBlockchain(item.id, false, (contents) =>
                    {
                        if (!string.IsNullOrWhiteSpace(contents) && (
                            contents.Contains("\"policy_id\":\"ed6bde8a9c920d77e39b41db84381409dae6e2f6557e56ae97fcfe3b\"")
                        ))
                        {
                            clockassetcontents assetcontents = JsonConvert.DeserializeObject<clockassetcontents>(contents);
                            MainWindow.Instance.Clocks.Add(assetcontents);                        
                        }
                    });
                }

                MainWindow.Instance.Clocks = MainWindow.Instance.Clocks.OrderBy(c => c.name).ToList();

                callback(assets);
            });
        }

        

        private void Clock_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.ActiveClock = JsonConvert.SerializeObject(MainWindow.Instance.ActiveClock);
            
            if (MainWindow.Instance.ActiveClock.onchain_metadata.type == "Aw0k3n Algorithm Special Edition Mashup" ||
                MainWindow.Instance.ActiveClock.onchain_metadata.type == "Custom nft mashup")
            {
                MainWindow.Instance.ClockOpacity = 0;
                TransparencyLabel.Visibility = Visibility.Visible;
                Transparency.Visibility = Visibility.Visible;
            }
            else
            {
                MainWindow.Instance.ClockOpacity = 255;
                TransparencyLabel.Visibility = Visibility.Collapsed;
                Transparency.Visibility = Visibility.Collapsed;                
            }            

            if (Convert.ToInt32(Transparency.Value) != MainWindow.Instance.ClockOpacity)
            {
                Transparency.Value = MainWindow.Instance.ClockOpacity;
                Properties.Settings.Default.ClockOpacity = MainWindow.Instance.ClockOpacity;
                Properties.Settings.Default.Save();
            }
            else
            {
                MainWindow.Instance.UpdateClock();            
                Properties.Settings.Default.Save();
            }
        }       

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }       

        private void Transparency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MainWindow.Instance.ClockOpacity= Convert.ToInt32(e.NewValue);
            Properties.Settings.Default.ClockOpacity = MainWindow.Instance.ClockOpacity;

            Properties.Settings.Default.Save();
       
            MainWindow.Instance.UpdateClock();
        }

        private void ShowUTC_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.UTCDetails = true;

            Properties.Settings.Default.Save();

            MainWindow.Instance.UpdateClock();
        }

        private void ShowUTC_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.UTCDetails = false;

            Properties.Settings.Default.Save();

            MainWindow.Instance.UpdateClock();
        }

        private void Refresh_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            FindNFTs();
        }

        private void AlwaysOnTop_Checked(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.SetToForeground();
        }

        private void AlwaysInBackground_Checked(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.SetToBackground();
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
            MainWindow.Instance.Topmost = true;
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
