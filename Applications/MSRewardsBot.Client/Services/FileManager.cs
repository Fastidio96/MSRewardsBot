using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using MSRewardsBot.Client.DataEntities;

namespace MSRewardsBot.Client.Services
{
    public class FileManager
    {
        public static string GetFolderApp => new Uri(AppDomain.CurrentDomain.BaseDirectory).LocalPath;

        private static string _filePath => Path.Combine(_folderPath, "data.xml");
        private static string _folderPath => AppConstants.IS_PRODUCTION ?
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MSRB") :
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MSRB", "Debug");

        #region Data

        public static bool SaveData(AppData data)
        {
            CheckDataFolder();

            try
            {
                using (FileStream fs = File.Open(_filePath, FileMode.Create))
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    XmlSerializer xml = new XmlSerializer(typeof(AppData));
                    xml.Serialize(sw, data);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool LoadData(out AppData data)
        {
            data = new AppData();

            CheckDataFolder();

            try
            {
                using (FileStream fs = File.Open(_filePath, FileMode.OpenOrCreate))
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                {
                    if (fs.Length == 0)
                    {
                        return false;
                    }

                    XmlSerializer xml = new XmlSerializer(typeof(AppData));
                    object obj = xml.Deserialize(sr);

                    if (obj is AppData)
                    {
                        data = (AppData)obj;
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                ClearData();
                return false;
            }
        }

        private static void ClearData()
        {
            CheckDataFolder();

            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }

            SaveData(new AppData());
        }

        private static void CheckDataFolder()
        {
            try
            {
                if (!Directory.Exists(_folderPath))
                {
                    Directory.CreateDirectory(_folderPath);
                }
            }
            catch
            {
                Utils.ShowMessage($"Cannot open or create the data folder: {_filePath}");
            }
        }

        #endregion

        #region Updates

        public static string GetUpdateFolder()
        {
            string updateFolder = Path.Combine(GetFolderApp, "updates");
            if (!Directory.Exists(updateFolder))
            {
                Directory.CreateDirectory(updateFolder);
            }

            return updateFolder;
        }

        private static string GetBackupUpdateFolder()
        {
            string backupFolder = Path.Combine(GetFolderApp, "backup");
            if (!Directory.Exists(backupFolder))
            {
                Directory.CreateDirectory(backupFolder);
            }

            return backupFolder;
        }

        public static bool ApplyUpdate()
        {
            try
            {
                if (!BackupAppFiles())
                {
                    return false;
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
                if (Directory.Exists(GetBackupUpdateFolder()))
                {
                    Directory.Delete(GetBackupUpdateFolder(), true);
                }

                Directory.CreateDirectory(GetBackupUpdateFolder());

                string[] files = Directory.GetFiles(GetFolderApp);
                foreach (string file in files)
                {
                    if (Path.GetFileName(file) == "update.zip")
                    {
                        continue;
                    }

                    //File.Copy(file, )
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
