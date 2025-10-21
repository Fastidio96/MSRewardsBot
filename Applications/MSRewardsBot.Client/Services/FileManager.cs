using System;
using System.IO;
using System.Xml.Serialization;
using MSRewardsBot.Client.DataEntities;

namespace MSRewardsBot.Client.Services
{
    public class FileManager
    {
        private string _filePath => Path.Combine(_folderPath, "data.xml");
        private string _folderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MSRB");

        public bool SaveData(AppData data)
        {
            CheckDataFolder();

            try
            {
                using (FileStream fs = File.Open(_filePath, FileMode.OpenOrCreate))
                {
                    XmlSerializer xml = new XmlSerializer(typeof(AppData));
                    xml.Serialize(fs, data);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool LoadData(out AppData data)
        {
            data = new AppData();

            CheckDataFolder();

            try
            {
                using (FileStream fs = File.Open(_filePath, FileMode.OpenOrCreate))
                {
                    if (fs.Length == 0)
                    {
                        fs.Close();
                        return SaveData(data);
                    }

                    XmlSerializer xml = new XmlSerializer(typeof(AppData));
                    object obj = xml.Deserialize(fs);

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
                return false;
            }
        }

        private void CheckDataFolder()
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
                Utils.ShowError($"Cannot open or create the data folder: {_filePath}");
            }
        }
    }
}
