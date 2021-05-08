using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using VisioForge.Shared.Newtonsoft.Json;

namespace scraper
{
    class Walutka
    {
        public object time, bpi; 
        public Walutka(object t, object b)
        {
            time = t;
            bpi = b;
        }
    }
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {

            const string fromUrl = "https://api.coingecko.com/api/v3/exchange_rates";
            string intoUrl = "http://apinaszedocelowe.com.pl/szczecin/szanty/golebabki";
            Walutka data;

            while (true)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fromUrl);
                //request.Method = Get();
                request.ContentType = "application/json";
               // request.ContentLength = Data.Length;
                try
                {
                    WebResponse webResponse = request.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();
                    StreamReader responseReader = new StreamReader(webStream);
                    string response = responseReader.ReadToEnd();
                   object obiekt = JsonConvert.DeserializeObject<object>(response);

                   //data = JsonConvert.DeserializeObject<Walutka>(response);
                    
                      //Console.WriteLine(data.time);
                       //Console.WriteLine(data.bpi);

                    responseReader.Close();
                   
                    var client = new HttpClient();
                    var values = new Dictionary<string, string>()
                    {
                        {"", obiekt.ToString()},
                        //{"bpi", data.bpi.ToString()},
                    };
                    Console.WriteLine(obiekt.ToString());
                    var content = new FormUrlEncodedContent(values);
                    var responseSend = await client.PostAsync(intoUrl, content);
                    responseSend.EnsureSuccessStatusCode();

                }
                catch (Exception e)
                {
                    Console.WriteLine("-----------------");
                    Console.WriteLine(e.Message);
                }
              
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}
