using System;
using System.IO;
using System.Runtime.InteropServices;

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

        #region Enable console colors

        const int STD_OUTPUT_HANDLE = -11;
        const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        public static void EnableANSI()
        {
            nint handle = GetStdHandle(STD_OUTPUT_HANDLE);
            GetConsoleMode(handle, out uint mode);
            SetConsoleMode(handle, mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
        }

        #endregion
    }
}
