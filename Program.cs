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

            const string fromUrl = "https://api.coindesk.com/v1/bpi/currentprice.json";
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
                    //string list = JsonConvert.DeserializeObject<string>(response);

                   data = JsonConvert.DeserializeObject<Walutka>(response);
                    
                      Console.WriteLine(data.time);
                       Console.WriteLine(data.bpi);

                    responseReader.Close();
                   
                    var client = new HttpClient();
                    var values = new Dictionary<string, string>()
                    {
                        {"time", data.time.ToString()},
                        {"bpi", data.bpi.ToString()},
                    };
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
