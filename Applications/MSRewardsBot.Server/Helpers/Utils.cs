using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace MSRewardsBot.Server.Helpers
{
    public class Utils
    {
        public static string GetMd5FileHash(string path)
        {
            if (!File.Exists(path))
            {
                return string.Empty;
            }

            using (FileStream fs = File.OpenRead(path))
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(fs);
                return Convert.ToBase64String(hash);
            }
        }

        public static bool VerifyFileSha256(string path, string hash)
        {
            if (hash.ToLower().StartsWith("sha256:"))
            {
                hash = hash.Replace("sha256:", "");
            }

            using (FileStream fs = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
            {
                byte[] checksum = sha.ComputeHash(fs);
                return hash.ToLower() == BitConverter.ToString(checksum).Replace("-", string.Empty).ToLower();
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
