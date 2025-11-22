using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MSRewardsBot.Server.Automation
{
    public class KeywordProvider : IKeywordProvider, IDisposable
    {
        private readonly KeywordStore _store;

        private List<string> _keywords = new List<string>();
        private readonly object _randomLock = new object();
        private readonly FileSystemWatcher _watcher;
        private readonly string _filePath;

        private int _keywordIndex = 0;

        public KeywordProvider(KeywordStore store)
        {
            _store = store;
            _filePath = Utils.GetKeywordsFile();

            LoadKeywords();

            _watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(_filePath),
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

        public async Task<string> GetKeyword()
        {
            if (_keywords.Count == 0)
            {
                return null;
            }

            string keyword = _keywords[_keywordIndex];
            if (_keywordIndex > _keywords.Count)
            {
                await _store.RefreshList();
                _keywordIndex = 0;
            }
            else
            {
                _keywordIndex += 1;
            }

            return keyword;
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }
    }
}
