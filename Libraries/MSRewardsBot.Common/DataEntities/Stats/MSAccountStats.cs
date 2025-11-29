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
        public void PCSearchFailed()
        {
            CurrentPointsPCSearches -= _pointsPerSearch;
        }

        public void MobileSearchCompleted()
        {
            CurrentPointsMobileSearches += _pointsPerSearch;
        }
        public void MobileSearchFailed()
        {
            CurrentPointsMobileSearches -= _pointsPerSearch;
        }

        public int TotalAccountPoints
        {
            get => _totalAccountPoints;
            set
            {
                if (_totalAccountPoints != value)
                {
                    _totalAccountPoints = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private int _totalAccountPoints;

        #region PC searches

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

        #endregion

        #region Mobile searches

        public int CurrentPointsMobileSearches
        {
            get => _currentPointsMobileSearches;
            set
            {
                if (_currentPointsMobileSearches != value)
                {
                    _currentPointsMobileSearches = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private int _currentPointsMobileSearches;

        public int MaxPointsMobileSearches
        {
            get => _maxPointsMobileSearches;
            set
            {
                if (_maxPointsMobileSearches != value)
                {
                    _maxPointsMobileSearches = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private int _maxPointsMobileSearches;

        public int MobileSearchesToDo => (MaxPointsMobileSearches - CurrentPointsMobileSearches) / _pointsPerSearch;

        #endregion

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

        [JsonIgnore]
        public int UserId { get; set; }

        public void ChangeProperty(MSAccountStats stats, string propertyName)
        {
            switch (propertyName)
            {
                case nameof(TotalAccountPoints):
                    {
                        TotalAccountPoints = stats.TotalAccountPoints;
                        break;
                    }
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
                case nameof(CurrentPointsMobileSearches):
                    {
                        CurrentPointsMobileSearches = stats.CurrentPointsMobileSearches;
                        break;
                    }
                case nameof(MaxPointsMobileSearches):
                    {
                        MaxPointsMobileSearches = stats.MaxPointsMobileSearches;
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
