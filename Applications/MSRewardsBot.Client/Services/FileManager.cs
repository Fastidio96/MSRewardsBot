using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using MSRewardsBot.Client.DataEntities;

namespace MSRewardsBot.Client.Services
{
    public class FileManager
    {
        public const string UPDATER_NAME = "MSRewardsBot.Client.Updater.exe";
        public static string AppFolderPath => new Uri(AppDomain.CurrentDomain.BaseDirectory).LocalPath;
        public static string LocalUpdatePackagePath => Path.Combine(AppFolderPath, "update.zip");
        public static string TempFolderUpdaterPath => Path.Combine(Path.GetTempPath(), "MSRB Client Updater");
        public static string LocalFolderUpdaterPath => Path.Combine(AppFolderPath, "updater");

        private static string _filePath => Path.Combine(_folderPath, "data.xml");
        private static string _folderPath => AppConstants.IS_PRODUCTION ?
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MSRB") :
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MSRB", "Debug");

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

        public static bool ApplyUpdate(byte[] file)
        {
            try
            {
                if (File.Exists(LocalUpdatePackagePath))
                {
                    File.Delete(LocalUpdatePackagePath);
                }

                File.WriteAllBytes(LocalUpdatePackagePath, file);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
