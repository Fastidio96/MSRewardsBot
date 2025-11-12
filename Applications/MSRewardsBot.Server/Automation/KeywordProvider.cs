using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MSRewardsBot.Server.Automation
{
    public class KeywordProvider : IKeywordProvider, IDisposable
    {
        private List<string> _keywords = new List<string>();
        private readonly Random _rnd = new Random();
        private readonly object _randomLock = new object();
        private readonly FileSystemWatcher _watcher;
        private readonly string _filePath;

        public KeywordProvider(string filePath)
        {
            _filePath = filePath;

            LoadKeywords();

            _watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(_filePath)!,
                Filter = Path.GetFileName(_filePath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };

            _watcher.Changed += (_, __) =>
            {
                // Small delay to avoid "file in use" issues
                Thread.Sleep(150);
                LoadKeywords();
            };

            _watcher.EnableRaisingEvents = true;
        }

        private void LoadKeywords()
        {
            if (!File.Exists(_filePath))
            {
                return;
            }

            string[] lines = File.ReadAllLines(_filePath);
            List<string> newList = new List<string>();

            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    newList.Add(line.Trim());
                }
            }

            Interlocked.Exchange(ref _keywords, newList);
        }

        public IReadOnlyList<string> GetAll()
        {
            return _keywords;
        }

        public string GetRandom()
        {
            lock (_randomLock)
            {
                if (_keywords.Count == 0)
                {
                    return null;
                }

                int i = _rnd.Next(_keywords.Count);
                return _keywords[i];
            }
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }
    }
}
