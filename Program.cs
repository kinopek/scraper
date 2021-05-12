using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using VisioForge.Shared.Newtonsoft.Json;

namespace scraper
{
    class Walutka
    {
        public Dictionary<string, string> name, unit, value, type;
        public Walutka(Dictionary<string, string> n, Dictionary<string, string> u, Dictionary<string, string> v, Dictionary<string, string> t)
        {
            name = n;
            unit = u;
            value = v;
            type = t;
        }
    }
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            const string intoUrl = "http://localhost:5000/";
            const string fromUrl = "https://api.coingecko.com/api/v3/exchange_rates";
            const string createCurrencyUrl = intoUrl+ "/Currencies/Create";
            const string getCurrencyUrl = intoUrl + "/Currencies/Details";
            const string createValuesUrl = intoUrl + "/Values/Create";
            const string getAllCurrencies = intoUrl +  "/Currencies";
            Dictionary<string, string> listOfCurrencyIDs;


            await CurrenciesTableAsync(fromUrl, createCurrencyUrl, getCurrencyUrl);
            listOfCurrencyIDs = await GetEveryID(getAllCurrencies);


            while (true)
            {
                await ValuesTableAsync(fromUrl, createValuesUrl, listOfCurrencyIDs);
                System.Threading.Thread.Sleep(1000);
            }
        }

        private static async Task<Dictionary<string, string>> GetEveryID(string getAllCurrenciesUrl)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(getAllCurrenciesUrl);
            request.ContentType = "application/json";
            WebResponse webResponse = request.GetResponse();
            Stream webStream = webResponse.GetResponseStream();
            StreamReader responseReader = new StreamReader(webStream);
            string response = responseReader.ReadToEnd();
            responseReader.Close();
            SortedList<string, Dictionary<string, string>> currenciesAndValues = StringIntoJson(response); 
            Dictionary<string, string> IDdictionary = new Dictionary<string, string>();

            foreach (Dictionary<string, string> oneCurrencyWithValue in currenciesAndValues.Values)
            {
                IDdictionary.Add(oneCurrencyWithValue["name"], oneCurrencyWithValue["currencyID"]);
            }
            return IDdictionary;
        }

        private static async Task ValuesTableAsync(string fromUrl, string createValuesUrl, Dictionary<string, string> listOfCurrencyIDs)
        {
            try
            {

                string response = ScrapFromCoinGecko(fromUrl);
                SortedList<string, Dictionary<string, string>> currenciesAndValues = StringIntoJson(response);
                Console.WriteLine(currenciesAndValues.ToString());


                foreach (Dictionary<string, string> oneCurrencyWithValue in currenciesAndValues.Values)
                {
                    Dictionary<string, string> properFormatInfo = new Dictionary<string, string>()
                        {
                        {"currencyID", listOfCurrencyIDs["name"]},
                        {"timeStamp", DateTime.Now.ToString()},
                        {"rate", oneCurrencyWithValue["value"]},
                        };
                    await UpdateCurrencyValues(properFormatInfo, createValuesUrl);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("-----------------");
                Console.WriteLine(e.Message);
            }
        }

        private static async Task CurrenciesTableAsync(string fromUrl, string createCurrencyUrl, string getCurrencyUrl)
        {
            try
            {
                string response = ScrapFromCoinGecko(fromUrl);
                SortedList<string, Dictionary<string, string>> manyCurrencies = StringIntoJson(response);
                foreach (Dictionary<string, string> currency in manyCurrencies.Values)
                {
                    bool exists = false;
                    exists = await CheckIfCurrencyExistsInDB(getCurrencyUrl, currency["name"]);
                    if (!exists)
                    {

                        Dictionary<string, string> properFormatInfo = new Dictionary<string, string>()
                        {
                        {"currencyID", currency["name"]},
                        {"name", currency["name"]},
                        {"symbol", currency["unit"]},
                        };
                        await InsertCurrencyIntoDB(createCurrencyUrl, properFormatInfo);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("----\n" + e.Message);
            }
        }

        private static async Task InsertCurrencyIntoDB(string intoUrl, object v)
        {
            var client = new HttpClient();
            var values = new Dictionary<string, string>()
                    {
                        {"", v.ToString()},
                    };
            var content = new FormUrlEncodedContent(values);
            var responseSend = await client.PostAsync(intoUrl, content);
            // var responseSend = await client.PutAsync(intoUrl, content);
            responseSend.EnsureSuccessStatusCode();
        }

        private static SortedList<string, Dictionary<string, string>> StringIntoJson(string response)
        {
            SortedList<string, SortedList<string, Dictionary<string, string>>> json = JsonConvert.DeserializeObject<SortedList<string, SortedList<string, Dictionary<string, string>>>>(response);
            SortedList<string, Dictionary<string, string>> smallerJson = new SortedList<string, Dictionary<string, string>>();
            foreach (SortedList<string, Dictionary<string, string>> v in json.Values)
            {
                foreach (string s in v.Keys)
                {
                    smallerJson.Add(s, v[s]);

                }
            }
            return smallerJson;
        }

        private static string ScrapFromCoinGecko(string fromUrl)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fromUrl);
            request.ContentType = "application/json";
            WebResponse webResponse = request.GetResponse();
            Stream webStream = webResponse.GetResponseStream();
            StreamReader responseReader = new StreamReader(webStream);
            string response = responseReader.ReadToEnd();
            responseReader.Close();
            return response;
        }

        private static async System.Threading.Tasks.Task UpdateCurrencyValues(Dictionary<string, string> values, string intoUrl)
        {
            var client = new HttpClient();
            var content = new FormUrlEncodedContent(values);
            var responseSend = await client.PostAsync(intoUrl, content);
            responseSend.EnsureSuccessStatusCode();
        }

        private static async System.Threading.Tasks.Task<bool> CheckIfCurrencyExistsInDB(string getCurrencyUrl, string currencyName)
        {
            var client = new HttpClient();
            var responseSend = await client.GetAsync(getCurrencyUrl + currencyName);
            responseSend.EnsureSuccessStatusCode();

            if (responseSend != null)
            {
                return true;
            }
            else
            {
                return false;

            }
        }


    }
}
