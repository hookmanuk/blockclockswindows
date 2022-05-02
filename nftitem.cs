using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockClocksWindows
{
    [System.Configuration.SettingsSerializeAsAttribute(System.Configuration.SettingsSerializeAs.String)]
    public class NFTItem
    {
        public string Asset { get; set; }
        public string NFT { get; set; }
        public int Opacity { get; set; }
        public bool BackgroundStyle { get; set; }
        public bool ShowUTC { get; set; }

        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public NFTItem()
        {
            Left = System.Windows.SystemParameters.PrimaryScreenHeight / 4;
            Top = System.Windows.SystemParameters.PrimaryScreenHeight / 4;
            Height = System.Windows.SystemParameters.PrimaryScreenHeight / 2;
            Width = System.Windows.SystemParameters.PrimaryScreenHeight / 2;
        }

        public override string ToString()
        {
            return NFT??"Unassigned";
        }
    }    
}
