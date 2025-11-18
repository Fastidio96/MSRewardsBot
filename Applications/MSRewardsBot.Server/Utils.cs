using System;
using System.IO;

namespace MSRewardsBot.Server
{
    public class Utils
    {
        private static string _serverPath => new Uri(AppDomain.CurrentDomain.BaseDirectory + "MSRB").LocalPath;

        public static string GetFolderServerData()
        {
            if (!Directory.Exists(_serverPath))
            {
                Directory.CreateDirectory(_serverPath);
            }

            return _serverPath;
        }

        public static string GetDBFile()
        {
            return Path.Combine(GetFolderServerData(), "data.db");
        }

        public static string GetKeywordsFile()
        {
            return Path.Combine(GetFolderServerData(), "keywords.txt");
        }
    }
}
