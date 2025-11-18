using System;
using System.IO;

namespace MSRewardsBot.Server
{
    public class Utils
    {
        private static string _serverPath => new Uri(AppDomain.CurrentDomain.BaseDirectory + "MSRB").LocalPath;
        private static string _logsPath => Path.Combine(_serverPath, "logs");

        public static string GetFolderServerData()
        {
            if (!Directory.Exists(_serverPath))
            {
                Directory.CreateDirectory(_serverPath);
            }

            return _serverPath;
        }

        public static string GetFolderLogs()
        {
            if (!Directory.Exists(_logsPath))
            {
                Directory.CreateDirectory(_logsPath);
            }

            return _logsPath;
        }

        public static string GetDBFile()
        {
            return Path.Combine(GetFolderServerData(), "data.db");
        }

        public static string GetKeywordsFile()
        {
            return Path.Combine(GetFolderServerData(), "keywords.txt");
        }

        public static string GetLogFile()
        {
            string now = DateTime.Now.ToString("yyyy-MM-dd");
            return Path.Combine(GetFolderLogs(), $"{now}.txt");
        }
    }
}
