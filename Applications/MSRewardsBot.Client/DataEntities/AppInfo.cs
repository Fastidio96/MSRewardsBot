using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using MSRewardsBot.Common.DataEntities.Accounting;

namespace MSRewardsBot.Client.DataEntities
{
    /// <summary>
    /// Stored into RAM
    /// </summary>
    public class AppInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public AppInfo()
        {
            Accounts = new ObservableCollection<MSAccount>();
            Accounts.CollectionChanged += Accounts_CollectionChanged;

            Version = Assembly.GetExecutingAssembly().GetName().Version;
        }

        private void Accounts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged(nameof(Accounts));
        }

        public bool ConnectedToServer
        {
            get => _connectedToServer;
            set
            {
                if (_connectedToServer != value)
                {
                    _connectedToServer = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private bool _connectedToServer;

        public bool IsUserLogged
        {
            get => _isUserLogged;
            set
            {
                if (_isUserLogged != value)
                {
                    _isUserLogged = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private bool _isUserLogged;

        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string _username;

        public ObservableCollection<MSAccount> Accounts { get; set; }

        public Version Version { get; set; }

        private void NotifyPropertyChanged([CallerMemberName] string propName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
    }
}
