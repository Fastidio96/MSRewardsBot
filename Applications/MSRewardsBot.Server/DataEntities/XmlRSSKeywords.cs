using System.Collections.Generic;
using System.Xml.Serialization;

namespace MSRewardsBot.Server.DataEntities
{
    [XmlRoot("rss", Namespace = "", IsNullable = false)]
    public class RssFeed
    {
        [XmlElement("channel")]
        public Channel Channel { get; set; }
    }

    public class Channel
    {
        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("item")]
        public List<Item> Items { get; set; }
    }

    public class Item
    {
        [XmlElement("title")]
        public string Title { get; set; }
    }
}
