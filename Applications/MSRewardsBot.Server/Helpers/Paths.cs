using System;
using System.IO;

namespace MSRewardsBot.Server.Helpers
{
    public class Paths
    {
        private static string _serverPath => new Uri(AppDomain.CurrentDomain.BaseDirectory + "MSRB").LocalPath;
        private static string _logsPath => Path.Combine(_serverPath, "logs");
        private static string _clientUpdatePath => Path.Combine(_serverPath, "updates");

        #region Folders

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

        public static string GetFolderClientUpdate()
        {
            if (!Directory.Exists(_clientUpdatePath))
            {
                Directory.CreateDirectory(_clientUpdatePath);
            }

            return _clientUpdatePath;
        }

        #endregion

        #region Files

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

        public static string GetVersionFile()
        {
            return Path.Combine(GetFolderClientUpdate(), "latest.txt");
        }

        public static string GetPathClientUpdate()
        {
            return Path.Combine(GetFolderClientUpdate(), "client.zip");
        }

        #endregion
    }
}
