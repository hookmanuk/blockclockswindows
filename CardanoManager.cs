using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace BlockClocksWindows
{
    public partial class CardanoManager
    {
        public static CardanoManager Instance { get; set; }
        const string APIROOT = "https://cardano-mainnet.blockfrost.io/api/v0/";
        const string JSONPOLICYFILENAMEFORMAT = "policy/{0}-{1}.json";
        const string JSONWALLETFILENAMEFORMAT = "wallet/{0}-{1}.json";
        const string JSONASSETFILENAMEFORMAT = "asset/{0}.json";        

        public CardanoManager()
        {
            Instance = this;            
        }
    
        public void GetNFTDetails(string asset, bool cache, System.Action<string, string, JToken, string, string> callback)
        {
            GetNFTInfo(asset, cache, (info) =>
            {
                ProcessDetails(info, callback);
            });
        }

        public void ProcessDetails(string info, System.Action<string, string, JToken, string, string> callback)
        {
            JObject jsoninfo = JObject.Parse(info);
            JToken onchainmetadata = jsoninfo.SelectToken("onchain_metadata");
            string encodeddata = null;
            string videohash = null;
            if (onchainmetadata != null)
            {
                JToken src = onchainmetadata.SelectToken("files[0].src");
                if (src != null)
                {
                    if (onchainmetadata.SelectToken("files[0].mediaType")?.ToString() == "video/mp4")
                    {
                        videohash = src.ToString();

                        videohash = Stripipfs(videohash);
                    }
                    else
                    {
                        if (src.Type.ToString() == "Array")
                        {
                            var filesrc = onchainmetadata.SelectToken("files[0].src")?.ToObject<string[]>();

                            if (filesrc != null)
                            {
                                encodeddata = string.Concat(filesrc).Replace("data:text/html;base64,", "");
                            }
                        }
                    }
                }
            }

            string image = FindImageIPFSHash(jsoninfo);

            callback(image, FindName(jsoninfo), onchainmetadata, encodeddata, videohash);
        }

        private string FindImageIPFSHash(JObject jobject)
        {
            try
            {
                if (jobject.SelectToken("onchain_metadata.image")?.Type == JTokenType.Array)
                {
                    //sometimes an onchain array of the file... how to handle this??
                    return null;
                }
                else
                {
                    string image = jobject.SelectToken("onchain_metadata.image")?.Value<string>();

                    if (image != null)
                    {
                        return Stripipfs(image);
                    }

                    foreach (var item in jobject)
                    {
                        if (item.Value.GetType() == typeof(JObject))
                        {
                            var stringFound = FindImageIPFSHash((JObject)item.Value);

                            if (stringFound != null)
                            {
                                return stringFound;
                            }
                        }
                        else
                        {
                            if (item.Value.Type.ToString() == "Array")
                            {
                                foreach (var arrayItem in ((JContainer)item.Value).Children())
                                {
                                    foreach (var child in arrayItem.Children())
                                    {
                                        if (child.First.ToString().StartsWith("ipfs://"))
                                        {
                                            return Stripipfs(child.First.ToString());
                                        }
                                    }

                                }
                            }
                            return Stripipfs(item.Value.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }

            return null;
        }

        private string Stripipfs(string tostrip)
        {
            if (tostrip.StartsWith("ipfs://"))
            {
                tostrip = tostrip.Substring(7).Replace("ipfs", "").Replace("/", "");
            }

            return tostrip;
        }

        private string FindName(JObject jobject)
        {
            foreach (var item in jobject)
            {
                if (item.Value.GetType() == typeof(JObject))
                {
                    var stringFound = FindName((JObject)item.Value);

                    if (stringFound != null)
                    {
                        return stringFound;
                    }
                }
                else
                {
                    if (string.Equals(item.Key, "name", System.StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(item.Key, "title", System.StringComparison.OrdinalIgnoreCase))
                    {
                        return item.Value.ToString();
                    }
                }
            }

            return null;
        }

        public void GetNFTInfo(string asset, bool cache, System.Action<string> callback)
        {                        
            GetNFTInfoFromBlockchain(asset, cache, callback);
        }        

        public void GetNFTInfoFromBlockchain(string asset, bool cache, System.Action<string> callback, System.Action<string, string> errorCallback = null)
        {
            WebRequest request = WebRequest.Create(APIROOT + "assets/" + asset);
            request.Headers.Add("project_id", ApiKey);

            string responseFromServer;

            WebResponse response = request.GetResponse();
            using (Stream dataStream = response.GetResponseStream())
            {
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                responseFromServer = reader.ReadToEnd();
                // Display the content.
                Console.WriteLine(responseFromServer);
            }

            // Close the response.
            response.Close();

            callback(responseFromServer);
        }     

        public void GetPolicyAssetListFromBlockchain(string policy, string order, System.Action<List<assetinfo>> callback)
        {
            WebRequest request = WebRequest.Create(APIROOT + "assets/policy/" + policy + "?order=" + order);
            request.Headers.Add("project_id", ApiKey);

            string responseFromServer;

            WebResponse response = request.GetResponse();
            using (Stream dataStream = response.GetResponseStream())
            {
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                responseFromServer = reader.ReadToEnd();
                // Display the content.
                Console.WriteLine(responseFromServer);
            }

            // Close the response.
            response.Close();

            List<assetinfo> assetlist = JsonConvert.DeserializeObject<List<assetinfo>>(responseFromServer);

            callback(assetlist);
        }       

        public void GetWalletStakeFromAddress(string wallet, System.Action<string> callback, System.Action<string> error = null)
        {
            //get stake key from address
            WebRequest request = WebRequest.Create(APIROOT + "addresses/" + wallet);
            request.Headers.Add("project_id", ApiKey);

            string responseFromServer;

            WebResponse response = request.GetResponse();
            using (Stream dataStream = response.GetResponseStream())
            {
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                responseFromServer = reader.ReadToEnd();
                // Display the content.
                Console.WriteLine(responseFromServer);
            }

            // Close the response.
            response.Close();

            address address = JsonConvert.DeserializeObject<address>(responseFromServer);
            callback(address.stake_address);            
        }

        public void GetWalletAssetListFromBlockchainProcess(string req, System.Action<List<assetinfo>> callback)
        {            
            //get stake key from address
            WebRequest request = WebRequest.Create(req);
            request.Headers.Add("project_id", ApiKey);

            string responseFromServer;

            WebResponse response = request.GetResponse();
            using (Stream dataStream = response.GetResponseStream())
            {
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                responseFromServer = reader.ReadToEnd();
                // Display the content.
                Console.WriteLine(responseFromServer);
            }

            // Close the response.
            response.Close();

            List<assetinfo> assetlist = JsonConvert.DeserializeObject<List<assetinfo>>(responseFromServer);

            callback(assetlist);                                         
        }

        public void GetAllWalletAssetListFromBlockchain(string wallet, string order, System.Action<List<assetinfo>> callback)
        {
            int count = 100;
            int page = 1;
            List<assetinfo> assets = new List<assetinfo>();
            getWalletAssetListPage(wallet, order, count, page, assets, callback);
        }

        private void getWalletAssetListPage(string wallet, string order, int count, int page, List<assetinfo> assets, System.Action<List<assetinfo>> callback)
        {
            GetWalletAssetListFromBlockchain(wallet, order, count, page, (List<assetinfo> assetspage) => {
                assets.AddRange(assetspage);
                if (assetspage.Count == count)
                {
                    getWalletAssetListPage(wallet, order, count, page + 1, assets, callback);
                }
                else
                {
                    callback(assets);
                }
            });
        }

        public void GetWalletAssetListFromBlockchain(string wallet, string order, int count, int page, System.Action<List<assetinfo>> callback)
        {
            string req = "";
            if (wallet.StartsWith("addr"))
            {
                GetWalletStakeFromAddress(wallet, (stake) => {
                    req = $"{APIROOT}accounts/{stake}/addresses/assets?order={order}&count={count}&page={page}";
                    GetWalletAssetListFromBlockchainProcess(req, callback);
                });
            }
            else
            {
                req = $"{APIROOT}accounts/{wallet}/addresses/assets?order={order}&count={count}&page={page}";
                GetWalletAssetListFromBlockchainProcess(req, callback);
            }
        }

        public void GetTransactionUTXOs(string transaction, System.Action<transaction> callback, System.Action<string> error = null)
        {
            //get stake key from address
            WebRequest request = WebRequest.Create(APIROOT + "txs/" + transaction + "/utxos");
            request.Headers.Add("project_id", ApiKey);

            string responseFromServer;

            WebResponse response = request.GetResponse();
            using (Stream dataStream = response.GetResponseStream())
            {
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                responseFromServer = reader.ReadToEnd();
                // Display the content.
                Console.WriteLine(responseFromServer);
            }

            // Close the response.
            response.Close();

            transaction transactionResult = JsonConvert.DeserializeObject<transaction>(responseFromServer);

            callback(transactionResult);
        }
    }    
}