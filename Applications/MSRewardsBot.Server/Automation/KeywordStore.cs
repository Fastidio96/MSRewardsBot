using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using MSRewardsBot.Server.DataEntities;
using MSRewardsBot.Server.Helpers;

namespace MSRewardsBot.Server.Automation
{
    public class KeywordStore
    {
        private const string URL_BASE = "https://trends.google.com/trending/rss?geo=";
        private readonly string[] _countries;

        private string _filePath;

        public DateTime LastRefresh { get; private set; }

        public KeywordStore(string[] countries)
        {
            _countries = countries;
            _filePath = Utils.GetKeywordsFile();
        }

        public async Task<bool> RefreshList()
        {
            List<Item> items = new List<Item>();
            for (int idx = 0; idx < _countries.Length; idx++)
            {
                items.AddRange(await DownloadSingleList(_countries[idx]));
            }

            if (items.Count == 0)
            {
                return false;
            }

            if (!SaveItemsToFile(items))
            {
                return false;
            }

            LastRefresh = DateTime.Now;
            return true;
        }

        private async Task<List<Item>> DownloadSingleList(string country)
        {
            List<Item> res = new List<Item>();

            string content = await DownloadList(country);
            if (content == null)
            {
                return res;
            }

            return ParseList(content);
        }

        private async Task<string> DownloadList(string country)
        {
            string res = null;
            string url = URL_BASE + country.ToUpper();

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    res = await client.GetStringAsync(url);
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }

            return res;
        }

        private List<Item> ParseList(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return null;
            }

            RssFeed data = null;
            try
            {
                using (StringReader sr = new StringReader(content))
                {
                    XmlSerializer xml = new XmlSerializer(typeof(RssFeed));
                    object obj = xml.Deserialize(sr);

                    if (obj is not RssFeed feed)
                    {
                        return null;
                    }

                    data = feed;
                }
            }
            catch
            {
                return null;
            }

            return [.. data.Channel.Items];
        }

        private bool SaveItemsToFile(List<Item> items)
        {
            try
            {
                using (FileStream fs = File.Open(_filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    foreach (Item item in items)
                    {
                        sw.WriteLine(item.Title);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
