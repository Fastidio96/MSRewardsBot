using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace MSRewardsBot.Client.Updater
{
    internal class Program
    {
        const string APP_NAME = "MSRewardsBot.Client.exe";
        // Process.GetProcessesByName wants the name WITHOUT extension on Windows.
        static readonly string APP_PROCESS_NAME = Path.GetFileNameWithoutExtension(APP_NAME);

        // Hard cap so we never hang forever if some unrelated process matches the name
        // or if the client crashes mid-shutdown without releasing handles cleanly.
        const int WAIT_FOR_CLIENT_EXIT_TIMEOUT_MS = 30_000;

        static string AppFolderPath;
        static string CurrentDir => new Uri(AppDomain.CurrentDomain.BaseDirectory).LocalPath;
        static string UpdatePackagePath => Path.Combine(CurrentDir, "update.zip");
        static string BackupFolderPath => Path.Combine(AppFolderPath, "backup");

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Environment.Exit(-1);
            }

            AppFolderPath = args[0];
            if (string.IsNullOrWhiteSpace(AppFolderPath) || !Directory.Exists(AppFolderPath))
            {
                Environment.Exit(-1);
            }

            // Normalize so internal path comparisons are stable.
            AppFolderPath = Path.GetFullPath(AppFolderPath);

            WaitMainAppToClose();
            Install();
        }

        private static void WaitMainAppToClose()
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (Process.GetProcessesByName(APP_PROCESS_NAME).Any())
            {
                if (sw.ElapsedMilliseconds > WAIT_FOR_CLIENT_EXIT_TIMEOUT_MS)
                {
                    // Give up: client did not exit.
                    Environment.Exit(-1);
                }
                Thread.Sleep(500);
            }
        }

        private static void Install()
        {
            bool updateApplied = false;
            bool rollbackOk = false;

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

                if (ApplyUpdate())
                {
                    updateApplied = true;
                }
                else
                {
                    rollbackOk = RollbackUpdate();
                }
            }
            catch
            {
                // Apply/Backup threw. Try to rollback if we have a backup to roll back to.
                if (!updateApplied && Directory.Exists(BackupFolderPath))
                {
                    try
                    { rollbackOk = RollbackUpdate(); }
                    catch { rollbackOk = false; }
                }
            }

            // Only delete the backup if either the new install succeeded OR the rollback succeeded.
            // If both failed, KEEP the backup so the user has a manual recovery path.
            if (updateApplied || rollbackOk)
            {
                TryDeleteBackup();
            }

            // Only launch the client if the executable actually exists on disk.
            string clientExe = Path.Combine(AppFolderPath, APP_NAME);
            if (File.Exists(clientExe))
            {
                try
                { Process.Start(clientExe); }
                catch { /* nothing useful to do */ }
            }

            Environment.Exit(updateApplied || rollbackOk ? 0 : -1);
        }

        private static void TryDeleteBackup()
        {
            try
            {
                if (Directory.Exists(BackupFolderPath))
                {
                    Directory.Delete(BackupFolderPath, true);
                }
            }
            catch
            {
                // Backup cleanup failure must NOT trigger a rollback of an already-applied update.
            }
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
                    if (IsBackupFolder(dir.FullName))
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

                if (!DeleteAppFiles())
                {
                    return false;
                }

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
            DirectoryInfo dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
            }

            // Cache directories before we start copying — also lets us filter out the backup folder
            // if it happens to be a child of the source (which it is during BackupAppFiles).
            DirectoryInfo[] dirs = dir.GetDirectories();

            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
#if DEBUG
                if (file.Extension == ".pdb")
                {
                    continue;
                }
#endif

                file.CopyTo(targetFilePath, true);
            }

            foreach (DirectoryInfo subDir in dirs)
            {
                if (IsBackupFolder(subDir.FullName))
                {
                    continue;
                }

                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }

        // Case-insensitive, normalized path comparison: on Windows two paths that point to the same
        // folder may differ in casing or trailing separators, so a raw == check is unsafe.
        private static bool IsBackupFolder(string path)
        {
            return string.Equals(
                Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(BackupFolderPath).TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
