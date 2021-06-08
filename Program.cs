using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using VisioForge.Shared.Newtonsoft.Json;
using Prometheus;


namespace scraper
{
    class Program
    {
        private static Counter createDictionary_exception = Metrics.CreateCounter("createDictionary_exception", "During creating the dictionary by asking the database, there were some problems.");
        private static Counter rateIntoDB_exception = Metrics.CreateCounter("rateIntoDB_exception", "During sending a rate of a currency into the database, there were some problems.");
        private static Counter allRatesIntoDB_exception = Metrics.CreateCounter("allRatesIntoDB_exception", "During sending rates of all currencies into the database, there were some problems.");
        private static Counter currencyIntoDB_exception = Metrics.CreateCounter("currencyIntoDB_exception", "During sending a currency into databse, something went wrong");
        private static Counter allCurrenciesIntoDB_exception = Metrics.CreateCounter("allCurrenciesIntoDB_exception", "During sending all currencies into the database, there were some problems.");
        private static Counter currencyExistance_exception = Metrics.CreateCounter("currencyExistance_exception", "During checking the existance of some currency in the database, something went wrong");

        private static Counter createDictionary_ok = Metrics.CreateCounter("createDictionary_ok", "Creating the dictionary, by asking the database, completed successfully.");
        private static Counter rateIntoDB_ok = Metrics.CreateCounter("rateIntoDB_ok", "Successfully sent a rate of a currency into the database.");
        private static Counter allRatesIntoDB_ok = Metrics.CreateCounter("allRatesIntoDB_ok", "All current rates have been sent into the database.");
        private static Counter currencyIntoDB_ok = Metrics.CreateCounter("currencyIntoDB_ok", "Successfully sent a currency into the database.");
        private static Counter allCurrenciesIntoDB_ok = Metrics.CreateCounter("allCurrenciesIntoDB_ok", "All currencies shouold now be in the database.");

        static void Main(string[] args)
        {
            const int minutesToWait = 1;
            const string intoUrl = "http://localhost:50866/";
            const string fromUrl = "https://api.coingecko.com/api/v3/exchange_rates/";
            const string createCurrencyUrl = intoUrl + "Currencies/Create/";
            const string getCurrencyUrl = intoUrl + "Currencies/GetID/";
            const string createValuesUrl = intoUrl + "Values/Create/";
            const string getAllCurrenciesUrl = intoUrl + "Currencies/";
            Dictionary<string, string> listOfCurrencyIDs;

            SetupRequirements();

            CurrenciesTable(fromUrl, createCurrencyUrl, getCurrencyUrl);
            listOfCurrencyIDs = GetEveryID(getAllCurrenciesUrl);
            while (true)
            {
                ValuesTable(fromUrl, createValuesUrl, listOfCurrencyIDs);
                System.Threading.Thread.Sleep(minutesToWait * 1000 * 60);
            }
        }

        private static void SetupRequirements()
        {
            //var metricServer = new MetricServer(1234);
            // metricServer.Start();//exception no access
            var server = new MetricServer(hostname: "localhost", port: 1234);
            server.Start();
        }

        private static Dictionary<string, string> GetEveryID(string getAllCurrenciesUrl)
        {
            Dictionary<string, string> IDdictionary = new Dictionary<string, string>();
            try
            {
                string response = Get(getAllCurrenciesUrl);
                if (response.Length > 0)
                {
                    List<Dictionary<string, string>> currenciesAndValues = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(response);
                    foreach (Dictionary<string, string> oneCurrencyWithValue in currenciesAndValues)
                    {
                        try
                        {
                            IDdictionary.Add(oneCurrencyWithValue["name"], oneCurrencyWithValue["currencyID"]);
                        }
                        catch (Exception e)
                        {
                            //    Console.WriteLine(e.Message +" OMMITING");
                            createDictionary_exception.Inc(1);
                        }
                    }
                    Console.WriteLine("created dictionary of IDs");
                    createDictionary_ok.Inc(1);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("-----------------");
                Console.WriteLine(e.Message);
                createDictionary_exception.Inc(1);
                IDdictionary = GetEveryID(getAllCurrenciesUrl);
            }
            return IDdictionary;
        }

        private static void ValuesTable(string fromUrl, string createValuesUrl, Dictionary<string, string> listOfCurrencyIDs)
        {
            try
            {
                string response = Get(fromUrl);
                SortedList<string, Dictionary<string, string>> currenciesAndValues = StringIntoJson(response);
                //     Console.WriteLine(currenciesAndValues.ToString());

                foreach (Dictionary<string, string> oneCurrencyWithValue in currenciesAndValues.Values)
                {
                    try
                    {
                        Dictionary<string, string> properFormatInfo = new Dictionary<string, string>()
                        {
                        {"currencyID", listOfCurrencyIDs[oneCurrencyWithValue["name"]]},
                        {"timeStamp", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")},
                        {"rate", oneCurrencyWithValue["value"]}
                        };
                        InsertIntoDB(createValuesUrl, properFormatInfo);
                        rateIntoDB_ok.Inc(1);


                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("-------During " + oneCurrencyWithValue["name"]);
                        Console.WriteLine(e.Message);
                        rateIntoDB_exception.Inc(1);

                    }
                }
                Console.WriteLine("sent the current currency prices into the database");
                allRatesIntoDB_ok.Inc(1);

            }
            catch (Exception e)
            {
                Console.WriteLine("-----------------");
                Console.WriteLine(e.Message);
                allRatesIntoDB_exception.Inc(1);

            }
        }

        private static void CurrenciesTable(string fromUrl, string createCurrencyUrl, string getCurrencyUrl)
        {
            try
            {
                string response = Get(fromUrl);
                SortedList<string, Dictionary<string, string>> manyCurrencies = StringIntoJson(response);
                foreach (Dictionary<string, string> currency in manyCurrencies.Values)
                {
                    try
                    {

                        bool exists = false;
                        exists = CheckIfCurrencyExistsInDBAsync(getCurrencyUrl, currency["name"]);
                        if (!exists)
                        {

                            Console.WriteLine("there is no " + currency["name"] + " in the database. inserting");
                            Dictionary<string, string> properFormatInfo = new Dictionary<string, string>()
                        {
                        {"name", currency["name"]},
                        {"symbol", currency["unit"]}
                        };
                            InsertIntoDB(createCurrencyUrl, properFormatInfo);
                            Console.WriteLine("sent " + currency["name"] + " into the database");
                            currencyIntoDB_ok.Inc(1);



                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("----\n" + e.Message);
                        currencyIntoDB_exception.Inc(1);


                    }
                }
                Console.WriteLine("Now all the currencies should be in the database");
                allCurrenciesIntoDB_ok.Inc(1);

            }
            catch (Exception e)
            {
                Console.WriteLine("----\n" + e.Message);
                allCurrenciesIntoDB_exception.Inc(1);

            }
        }

        private static void InsertIntoDB(string intoUrl, Dictionary<string, string> v)
        {
            string json = JsonConvert.SerializeObject(v);
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(intoUrl);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(json);
            }
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
            }
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

        private static bool CheckIfCurrencyExistsInDBAsync(string getCurrencyUrl, string currencyName)
        {
            try
            {
                string response = Get(getCurrencyUrl + currencyName);

                if (response != null)
                {
                    if (response.Length > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                currencyExistance_exception.Inc(1);
                return false;

            }
        }

        public static string Get(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
