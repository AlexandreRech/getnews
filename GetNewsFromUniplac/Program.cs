using GetNews.Domain;
using GetNewsFromUniplac.MinimalCometLibrary;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace GetNewsFromUniplac
{
    class Program
    {
        private static CountdownEvent _countdownLatch;
        private static Guid _myId = Guid.Parse("36d9ca5c-2f8d-4e60-9f2b-c7c3ef0b37ff");
        private static string _apiKey = "oJ0vAmCntaHRH5q0OFxC2+5MT8/Haj//fUhXIBzwu5uuVDB0nAmPhymDb/ZsvKh9zKKcnqPVHhSIDzZ+0T6mxQ==";

        private static List<News> _newsList = new List<News>();

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;

            Console.WriteLine("Digite o id da notícia:");

            int newsId = Convert.ToInt32(Console.ReadLine());

            ImportIO io = new ImportIO("https://query.import.io", _myId, _apiKey);
            io.Connect();

            _countdownLatch = new CountdownEvent(1);

            Dictionary<String, Object> query1 = new Dictionary<String, Object>();
            query1.Add("input", new Dictionary<String, String>() { { "webpage/url", "http://www.uniplac.net/noticias/index.php?id_noticia=" + newsId } });
            query1.Add("connectorGuids", new List<String>() { "4690f479-fbea-47b7-b743-c296e33e147f" });

            io.DoQuery(query1, HandleQuery);

            _countdownLatch.Wait();

            io.Disconnect();

            News newsDetails = _newsList[0];

            Console.WriteLine(newsDetails);

            File.WriteAllText("C:\\temp\\newsDetails_" +  newsId + ".txt", newsDetails.ToString());

            Console.ReadKey();
        }

        private static void HandleQuery(Query query, Dictionary<String, Object> message)
        {
            if (message["type"].Equals("MESSAGE"))
            {
                Console.WriteLine("Got data!");

                var news = ((JObject)message["data"])["results"][0].ToObject<News>();
                news.FixParagraphs();

                _newsList.Add(news);
            }

            if (query.IsFinished)
                _countdownLatch.Signal();
        }
    }
}
