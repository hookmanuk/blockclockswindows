using Newtonsoft.Json;

namespace BlockClocksWindows
{
    public class assetinfo
    {
        public string unit;
        public string asset;
        public string quantity;

        public string id { get { return asset ?? unit; } }
        public int retries;

        public assetinfo()
        {
            retries = 0;
        }
    }

    public class clockassetcontents
    {
        public string asset { get; set; }
        public string policy_id { get; set; }
        public string asset_name { get; set; }
        public string fingerprint { get; set; }
        public string quantity { get; set; }
        public string initial_mint_tx_hash { get; set; }
        public int mint_or_burn_count { get; set; }
        public Onchain_Metadata onchain_metadata { get; set; }
        public object metadata { get; set; }

        public string name { get { return onchain_metadata.name + " - " + onchain_metadata.background; } }
    }

    public class Onchain_Metadata
    {
        public string name { get; set; }
        public string image { get; set; }
        public string type { get; set; }
        public File[] files { get; set; }
        public string hands { get; set; }
        public string artist { get; set; }
        public string medium { get; set; }
        public string authNFT { get; set; }
        public string numbers { get; set; }
        public string twitter { get; set; }
        public string mediaType { get; set; }
        public string background { get; set; }
    }

    public class File
    {
        public string[] src { get; set; }
        public string name { get; set; }
        public string mediaType { get; set; }
    }

}



