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

        private int _pointsPerSearch = 3;
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
        public DateTime LastServerCheck { get; set; }


        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
