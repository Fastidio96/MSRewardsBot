using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Common.Utilities;
using MSRewardsBot.Server.Automation;
using MSRewardsBot.Server.DataEntities;
using MSRewardsBot.Server.DataEntities.Updater;
using MSRewardsBot.Server.Helpers;
using MSRewardsBot.Server.Network;

namespace MSRewardsBot.Server.Core
{
    public class Updater : IDisposable
    {
        private readonly ILogger<Updater> _logger;
        private readonly IConnectionManager _connectionManager;
        private readonly CommandHubProxy _commandHubProxy;

        private const string API_URL = "https://api.github.com/repos/Fastidio96/MSRewardsBot/releases/latest";

        private ReleaseInfo _release;

        private Thread _updateChecker;
        private bool _isDisposing = false;

        public Updater(ILogger<Updater> logger, IConnectionManager connectionManager, CommandHubProxy hubProxy)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _commandHubProxy = hubProxy;
        }

        public void Start()
        {
            _connectionManager.ClientConnected += ConnectionManager_ClientConnected;
            _connectionManager.ClientUpdateVersion += ConnectionManager_ClientUpdateVersion;

            _updateChecker = new Thread(CheckUpdateLoop);
            _updateChecker.Name = nameof(CheckUpdateLoop);
            _updateChecker.Start();
        }

        private async void ConnectionManager_ClientUpdateVersion(object? sender, ClientArgs e)
        {
           await Utils.RetryAsync(new TimeSpan(0, 0, 30), async delegate ()
           {
              return await StartClientUpdate(e.ConnectionId);
           }, 3);
        }

        private async void ConnectionManager_ClientConnected(object? sender, ClientArgs e)
        {
            ClientInfo client = _connectionManager.GetConnection(e.ConnectionId);
            if (client == null)
            {
                return;
            }

            if (client.Version == null)
            {
                client.LastVersionRequest = DateTime.Now;
                await _commandHubProxy.RequestClientVersion(client.ConnectionId);
            }
        }

        private async void CheckUpdateLoop()
        {
            DateTime lastCheck = DateTime.MinValue;

            while (!_isDisposing)
            {
                DateTime now = DateTime.Now;

                try
                {
                    if (DateTimeUtilities.HasElapsed(now, lastCheck, new TimeSpan(0, 10, 0)))
                    {
                        lastCheck = now;

                        Version localVersion = GetCurrentVersion();
                        _release = await GetLatestGitHubVersion();

                        if (_release == null || _release.Version == null)
                        {
                            _logger.LogWarning("Cannot check latest version on GitHub");
                            continue;
                        }

                        if (localVersion < _release.Version)
                        {
                            _logger.LogInformation("A new version is available! Current version: {localVersion} | New version: {release}",
                                localVersion, _release.Version);

                            if (!await Utils.RetryAsync(new TimeSpan(0, 5, 0), DownloadUpdates, 5))
                            {
                                _logger.LogError("Cannot download the new release!");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error on updater: {ex}", ex.Message);
                }

                Thread.Sleep(5000);
            }
        }

        private async Task<bool> StartClientUpdate(string connectionId)
        {
            if (!File.Exists(Paths.GetPathClientUpdate()))
            {
                return false;
            }

            byte[] file = File.ReadAllBytes(Paths.GetPathClientUpdate());
            if (file == null || file.Length == 0)
            {
                return false;
            }

            DateTime now = DateTime.Now;
            ClientInfo client = _connectionManager.GetConnection(connectionId);

            if (client.Version == null || _release == null)
            {
                return false;
            }

            if (client.Version < _release.Version)
            {
                return true;
            }

            if (DateTimeUtilities.HasElapsed(now, client.LastServerCheck, new TimeSpan(0, 5, 0)))
            {
                client.LastServerCheck = now;
                await _commandHubProxy.SendClientUpdateFile(client.ConnectionId, file);
            }

            return true;
        }

        private async Task<bool> DownloadUpdates()
        {
            try
            {
                if (File.Exists(Paths.GetVersionFile()))
                {
                    string test = File.ReadAllText(Paths.GetVersionFile());

                    Version fileVersion = null;
                    if (string.IsNullOrEmpty(test) || !Version.TryParse(test, out fileVersion) || fileVersion == null)
                    {
                        File.Delete(Paths.GetVersionFile());
                    }

                    if (fileVersion >= _release.Version)
                    {
                        return true;
                    }
                }

                foreach (Asset asset in _release.Assets)
                {
                    if (asset.Name.StartsWith("msrb.client"))
                    {
                        _logger.LogDebug("Downloading update ({name}) for the client..", asset.Name);

                        if (File.Exists(Paths.GetPathClientUpdate()))
                        {
                            File.Delete(Paths.GetPathClientUpdate());
                        }

                        using (HttpClient client = new HttpClient())
                        {
                            client.DefaultRequestHeaders.Add("User-Agent", BrowserConstants.UA_PC_CHROME);
                            using (Stream sr = await client.GetStreamAsync(asset.DownloadUrl))
                            using (FileStream fs = new FileStream(Paths.GetPathClientUpdate(), FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                            {
                                await sr.CopyToAsync(fs);
                            }
                        }

                        if (!Utils.VerifyFileSha256(Paths.GetPathClientUpdate(), asset.Digest))
                        {
                            _logger.LogDebug("The file {name} is corrupted", asset.Name);

                            if (File.Exists(Paths.GetPathClientUpdate()))
                            {
                                File.Delete(Paths.GetPathClientUpdate());
                            }

                            return false;
                        }
                    }
                }

                if (!File.Exists(Paths.GetVersionFile()))
                {
                    using (FileStream fs = new FileStream(Paths.GetVersionFile(), FileMode.Create, FileAccess.Write))
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.Write(_release.Version.ToString());
                    }
                }

                return true;
            }
            catch (UnauthorizedAccessException unauthorizeEx)
            {
                try
                { Directory.Delete(Paths.GetFolderClientUpdate()); }
                catch { }
                _logger.LogError("Encountered an error while downloading from GitHub: {err}", unauthorizeEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError("Encountered an error while downloading from GitHub: {err}", ex.Message);
                return false;
            }
        }

        public Version GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        public async Task<ReleaseInfo> GetLatestGitHubVersion()
        {
            try
            {
                HttpResponseMessage response;
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", BrowserConstants.UA_PC_CHROME);
                    response = await client.GetAsync(API_URL);
                    response.EnsureSuccessStatusCode();
                }

                string json = await response.Content.ReadAsStringAsync();
                JsonNode node = JsonNode.Parse(json);

                ReleaseInfo releaseInfo = new ReleaseInfo();
                releaseInfo.TagName = (string)node["tag_name"];

                if (!Version.TryParse(releaseInfo.TagName.Replace("v", ""), out Version v))
                {
                    _logger.LogError("Cannot get the current version: {test}", releaseInfo.TagName);
                    return null;
                }

                releaseInfo.Version = v;
                releaseInfo.Assets = new List<Asset>();

                JsonArray assets = (JsonArray)node["assets"];

                foreach (JsonNode asset in assets)
                {
                    Asset assetInfo = new Asset
                    {
                        Name = (string)asset["name"],
                        DownloadUrl = (string)asset["browser_download_url"],
                        Digest = (string)asset["digest"],
                        ContentType = (string)asset["content_type"]
                    };

                    releaseInfo.Assets.Add(assetInfo);
                }

                return releaseInfo;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Encountered an error while checking a new version from GitHub: {err}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("Encountered an error while checking a new version from GitHub: {err}", ex.Message);
                return null;
            }
        }

        public void Dispose()
        {
            _isDisposing = true;

            _connectionManager.ClientConnected -= ConnectionManager_ClientConnected;
            _connectionManager.ClientUpdateVersion -= ConnectionManager_ClientUpdateVersion;

            if (_updateChecker != null)
            {
                if (_updateChecker.IsAlive)
                {
                    _updateChecker.Join(5000);
                }

                _updateChecker = null;
            }
        }
    }
}
