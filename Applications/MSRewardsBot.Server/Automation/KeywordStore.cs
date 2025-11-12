using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using MSRewardsBot.Server.DataEntities;

namespace MSRewardsBot.Server.Automation
{
    public class KeywordStore
    {
        private const string URL_BASE = "https://trends.google.com/trending/rss?geo=";
        private readonly string[] _countries = new[] { "IT", "US", "GB", "DE", "FR", "ES" };
        private int _countryIdx = 0;

        public DateTime LastRefresh { get; private set; }

        private string GetUrlList()
        {
            return URL_BASE + _countries[0];
        }

        public async Task<bool> RefreshList()
        {
            string content = await DownloadList();
            if (content == null)
            {
                return false;
            }

            List<Item> items = ParseList(content);
            if(items == null || items.Count == 0)
            {
                return false;
            }

            LastRefresh = DateTime.Now;
            return true;
        }

        private async Task<string> DownloadList()
        {
            string res = null;
            using (HttpClient client = new HttpClient())
            {
                res = await client.GetStringAsync(GetUrlList());
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
                using (StreamReader sr = new StreamReader(content, Encoding.UTF8))
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
    }
}
