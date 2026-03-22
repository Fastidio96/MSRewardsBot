using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace MSRewardsBot.Client.Updater
{
    internal class Program
    {
        const string APP_NAME = "MSRewardsBot.Client.exe";

        static string AppFolderPath;
        static string CurrentDir => new Uri(AppDomain.CurrentDomain.BaseDirectory).LocalPath;
        static string UpdatePackagePath => Path.Combine(CurrentDir, "update.zip");
        static string BackupFolderPath => Path.Combine(AppFolderPath, "backup");

        static void Main(string[] args)
        {
            nint handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);

            if (args.Length != 1)
            {
                Environment.Exit(-1);
            }

            AppFolderPath = args[0];
            if (string.IsNullOrEmpty(AppFolderPath) || !Uri.TryCreate(AppFolderPath, UriKind.RelativeOrAbsolute, out _))
            {
                Environment.Exit(-1);
            }

            WaitMainAppToClose();
            Install();
        }

        private static void WaitMainAppToClose()
        {
            while (Process.GetProcessesByName(APP_NAME).Any())
            {
                Thread.Sleep(500);
            }
        }

        private static void Install()
        {
            try
            {
                if (!File.Exists(UpdatePackagePath))
                {
                    return;
                }

                if (!BackupAppFiles())
                {
                    Environment.Exit(-1);
                }

                if (!ApplyUpdate())
                {
                    RollbackUpdate();
                }

                Directory.Delete(BackupFolderPath, true);
            }
            catch
            {
                RollbackUpdate();
            }

            Process.Start(Path.Combine(AppFolderPath, APP_NAME));
            Environment.Exit(0);
        }

        private static bool ApplyUpdate()
        {
            try
            {
                if (!DeleteAppFiles())
                {
                    return false;
                }

                using (FileStream fs = new FileStream(UpdatePackagePath, FileMode.Open, FileAccess.Read))
                using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    zip.ExtractToDirectory(AppFolderPath);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool DeleteAppFiles()
        {
            try
            {
                DirectoryInfo src = new DirectoryInfo(AppFolderPath);

                foreach (FileInfo file in src.GetFiles())
                {
#if DEBUG
                    if (file.Extension == ".pdb")
                    {
                        continue;
                    }
#endif

                    file.Delete();
                }

                foreach (DirectoryInfo dir in src.GetDirectories())
                {
                    if (dir.FullName == BackupFolderPath)
                    {
                        continue;
                    }

                    dir.Delete(true);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool BackupAppFiles()
        {
            try
            {
                if (Directory.Exists(BackupFolderPath))
                {
                    Directory.Delete(BackupFolderPath, true);
                }

                CopyDirectory(AppFolderPath, BackupFolderPath);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool RollbackUpdate()
        {
            try
            {
                if (!Directory.Exists(BackupFolderPath))
                {
                    return false;
                }

                DeleteAppFiles();

                CopyDirectory(BackupFolderPath, AppFolderPath);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void CopyDirectory(string sourceDir, string destinationDir)
        {
            // Get information about the source directory
            DirectoryInfo dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
            }

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
#if DEBUG
                if (file.Extension == ".pdb")
                {
                    continue;
                }
#endif

                file.CopyTo(targetFilePath);
            }

            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }

        #region Hide console

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;

        #endregion
    }
}
