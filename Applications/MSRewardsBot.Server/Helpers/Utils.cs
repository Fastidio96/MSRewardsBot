using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MSRewardsBot.Server.Helpers
{
    public class Utils
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

        public static string GetFileLatestUpdate()
        {
            return Path.Combine(GetFolderClientUpdate(), "update.zip");
        }

        #endregion

        public static string GetFileHash(string path)
        {
            if (!File.Exists(path))
            {
                return string.Empty;
            }

            using (FileStream fs = File.OpenRead(path))
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(fs);
                return Convert.ToBase64String(hash);
            }
        }

        #region Enable console colors

        const int STD_OUTPUT_HANDLE = -11;
        const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern nint GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(nint hConsoleHandle, uint dwMode);

        public static void EnableConsoleANSI()
        {
            nint handle = GetStdHandle(STD_OUTPUT_HANDLE);
            GetConsoleMode(handle, out uint mode);
            SetConsoleMode(handle, mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
        }

        #endregion
    }
}
