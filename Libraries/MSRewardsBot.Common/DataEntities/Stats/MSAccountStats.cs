using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MSRewardsBot.Common.DataEntities.Stats
{
    public class MSAccountStats : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public MSAccountStats()
        {
            LastDashboardUpdate = DateTime.MinValue;
        }

        public void PCSearchCompleted()
        {
            CurrentPointsPCSearches += _pointsPerSearch;
        }

        public int CurrentPointsPCSearches
        {
            get => _currentPointsPCSearches;
            set
            {
                if (_currentPointsPCSearches != value)
                {
                    _currentPointsPCSearches = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private int _currentPointsPCSearches;

        public int MaxPointsPCSearches
        {
            get => _maxPointsPCSearches;
            set
            {
                if (_maxPointsPCSearches != value)
                {
                    _maxPointsPCSearches = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private int _maxPointsPCSearches;

        private readonly int _pointsPerSearch = 3;
        public int PCSearchesToDo => (MaxPointsPCSearches - CurrentPointsPCSearches) / _pointsPerSearch;


        public int CurrentAccountLevel
        {
            get => _currentAccountLevel;
            set
            {
                if (_currentAccountLevel != value)
                {
                    _currentAccountLevel = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private int _currentAccountLevel;

        public int CurrentAccountLevelPoints
        {
            get => _currentAccountLevelPoints;
            set
            {
                if (_currentAccountLevelPoints != value)
                {
                    _currentAccountLevelPoints = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private int _currentAccountLevelPoints;

        public DateTime LastDashboardUpdate
        {
            get => _lastDashboardUpdate;
            set
            {
                _lastDashboardUpdate = value;

                if (_lastDashboardUpdate != value || _lastDashboardUpdate != DateTime.MinValue)
                {
                    NotifyPropertyChanged();
                }
            }
        }
        private DateTime _lastDashboardUpdate;

        [JsonIgnore]
        public DateTime LastDashboardCheck { get; set; }

        [JsonIgnore]
        public DateTime LastSearchesCheck { get; set; }

        public int MSAccountId { get; set; }
        public int UserId { get; set; }

        public void ChangeProperty(MSAccountStats stats, string propertyName)
        {
            switch (propertyName)
            {
                case nameof(CurrentAccountLevel):
                    {
                        CurrentAccountLevel = stats.CurrentAccountLevel;
                        break;
                    }
                case nameof(CurrentAccountLevelPoints):
                    {
                        CurrentAccountLevelPoints = stats.CurrentAccountLevelPoints;
                        break;
                    }
                case nameof(CurrentPointsPCSearches):
                    {
                        CurrentPointsPCSearches = stats.CurrentPointsPCSearches;
                        break;
                    }
                case nameof(MaxPointsPCSearches):
                    {
                        MaxPointsPCSearches = stats.MaxPointsPCSearches;
                        break;
                    }
                case nameof(LastDashboardUpdate):
                    {
                        LastDashboardUpdate = stats.LastDashboardUpdate;
                        break;
                    }
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
