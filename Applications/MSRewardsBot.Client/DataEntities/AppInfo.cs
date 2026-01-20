using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR.Client;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Common.DataEntities.Stats;

namespace MSRewardsBot.Client.DataEntities
{
    public class AppInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public AppInfo()
        {
            Accounts = new ObservableCollection<MSAccount>();
            Accounts.CollectionChanged += Accounts_CollectionChanged;

            Version = Assembly.GetExecutingAssembly().GetName().Version;
        }

        ~AppInfo()
        {
            Accounts.CollectionChanged -= Accounts_CollectionChanged;
            Accounts.Clear();
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

        public HubConnectionState ConnectionState
        {
            get => _connectionState;
            set
            {
                if (_connectionState != value)
                {
                    _connectionState = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private HubConnectionState _connectionState;

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

        public MSAccount SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                _selectedAccount = value;
                SelectedAccountStats = _selectedAccount?.Stats;

                NotifyPropertyChanged(nameof(SelectedAccount));
            }
        }
        private MSAccount _selectedAccount;

        public MSAccountStats SelectedAccountStats
        {
            get => _selectedAccountStats;
            set
            {
                _selectedAccountStats = value;
                NotifyPropertyChanged(nameof(SelectedAccountStats));
            }
        }
        private MSAccountStats _selectedAccountStats;

        public ObservableCollection<MSAccount> Accounts { get; set; }

        public Version Version { get; set; }

        private void NotifyPropertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
