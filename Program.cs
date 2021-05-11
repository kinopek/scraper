using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using VisioForge.Shared.Newtonsoft.Json;

namespace scraper
{
   /* class Walutka
    {
        public object time, bpi; 
        public Walutka(object t, object b)
        {
            time = t;
            bpi = b;
        }
    }*/
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {

            const string fromUrl = "https://api.coingecko.com/api/v3/exchange_rates";
            string intoUrl = "http://apinaszedocelowe.com.pl/szczecin/szanty/golebabki";
            //Walutka data;


            bool exists = await CheckIfDatabaseExistsAsync(intoUrl);

            while (true)
            {
                

                try
                {
                    string response = ScrapFromCoinGecko(fromUrl);
                   
                   object obiekt = JsonConvert.DeserializeObject<object>(response);

                    //data = JsonConvert.DeserializeObject<Walutka>(response);

                    var values = new Dictionary<string, string>()
                    {
                        {"", obiekt.ToString()},
                    };
                    Console.WriteLine(obiekt.ToString());

                    if (!exists)
                    {
                        CreateDatabaseAsync(values, intoUrl);
                        exists = true;
                    }
                    else
                    {
                        UpdateDatabaseAsync(values, intoUrl);
                    }

                   

                }
                catch (Exception e)
                {
                    Console.WriteLine("-----------------");
                    Console.WriteLine(e.Message);
                }
              
                System.Threading.Thread.Sleep(1000);
            }
        }

        private static string ScrapFromCoinGecko(string fromUrl)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fromUrl);
            request.ContentType = "application/json";
            WebResponse webResponse = request.GetResponse();
            Stream webStream = webResponse.GetResponseStream();
            StreamReader responseReader = new StreamReader(webStream);
            string response=  responseReader.ReadToEnd();
            responseReader.Close();
            return response;
        }

        private static async System.Threading.Tasks.Task UpdateDatabaseAsync(Dictionary<string, string> values, string intoUrl)
        {
            var client = new HttpClient();
            var content = new FormUrlEncodedContent(values);
            var responseSend = await client.PutAsync(intoUrl, content);
            responseSend.EnsureSuccessStatusCode();
        }

        private static async System.Threading.Tasks.Task<bool> CheckIfDatabaseExistsAsync(string Url)
        {
            var client = new HttpClient();
            var responseSend = await client.GetAsync(Url);
            responseSend.EnsureSuccessStatusCode();

            if (responseSend !=null)
            {
                return true;
            }
            else
            {
                return false;

            }
        }

        private static async System.Threading.Tasks.Task CreateDatabaseAsync(Dictionary<string, string> values, string intoUrl)
        {
            var client = new HttpClient();
            var content = new FormUrlEncodedContent(values);
            var responseSend = await client.PostAsync(intoUrl, content);
            responseSend.EnsureSuccessStatusCode();

        }
    }
}
