using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
