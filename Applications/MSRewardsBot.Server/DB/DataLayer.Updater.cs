using System;
using System.IO;
using System.Timers;
using Microsoft.Extensions.Logging;

namespace MSRewardsBot.Server.DB
{
    public partial class DataLayer
    {
        private System.Timers.Timer _timer;
        private string _clientVersionFileHash;

        public Version LatestClientVersion
        {
            get => _latestClientVersion;
            private set
            {
                if (value != _latestClientVersion)
                {
                    _latestClientVersion = value;
                }
            }
        }
        private Version _latestClientVersion;

        private void InitUpdater()
        {
            if (_timer == null)
            {
                _timer = new System.Timers.Timer(5000);
                _timer.Elapsed += PollingFileVersion_Elapsed;
                _timer.AutoReset = true;
                _timer.Start();
            }
        }

        private void PollingFileVersion_Elapsed(object? sender, ElapsedEventArgs e)
        {
            string hash = Utils.GetFileHash(Utils.GetVersionFile());

            if (_clientVersionFileHash == null || _clientVersionFileHash != hash)
            {
                _clientVersionFileHash = hash;
                LoadLatestVersionFile();
            }
        }

        private void LoadLatestVersionFile()
        {
            if (!File.Exists(Utils.GetVersionFile()))
            {
                _logger.LogCritical("The version file does not exists!");
                return;
            }

            string[] lines = File.ReadAllLines(Utils.GetVersionFile());
            if (lines.Length == 0 || string.IsNullOrEmpty(lines[0]))
            {
                _logger.LogCritical("The version file is empty!");
                return;
            }

            LatestClientVersion = new Version(lines[0]);
        }

        public byte[] GetClientUpdateFile()
        {
            try
            {
                if (!File.Exists(Utils.GetFileLatestUpdate()))
                {
                    return null;
                }

                return File.ReadAllBytes(Utils.GetFileLatestUpdate());
            }
            catch (Exception e) 
            {
                _logger.LogError("Error while reading client update file: {Err}", e);
                return null;
            }
        }
    }
}
